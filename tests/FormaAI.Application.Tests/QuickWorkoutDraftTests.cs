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

    private static ExerciseResponse Exercise(string name) =>
        new(Guid.NewGuid(), name, MuscleGroup.FullBody, Equipment.Barbell, false, false);
}
