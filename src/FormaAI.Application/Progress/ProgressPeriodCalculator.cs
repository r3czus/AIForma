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

    public static decimal AdherencePercent(int successfulDays, int eligibleDays) =>
        eligibleDays == 0 ? 0 : decimal.Round(Math.Min(successfulDays, eligibleDays) * 100m / eligibleDays, 1);

    public static bool IsInRange(DateOnly date, (DateOnly From, DateOnly To)? range) =>
        range is not null && date >= range.Value.From && date <= range.Value.To;

    public static (DateTime From, DateTime ToExclusive) UtcBounds(
        DateOnly from,
        DateOnly to,
        TimeZoneInfo zone)
    {
        var localFrom = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        var localToExclusive = DateTime.SpecifyKind(to.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(localFrom, zone), TimeZoneInfo.ConvertTimeToUtc(localToExclusive, zone));
    }
}
