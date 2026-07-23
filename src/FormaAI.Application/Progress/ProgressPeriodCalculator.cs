namespace FormaAI.Application.Progress;

public static class ProgressPeriodCalculator
{
    public static (DateOnly From, DateOnly To)? EligibleRange(
        DateOnly from,
        DateOnly to,
        DateOnly today,
        DateOnly? firstTargetDate)
    {
        if (firstTargetDate is null) return null;

        var start = DateOnly.FromDayNumber(Math.Max(from.DayNumber, firstTargetDate.Value.DayNumber));
        var end = DateOnly.FromDayNumber(Math.Min(to.DayNumber, today.DayNumber));
        return start > end ? null : (start, end);
    }
}
