namespace FormaAI.Application.Progress;

public static class WeightGoalCopy
{
    public static string For(decimal currentKg, decimal targetKg)
    {
        var difference = decimal.Round(targetKg - currentKg, 1);
        if (Math.Abs(difference) < .05m) return "Cel zakłada utrzymanie obecnej masy";

        return difference < 0
            ? $"Do zrzucenia {Math.Abs(difference):0.#} kg"
            : $"Do przybrania {difference:0.#} kg";
    }
}
