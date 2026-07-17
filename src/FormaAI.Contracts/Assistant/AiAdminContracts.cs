using System.ComponentModel.DataAnnotations;
using FormaAI.Domain.Assistant;

namespace FormaAI.Contracts.Assistant;

public sealed record AiConfigurationResponse(
    AiProvider Provider,
    string ApiBaseUrl,
    string Model,
    bool HasApiKey,
    DateTime UpdatedAtUtc);

public sealed record SaveAiConfigurationRequest(
    AiProvider Provider,
    [Required, Url, MaxLength(500)] string ApiBaseUrl,
    [Required, MaxLength(120)] string Model,
    [MaxLength(1000)] string? ApiKey);

public sealed record AiConnectionTestResponse(bool Succeeded, string Message);
