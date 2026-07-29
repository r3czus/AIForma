using FormaAI.Application.Training;
using FormaAI.Contracts.Training;
using FormaAI.Domain.Training;

namespace FormaAI.Application.Tests;

public sealed class QuickWorkoutDraftTests
{
    [Fact]
    public void ToRequestConnectsConsecutiveExercisesIntoOneSuperset()
    {
        var draft = new QuickWorkoutDraft("Góra", 50);
        draft.Exercises.Add(new QuickWorkoutExerciseDraft(Exercise("Wyciskanie"), 3) { LinkWithNext = true, IntervalSeconds = 20 });
        draft.Exercises.Add(new QuickWorkoutExerciseDraft(Exercise("Wiosłowanie"), 3) { LinkWithNext = true, IntervalSeconds = 30 });
        draft.Exercises.Add(new QuickWorkoutExerciseDraft(Exercise("Rozpiętki"), 3) { IntervalSeconds = 75 });

        var request = draft.ToRequest();

        Assert.NotNull(request.Exercises[0].SupersetGroupId);
        Assert.Equal(request.Exercises[0].SupersetGroupId, request.Exercises[1].SupersetGroupId);
        Assert.Equal(request.Exercises[1].SupersetGroupId, request.Exercises[2].SupersetGroupId);
        Assert.Equal(new int?[] { 1, 2, 3 }, request.Exercises.Select(x => x.SupersetPosition));
        Assert.Equal(new int?[] { 20, 30, 75 }, request.Exercises.Select(x => x.IntervalSeconds));
    }

    [Fact]
    public void ToRequestKeepsStandaloneExerciseOutsideSuperset()
    {
        var draft = new QuickWorkoutDraft();
        draft.Exercises.Add(new QuickWorkoutExerciseDraft(Exercise("Przysiad"), 4));

        var item = Assert.Single(draft.ToRequest().Exercises);

        Assert.Null(item.SupersetGroupId);
        Assert.Null(item.SupersetPosition);
        Assert.Null(item.IntervalSeconds);
    }

    [Fact]
    public void ValidateRejectsEmptyDraftAndInvalidRepRange()
    {
        var draft = new QuickWorkoutDraft();

        Assert.Contains(draft.Validate(), error => error.Contains("ćwiczenie", StringComparison.OrdinalIgnoreCase));

        draft.Exercises.Add(new QuickWorkoutExerciseDraft(Exercise("Martwy ciąg"), 3)
        {
            MinReps = 12,
            MaxReps = 8
        });

        Assert.Contains(draft.Validate(), error => error.Contains("zakres", StringComparison.OrdinalIgnoreCase));
        Assert.Throws<InvalidOperationException>(() => draft.ToRequest());
    }

    [Fact]
    public void DraftBuildsLivePresetsAndCompletedSetsFromEnteredPerformance()
    {
        var draft = new QuickWorkoutDraft("Zaległy trening", 45);
        draft.Exercises.Add(new QuickWorkoutExerciseDraft(Exercise("Wyciskanie"), 3)
        {
            WeightKg = 82.5m,
            CompletedRepetitions = 8,
            TargetRir = 2
        });

        var live = Assert.Single(draft.ToRequest().Exercises);
        var completed = Assert.Single(draft.ToCompletedRequest(new DateOnly(2026, 7, 20)).Exercises);

        Assert.Equal(3, live.Presets!.Count);
        Assert.All(live.Presets, set =>
        {
            Assert.Equal(82.5m, set.WeightKg);
            Assert.Equal(8, set.Repetitions);
            Assert.Equal(2, set.Rir);
        });
        Assert.Equal(new DateOnly(2026, 7, 20), draft.ToCompletedRequest(new DateOnly(2026, 7, 20)).LocalDate);
        Assert.Equal(3, completed.Sets.Count);
        Assert.All(completed.Sets, set =>
        {
            Assert.Equal(82.5m, set.WeightKg);
            Assert.Equal(8, set.Repetitions);
            Assert.Equal(2, set.Rir);
        });
    }

    private static ExerciseResponse Exercise(string name) =>
        new(Guid.NewGuid(), name, MuscleGroup.FullBody, Equipment.Barbell, false, false);
}
