using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Users;

namespace FormaAI.Application.Nutrition;

public readonly record struct Macro(decimal CaloriesKcal, decimal ProteinG, decimal FatG, decimal CarbohydratesG)
{
    public static Macro operator +(Macro a, Macro b) => new(
        a.CaloriesKcal + b.CaloriesKcal,
        a.ProteinG + b.ProteinG,
        a.FatG + b.FatG,
        a.CarbohydratesG + b.CarbohydratesG);

    public static Macro operator -(Macro a, Macro b) => new(
        a.CaloriesKcal - b.CaloriesKcal,
        a.ProteinG - b.ProteinG,
        a.FatG - b.FatG,
        a.CarbohydratesG - b.CarbohydratesG);
}

public static class NutritionCalculator
{
    public static Macro ForProduct(Product product, decimal amountGrams) => new(
        product.CaloriesPer100 * amountGrams / 100,
        product.ProteinPer100 * amountGrams / 100,
        product.FatPer100 * amountGrams / 100,
        product.CarbohydratesPer100 * amountGrams / 100);

    public static decimal TrainingBonus(ActivityLevel? intensity, TimeSpan duration, int workingSets)
    {
        if (duration <= TimeSpan.Zero && workingSets == 0) return 0;
        var kcalPerMinute = intensity switch
        {
            ActivityLevel.Light => 4m,
            ActivityLevel.Moderate => 5.5m,
            ActivityLevel.High => 7m,
            _ => 3m
        };
        var minutes = Math.Clamp((decimal)duration.TotalMinutes, 0, 180);
        return Math.Clamp(decimal.Round(minutes * kcalPerMinute + workingSets * 2m), 50, 900);
    }
}
