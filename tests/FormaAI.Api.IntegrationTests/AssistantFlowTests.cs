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
    public void Enqueue(AssistantModelTurn turn) => _turns.Enqueue(turn);
    public Task<AssistantModelTurn> Generate(AssistantModelRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(_turns.TryDequeue(out var turn) ? turn : new AssistantModelTurn("Nie mam kolejnej odpowiedzi.", null, 0, 0));
}
