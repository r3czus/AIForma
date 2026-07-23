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
}
