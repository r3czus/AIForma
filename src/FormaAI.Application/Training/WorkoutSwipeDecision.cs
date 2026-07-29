namespace FormaAI.Application.Training;

public static class WorkoutSwipeDecision
{
    public const double DefaultThreshold = 56;
    public const double HorizontalDominance = 1.25;

    public static int TargetIndex(
        int activeIndex,
        int exerciseCount,
        double deltaX,
        double deltaY)
    {
        if (exerciseCount <= 0)
            return 0;
        if (activeIndex < 0 || activeIndex >= exerciseCount)
            throw new ArgumentOutOfRangeException(nameof(activeIndex));

        var horizontalDistance = Math.Abs(deltaX);
        if (horizontalDistance < DefaultThreshold ||
            horizontalDistance <= Math.Abs(deltaY) * HorizontalDominance)
            return activeIndex;

        if (deltaX < 0)
            return Math.Min(activeIndex + 1, exerciseCount - 1);

        return Math.Max(activeIndex - 1, 0);
    }
}
