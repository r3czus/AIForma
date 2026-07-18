using System.ComponentModel.DataAnnotations;

namespace FormaAI.Contracts.Progress;

public sealed record SaveBodyMeasurementRequest(DateOnly LocalDate, [Range(20, 500)] decimal WeightKg, [Range(30, 300)] decimal? WaistCm, [MaxLength(500)] string? Notes,
    [Range(30, 300)] decimal? ChestCm = null, [Range(30, 300)] decimal? HipsCm = null, [Range(10, 100)] decimal? ArmCm = null, [Range(20, 150)] decimal? ThighCm = null);
public sealed record BodyMeasurementResponse(Guid Id, DateOnly LocalDate, decimal WeightKg, decimal? WaistCm, string? Notes,
    decimal? ChestCm = null, decimal? HipsCm = null, decimal? ArmCm = null, decimal? ThighCm = null);
public sealed record WeightProgressPoint(DateOnly Date, decimal WeightKg, decimal SevenDayAverageKg);
public sealed record WeightProgressResponse(IReadOnlyList<WeightProgressPoint> Points, decimal? ChangeKg, decimal? CurrentAverageKg);
public sealed record ExerciseProgressPoint(DateOnly Date, decimal VolumeKg, decimal? EstimatedOneRepMaxKg);
public sealed record ExerciseProgressResponse(Guid ExerciseId, string ExerciseName, IReadOnlyList<ExerciseProgressPoint> Points, decimal? VolumeChangePercent, decimal? EstimatedOneRepMaxChangeKg);
public sealed record ConsistencyPoint(DateOnly WeekStarting, int CompletedWorkouts);
public sealed record ConsistencyResponse(int CompletedWorkouts, int PlannedWorkouts, decimal CompletionPercent, IReadOnlyList<ConsistencyPoint> Weeks);
public sealed record NutritionAdherencePoint(DateOnly Date, decimal? TargetKcal, decimal ConsumedKcal, bool HasMeals, bool IsWithinTarget);
public sealed record NutritionAdherenceResponse(DateOnly Month, int ToleranceKcal, int DaysOnTarget, int LoggedDays, IReadOnlyList<NutritionAdherencePoint> Days);
