using System.ComponentModel.DataAnnotations;
using FormaAI.Domain.Training;

namespace FormaAI.Contracts.Training;

public sealed record ExerciseResponse(Guid Id, string Name, MuscleGroup MuscleGroup, Equipment Equipment, bool IsUnilateral, bool IsOwn, string? Description = null);
public sealed record SaveExerciseRequest([Required, MaxLength(150)] string Name, MuscleGroup MuscleGroup, Equipment Equipment, bool IsUnilateral, [MaxLength(1000)] string? Description = null);
public sealed record PlannedExerciseRequest(Guid ExerciseId, [Range(1, 10)] int Sets, [Range(1, 100)] int MinReps, [Range(1, 100)] int MaxReps, [Range(0, 10)] decimal? TargetRir, [Range(0, 3600)] int? RestSeconds);
public sealed record TrainingDayRequest([Required, MaxLength(100)] string Name, DayOfWeek? DayOfWeek, [MinLength(1)] IReadOnlyList<PlannedExerciseRequest> Exercises);
public sealed record SaveTrainingPlanRequest([Required, MaxLength(150)] string Name, [MaxLength(500)] string Goal, DateOnly StartsOn, [MinLength(1)] IReadOnlyList<TrainingDayRequest> Days);
public sealed record PlannedExerciseResponse(Guid Id, Guid ExerciseId, string ExerciseName, int Sets, int MinReps, int MaxReps, decimal? TargetRir, int? RestSeconds);
public sealed record TrainingDayResponse(Guid Id, string Name, DayOfWeek? DayOfWeek, int SequenceNumber, IReadOnlyList<PlannedExerciseResponse> Exercises);
public sealed record TrainingPlanResponse(Guid Id, string Name, string Goal, bool IsActive, DateOnly StartsOn, IReadOnlyList<TrainingDayResponse> Days);
public sealed record TodayWorkoutResponse(Guid PlanId, Guid TrainingDayId, string PlanName, string DayName, bool AlreadyCompleted, IReadOnlyList<PlannedExerciseResponse> Exercises);
public sealed record StartWorkoutRequest(Guid TrainingDayId);
public sealed record SaveSetRequest(Guid WorkoutExerciseId, [Range(1, 50)] int SetNumber, [Range(0, 1000)] decimal WeightKg, [Range(1, 1000)] int Repetitions, [Range(0, 10)] decimal? Rir, SetType SetType);
public sealed record CompletedSetResponse(Guid Id, int SetNumber, decimal WeightKg, int Repetitions, decimal? Rir, SetType SetType, DateTime CompletedAtUtc);
public sealed record WorkoutExerciseResponse(Guid Id, Guid? ExerciseId, string ExerciseName, int Order, int PlannedSets, int MinReps, int MaxReps, decimal? TargetRir, int? RestSeconds, IReadOnlyList<CompletedSetResponse> Sets);
public sealed record WorkoutSessionResponse(Guid Id, string Name, DateTime StartedAtUtc, DateTime? FinishedAtUtc, SessionStatus Status, IReadOnlyList<WorkoutExerciseResponse> Exercises);
public sealed record ExerciseHistoryEntry(DateTime CompletedAtUtc, decimal WeightKg, int Repetitions, decimal? Rir, decimal Volume);
