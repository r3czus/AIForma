using FormaAI.Contracts.Training;

namespace FormaAI.Application.Training;

public sealed class QuickWorkoutDraft(string name = "Trening na dziś", int minutes = 45)
{
    public string Name { get; set; } = name;
    public int Minutes { get; set; } = minutes;
    public List<QuickWorkoutExerciseDraft> Exercises { get; } = [];

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Podaj nazwę treningu.");
        else if (Name.Trim().Length > 120)
            errors.Add("Nazwa treningu może mieć maksymalnie 120 znaków.");
        if (Minutes is < 5 or > 300)
            errors.Add("Planowany czas musi mieścić się w zakresie 5–300 minut.");
        if (Exercises.Count == 0)
            errors.Add("Dodaj co najmniej jedno ćwiczenie.");
        if (Exercises.Count > 20)
            errors.Add("Trening może zawierać maksymalnie 20 ćwiczeń.");
        if (Exercises.Select(x => x.Exercise.Id).Distinct().Count() != Exercises.Count)
            errors.Add("Każde ćwiczenie można dodać tylko raz.");
        if (Exercises.Count > 0 && Exercises[^1].LinkWithNext)
            errors.Add("Ostatnie ćwiczenie nie może rozpoczynać superserii.");

        foreach (var exercise in Exercises)
        {
            if (exercise.Sets is < 1 or > 10)
                errors.Add($"{exercise.Exercise.Name}: liczba serii musi mieścić się w zakresie 1–10.");
            if (exercise.MinReps is < 1 or > 100 ||
                exercise.MaxReps is < 1 or > 100 ||
                exercise.MinReps > exercise.MaxReps)
                errors.Add($"{exercise.Exercise.Name}: sprawdź zakres powtórzeń.");
            if (exercise.TargetRir is < 0 or > 10)
                errors.Add($"{exercise.Exercise.Name}: RIR musi mieścić się w zakresie 0–10.");
            if (exercise.RestSeconds is < 0 or > 3600)
                errors.Add($"{exercise.Exercise.Name}: przerwa musi mieścić się w zakresie 0–3600 sekund.");
            if (exercise.IntervalSeconds is < 0 or > 3600)
                errors.Add($"{exercise.Exercise.Name}: interwał musi mieścić się w zakresie 0–3600 sekund.");
            if (exercise.WeightKg is < 0 or > 1000)
                errors.Add($"{exercise.Exercise.Name}: ciężar musi mieścić się w zakresie 0–1000 kg.");
            if (exercise.CompletedRepetitions is < 1 or > 1000)
                errors.Add($"{exercise.Exercise.Name}: wykonane powtórzenia muszą mieścić się w zakresie 1–1000.");
        }

        return errors;
    }

    public StartQuickWorkoutRequest ToRequest()
    {
        var errors = Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        var groups = SupersetAssignments();
        var exercises = Exercises
            .Select((exercise, index) =>
            {
                var assignment = groups[index];
                return new QuickWorkoutExerciseRequest(
                    exercise.Exercise.Id,
                    exercise.Sets,
                    exercise.MinReps,
                    exercise.MaxReps,
                    exercise.TargetRir,
                    exercise.RestSeconds,
                    assignment.GroupId,
                    assignment.Position,
                    assignment.GroupId is null ? null : exercise.IntervalSeconds,
                    Enumerable.Range(1, exercise.Sets)
                        .Select(setNumber => new QuickWorkoutSetPresetRequest(
                            setNumber,
                            exercise.WeightKg,
                            exercise.CompletedRepetitions,
                            exercise.TargetRir))
                        .ToList());
            })
            .ToList();

        return new StartQuickWorkoutRequest(Name.Trim(), Minutes, exercises);
    }

    public SaveCompletedWorkoutRequest ToCompletedRequest(DateOnly localDate)
    {
        var errors = Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        return new SaveCompletedWorkoutRequest(
            localDate,
            Name.Trim(),
            Exercises.Select(exercise => new CompletedWorkoutExerciseRequest(
                exercise.Exercise.Id,
                exercise.Exercise.Name,
                Enumerable.Range(0, exercise.Sets)
                    .Select(_ => new CompletedWorkoutSetRequest(
                        exercise.WeightKg,
                        exercise.CompletedRepetitions,
                        exercise.TargetRir))
                    .ToList()))
                .ToList());
    }

    private SupersetAssignment[] SupersetAssignments()
    {
        var assignments = Enumerable
            .Range(0, Exercises.Count)
            .Select(_ => new SupersetAssignment(null, null))
            .ToArray();

        var index = 0;
        while (index < Exercises.Count - 1)
        {
            if (!Exercises[index].LinkWithNext)
            {
                index++;
                continue;
            }

            var groupId = Guid.NewGuid();
            var position = 1;
            assignments[index] = new(groupId, position++);
            while (index < Exercises.Count - 1 && Exercises[index].LinkWithNext)
            {
                index++;
                assignments[index] = new(groupId, position++);
            }
            index++;
        }

        return assignments;
    }

    private sealed record SupersetAssignment(Guid? GroupId, int? Position);
}

public sealed class QuickWorkoutExerciseDraft(ExerciseResponse exercise, int sets = 3)
{
    public ExerciseResponse Exercise { get; } = exercise;
    public int Sets { get; set; } = sets;
    public int MinReps { get; set; } = 8;
    public int MaxReps { get; set; } = 12;
    public decimal? TargetRir { get; set; } = 2;
    public int? RestSeconds { get; set; } = 90;
    public int? IntervalSeconds { get; set; } = 15;
    public decimal WeightKg { get; set; }
    public int CompletedRepetitions { get; set; } = 8;
    public bool LinkWithNext { get; set; }
}
