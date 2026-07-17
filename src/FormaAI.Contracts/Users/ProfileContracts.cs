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
    int? PlannedTrainingsPerWeek);

public sealed record UpdateUserProfileRequest(
    [Required] string TimeZoneId,
    [Range(50, 280)] decimal? HeightCm,
    [Range(20, 500)] decimal? StartingWeightKg,
    BodyGoal? Goal,
    [Range(1, 14)] int? PlannedTrainingsPerWeek);
