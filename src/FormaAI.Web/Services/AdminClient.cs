using System.Net.Http.Json;
using System.Text.Json;
using FormaAI.Contracts.Assistant;
using FormaAI.Contracts.Users;

namespace FormaAI.Web.Services;

public sealed class AdminClient(HttpClient http)
{
    public async Task<AiConfigurationResponse> GetAiConfiguration()
    {
        using var response = await http.GetAsync("api/v1/admin/ai");
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<AiConfigurationResponse>())!;
    }

    public async Task<AiConfigurationResponse> SaveAiConfiguration(SaveAiConfigurationRequest request)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var message = new HttpRequestMessage(HttpMethod.Put, "api/v1/admin/ai") { Content = JsonContent.Create(request) };
        message.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        using var response = await http.SendAsync(message);
        await EnsureSuccess(response);
        return (await response.Content.ReadFromJsonAsync<AiConfigurationResponse>())!;
    }

    public async Task<AiConnectionTestResponse> TestAiConnection()
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var message = new HttpRequestMessage(HttpMethod.Post, "api/v1/admin/ai/test");
        message.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        using var response = await http.SendAsync(message);
        var result = await response.Content.ReadFromJsonAsync<AiConnectionTestResponse>();
        return result ?? new AiConnectionTestResponse(false, "Nie udało się odczytać odpowiedzi usługi.");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        var message = "Serwer odrzucił zapis konfiguracji.";
        if (!string.IsNullOrWhiteSpace(body))
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("detail", out var detail)) message = detail.GetString() ?? message;
            else if (json.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message;
        }

        throw new HttpRequestException(message, null, response.StatusCode);
    }
}
