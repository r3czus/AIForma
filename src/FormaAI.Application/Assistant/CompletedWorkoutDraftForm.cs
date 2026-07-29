using FormaAI.Contracts.Assistant;

namespace FormaAI.Application.Assistant;

public sealed class CompletedWorkoutDraftForm
{
    public Guid Id { get; init; }
    public DateOnly LocalDate { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<CompletedWorkoutExerciseForm> Exercises { get; set; } = [];
    public List<CompletedWorkoutCardioForm> Cardio { get; set; } = [];

    public static CompletedWorkoutDraftForm From(AssistantCompletedWorkoutDraftResponse response) =>
        new()
        {
            Id = response.Id,
            LocalDate = response.LocalDate,
            Name = response.Name,
            Exercises = response.Exercises.Select(x => new CompletedWorkoutExerciseForm
            {
                ExerciseId = x.ExerciseId,
                ExerciseName = x.ExerciseName,
                Sets = x.Sets.Select(set => new CompletedWorkoutSetForm
                {
                    WeightKg = set.WeightKg,
                    Repetitions = set.Repetitions,
                    Rir = set.Rir
                }).ToList()
            }).ToList(),
            Cardio = (response.Cardio ?? []).Select(x => new CompletedWorkoutCardioForm
            {
                ActivityName = x.ActivityName,
                DurationMinutes = x.DurationMinutes,
                DistanceKm = x.DistanceKm,
                AverageHeartRate = x.AverageHeartRate
            }).ToList()
        };

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Podaj nazwę treningu.");
        else if (Name.Trim().Length > 150)
            errors.Add("Nazwa treningu może mieć maksymalnie 150 znaków.");
        if (Exercises.Count > 20)
            errors.Add("Szkic może zawierać maksymalnie 20 ćwiczeń.");
        if (Exercises.Count == 0 && Cardio.Count == 0)
            errors.Add("Szkic musi zawierać ćwiczenie siłowe albo aktywność cardio.");
        foreach (var exercise in Exercises)
        {
            if (exercise.Sets.Count is < 1 or > 50)
                errors.Add($"{exercise.ExerciseName}: dodaj co najmniej jedną serię.");
            if (exercise.Sets.Any(set =>
                    set.WeightKg is < 0 or > 1000 ||
                    set.Repetitions is < 1 or > 1000 ||
                    set.Rir is < 0 or > 10))
                errors.Add($"{exercise.ExerciseName}: sprawdź ciężar, powtórzenia i RIR.");
        }
        foreach (var cardio in Cardio)
        {
            if (string.IsNullOrWhiteSpace(cardio.ActivityName) ||
                cardio.ActivityName.Trim().Length > 150 ||
                cardio.DurationMinutes is < 1 or > 1440 ||
                cardio.DistanceKm is < 0 or > 1000 ||
                cardio.AverageHeartRate is < 30 or > 250)
                errors.Add("Sprawdź nazwę, czas, dystans i tętno aktywności cardio.");
        }
        return errors;
    }

    public UpdateAssistantCompletedWorkoutDraftRequest ToRequest()
    {
        var errors = Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));
        return new(
            Name.Trim(),
            LocalDate,
            Exercises.Select(x => new AssistantWorkoutExerciseDraft(
                x.ExerciseId,
                x.ExerciseName,
                x.Sets.Select(set => new AssistantWorkoutSetDraft(
                    set.WeightKg,
                    set.Repetitions,
                    set.Rir)).ToList())).ToList(),
            Cardio.Select(x => new AssistantWorkoutCardioDraft(
                x.ActivityName.Trim(),
                x.DurationMinutes,
                x.DistanceKm,
                x.AverageHeartRate)).ToList());
    }
}

public sealed class CompletedWorkoutExerciseForm
{
    public Guid ExerciseId { get; init; }
    public string ExerciseName { get; init; } = string.Empty;
    public List<CompletedWorkoutSetForm> Sets { get; set; } = [];
}

public sealed class CompletedWorkoutSetForm
{
    public decimal WeightKg { get; set; }
    public int Repetitions { get; set; }
    public decimal? Rir { get; set; }
}

public sealed class CompletedWorkoutCardioForm
{
    public string ActivityName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? AverageHeartRate { get; set; }
}
