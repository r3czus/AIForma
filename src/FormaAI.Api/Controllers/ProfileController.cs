using System.Security.Claims;
using System.Globalization;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Users;
using FormaAI.Infrastructure.Identity;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(
    AppDbContext db,
    UserManager<ApplicationUser> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await db.UserProfiles.SingleAsync(x => x.UserId == userId);
        var email = (await users.FindByIdAsync(userId))!.Email!;

        return new UserProfileResponse(
            profile.Id,
            email,
            profile.TimeZoneId,
            profile.HeightCm,
            profile.StartingWeightKg,
            profile.Goal,
            profile.PlannedTrainingsPerWeek,
            profile.BirthDate,
            profile.Sex,
            profile.ActivityLevel,
            profile.TargetWeightKg,
            profile.CalorieToleranceKcal,
            Slots(profile),
            profile.WorkActivityLevel ?? profile.ActivityLevel,
            profile.TrainingActivityLevel,
            Schedule(profile),
            profile.DefaultRestSeconds,
            profile.DefaultRepetitions,
            profile.DefaultSets,
            profile.WeekStartsOn,
            profile.ThemePreference,
            profile.MealRemindersEnabled,
            profile.MealReminderMinutesBefore);
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<UserProfileResponse>> Update(UpdateUserProfileRequest request)
    {
        if (!IsKnownTimeZone(request.TimeZoneId))
        {
            ModelState.AddModelError(nameof(request.TimeZoneId), "Nieznana strefa czasowa.");
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var profile = await db.UserProfiles.SingleAsync(x => x.UserId == userId);
        profile.Update(
            request.TimeZoneId,
            request.HeightCm,
            request.StartingWeightKg,
            request.Goal,
            request.PlannedTrainingsPerWeek,
            request.BirthDate,
            request.Sex,
            request.ActivityLevel,
            request.TargetWeightKg,
            request.CalorieToleranceKcal,
            request.MealSlots,
            request.WorkActivityLevel,
            request.TrainingActivityLevel,
            request.MealSchedule,
            request.DefaultRestSeconds,
            request.DefaultRepetitions,
            request.DefaultSets,
            request.WeekStartsOn,
            request.ThemePreference,
            request.MealRemindersEnabled,
            request.MealReminderMinutesBefore);
        await db.SaveChangesAsync();

        var email = (await users.FindByIdAsync(userId))!.Email!;
        return new UserProfileResponse(
            profile.Id,
            email,
            profile.TimeZoneId,
            profile.HeightCm,
            profile.StartingWeightKg,
            profile.Goal,
            profile.PlannedTrainingsPerWeek,
            profile.BirthDate,
            profile.Sex,
            profile.ActivityLevel,
            profile.TargetWeightKg,
            profile.CalorieToleranceKcal,
            Slots(profile),
            profile.WorkActivityLevel ?? profile.ActivityLevel,
            profile.TrainingActivityLevel,
            Schedule(profile),
            profile.DefaultRestSeconds,
            profile.DefaultRepetitions,
            profile.DefaultSets,
            profile.WeekStartsOn,
            profile.ThemePreference,
            profile.MealRemindersEnabled,
            profile.MealReminderMinutesBefore);
    }

    private static string[] Slots(FormaAI.Domain.Users.UserProfile profile) =>
        profile.MealSlots.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static MealScheduleEntry[] Schedule(FormaAI.Domain.Users.UserProfile profile) =>
        profile.MealSchedule
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Split('~'))
            .Where(parts => parts.Length == 3 && TimeOnly.TryParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .Select(parts => new MealScheduleEntry(parts[0], TimeOnly.ParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture), parts[2] == "1"))
            .ToArray();

    private static bool IsKnownTimeZone(string id)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
