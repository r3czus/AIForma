using System.Net.Http.Json;
using FormaAI.Contracts.Assistant;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;
using FormaAI.Contracts.Training;

namespace FormaAI.Web.Services;

public sealed class AssistantClient(HttpClient http)
{
    public Task<AssistantMessageResponse> Send(SendAssistantMessageRequest request) =>
        Send<AssistantMessageResponse>(HttpMethod.Post, "api/v1/assistant/messages", request);

    public Task<MealResponse> Confirm(Guid draftId) =>
        Send<MealResponse>(HttpMethod.Post, $"api/v1/assistant/actions/{draftId}/confirm", new { });

    public Task<TrainingPlanResponse> ConfirmTrainingPlan(Guid draftId) =>
        Send<TrainingPlanResponse>(HttpMethod.Post, $"api/v1/assistant/actions/{draftId}/confirm", new { });

    public async Task Reject(Guid draftId)
    {
        using var response = await Send(HttpMethod.Post, $"api/v1/assistant/actions/{draftId}/reject", new { });
        response.EnsureSuccessStatusCode();
    }

    private async Task<T> Send<T>(HttpMethod method, string uri, object body)
    {
        using var response = await Send(method, uri, body);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(message.Trim('"'));
        }
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string uri, object body)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        return await http.SendAsync(request);
    }
}
