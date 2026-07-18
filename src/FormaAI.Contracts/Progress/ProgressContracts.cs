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
public sealed record NutritionAdherencePoint(DateOnly Date, decimal? TargetKcal, decimal ConsumedKcal, bool HasMeals, bool IsWithinTarget, bool HasCompletedWorkout,
    decimal ProteinG = 0, decimal FatG = 0, decimal CarbohydratesG = 0, decimal? TargetProteinG = null, decimal? TargetFatG = null, decimal? TargetCarbohydratesG = null,
    string? WorkoutName = null, int WorkoutMinutes = 0, int WorkoutExercises = 0, int WorkoutSets = 0, decimal WorkoutVolumeKg = 0);
public sealed record NutritionAdherenceResponse(DateOnly Month, int ToleranceKcal, int DaysOnTarget, int LoggedDays, IReadOnlyList<NutritionAdherencePoint> Days);
public sealed record TrainingWeekSummaryResponse(int CompletedWorkouts, int PlannedWorkouts, int TotalMinutes, int WorkingSets,
    decimal SetCompletionPercent, decimal TotalVolumeKg, decimal? VolumeChangePercent);
public sealed record NutritionWeekSummaryResponse(int LoggedDays, int DaysOnCalories, decimal CalorieAdherencePercent,
    decimal AverageCalories, decimal AverageTargetCalories, int DaysOnProtein, decimal AverageProteinG, decimal AverageCalorieDifference);
public sealed record ProgressWeekSummaryResponse(DateOnly From, DateOnly To, TrainingWeekSummaryResponse Training, NutritionWeekSummaryResponse Nutrition);
public sealed record MuscleVolumePoint(string MuscleGroup, int WorkingSets, decimal VolumeKg);
public sealed record SaveWeeklyCheckInRequest(DateOnly LocalDate, [Range(1, 5)] int Energy, [Range(1, 5)] int Sleep, [Range(1, 5)] int Hunger, [Range(1, 5)] int Recovery, [MaxLength(500)] string? Notes);
public sealed record WeeklyCheckInResponse(Guid Id, DateOnly LocalDate, int Energy, int Sleep, int Hunger, int Recovery, string? Notes);
