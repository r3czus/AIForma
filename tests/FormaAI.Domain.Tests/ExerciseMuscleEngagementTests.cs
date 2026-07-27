using FormaAI.Domain.Training;

namespace FormaAI.Domain.Tests;

public sealed class ExerciseMuscleEngagementTests
{
    [Fact]
    public void ForearmsAreAvailableForCatalogEngagements()
    {
        Assert.True(Enum.IsDefined(MuscleGroup.Forearms));
    }

    [Fact]
    public void EngagementsMustAddUpToOneHundredPercent()
    {
        var exercise = new Exercise("user", "Wyciskanie", MuscleGroup.Chest, Equipment.Barbell);

        Assert.Throws<ArgumentException>(() => exercise.SetMuscleEngagements([
            (MuscleGroup.Chest, 60),
            (MuscleGroup.Triceps, 20)
        ]));
    }

    [Fact]
    public void LargestEngagementBecomesPrimaryAndIsCopiedToWorkout()
    {
        var exercise = new Exercise("user", "Wyciskanie", MuscleGroup.Chest, Equipment.Barbell);
        exercise.SetMuscleEngagements([(MuscleGroup.Chest, 60), (MuscleGroup.Triceps, 25), (MuscleGroup.Shoulders, 15)]);

        var workout = new WorkoutExercise(exercise, 1, 4, 6, 10, 2, 120);
        exercise.SetMuscleEngagements([(MuscleGroup.Triceps, 100)]);

        Assert.Equal(MuscleGroup.Triceps, exercise.PrimaryMuscleGroup);
        Assert.Equal(100, workout.MuscleEngagements.Sum(x => x.Percentage));
        Assert.Contains(workout.MuscleEngagements, x => x.MuscleGroup == MuscleGroup.Chest && x.Percentage == 60);
    }

    [Fact]
    public void QuickWorkoutHasNoPlanReferencesAndKeepsTimeLimit()
    {
        var session = new WorkoutSession("user", "Trening własny", 45);

        Assert.Null(session.TrainingPlanId);
        Assert.Null(session.TrainingDayId);
        Assert.Equal("Trening własny", session.NameSnapshot);
        Assert.Equal(45, session.TimeLimitMinutes);
        Assert.Equal(SessionStatus.InProgress, session.Status);
    }

    [Fact]
    public void ReplacingExerciseReusesSnapshotRowsWhenEngagementCountMatches()
    {
        var original = new Exercise("user", "Wyciskanie", MuscleGroup.Chest, Equipment.Barbell);
        var replacement = new Exercise("user", "Wiosłowanie", MuscleGroup.Back, Equipment.Cable);
        var workout = new WorkoutExercise(original, 1, 3, 8, 12, 2, 90);
        var snapshotId = Assert.Single(workout.MuscleEngagements).Id;

        workout.ReplaceExercise(replacement);

        var engagement = Assert.Single(workout.MuscleEngagements);
        Assert.Equal(snapshotId, engagement.Id);
        Assert.Equal(MuscleGroup.Back, engagement.MuscleGroup);
        Assert.Equal(100, engagement.Percentage);
    }
}
