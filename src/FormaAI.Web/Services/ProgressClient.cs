using System.Net.Http.Json;
using FormaAI.Contracts.Progress;
using FormaAI.Contracts.Users;

namespace FormaAI.Web.Services;

public sealed class ProgressClient(HttpClient http)
{
    public Task<WeightProgressResponse?> GetWeight(DateOnly from, DateOnly to) =>
        http.GetFromJsonAsync<WeightProgressResponse>($"api/v1/progress/weight?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public async Task<IReadOnlyList<BodyMeasurementResponse>> GetBodyMeasurements(DateOnly from, DateOnly to) =>
        await http.GetFromJsonAsync<List<BodyMeasurementResponse>>($"api/v1/progress/body-measurements?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? [];

    public async Task<IReadOnlyList<MuscleVolumePoint>> GetMuscleVolume(DateOnly from, DateOnly to) =>
        await http.GetFromJsonAsync<List<MuscleVolumePoint>>($"api/v1/progress/muscle-volume?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? [];

    public Task<ExerciseProgressResponse?> GetExercise(Guid exerciseId, DateOnly from, DateOnly to) =>
        http.GetFromJsonAsync<ExerciseProgressResponse>($"api/v1/progress/exercises/{exerciseId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public Task<ConsistencyResponse?> GetConsistency(DateOnly from, DateOnly to) =>
        http.GetFromJsonAsync<ConsistencyResponse>($"api/v1/progress/consistency?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");

    public Task<NutritionAdherenceResponse?> GetNutritionAdherence(DateOnly month) =>
        http.GetFromJsonAsync<NutritionAdherenceResponse>($"api/v1/progress/nutrition-adherence?month={month:yyyy-MM-dd}");

    public Task<ProgressWeekSummaryResponse?> GetWeekSummary() =>
        http.GetFromJsonAsync<ProgressWeekSummaryResponse>("api/v1/progress/week-summary");

    public async Task<BodyMeasurementResponse> SaveMeasurement(SaveBodyMeasurementRequest body)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/body-measurements") { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BodyMeasurementResponse>())!;
    }
}
