using System.ComponentModel.DataAnnotations;
using FormaAI.Domain.Nutrition;
using FormaAI.Domain.Progress;

namespace FormaAI.Contracts.Progress;

public sealed record DailyCoachResponse(string Type, string Title, string Description, string ActionLabel, string ActionUrl, string ReasonCode);
public sealed record MomentumResponse(int CompletedThisWeek, int WeeklyGoal, int SuccessfulWeeks, int WeeksWithActivity, int CompleteNutritionDays, int CompletedCheckIns);
public sealed record WeeklyDayQualityResponse(DateOnly Date, NutritionDayStatus Status, int Meals, decimal Calories);
public sealed record WeeklyReviewDataResponse(DateOnly From, DateOnly To, IReadOnlyList<WeeklyDayQualityResponse> Days, int CompletedWorkouts, int PlannedWorkouts, int Measurements, bool HasCheckIn);
public sealed record SaveWeeklyReviewRequest(DateOnly WeekStarting, [Range(1, 5)] int Energy, [Range(1, 5)] int Sleep, [Range(1, 5)] int Hunger, [Range(1, 5)] int Recovery, [Range(1, 5)] int Stress, [MaxLength(1000)] string? Notes, [MaxLength(500)] string? DeviationReasons);
public sealed record WeeklyReviewResponse(Guid Id, DateOnly WeekStarting, int Energy, int Sleep, int Hunger, int Recovery, int Stress, string? Notes, string? DeviationReasons, string? Summary);
public sealed record AchievementResponse(string Key, string Title, string Detail, DateOnly? EarnedOn, string Category, string Requirement);
public sealed record ProgressPhotoResponse(Guid Id, DateOnly LocalDate, ProgressPhotoPose Pose, string Url);
public sealed record PushSubscriptionRequest([Required, MaxLength(2000)] string Endpoint, [Required, MaxLength(500)] string P256Dh, [Required, MaxLength(500)] string Auth);
public sealed record VapidPublicKeyResponse(string PublicKey);
