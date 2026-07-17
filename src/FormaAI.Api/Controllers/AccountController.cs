using System.Security.Claims;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Users;
using FormaAI.Infrastructure.Identity;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Route("api/account")]
public sealed class AccountController(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn,
    AppDbContext db,
    IAntiforgery antiforgery,
    IConfiguration configuration) : ControllerBase
{
    [HttpGet("antiforgery")]
    [AllowAnonymous]
    public ActionResult<AntiforgeryResponse> GetAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return new AntiforgeryResponse(tokens.RequestToken!);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CurrentUserResponse>> Register(RegisterRequest request)
    {
        if (!IsKnownTimeZone(request.TimeZoneId))
        {
            ModelState.AddModelError(nameof(request.TimeZoneId), "Nieznana strefa czasowa.");
            return ValidationProblem(ModelState);
        }

        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem(ModelState);
        }

        db.UserProfiles.Add(new UserProfile(user.Id, request.TimeZoneId));
        await db.SaveChangesAsync();
        await signIn.SignInAsync(user, isPersistent: true);

        return CurrentUser(user);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<CurrentUserResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized();
        }

        var result = await signIn.PasswordSignInAsync(user, request.Password, true, lockoutOnFailure: true);
        return result.Succeeded
            ? CurrentUser(user)
            : Unauthorized();
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signIn.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserResponse>> Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await users.FindByIdAsync(id);
        return user is null
            ? Unauthorized()
            : CurrentUser(user);
    }

    [HttpDelete]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await users.FindByIdAsync(userId);
        if (user is null) return Unauthorized();
        if (request.Confirmation != "USUŃ" || !await users.CheckPasswordAsync(user, request.Password)) return BadRequest("Nieprawidłowe potwierdzenie lub hasło.");

        await using var transaction = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        db.AssistantActionDrafts.RemoveRange(db.AssistantActionDrafts.Where(x => x.UserId == userId));
        db.AiToolExecutions.RemoveRange(db.AiToolExecutions.Where(x => x.UserId == userId));
        db.Conversations.RemoveRange(db.Conversations.Where(x => x.UserId == userId));
        db.Meals.RemoveRange(db.Meals.Where(x => x.UserId == userId));
        db.NutritionTargets.RemoveRange(db.NutritionTargets.Where(x => x.UserId == userId));
        db.WorkoutSessions.RemoveRange(db.WorkoutSessions.Where(x => x.UserId == userId));
        db.TrainingPlans.RemoveRange(db.TrainingPlans.Where(x => x.UserId == userId));
        db.BodyMeasurements.RemoveRange(db.BodyMeasurements.Where(x => x.UserId == userId));
        db.Recipes.RemoveRange(db.Recipes.Where(x => x.UserId == userId));
        db.ShoppingLists.RemoveRange(db.ShoppingLists.Where(x => x.UserId == userId));
        db.PantryItems.RemoveRange(db.PantryItems.Where(x => x.UserId == userId));
        db.UserProfiles.RemoveRange(db.UserProfiles.Where(x => x.UserId == userId));
        await db.SaveChangesAsync();

        db.Products.RemoveRange(db.Products.Where(x => x.OwnerUserId == userId));
        db.Exercises.RemoveRange(db.Exercises.Where(x => x.OwnerUserId == userId));
        await db.SaveChangesAsync();
        var result = await users.DeleteAsync(user);
        if (!result.Succeeded) return Problem("Nie udało się usunąć konta.");
        if (transaction is not null) await transaction.CommitAsync();
        await signIn.SignOutAsync();
        return NoContent();
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

    private CurrentUserResponse CurrentUser(ApplicationUser user) =>
        new(user.Id, user.Email!, string.Equals(user.Email, configuration["Admin:Email"], StringComparison.OrdinalIgnoreCase));
}
