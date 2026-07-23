using FormaAI.Application.Assistant;
using FormaAI.Contracts.Assistant;
using FormaAI.Domain.Assistant;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/v1/admin/ai")]
public sealed class AdminAiController(
    AppDbContext db,
    IDataProtectionProvider dataProtection,
    IAssistantModel assistantModel) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AiConfigurationResponse>> Get()
    {
        var config = await db.AiConfigurations.SingleOrDefaultAsync();
        return config is null
            ? new AiConfigurationResponse(AiProvider.Gemini, AiDefaults.GeminiBaseUrl, AiDefaults.GeminiModel, false, DateTime.MinValue)
            : ToResponse(config);
    }

    [HttpPut]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AiConfigurationResponse>> Save(SaveAiConfigurationRequest request)
    {
        if (!Uri.TryCreate(request.ApiBaseUrl, UriKind.Absolute, out var uri) || !AllowedEndpoint(uri))
            return ValidationProblem("Zdalny adres API musi używać HTTPS. HTTP jest dozwolone tylko dla localhost.");

        var config = await db.AiConfigurations.SingleOrDefaultAsync();
        var protectedKey = string.IsNullOrWhiteSpace(request.ApiKey) ? null : Protector().Protect(request.ApiKey.Trim());
        if (config is null)
        {
            if (protectedKey is null) return ValidationProblem("Przy pierwszym zapisie podaj klucz API.");
            config = new AiConfiguration(request.Provider, request.ApiBaseUrl, request.Model, protectedKey);
            db.AiConfigurations.Add(config);
        }
        else
        {
            config.Update(request.Provider, request.ApiBaseUrl, request.Model, protectedKey);
        }

        await db.SaveChangesAsync();
        return ToResponse(config);
    }

    [HttpPost("test")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<AiConnectionTestResponse>> Test(CancellationToken cancellationToken)
    {
        try
        {
            var result = await assistantModel.Generate(new AssistantModelRequest(
                "Odpowiedz po polsku. Zwróć wyłącznie krótkie potwierdzenie działania połączenia, bez wywoływania narzędzi.",
                [new AssistantModelMessage("user", "Sprawdź połączenie.")],
                []), cancellationToken);
            return new AiConnectionTestResponse(true, result.Reply ?? "Połączenie z modelem działa.");
        }
        catch (AssistantModelUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new AiConnectionTestResponse(false, ex.Message));
        }
    }

    private static bool AllowedEndpoint(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps ||
        (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback);

    private IDataProtector Protector() => dataProtection.CreateProtector("FormaAI.AiApiKey.v1");
    private static AiConfigurationResponse ToResponse(AiConfiguration config) =>
        new(config.Provider, config.ApiBaseUrl, config.Model, config.EncryptedApiKey.Length > 0, config.UpdatedAtUtc);
}
