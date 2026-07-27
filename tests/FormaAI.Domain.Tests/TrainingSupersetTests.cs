using FormaAI.Domain.Training;

namespace FormaAI.Domain.Tests;

public sealed class TrainingSupersetTests
{
    [Fact]
    public void WorkoutExerciseCanJoinSupersetDuringLiveSession()
    {
        var exercise = new Exercise(null, "Wyciskanie", MuscleGroup.Chest, Equipment.Barbell, false, null);
        var workoutExercise = new WorkoutExercise(exercise, 1, 3, 6, 8, 2, 90);
        var groupId = Guid.NewGuid();

        workoutExercise.ConfigureSuperset(groupId, 2, 15, 120);

        Assert.Equal(groupId, workoutExercise.SupersetGroupId);
        Assert.Equal(2, workoutExercise.SupersetPosition);
        Assert.Equal(15, workoutExercise.IntervalSeconds);
        Assert.Equal(120, workoutExercise.RestSeconds);
    }

    [Fact]
    public void WorkoutSessionCanStoreCompletedCardio()
    {
        var session = new WorkoutSession("user-1", "Bieg i siła", null);
        var cardio = new WorkoutCardioEntry(session.Id, "Bieg na bieżni", 40, 5, null);

        session.CardioEntries.Add(cardio);

        Assert.Equal(40, Assert.Single(session.CardioEntries).DurationMinutes);
        Assert.Equal(5, cardio.DistanceKm);
    }

    [Fact]
    public void PlannedExerciseAcceptsValidSupersetSettings()
    {
        var groupId = Guid.NewGuid();

        var exercise = new PlannedExercise(
            Guid.NewGuid(),
            order: 1,
            sets: 3,
            minReps: 8,
            maxReps: 12,
            targetRir: 2,
            restSeconds: 90,
            supersetGroupId: groupId,
            supersetPosition: 1,
            intervalSeconds: 15);

        Assert.Equal(groupId, exercise.SupersetGroupId);
        Assert.Equal(1, exercise.SupersetPosition);
        Assert.Equal(15, exercise.IntervalSeconds);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3601)]
    public void PlannedExerciseRejectsIntervalOutsideAllowedRange(int intervalSeconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PlannedExercise(
            Guid.NewGuid(),
            order: 1,
            sets: 3,
            minReps: 8,
            maxReps: 12,
            targetRir: 2,
            restSeconds: 90,
            supersetGroupId: Guid.NewGuid(),
            supersetPosition: 1,
            intervalSeconds: intervalSeconds));
    }

    [Fact]
    public void PlannedExerciseRejectsSupersetPositionWithoutGroup()
    {
        Assert.Throws<ArgumentException>(() => new PlannedExercise(
            Guid.NewGuid(),
            order: 1,
            sets: 3,
            minReps: 8,
            maxReps: 12,
            targetRir: 2,
            restSeconds: 90,
            supersetGroupId: null,
            supersetPosition: 1,
            intervalSeconds: null));
    }

    [Fact]
    public void WorkoutExerciseCopiesSupersetSettingsFromPlan()
    {
        var groupId = Guid.NewGuid();
        var planned = new PlannedExercise(Guid.NewGuid(), 1, 3, 8, 12, 2, 120, groupId, 2, 10);
        var exercise = new Exercise("user", "Wiosłowanie", MuscleGroup.Back, Equipment.Cable);

        var workoutExercise = new WorkoutExercise(planned, exercise);

        Assert.Equal(groupId, workoutExercise.SupersetGroupId);
        Assert.Equal(2, workoutExercise.SupersetPosition);
        Assert.Equal(10, workoutExercise.IntervalSeconds);
    }
}
