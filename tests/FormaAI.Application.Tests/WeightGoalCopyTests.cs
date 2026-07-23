using FormaAI.Application.Progress;

namespace FormaAI.Application.Tests;

public sealed class WeightGoalCopyTests
{
    [Theory]
    [InlineData(80, 75, "Do zrzucenia 5 kg")]
    [InlineData(75, 80, "Do przybrania 5 kg")]
    [InlineData(80, 80, "Cel zakłada utrzymanie obecnej masy")]
    public void DescribesWeightDifference(decimal current, decimal target, string expected) =>
        Assert.Equal(expected, WeightGoalCopy.For(current, target));
}
