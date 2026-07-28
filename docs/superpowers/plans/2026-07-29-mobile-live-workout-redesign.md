# Mobile Live Workout Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `/workout/{id}` as a screen-faithful mobile exercise plate with a dominant exercise image, swipe navigation, flat set logging, and full-screen history, swap, and session-action panels while preserving all current workout behavior.

**Architecture:** Keep session state, API mutations, timers, and completion logic in `Workout.razor`. Extract the two self-contained visual units—gesture-aware hero and exercise insights sheet—into focused Razor components. Reuse the existing API contracts and `TrainingClient`; no backend, persistence, or contract changes are required.

**Tech Stack:** .NET 8, Blazor WebAssembly, Razor Components, MudBlazor, C#, CSS custom properties, xUnit source-contract tests.

## Global Constraints

- Phone widths from 320–600 px use the full-screen reference layout.
- Wider screens center the same phone-shaped work surface instead of stretching it into a dashboard.
- Preserve the existing Forma AI palette, Barlow Semi Condensed, Onest, and IBM Plex Mono.
- Keep every existing workout capability and API contract unchanged.
- Touch targets are at least 44 × 44 px.
- The page must not horizontally scroll at 320 px.
- Swipe must not interfere with vertical scrolling.
- Full-screen sheets must preserve the active exercise, set form, timer state, and scroll context.
- Respect `prefers-reduced-motion` and `env(safe-area-inset-bottom)`.
- Do not add a JavaScript dependency or a third-party gesture/chart package.

---

## File Map

- Create `src/FormaAI.Web/Components/Training/WorkoutExerciseHero.razor`
  - Owns exercise media presentation, overlay actions, segmented navigation, and horizontal swipe recognition.
- Create `src/FormaAI.Web/Components/Training/WorkoutHistorySheet.razor`
  - Owns the full-screen History, Chart, and Technique views for the active exercise.
- Modify `src/FormaAI.Web/Pages/Workout.razor`
  - Remains the state owner; composes the new components, restructures set logging, and exposes full-screen swap and menu sheets.
- Modify `src/FormaAI.Web/wwwroot/css/app.css`
  - Adds the reference-faithful mobile workout surface and responsive/full-screen sheet styling.
- Modify `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`
  - Locks the required component boundaries, controls, accessibility hooks, and preserved workout actions.

---

### Task 1: Lock the new workout structure with failing source-contract tests

**Files:**
- Modify: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: source files under `src/FormaAI.Web`.
- Produces: contract tests for `WorkoutExerciseHero`, `WorkoutHistorySheet`, and the restructured `Workout.razor`.

- [ ] **Step 1: Add failing tests for the hero and full-screen sheets**

Add these tests to `WorkoutNavigationSourceTests`:

```csharp
[Fact]
public void LiveWorkoutUsesGestureHeroAndFullScreenTaskSheets()
{
    var workout = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));
    var hero = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Components", "Training", "WorkoutExerciseHero.razor"));
    var history = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Components", "Training", "WorkoutHistorySheet.razor"));

    Assert.Contains("<WorkoutExerciseHero", workout);
    Assert.Contains("<WorkoutHistorySheet", workout);
    Assert.Contains("live-workout-surface", workout);
    Assert.Contains("workout-history-sheet", history);
    Assert.Contains("role=\"dialog\"", history);
    Assert.Contains("aria-modal=\"true\"", history);
    Assert.Contains("@onpointerdown=\"HandlePointerDown\"", hero);
    Assert.Contains("@onpointerup=\"HandlePointerUp\"", hero);
    Assert.Contains("SwipeThreshold", hero);
}

[Fact]
public void LiveWorkoutKeepsAllOperationalActionsBehindReferenceControls()
{
    var source = File.ReadAllText(SourcePath("src", "FormaAI.Web", "Pages", "Workout.razor"));

    Assert.Contains("OpenHistory", source);
    Assert.Contains("BeginSwap", source);
    Assert.Contains("OpenWorkoutMenu", source);
    Assert.Contains("BeginSuperset", source);
    Assert.Contains("SaveSuperset", source);
    Assert.Contains("AddExercise", source);
    Assert.Contains("SaveNotes", source);
    Assert.Contains("Complete", source);
    Assert.Contains("Abandon", source);
    Assert.Contains("workout-sticky-action", source);
}
```

- [ ] **Step 2: Run the focused tests and verify the expected failure**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
```

Expected: FAIL because `WorkoutExerciseHero.razor`, `WorkoutHistorySheet.razor`, and the new markup tokens do not exist yet.

- [ ] **Step 3: Commit the red tests**

```powershell
git add tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Testować mobilny talerz aktywnego treningu"
```

---

### Task 2: Build the gesture-aware exercise hero

**Files:**
- Create: `src/FormaAI.Web/Components/Training/WorkoutExerciseHero.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes:
  - `ExerciseResponse? Exercise`
  - `string ExerciseName`
  - `int ActiveIndex`
  - `int ExerciseCount`
  - `EventCallback OnBack`
  - `EventCallback OnHistory`
  - `EventCallback OnSwap`
  - `EventCallback OnMenu`
  - `EventCallback OnPrevious`
  - `EventCallback OnNext`
- Produces: `<WorkoutExerciseHero ... />` with accessible overlay actions and pointer-based swipe callbacks.

- [ ] **Step 1: Create the component with overlay actions and segmented progress**

Create `WorkoutExerciseHero.razor` with this structure and component API:

```razor
<section class="workout-exercise-hero"
         aria-label="@($"Ćwiczenie {ActiveIndex + 1} z {ExerciseCount}: {ExerciseName}")"
         @onpointerdown="HandlePointerDown"
         @onpointerup="HandlePointerUp"
         @onpointercancel="CancelPointer"
         @onpointerleave="CancelPointer">
    <ExerciseMediaFrame Exercise="Exercise"
                        Alt="@($"Podgląd wykonania: {ExerciseName}")"
                        AllowPlayback="true"
                        Class="workout-hero-media" />

    <div class="workout-hero-actions">
        <MudIconButton Icon="@Icons.Material.Outlined.ArrowBackIosNew"
                       OnClick="() => OnBack.InvokeAsync()"
                       aria-label="Wróć do treningów" />
        <span></span>
        <MudIconButton Icon="@Icons.Material.Outlined.BarChart"
                       OnClick="() => OnHistory.InvokeAsync()"
                       aria-label="Historia i wykres ćwiczenia" />
        <MudIconButton Icon="@Icons.Material.Outlined.SwapHoriz"
                       OnClick="() => OnSwap.InvokeAsync()"
                       aria-label="Zamień ćwiczenie" />
        <MudIconButton Icon="@Icons.Material.Outlined.MoreHoriz"
                       OnClick="() => OnMenu.InvokeAsync()"
                       aria-label="Więcej opcji treningu" />
    </div>
</section>

<nav class="workout-hero-progress" aria-label="Pozycja w treningu">
    @for (var index = 0; index < ExerciseCount; index++)
    {
        var target = index;
        <button type="button"
                class="@(index == ActiveIndex ? "active" : null)"
                aria-current="@(index == ActiveIndex ? "step" : null)"
                aria-label="@($"Przejdź do ćwiczenia {index + 1}")"
                @onclick="() => OnSelect.InvokeAsync(target)">
            <span></span>
        </button>
    }
</nav>

@code {
    private const double SwipeThreshold = 56;
    private double? _pointerStartX;
    private double? _pointerStartY;

    [Parameter] public ExerciseResponse? Exercise { get; set; }
    [Parameter, EditorRequired] public string ExerciseName { get; set; } = string.Empty;
    [Parameter] public int ActiveIndex { get; set; }
    [Parameter] public int ExerciseCount { get; set; }
    [Parameter] public EventCallback OnBack { get; set; }
    [Parameter] public EventCallback OnHistory { get; set; }
    [Parameter] public EventCallback OnSwap { get; set; }
    [Parameter] public EventCallback OnMenu { get; set; }
    [Parameter] public EventCallback OnPrevious { get; set; }
    [Parameter] public EventCallback OnNext { get; set; }
    [Parameter] public EventCallback<int> OnSelect { get; set; }

    private void HandlePointerDown(PointerEventArgs args)
    {
        if (!args.IsPrimary) return;
        _pointerStartX = args.ClientX;
        _pointerStartY = args.ClientY;
    }

    private async Task HandlePointerUp(PointerEventArgs args)
    {
        if (_pointerStartX is null || _pointerStartY is null) return;
        var deltaX = args.ClientX - _pointerStartX.Value;
        var deltaY = args.ClientY - _pointerStartY.Value;
        CancelPointer();
        if (Math.Abs(deltaX) < SwipeThreshold || Math.Abs(deltaX) <= Math.Abs(deltaY) * 1.25)
            return;
        if (deltaX < 0 && ActiveIndex < ExerciseCount - 1)
            await OnNext.InvokeAsync();
        else if (deltaX > 0 && ActiveIndex > 0)
            await OnPrevious.InvokeAsync();
    }

    private void CancelPointer(PointerEventArgs? _ = null)
    {
        _pointerStartX = null;
        _pointerStartY = null;
    }
}
```

- [ ] **Step 2: Add focused hero styles**

Append the following rules in the workout section of `app.css`:

```css
.workout-exercise-hero {
    background: #101915;
    min-height: 280px;
    overflow: hidden;
    position: relative;
    touch-action: pan-y;
}
.workout-exercise-hero .workout-hero-media {
    aspect-ratio: 1.42 / 1;
    border-radius: 0;
    height: auto;
}
.workout-hero-actions {
    align-items: start;
    display: grid;
    gap: 8px;
    grid-template-columns: auto 1fr repeat(3, auto);
    inset: max(14px, env(safe-area-inset-top)) 14px auto;
    pointer-events: none;
    position: absolute;
    z-index: 2;
}
.workout-hero-actions .mud-icon-button-root {
    backdrop-filter: blur(12px);
    background: rgb(23 33 28 / .62);
    border-radius: 13px;
    color: white;
    min-height: 48px;
    min-width: 48px;
    pointer-events: auto;
}
.workout-hero-progress {
    background: var(--surface);
    display: grid;
    gap: 8px;
    grid-auto-columns: 1fr;
    grid-auto-flow: column;
    padding: 8px 14px 6px;
}
.workout-hero-progress button {
    background: transparent;
    border: 0;
    min-height: 24px;
    padding: 8px 0;
}
.workout-hero-progress span {
    background: var(--rule-strong);
    border-radius: 999px;
    display: block;
    height: 5px;
}
.workout-hero-progress button.active span { background: var(--action); }
```

- [ ] **Step 3: Compose the hero from `Workout.razor`**

Replace the old `workout-heading`, `workout-console`, `exercise-switcher`, and direct `ExerciseMediaFrame` in the active-exercise branch with:

```razor
<main class="live-workout-surface">
    <WorkoutExerciseHero Exercise="ActiveExerciseMedia"
                         ExerciseName="@exercise.ExerciseName"
                         ActiveIndex="_activeExerciseIndex"
                         ExerciseCount="@OrderedExercises.Count"
                         OnBack="BackToTraining"
                         OnHistory="OpenHistory"
                         OnSwap="() => BeginSwap(exercise)"
                         OnMenu="OpenWorkoutMenu"
                         OnPrevious="PreviousExercise"
                         OnNext="NextExercise"
                         OnSelect="SelectExercise" />
```

Add these state transitions in `@code`:

```csharp
private bool _historyOpen;
private bool _workoutMenuOpen;

private void BackToTraining() => Navigation.NavigateTo("/training");
private void OpenHistory() => _historyOpen = true;
private void OpenWorkoutMenu() => _workoutMenuOpen = true;
private void CloseWorkoutMenu() => _workoutMenuOpen = false;
```

Close the `main` element at the end of the active-exercise branch. Move the existing `workout-heading` into the completed and cardio-only branches so it is not rendered above the active exercise hero, but remains available on non-live states.

- [ ] **Step 4: Run the focused tests**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
```

Expected: the hero assertions PASS; the history-sheet assertions still FAIL because the second component is not created yet.

- [ ] **Step 5: Commit the hero**

```powershell
git add src/FormaAI.Web/Components/Training/WorkoutExerciseHero.razor src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Dodać mobilny hero aktywnego ćwiczenia"
```

---

### Task 3: Rebuild the live set plate and sticky primary action

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: existing `SetForm`, `SaveSet`, `EditSet`, timer functions, `BestEstimatedOneRepMax`, and preset data.
- Produces: `.live-exercise-plate`, `.live-set-grid`, and `.workout-sticky-action`.

- [ ] **Step 1: Replace the generic loading state with a workout-shaped skeleton**

Replace the loading branch with:

```razor
<div class="workout-live-skeleton" aria-label="Przygotowuję sesję" aria-busy="true">
    <span class="skeleton-block hero"></span>
    <span class="skeleton-block title"></span>
    <div><span class="skeleton-block timer"></span><span class="skeleton-block timer"></span></div>
    <span class="skeleton-block row"></span>
    <span class="skeleton-block row"></span>
    <span class="skeleton-block row"></span>
</div>
```

Add:

```css
.workout-live-skeleton { background: var(--surface); display: grid; gap: 14px; min-height: 100dvh; padding-bottom: 24px; }
.workout-live-skeleton .hero { aspect-ratio: 1.42 / 1; border-radius: 0; width: 100%; }
.workout-live-skeleton .title { height: 54px; margin-inline: 18px; width: min(72%, 360px); }
.workout-live-skeleton > div { display: grid; gap: 10px; grid-template-columns: repeat(2, 1fr); padding-inline: 18px; }
.workout-live-skeleton .timer { height: 64px; }
.workout-live-skeleton .row { height: 58px; margin-inline: 18px; }
```

The existing `.skeleton-block` animation already stops under `prefers-reduced-motion`.

- [ ] **Step 2: Restructure the active exercise metadata and timer controls**

Inside `live-workout-surface`, render:

```razor
<article class="live-exercise-plate">
    <header class="live-exercise-title">
        <div>
            <span class="card-kicker">Ćwiczenie @(_activeExerciseIndex + 1) z @OrderedExercises.Count</span>
            <h1>@exercise.ExerciseName</h1>
            <p>@exercise.PlannedSets serie × @exercise.MinReps–@exercise.MaxReps powt.
                @if (exercise.TargetRir is not null) { <span> · RIR @exercise.TargetRir</span> }
            </p>
        </div>
        <span class="live-e1rm">1RM <strong>@BestEstimatedOneRepMax(exercise).ToString("0.#")</strong></span>
    </header>

    <div class="live-timer-controls">
        <button type="button" class="@(_timerMode == TimerMode.Rest ? "active" : null)"
                @onclick="() => StartRestTimer(exercise.RestSeconds ?? 90)">
            <MudIcon Icon="@Icons.Material.Outlined.Schedule" />
            <span>Przerwa</span>
            <strong>@(_timerMode == TimerMode.Rest ? FormatTime(_timerSeconds) : FormatTime(exercise.RestSeconds ?? 90))</strong>
        </button>
        <button type="button" class="@(_timerMode == TimerMode.Interval ? "active" : null)"
                @onclick="StartInterval">
            <MudIcon Icon="@Icons.Material.Outlined.Timer" />
            <span>Interwał</span>
            <strong>@(_timerMode == TimerMode.Interval ? FormatTime(_timerSeconds) : "stoper")</strong>
        </button>
    </div>
```

When `_timerMode != TimerMode.None`, place pause, reset, and close actions immediately beneath these controls using the existing `ToggleTimer`, `ResetTimer`, and `CloseTimer` methods.

- [ ] **Step 3: Convert the current set table to the reference-style flat grid**

Retain the same loops, bindings, and click handlers, but rename the container and row classes:

```razor
<div class="live-set-grid" role="table" aria-label="Serie ćwiczenia">
    <div class="live-set-header" role="row">
        <span>Seria</span><span>kg</span><span>Powt.</span><span>RIR</span><span>Stan</span>
    </div>
    @foreach (var set in exercise.Sets.OrderBy(x => x.SetNumber))
    {
        <button type="button"
                class="live-set-row saved"
                @onclick="() => EditSet(exercise, set)"
                role="row">
            <span class="set-number">@set.SetNumber@(set.SetType == SetType.Warmup ? "R" : null)</span>
            <strong>@set.WeightKg</strong>
            <strong>@set.Repetitions</strong>
            <span>@(set.Rir?.ToString("0.#") ?? "–")</span>
            <span class="set-status"><MudIcon Icon="@Icons.Material.Filled.Check" /></span>
        </button>
    }
    <div class="live-set-row active @(form.ValidationError is null ? null : "invalid")" role="row">
        <span class="set-number">@(form.SetNumber ?? exercise.Sets.Count + 1)</span>
        <MudNumericField @bind-Value="form.WeightKg" aria-label="Ciężar w kilogramach" Min="0m" Max="1000m" HideSpinButtons="true" />
        <MudNumericField @bind-Value="form.Repetitions" aria-label="Liczba powtórzeń" Min="1" Max="1000" HideSpinButtons="true" />
        <MudNumericField @bind-Value="form.Rir" aria-label="Powtórzenia w zapasie" Min="0m" Max="10m" HideSpinButtons="true" />
        <span class="set-status current"><MudIcon Icon="@Icons.Material.Outlined.Edit" /></span>
    </div>
</div>
```

Keep the planned-set loop and validation message directly after the active row.

- [ ] **Step 4: Move the primary set action into a safe-area sticky bar**

Wrap the existing save button:

```razor
<div class="workout-sticky-action">
    <MudButton Variant="Variant.Filled"
               Color="Color.Primary"
               FullWidth="true"
               Size="Size.Large"
               Disabled="_savingSet"
               OnClick="() => SaveSet(exercise, form)"
               Class="log-set-button">
        @(_savingSet ? "Zapisuję…" : form.SetId is null ? "Zapisz serię" : "Zapisz poprawioną serię")
    </MudButton>
</div>
```

- [ ] **Step 5: Add the flat plate and responsive set styles**

Add:

```css
body:has(.workout-mode) .top-bar { display: none; }
.workout-mode { max-width: 680px !important; padding: 0 !important; }
.live-workout-surface { background: var(--surface); min-height: 100dvh; }
.live-exercise-plate { padding: 0 18px 112px; }
.live-exercise-title {
    align-items: end;
    display: grid;
    gap: 14px;
    grid-template-columns: minmax(0, 1fr) auto;
    padding: 16px 0 12px;
}
.live-exercise-title h1 {
    font-family: "Barlow Semi Condensed", sans-serif;
    font-size: clamp(2rem, 9vw, 3.2rem);
    letter-spacing: -.035em;
    line-height: .96;
    margin: 4px 0 7px;
}
.live-exercise-title p { color: var(--muted); margin: 0; }
.live-e1rm { color: var(--muted); font: 600 .72rem "IBM Plex Mono", monospace; white-space: nowrap; }
.live-e1rm strong { color: var(--action); font-size: 1.35rem; }
.live-timer-controls { display: grid; gap: 10px; grid-template-columns: repeat(2, 1fr); margin: 8px 0 18px; }
.live-timer-controls button {
    align-items: center;
    background: var(--surface-soft);
    border: 1px solid var(--rule);
    border-radius: 12px;
    color: var(--ink);
    display: grid;
    gap: 2px 9px;
    grid-template-columns: auto 1fr;
    min-height: 64px;
    padding: 9px 12px;
    text-align: left;
}
.live-timer-controls button.active { border-color: var(--action); }
.live-timer-controls .mud-icon-root { grid-row: 1 / 3; }
.live-timer-controls span { color: var(--muted); font-size: .72rem; }
.live-timer-controls strong { font: 600 1rem "IBM Plex Mono", monospace; }
.live-set-grid { border-top: 1px solid var(--rule); margin-inline: -18px; }
.live-set-header,
.live-set-row {
    align-items: center;
    display: grid;
    gap: 6px;
    grid-template-columns: 48px repeat(3, minmax(0, 1fr)) 42px;
    min-height: 58px;
    padding: 0 12px;
}
.live-set-header { color: var(--muted); font-size: .7rem; min-height: 42px; }
.live-set-row { background: var(--surface); border: 0; border-top: 1px solid var(--rule); color: var(--ink); width: 100%; }
.live-set-row.active { background: var(--action-soft); }
.live-set-row .set-number,
.live-set-row strong,
.live-set-row .mud-input { font-family: "IBM Plex Mono", monospace; font-variant-numeric: tabular-nums; }
.live-set-row.saved .set-status { background: var(--recovery-soft); color: var(--recovery); }
.live-set-row.active .set-number { background: var(--action); color: white; }
.live-set-row .set-number,
.live-set-row .set-status { border-radius: 50%; display: grid; height: 36px; place-items: center; width: 36px; }
.workout-sticky-action {
    background: linear-gradient(transparent, var(--surface) 24%);
    bottom: 0;
    left: 50%;
    max-width: 680px;
    padding: 28px 18px max(14px, env(safe-area-inset-bottom));
    position: fixed;
    transform: translateX(-50%);
    width: 100%;
    z-index: 20;
}
```

- [ ] **Step 6: Build the web project**

Run:

```powershell
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
```

Expected: BUILD SUCCEEDED with no Razor compilation errors.

- [ ] **Step 7: Commit the live set plate**

```powershell
git add src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Przebudować mobilny zapis serii"
```

---

### Task 4: Add the full-screen History, Chart, and Technique sheet

**Files:**
- Create: `src/FormaAI.Web/Components/Training/WorkoutHistorySheet.razor`
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes:
  - `ExerciseResponse Exercise`
  - `IReadOnlyList<ExerciseHistoryEntry> History`
  - `EventCallback OnClose`
- Produces: modal `.workout-history-sheet` with three tabs and grouped history/chart presentation.

- [ ] **Step 1: Change workout history state from one entry to full lists**

In `Workout.razor`, replace:

```csharp
private readonly Dictionary<Guid, ExerciseHistoryEntry?> _history = [];
```

with:

```csharp
private readonly Dictionary<Guid, IReadOnlyList<ExerciseHistoryEntry>> _history = [];
```

Update both history-loading locations to assign the complete result:

```csharp
_history[exerciseId] = await TrainingApi.GetHistory(exerciseId);
```

Update the “Ostatnio” block to obtain the first item:

```razor
@if (exercise.ExerciseId is Guid exerciseId &&
     _history.TryGetValue(exerciseId, out var history) &&
     history.FirstOrDefault() is { } last)
{
    <div class="last-performance">
        <span>Ostatnio</span>
        <strong>@last.WeightKg kg × @last.Repetitions</strong>
        <small>RIR @(last.Rir?.ToString("0.#") ?? "brak")</small>
    </div>
}
```

- [ ] **Step 2: Create `WorkoutHistorySheet.razor`**

Create `WorkoutHistorySheet.razor`:

```razor
<div class="workout-sheet-backdrop" @onclick="() => OnClose.InvokeAsync()">
    <section class="workout-sheet workout-history-sheet"
             role="dialog"
             aria-modal="true"
             aria-labelledby="workout-history-title"
             tabindex="-1"
             @onkeydown="HandleKeyDown"
             @onclick:stopPropagation="true">
        <header class="workout-sheet-header">
            <MudIconButton Icon="@Icons.Material.Outlined.ArrowBack"
                           OnClick="() => OnClose.InvokeAsync()"
                           aria-label="Wróć do aktywnego treningu" />
            <strong>FORMA<span>/AI</span></strong>
            <MudIconButton Icon="@Icons.Material.Outlined.Close"
                           OnClick="() => OnClose.InvokeAsync()"
                           aria-label="Zamknij historię ćwiczenia" />
        </header>

        <div class="workout-history-content">
            <section class="workout-history-hero">
                <ExerciseMediaFrame Exercise="Exercise"
                                    Alt="@($"Podgląd wykonania: {Exercise.Name}")"
                                    AllowPlayback="true" />
                <div>
                    <h1 id="workout-history-title">@Exercise.Name</h1>
                    <p><strong>Mięśnie główne</strong><span>@MuscleGroupName(Exercise.MuscleGroup)</span></p>
                    <p><strong>Sprzęt</strong><span>@EquipmentName(Exercise.Equipment)</span></p>
                </div>
            </section>

            <nav class="workout-history-tabs" aria-label="Informacje o ćwiczeniu">
                <button type="button" class="@(_tab == 0 ? "active" : null)" @onclick="() => _tab = 0">Historia</button>
                <button type="button" class="@(_tab == 1 ? "active" : null)" @onclick="() => _tab = 1">Wykres</button>
                <button type="button" class="@(_tab == 2 ? "active" : null)" @onclick="() => _tab = 2">Technika</button>
            </nav>

            @if (_tab == 0)
            {
                <section class="workout-history-list">
                    @if (History.Count == 0)
                    {
                        <div class="module-empty">
                            <MudIcon Icon="@Icons.Material.Outlined.History" />
                            <div><strong>Brak zapisanych serii</strong><span>Historia pojawi się po pierwszym ukończonym treningu.</span></div>
                        </div>
                    }
                    @foreach (var group in History.GroupBy(x => x.CompletedAtUtc.ToLocalTime().Date).OrderByDescending(x => x.Key))
                    {
                        <article>
                            <h2><MudIcon Icon="@Icons.Material.Outlined.CalendarMonth" /> @group.Key.ToString("dd.MM.yyyy")</h2>
                            <div class="workout-history-table">
                                <div><span>Seria</span><span>Ciężar × powt.</span><span>1RM</span></div>
                                @{ var setNumber = 0; }
                                @foreach (var set in group.OrderBy(x => x.CompletedAtUtc))
                                {
                                    setNumber++;
                                    <div>
                                        <span>@setNumber</span>
                                        <strong>@set.WeightKg.ToString("0.#") kg × @set.Repetitions</strong>
                                        <strong>@EstimatedOneRepMax(set).ToString("0.#") kg</strong>
                                    </div>
                                }
                            </div>
                        </article>
                    }
                </section>
            }
            else if (_tab == 1)
            {
                <section class="workout-history-chart-panel">
                    <div><span class="card-kicker">Ostatnie wyniki</span><h2>Szacowane 1RM</h2><p>Najlepszy wynik z każdej sesji.</p></div>
                    @if (ChartPoints.Count == 0)
                    {
                        <div class="module-empty">
                            <MudIcon Icon="@Icons.Material.Outlined.BarChart" />
                            <div><strong>Brak danych do wykresu</strong><span>Zapisz pierwszą serię, aby zobaczyć trend.</span></div>
                        </div>
                    }
                    else
                    {
                        <div class="workout-history-chart" aria-label="Wykres szacowanego maksymalnego ciężaru">
                            @foreach (var point in ChartPoints)
                            {
                                <div style="@($"--value:{point.Percent}%")">
                                    <i></i>
                                    <strong>@point.Value.ToString("0.#")</strong>
                                    <span>@point.Date.ToString("dd.MM")</span>
                                </div>
                            }
                        </div>
                    }
                </section>
            }
            else
            {
                <section class="workout-technique-panel">
                    <article><span class="card-kicker">Wykonanie</span><h2>Opis i wskazówki</h2><p>@TechniqueCopy</p></article>
                    <article>
                        <span class="card-kicker">Zaangażowanie</span>
                        <h2>Pracujące mięśnie</h2>
                        @foreach (var engagement in Engagements)
                        {
                            <div class="workout-muscle-row">
                                <span><strong>@MuscleGroupName(engagement.MuscleGroup)</strong><small>@engagement.Percentage%</small></span>
                                <i style="@($"--engagement:{engagement.Percentage}%")"></i>
                            </div>
                        }
                    </article>
                </section>
            }
        </div>
    </section>
</div>

@code {
[Parameter, EditorRequired] public ExerciseResponse Exercise { get; set; } = default!;
[Parameter] public IReadOnlyList<ExerciseHistoryEntry> History { get; set; } = [];
[Parameter] public EventCallback OnClose { get; set; }

private int _tab;
private string TechniqueCopy => string.IsNullOrWhiteSpace(Exercise.Description)
    ? "Do tego ćwiczenia nie dodano jeszcze wskazówek wykonania."
    : Exercise.Description;
private IReadOnlyList<ExerciseMuscleEngagementResponse> Engagements =>
    Exercise.MuscleEngagements is { Count: > 0 } values
        ? values.OrderByDescending(x => x.Percentage).ToList()
        : [new(Exercise.MuscleGroup, 100)];

private static decimal EstimatedOneRepMax(ExerciseHistoryEntry set) =>
    set.Repetitions <= 1 ? set.WeightKg : set.WeightKg * (1 + set.Repetitions / 30m);

private IReadOnlyList<HistoryChartPoint> ChartPoints
{
    get
    {
        var values = History
            .GroupBy(x => x.CompletedAtUtc.ToLocalTime().Date)
            .OrderBy(x => x.Key)
            .TakeLast(8)
            .Select(x => new HistoryChartPoint(x.Key, x.Max(EstimatedOneRepMax), 0))
            .ToList();
        var max = values.Select(x => x.Value).DefaultIfEmpty().Max();
        return values
            .Select(x => x with { Percent = max == 0 ? 0 : Math.Max(8, (int)Math.Round(x.Value * 100 / max)) })
            .ToList();
    }
}

private void HandleKeyDown(KeyboardEventArgs args)
{
    if (args.Key == "Escape")
        _ = OnClose.InvokeAsync();
}

private static string EquipmentName(Equipment equipment) => equipment switch
{
    Equipment.Barbell => "Sztanga",
    Equipment.Dumbbell => "Hantle",
    Equipment.Machine => "Maszyna",
    Equipment.Cable => "Wyciąg",
    Equipment.Bodyweight => "Masa ciała",
    Equipment.Kettlebell => "Kettlebell",
    _ => "Inny sprzęt"
};

private static string MuscleGroupName(MuscleGroup group) => group switch
{
    MuscleGroup.Chest => "Klatka piersiowa",
    MuscleGroup.Back => "Plecy",
    MuscleGroup.Shoulders => "Barki",
    MuscleGroup.Biceps => "Biceps",
    MuscleGroup.Triceps => "Triceps",
    MuscleGroup.Quadriceps => "Czworogłowe uda",
    MuscleGroup.Hamstrings => "Dwugłowe uda",
    MuscleGroup.Glutes => "Pośladki",
    MuscleGroup.Calves => "Łydki",
    MuscleGroup.Core => "Core",
    MuscleGroup.Forearms => "Przedramiona",
    _ => "Całe ciało"
};

private sealed record HistoryChartPoint(DateTime Date, decimal Value, int Percent);
}
```

- [ ] **Step 3: Render the sheet without navigating away**

In `Workout.razor`, add:

```razor
@if (_historyOpen && ActiveExerciseMedia is { } historyExercise)
{
    var history = _history.TryGetValue(historyExercise.Id, out var entries)
        ? entries
        : Array.Empty<ExerciseHistoryEntry>();
    <WorkoutHistorySheet Exercise="historyExercise"
                         History="history"
                         OnClose="() => _historyOpen = false" />
}
```

- [ ] **Step 4: Add full-screen history styles**

Add CSS for:

- `.workout-history-sheet` with `min-height: 100dvh`, `max-height: 100dvh`, and no border radius at phone widths;
- `.workout-history-hero` with a 16:9 media frame;
- `.workout-history-tabs` as a three-column underline navigation;
- `.workout-history-content` with vertical scrolling and bottom safe-area padding;
- `.workout-history-table` as a thin-lined three-column grid;
- `.workout-history-chart` as an eight-column bar chart using `--value`;
- `.workout-technique-panel` using flat sections and separators.

Keep all text at 12 px or larger and use `--action` for the active tab and best 1RM value.

- [ ] **Step 5: Run the focused tests and web build**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
```

Expected: all focused tests PASS and BUILD SUCCEEDED.

- [ ] **Step 6: Commit the history sheet**

```powershell
git add src/FormaAI.Web/Components/Training/WorkoutHistorySheet.razor src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Dodać pełnoekranową historię ćwiczenia"
```

---

### Task 5: Restyle replacement as the full-screen reference workflow

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: existing `BeginSwap`, `CancelSwap`, `SwapOptions`, filters, `ReplaceExercise`, and `SwapExplanation`.
- Produces: `.workout-swap-sheet` with current-exercise summary, filter row, image list, and sticky confirmation.

- [ ] **Step 1: Reorder the existing swap markup to match the reference**

Keep the existing state and mutation methods, but change the sheet class to:

```razor
<section class="workout-sheet workout-swap-sheet"
         role="dialog"
         aria-modal="true"
         aria-labelledby="swap-sheet-title"
         tabindex="-1"
         @onkeydown="HandleSwapKeyDown"
         @onclick:stopPropagation="true">
```

Order the sheet content as:

1. header with close control and compact current-exercise summary;
2. `h2` with `Zamień ćwiczenie`;
3. search field;
4. three filters: muscle group, equipment, similar-only;
5. vertical results;
6. sticky `Anuluj` and selected replacement action.

For every result, keep `ExerciseMediaFrame`, name, muscle group, equipment, and a swap icon. The currently active exercise is shown in the summary and is not duplicated as a selectable result.

- [ ] **Step 2: Preserve current replacement safety behavior**

Keep:

```csharp
if (_selectedExerciseId is null) return;
_swapping = true;
```

and the existing `try/catch/finally`, full-session reload, active-index restoration, history load, snackbar copy, and `_swapping` reset.

The confirmation button remains disabled when:

```razor
Disabled="@(_selectedExerciseId is null || _swapping)"
```

- [ ] **Step 3: Replace the old swap CSS with the screen-faithful list**

Style `.workout-swap-sheet` as a full-height white surface. Use:

- a 92 px current-exercise thumbnail;
- search height of at least 52 px;
- horizontally fitting three-filter grid that wraps to two rows below 390 px;
- result rows with a 72 px circular image, flat separators, and a 48 px outlined swap action;
- `var(--action-soft)` for selected results;
- sticky bottom actions with safe-area padding.

Do not show the standard app bottom navigation while the sheet is open.

- [ ] **Step 4: Run focused tests and build**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
```

Expected: tests PASS and BUILD SUCCEEDED.

- [ ] **Step 5: Commit the replacement workflow**

```powershell
git add src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Ujednolicić pełnoekranową zamianę ćwiczenia"
```

---

### Task 6: Move supersets and secondary session controls into the ellipsis sheet

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: existing superset builder state and methods, session notes, add-exercise state, `Complete`, and `Abandon`.
- Produces: `.workout-options-sheet` opened only from the hero ellipsis action.

- [ ] **Step 1: Add the full-screen options sheet**

Render when `_workoutMenuOpen`:

```razor
<div class="workout-sheet-backdrop" @onclick="CloseWorkoutMenu">
    <section class="workout-sheet workout-options-sheet"
             role="dialog"
             aria-modal="true"
             aria-labelledby="workout-options-title"
             @onclick:stopPropagation="true">
        <header class="workout-sheet-header">
            <div>
                <span class="card-kicker">Aktywna sesja</span>
                <h2 id="workout-options-title">Opcje treningu</h2>
            </div>
            <MudIconButton Icon="@Icons.Material.Outlined.Close"
                           OnClick="CloseWorkoutMenu"
                           aria-label="Zamknij opcje treningu" />
        </header>
        <div class="workout-options-content">
            <button type="button" class="workout-option-row" @onclick="() => BeginSuperset(exercise)">
                <MudIcon Icon="@Icons.Material.Outlined.Layers" />
                <span><strong>Superseria</strong><small>Połącz lub edytuj kolejność ćwiczeń</small></span>
                <MudIcon Icon="@Icons.Material.Outlined.ChevronRight" />
            </button>
            <details class="workout-option-details">
                <summary>Notatka i typ aktualnej serii</summary>
                <MudTextField @bind-Value="form.Notes" Label="Notatka do serii" MaxLength="300" />
                <MudCheckBox @bind-Value="form.IsWarmup" Label="Seria rozgrzewkowa" />
            </details>
            <details class="workout-option-details">
                <summary>Dodaj ćwiczenie</summary>
                <MudSelect T="Guid?" @bind-Value="_selectedExerciseId" Label="Ćwiczenie">
                    @foreach (var option in _catalog)
                    {
                        <MudSelectItem T="Guid?" Value="@((Guid?)option.Id)">@option.Name</MudSelectItem>
                    }
                </MudSelect>
                <MudButton Variant="Variant.Outlined" FullWidth="true"
                           Disabled="@(_selectedExerciseId is null)"
                           OnClick="AddExercise">Dodaj do sesji</MudButton>
            </details>
            <details class="workout-option-details">
                <summary>Notatka do treningu</summary>
                <MudTextField @bind-Value="_sessionNotes" Label="Notatka" Lines="3" MaxLength="1000" />
                <MudButton Variant="Variant.Text" FullWidth="true" OnClick="SaveNotes">Zapisz notatkę</MudButton>
            </details>
            <div class="workout-session-metrics">
                <span><small>Czas</small><strong>@FormatElapsed(_elapsed)</strong></span>
                <span><small>Postęp</small><strong>@SessionPercent%</strong></span>
                <span><small>Serie</small><strong>@CompletedSets/@PlannedSets</strong></span>
            </div>
            <MudButton Variant="Variant.Filled" Color="Color.Success" FullWidth="true"
                       OnClick="Complete">Zakończ i zobacz podsumowanie</MudButton>
            <MudButton Variant="Variant.Text" Color="Color.Error" FullWidth="true"
                       OnClick="Abandon">Porzuć trening</MudButton>
        </div>
    </section>
</div>
```

- [ ] **Step 2: Keep the superset editor inside the same full-screen task flow**

Change `BeginSuperset` to leave `_workoutMenuOpen = true` and set `_supersetBuilderOpen = true`. While `_supersetBuilderOpen`, replace the options list with the existing:

- selected-member list;
- reorder controls;
- additional exercise choices;
- rounds, interval, and rest fields;
- `SaveSuperset` action.

Change `CancelSuperset` to return to the options list:

```csharp
private void CancelSuperset()
{
    _supersetBuilderOpen = false;
    _supersetExerciseIds.Clear();
}
```

After a successful `SaveSuperset`, close both layers:

```csharp
CancelSuperset();
CloseWorkoutMenu();
```

- [ ] **Step 3: Keep the active superset strip in the main plate**

Retain the existing `workout-superset-strip`, but style it as a flat horizontal track with:

- `A1`, `A2`, and later position badges;
- exercise thumbnails from `_catalog`;
- round copy `@SupersetRound z @SupersetRounds rund`;
- active exercise outline in `--action`;
- horizontal scrolling contained inside the strip.

- [ ] **Step 4: Remove duplicate secondary controls from the main plate**

Delete the old visible:

- swap text button;
- superset text button;
- `set-details`;
- `exercise-page-actions`;
- `workout-options`;
- bottom `session-actions`.

The hero icons and ellipsis sheet are now the only entry points for these actions.

- [ ] **Step 5: Add options-sheet styles**

Style:

- `.workout-options-sheet` as full-height at phone widths;
- `.workout-options-content` as a vertically scrolling flat list;
- `.workout-option-row` with icon, two-line copy, chevron, and a 56 px minimum height;
- `.workout-option-details` with a separator and 16 px vertical padding;
- `.workout-session-metrics` as three equal data columns using IBM Plex Mono;
- `.superset-builder` without a nested card background inside the sheet.

- [ ] **Step 6: Run tests and build**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
```

Expected: tests PASS and BUILD SUCCEEDED.

- [ ] **Step 7: Commit the session options flow**

```powershell
git add src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Przenieść opcje sesji pod menu aktywnego ćwiczenia"
```

---

### Task 7: Responsive, accessibility, and visual regression pass

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/Components/Training/WorkoutExerciseHero.razor`
- Modify: `src/FormaAI.Web/Components/Training/WorkoutHistorySheet.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Modify: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: completed live workout UI.
- Produces: final verified phone-first experience at 320, 390, 430, 768, and 1280 px.

- [ ] **Step 1: Add accessibility assertions**

Extend the focused test:

```csharp
Assert.Contains("aria-label=\"Historia i wykres ćwiczenia\"", hero);
Assert.Contains("aria-label=\"Zamień ćwiczenie\"", hero);
Assert.Contains("aria-label=\"Więcej opcji treningu\"", hero);
Assert.Contains("aria-current", hero);
Assert.Contains("prefers-reduced-motion", File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css")));
Assert.Contains("safe-area-inset-bottom", File.ReadAllText(SourcePath("src", "FormaAI.Web", "wwwroot", "css", "app.css")));
```

- [ ] **Step 2: Verify the expected test failure if a hook is missing**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
```

Expected: FAIL only for any missing accessibility or responsive hook; add the exact missing label or CSS hook before continuing.

- [ ] **Step 3: Add final mobile guardrails**

Add:

```css
@media (max-width: 390px) {
    .live-exercise-plate { padding-inline: 14px; }
    .live-set-grid { margin-inline: -14px; }
    .live-set-header,
    .live-set-row {
        gap: 4px;
        grid-template-columns: 42px repeat(3, minmax(0, 1fr)) 36px;
        padding-inline: 7px;
    }
    .live-exercise-title h1 { font-size: 2rem; }
}

@media (min-width: 601px) {
    .workout-mode {
        box-shadow: 0 24px 72px rgb(23 33 28 / .12);
        margin-block: 18px;
    }
    .live-workout-surface,
    .workout-mode { border-radius: 18px; overflow: hidden; }
}

@media (prefers-reduced-motion: reduce) {
    .workout-sheet,
    .workout-exercise-hero,
    .workout-hero-progress span { transition: none !important; }
}
```

Confirm every interactive control has a visible `:focus-visible` outline and a minimum 44 px hit area.

- [ ] **Step 4: Run the complete automated verification**

Run:

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln
```

Expected: BUILD SUCCEEDED and all solution tests PASS.

- [ ] **Step 5: Run the application for rendered verification**

Run:

```powershell
dotnet run --project src/FormaAI.Web/FormaAI.Web.csproj
```

Open an active workout and inspect at:

- 320 × 700;
- 390 × 844;
- 430 × 932;
- 768 × 1024;
- 1280 × 900.

Verify:

- the image dominates the top of the phone screen;
- vertical scrolling is not captured by the swipe area;
- left/right swipe changes only one exercise;
- history closes back to the unchanged set form and timer;
- swap filters and confirmation remain reachable;
- supersets are created and reordered from the ellipsis sheet;
- the sticky save action never covers the active row;
- there is no page-level horizontal scroll;
- dark theme remains legible even though the target reference is light;
- keyboard focus and Escape work in every full-screen sheet.

- [ ] **Step 6: Commit final polish**

```powershell
git add src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/Components/Training/WorkoutExerciseHero.razor src/FormaAI.Web/Components/Training/WorkoutHistorySheet.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Dopracować responsywną sesję treningową"
```

---

## Final Acceptance Checklist

- The phone screen is immediately recognizable as the supplied active-session reference.
- Exercise media changes through horizontal swipe and explicit progress controls.
- History, Chart, and Technique open full-screen and preserve workout state.
- Swap opens full-screen from the dedicated swap icon.
- Superset creation and secondary actions live under the ellipsis control.
- Saved, active, planned, invalid, loading, empty, and disabled states are visible and coherent.
- Existing timers, set editing, presets, progression suggestions, cardio, completion, abandonment, and notes still work.
- The full solution builds and all tests pass.
