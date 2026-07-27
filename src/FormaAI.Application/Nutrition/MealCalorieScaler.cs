namespace FormaAI.Application.Nutrition;

public static class MealCalorieScaler
{
    public static IReadOnlyList<decimal> ScaleAmounts(
        IReadOnlyList<decimal> amounts,
        decimal currentCalories,
        decimal targetCalories)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        if (amounts.Count == 0)
            throw new ArgumentException("Posiłek nie ma składników.", nameof(amounts));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(currentCalories, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(targetCalories, 0);

        var factor = targetCalories / currentCalories;
        return amounts
            .Select(amount => Math.Max(0.1m, decimal.Round(amount * factor, 1, MidpointRounding.AwayFromZero)))
            .ToList();
    }
}
