# Poprawki treningu, jedzenia, zdjęć i asystenta — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wdrożyć uzgodnione poprawki planów i sesji treningowych, skalowania posiłków, wielozdjęciowego importu oraz propozycji dań do brakującego makro, a następnie scalić je do `main` i wystawić wersję testową.

**Architecture:** Zakres jest podzielony na niezależnie testowalne moduły i osobne commity. Logika obliczeń trafia do warstwy Application, reguły sesji i dostęp do danych pozostają po stronie API/Domain, a komponenty Blazor wyłącznie sterują przepływem i prezentacją.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, Entity Framework Core, xUnit, SQL Server LocalDB, Cloudflare Quick Tunnel.

## Global Constraints

- Zachować obecny kierunek wizualny FormaAI i projektować przede wszystkim na telefon.
- Nie zapisywać kluczy API w repozytorium, dokumentacji ani frontendzie.
- Analiza i import przyjmują maksymalnie 5 zdjęć, każde do 12 MB.
- Zmiana planu lub zapis szkicu AI nadal wymagają świadomej akcji użytkownika.
- Każdy zamknięty moduł kończy osobny commit z polskim opisem.
- Każdą zmianę zachowania poprzedza test, który najpierw zawodzi z oczekiwanego powodu.

---

### Task 1: Pionowy plan i strona szczegółów ćwiczenia

**Files:**
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Services/TrainingClient.cs`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Create: `src/FormaAI.Web/Pages/ExerciseDetails.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Produces: `GET api/v1/exercises/{id:guid}` returning `ExerciseResponse`.
- Produces: `TrainingClient.GetExercise(Guid id)`.
- Consumes: existing `ExerciseResponse`, `TrainingPlanResponse` and `PlannedExerciseResponse`.

- [ ] **Step 1: Write failing access tests**

Add integration tests proving that a signed-in user can fetch a global or own exercise and receives `404` for another user's exercise:

```csharp
[Fact]
public async Task ExerciseDetailsExposeGlobalAndOwnButNotForeignExercises()
{
    var global = await SeedExercise(null, "Przysiad");
    var own = await SeedExercise(UserId, "Moje ćwiczenie");
    var foreign = await SeedExercise("other-user", "Cudze ćwiczenie");

    Assert.Equal(global.Id, (await Client.GetFromJsonAsync<ExerciseResponse>($"api/v1/exercises/{global.Id}"))!.Id);
    Assert.Equal(own.Id, (await Client.GetFromJsonAsync<ExerciseResponse>($"api/v1/exercises/{own.Id}"))!.Id);
    Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"api/v1/exercises/{foreign.Id}")).StatusCode);
}
```

- [ ] **Step 2: Run the targeted test and verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter ExerciseDetailsExposeGlobalAndOwnButNotForeignExercises
```

Expected: `FAIL` because `GET api/v1/exercises/{id}` does not exist.

- [ ] **Step 3: Add the minimal exercise endpoint and client**

Add:

```csharp
[HttpGet("exercises/{id:guid}")]
public async Task<ActionResult<ExerciseResponse>> Exercise(Guid id)
{
    var userId = UserId();
    var exercise = await db.Exercises.Include(x => x.MuscleEngagements)
        .SingleOrDefaultAsync(x => x.Id == id && x.IsActive &&
            (x.OwnerUserId == null || x.OwnerUserId == userId));
    return exercise is null ? NotFound() : ExerciseResponse(exercise);
}
```

and:

```csharp
public Task<ExerciseResponse?> GetExercise(Guid id) =>
    http.GetFromJsonAsync<ExerciseResponse>($"api/v1/exercises/{id}");
```

- [ ] **Step 4: Implement the page and plan navigation**

Create route `/training/exercises/{ExerciseId:guid}` with optional `plan` and `day` query parameters. Load the exercise and the selected planned exercise, render description, equipment, muscle shares, sets, reps, RIR and rest. Link own-exercise editing to `/training?editExercise={id}` and plan-day editing to `/training?editPlan={planId}&editDay={dayId}`.

Replace the noninteractive plan row with:

```razor
<button type="button" class="plan-exercise-link"
        @onclick="() => OpenExercise(plan.Id, day.Id, exercise.ExerciseId)">
    <strong>@exercise.ExerciseName</strong>
    <span>@exercise.Sets serie · @exercise.MinReps-@exercise.MaxReps powt.</span>
</button>
```

Handle the edit query parameters after `Reload()` so the existing editors open with the selected item.

- [ ] **Step 5: Make the day layout a full-width vertical list**

Set `.plan-day-grid` to one column at every breakpoint, remove alternating right borders, and give each expanded list full width. Add focus-visible and touch target styles to `.plan-exercise-link`.

- [ ] **Step 6: Verify and commit**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter ExerciseDetails
dotnet build FormaAI.sln
```

Expected: targeted tests and build pass.

Commit:

```powershell
git add src/FormaAI.Api/Controllers/TrainingController.cs src/FormaAI.Web/Services/TrainingClient.cs src/FormaAI.Web/Pages/Training.razor src/FormaAI.Web/Pages/ExerciseDetails.razor src/FormaAI.Web/wwwroot/css/app.css src/FormaAI.Web/wwwroot/css/forma-signal.css tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs
git commit -m "Usprawnić listę planu i szczegóły ćwiczenia"
```

### Task 2: Bezpieczna wymiana ćwiczenia w aktywnej sesji

**Files:**
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Consumes: existing `PUT api/v1/workout-sessions/{id}/exercises/{workoutExerciseId}`.
- Produces: the same endpoint returns the replaced row before any set or a newly inserted `WorkoutExerciseResponse` after completed sets.
- Produces: `WorkoutExercise.ChangeOrder(int order)`.

- [ ] **Step 1: Write failing replacement tests**

Add three tests:

```csharp
[Fact]
public async Task ReplacementBeforeFirstSetKeepsPrescriptionAndReplacesInPlace()
{
    var original = Assert.Single(session.Exercises);
    var replacement = await Put<WorkoutExerciseResponse>(
        client,
        $"api/v1/workout-sessions/{session.Id}/exercises/{original.Id}",
        new ReplaceWorkoutExerciseRequest(replacementExercise.Id));

    Assert.Equal(original.Id, replacement.Id);
    Assert.Equal(replacementExercise.Id, replacement.ExerciseId);
    Assert.Equal(original.PlannedSets, replacement.PlannedSets);
    Assert.Equal(original.MinReps, replacement.MinReps);
    Assert.Equal(original.MaxReps, replacement.MaxReps);
}

[Fact]
public async Task ReplacementAfterASetPreservesHistoryAndAddsRemainingSetsNext()
{
    var original = Assert.Single(session.Exercises);
    await Post<CompletedSetResponse>(
        client,
        $"api/v1/workout-sessions/{session.Id}/sets",
        new SaveSetRequest(original.Id, 1, 80, 8, 2, SetType.Working));

    var replacement = await Put<WorkoutExerciseResponse>(
        client,
        $"api/v1/workout-sessions/{session.Id}/exercises/{original.Id}",
        new ReplaceWorkoutExerciseRequest(replacementExercise.Id));
    var refreshed = await client.GetFromJsonAsync<WorkoutSessionResponse>(
        $"api/v1/workout-sessions/{session.Id}");

    Assert.Single(refreshed!.Exercises.Single(x => x.Id == original.Id).Sets);
    Assert.Equal(original.Order + 1, replacement.Order);
    Assert.Equal(Math.Max(1, original.PlannedSets - 1), replacement.PlannedSets);
}

[Fact]
public async Task ReplacementRejectsExerciseAlreadyPresentInSession()
{
    var first = session.Exercises.OrderBy(x => x.Order).First();
    var second = session.Exercises.OrderBy(x => x.Order).Last();
    var response = await client.PutAsJsonAsync(
        $"api/v1/workout-sessions/{session.Id}/exercises/{first.Id}",
        new ReplaceWorkoutExerciseRequest(second.ExerciseId!.Value));

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

Build the arranged `session`, `replacementExercise`, and authenticated `client` with the same seed/register/start helpers already used in `TrainingFlowTests`. The second test must also reload the source plan and assert that its exercise IDs are unchanged.

- [ ] **Step 2: Run the three tests and verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Replacement
```

Expected: the after-set case fails with the current conflict and the duplicate case is accepted.

- [ ] **Step 3: Implement session-only replacement**

Add domain ordering:

```csharp
public void ChangeOrder(int order)
{
    if (order < 1) throw new ArgumentOutOfRangeException(nameof(order));
    Order = order;
}
```

In the endpoint:

```csharp
if (session.Status != SessionStatus.InProgress) return Conflict("Trening jest już zakończony.");
if (session.Exercises.Any(x => x.Id != item.Id && x.ExerciseId == request.ExerciseId))
    return Conflict("To ćwiczenie jest już w tej sesji.");

if (item.Sets.Count == 0)
{
    item.ReplaceExercise(exercise);
    await db.SaveChangesAsync();
    return ExerciseResponse(item);
}

foreach (var later in session.Exercises.Where(x => x.Order > item.Order))
    later.ChangeOrder(later.Order + 1);

var remainingSets = Math.Max(1, item.PlannedSets - item.Sets.Count);
var replacement = new WorkoutExercise(exercise, item.Order + 1, remainingSets,
    item.MinReps, item.MaxReps, item.TargetRir, item.RestSeconds);
session.Exercises.Add(replacement);
await db.SaveChangesAsync();
return ExerciseResponse(replacement);
```

- [ ] **Step 4: Expose a prominent searchable swap flow**

Move `Zamień ćwiczenie` next to the current exercise heading. Keep the existing options section for adding an extra exercise and notes. Filter out all exercise IDs already present in the session, show the number of remaining sets, disable double submission, then select the returned replacement after reload and load its history.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Replacement
dotnet build FormaAI.sln
```

Commit:

```powershell
git add src/FormaAI.Domain/Training/TrainingModels.cs src/FormaAI.Api/Controllers/TrainingController.cs src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs
git commit -m "Dodać bezpieczną wymianę ćwiczenia w sesji"
```

### Task 3: Skalowanie zapisanego i proponowanego posiłku do kalorii

**Files:**
- Create: `src/FormaAI.Application/Nutrition/MealCalorieScaler.cs`
- Create: `tests/FormaAI.Application.Tests/MealCalorieScalerTests.cs`
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`

**Interfaces:**
- Produces: `MealCalorieScaler.ScaleAmounts(IReadOnlyList<decimal> amounts, decimal currentCalories, decimal targetCalories)`.
- Consumes: private meal item forms in both pages.

- [ ] **Step 1: Write failing calculation tests**

```csharp
[Theory]
[InlineData(500, 750, 100, 150)]
[InlineData(800, 400, 100, 50)]
public void ScaleAmountsPreservesProportions(decimal current, decimal target, decimal amount, decimal expected)
{
    var result = MealCalorieScaler.ScaleAmounts([amount], current, target);
    Assert.Equal(expected, result[0]);
}

[Theory]
[InlineData(0, 500)]
[InlineData(500, 0)]
public void ScaleAmountsRejectsInvalidCalories(decimal current, decimal target) =>
    Assert.Throws<ArgumentOutOfRangeException>(() => MealCalorieScaler.ScaleAmounts([100], current, target));
```

Also test an empty item list and a small ingredient that must not round to zero.

- [ ] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter MealCalorieScaler
```

Expected: compile failure because the scaler does not exist.

- [ ] **Step 3: Implement the scaler**

```csharp
public static class MealCalorieScaler
{
    public static IReadOnlyList<decimal> ScaleAmounts(
        IReadOnlyList<decimal> amounts, decimal currentCalories, decimal targetCalories)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        if (amounts.Count == 0) throw new ArgumentException("Posiłek nie ma składników.", nameof(amounts));
        if (currentCalories <= 0) throw new ArgumentOutOfRangeException(nameof(currentCalories));
        if (targetCalories <= 0) throw new ArgumentOutOfRangeException(nameof(targetCalories));
        var factor = targetCalories / currentCalories;
        return amounts.Select(x => Math.Max(0.1m, decimal.Round(x * factor, 1, MidpointRounding.AwayFromZero))).ToList();
    }
}
```

- [ ] **Step 4: Add the shared UX to both forms**

For saved meal edit and AI draft, show a numeric `Docelowe kalorie` field plus `Dopasuj porcje`. Compute current calories from live item values, call the scaler, assign returned gram amounts, and show an error snackbar without partial mutation when validation fails. Reset the target when a different meal or AI draft is loaded.

Make the AI draft visually close the add-meal surface and keep only the global safe-area spacing below the card:

```css
.meal-add-shell:has(.ai-meal-draft) { overflow: hidden; padding-bottom: 0; }
.meal-add-shell:has(.ai-meal-draft) .ai-meal-draft { border-radius: 16px 16px 0 0; margin-bottom: 0; }
```

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter MealCalorieScaler
dotnet build FormaAI.sln
```

Commit:

```powershell
git add src/FormaAI.Application/Nutrition/MealCalorieScaler.cs tests/FormaAI.Application.Tests/MealCalorieScalerTests.cs src/FormaAI.Web/Pages/Food.razor src/FormaAI.Web/Pages/AddMeal.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Dodać dopasowanie posiłku do kalorii"
```

### Task 4: Analiza wielu zdjęć jednego posiłku

**Files:**
- Modify: `src/FormaAI.Application/Assistant/AssistantModel.cs`
- Modify: `src/FormaAI.Infrastructure/External/GeminiAssistantModel.cs`
- Modify: `src/FormaAI.Api/Controllers/NutritionController.cs`
- Modify: `src/FormaAI.Web/Services/NutritionClient.cs`
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`

**Interfaces:**
- Produces: `MealImage(byte[] Content, string MimeType)`.
- Changes: `IAssistantModel.AnalyzeMealPhoto(IReadOnlyList<MealImage> images, CancellationToken)`.
- Changes: multipart field `photos` accepts 1–5 files.

- [ ] **Step 1: Write failing multipart tests**

Test successful forwarding of two images, rejection of zero files, six files, an unsupported MIME type, and a file over 12 MB. The fake model must capture `LastMealImages` so the success test asserts a count of two.

- [ ] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter MealPhoto
```

Expected: the two-file request fails because the endpoint accepts only `photo`.

- [ ] **Step 3: Extend model adapters and API**

Use:

```csharp
public sealed record MealImage(byte[] Content, string MimeType);
Task<MealPhotoDraftResponse> AnalyzeMealPhoto(IReadOnlyList<MealImage> images, CancellationToken cancellationToken);
```

Gemini request parts contain the prompt followed by one `inlineData` part per image. OpenAI-compatible request content contains the prompt followed by one `image_url` item per image. Validate the whole batch before reading and calling the model. Set `[RequestSizeLimit(60 * 1024 * 1024)]`.

- [ ] **Step 4: Add separate camera and gallery controls**

Keep a single camera input with `capture="environment"`. Add a gallery input with `multiple` and no `capture`, call `args.GetMultipleFiles(5)`, and submit the selected list through `NutritionClient.AnalyzeMealPhotos(IReadOnlyList<IBrowserFile>)`.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter MealPhoto
dotnet build FormaAI.sln
```

Commit:

```powershell
git add src/FormaAI.Application/Assistant/AssistantModel.cs src/FormaAI.Infrastructure/External/GeminiAssistantModel.cs src/FormaAI.Api/Controllers/NutritionController.cs src/FormaAI.Web/Services/NutritionClient.cs src/FormaAI.Web/Pages/Food.razor src/FormaAI.Web/Pages/AddMeal.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs
git commit -m "Dodać analizę wielu zdjęć posiłku"
```

### Task 5: Grupowe dodawanie zdjęć progresu

**Files:**
- Modify: `src/FormaAI.Web/Pages/ProgressPhotos.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`

**Interfaces:**
- Consumes: existing `CoachingClient.AddPhoto(IBrowserFile, DateOnly, ProgressPhotoPose)`.
- Produces: UI batch of 1–5 `IBrowserFile` with per-file success/failure summary.

- [ ] **Step 1: Change the form state to a list**

Replace `_file` with:

```csharp
private IReadOnlyList<IBrowserFile> _files = [];
private void SelectFiles(InputFileChangeEventArgs args) => _files = args.GetMultipleFiles(5);
```

Use `MudFileUpload<IReadOnlyList<IBrowserFile>>` if supported by the installed MudBlazor version; otherwise use Blazor `InputFile multiple` inside the existing styled activator.

- [ ] **Step 2: Implement partial-success upload**

```csharp
var saved = 0;
var failed = new List<string>();
foreach (var file in _files)
{
    try { await Coaching.AddPhoto(file, date, _pose); saved++; }
    catch (Exception) { failed.Add(file.Name); }
}
await Reload();
```

Show a success snackbar for `saved` and a warning naming rejected files. Clear the selection only after the loop.

- [ ] **Step 3: Build and commit**

Run:

```powershell
dotnet build FormaAI.sln
```

Manually verify a mixed valid/oversized batch using browser devtools file selection.

Commit:

```powershell
git add src/FormaAI.Web/Pages/ProgressPhotos.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Dodać grupowe zdjęcia progresu"
```

### Task 6: Propozycje dań do brakującego makro

**Files:**
- Modify: `src/FormaAI.Api/Controllers/AssistantController.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`

**Interfaces:**
- Changes tool result `get_today_nutrition_summary` to `{ date, target, consumed, remaining, overBy }`.
- Keeps draft confirmation behavior unchanged.

- [ ] **Step 1: Write failing tool-result tests**

Queue the fake model to call `get_today_nutrition_summary`, then inspect the tool result passed into the next model request:

```csharp
Assert.Equal(900m, result.GetProperty("remaining").GetProperty("calories").GetDecimal());
Assert.Equal(0m, result.GetProperty("overBy").GetProperty("protein").GetDecimal());
```

Add a case with exceeded fat and a case without a target.

- [ ] **Step 2: Verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter AssistantNutritionSummary
```

Expected: `remaining` and `overBy` are absent.

- [ ] **Step 3: Calculate signed remaining and positive overage**

Serialize:

```csharp
var remaining = target is null ? null : new Macro(
    target.CaloriesKcal - consumed.CaloriesKcal,
    target.ProteinG - consumed.ProteinG,
    target.FatG - consumed.FatG,
    target.CarbohydratesG - consumed.CarbohydratesG);
var overBy = remaining is null ? null : new Macro(
    Math.Max(0, -remaining.CaloriesKcal),
    Math.Max(0, -remaining.ProteinG),
    Math.Max(0, -remaining.FatG),
    Math.Max(0, -remaining.CarbohydratesG));
```

Use anonymous response properties with the existing JSON options.

- [ ] **Step 4: Harden the assistant instruction**

Add a dedicated instruction block requiring the sequence summary → preferences/allergies → product/recipe/pantry search → `calculate_meal`, with at most three concrete dishes and the response shape `brakuje / danie / po zjedzeniu zostanie`. Explicitly prohibit creating a draft unless requested and prohibit guessing when the target is missing.

- [ ] **Step 5: Verify and commit**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Assistant
dotnet build FormaAI.sln
```

Commit:

```powershell
git add src/FormaAI.Api/Controllers/AssistantController.cs tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs
git commit -m "Dopasować propozycje dań do brakującego makro"
```

### Task 7: Pełna weryfikacja, scalenie i wersja testowa

**Files:**
- Verify: all changed files and commit history.
- Do not modify secrets or tracked configuration.

- [ ] **Step 1: Run the complete verification gate**

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
git diff main...HEAD --check
git status --short
```

Expected: build exit `0`, all tests pass, diff check clean, working tree clean.

- [ ] **Step 2: Manually verify the requested flows**

Start the app from the feature worktree with LocalDB and the documented demo admin email. Verify plan list, exercise details, both replacement cases, saved/AI meal scaling, multi-image meal analysis, multi-photo progress upload, white-space removal, and assistant macro suggestion.

- [ ] **Step 3: Merge to main without discarding user changes**

Before merge, inspect the main worktree. Preserve unrelated modified/untracked files. From the main checkout run:

```powershell
git merge --ff-only poprawki/trening-jedzenie-zdjecia-asystent
```

If fast-forward is impossible, stop and inspect history; do not reset or overwrite.

- [ ] **Step 4: Re-run verification on main**

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

- [ ] **Step 5: Launch the version and tunnel**

Start LocalDB. Launch the API on `http://0.0.0.0:5082` with session-only `ConnectionStrings__FormaAI` and `Admin__Email=demo.admin@formaai.pl`, then start:

```powershell
cloudflared tunnel --url http://127.0.0.1:5082 --no-autoupdate
```

Run both as hidden background processes with logs under `App_Data`, capture exact PIDs, and extract the generated `https://*.trycloudflare.com` URL.

- [ ] **Step 6: Verify local, public and authenticated health**

Check local and public `/health/live`. Log in through the documented account endpoint with the demo credentials and perform one authenticated read such as `GET api/v1/training-plans`. Report the URL, branch/commit, process IDs, test totals, and that Quick Tunnel is temporary.
