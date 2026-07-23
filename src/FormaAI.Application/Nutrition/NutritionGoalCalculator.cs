using FormaAI.Domain.Users;

namespace FormaAI.Application.Nutrition;

public static class NutritionGoalCalculator
{
    public static Macro Calculate(
        decimal weightKg,
        decimal heightCm,
        int age,
        BiologicalSex sex,
        ActivityLevel activity,
        BodyGoal goal,
        decimal weeklyWeightChangeKg)
    {
        var bmr = 10m * weightKg + 6.25m * heightCm - 5m * age + (sex == BiologicalSex.Male ? 5m : -161m);
        var factor = activity switch
        {
            ActivityLevel.Low => 1.2m,
            ActivityLevel.Light => 1.32m,
            ActivityLevel.Moderate => 1.45m,
            _ => 1.6m
        };
        var weeklyEnergy = weeklyWeightChangeKg * 7700m;
        var adjustment = goal switch
        {
            BodyGoal.Reduction => -weeklyEnergy / 7m,
            BodyGoal.MuscleGain => weeklyEnergy / 7m,
            _ => 0m
        };
        var protein = decimal.Round(weightKg * 2m);
        var fat = decimal.Round(weightKg * .9m);
        var macroFloor = decimal.Ceiling((protein * 4m + fat * 9m) / .8m);
        var calories = decimal.Round(Math.Max(Math.Max(1200m, bmr * factor + adjustment), macroFloor));
        var carbs = decimal.Round((calories - protein * 4m - fat * 9m) / 4m);

        return new Macro(calories, protein, fat, carbs);
    }
}
