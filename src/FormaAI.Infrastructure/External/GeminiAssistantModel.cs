using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FormaAI.Application.Assistant;
using FormaAI.Domain.Assistant;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace FormaAI.Infrastructure.External;

public sealed class GeminiAssistantModel(
    HttpClient http,
    GeminiSettings fallback,
    AppDbContext db,
    IDataProtectionProvider dataProtection) : IAssistantModel
{
    public async Task<AssistantModelTurn> Generate(AssistantModelRequest request, CancellationToken cancellationToken)
    {
        var config = await RuntimeSettings(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.ApiKey)) throw new AssistantModelUnavailableException("Brak klucza API.");

        var transcript = string.Join("\n", request.Messages.Select(x => $"{x.Role}: {x.Text}"));
        var toolResults = request.ToolResults.Count == 0 ? "brak" : string.Join("\n", request.ToolResults.Select(x => $"{x.Name}({x.Arguments.GetRawText()}): {x.Result}"));
        var prompt = $"""
            Rozmowa:
            {transcript}

            Wyniki wykonanych narzędzi:
            {toolResults}

            Zwróć odpowiedź albo jedno kolejne wywołanie narzędzia. Nie powtarzaj identycznego wywołania.
            """;

        return config.Provider == AiProvider.Gemini
            ? await SendGemini(config, request.SystemInstruction, prompt, cancellationToken)
            : await SendOpenAi(config, request.SystemInstruction, prompt, cancellationToken);
    }

    private async Task<AssistantModelTurn> SendGemini(RuntimeConfig config, string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        var generationConfig = new Dictionary<string, object>
        {
            ["responseMimeType"] = "application/json",
            ["responseJsonSchema"] = new
            {
                type = "object",
                properties = new
                {
                    reply = new { type = new[] { "string", "null" } },
                    toolCall = new
                    {
                        type = new[] { "object", "null" },
                        properties = new { name = new { type = "string" }, arguments = new { type = "object" } },
                        required = new[] { "name", "arguments" }
                    }
                },
                required = new[] { "reply", "toolCall" }
            },
            ["maxOutputTokens"] = 4096
        };
        if (config.Model.StartsWith("gemini-3", StringComparison.OrdinalIgnoreCase))
            generationConfig["thinkingConfig"] = new { thinkingLevel = "low" };

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemInstruction } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = prompt } } } },
            generationConfig
        };
        var endpoint = new Uri(new Uri(config.ApiBaseUrl), $"v1beta/models/{Uri.EscapeDataString(config.Model)}:generateContent");
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(body) };
        message.Headers.Add("x-goog-api-key", config.ApiKey);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw ProviderError("Gemini", response.StatusCode, config.Model);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        var candidate = json?["candidates"]?[0];
        if (string.Equals(candidate?["finishReason"]?.GetValue<string>(), "MAX_TOKENS", StringComparison.OrdinalIgnoreCase))
            throw new AssistantModelUnavailableException("Model przerwał odpowiedź z powodu limitu długości. Spróbuj zadać węższe pytanie.");
        var texts = candidate?["content"]?["parts"]?.AsArray()
            .OfType<JsonObject>()
            .Where(x => x["thought"]?.GetValue<bool>() != true)
            .Select(x => x["text"]?.GetValue<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList() ?? [];
        var usage = json?["usageMetadata"];
        return Parse(texts, usage?["promptTokenCount"]?.GetValue<int>() ?? 0, usage?["candidatesTokenCount"]?.GetValue<int>() ?? 0);
    }

    private async Task<AssistantModelTurn> SendOpenAi(RuntimeConfig config, string systemInstruction, string prompt, CancellationToken cancellationToken)
    {
        var body = new
        {
            model = config.Model,
            messages = new[] { new { role = "system", content = systemInstruction }, new { role = "user", content = prompt } },
            response_format = new { type = "json_object" },
            temperature = 0.2,
            max_tokens = 1200
        };
        var baseUri = new Uri(config.ApiBaseUrl);
        var suffix = baseUri.AbsolutePath.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? "chat/completions"
            : "v1/chat/completions";
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, suffix)) { Content = JsonContent.Create(body) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw ProviderError("Wybrane API", response.StatusCode, config.Model);
        var json = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        var text = json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
        var usage = json?["usage"];
        return Parse(string.IsNullOrWhiteSpace(text) ? [] : [text], usage?["prompt_tokens"]?.GetValue<int>() ?? 0, usage?["completion_tokens"]?.GetValue<int>() ?? 0);
    }

    private async Task<RuntimeConfig> RuntimeSettings(CancellationToken cancellationToken)
    {
        var saved = await db.AiConfigurations.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (saved is null)
            return new RuntimeConfig(AiProvider.Gemini, http.BaseAddress?.ToString() ?? "https://generativelanguage.googleapis.com/", fallback.Model, fallback.ApiKey);

        var protector = dataProtection.CreateProtector("FormaAI.AiApiKey.v1");
        string apiKey;
        try { apiKey = protector.Unprotect(saved.EncryptedApiKey); }
        catch (System.Security.Cryptography.CryptographicException) { throw new AssistantModelUnavailableException("Nie można odczytać zapisanego klucza API. Zapisz go ponownie w panelu administratora."); }
        return new RuntimeConfig(saved.Provider, saved.ApiBaseUrl, saved.Model, apiKey);
    }

    private static AssistantModelTurn Parse(IReadOnlyList<string> texts, int inputTokens, int outputTokens)
    {
        if (texts.Count == 0) throw new AssistantModelUnavailableException("Model zwrócił pustą odpowiedź.");
        ModelJson? parsed = null;
        foreach (var text in texts.AsEnumerable().Reverse())
        {
            try { parsed = JsonSerializer.Deserialize<ModelJson>(text, JsonOptions); }
            catch (JsonException) { }
            if (parsed is not null) break;
        }
        if (parsed is null && texts.Count > 1)
        {
            try { parsed = JsonSerializer.Deserialize<ModelJson>(string.Concat(texts), JsonOptions); }
            catch (JsonException) { }
        }
        if (parsed is null) throw new AssistantModelUnavailableException("Model zwrócił odpowiedź w nieobsługiwanym formacie.");
        return new AssistantModelTurn(parsed.Reply, parsed.ToolCall is null ? null : new AssistantToolCall(parsed.ToolCall.Name, parsed.ToolCall.Arguments), inputTokens, outputTokens);
    }

    private static AssistantModelUnavailableException ProviderError(string provider, HttpStatusCode status, string model) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new($"{provider} odrzuciło klucz API. Zapisz poprawny klucz w panelu administratora."),
        HttpStatusCode.NotFound => new($"Model {model} nie jest dostępny dla zapisanej konfiguracji."),
        HttpStatusCode.TooManyRequests => new($"{provider} wykorzystało dostępny limit zapytań. Spróbuj ponownie później."),
        HttpStatusCode.BadRequest => new($"{provider} odrzuciło ustawienia żądania. Sprawdź model i adres API w panelu administratora."),
        _ => new($"{provider} nie odpowiedziało poprawnie (HTTP {(int)status}).")
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private sealed record RuntimeConfig(AiProvider Provider, string ApiBaseUrl, string Model, string ApiKey);
    private sealed record ModelJson(string? Reply, ToolJson? ToolCall);
    private sealed record ToolJson(string Name, JsonElement Arguments);
}

public sealed record GeminiSettings(string ApiKey, string Model);
