namespace FormaAI.Application.Progress;

public static class WeightGoalDate
{
    public static DateOnly? Estimate(decimal currentKg, decimal targetKg, decimal weeklyChangeKg, DateOnly effectiveFrom)
    {
        if (weeklyChangeKg <= 0)
        {
            return null;
        }

        var weeks = Math.Abs(targetKg - currentKg) / weeklyChangeKg;
        return effectiveFrom.AddDays((int)Math.Ceiling(weeks * 7m));
    }
}
