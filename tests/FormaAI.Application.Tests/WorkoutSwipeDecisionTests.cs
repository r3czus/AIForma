using FormaAI.Application.Training;

namespace FormaAI.Application.Tests;

public sealed class WorkoutSwipeDecisionTests
{
    [Theory]
    [InlineData(55.9)]
    [InlineData(-55.9)]
    public void MovementBelowThresholdKeepsCurrentExercise(double deltaX)
    {
        var target = WorkoutSwipeDecision.TargetIndex(2, 5, deltaX, 0);

        Assert.Equal(2, target);
    }

    [Theory]
    [InlineData(56, 0, 1)]
    [InlineData(-56, 0, 3)]
    [InlineData(400, 0, 1)]
    [InlineData(-400, 0, 3)]
    public void HorizontalSwipeMovesExactlyOneExercise(double deltaX, double deltaY, int expected)
    {
        var target = WorkoutSwipeDecision.TargetIndex(2, 5, deltaX, deltaY);

        Assert.Equal(expected, target);
    }

    [Theory]
    [InlineData(100, 81)]
    [InlineData(-100, 81)]
    [InlineData(56, 56)]
    public void VerticalOrDiagonalMovementDoesNotNavigate(double deltaX, double deltaY)
    {
        var target = WorkoutSwipeDecision.TargetIndex(2, 5, deltaX, deltaY);

        Assert.Equal(2, target);
    }

    [Theory]
    [InlineData(0, 5, 100)]
    [InlineData(4, 5, -100)]
    public void SwipeAtExerciseEdgeKeepsCurrentExercise(int activeIndex, int exerciseCount, double deltaX)
    {
        var target = WorkoutSwipeDecision.TargetIndex(activeIndex, exerciseCount, deltaX, 0);

        Assert.Equal(activeIndex, target);
    }
}
