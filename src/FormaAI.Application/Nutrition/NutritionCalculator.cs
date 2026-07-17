using FormaAI.Domain.Nutrition;

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
}
