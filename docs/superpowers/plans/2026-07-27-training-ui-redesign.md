# Training UI Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Przebudować moduł Trening zgodnie z zaakceptowanymi referencjami, naprawić treningowy flow AI, dodać zapis wykonanego treningu i edytowalne superserie podczas sesji oraz wyrównać opis posiłku do lewej.

**Architecture:** Zachowujemy istniejące encje planów i sesji, rozszerzając je jedynie o jawny zapis cardio oraz operację zmiany superserii aktywnej sesji. `Training.razor` pozostaje koordynatorem danych, a trzy główne sekcje modułu zostają wydzielone do komponentów prezentacyjnych; `/workout/new`, `/workout/{id}`, szczegóły ćwiczenia i zamiana pozostają dedykowanymi widokami. AI zawsze tworzy edytowalny szkic, który użytkownik jawnie rozpoczyna albo zapisuje jako wykonany.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, Entity Framework Core, SQL Server, xUnit.

## Global Constraints

- Globalna nawigacja aplikacji i jej zakładki pozostają bez zmian.
- Wewnętrzny moduł `Trening` ma dokładnie trzy podstawowe sekcje: `Trening`, `Plany`, `Ćwiczenia`.
- Zachowujemy obecną paletę, kroje pisma i styl FormaAI; referencje wyznaczają hierarchię oraz ergonomię.
- Układ jest mobile-first i działa również na desktopie.
- Minimalny cel dotykowy ma `44 × 44 px`.
- AI niczego nie zapisuje przed jawnym zatwierdzeniem.
- Pozycje superserii przechowują osobne kg, powtórzenia i RIR dla każdego ćwiczenia oraz rundy.
- Każdy zamknięty moduł kończy się osobnym commitem z polskim opisem.
- Po zakończeniu uruchamiamy `dotnet build FormaAI.sln` i `dotnet test FormaAI.sln --no-build`.

---

### Task 1: Treningowy szkic AI i poprawne komunikaty

**Files:**
- Modify: `src/FormaAI.Api/Services/Ai/AiToolDefinitions.cs`
- Modify: `src/FormaAI.Api/Services/Ai/AiSystemPrompt.cs`
- Modify: `src/FormaAI.Web/Pages/NewWorkout.razor`
- Modify: `src/FormaAI.Application/Assistant/CompletedWorkoutDraftForm.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`
- Test: `tests/FormaAI.Application.Tests/CompletedWorkoutDraftFormTests.cs`

**Interfaces:**
- Consumes: `AssistantCompletedWorkoutDraftResponse`, `CompletedWorkoutDraftForm.From(AssistantCompletedWorkoutDraftResponse)`.
- Produces: treningowy szkic bez tekstów żywieniowych oraz dwie jawne akcje `ConfirmCompletedWorkout(Guid)` i `StartWorkout(Guid)`.

- [ ] **Step 1: Write failing prompt and source-flow tests**

Add assertions verifying the workout prompt forces `create_completed_workout_draft`, rejects nutrition language, and exposes both final actions:

```csharp
[Fact]
public void WorkoutPromptUsesOnlyWorkoutDraftTool()
{
    var prompt = AiSystemPrompt.ForWorkoutEntry;
    Assert.Contains("create_completed_workout_draft", prompt);
    Assert.Contains("serie", prompt, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("produkt", prompt, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("porcj", prompt, StringComparison.OrdinalIgnoreCase);
}

[Fact]
public void NewWorkoutOffersSaveAndStartAfterAiReview()
{
    var source = File.ReadAllText(WebSource("Pages", "NewWorkout.razor"));
    Assert.Contains("Zapisz jako wykonany", source);
    Assert.Contains("Rozpocznij trening", source);
    Assert.Contains("ConfirmCompletedWorkout", source);
    Assert.Contains("StartWorkout", source);
}
```

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "WorkoutPrompt|NewWorkoutOffers" --nologo
```

Expected: FAIL because the dedicated prompt and completed action are missing.

- [ ] **Step 3: Implement the dedicated AI contract**

Expose one exact instruction used by `/workout/new`:

```csharp
public static string ForWorkoutEntry =>
    """
    Zamień opis użytkownika na edytowalny szkic treningu.
    Zawsze użyj create_completed_workout_draft.
    Rozpoznaj ćwiczenia, serie, ciężary, powtórzenia, RIR i bloki cardio.
    Nie używaj narzędzi ani komunikatów dotyczących jedzenia, produktów lub porcji.
    Nie zapisuj i nie rozpoczynaj treningu bez osobnego zatwierdzenia użytkownika.
    """;
```

In `NewWorkout.razor`, use that instruction, keep the draft visible on failure, and render:

```razor
<MudButton OnClick="SaveAiWorkoutAsCompleted">Zapisz jako wykonany</MudButton>
<MudButton OnClick="StartAiWorkout">Rozpocznij trening</MudButton>
```

with:

```csharp
private async Task SaveAiWorkoutAsCompleted()
{
    if (_aiDraft is null || AiValidationErrors.Count > 0 || _saving) return;
    _saving = true;
    try
    {
        await AssistantApi.UpdateCompletedWorkout(_aiDraft.Id, _aiDraft.ToRequest());
        var session = await AssistantApi.ConfirmCompletedWorkout(_aiDraft.Id);
        Navigation.NavigateTo($"/workout/{session.Id}", replace: true);
    }
    catch (HttpRequestException exception)
    {
        Snackbar.Add($"Nie udało się zapisać treningu: {exception.Message}", Severity.Error);
    }
    finally
    {
        _saving = false;
    }
}
```

- [ ] **Step 4: Run focused tests**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "WorkoutPrompt|NewWorkoutOffers|CompletedWorkoutDraftForm" --nologo
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Api/Services/Ai src/FormaAI.Web/Pages/NewWorkout.razor src/FormaAI.Application/Assistant tests/FormaAI.Application.Tests tests/FormaAI.Api.IntegrationTests
git commit -m "Naprawić treningowy szkic AI"
```

---

### Task 2: Cardio w szkicu i zapisanym treningu

**Files:**
- Modify: `src/FormaAI.Contracts/Assistant/AssistantContracts.cs`
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/FormaAI.Infrastructure/Persistence/Migrations/<timestamp>_AddWorkoutCardioEntries.cs`
- Modify: `src/FormaAI.Api/Controllers/AssistantController.cs`
- Modify: `src/FormaAI.Application/Assistant/CompletedWorkoutDraftForm.cs`
- Test: `tests/FormaAI.Domain.Tests/WorkoutCardioEntryTests.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`

**Interfaces:**
- Produces: `AssistantWorkoutCardioDraft`, `WorkoutCardioEntryResponse`, `WorkoutSession.CardioEntries`.
- Consumers: AI review in Task 6 and workout summary in Task 7.

- [ ] **Step 1: Write failing domain and API tests**

```csharp
[Fact]
public void WorkoutCardioEntryAcceptsDurationDistanceAndPace()
{
    var entry = new WorkoutCardioEntry("Bieg na bieżni", 2400, 5m, 8m);
    Assert.Equal(2400, entry.DurationSeconds);
    Assert.Equal(5m, entry.DistanceKm);
    Assert.Equal(8m, entry.SpeedKph);
}

[Fact]
public async Task CompletedWorkoutDraftPersistsCardioAlongsideStrengthSets()
{
    var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    await Register(client, "assistant-cardio-workout@example.test");
    var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(
        client,
        HttpMethod.Post,
        "api/v1/exercises",
        new("Wyciskanie z cardio", MuscleGroup.Chest, Equipment.Barbell, false));
    var localDate = DateOnly.FromDateTime(DateTime.UtcNow);

    _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall(
        "create_completed_workout_draft",
        JsonSerializer.SerializeToElement(new
        {
            name = "Bieg i klatka",
            localDate,
            cardio = new[] { new { name = "Bieg na bieżni", durationSeconds = 2400, distanceKm = 5m, speedKph = 8m, averageHeartRate = (int?)null } },
            exercises = new[] { new { exerciseId = exercise.Id, exerciseName = exercise.Name, sets = new[] { new { weightKg = 40m, repetitions = 10, rir = (decimal?)2 } } } }
        })), 20, 8));
    _factory.Model.Enqueue(new AssistantModelTurn("Sprawdź trening przed zapisem.", null, 30, 12));

    var draft = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(
        client,
        HttpMethod.Post,
        "api/v1/assistant/messages",
        new(null, "Biegałem 40 minut i zrobiłem wyciskanie 40 kg na 10", localDate));
    var saved = await Send<object, WorkoutSessionResponse>(
        client,
        HttpMethod.Post,
        $"api/v1/assistant/actions/{draft.CompletedWorkoutDraft!.Id}/confirm",
        new { });

    Assert.Single(saved.CardioEntries!);
    Assert.Equal(2400, saved.CardioEntries![0].DurationSeconds);
    Assert.Single(saved.Exercises);
}
```

- [ ] **Step 2: Run RED tests**

Run:

```powershell
dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter WorkoutCardioEntry --nologo
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter CompletedWorkoutDraftPersistsCardio --nologo
```

Expected: compilation failure because cardio types do not exist.

- [ ] **Step 3: Add exact contracts and domain model**

```csharp
public sealed record AssistantWorkoutCardioDraft(
    [Required, MaxLength(150)] string Name,
    [Range(1, 86400)] int DurationSeconds,
    [Range(0, 1000)] decimal? DistanceKm,
    [Range(0, 100)] decimal? SpeedKph,
    [Range(0, 250)] int? AverageHeartRate);

public sealed record WorkoutCardioEntryResponse(
    Guid Id,
    string Name,
    int DurationSeconds,
    decimal? DistanceKm,
    decimal? SpeedKph,
    int? AverageHeartRate);
```

Extend completed draft payload/request/response with `IReadOnlyList<AssistantWorkoutCardioDraft> Cardio`, and extend `WorkoutSessionResponse` with `IReadOnlyList<WorkoutCardioEntryResponse>? CardioEntries = null`.

Add:

```csharp
public sealed class WorkoutCardioEntry
{
    private WorkoutCardioEntry() { }

    public WorkoutCardioEntry(string name, int durationSeconds, decimal? distanceKm, decimal? speedKph, int? averageHeartRate = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Nazwa cardio jest wymagana.") : name.Trim();
        DurationSeconds = durationSeconds is > 0 and <= 86400 ? durationSeconds : throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        DistanceKm = distanceKm;
        SpeedKph = speedKph;
        AverageHeartRate = averageHeartRate;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkoutSessionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int DurationSeconds { get; private set; }
    public decimal? DistanceKm { get; private set; }
    public decimal? SpeedKph { get; private set; }
    public int? AverageHeartRate { get; private set; }
}
```

- [ ] **Step 4: Map EF and persist on both confirm and response paths**

Configure the owned relationship as a normal entity with cascade delete and indexes on `WorkoutSessionId`. Generate the migration:

```powershell
dotnet ef migrations add AddWorkoutCardioEntries --project src/FormaAI.Infrastructure --startup-project src/FormaAI.Api --output-dir Persistence/Migrations
```

In `ConfirmCompletedWorkout`, add every validated cardio block before calling `session.Complete()`. Include cardio in the session query and response mapper.

- [ ] **Step 5: Run focused tests and commit**

```powershell
dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter WorkoutCardioEntry --nologo
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter "CompletedWorkoutDraftPersistsCardio|CompletedWorkoutDraftIsSavedOnlyAfterExplicitConfirmation" --nologo
git add src tests
git commit -m "Dodać cardio do zapisu treningu AI"
```

Expected: PASS.

---

### Task 3: Edycja superserii w aktywnej sesji

**Files:**
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Create: `src/FormaAI.Application/Training/WorkoutSupersetEditor.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Services/TrainingClient.cs`
- Test: `tests/FormaAI.Application.Tests/WorkoutSupersetEditorTests.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Produces: `UpdateWorkoutSupersetRequest`, `WorkoutSupersetEditor.Apply(IReadOnlyList<WorkoutExercise>, UpdateWorkoutSupersetRequest)`, `TrainingClient.UpdateSuperset(Guid, UpdateWorkoutSupersetRequest)`.
- Consumes: existing `WorkoutSequence.Next(IReadOnlyList<WorkoutSequenceItem>, Guid)`.

- [ ] **Step 1: Write failing editor tests**

```csharp
[Fact]
public void ApplyBuildsOrderedGroupAndSetsRoundRestOnLastMember()
{
    var result = WorkoutSupersetEditor.Apply(
        [Exercise(firstId), Exercise(secondId)],
        new UpdateWorkoutSupersetRequest(
            [new(firstId, 1), new(secondId, 2)],
            3,
            120));

    Assert.NotNull(result[0].SupersetGroupId);
    Assert.All(result, item => Assert.Equal(result[0].SupersetGroupId, item.SupersetGroupId));
    Assert.Equal([1, 2], result.Select(x => x.SupersetPosition));
    Assert.Equal(120, result.Last().RestSeconds);
    Assert.Equal(0, result.First().IntervalSeconds);
}

[Fact]
public void ApplyRejectsSingleExerciseAndDuplicateMembers()
{
    var first = Exercise(Guid.NewGuid());
    var invalid = new UpdateWorkoutSupersetRequest(
        [new WorkoutSupersetMemberRequest(first.Id, 1)],
        3,
        90);

    Assert.Throws<ValidationException>(() => WorkoutSupersetEditor.Apply([first], invalid));
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutSupersetEditor --nologo
```

Expected: compilation failure.

- [ ] **Step 3: Add contracts and editor**

```csharp
public sealed record WorkoutSupersetMemberRequest(Guid WorkoutExerciseId, [Range(1, 20)] int Position);

public sealed record UpdateWorkoutSupersetRequest(
    [MinLength(2)] IReadOnlyList<WorkoutSupersetMemberRequest> Members,
    [Range(1, 20)] int Rounds,
    [Range(0, 3600)] int RestAfterRoundSeconds);
```

`WorkoutSupersetEditor.Apply` must validate unique members, consecutive positions and session ownership, then call a domain method:

```csharp
public void ConfigureSuperset(Guid? groupId, int? position, int plannedSets, int? restSeconds, int? intervalSeconds)
{
    SupersetGroupId = groupId;
    SupersetPosition = position;
    PlannedSets = plannedSets;
    RestSeconds = restSeconds;
    IntervalSeconds = intervalSeconds;
}
```

- [ ] **Step 4: Add authenticated endpoint**

```csharp
[HttpPut("workout-sessions/{sessionId:guid}/superset")]
public async Task<ActionResult<WorkoutSessionResponse>> UpdateSuperset(
    Guid sessionId,
    UpdateWorkoutSupersetRequest request,
    CancellationToken cancellationToken)
```

The endpoint loads the active user-owned session, rejects completed sessions, applies the editor, saves, and returns the refreshed session. Add:

```csharp
public Task<WorkoutSessionResponse> UpdateSuperset(Guid sessionId, UpdateWorkoutSupersetRequest request) =>
    Send<WorkoutSessionResponse>(HttpMethod.Put, $"api/v1/workout-sessions/{sessionId}/superset", request);
```

- [ ] **Step 5: Prove sequence timing**

Add an integration test that creates three exercises, groups two, saves a set for member one and receives an interval of zero/next member, then saves member two and receives a rest timer returning to member one for the next round.

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "WorkoutSupersetEditor|WorkoutSequence" --nologo
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Superset --nologo
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src tests
git commit -m "Dodać edycję superserii podczas sesji"
```

---

### Task 4: Własne referencje wizualne i system modułu

**Files:**
- Create: `docs/design-references/training/training-overview.png`
- Create: `docs/design-references/training/training-plan.png`
- Create: `docs/design-references/training/workout-live.png`
- Create: `docs/design-references/training/workout-swap-superset.png`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`

**Interfaces:**
- Produces: cztery samodzielne, czytelne referencje oraz zestaw klas `training-shell-*`, `training-plan-*`, `workout-live-*`.

- [ ] **Step 1: Generate four standalone references**

Use `imagegen` separately for:

1. mobile training overview with three internal tabs and today variants;
2. desktop/mobile plan day with a vertical exercise list;
3. mobile live workout with media, set grid and superset rail;
4. mobile swap/superset selection flow.

Every prompt must specify: existing FormaAI white/ink/cobalt palette, Barlow-like condensed headings, 390 px mobile viewport or 1200 px desktop viewport, no global navigation changes, no nested-card clutter, Polish labels, 44 px controls.

- [ ] **Step 2: Inspect every generated image**

Record exact extracted values in the plan execution notes:

```text
content max width
page gutters
heading sizes
row heights
media aspect ratios
border radii
separator colors
primary/secondary action hierarchy
mobile/desktop breakpoint behavior
```

- [ ] **Step 3: Add module-scoped design tokens**

Use existing global colors and add only scoped aliases:

```css
.training-shell {
    --training-blue: var(--primary);
    --training-ink: var(--ink);
    --training-line: var(--rule);
    --training-soft: var(--surface-soft);
}

.training-shell button,
.workout-mode button {
    min-height: 44px;
}

@media (prefers-reduced-motion: reduce) {
    .training-shell *,
    .workout-mode * {
        scroll-behavior: auto;
        transition-duration: .01ms !important;
    }
}
```

- [ ] **Step 4: Run CSS/source guard and commit**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutNavigationSourceTests --nologo
git add docs/design-references/training src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Ustalić kierunek wizualny treningu"
```

Expected: PASS.

---

### Task 5: Nowa struktura zakładki Trening i planów

**Files:**
- Create: `src/FormaAI.Web/Components/Training/TrainingTodayPanel.razor`
- Create: `src/FormaAI.Web/Components/Training/TrainingPlansPanel.razor`
- Create: `src/FormaAI.Web/Components/Training/ExerciseCatalogPanel.razor`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Modify: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- `TrainingTodayPanel`: `TodayWorkoutResponse? Today`, `WorkoutSessionResponse? ActiveSession`, callbacks for start variants.
- `TrainingPlansPanel`: `IReadOnlyList<TrainingPlanResponse> Plans`, selected day state, callbacks for create/edit.
- `ExerciseCatalogPanel`: `IReadOnlyList<ExerciseResponse> Exercises`, query and create/edit callbacks.

- [ ] **Step 1: Write failing source tests**

```csharp
[Fact]
public void TrainingUsesExactlyThreePrimarySections()
{
    var source = File.ReadAllText(WebSource("Pages", "Training.razor"));
    Assert.Contains("TreningTodayPanel", source);
    Assert.Contains("TrainingPlansPanel", source);
    Assert.Contains("ExerciseCatalogPanel", source);
    Assert.DoesNotContain("Nowe ćwiczenie</MudTabPanel>", source);
    Assert.DoesNotContain("Nowy plan</MudTabPanel>", source);
}

[Fact]
public void PlanExercisesRenderBelowSelectedDay()
{
    var source = File.ReadAllText(WebSource("Components", "Training", "TrainingPlansPanel.razor"));
    Assert.Contains("training-day-selector", source);
    Assert.Contains("training-plan-exercise-list", source);
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "TrainingUsesExactlyThree|PlanExercisesRender" --nologo
```

Expected: FAIL because components do not exist.

- [ ] **Step 3: Build the three-section coordinator**

`Training.razor` must render exactly:

```razor
<MudTabs @bind-ActivePanelIndex="_activeTab" Class="training-primary-tabs">
    <MudTabPanel Text="Trening">
        <TrainingTodayPanel Today="_today" ActiveSession="_activeSession"
                            OnStartFull="StartFull" OnStartShort="StartShort"
                            OnStartMinimum="StartMinimum" />
    </MudTabPanel>
    <MudTabPanel Text="Plany">
        <TrainingPlansPanel Plans="_plans" />
    </MudTabPanel>
    <MudTabPanel Text="Ćwiczenia">
        <ExerciseCatalogPanel Exercises="_exercises" />
    </MudTabPanel>
</MudTabs>
```

Move creation forms behind `Nowy plan`/`Nowe ćwiczenie` actions without adding primary tabs. Preserve all existing calls to `TrainingClient`.

- [ ] **Step 4: Implement plan list hierarchy**

`TrainingPlansPanel.razor` renders one plan header, a horizontally scrollable day selector, and one vertical list below it. Each exercise row includes media, name, muscle group, `serie × powtórzenia`, rest, superseries marker and detail link.

- [ ] **Step 5: Run focused tests and Web build**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "TrainingUsesExactlyThree|PlanExercisesRender" --nologo
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
```

Expected: PASS and 0 errors.

- [ ] **Step 6: Commit**

```powershell
git add src/FormaAI.Web tests/FormaAI.Application.Tests
git commit -m "Przebudować zakładkę treningu i plany"
```

---

### Task 6: Przygotowanie treningu i podgląd AI

**Files:**
- Modify: `src/FormaAI.Web/Pages/NewWorkout.razor`
- Create: `src/FormaAI.Web/Components/Training/AiWorkoutReview.razor`
- Create: `src/FormaAI.Web/Components/Training/ManualWorkoutBuilder.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- `AiWorkoutReview`: `CompletedWorkoutDraftForm Draft`, `EventCallback SaveCompleted`, `EventCallback StartLive`.
- `ManualWorkoutBuilder`: `QuickWorkoutDraft Draft`, exercise search and start callbacks.

- [ ] **Step 1: Write failing component tests**

```csharp
[Fact]
public void AiReviewRendersCardioExercisesAndBothApprovalActions()
{
    var source = File.ReadAllText(WebSource("Components", "Training", "AiWorkoutReview.razor"));
    Assert.Contains("ai-cardio-row", source);
    Assert.Contains("ai-strength-set-grid", source);
    Assert.Contains("Zapisz jako wykonany", source);
    Assert.Contains("Rozpocznij trening", source);
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter AiReviewRenders --nologo
```

- [ ] **Step 3: Extract focused review and manual components**

Render cardio with editable duration/distance/speed, strength with per-set kg/reps/RIR, exercise replacement and inline validation. Keep the final actions in one sticky bar:

```razor
<footer class="workout-review-actions">
    <MudButton Variant="Variant.Outlined" OnClick="SaveCompleted">Zapisz jako wykonany</MudButton>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="StartLive">Rozpocznij trening</MudButton>
</footer>
```

- [ ] **Step 4: Verify**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "AiReviewRenders|CompletedWorkoutDraftForm" --nologo
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web tests/FormaAI.Application.Tests
git commit -m "Uprościć przygotowanie treningu i podgląd AI"
```

---

### Task 7: Aktywna sesja, wybór superserii i zamiana

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Create: `src/FormaAI.Web/Components/Training/WorkoutSetGrid.razor`
- Create: `src/FormaAI.Web/Components/Training/WorkoutSupersetRail.razor`
- Create: `src/FormaAI.Web/Components/Training/WorkoutSupersetEditor.razor`
- Create: `src/FormaAI.Web/Components/Training/WorkoutExercisePicker.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- `WorkoutSetGrid`: current `WorkoutExerciseResponse`, `SetForm`, save callback.
- `WorkoutSupersetRail`: group members, current exercise, current round, select callback.
- `WorkoutSupersetEditor`: candidate exercises, order/round/rest form, confirm callback.
- `WorkoutExercisePicker`: reusable filtered list for swap and superseries.

- [ ] **Step 1: Write failing structure tests**

```csharp
[Fact]
public void LiveWorkoutUsesFocusedExerciseComponents()
{
    var source = File.ReadAllText(WebSource("Pages", "Workout.razor"));
    Assert.Contains("WorkoutSetGrid", source);
    Assert.Contains("WorkoutSupersetRail", source);
    Assert.Contains("WorkoutSupersetEditor", source);
    Assert.Contains("WorkoutExercisePicker", source);
}

[Fact]
public void SupersetEditorCapturesOrderRoundsAndRest()
{
    var source = File.ReadAllText(WebSource("Components", "Training", "WorkoutSupersetEditor.razor"));
    Assert.Contains("Liczba rund", source);
    Assert.Contains("Odpoczynek po rundzie", source);
    Assert.Contains("Połącz w superserię", source);
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "LiveWorkoutUses|SupersetEditorCaptures" --nologo
```

- [ ] **Step 3: Build the focused live layout**

Order the page as: compact header → media → exercise title/actions → timers → set grid → superseries rail → sticky primary action. Keep summary mode separate from live mode.

- [ ] **Step 4: Implement superseries editor**

Use `WorkoutExercisePicker` for multi-selection. Require at least two unique items, expose reorder controls, rounds `1..20`, rest `0..3600`, then call:

```csharp
await TrainingApi.UpdateSuperset(
    Id,
    new UpdateWorkoutSupersetRequest(
        selected.Select((id, index) => new WorkoutSupersetMemberRequest(id, index + 1)).ToList(),
        rounds,
        restSeconds));
```

After save, reload the session and select the first group member.

- [ ] **Step 5: Use the same picker for replacement**

Single-select mode keeps filters and preserves already completed sets. The confirmation bar is always visible on mobile and exposes `Anuluj` plus `Potwierdź zamianę`.

- [ ] **Step 6: Verify sequence and Web compilation**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "WorkoutSequence|LiveWorkoutUses|SupersetEditorCaptures" --nologo
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/FormaAI.Web tests/FormaAI.Application.Tests
git commit -m "Przebudować aktywną sesję i superserie"
```

---

### Task 8: Szczegóły ćwiczenia

**Files:**
- Modify: `src/FormaAI.Web/Pages/ExerciseDetails.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: `ExerciseResponse`, `ExerciseHistoryEntry`, existing progression/history endpoints.
- Produces: tabs `Historia`, `Wykres`, `Technika` and mobile-safe history tables.

- [ ] **Step 1: Write failing source test**

```csharp
[Fact]
public void ExerciseDetailsExposeHistoryChartAndTechnique()
{
    var source = File.ReadAllText(WebSource("Pages", "ExerciseDetails.razor"));
    Assert.Contains("Historia", source);
    Assert.Contains("Wykres", source);
    Assert.Contains("Technika", source);
    Assert.Contains("1RM", source);
}
```

- [ ] **Step 2: Run RED, implement and verify**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter ExerciseDetailsExpose --nologo
```

Rebuild the view with media first, metadata second and three tabs. Group history by workout date and render columns `Seria`, `Ciężar × powt.`, `1RM`. Use existing `ExerciseHistoryEntry` and calculate estimated 1RM with the existing formula used by the session.

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter ExerciseDetailsExpose --nologo
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
```

Expected: PASS.

- [ ] **Step 3: Commit**

```powershell
git add src/FormaAI.Web/Pages/ExerciseDetails.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests
git commit -m "Przebudować szczegóły ćwiczenia"
```

---

### Task 9: Wyrównanie posiłku, dostępność i końcowa responsywność

**Files:**
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Produces: lewostronny klikalny opis posiłku oraz końcowe reguły mobile/desktop.

- [ ] **Step 1: Write failing food alignment test**

```csharp
[Fact]
public void MealRowClickableCopyIsLeftAligned()
{
    var css = File.ReadAllText(WebSource("wwwroot", "css", "app.css"));
    Assert.Contains(".meal-row-link", css);
    Assert.Contains("text-align: left", css);
    Assert.Contains("justify-items: start", css);
}
```

- [ ] **Step 2: Run RED and apply the minimal food fix**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter MealRowClickableCopy --nologo
```

Add:

```css
.meal-row-link {
    align-content: center;
    justify-items: start;
    text-align: left;
    width: 100%;
}

.meal-row-link > * {
    text-align: left;
}
```

- [ ] **Step 3: Audit mobile and desktop states**

Verify widths `360`, `390`, `768`, `1024`, and `1440` for:

- three training tabs;
- horizontal day selector;
- plan exercise rows;
- AI review;
- set grid;
- superseries editor;
- replacement picker;
- exercise history;
- meal row.

No control may be smaller than `44 × 44 px`; no history/set table may overflow at `360 px`.

- [ ] **Step 4: Run focused test and commit**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter MealRowClickableCopy --nologo
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
git add src/FormaAI.Web tests/FormaAI.Application.Tests
git commit -m "Dopracować responsywność treningu i opis posiłku"
```

Expected: PASS.

---

### Task 10: Pełna weryfikacja, review i wydanie

**Files:**
- Verify: all changed files

**Interfaces:**
- Produces: czysty branch gotowy do scalenia i publikacji.

- [ ] **Step 1: Run source and whitespace checks**

```powershell
git diff main...HEAD --check
git status --short
```

Expected: no whitespace errors and only intentional files.

- [ ] **Step 2: Run the full build**

```powershell
dotnet build FormaAI.sln --nologo -v:minimal
```

Expected: exit code `0`, no compilation errors.

- [ ] **Step 3: Run the complete test suite**

```powershell
dotnet test FormaAI.sln --no-build --nologo -v:minimal
```

Expected: all Domain, Application and API integration tests pass.

- [ ] **Step 4: Perform visual and accessibility review**

Check keyboard focus, `aria-current`/`aria-pressed`, reduced motion, sticky action overlap, empty/loading/error states, text wrapping and contrast. Address every P1/P2 finding and repeat Steps 1–3 after material fixes.

- [ ] **Step 5: Merge and publish after verification**

Merge `feature/training-ui-redesign` into `main`, rerun build/tests on the merge result, push `main`, restart the API, then verify:

```text
GET /health = 200
GET /training = 200
GET /workout/new = 200
authenticated GET /api/account/me = 200
```

Preserve unrelated user files in the main worktree.
