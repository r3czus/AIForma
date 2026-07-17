using FormaAI.Application.Progress;

namespace FormaAI.Application.Tests;

public sealed class ProgressMetricsTests
{
    [Fact]
    public void CalculatesVolumeAndEpleyEstimate()
    {
        Assert.Equal(500m, ProgressMetrics.Volume(50m, 10));
        Assert.Equal(66.67m, ProgressMetrics.EstimatedOneRepMax(50m, 10));
        Assert.Null(ProgressMetrics.EstimatedOneRepMax(50m, 15));
        Assert.Equal(25m, ProgressMetrics.PercentageChange(400m, 500m));
    }
}
