using FormaAI.Application.Training;

namespace FormaAI.Application.Tests;

public sealed class WorkoutSequenceTests
{
    [Fact]
    public void MiddleOfSupersetUsesIntervalAndNextGroupMember()
    {
        var groupId = Guid.NewGuid();
        var first = State(1, planned: 3, completed: 1, groupId, position: 1, interval: 12, rest: 90);
        var second = State(2, planned: 3, completed: 0, groupId, position: 2, interval: 12, rest: 90);

        var step = WorkoutSequence.Next([first, second], first.Id);

        Assert.Equal(new WorkoutStep(second.Id, WorkoutTimerKind.Interval, 12), step);
    }

    [Fact]
    public void EndOfSupersetRoundUsesRestAndReturnsFirstMember()
    {
        var groupId = Guid.NewGuid();
        var first = State(1, planned: 3, completed: 1, groupId, position: 1, interval: 10, rest: 105);
        var second = State(2, planned: 3, completed: 1, groupId, position: 2, interval: 10, rest: 105);

        var step = WorkoutSequence.Next([first, second], second.Id);

        Assert.Equal(new WorkoutStep(first.Id, WorkoutTimerKind.Rest, 105), step);
    }

    [Fact]
    public void FinishedSupersetMovesToNextExerciseAfterRest()
    {
        var groupId = Guid.NewGuid();
        var first = State(1, planned: 1, completed: 1, groupId, position: 1, interval: 10, rest: 120);
        var second = State(2, planned: 1, completed: 1, groupId, position: 2, interval: 10, rest: 120);
        var third = State(3, planned: 3, completed: 0, null, null, null, rest: 75);

        var step = WorkoutSequence.Next([first, second, third], second.Id);

        Assert.Equal(new WorkoutStep(third.Id, WorkoutTimerKind.Rest, 120), step);
    }

    [Fact]
    public void StandaloneExerciseUsesRestBetweenSets()
    {
        var exercise = State(1, planned: 3, completed: 1, null, null, null, rest: 75);

        var step = WorkoutSequence.Next([exercise], exercise.Id);

        Assert.Equal(new WorkoutStep(exercise.Id, WorkoutTimerKind.Rest, 75), step);
    }

    [Fact]
    public void CompletedWorkoutReturnsNoTimer()
    {
        var exercise = State(1, planned: 1, completed: 1, null, null, null, rest: 75);

        var step = WorkoutSequence.Next([exercise], exercise.Id);

        Assert.Equal(new WorkoutStep(null, WorkoutTimerKind.None, 0), step);
    }

    private static WorkoutExerciseState State(
        int order,
        int planned,
        int completed,
        Guid? groupId,
        int? position,
        int? interval,
        int? rest) =>
        new(Guid.NewGuid(), order, planned, completed, groupId, position, interval, rest);
}
