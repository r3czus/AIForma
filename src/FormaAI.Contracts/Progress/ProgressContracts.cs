using System.ComponentModel.DataAnnotations;

namespace FormaAI.Contracts.Progress;

public sealed record SaveBodyMeasurementRequest(DateOnly LocalDate, [Range(20, 500)] decimal WeightKg, [Range(30, 300)] decimal? WaistCm, [MaxLength(500)] string? Notes);
public sealed record BodyMeasurementResponse(Guid Id, DateOnly LocalDate, decimal WeightKg, decimal? WaistCm, string? Notes);
public sealed record WeightProgressPoint(DateOnly Date, decimal WeightKg, decimal SevenDayAverageKg);
public sealed record WeightProgressResponse(IReadOnlyList<WeightProgressPoint> Points, decimal? ChangeKg, decimal? CurrentAverageKg);
public sealed record ExerciseProgressPoint(DateOnly Date, decimal VolumeKg, decimal? EstimatedOneRepMaxKg);
public sealed record ExerciseProgressResponse(Guid ExerciseId, string ExerciseName, IReadOnlyList<ExerciseProgressPoint> Points, decimal? VolumeChangePercent, decimal? EstimatedOneRepMaxChangeKg);
public sealed record ConsistencyPoint(DateOnly WeekStarting, int CompletedWorkouts);
public sealed record ConsistencyResponse(int CompletedWorkouts, int PlannedWorkouts, decimal CompletionPercent, IReadOnlyList<ConsistencyPoint> Weeks);
