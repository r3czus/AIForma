using System.Security.Claims;
using FormaAI.Contracts.Users;
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
            profile.PlannedTrainingsPerWeek);
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
            request.PlannedTrainingsPerWeek);
        await db.SaveChangesAsync();

        var email = (await users.FindByIdAsync(userId))!.Email!;
        return new UserProfileResponse(
            profile.Id,
            email,
            profile.TimeZoneId,
            profile.HeightCm,
            profile.StartingWeightKg,
            profile.Goal,
            profile.PlannedTrainingsPerWeek);
    }

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
