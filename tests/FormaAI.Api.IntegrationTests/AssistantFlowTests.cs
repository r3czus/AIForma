using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FormaAI.Application.Assistant;
using FormaAI.Contracts.Assistant;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;
using FormaAI.Contracts.Training;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FormaAI.Api.IntegrationTests;

public sealed class AssistantFlowTests : IClassFixture<AssistantFormaAiFactory>
{
    private readonly AssistantFormaAiFactory _factory;
    public AssistantFlowTests(AssistantFormaAiFactory factory) => _factory = factory;

    [Fact]
    public async Task DraftNeedsConfirmationAndConfirmationIsIdempotent()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "assistant-owner@example.test");
        await Register(other, "assistant-other@example.test");
        var product = await Send<SaveProductRequest, ProductResponse>(owner, HttpMethod.Post, "api/v1/products", new("Skyr", null, 64, 12, 0.2m, 4));

        _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall("create_meal_draft", JsonSerializer.SerializeToElement(new
        {
            name = "Kolacja ze skyrem",
            occurredAt = DateTimeOffset.UtcNow,
            items = new[] { new { productId = product.Id, amountGrams = 200m, isEstimated = false } }
        })), 20, 8));
        _factory.Model.Enqueue(new AssistantModelTurn("Przygotowałem szkic kolacji. Sprawdź go i zatwierdź, jeśli wszystko się zgadza.", null, 30, 12));

        var response = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(owner, HttpMethod.Post, "api/v1/assistant/messages", new(null, "Dodaj mi 200 g skyru na kolację", DateOnly.FromDateTime(DateTime.UtcNow)));
        Assert.NotNull(response.Draft);
        Assert.Equal(128, response.Draft.Macro.CaloriesKcal);
        var before = await owner.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");
        Assert.Empty(before!.Meals);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"api/v1/assistant/actions/{response.Draft.Id}")).StatusCode);

        var first = await Send<object, MealResponse>(owner, HttpMethod.Post, $"api/v1/assistant/actions/{response.Draft.Id}/confirm", new { });
        var second = await Send<object, MealResponse>(owner, HttpMethod.Post, $"api/v1/assistant/actions/{response.Draft.Id}/confirm", new { });
        Assert.Equal(first.Id, second.Id);
        var after = await owner.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");
        Assert.Single(after!.Meals);
    }

    [Fact]
    public async Task TrainingPlanDraftIsValidatedAndSavedOnlyAfterConfirmation()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "assistant-training@example.test");
        var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(client, HttpMethod.Post, "api/v1/exercises", new("Przysiad testowy", FormaAI.Domain.Training.MuscleGroup.Quadriceps, FormaAI.Domain.Training.Equipment.Barbell, false));
        var exerciseId = exercise.Id;
        var plan = new
        {
            name = "Plan siłowy 3 dni",
            goal = "Budowa siły",
            startsOn = DateOnly.FromDateTime(DateTime.UtcNow),
            days = new[] { new { name = "Dzień A", dayOfWeek = (DayOfWeek?)DayOfWeek.Monday, exercises = new[] { new { exerciseId, sets = 3, minReps = 5, maxReps = 8, targetRir = (decimal?)2, restSeconds = (int?)120 } } } }
        };
        _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall("create_training_plan_draft", JsonSerializer.SerializeToElement(new { plan })), 20, 8));
        _factory.Model.Enqueue(new AssistantModelTurn("Plan jest gotowy do sprawdzenia i zatwierdzenia.", null, 30, 12));

        var response = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(client, HttpMethod.Post, "api/v1/assistant/messages", new(null, "Przygotuj plan siłowy", DateOnly.FromDateTime(DateTime.UtcNow)));
        Assert.NotNull(response.TrainingPlanDraft);
        Assert.Empty((await client.GetFromJsonAsync<List<TrainingPlanResponse>>("api/v1/training-plans"))!);

        var first = await Send<object, TrainingPlanResponse>(client, HttpMethod.Post, $"api/v1/assistant/actions/{response.TrainingPlanDraft.Id}/confirm", new { });
        var second = await Send<object, TrainingPlanResponse>(client, HttpMethod.Post, $"api/v1/assistant/actions/{response.TrainingPlanDraft.Id}/confirm", new { });
        Assert.Equal(first.Id, second.Id);
        Assert.Single((await client.GetFromJsonAsync<List<TrainingPlanResponse>>("api/v1/training-plans"))!);
    }

    [Fact]
    public async Task CompletedWorkoutDraftIsSavedOnlyAfterExplicitConfirmation()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "assistant-completed-workout@example.test");
        var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(
            client,
            HttpMethod.Post,
            "api/v1/exercises",
            new("Wyciskanie sztangi testowe", FormaAI.Domain.Training.MuscleGroup.Chest, FormaAI.Domain.Training.Equipment.Barbell, false));
        var localDate = DateOnly.FromDateTime(DateTime.UtcNow);

        _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall(
            "create_completed_workout_draft",
            JsonSerializer.SerializeToElement(new
            {
                name = "Trening klatki",
                localDate,
                exercises = new[]
                {
                    new
                    {
                        exerciseId = exercise.Id,
                        exerciseName = exercise.Name,
                        sets = new[]
                        {
                            new { weightKg = 50m, repetitions = 10, rir = (decimal?)2 },
                            new { weightKg = 55m, repetitions = 8, rir = (decimal?)1 }
                        }
                    }
                }
            })), 20, 8));
        _factory.Model.Enqueue(new AssistantModelTurn(
            "Rozpisałem wykonany trening. Sprawdź serie i zatwierdź zapis.",
            null,
            30,
            12));

        var response = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(
            client,
            HttpMethod.Post,
            "api/v1/assistant/messages",
            new(null, "Dziś zrobiłem wyciskanie: 50 na 10 i 55 na 8", localDate));

        Assert.NotNull(response.CompletedWorkoutDraft);
        Assert.Equal(2, response.CompletedWorkoutDraft.Exercises.Single().Sets.Count);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("api/v1/workout-sessions/active")).StatusCode);

        var correctedExercise = response.CompletedWorkoutDraft.Exercises.Single() with
        {
            Sets =
            [
                response.CompletedWorkoutDraft.Exercises.Single().Sets[0] with { WeightKg = 52.5m },
                response.CompletedWorkoutDraft.Exercises.Single().Sets[1]
            ]
        };
        var corrected = await Send<UpdateAssistantCompletedWorkoutDraftRequest, AssistantCompletedWorkoutDraftResponse>(
            client,
            HttpMethod.Put,
            $"api/v1/assistant/actions/{response.CompletedWorkoutDraft.Id}/completed-workout",
            new("Trening klatki — poprawiony", localDate, [correctedExercise]));
        Assert.Equal(52.5m, corrected.Exercises.Single().Sets[0].WeightKg);

        var first = await Send<object, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            $"api/v1/assistant/actions/{response.CompletedWorkoutDraft.Id}/confirm",
            new { });
        var second = await Send<object, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            $"api/v1/assistant/actions/{response.CompletedWorkoutDraft.Id}/confirm",
            new { });

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(FormaAI.Domain.Training.SessionStatus.Completed, first.Status);
        Assert.Equal("Trening klatki — poprawiony", first.Name);
        Assert.Equal(52.5m, first.Exercises.Single().Sets[0].WeightKg);
        Assert.Equal(2, first.Exercises.Single().Sets.Count);
    }

    [Fact]
    public async Task ConversationAcceptsASecondUserMessage()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "assistant-conversation@example.test");
        _factory.Model.Enqueue(new AssistantModelTurn("Jaki jest Twój cel?", null, 12, 6));
        _factory.Model.Enqueue(new AssistantModelTurn("Dziękuję, przygotuję propozycję.", null, 14, 7));

        var first = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(client, HttpMethod.Post, "api/v1/assistant/messages", new(null, "Pomóż mi ułożyć plan", DateOnly.FromDateTime(DateTime.UtcNow)));
        var second = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(client, HttpMethod.Post, "api/v1/assistant/messages", new(first.ConversationId, "Chcę trenować trzy razy w tygodniu", DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(first.ConversationId, second.ConversationId);
        Assert.Equal("Dziękuję, przygotuję propozycję.", second.Reply);
    }

    [Fact]
    public async Task AssistantCanRecoverFromOneRepeatedToolCall()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "assistant-repeat@example.test");
        var arguments = JsonSerializer.SerializeToElement(new { query = "przysiad", limit = 5 });
        _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall("search_exercises", arguments), 10, 5));
        _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall("search_exercises", arguments), 10, 5));
        _factory.Model.Enqueue(new AssistantModelTurn("Mam już potrzebne ćwiczenia i mogę ułożyć plan.", null, 12, 6));

        var response = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(client, HttpMethod.Post, "api/v1/assistant/messages", new(null, "Znajdź ćwiczenia do planu", DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal("Mam już potrzebne ćwiczenia i mogę ułożyć plan.", response.Reply);
    }

    [Fact]
    public async Task NutritionSummaryExposesMissingMacrosAndOverages()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "assistant-macros@example.test");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await Send<SaveNutritionTargetRequest, NutritionTargetResponse>(
            client, HttpMethod.Post, "api/v1/nutrition-targets", new(today, 2000, 150, 70, 220));
        _factory.Model.Enqueue(new AssistantModelTurn(null,
            new AssistantToolCall("get_today_nutrition_summary", JsonSerializer.SerializeToElement(new { })), 10, 5));
        _factory.Model.Enqueue(new AssistantModelTurn("Proponuję konkretne danie.", null, 20, 8));

        await Send<SendAssistantMessageRequest, AssistantMessageResponse>(
            client, HttpMethod.Post, "api/v1/assistant/messages", new(null, "Co zjeść, żeby dobić makro?", today));

        var result = _factory.Model.LastRequest!.ToolResults.Single().Result;
        using var json = JsonDocument.Parse(result);
        Assert.True(json.RootElement.GetProperty("hasTarget").GetBoolean());
        Assert.Equal(2000, json.RootElement.GetProperty("remaining").GetProperty("caloriesKcal").GetDecimal());
        Assert.Equal(0, json.RootElement.GetProperty("overBy").GetProperty("proteinG").GetDecimal());
    }

    private static async Task Register(HttpClient client, string email) =>
        _ = await Send<RegisterRequest, CurrentUserResponse>(client, HttpMethod.Post, "api/account/register", new(email, "FormaAI!123", "UTC"));

    private static async Task<TResponse> Send<TRequest, TResponse>(HttpClient client, HttpMethod method, string uri, TRequest body)
    {
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        var request = new HttpRequestMessage(method, uri) { Content = JsonContent.Create(body) };
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TResponse>())!;
    }
}

public sealed class AssistantFormaAiFactory : FormaAiFactory
{
    public FakeAssistantModel Model { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAssistantModel>();
            services.AddSingleton<IAssistantModel>(Model);
        });
    }
}

public sealed class FakeAssistantModel : IAssistantModel
{
    private readonly ConcurrentQueue<AssistantModelTurn> _turns = new();
    public AssistantModelRequest? LastRequest { get; private set; }
    public void Enqueue(AssistantModelTurn turn) => _turns.Enqueue(turn);
    public Task<AssistantModelTurn> Generate(AssistantModelRequest request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_turns.TryDequeue(out var turn) ? turn : new AssistantModelTurn("Nie mam kolejnej odpowiedzi.", null, 0, 0));
    }
    public Task<MealPhotoDraftResponse> AnalyzeMealPhotos(IReadOnlyList<MealImage> images, CancellationToken cancellationToken) =>
        Task.FromResult(new MealPhotoDraftResponse("Test", null, []));
    public Task<MealPhotoDraftResponse> AnalyzeMealText(string description, CancellationToken cancellationToken) =>
        Task.FromResult(new MealPhotoDraftResponse("Test", null, []));
}
