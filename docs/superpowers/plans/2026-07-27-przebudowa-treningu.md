# Przebudowa treningu Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Przebudować planowanie i prowadzenie treningu zgodnie z zaakceptowanymi referencjami, dodając superserie, timery oraz zatwierdzany szkic wykonanego treningu z AI.

**Architecture:** Model treningowy przechowuje grupę superserii i interwał zarówno w planie, jak i migawce sesji. API zachowuje obecne endpointy, rozszerza kontrakty i dodaje bezpieczny workflow szkicu AI. Interfejs jest dzielony na małe komponenty Blazor, ale zachowuje trasy i klientów FormaAI.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, EF Core, SQL Server, xUnit.

## Global Constraints

- Kolorystyka i system Forma Signal pozostają bez zmian.
- Widoki treningowe odwzorowują kompozycję dostarczonych ekranów.
- Trzy zakładki dni są widoczne, a ćwiczenia wybranego dnia tworzą listę pełnej szerokości.
- AI nigdy nie zapisuje treningu bez jawnego zatwierdzenia.
- Cele dotykowe mają minimum 44 × 44 px, focus jest widoczny, a ruch respektuje `prefers-reduced-motion`.
- UI używa `image-to-code`, `frontend-design`, `impeccable`, `emil-design-eng` i właściwych reguł Good Taste.

---

### Task 1: Model superserii

**Files:**
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Infrastructure/Persistence/AppDbContext.cs`
- Create: `tests/FormaAI.Domain.Tests/TrainingSupersetTests.cs`

**Interfaces:**
- Produces: `SupersetGroupId`, `SupersetPosition`, `IntervalSeconds` on `PlannedExercise` and `WorkoutExercise`.
- Produces: `ConfigureSuperset(Guid? groupId, int? position, int? intervalSeconds, int? restSeconds)`.

- [ ] **Step 1: Write failing domain tests**

```csharp
[Fact]
public void Planned_exercise_accepts_valid_superset_settings()
{
    var item = new PlannedExercise(Guid.NewGuid(), 1, 3, 8, 12, 2, 90, Guid.NewGuid(), 1, 15);
    Assert.NotNull(item.SupersetGroupId);
    Assert.Equal(1, item.SupersetPosition);
    Assert.Equal(15, item.IntervalSeconds);
}

[Theory]
[InlineData(0)]
[InlineData(3601)]
public void Interval_outside_range_is_rejected(int seconds) =>
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        new PlannedExercise(Guid.NewGuid(), 1, 3, 8, 12, 2, 90, Guid.NewGuid(), 1, seconds));
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter TrainingSupersetTests`
Expected: FAIL because the new constructor and properties do not exist.

- [ ] **Step 3: Implement model and mappings**

```csharp
public Guid? SupersetGroupId { get; private set; }
public int? SupersetPosition { get; private set; }
public int? IntervalSeconds { get; private set; }

private static void ValidateTiming(Guid? groupId, int? position, int? intervalSeconds)
{
    if (intervalSeconds is < 0 or > 3600) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
    if (groupId is null && (position is not null || intervalSeconds is not null))
        throw new ArgumentException("Interwał i pozycja wymagają grupy superserii.");
    if (groupId is not null && position is null or < 1)
        throw new ArgumentOutOfRangeException(nameof(position));
}
```

Copy the values from `PlannedExercise` in `WorkoutExercise(PlannedExercise planned, Exercise exercise)`.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter TrainingSupersetTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Domain/Training/TrainingModels.cs src/FormaAI.Contracts/Training/TrainingContracts.cs src/FormaAI.Infrastructure/Persistence/AppDbContext.cs tests/FormaAI.Domain.Tests/TrainingSupersetTests.cs
git commit -m "Dodać model superserii i interwałów"
```

### Task 2: Migracja i API superserii

**Files:**
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Create: `src/FormaAI.Infrastructure/Persistence/Migrations/<timestamp>_AddTrainingSupersets.cs`
- Modify: `src/FormaAI.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Consumes: rozszerzone `PlannedExerciseRequest` i `WorkoutExerciseResponse`.
- Produces: zapis/odczyt grup superserii w planach i sesjach.

- [ ] **Step 1: Add failing API tests**

```csharp
[Fact]
public async Task Plan_and_session_preserve_superset_timing()
{
    var group = Guid.NewGuid();
    var plan = await SavePlan([
        new PlannedExerciseRequest(_benchId, 3, 8, 10, 2, 120, group, 1, 10),
        new PlannedExerciseRequest(_rowId, 3, 8, 10, 2, 120, group, 2, 10)
    ]);
    var session = await Start(plan.Days.Single().Id);
    Assert.All(session.Exercises, x => Assert.Equal(group, x.SupersetGroupId));
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Plan_and_session_preserve_superset_timing`
Expected: FAIL because contracts and mapping omit supersets.

- [ ] **Step 3: Map the fields and generate migration**

Update `BuildDays`, `PlannedResponses`, `ExerciseResponse` and `SessionResponse` to pass `SupersetGroupId`, `SupersetPosition`, and `IntervalSeconds`.

Run: `dotnet ef migrations add AddTrainingSupersets --project src/FormaAI.Infrastructure --startup-project src/FormaAI.Api`
Expected: migration adds nullable columns to `PlannedExercises` and `WorkoutExercises`.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Plan_and_session_preserve_superset_timing`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Api/Controllers/TrainingController.cs src/FormaAI.Infrastructure/Persistence/Migrations tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs
git commit -m "Zapisywać superserie w planach i sesjach"
```

### Task 3: Silnik kolejności i timerów

**Files:**
- Create: `src/FormaAI.Application/Training/WorkoutSequence.cs`
- Create: `tests/FormaAI.Application.Tests/WorkoutSequenceTests.cs`

**Interfaces:**
- Produces: `WorkoutStep Next(IReadOnlyList<WorkoutExerciseState> exercises, Guid completedExerciseId)`.
- Produces: `WorkoutStep(Guid NextExerciseId, TimerKind Timer, int Seconds)`.

- [ ] **Step 1: Write failing sequence tests**

```csharp
[Fact]
public void Middle_of_superset_uses_interval_and_next_group_member()
{
    var group = Guid.NewGuid();
    var first = new WorkoutExerciseState(Guid.NewGuid(), 1, 3, 1, group, 1, 12, 90);
    var second = new WorkoutExerciseState(Guid.NewGuid(), 2, 3, 0, group, 2, 12, 90);
    Assert.Equal(new WorkoutStep(second.Id, TimerKind.Interval, 12), WorkoutSequence.Next([first, second], first.Id));
}

[Fact]
public void End_of_superset_round_uses_group_rest_and_returns_first_incomplete_member()
{
    var group = Guid.NewGuid();
    var first = new WorkoutExerciseState(Guid.NewGuid(), 1, 3, 1, group, 1, 12, 90);
    var second = new WorkoutExerciseState(Guid.NewGuid(), 2, 3, 1, group, 2, 12, 90);
    Assert.Equal(new WorkoutStep(first.Id, TimerKind.Rest, 90), WorkoutSequence.Next([first, second], second.Id));
}

[Fact]
public void Standalone_exercise_uses_its_rest()
{
    var first = new WorkoutExerciseState(Guid.NewGuid(), 1, 3, 1, null, null, null, 75);
    Assert.Equal(new WorkoutStep(first.Id, TimerKind.Rest, 75), WorkoutSequence.Next([first], first.Id));
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutSequenceTests`
Expected: FAIL because `WorkoutSequence` does not exist.

- [ ] **Step 3: Implement deterministic sequence**

```csharp
public enum TimerKind { None, Interval, Rest }
public sealed record WorkoutExerciseState(Guid Id, int Order, int PlannedSets, int CompletedSets, Guid? GroupId, int? GroupPosition, int? IntervalSeconds, int? RestSeconds);
public sealed record WorkoutStep(Guid? NextExerciseId, TimerKind Timer, int Seconds);
```

Sort by `Order`, move inside the group while members have an unfinished round, otherwise return the next incomplete exercise and the group rest.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutSequenceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Application/Training/WorkoutSequence.cs tests/FormaAI.Application.Tests/WorkoutSequenceTests.cs
git commit -m "Dodać kolejność rund superserii"
```

### Task 4: Szkic wykonanego treningu z AI

**Files:**
- Modify: `src/FormaAI.Domain/Assistant/AssistantModels.cs`
- Modify: `src/FormaAI.Contracts/Assistant/AssistantContracts.cs`
- Modify: `src/FormaAI.Api/Controllers/AssistantController.cs`
- Modify: `src/FormaAI.Web/Services/AssistantClient.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`

**Interfaces:**
- Produces: `AssistantCompletedWorkoutDraftPayload`.
- Produces: `AssistantCompletedWorkoutDraftResponse`.
- Produces: narzędzie `create_completed_workout_draft`.
- Produces: zatwierdzenie szkicu tworzące `WorkoutSession` ze statusem `Completed`.

- [ ] **Step 1: Add failing draft and confirmation tests**

```csharp
[Fact]
public async Task Workout_draft_does_not_create_session_before_confirmation()
{
    var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    await Register(client, "assistant-workout-draft@example.test");
    var exercise = await Send<SaveExerciseRequest, ExerciseResponse>(client, HttpMethod.Post, "api/v1/exercises",
        new("Przysiad", MuscleGroup.Quadriceps, Equipment.Barbell, false));
    _factory.Model.Enqueue(new AssistantModelTurn(null, new AssistantToolCall("create_completed_workout_draft",
        JsonSerializer.SerializeToElement(new { name = "Nogi", exercises = new[] { new { exerciseId = exercise.Id, exerciseName = exercise.Name, sets = new[] { new { weightKg = 80m, repetitions = 8, rir = (decimal?)2 } } } } })), 10, 5));
    _factory.Model.Enqueue(new AssistantModelTurn("Sprawdź szkic.", null, 10, 5));
    var response = await Send<SendAssistantMessageRequest, AssistantMessageResponse>(client, HttpMethod.Post, "api/v1/assistant/messages",
        new(null, "Zrobiłem przysiad 80 kg x 8", DateOnly.FromDateTime(DateTime.UtcNow)));
    Assert.NotNull(response.CompletedWorkoutDraft);
    Assert.Null(await client.GetFromJsonAsync<WorkoutSessionResponse?>("api/v1/workout-sessions/active"));
}

[Fact]
public async Task Confirmed_workout_draft_creates_exactly_one_completed_session()
{
    var draft = await CreateWorkoutDraft();
    var first = await Send<object, WorkoutSessionResponse>(_client, HttpMethod.Post, $"api/v1/assistant/actions/{draft.Id}/confirm", new { });
    var second = await Send<object, WorkoutSessionResponse>(_client, HttpMethod.Post, $"api/v1/assistant/actions/{draft.Id}/confirm", new { });
    Assert.Equal(first.Id, second.Id);
    Assert.Equal(SessionStatus.Completed, first.Status);
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Workout_draft`
Expected: FAIL because the action type and tool do not exist.

- [ ] **Step 3: Implement draft payload and confirmation**

```csharp
public sealed record AssistantWorkoutSetDraft(decimal WeightKg, int Repetitions, decimal? Rir);
public sealed record AssistantWorkoutExerciseDraft(Guid? ExerciseId, string ExerciseName, IReadOnlyList<AssistantWorkoutSetDraft> Sets, bool NeedsReview);
public sealed record AssistantCompletedWorkoutDraftPayload(DateOnly LocalDate, string Name, IReadOnlyList<AssistantWorkoutExerciseDraft> Exercises);
```

Serialize the payload in `AssistantActionDraft`; on confirmation validate ownership, resolve every `ExerciseId`, create one completed session, mark draft confirmed in the same transaction, and return the session ID.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Workout_draft`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Domain/Assistant/AssistantModels.cs src/FormaAI.Contracts/Assistant/AssistantContracts.cs src/FormaAI.Api/Controllers/AssistantController.cs src/FormaAI.Web/Services/AssistantClient.cs tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs
git commit -m "Dodać zatwierdzany szkic wykonanego treningu"
```

### Task 5: Referencje wizualne i kontrakt ekranu

**Files:**
- Create: `docs/design/training-plan-reference.png`
- Create: `docs/design/workout-session-reference.png`
- Create: `docs/design/exercise-swap-reference.png`
- Create: `docs/design/exercise-details-reference.png`
- Create: `docs/design/training-ui-analysis.md`

**Interfaces:**
- Produces: zatwierdzone obrazy i analizę układu używane przez Tasks 6-8.

- [ ] **Step 1: Generate four standalone references**

Use `image-to-code` and the image generator. Each image must show one screen, preserve Forma Signal colors, and reproduce the supplied hierarchy.

- [ ] **Step 2: Analyze each reference**

Record exact hierarchy, media ratio, gutters, typography roles, controls, touch targets, separators, sticky actions and mobile/desktop differences in `training-ui-analysis.md`.

- [ ] **Step 3: Validate direction**

Apply `frontend-design`, `impeccable`, `emil-design-eng` and the applicable Good Taste checks. Reject nested cards, decorative motion and generic MudBlazor composition.

- [ ] **Step 4: Commit**

```powershell
git add docs/design
git commit -m "Ustalić referencje interfejsu treningu"
```

### Task 6: Zakładki planu i kreator superserii

**Files:**
- Create: `src/FormaAI.Web/Components/Training/PlanDayTabs.razor`
- Create: `src/FormaAI.Web/Components/Training/SupersetEditor.razor`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Modify: `src/FormaAI.Web/Services/TrainingClient.cs`

**Interfaces:**
- Consumes: superserie z Tasks 1-2 and `training-ui-analysis.md`.
- Produces: wariant B i edycję grup superserii.

- [ ] **Step 1: Extract state logic into testable helpers**

```csharp
internal static int ClampDayIndex(int requested, int count) => count == 0 ? 0 : Math.Clamp(requested, 0, count - 1);
internal static IReadOnlyList<PlannedExerciseDraft> GroupSelected(
    IReadOnlyList<PlannedExerciseDraft> current,
    IReadOnlySet<Guid> selectedIds,
    Guid groupId,
    int intervalSeconds,
    int restSeconds) =>
    current.Select((item, index) => selectedIds.Contains(item.ExerciseId)
        ? item with { SupersetGroupId = groupId, SupersetPosition = current.Take(index + 1).Count(x => selectedIds.Contains(x.ExerciseId)), IntervalSeconds = intervalSeconds, RestSeconds = restSeconds }
        : item).ToList();
```

- [ ] **Step 2: Implement the plan tabs**

Render a scroll-snap tab strip with `role="tablist"`, three tabs visible at common widths and a single full-width exercise list below. Use `aria-selected`, keyboard arrows and `focus-visible`.

- [ ] **Step 3: Implement the superserie editor**

Allow selecting at least two day exercises, grouping/ungrouping them, editing interval and group rest, and showing the group as one connected block in the summary.

- [ ] **Step 4: Verify**

Run: `dotnet build FormaAI.sln`
Expected: build succeeds with no errors.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Components/Training src/FormaAI.Web/Pages/Training.razor src/FormaAI.Web/wwwroot/css/forma-signal.css src/FormaAI.Web/Services/TrainingClient.cs
git commit -m "Przebudować plany i edycję superserii"
```

### Task 7: Referencyjna aktywna sesja i wymiana

**Files:**
- Create: `src/FormaAI.Web/Components/Training/ExerciseHero.razor`
- Create: `src/FormaAI.Web/Components/Training/WorkoutSetEditor.razor`
- Create: `src/FormaAI.Web/Components/Training/WorkoutTimer.razor`
- Create: `src/FormaAI.Web/Components/Training/ExerciseSwapSheet.razor`
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Consumes: `WorkoutSequence`, media URLs and expanded session response.
- Produces: mobile-first session matching supplied references.

- [ ] **Step 1: Build components from the approved references**

Use a full-width fixed-ratio media hero, plain set rows, rest/interval controls, visible 1RM, a sticky thumb-zone action and a full-screen swap sheet with thumbnails.

- [ ] **Step 2: Wire the sequence**

After a set save, call the deterministic sequence helper, select the next exercise and start the returned `Interval` or `Rest` timer. Keep completed sets after replacement.

- [ ] **Step 3: Add interaction motion**

Use only transform/opacity transitions, 100-250 ms, `cubic-bezier(.23,1,.32,1)`, `scale(.97)` press feedback and a reduced-motion fallback.

- [ ] **Step 4: Verify**

Run: `dotnet build FormaAI.sln`
Expected: build succeeds. Manually verify 320 px, 390 px and desktop widths.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Components/Training src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/forma-signal.css
git commit -m "Przebudować przebieg aktywnego treningu"
```

### Task 8: Workflow AI i końcowy audyt treningu

**Files:**
- Create: `src/FormaAI.Web/Components/Training/WorkoutAiDraft.razor`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/Pages/Assistant.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Modify: `docs/design/training-ui-analysis.md`

**Interfaces:**
- Consumes: szkic z Task 4.
- Produces: opis -> edytowalny podgląd -> jawny zapis.

- [ ] **Step 1: Implement the editable draft**

Show every exercise and set, require resolution of `NeedsReview`, allow editing weights/repetitions/RIR, and expose one final button `Zapisz trening na dziś`.

- [ ] **Step 2: Run visual and motion review**

Compare phone and desktop renders with references. Record the Emil review as:

| Before | After | Why |
| --- | --- | --- |
| current implementation | final correction | user-visible reason |

- [ ] **Step 3: Run Impeccable detector once**

Run: `node C:\Users\Jannu\.codex\skills\impeccable\scripts\detect.mjs --json src/FormaAI.Web/Pages/Training.razor src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/Pages/ExerciseDetails.razor src/FormaAI.Web/Components/Training src/FormaAI.Web/wwwroot/css/forma-signal.css`
Expected: JSON findings are reviewed and material issues fixed.

- [ ] **Step 4: Run the Impeccable finish review**

Give the reviewer the original request, accepted spec, references, `DESIGN.md`, changed targets and detector findings. Apply material corrections.

- [ ] **Step 5: Verify and commit**

Run: `dotnet build FormaAI.sln`
Run: `dotnet test FormaAI.sln --no-build`
Expected: all tests pass.

```powershell
git add src/FormaAI.Web docs/design
git commit -m "Dokończyć workflow treningu z AI"
```
