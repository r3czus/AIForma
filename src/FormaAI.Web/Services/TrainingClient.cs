using System.Net;
using System.Net.Http.Json;
using FormaAI.Contracts.Training;
using FormaAI.Contracts.Users;

namespace FormaAI.Web.Services;

public sealed class TrainingClient(HttpClient http)
{
    public async Task<IReadOnlyList<ExerciseResponse>> GetExercises() => await http.GetFromJsonAsync<List<ExerciseResponse>>("api/v1/exercises") ?? [];
    public async Task<IReadOnlyList<TrainingPlanResponse>> GetPlans() => await http.GetFromJsonAsync<List<TrainingPlanResponse>>("api/v1/training-plans") ?? [];
    public async Task<TodayWorkoutResponse?> GetToday()
    {
        var response = await http.GetAsync("api/v1/workouts/today");
        return response.StatusCode == HttpStatusCode.NotFound ? null : await Read<TodayWorkoutResponse>(response);
    }
    public async Task<WorkoutSessionResponse> GetSession(Guid id) => (await http.GetFromJsonAsync<WorkoutSessionResponse>($"api/v1/workout-sessions/{id}"))!;
    public async Task<WorkoutSessionResponse?> GetActiveSession()
    {
        using var response = await http.GetAsync("api/v1/workout-sessions/active");
        return response.StatusCode == HttpStatusCode.NotFound ? null : await Read<WorkoutSessionResponse>(response);
    }
    public async Task<IReadOnlyList<ExerciseHistoryEntry>> GetHistory(Guid id) => await http.GetFromJsonAsync<List<ExerciseHistoryEntry>>($"api/v1/exercises/{id}/history") ?? [];
    public Task<ExerciseResponse> SaveExercise(SaveExerciseRequest request) => Send<ExerciseResponse>(HttpMethod.Post, "api/v1/exercises", request);
    public Task<ExerciseResponse> UpdateExercise(Guid id, SaveExerciseRequest request) => Send<ExerciseResponse>(HttpMethod.Put, $"api/v1/exercises/{id}", request);
    public Task<TrainingPlanResponse> SavePlan(SaveTrainingPlanRequest request) => Send<TrainingPlanResponse>(HttpMethod.Post, "api/v1/training-plans", request);
    public Task<TrainingPlanResponse> UpdatePlan(Guid id, SaveTrainingPlanRequest request) => Send<TrainingPlanResponse>(HttpMethod.Put, $"api/v1/training-plans/{id}", request);
    public Task<WorkoutSessionResponse> Start(Guid dayId) => Send<WorkoutSessionResponse>(HttpMethod.Post, "api/v1/workout-sessions", new StartWorkoutRequest(dayId));
    public Task<CompletedSetResponse> SaveSet(Guid sessionId, SaveSetRequest request) => Send<CompletedSetResponse>(HttpMethod.Post, $"api/v1/workout-sessions/{sessionId}/sets", request);
    public Task<CompletedSetResponse> UpdateSet(Guid sessionId, Guid setId, SaveSetRequest request) => Send<CompletedSetResponse>(HttpMethod.Put, $"api/v1/workout-sessions/{sessionId}/sets/{setId}", request);
    public Task<WorkoutExerciseResponse> AddExercise(Guid sessionId, AddWorkoutExerciseRequest request) => Send<WorkoutExerciseResponse>(HttpMethod.Post, $"api/v1/workout-sessions/{sessionId}/exercises", request);
    public Task<WorkoutExerciseResponse> ReplaceExercise(Guid sessionId, Guid workoutExerciseId, Guid exerciseId) => Send<WorkoutExerciseResponse>(HttpMethod.Put, $"api/v1/workout-sessions/{sessionId}/exercises/{workoutExerciseId}", new ReplaceWorkoutExerciseRequest(exerciseId));
    public Task SaveNotes(Guid sessionId, string? notes) => SendNoContent(HttpMethod.Put, $"api/v1/workout-sessions/{sessionId}/notes", new SaveWorkoutNotesRequest(notes));
    public Task Activate(Guid planId) => SendNoContent(HttpMethod.Post, $"api/v1/training-plans/{planId}/activate");
    public Task Complete(Guid sessionId) => SendNoContent(HttpMethod.Post, $"api/v1/workout-sessions/{sessionId}/complete");
    public Task Abandon(Guid sessionId) => SendNoContent(HttpMethod.Post, $"api/v1/workout-sessions/{sessionId}/abandon");

    private async Task<T> Send<T>(HttpMethod method, string uri, object body)
    {
        using var response = await Send(method, uri, body);
        return await Read<T>(response);
    }
    private async Task SendNoContent(HttpMethod method, string uri, object? body = null)
    {
        using var response = await Send(method, uri, body);
        response.EnsureSuccessStatusCode();
    }
    private async Task<HttpResponseMessage> Send(HttpMethod method, string uri, object? body)
    {
        var csrf = await http.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await http.SendAsync(request);
    }
    private static async Task<T> Read<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
