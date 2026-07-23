using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FormaAI.Application.Assistant;
using FormaAI.Contracts.Assistant;
using FormaAI.Contracts.Nutrition;
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

    public async Task<MealPhotoDraftResponse> AnalyzeMealPhoto(byte[] image, string mimeType, CancellationToken cancellationToken)
    {
        var config = await RuntimeSettings(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.ApiKey)) throw new AssistantModelUnavailableException("Brak klucza API.");

        var prompt = """
            Przeanalizuj zdjęcie jedzenia. Rozpoznaj osobne składniki widoczne na talerzu i oszacuj ich masę oraz wartości odżywcze na 100 g.
            Nie udawaj pewności: w polu note krótko opisz najważniejsze założenia. Gdy zdjęcie nie przedstawia jedzenia, zwróć pustą listę items.
            Nazwy podaj po polsku. Nie dodawaj składników, których nie widać, poza oczywistym olejem lub sosem potrzebnym do przygotowania potrawy.
            """;

        var json = config.Provider == AiProvider.Gemini
            ? await SendGeminiMeal(config, prompt, image, mimeType, cancellationToken)
            : await SendOpenAiMeal(config, prompt, image, mimeType, cancellationToken);
        return ParseMeal(json);
    }

    public async Task<MealPhotoDraftResponse> AnalyzeMealText(string description, CancellationToken cancellationToken)
    {
        var config = await RuntimeSettings(cancellationToken);
        if (string.IsNullOrWhiteSpace(config.ApiKey)) throw new AssistantModelUnavailableException("Brak klucza API.");
        var prompt = $"""
            Rozpisz opis posiłku na osobne składniki, oszacuj ich masę oraz wartości odżywcze na 100 g.
            Nazwy podaj po polsku. Nie dodawaj składników, których opis nie sugeruje. W polu note krótko opisz najważniejsze założenia.
            Opis użytkownika: {description}
            """;
        var json = config.Provider == AiProvider.Gemini
            ? await SendGeminiMeal(config, prompt, null, null, cancellationToken)
            : await SendOpenAiMeal(config, prompt, null, null, cancellationToken);
        return ParseMeal(json);
    }

    private static MealPhotoDraftResponse ParseMeal(string json)
    {
        var parsed = JsonSerializer.Deserialize<PhotoJson>(json, JsonOptions)
            ?? throw new AssistantModelUnavailableException("Model zwrócił odpowiedź w nieobsługiwanym formacie.");
        var items = parsed.Items.Take(12).Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.AmountGrams > 0)
            .Select(x => new MealPhotoItemDraft(
                x.Name.Trim(),
                decimal.Clamp(x.AmountGrams, 1, 5000),
                decimal.Clamp(x.CaloriesPer100, 0, 5000),
                decimal.Clamp(x.ProteinPer100, 0, 1000),
                decimal.Clamp(x.FatPer100, 0, 1000),
                decimal.Clamp(x.CarbohydratesPer100, 0, 1000)))
            .ToList();
        return new MealPhotoDraftResponse(string.IsNullOrWhiteSpace(parsed.MealName) ? "Posiłek ze zdjęcia" : parsed.MealName.Trim(), parsed.Note?.Trim(), items);
    }

    private async Task<string> SendGeminiMeal(RuntimeConfig config, string prompt, byte[]? image, string? mimeType, CancellationToken cancellationToken)
    {
        object[] parts = image is null
            ? [new { text = prompt }]
            : [new { text = prompt }, new { inlineData = new { mimeType, data = Convert.ToBase64String(image) } }];
        var body = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                responseJsonSchema = PhotoSchema,
                maxOutputTokens = 2048
            }
        };
        var endpoint = new Uri(new Uri(config.ApiBaseUrl), $"v1beta/models/{Uri.EscapeDataString(config.Model)}:generateContent");
        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = JsonContent.Create(body) };
        message.Headers.Add("x-goog-api-key", config.ApiKey);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw ProviderError("Gemini", response.StatusCode, config.Model);
        var result = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        var text = result?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(text) ? throw new AssistantModelUnavailableException("Model nie rozpoznał zawartości zdjęcia.") : text;
    }

    private async Task<string> SendOpenAiMeal(RuntimeConfig config, string prompt, byte[]? image, string? mimeType, CancellationToken cancellationToken)
    {
        var format = " Zwróć JSON: mealName, note oraz items z polami name, amountGrams, caloriesPer100, proteinPer100, fatPer100, carbohydratesPer100.";
        object[] content = image is null
            ? [new { type = "text", text = prompt + format }]
            : [new { type = "text", text = prompt + format }, new { type = "image_url", image_url = new { url = $"data:{mimeType};base64,{Convert.ToBase64String(image)}" } }];
        var body = new
        {
            model = config.Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content
                }
            },
            response_format = new { type = "json_object" },
            temperature = 0.1,
            max_tokens = 1600
        };
        var baseUri = new Uri(config.ApiBaseUrl);
        var suffix = baseUri.AbsolutePath.TrimEnd('/').EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? "chat/completions" : "v1/chat/completions";
        using var message = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, suffix)) { Content = JsonContent.Create(body) };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) throw ProviderError("Wybrane API", response.StatusCode, config.Model);
        var result = await response.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
        var text = result?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(text) ? throw new AssistantModelUnavailableException("Model nie rozpoznał zawartości zdjęcia.") : text;
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
            generationConfig["thinkingConfig"] = new { thinkingLevel = AiDefaults.GeminiThinkingLevel(config.Model) };

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
    private static readonly object PhotoSchema = new
    {
        type = "object",
        properties = new
        {
            mealName = new { type = "string" },
            note = new { type = new[] { "string", "null" } },
            items = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string" },
                        amountGrams = new { type = "number" },
                        caloriesPer100 = new { type = "number" },
                        proteinPer100 = new { type = "number" },
                        fatPer100 = new { type = "number" },
                        carbohydratesPer100 = new { type = "number" }
                    },
                    required = new[] { "name", "amountGrams", "caloriesPer100", "proteinPer100", "fatPer100", "carbohydratesPer100" }
                }
            }
        },
        required = new[] { "mealName", "note", "items" }
    };
    private sealed record RuntimeConfig(AiProvider Provider, string ApiBaseUrl, string Model, string ApiKey);
    private sealed record ModelJson(string? Reply, ToolJson? ToolCall);
    private sealed record ToolJson(string Name, JsonElement Arguments);
    private sealed record PhotoJson(string MealName, string? Note, IReadOnlyList<PhotoItemJson> Items);
    private sealed record PhotoItemJson(string Name, decimal AmountGrams, decimal CaloriesPer100, decimal ProteinPer100, decimal FatPer100, decimal CarbohydratesPer100);
}

public sealed record GeminiSettings(string ApiKey, string Model);
