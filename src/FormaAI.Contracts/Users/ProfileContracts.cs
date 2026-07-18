using System.ComponentModel.DataAnnotations;
using FormaAI.Domain.Users;

namespace FormaAI.Contracts.Users;

public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string TimeZoneId,
    decimal? HeightCm,
    decimal? StartingWeightKg,
    BodyGoal? Goal,
    int? PlannedTrainingsPerWeek,
    DateOnly? BirthDate = null,
    BiologicalSex? Sex = null,
    ActivityLevel? ActivityLevel = null,
    decimal? TargetWeightKg = null,
    int CalorieToleranceKcal = 100,
    IReadOnlyList<string>? MealSlots = null);

public sealed record UpdateUserProfileRequest(
    [Required] string TimeZoneId,
    [Range(50, 280)] decimal? HeightCm,
    [Range(20, 500)] decimal? StartingWeightKg,
    BodyGoal? Goal,
    [Range(1, 14)] int? PlannedTrainingsPerWeek,
    DateOnly? BirthDate = null,
    BiologicalSex? Sex = null,
    ActivityLevel? ActivityLevel = null,
    [Range(20, 500)] decimal? TargetWeightKg = null,
    [Range(0, 1000)] int CalorieToleranceKcal = 100,
    [MinLength(1), MaxLength(10)] IReadOnlyList<string>? MealSlots = null);
