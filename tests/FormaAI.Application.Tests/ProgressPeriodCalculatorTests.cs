using FormaAI.Application.Progress;

namespace FormaAI.Application.Tests;

public sealed class ProgressPeriodCalculatorTests
{
    [Fact]
    public void ExcludesFutureDaysAndDaysBeforeFirstTarget()
    {
        var range = ProgressPeriodCalculator.EligibleRange(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 5));

        Assert.Equal(new DateOnly(2026, 7, 5), range!.Value.From);
        Assert.Equal(new DateOnly(2026, 7, 18), range.Value.To);
    }

    [Fact]
    public void ReturnsNoRangeBeforeFirstTarget()
    {
        var range = ProgressPeriodCalculator.EligibleRange(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 18),
            new DateOnly(2026, 7, 20));

        Assert.Null(range);
    }

    [Fact]
    public void OneSuccessfulDayOutOfEighteenIsNotAFullBar() =>
        Assert.Equal(5.6m, ProgressPeriodCalculator.AdherencePercent(1, 18));

    [Fact]
    public void AdherenceCannotExceedOneHundredPercent() =>
        Assert.Equal(100m, ProgressPeriodCalculator.AdherencePercent(19, 18));

    [Fact]
    public void ConvertsLocalPeriodBoundariesToUtc()
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");

        var bounds = ProgressPeriodCalculator.UtcBounds(
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 18),
            zone);

        Assert.Equal(new DateTime(2026, 6, 30, 22, 0, 0, DateTimeKind.Utc), bounds.From);
        Assert.Equal(new DateTime(2026, 7, 18, 22, 0, 0, DateTimeKind.Utc), bounds.ToExclusive);
    }

    [Fact]
    public void IncludesOnlyDatesInsideEligibleRange()
    {
        var range = (new DateOnly(2026, 7, 5), new DateOnly(2026, 7, 18));

        Assert.False(ProgressPeriodCalculator.IsInRange(new DateOnly(2026, 7, 4), range));
        Assert.True(ProgressPeriodCalculator.IsInRange(new DateOnly(2026, 7, 5), range));
        Assert.True(ProgressPeriodCalculator.IsInRange(new DateOnly(2026, 7, 18), range));
        Assert.False(ProgressPeriodCalculator.IsInRange(new DateOnly(2026, 7, 19), range));
    }
}
