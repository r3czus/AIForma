using System.Net;
using System.Net.Http.Json;
using FormaAI.Contracts.Training;
using FormaAI.Contracts.Nutrition;
using FormaAI.Contracts.Users;
using FormaAI.Domain.Training;
using FormaAI.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task QuickWorkoutPreservesConfigurationAndSetPresets()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "quick-workout-configuration@example.test");
        var press = await CreateExercise(client, "Wyciskanie konfigurowane", MuscleGroup.Chest, Equipment.Barbell);
        var row = await CreateExercise(client, "Wiosłowanie konfigurowane", MuscleGroup.Back, Equipment.Dumbbell);
        var groupId = Guid.NewGuid();

        var session = await Send<StartQuickWorkoutRequest, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            "api/v1/workout-sessions/quick",
            new("Trening przygotowany", 50,
            [
                new(
                    press.Id,
                    2,
                    6,
                    8,
                    1,
                    120,
                    groupId,
                    1,
                    20,
                    [
                        new(1, 80, 8, 2),
                        new(2, 82.5m, 6, 1)
                    ]),
                new(row.Id, 2, 10, 12, 2, 90, groupId, 2, 75)
            ]));

        Assert.Equal(2, session.Exercises.Count);
        var first = session.Exercises[0];
        Assert.Equal(6, first.MinReps);
        Assert.Equal(8, first.MaxReps);
        Assert.Equal(1, first.TargetRir);
        Assert.Equal(120, first.RestSeconds);
        Assert.Equal(groupId, first.SupersetGroupId);
        Assert.Equal(1, first.SupersetPosition);
        Assert.Equal(20, first.IntervalSeconds);
        Assert.Collection(
            first.Presets!,
            preset =>
            {
                Assert.Equal(1, preset.SetNumber);
                Assert.Equal(80, preset.WeightKg);
                Assert.Equal(8, preset.Repetitions);
                Assert.Equal(2, preset.Rir);
            },
            preset =>
            {
                Assert.Equal(2, preset.SetNumber);
                Assert.Equal(82.5m, preset.WeightKg);
                Assert.Equal(6, preset.Repetitions);
                Assert.Equal(1, preset.Rir);
            });
    }

    [Fact]
    public async Task ReplacingExerciseAfterASetShortensTheOriginalPlan()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "training-partial-swap@example.test");
        var original = await CreateExercise(client, "Ćwiczenie przed zamianą", MuscleGroup.Chest, Equipment.Barbell);
        var replacement = await CreateExercise(client, "Ćwiczenie po zamianie", MuscleGroup.Chest, Equipment.Dumbbell);
        var session = await Send<StartQuickWorkoutRequest, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            "api/v1/workout-sessions/quick",
            new("Zamiana", 45, [new(original.Id, 3)]));
        var originalSessionExercise = session.Exercises.Single();
        await Send<SaveSetRequest, CompletedSetResponse>(
            client,
            HttpMethod.Post,
            $"api/v1/workout-sessions/{session.Id}/sets",
            new(originalSessionExercise.Id, 1, 50, 8, 2, SetType.Working));

        await Send<ReplaceWorkoutExerciseRequest, WorkoutExerciseResponse>(
            client,
            HttpMethod.Put,
            $"api/v1/workout-sessions/{session.Id}/exercises/{originalSessionExercise.Id}",
            new(replacement.Id));
        var saved = await client.GetFromJsonAsync<WorkoutSessionResponse>($"api/v1/workout-sessions/{session.Id}");

        Assert.Equal(1, saved!.Exercises.Single(x => x.Id == originalSessionExercise.Id).PlannedSets);
        Assert.Equal(2, saved.Exercises.Single(x => x.ExerciseId == replacement.Id).PlannedSets);
        Assert.Equal(3, saved.Exercises.Sum(x => x.PlannedSets));
    }

    [Fact]
    public async Task PlanAndSessionPreserveSupersetTiming()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "training-superset@example.test");
        var press = await CreateExercise(client, "Wyciskanie superseria", MuscleGroup.Chest, Equipment.Dumbbell);
        var row = await CreateExercise(client, "Wiosłowanie superseria", MuscleGroup.Back, Equipment.Dumbbell);
        var groupId = Guid.NewGuid();
        var planRequest = new SaveTrainingPlanRequest(
            "Plan superserii",
            "Tempo",
            DateOnly.FromDateTime(DateTime.UtcNow),
            [
                new TrainingDayRequest(
                    "Góra",
                    DateTime.UtcNow.DayOfWeek,
                    [
                        new PlannedExerciseRequest(press.Id, 3, 8, 12, 2, 105, groupId, 1, 12),
                        new PlannedExerciseRequest(row.Id, 3, 8, 12, 2, 105, groupId, 2, 12)
                    ])
            ]);

        var plan = await Send<SaveTrainingPlanRequest, TrainingPlanResponse>(
            client,
            HttpMethod.Post,
            "api/v1/training-plans",
            planRequest);
        var planned = plan.Days.Single().Exercises.OrderBy(x => x.SupersetPosition).ToList();

        Assert.All(planned, x => Assert.Equal(groupId, x.SupersetGroupId));
        Assert.Equal([1, 2], planned.Select(x => x.SupersetPosition).ToArray());
        Assert.All(planned, x => Assert.Equal(12, x.IntervalSeconds));

        var session = await Send<StartWorkoutRequest, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            "api/v1/workout-sessions",
            new(plan.Days.Single().Id));

        Assert.All(session.Exercises, x => Assert.Equal(groupId, x.SupersetGroupId));
        Assert.Equal([1, 2], session.Exercises.OrderBy(x => x.Order).Select(x => x.SupersetPosition).ToArray());
        Assert.All(session.Exercises, x => Assert.Equal(12, x.IntervalSeconds));
    }

    [Fact]
    public async Task ExerciseDetailsExposeGlobalAndOwnButNotForeignExercises()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "exercise-details-owner@example.test");
        await Register(other, "exercise-details-other@example.test");

        var own = await Send<SaveExerciseRequest, ExerciseResponse>(
            owner,
            HttpMethod.Post,
            "api/v1/exercises",
            new("Moje ćwiczenie", MuscleGroup.Chest, Equipment.Dumbbell, false, "Opis własnego ćwiczenia."));
        var foreign = await Send<SaveExerciseRequest, ExerciseResponse>(
            other,
            HttpMethod.Post,
            "api/v1/exercises",
            new("Cudze ćwiczenie", MuscleGroup.Back, Equipment.Cable, false));

        var global = new Exercise(null, "Ćwiczenie globalne", MuscleGroup.Quadriceps, Equipment.Barbell, description: "Opis globalny.");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Exercises.Add(global);
            await db.SaveChangesAsync();
        }

        var globalResponse = await owner.GetFromJsonAsync<ExerciseResponse>($"api/v1/exercises/{global.Id}");
        var ownResponse = await owner.GetFromJsonAsync<ExerciseResponse>($"api/v1/exercises/{own.Id}");
        var foreignResponse = await owner.GetAsync($"api/v1/exercises/{foreign.Id}");

        Assert.Equal(global.Id, globalResponse!.Id);
        Assert.Equal("Opis globalny.", globalResponse.Description);
        Assert.Equal(own.Id, ownResponse!.Id);
        Assert.Equal(HttpStatusCode.NotFound, foreignResponse.StatusCode);
    }

    [Fact]
    public async Task OwnerCanUploadExerciseAnimationAndOtherUserCannotReadIt()
    {
        var options = new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") };
        var owner = _factory.CreateClient(options);
        var other = _factory.CreateClient(options);
        await Register(owner, "exercise-media-owner@example.test");
        await Register(other, "exercise-media-other@example.test");
        var exercise = await CreateExercise(owner, "Animowane ćwiczenie", MuscleGroup.FullBody, Equipment.Bodyweight);

        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent("GIF89a"u8.ToArray());
        bytes.Headers.ContentType = new("image/gif");
        content.Add(bytes, "media", "ruch.gif");
        content.Add(new StringContent("Autor testowy"), "author");
        content.Add(new StringContent("CC BY-SA 4.0"), "license");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/exercises/{exercise.Id}/media") { Content = content };
        var csrf = await owner.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var upload = await owner.SendAsync(request);
        upload.EnsureSuccessStatusCode();
        var saved = (await upload.Content.ReadFromJsonAsync<ExerciseResponse>())!;
        Assert.Equal("image/gif", saved.MediaContentType);
        Assert.Equal("Autor testowy · CC BY-SA 4.0", saved.MediaAttribution);
        Assert.NotNull(saved.MediaUrl);
        Assert.Equal("image/gif", (await owner.GetAsync(saved.MediaUrl)).Content.Headers.ContentType!.MediaType);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync(saved.MediaUrl)).StatusCode);
    }

    [Fact]
    public async Task OwnerCanUploadExercisePhoto()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "exercise-photo-owner@example.test");
        var exercise = await CreateExercise(client, "Ćwiczenie ze zdjęciem", MuscleGroup.FullBody, Equipment.Bodyweight);

        using var content = new MultipartFormDataContent();
        var bytes = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        bytes.Headers.ContentType = new("image/png");
        content.Add(bytes, "media", "ruch.png");
        content.Add(new StringContent("Użytkownik"), "author");
        content.Add(new StringContent("Materiał własny"), "license");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/exercises/{exercise.Id}/media") { Content = content };
        var csrf = await client.GetFromJsonAsync<AntiforgeryResponse>("api/account/antiforgery");
        request.Headers.Add("X-CSRF-TOKEN", csrf!.Token);

        var upload = await client.SendAsync(request);
        upload.EnsureSuccessStatusCode();
        var saved = (await upload.Content.ReadFromJsonAsync<ExerciseResponse>())!;

        Assert.Equal("image/png", saved.MediaContentType);
        Assert.NotNull(saved.MediaUrl);
        Assert.Equal("image/png", (await client.GetAsync(saved.MediaUrl)).Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task ReplacementBeforeFirstSetKeepsPrescriptionAndReplacesInPlace()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "replace-before-set@example.test");
        var originalExercise = await CreateExercise(client, "Wyciskanie sztangi", MuscleGroup.Chest, Equipment.Barbell);
        var replacementExercise = await CreateExercise(client, "Wyciskanie hantli", MuscleGroup.Chest, Equipment.Dumbbell);
        var session = await StartQuickWorkout(client, "Zamiana przed serią", (originalExercise.Id, 4));
        var original = Assert.Single(session.Exercises);

        var replacement = await Send<ReplaceWorkoutExerciseRequest, WorkoutExerciseResponse>(
            client,
            HttpMethod.Put,
            $"api/v1/workout-sessions/{session.Id}/exercises/{original.Id}",
            new(replacementExercise.Id));

        Assert.Equal(original.Id, replacement.Id);
        Assert.Equal(replacementExercise.Id, replacement.ExerciseId);
        Assert.Equal(original.PlannedSets, replacement.PlannedSets);
        Assert.Equal(original.MinReps, replacement.MinReps);
        Assert.Equal(original.MaxReps, replacement.MaxReps);
        Assert.Equal(original.TargetRir, replacement.TargetRir);
        Assert.Equal(original.RestSeconds, replacement.RestSeconds);
    }

    [Fact]
    public async Task ReplacementAfterASetPreservesHistoryAndAddsRemainingSetsNext()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "replace-after-set@example.test");
        var originalExercise = await CreateExercise(client, "Przysiad", MuscleGroup.Quadriceps, Equipment.Barbell);
        var replacementExercise = await CreateExercise(client, "Suwnica", MuscleGroup.Quadriceps, Equipment.Machine);
        var session = await StartQuickWorkout(client, "Zamiana po serii", (originalExercise.Id, 4));
        var original = Assert.Single(session.Exercises);
        await Send<SaveSetRequest, CompletedSetResponse>(
            client,
            HttpMethod.Post,
            $"api/v1/workout-sessions/{session.Id}/sets",
            new(original.Id, 1, 100, 8, 2, SetType.Working));

        var replacement = await Send<ReplaceWorkoutExerciseRequest, WorkoutExerciseResponse>(
            client,
            HttpMethod.Put,
            $"api/v1/workout-sessions/{session.Id}/exercises/{original.Id}",
            new(replacementExercise.Id));
        var refreshed = await client.GetFromJsonAsync<WorkoutSessionResponse>($"api/v1/workout-sessions/{session.Id}");

        Assert.NotEqual(original.Id, replacement.Id);
        Assert.Equal(replacementExercise.Id, replacement.ExerciseId);
        Assert.Equal(original.Order + 1, replacement.Order);
        Assert.Equal(3, replacement.PlannedSets);
        Assert.Single(refreshed!.Exercises.Single(x => x.Id == original.Id).Sets);
        Assert.Equal([original.Id, replacement.Id], refreshed.Exercises.OrderBy(x => x.Order).Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task ReplacementRejectsExerciseAlreadyPresentInSession()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await Register(client, "replace-duplicate@example.test");
        var firstExercise = await CreateExercise(client, "Wiosłowanie", MuscleGroup.Back, Equipment.Cable);
        var secondExercise = await CreateExercise(client, "Ściąganie drążka", MuscleGroup.Back, Equipment.Cable);
        var session = await StartQuickWorkout(client, "Duplikat zamiany", (firstExercise.Id, 3), (secondExercise.Id, 3));
        var rows = session.Exercises.OrderBy(x => x.Order).ToList();

        using var response = await client.SendAsync(await Request(
            client,
            HttpMethod.Put,
            $"api/v1/workout-sessions/{session.Id}/exercises/{rows[0].Id}",
            new ReplaceWorkoutExerciseRequest(rows[1].ExerciseId!.Value)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static Task<ExerciseResponse> CreateExercise(HttpClient client, string name, MuscleGroup group, Equipment equipment) =>
        Send<SaveExerciseRequest, ExerciseResponse>(client, HttpMethod.Post, "api/v1/exercises", new(name, group, equipment, false));

    private static Task<WorkoutSessionResponse> StartQuickWorkout(
        HttpClient client,
        string name,
        params (Guid ExerciseId, int Sets)[] exercises) =>
        Send<StartQuickWorkoutRequest, WorkoutSessionResponse>(
            client,
            HttpMethod.Post,
            "api/v1/workout-sessions/quick",
            new(name, 45, exercises.Select(x => new QuickWorkoutExerciseRequest(x.ExerciseId, x.Sets)).ToList()));

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
