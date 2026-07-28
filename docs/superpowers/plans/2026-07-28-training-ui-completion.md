# Training UI Completion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Domknąć wszystkie widoki modułu Trening, szybki zapis serii, pełny przepływ superserii i zamiany ćwiczenia oraz wspólną obsługę zdjęć, GIF-ów i filmów.

**Architecture:** Zachowujemy istniejące trasy, MudBlazor, kontrakty sesji i logikę `WorkoutSequence`. Wspólne zasady plików trafiają do `FormaAI.Contracts`, wspólna prezentacja medium do komponentu Blazor, a duży widok aktywnej sesji zostaje uporządkowany bez wymiany jego logiki czasu i zapisu. Liczba rund superserii jest jawnie zapisywana jako wspólna liczba planowanych serii jej członków.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, Entity Framework Core, xUnit, CSS.

## Global Constraints

- Zachować globalną nawigację, branding i system Forma Signal z `DESIGN.md`.
- Używać wyłącznie MudBlazor oraz istniejących ikon Material.
- Projektować najpierw dla telefonu, bez przewijania całej strony w poziomie przy 320 px.
- Minimalny cel dotykowy wynosi 44 na 44 px.
- Zdjęcia: JPG, JPEG, PNG i WebP. Animacje: GIF. Filmy: MP4 i WebM. Limit: 15 MB.
- Przy ograniczeniu ruchu GIF i film nie uruchamiają się automatycznie.
- Nie zmieniać istniejących danych ani wykonywać migracji, jeśli nie są konieczne.
- Każdy zamknięty moduł kończy się polskim commitem.
- Końcowo uruchomić `dotnet build FormaAI.sln` i `dotnet test FormaAI.sln --no-build`.

---

## File Structure

- `src/FormaAI.Contracts/Training/ExerciseMediaPolicy.cs`: jedno źródło prawdy dla MIME, rozszerzeń, limitu i atrybutu `accept`.
- `src/FormaAI.Web/Components/Training/ExerciseMediaFrame.razor`: wspólne renderowanie obrazu, GIF-u, filmu i placeholdera.
- `src/FormaAI.Web/Pages/ExerciseDetails.razor`: przesyłanie oraz pełny podgląd medium.
- `src/FormaAI.Web/Pages/Training.razor`: miniatury w planie i katalogu.
- `src/FormaAI.Web/Pages/NewWorkout.razor`: miniatury wyników oraz przegląd ćwiczeń.
- `src/FormaAI.Web/Pages/Workout.razor`: aktywne ćwiczenie, serie, superserie i pełny wybór zamiennika.
- `src/FormaAI.Api/Controllers/TrainingController.cs`: walidacja medium i jawna liczba rund superserii.
- `src/FormaAI.Domain/Training/TrainingModels.cs`: bezpieczna zmiana liczby zaplanowanych rund.
- `src/FormaAI.Contracts/Training/TrainingContracts.cs`: kontrakt rund superserii.
- `src/FormaAI.Web/wwwroot/css/app.css`: responsywny układ wszystkich zmienianych powierzchni.
- `tests/FormaAI.Application.Tests/ExerciseMediaPolicyTests.cs`: testy normalizacji mediów.
- `tests/FormaAI.Domain.Tests/TrainingSupersetTests.cs`: testy liczby rund.
- `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`: kontrakty struktury UI.
- `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`: walidacja endpointu superserii i mediów.

---

### Task 1: Wspólna polityka i komponent mediów

**Files:**
- Create: `src/FormaAI.Contracts/Training/ExerciseMediaPolicy.cs`
- Create: `src/FormaAI.Web/Components/Training/ExerciseMediaFrame.razor`
- Modify: `src/FormaAI.Web/_Imports.razor`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Services/TrainingClient.cs`
- Test: `tests/FormaAI.Application.Tests/ExerciseMediaPolicyTests.cs`

**Interfaces:**
- Produces: `ExerciseMediaPolicy.MaxBytes`, `ExerciseMediaPolicy.Accept`, `ExerciseMediaPolicy.TryNormalize(string?, string?, out string, out string)`.
- Produces: `<ExerciseMediaFrame Exercise="" Alt="" Compact="" AllowPlayback="" />`.

- [ ] **Step 1: Write failing policy tests**

```csharp
using FormaAI.Contracts.Training;

namespace FormaAI.Application.Tests;

public sealed class ExerciseMediaPolicyTests
{
    [Theory]
    [InlineData("image/jpeg", "ruch.jpeg", "image/jpeg", ".jpg")]
    [InlineData("image/png", "ruch.png", "image/png", ".png")]
    [InlineData("image/webp", "ruch.webp", "image/webp", ".webp")]
    [InlineData("image/gif", "ruch.gif", "image/gif", ".gif")]
    [InlineData("video/mp4", "ruch.mp4", "video/mp4", ".mp4")]
    [InlineData("video/webm", "ruch.webm", "video/webm", ".webm")]
    public void NormalizesAllowedMedia(string contentType, string fileName, string expectedType, string expectedExtension)
    {
        Assert.True(ExerciseMediaPolicy.TryNormalize(contentType, fileName, out var normalizedType, out var extension));
        Assert.Equal(expectedType, normalizedType);
        Assert.Equal(expectedExtension, extension);
    }

    [Theory]
    [InlineData("image/svg+xml", "ruch.svg")]
    [InlineData("video/quicktime", "ruch.mov")]
    [InlineData("image/png", "ruch.exe")]
    public void RejectsUnsupportedOrMismatchedMedia(string contentType, string fileName)
    {
        Assert.False(ExerciseMediaPolicy.TryNormalize(contentType, fileName, out _, out _));
    }
}
```

- [ ] **Step 2: Run the test to verify RED**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter ExerciseMediaPolicyTests
```

Expected: FAIL because `ExerciseMediaPolicy` does not exist.

- [ ] **Step 3: Implement the policy**

```csharp
namespace FormaAI.Contracts.Training;

public static class ExerciseMediaPolicy
{
    public const long MaxBytes = 15 * 1024 * 1024;
    public const string Accept = "image/jpeg,image/png,image/webp,image/gif,video/mp4,video/webm";

    private static readonly IReadOnlyDictionary<string, string[]> Extensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"],
            ["image/webp"] = [".webp"],
            ["image/gif"] = [".gif"],
            ["video/mp4"] = [".mp4"],
            ["video/webm"] = [".webm"]
        };

    public static bool TryNormalize(string? contentType, string? fileName, out string normalizedType, out string extension)
    {
        normalizedType = string.Empty;
        extension = string.Empty;
        if (string.IsNullOrWhiteSpace(contentType) || string.IsNullOrWhiteSpace(fileName))
            return false;
        var type = contentType.Trim().ToLowerInvariant();
        var sourceExtension = Path.GetExtension(fileName);
        if (!Extensions.TryGetValue(type, out var allowed) ||
            !allowed.Contains(sourceExtension, StringComparer.OrdinalIgnoreCase))
            return false;
        normalizedType = type;
        extension = type == "image/jpeg" ? ".jpg" : allowed[0];
        return true;
    }
}
```

- [ ] **Step 4: Use the policy in the API and browser client**

Replace duplicated MIME switches and byte constants with `ExerciseMediaPolicy`. The API must reject mismatched extension and MIME, generate a GUID storage name, and continue deleting the previous stored file only after the database save succeeds.

`TrainingClient.UploadExerciseMedia` must call:

```csharp
media.OpenReadStream(ExerciseMediaPolicy.MaxBytes)
```

- [ ] **Step 5: Add the shared media renderer**

The component renders:

```razor
@if (Exercise?.MediaUrl is null)
{
    <div class="exercise-media-frame-placeholder">
        <MudIcon Icon="@Icons.Material.Outlined.SportsGymnastics" />
        @if (!Compact) { <span>Podgląd ruchu nie został jeszcze dodany</span> }
    </div>
}
else if (IsVideo)
{
    <video src="@Exercise.MediaUrl" muted playsinline controls="@AllowPlayback"
           autoplay="@(AllowPlayback && MotionAllowed)" loop="@(AllowPlayback && MotionAllowed)"
           preload="metadata" aria-label="@Alt"></video>
}
else if (IsGif && AllowPlayback && !MotionAllowed)
{
    <button type="button" class="exercise-media-paused" @onclick="EnableMotion">
        <MudIcon Icon="@Icons.Material.Outlined.MotionPhotosPaused" />
        <span>Pokaż animację</span>
    </button>
}
else
{
    <img src="@Exercise.MediaUrl" alt="@Alt" loading="@(Compact ? "lazy" : "eager")" />
}
```

The component resolves `formaMotion.allowsMotion` once in `OnAfterRenderAsync`, reserves a stable aspect ratio and does not autoplay compact previews.

- [ ] **Step 6: Run focused tests and build**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter ExerciseMediaPolicyTests
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
```

Expected: PASS and zero build errors.

- [ ] **Step 7: Commit**

```powershell
git add src/FormaAI.Contracts/Training/ExerciseMediaPolicy.cs src/FormaAI.Web/Components/Training/ExerciseMediaFrame.razor src/FormaAI.Web/_Imports.razor src/FormaAI.Api/Controllers/TrainingController.cs src/FormaAI.Web/Services/TrainingClient.cs tests/FormaAI.Application.Tests/ExerciseMediaPolicyTests.cs
git commit -m "Ujednolicić media ćwiczeń"
```

---

### Task 2: Szczegóły ćwiczenia, plan i przygotowanie sesji

**Files:**
- Modify: `src/FormaAI.Web/Pages/ExerciseDetails.razor`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/Pages/NewWorkout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: `ExerciseMediaPolicy.Accept`, `ExerciseMediaPolicy.MaxBytes`, `ExerciseMediaFrame`.
- Produces: spójne pełne media i miniatury na trzech powierzchniach.

- [ ] **Step 1: Extend source contract tests**

Tests assert that:

```csharp
Assert.Contains("<ExerciseMediaFrame", detailsSource);
Assert.Contains("ExerciseMediaPolicy.Accept", detailsSource);
Assert.Contains("<ExerciseMediaFrame", trainingSource);
Assert.Contains("<ExerciseMediaFrame", builderSource);
Assert.DoesNotContain("<video src=", trainingSource);
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutNavigationSourceTests
```

Expected: FAIL because pages still render media independently.

- [ ] **Step 3: Replace duplicated media markup**

Use `ExerciseMediaFrame`:

```razor
<ExerciseMediaFrame Exercise="_exercise"
                    Alt="@($"Podgląd wykonania: {_exercise.Name}")"
                    AllowPlayback="true" />
```

For plan rows and search results:

```razor
<ExerciseMediaFrame Exercise="media"
                    Alt=""
                    Compact="true"
                    AllowPlayback="false" />
```

The upload input uses `accept="@ExerciseMediaPolicy.Accept"` and checks `ExerciseMediaPolicy.MaxBytes`.

- [ ] **Step 4: Normalize responsive layout**

CSS must keep:

- full media at `aspect-ratio: 16 / 9`;
- thumbnails at a stable 4:3 crop;
- history without page-level horizontal overflow;
- upload editor with one column below 600 px;
- one separator between history rows, not borders around every cell on phone.

- [ ] **Step 5: Verify and commit**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutNavigationSourceTests
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
git add src/FormaAI.Web/Pages/ExerciseDetails.razor src/FormaAI.Web/Pages/Training.razor src/FormaAI.Web/Pages/NewWorkout.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Ujednolicić media w widokach treningu"
```

---

### Task 3: Liczba rund i kolejność superserii

**Files:**
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Domain.Tests/TrainingSupersetTests.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Produces: `WorkoutExercise.ConfigureSuperset(Guid groupId, int position, int rounds, int intervalSeconds, int restSeconds)`.
- Produces: `UpdateWorkoutSupersetRequest(..., int Rounds = 3, ...)`.

- [ ] **Step 1: Write failing domain tests**

```csharp
[Fact]
public void SupersetRoundsSetThePlannedSets()
{
    var exercise = new Exercise("user", "Wiosłowanie", MuscleGroup.Back, Equipment.Dumbbell, false);
    var workout = new WorkoutExercise(exercise, 1, 3, 8, 12, 2, 90);

    workout.ConfigureSuperset(Guid.NewGuid(), 1, 5, 15, 120);

    Assert.Equal(5, workout.PlannedSets);
}

[Fact]
public void SupersetRoundsCannotHideCompletedSets()
{
    var exercise = new Exercise("user", "Wiosłowanie", MuscleGroup.Back, Equipment.Dumbbell, false);
    var workout = new WorkoutExercise(exercise, 1, 3, 8, 12, 2, 90);
    workout.Sets.Add(new CompletedSet(workout.Id, 1, 40, 10, 2, SetType.Working));

    Assert.Throws<InvalidOperationException>(() =>
        workout.ConfigureSuperset(Guid.NewGuid(), 1, 0, 15, 120));
}
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter TrainingSupersetTests
```

Expected: FAIL because `ConfigureSuperset` has no rounds parameter.

- [ ] **Step 3: Implement rounds safely**

```csharp
public void ConfigureSuperset(Guid groupId, int position, int rounds, int intervalSeconds, int restSeconds)
{
    ValidateSuperset(groupId, position, intervalSeconds);
    if (rounds is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(rounds));
    if (rounds < Sets.Count) throw new InvalidOperationException("Liczba rund nie może być mniejsza od liczby wykonanych serii.");
    if (restSeconds is < 0 or > 3600) throw new ArgumentOutOfRangeException(nameof(restSeconds));
    PlannedSets = rounds;
    SupersetGroupId = groupId;
    SupersetPosition = position;
    IntervalSeconds = intervalSeconds;
    RestSeconds = restSeconds;
}
```

- [ ] **Step 4: Extend contract and endpoint**

```csharp
public sealed record UpdateWorkoutSupersetRequest(
    [MinLength(2), MaxLength(5)] IReadOnlyList<Guid> WorkoutExerciseIds,
    [Range(1, 10)] int Rounds = 3,
    [Range(0, 3600)] int IntervalSeconds = 15,
    [Range(0, 3600)] int RestSeconds = 90);
```

The API validates `Rounds >= selected.Max(x => x!.Sets.Count)` and calls the new domain method.

- [ ] **Step 5: Replace HashSet with ordered selection**

`Workout.razor` stores:

```csharp
private readonly List<Guid> _supersetExerciseIds = [];
private int _supersetRounds = 3;
```

It exposes `MoveSupersetExercise(Guid id, int offset)` and renders up/down controls. `SaveSuperset` sends the list in its visible order and includes `_supersetRounds`.

- [ ] **Step 6: Run focused tests**

```powershell
dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter TrainingSupersetTests
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter TrainingFlowTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/FormaAI.Domain/Training/TrainingModels.cs src/FormaAI.Contracts/Training/TrainingContracts.cs src/FormaAI.Api/Controllers/TrainingController.cs src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Domain.Tests/TrainingSupersetTests.cs tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs
git commit -m "Domknąć edycję rund superserii"
```

---

### Task 4: Aktywna sesja, szybkie serie i zamiana ćwiczenia

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: `ExerciseMediaFrame`, istniejące `SaveSet`, `WorkoutSequence.Next`.
- Produces: cztery wizualne stany wiersza serii oraz pełnoekranowy picker zamiennika.

- [ ] **Step 1: Write source structure tests**

```csharp
Assert.Contains("workout-set-row active", source);
Assert.Contains("workout-set-row saved", source);
Assert.Contains("workout-primary-action", source);
Assert.Contains("workout-sheet swap-sheet", source);
Assert.Contains("swap-filter", source);
Assert.Contains("aria-modal=\"true\"", source);
Assert.Contains("<ExerciseMediaFrame", source);
```

- [ ] **Step 2: Run RED**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutNavigationSourceTests
```

Expected: FAIL on new structure contracts.

- [ ] **Step 3: Recompose the live hierarchy**

The active exercise contains:

- compact session header;
- shared 16:9 media;
- title and timer actions;
- set table;
- superseries strip;
- sticky primary action.

Saved sets render as `workout-set-row saved`. The editing row renders as `workout-set-row active`. Validation errors add `invalid` and `aria-invalid="true"`.

- [ ] **Step 4: Guard repeated set writes**

Add `_savingSet` and disable the primary action during save:

```csharp
if (_savingSet) return;
_savingSet = true;
try
{
    await JS.InvokeVoidAsync("formaTimer.ready");
    var setNumber = form.SetNumber ?? (exercise.Sets.Count == 0 ? 1 : exercise.Sets.Max(x => x.SetNumber) + 1);
    var request = new SaveSetRequest(
        exercise.Id,
        setNumber,
        form.WeightKg,
        form.Repetitions,
        form.Rir,
        form.IsWarmup ? SetType.Warmup : SetType.Working,
        form.Notes);
    if (form.SetId is Guid setId)
        await TrainingApi.UpdateSet(Id, setId, request);
    else
        await TrainingApi.SaveSet(Id, request);
    await Reload();
}
finally
{
    _savingSet = false;
}
```

Weight and RIR use decimal input mode. Repetitions use numeric input mode. A saved set remains editable by tapping its row.

- [ ] **Step 5: Turn replacement into a sheet**

The sheet keeps the current exercise summary, search, real thumbnail, muscle filter, equipment filter, similarity toggle, selected result and sticky confirmation. On mobile it fills the viewport. On desktop it is a centered panel with a constrained height.

The initial filter values match the current exercise, and users can clear them. Search and filters operate on the already loaded catalog.

- [ ] **Step 6: Apply motion and accessibility rules**

- only `transform` and `opacity` transition;
- press feedback uses scale `0.98` for 140 ms;
- sheet enter transition uses 200 ms custom ease-out;
- `prefers-reduced-motion` removes translation;
- focus remains visible;
- sheet close button and Escape-compatible dialog semantics are present;
- sticky action reserves bottom safe-area space.

- [ ] **Step 7: Verify and commit**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WorkoutNavigationSourceTests
dotnet build src/FormaAI.Web/FormaAI.Web.csproj
git add src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Dopracować aktywny trening i serie"
```

---

### Task 5: Cały moduł, wizualna weryfikacja i wydanie

**Files:**
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/Pages/NewWorkout.razor`
- Modify: `src/FormaAI.Web/Pages/ExerciseDetails.razor`
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Modify: `docs/superpowers/plans/2026-07-28-training-ui-completion.md`

**Interfaces:**
- Consumes: wszystkie wcześniejsze zadania.
- Produces: zweryfikowany moduł gotowy do uruchomienia.

- [ ] **Step 1: Run copy and source checks**

```powershell
rg -n "transition:\\s*all|scale\\(0\\)|ease-in\\b|font-size:\\s*(?:[0-9]|1[01])px" src/FormaAI.Web/wwwroot/css/app.css
rg -n "FIXME|Lorem|Acme|John Doe|Jane Doe" src/FormaAI.Web/Pages src/FormaAI.Web/Components/Training
git diff --check
```

Expected: no new design violations, placeholders or whitespace errors.

- [ ] **Step 2: Run the Impeccable detector**

```powershell
node C:\Users\Jannu\.codex\skills\impeccable\scripts\detect.mjs --json src/FormaAI.Web/Pages/Training.razor src/FormaAI.Web/Pages/NewWorkout.razor src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/Pages/ExerciseDetails.razor src/FormaAI.Web/Components/Training/ExerciseMediaFrame.razor src/FormaAI.Web/wwwroot/css/app.css
```

Expected: no unresolved high-confidence findings in changed UI.

- [ ] **Step 3: Build and test**

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

Expected: build succeeds and all tests pass.

- [ ] **Step 4: Run and inspect mobile plus desktop**

Start the documented local environment and inspect:

- `/training` at 390 by 844 and 1440 by 1000;
- `/workout/new` at both sizes;
- `/workout/{id}` with an active session;
- `/training/exercises/{id}` with image, GIF/video state and placeholder;
- replacement sheet;
- superseries editor;
- reduced-motion rendering.

For every inspected route verify `document.documentElement.scrollWidth == window.innerWidth`, the focused element has a visible outline, and the last control can scroll above the sticky action. If any assertion fails, adjust only the owning route class in `app.css`, repeat the same viewport check, then rerun `WorkoutNavigationSourceTests`.

- [ ] **Step 5: Review UI using Emil Kowalski format**

Record the final review as a Markdown table with columns `Before`, `After`, `Why`. Confirm exact transition properties, durations below 300 ms, gated hover states, press feedback and reduced-motion handling.

- [ ] **Step 6: Update checkboxes and commit final polish**

```powershell
git add src/FormaAI.Web docs/superpowers/plans/2026-07-28-training-ui-completion.md
git commit -m "Domknąć interfejs modułu treningowego"
```
