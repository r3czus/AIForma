using System.Net.Http.Headers;
using System.Net.Http.Json;
using FormaAI.Contracts.Progress;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Progress;
using Microsoft.AspNetCore.Components.Forms;

namespace FormaAI.Web.Services;

public sealed class CoachingClient(HttpClient http)
{
    public async Task<DailyCoachResponse> GetDaily() =>
        await http.GetFromJsonAsync<DailyCoachResponse>("api/v1/coaching/daily") ?? throw new InvalidOperationException("Nie udało się pobrać wskazówki dnia.");

    public async Task<MomentumResponse> GetMomentum() =>
        await http.GetFromJsonAsync<MomentumResponse>("api/v1/coaching/momentum") ?? throw new InvalidOperationException("Nie udało się pobrać regularności.");

    public async Task<WeeklyReviewDataResponse> GetWeekly(DateOnly weekStarting) =>
        await http.GetFromJsonAsync<WeeklyReviewDataResponse>($"api/v1/coaching/weekly/{weekStarting:yyyy-MM-dd}") ?? throw new InvalidOperationException("Nie udało się pobrać tygodnia.");

    public async Task<WeeklyReviewResponse> SaveWeekly(SaveWeeklyReviewRequest request)
    {
        using var response = await Send(HttpMethod.Post, "api/v1/coaching/weekly", request);
        return await response.Content.ReadFromJsonAsync<WeeklyReviewResponse>() ?? throw new InvalidOperationException("Nie udało się zapisać podsumowania.");
    }

    public async Task<IReadOnlyList<WeeklyReviewResponse>> GetReviews() =>
        await http.GetFromJsonAsync<IReadOnlyList<WeeklyReviewResponse>>("api/v1/coaching/weekly/reviews") ?? [];

    public async Task<IReadOnlyList<AchievementResponse>> GetAchievements() =>
        await http.GetFromJsonAsync<IReadOnlyList<AchievementResponse>>("api/v1/coaching/achievements") ?? [];

    public async Task<IReadOnlyList<ProgressPhotoResponse>> GetPhotos() =>
        await http.GetFromJsonAsync<IReadOnlyList<ProgressPhotoResponse>>("api/v1/coaching/photos") ?? [];

    public async Task<ProgressPhotoResponse> AddPhoto(IBrowserFile file, DateOnly date, ProgressPhotoPose pose)
    {
        using var content = new MultipartFormDataContent();
        var stream = new StreamContent(file.OpenReadStream(15 * 1024 * 1024));
        stream.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(stream, "photo", file.Name);
        content.Add(new StringContent(date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)), "localDate");
        content.Add(new StringContent(pose.ToString()), "pose");
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/coaching/photos") { Content = content };
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProgressPhotoResponse>() ?? throw new InvalidOperationException("Nie udało się zapisać zdjęcia.");
    }

    public async Task DeletePhoto(Guid id)
    {
        using var response = await Send(HttpMethod.Delete, $"api/v1/coaching/photos/{id}", null);
    }

    public async Task<string> GetPushPublicKey() =>
        (await http.GetFromJsonAsync<VapidPublicKeyResponse>("api/v1/coaching/push-public-key"))!.PublicKey;

    public async Task SavePushSubscription(PushSubscriptionRequest request)
    {
        using var response = await Send(HttpMethod.Post, "api/v1/coaching/push-subscriptions", request);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string uri, object? body)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
