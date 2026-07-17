using System.ComponentModel.DataAnnotations;

namespace FormaAI.Contracts.Users;

public sealed record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string TimeZoneId);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record CurrentUserResponse(string Id, string Email, bool IsAdmin = false);

public sealed record AntiforgeryResponse(string Token);

public sealed record DeleteAccountRequest(
    [Required] string Password,
    [Required] string Confirmation);
