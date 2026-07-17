using System.Net.Http.Json;
using FormaAI.Contracts.Progress;
using FormaAI.Contracts.Training;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Training;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FormaAI.Api.IntegrationTests;

public sealed class ProgressFlowTests : IClassFixture<FormaAiFactory>
{
    private readonly FormaAiFactory _factory;
    public ProgressFlowTests(FormaAiFactory factory) => _factory = factory;

    [Fact]
    public async Task ProgressUsesOnlyCompletedDataOwnedByCurrentUser()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "progress-owner@example.test");
        await Register(other, "progress-other@example.test");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await Send<SaveBodyMeasurementRequest, BodyMeasurementResponse>(owner, "api/v1/body-measurements", new(today.AddDays(-6), 80m, 90m, null));
        await Send<SaveBodyMeasurementRequest, BodyMeasurementResponse>(owner, "api/v1/body-measurements", new(today, 78m, 88m, "rano"));
        await Send<SaveBodyMeasurementRequest, BodyMeasurementResponse>(other, "api/v1/body-measurements", new(today, 200m, null, null));
        var weight = await owner.GetFromJsonAsync<WeightProgressResponse>($"api/v1/progress/weight?from={today.AddDays(-10):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        Assert.Equal(79m, weight!.CurrentAverageKg);
        Assert.DoesNotContain(weight.Points, x => x.WeightKg == 200m);

        var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(owner, "api/v1/exercises", new("Próba progresu", MuscleGroup.Back, Equipment.Cable, false));
        var plan = await Send<SaveTrainingPlanRequest, TrainingPlanResponse>(owner, "api/v1/training-plans",
            new("Progres", "Test", today, [new("Dzień", DateTime.UtcNow.DayOfWeek, [new(exercise.Id, 2, 8, 10, 2, 60)])]));
        var session = await Send<StartWorkoutRequest, WorkoutSessionResponse>(owner, "api/v1/workout-sessions", new(plan.Days.Single().Id));
        var workoutExercise = session.Exercises.Single();
        await Send<SaveSetRequest, CompletedSetResponse>(owner, $"api/v1/workout-sessions/{session.Id}/sets", new(workoutExercise.Id, 1, 50m, 10, 2, SetType.Working));
        await SendNoContent(owner, $"api/v1/workout-sessions/{session.Id}/complete");

        var progress = await owner.GetFromJsonAsync<ExerciseProgressResponse>($"api/v1/progress/exercises/{exercise.Id}?from={today.AddDays(-1):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}");
        Assert.Equal(500m, progress!.Points.Single().VolumeKg);
        Assert.Equal(66.67m, progress.Points.Single().EstimatedOneRepMaxKg);
        var consistency = await owner.GetFromJsonAsync<ConsistencyResponse>($"api/v1/progress/consistency?from={today.AddDays(-1):yyyy-MM-dd}&to={today.AddDays(1):yyyy-MM-dd}");
        Assert.Equal(1, consistency!.CompletedWorkouts);

        var otherProgress = await other.GetFromJsonAsync<WeightProgressResponse>($"api/v1/progress/weight?from={today.AddDays(-10):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        Assert.Single(otherProgress!.Points);
        Assert.Equal(200m, otherProgress.CurrentAverageKg);
    }

    private static async Task Register(HttpClient client, string email) =>
        _ = await Send<RegisterRequest, CurrentUserResponse>(client, "api/account/register", new(email, "FormaAI!123", "UTC"));

    private static async Task<TResponse> Send<TRequest, TResponse>(HttpClient client, string uri, TRequest body)
    {
        using var response = await client.SendAsync(await Request(client, HttpMethod.Post, uri, body));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private static async Task SendNoContent(HttpClient client, string uri)
    {
        using var response = await client.SendAsync(await Request(client, HttpMethod.Post, uri, null));
        response.EnsureSuccessStatusCode();
    }

    private static async Task<HttpRequestMessage> Request(HttpClient client, HttpMethod method, string uri, object? body)
    {
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }
}
