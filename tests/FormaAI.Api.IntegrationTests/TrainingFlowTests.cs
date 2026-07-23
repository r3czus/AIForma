using System.Net;
using System.Net.Http.Json;
using FormaAI.Contracts.Training;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Training;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FormaAI.Api.IntegrationTests;

public sealed class TrainingFlowTests : IClassFixture<FormaAiFactory>
{
    private readonly FormaAiFactory _factory;
    public TrainingFlowTests(FormaAiFactory factory) => _factory = factory;

    [Fact]
    public async Task UserCanCompletePlannedWorkoutAndSeeExerciseHistory()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "training-owner@example.test");
        await Register(other, "training-other@example.test");
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        await Send<SaveNutritionTargetRequest, NutritionTargetResponse>(owner, HttpMethod.Post, "api/v1/nutrition-targets", new(date, 2000, 150, 70, 220));

        var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(owner, HttpMethod.Post, "api/v1/exercises", new("Wyciskanie hantli", MuscleGroup.Chest, Equipment.Dumbbell, false, "Ławka dodatnia, kontrolowany ruch."));
        Assert.Equal("Ławka dodatnia, kontrolowany ruch.", exercise.Description);
        var planRequest = new SaveTrainingPlanRequest("Plan 3 dni", "Siła", DateOnly.FromDateTime(DateTime.UtcNow),
            [new("Góra A", DateTime.UtcNow.DayOfWeek, [new(exercise.Id, 3, 8, 10, 2, 90)])]);
        var plan = await Send<SaveTrainingPlanRequest, TrainingPlanResponse>(owner, HttpMethod.Post, "api/v1/training-plans", planRequest);
        await SendNoContent(owner, HttpMethod.Post, $"api/v1/training-plans/{plan.Id}/activate");

        var plannedDay = await owner.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}");
        Assert.Equal(2000, plannedDay!.Target!.CaloriesKcal);
        Assert.Equal(0, plannedDay.TrainingBonusCalories);

        var today = await owner.GetFromJsonAsync<TodayWorkoutResponse>("api/v1/workouts/today");
        Assert.Equal("Góra A", today!.DayName);
        var session = await Send<StartWorkoutRequest, WorkoutSessionResponse>(owner, HttpMethod.Post, "api/v1/workout-sessions", new(today.TrainingDayId));
        var workoutExercise = session.Exercises.Single();
        await Send<SaveSetRequest, CompletedSetResponse>(owner, HttpMethod.Post, $"api/v1/workout-sessions/{session.Id}/sets", new(workoutExercise.Id, 1, 32.5m, 9, 2, SetType.Working));

        var saved = await owner.GetFromJsonAsync<WorkoutSessionResponse>($"api/v1/workout-sessions/{session.Id}");
        Assert.Equal(32.5m, saved!.Exercises.Single().Sets.Single().WeightKg);
        await SendNoContent(owner, HttpMethod.Post, $"api/v1/workout-sessions/{session.Id}/complete");
        var completedDay = await owner.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{date:yyyy-MM-dd}");
        Assert.True(completedDay!.HasCompletedWorkout);
        Assert.Equal(50, completedDay.TrainingBonusCalories);
        Assert.Equal(2050, completedDay.Target!.CaloriesKcal);
        var history = await owner.GetFromJsonAsync<List<ExerciseHistoryEntry>>($"api/v1/exercises/{exercise.Id}/history");
        Assert.Equal(292.5m, history!.Single().Volume);

        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"api/v1/workout-sessions/{session.Id}")).StatusCode);
        Assert.DoesNotContain((await other.GetFromJsonAsync<List<ExerciseResponse>>("api/v1/exercises"))!, x => x.Id == exercise.Id);
    }

    [Fact]
    public async Task UserCanStartQuickWorkoutWithoutPlan()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "quick-workout@example.test");
        var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(
            client,
            HttpMethod.Post,
            "api/v1/exercises",
            new("Wiosłowanie siedząc", MuscleGroup.Back, Equipment.Cable, false));

        var session = await Send<StartQuickWorkoutRequest, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            "api/v1/workout-sessions/quick",
            new("Trening na dziś", 45, [new(exercise.Id, 4)]));

        Assert.Equal("Trening na dziś", session.Name);
        Assert.Equal(45, session.TimeLimitMinutes);
        Assert.False(session.IsShortened);
        var item = Assert.Single(session.Exercises);
        Assert.Equal(exercise.Id, item.ExerciseId);
        Assert.Equal(4, item.PlannedSets);
        Assert.Equal(8, item.MinReps);
        Assert.Equal(12, item.MaxReps);
        Assert.Equal(2, item.TargetRir);
        Assert.Equal(90, item.RestSeconds);
    }

    private static async Task Register(HttpClient client, string email) =>
        _ = await Send<RegisterRequest, CurrentUserResponse>(client, HttpMethod.Post, "api/account/register", new(email, "FormaAI!123", "UTC"));

    private static async Task<TResponse> Send<TRequest, TResponse>(HttpClient client, HttpMethod method, string uri, TRequest body)
    {
        var request = await Request(client, method, uri, body);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }

    private static async Task SendNoContent(HttpClient client, HttpMethod method, string uri)
    {
        var response = await client.SendAsync(await Request(client, method, uri, null));
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
