using FormaAI.Application.Nutrition;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Users;

namespace FormaAI.Application.Tests;

public sealed class NutritionCalculatorTests
{
    [Fact]
    public void CalculatesMacrosForExactAmountWithoutEarlyRounding()
    {
        var product = new Product("user-1", "Skyr", null, 64.4m, 12.3m, 0.2m, 3.8m);

        var macro = NutritionCalculator.ForProduct(product, 180);

        Assert.Equal(115.92m, macro.CaloriesKcal);
        Assert.Equal(22.14m, macro.ProteinG);
        Assert.Equal(0.36m, macro.FatG);
        Assert.Equal(6.84m, macro.CarbohydratesG);
    }

    [Fact]
    public void RemainingMacrosCanBeNegative()
    {
        var target = new Macro(2000, 150, 70, 220);
        var consumed = new Macro(2100, 140, 75, 230);

        var remaining = target - consumed;

        Assert.Equal(-100, remaining.CaloriesKcal);
        Assert.Equal(10, remaining.ProteinG);
        Assert.Equal(-5, remaining.FatG);
        Assert.Equal(-10, remaining.CarbohydratesG);
    }

    [Fact]
    public void TrainingBonusUsesOnlyCompletedWork()
    {
        Assert.Equal(0, NutritionCalculator.TrainingBonus(ActivityLevel.Moderate, TimeSpan.Zero, 0));
        Assert.Equal(195, NutritionCalculator.TrainingBonus(ActivityLevel.Moderate, TimeSpan.FromMinutes(30), 15));
    }

    [Theory]
    [InlineData(BodyGoal.Reduction)]
    [InlineData(BodyGoal.Maintenance)]
    [InlineData(BodyGoal.MuscleGain)]
    public void GoalCalculationAlwaysLeavesEnergyForCarbohydrates(BodyGoal goal)
    {
        var result = NutritionGoalCalculator.Calculate(150m, 181m, 35, BiologicalSex.Male, ActivityLevel.Low, goal, .7m);

        Assert.True(result.CarbohydratesG > 0);
        Assert.True(result.CarbohydratesG * 4m >= result.CaloriesKcal * .2m - 4m);
    }
}
