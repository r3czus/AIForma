namespace FormaAI.Application.Progress;

public static class ProgressMetrics
{
    public static decimal Volume(decimal weightKg, int repetitions) => weightKg * repetitions;

    public static decimal? EstimatedOneRepMax(decimal weightKg, int repetitions) =>
        repetitions is > 0 and <= 12 ? decimal.Round(weightKg * (1 + repetitions / 30m), 2) : null;

    public static decimal PercentageChange(decimal first, decimal last) =>
        first == 0 ? 0 : decimal.Round((last - first) / first * 100, 1);
}
