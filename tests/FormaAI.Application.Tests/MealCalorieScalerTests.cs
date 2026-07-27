using FormaAI.Application.Nutrition;

namespace FormaAI.Application.Tests;

public sealed class MealCalorieScalerTests
{
    [Fact]
    public void ScaleAmountsPreservesIngredientProportions()
    {
        var result = MealCalorieScaler.ScaleAmounts([100m, 200m, 10m], 500m, 750m);

        Assert.Equal([150m, 300m, 15m], result);
    }

    [Fact]
    public void ScaleAmountsCanReduceTheMeal()
    {
        var result = MealCalorieScaler.ScaleAmounts([100m, 25m], 800m, 400m);

        Assert.Equal([50m, 12.5m], result);
    }

    [Theory]
    [InlineData(0, 500)]
    [InlineData(-1, 500)]
    [InlineData(500, 0)]
    [InlineData(500, -1)]
    public void ScaleAmountsRejectsInvalidCalories(decimal currentCalories, decimal targetCalories)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealCalorieScaler.ScaleAmounts([100m], currentCalories, targetCalories));
    }

    [Fact]
    public void ScaleAmountsRejectsAnEmptyMeal()
    {
        Assert.Throws<ArgumentException>(() =>
            MealCalorieScaler.ScaleAmounts([], 500m, 600m));
    }

    [Fact]
    public void ScaleAmountsDoesNotRoundASmallIngredientToZero()
    {
        var result = MealCalorieScaler.ScaleAmounts([0.1m], 1000m, 100m);

        Assert.Equal(0.1m, result[0]);
    }
}
