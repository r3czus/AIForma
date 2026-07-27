# Osobny kreator i tryb sesji treningowej — plan implementacji

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Zastąpić rozwijany formularz z pulpitu osobną stroną `/workout/new`, na której można przygotować trening ręcznie lub z AI, a potem przejść do pełnego widoku sesji.

**Architecture:** Nowy kreator buduje jeden model szkicu i wysyła rozszerzony `StartQuickWorkoutRequest`. Szkic AI może zostać uruchomiony przez osobny, idempotentny endpoint, który tworzy aktywną sesję z presetami serii. Strona `/workout/{id}` odczytuje presety i podstawia je do kolejnych pól serii, a `Home.razor` odpowiada wyłącznie za nawigację.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, Entity Framework Core, SQL Server, xUnit.

## Global Constraints

- Zachować istniejącą kolorystykę i kierunek wizualny FormaAI.
- Projektować mobile-first, z pełnym flow w jednej kolumnie na telefonie.
- AI nie zapisuje ani nie rozpoczyna treningu przed wyraźnym zatwierdzeniem użytkownika.
- Nie umieszczać kluczy API w repozytorium ani frontendzie.
- Każdy zamknięty etap kończy osobny commit z polskim opisem.
- Po zmianach uruchomić `dotnet build FormaAI.sln` oraz `dotnet test FormaAI.sln --no-build`.

---

### Task 1: Rozszerzony szybki trening i presety serii

**Files:**
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Create through EF migration tooling: migration named `AddWorkoutSetPresets` in `src/FormaAI.Infrastructure/Persistence/Migrations`
- Modify: `src/FormaAI.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Produces: `QuickWorkoutSetPresetRequest`, rozszerzony `QuickWorkoutExerciseRequest`, `WorkoutSetPresetResponse`.
- Produces: `WorkoutExercise.Presets` oraz `WorkoutSetPreset`.
- Consumes: istniejące `StartQuickWorkoutRequest` i `WorkoutSessionResponse`.

- [ ] **Step 1: Napisać test integracyjny ustawień i presetów**

Dodać test tworzący dwa ćwiczenia połączone superserią i wysyłający:

```csharp
var groupId = Guid.NewGuid();
var session = await Send<StartQuickWorkoutRequest, WorkoutSessionResponse>(
    client,
    HttpMethod.Post,
    "api/v1/workout-sessions/quick",
    new("Trening przygotowany", 50,
    [
        new(first.Id, 2, 6, 8, 1, 120, groupId, 1, 20,
        [
            new(1, 80, 8, 2),
            new(2, 82.5m, 6, 1)
        ]),
        new(second.Id, 2, 10, 12, 2, 90, groupId, 2, 75)
    ]));

Assert.Equal(6, session.Exercises[0].MinReps);
Assert.Equal(groupId, session.Exercises[0].SupersetGroupId);
Assert.Equal(20, session.Exercises[0].IntervalSeconds);
Assert.Equal(82.5m, session.Exercises[0].Presets[1].WeightKg);
```

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter "FullyQualifiedName~QuickWorkoutPreservesConfigurationAndSetPresets"
```

Expected: FAIL, ponieważ kontrakty i `Presets` jeszcze nie istnieją.

- [ ] **Step 3: Dodać kontrakty**

Zastąpić prosty kontrakt ćwiczenia:

```csharp
public sealed record QuickWorkoutSetPresetRequest(
    [Range(1, 50)] int SetNumber,
    [Range(0, 1000)] decimal WeightKg,
    [Range(1, 1000)] int Repetitions,
    [Range(0, 10)] decimal? Rir);

public sealed record QuickWorkoutExerciseRequest(
    Guid ExerciseId,
    [Range(1, 10)] int Sets,
    [Range(1, 100)] int MinReps = 8,
    [Range(1, 100)] int MaxReps = 12,
    [Range(0, 10)] decimal? TargetRir = 2,
    [Range(0, 3600)] int? RestSeconds = 90,
    Guid? SupersetGroupId = null,
    [Range(1, 20)] int? SupersetPosition = null,
    [Range(0, 3600)] int? IntervalSeconds = null,
    IReadOnlyList<QuickWorkoutSetPresetRequest>? Presets = null);

public sealed record WorkoutSetPresetResponse(
    int SetNumber,
    decimal WeightKg,
    int Repetitions,
    decimal? Rir);
```

Rozszerzyć `WorkoutExerciseResponse` o `IReadOnlyList<WorkoutSetPresetResponse> Presets`.

- [ ] **Step 4: Dodać model domenowy i mapowanie EF**

W `WorkoutExercise` dodać:

```csharp
public List<WorkoutSetPreset> Presets { get; private set; } = [];
```

oraz encję:

```csharp
public sealed class WorkoutSetPreset
{
    private WorkoutSetPreset() { }

    public WorkoutSetPreset(
        Guid workoutExerciseId,
        int setNumber,
        decimal weightKg,
        int repetitions,
        decimal? rir)
    {
        Id = Guid.NewGuid();
        WorkoutExerciseId = workoutExerciseId;
        SetNumber = setNumber;
        WeightKg = weightKg;
        Repetitions = repetitions;
        Rir = rir;
    }

    public Guid Id { get; private set; }
    public Guid WorkoutExerciseId { get; private set; }
    public int SetNumber { get; private set; }
    public decimal WeightKg { get; private set; }
    public int Repetitions { get; private set; }
    public decimal? Rir { get; private set; }
}
```

W `AppDbContext` dodać `DbSet<WorkoutSetPreset> WorkoutSetPresets`, skonfigurować klucz, precyzję liczb, unikalny indeks `(WorkoutExerciseId, SetNumber)` i kaskadowe usuwanie.

- [ ] **Step 5: Rozszerzyć `StartQuick`**

Tworzyć `WorkoutExercise` z wartościami żądania zamiast stałych `8, 12, 2, 90`, walidować poprawność superserii i kopiować każdy preset:

```csharp
var workoutExercise = new WorkoutExercise(
    catalog[selected.ExerciseId],
    index + 1,
    selected.Sets,
    selected.MinReps,
    selected.MaxReps,
    selected.TargetRir,
    selected.RestSeconds,
    selected.SupersetGroupId,
    selected.SupersetPosition,
    selected.IntervalSeconds);

foreach (var preset in selected.Presets ?? [])
    workoutExercise.Presets.Add(new(
        workoutExercise.Id,
        preset.SetNumber,
        preset.WeightKg,
        preset.Repetitions,
        preset.Rir));
```

Do `SessionQuery` dodać `ThenInclude(x => x.Presets)`, a mapowanie odpowiedzi ma sortować presety po `SetNumber`.

- [ ] **Step 6: Wygenerować migrację i uruchomić test**

Run:

```powershell
dotnet ef migrations add AddWorkoutSetPresets --project src/FormaAI.Infrastructure --startup-project src/FormaAI.Api
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter "FullyQualifiedName~QuickWorkoutPreservesConfigurationAndSetPresets"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/FormaAI.Contracts src/FormaAI.Domain src/FormaAI.Infrastructure src/FormaAI.Api tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs
git commit -m "Rozszerzyć konfigurację szybkiego treningu"
```

---

### Task 2: Uruchamianie szkicu AI jako aktywnej sesji

**Files:**
- Modify: `src/FormaAI.Api/Controllers/AssistantController.cs`
- Modify: `src/FormaAI.Web/Services/AssistantClient.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`

**Interfaces:**
- Consumes: `AssistantCompletedWorkoutDraftPayload`, `WorkoutSetPreset`.
- Produces: `POST api/v1/assistant/actions/{id}/start-workout`.
- Produces: `AssistantClient.StartWorkout(Guid draftId)`.

- [ ] **Step 1: Napisać test braku zapisu przed zatwierdzeniem**

Po wygenerowaniu szkicu sprawdzić brak aktywnej sesji, następnie wywołać `/start-workout` dwukrotnie:

```csharp
Assert.Equal(HttpStatusCode.NotFound,
    (await client.GetAsync("api/v1/workout-sessions/active")).StatusCode);

var first = await Send<object, WorkoutSessionResponse>(
    client,
    HttpMethod.Post,
    $"api/v1/assistant/actions/{response.CompletedWorkoutDraft.Id}/start-workout",
    new { });
var second = await Send<object, WorkoutSessionResponse>(
    client,
    HttpMethod.Post,
    $"api/v1/assistant/actions/{response.CompletedWorkoutDraft.Id}/start-workout",
    new { });

Assert.Equal(SessionStatus.InProgress, first.Status);
Assert.Equal(first.Id, second.Id);
Assert.Equal(52.5m, first.Exercises.Single().Presets[0].WeightKg);
```

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run:

```powershell
dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter "FullyQualifiedName~CompletedWorkoutDraftStartsActiveWorkoutOnlyAfterExplicitApproval"
```

Expected: FAIL z kodem 404 dla nieistniejącego endpointu.

- [ ] **Step 3: Dodać endpoint**

Endpoint ma:

- pobrać szkic należący do użytkownika;
- zwrócić poprzednio utworzoną sesję przy ponownym żądaniu;
- odrzucić wygasły lub nieoczekujący szkic;
- nie tworzyć drugiej sesji, jeśli użytkownik ma inną aktywną sesję;
- utworzyć `WorkoutExercise` z liczbą presetów, minimalną i maksymalną liczbą powtórzeń oraz presetami kg/powtórzenia/RIR;
- oznaczyć szkic jako zatwierdzony dopiero po zapisaniu sesji.

Sygnatura:

```csharp
[HttpPost("actions/{id:guid}/start-workout")]
[ValidateAntiForgeryToken]
public async Task<ActionResult<WorkoutSessionResponse>> StartWorkout(
    Guid id,
    CancellationToken cancellationToken)
```

- [ ] **Step 4: Dodać metodę klienta i uruchomić test**

```csharp
public Task<WorkoutSessionResponse> StartWorkout(Guid draftId) =>
    Send<WorkoutSessionResponse>(
        HttpMethod.Post,
        $"api/v1/assistant/actions/{draftId}/start-workout",
        new { });
```

Run: test z kroku 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Api/Controllers/AssistantController.cs src/FormaAI.Web/Services/AssistantClient.cs tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs
git commit -m "Uruchamiać szkic AI jako aktywny trening"
```

---

### Task 3: Model szkicu ręcznego

**Files:**
- Create: `src/FormaAI.Application/Training/QuickWorkoutDraft.cs`
- Create: `tests/FormaAI.Application.Tests/QuickWorkoutDraftTests.cs`

**Interfaces:**
- Produces: `QuickWorkoutDraft`, `QuickWorkoutExerciseDraft`.
- Produces: `QuickWorkoutDraft.ToRequest()` zwracające `StartQuickWorkoutRequest`.
- Consumes: `ExerciseResponse`, `QuickWorkoutExerciseRequest`.

- [ ] **Step 1: Napisać test mapowania superserii**

```csharp
[Fact]
public void ToRequestConnectsConsecutiveExercisesIntoOneSuperset()
{
    var draft = new QuickWorkoutDraft("Góra", 50);
    draft.Exercises.Add(new(first, 3) { LinkWithNext = true });
    draft.Exercises.Add(new(second, 3) { LinkWithNext = true });
    draft.Exercises.Add(new(third, 3));

    var request = draft.ToRequest();

    Assert.NotNull(request.Exercises[0].SupersetGroupId);
    Assert.Equal(request.Exercises[0].SupersetGroupId, request.Exercises[1].SupersetGroupId);
    Assert.Equal(request.Exercises[1].SupersetGroupId, request.Exercises[2].SupersetGroupId);
    Assert.Equal([1, 2, 3], request.Exercises.Select(x => x.SupersetPosition));
}
```

Dodać drugi test, że samotne ćwiczenie nie tworzy superserii oraz walidacja blokuje pusty szkic i `MinReps > MaxReps`.

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~QuickWorkoutDraftTests"
```

Expected: FAIL, brak typów.

- [ ] **Step 3: Zaimplementować model**

Model przechowuje nazwę, czas, ćwiczenia i zwraca błędy walidacji. `ToRequest` ma grupować wyłącznie spójne łańcuchy co najmniej dwóch ćwiczeń; identyfikator grupy jest generowany raz dla całego łańcucha.

Minimalny interfejs:

```csharp
public sealed class QuickWorkoutDraft(string name = "Trening na dziś", int minutes = 45)
{
    public string Name { get; set; } = name;
    public int Minutes { get; set; } = minutes;
    public List<QuickWorkoutExerciseDraft> Exercises { get; } = [];
    public IReadOnlyList<string> Validate();
    public StartQuickWorkoutRequest ToRequest();
}
```

- [ ] **Step 4: Uruchomić testy i commit**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~QuickWorkoutDraftTests"
```

Expected: PASS.

```powershell
git add src/FormaAI.Application/Training/QuickWorkoutDraft.cs tests/FormaAI.Application.Tests/QuickWorkoutDraftTests.cs
git commit -m "Dodać model szkicu szybkiego treningu"
```

---

### Task 4: Osobna strona `/workout/new`

**Files:**
- Create: `src/FormaAI.Web/Pages/NewWorkout.razor`
- Modify: `src/FormaAI.Web/_Imports.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`

**Interfaces:**
- Consumes: `QuickWorkoutDraft`, `TrainingClient.GetExercises`, `TrainingClient.StartQuick`.
- Consumes: `AssistantClient.Send`, `AssistantClient.UpdateCompletedWorkout`, `AssistantClient.StartWorkout`.
- Produces: route `/workout/new`.

- [ ] **Step 1: Utworzyć stronę z dwoma trybami**

Strona ma zawierać:

```razor
@page "/workout/new"
@inject TrainingClient TrainingApi
@inject AssistantClient AssistantApi
@inject AccountClient Account
@inject NavigationManager Navigation

<header class="workout-builder-heading">
    <MudIconButton Href="/" Icon="@Icons.Material.Outlined.ArrowBack" aria-label="Wróć" />
    <div>
        <span class="page-kicker">Przygotuj sesję</span>
        <h1>Nowy trening</h1>
        <p>Sprawdź wszystko przed rozpoczęciem. Nic nie zapisze się bez Twojej decyzji.</p>
    </div>
</header>

<div class="workout-entry-modes">
    <button class="@ModeClass(WorkoutBuilderMode.Ai)" @onclick="() => SelectMode(WorkoutBuilderMode.Ai)">
        <MudIcon Icon="@Icons.Material.Filled.AutoAwesome" />
        <strong>Dodaj z AI</strong>
        <span>Opisz ćwiczenia, serie i ciężary.</span>
    </button>
    <button class="@ModeClass(WorkoutBuilderMode.Manual)" @onclick="() => SelectMode(WorkoutBuilderMode.Manual)">
        <MudIcon Icon="@Icons.Material.Outlined.EditNote" />
        <strong>Dodaj ręcznie</strong>
        <span>Wybierz ćwiczenia i ustaw parametry.</span>
    </button>
</div>
```

Domyślnie tryb wyboru nie tworzy sesji.

- [ ] **Step 2: Zaimplementować tryb ręczny**

Każdy wybrany element udostępnia: serie, min/max powtórzeń, RIR, przerwę, interwał i „Połącz z następnym”. Lista pozwala przesuwać ćwiczenie w górę/dół oraz usuwać je. Akcja wywołuje `draft.ToRequest()`, blokuje wielokrotne kliknięcie i nawiguje do `/workout/{session.Id}`.

- [ ] **Step 3: Zaimplementować tryb AI**

Opis trafia do `AssistantClient.Send`. Odpowiedź bez `CompletedWorkoutDraft` pokazuje tekst AI i pozwala doprecyzować opis. Odpowiedź ze szkicem wyświetla:

- nazwę;
- ćwiczenia;
- edytowalne kg, powtórzenia i RIR każdej serii;
- usuwanie serii i ćwiczeń;
- akcję „Rozpocznij ten trening”.

Przed startem wywołać `UpdateCompletedWorkout`, następnie `StartWorkout`, a dopiero potem nawigować do sesji.

- [ ] **Step 4: Dodać styl mobile-first**

Style muszą zapewnić:

- maksymalną szerokość `760px`;
- dwie karty trybów na desktopie i jedną kolumnę na telefonie;
- przyklejoną dolną akcję startową na małym ekranie;
- czytelną tabelę parametrów bez poziomego przewijania przy `360px`;
- widoczny fokus, stan `loading`, błędy przy właściwej sekcji;
- brak dialogu i brak zachowania `position: fixed` dla całej strony.

- [ ] **Step 5: Build**

Run:

```powershell
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
```

Expected: build bez błędów.

- [ ] **Step 6: Commit**

```powershell
git add src/FormaAI.Web/Pages/NewWorkout.razor src/FormaAI.Web/_Imports.razor src/FormaAI.Web/wwwroot/css/app.css
git commit -m "Dodać osobny kreator treningu"
```

---

### Task 5: Usunięcie starego panelu i spójny routing

**Files:**
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Create: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Produces: wszystkie wejścia „Wpisz trening” prowadzą do `/workout/new`.
- Consumes: `TrainingClient.GetActiveSession`.

- [ ] **Step 1: Napisać test regresyjny źródła widoku**

Test odczytuje `Home.razor` z katalogu rozwiązania i potwierdza:

```csharp
Assert.Contains("Href=\"/workout/new\"", source);
Assert.DoesNotContain("quick-workout-builder", source);
Assert.DoesNotContain("ToggleQuickWorkout", source);
Assert.DoesNotContain("_quickWorkoutOpen", source);
```

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run:

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
```

Expected: FAIL, ponieważ stary panel nadal istnieje.

- [ ] **Step 3: Usunąć lokalny kreator z `Home.razor`**

Usunąć markup, pola i metody starego szybkiego treningu. Przycisk ma używać:

```razor
<MudButton Href="@WorkoutEntryUrl"
           Variant="Variant.Outlined"
           StartIcon="@Icons.Material.Outlined.EditNote">
    @(_activeSession is null ? "Wpisz trening" : "Wznów trening")
</MudButton>
```

oraz:

```csharp
private string WorkoutEntryUrl =>
    _activeSession is null ? "/workout/new" : $"/workout/{_activeSession.Id}";
```

- [ ] **Step 4: Usunąć nieużywane style i uruchomić test**

Usunąć selektory ograniczone do starego `.quick-workout-builder`. Zachować style używane przez nową stronę tylko wtedy, gdy mają nową nazwę `workout-builder-*`.

Run: test z kroku 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Pages/Home.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Przenieść wpisywanie treningu na osobną stronę"
```

---

### Task 6: Presety i dopracowanie widoku stricte workout

**Files:**
- Modify: `src/FormaAI.Web/Pages/Workout.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Modify: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: `WorkoutExerciseResponse.Presets`.
- Produces: `ApplyPreset(WorkoutExerciseResponse, SetForm)`.

- [ ] **Step 1: Rozszerzyć test regresyjny**

Test ma potwierdzać obecność elementów sesji:

```csharp
Assert.Contains("workout-motion-hero", source);
Assert.Contains("workout-superset-strip", source);
Assert.Contains("swap-exercise-trigger", source);
Assert.Contains("ApplyNextPreset", source);
```

Uruchomić test i potwierdzić FAIL wyłącznie dla `ApplyNextPreset`.

- [ ] **Step 2: Podstawiać presety AI**

Przy pierwszym załadowaniu i po zapisaniu serii wybrać preset o numerze kolejnej serii:

```csharp
private static void ApplyNextPreset(WorkoutExerciseResponse exercise, SetForm form)
{
    var nextNumber = exercise.Sets.Select(x => x.SetNumber).DefaultIfEmpty(0).Max() + 1;
    var preset = exercise.Presets.FirstOrDefault(x => x.SetNumber == nextNumber);
    if (preset is null) return;
    form.WeightKg = preset.WeightKg;
    form.Repetitions = preset.Repetitions;
    form.Rir = preset.Rir;
}
```

Preset ma mieć pierwszeństwo przed historią ćwiczenia. Edycja zapisanej serii nadal używa wartości zapisanych.

- [ ] **Step 3: Dopracować hierarchię sesji**

Na telefonie kolejność ma być stała:

1. media;
2. nazwa i postęp;
3. przerwa/interwał;
4. tabela serii;
5. superseria;
6. główna akcja serii;
7. wymiana i pozostałe opcje.

Na desktopie zachować tę samą kolejność oraz ograniczenie szerokości. Nie dodawać nowej palety ani dolnej nawigacji kopiującej referencyjną aplikację.

- [ ] **Step 4: Uruchomić test i build**

```powershell
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"
dotnet build src/FormaAI.Web/FormaAI.Web.csproj --nologo
```

Expected: PASS i build bez błędów.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Pages/Workout.razor src/FormaAI.Web/wwwroot/css/app.css tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs
git commit -m "Dopracować pełny widok sesji treningowej"
```

---

### Task 7: Weryfikacja końcowa

**Files:**
- Verify: całe rozwiązanie

- [ ] **Step 1: Sprawdzić różnice i format**

```powershell
git diff main...HEAD --check
git status --short
```

Expected: brak błędów whitespace i brak nieoczekiwanych plików.

- [ ] **Step 2: Uruchomić pełny build**

```powershell
dotnet build FormaAI.sln --nologo -v:minimal
```

Expected: 0 błędów.

- [ ] **Step 3: Uruchomić wszystkie testy**

```powershell
dotnet test FormaAI.sln --no-build --nologo -v:minimal
```

Expected: wszystkie testy przechodzą.

- [ ] **Step 4: Weryfikacja manualna**

Sprawdzić:

- `/` nie rozwija żadnego kreatora;
- kliknięcie „Wpisz trening” otwiera `/workout/new`;
- AI pokazuje szkic bez zapisu;
- ręczny szkic tworzy sesję;
- szkic AI tworzy sesję dopiero po kliknięciu;
- `/workout/{id}` pokazuje media, presety, timers, serie, superserię i wymianę;
- odświeżenie nie tworzy drugiej sesji.

- [ ] **Step 5: Przygotować gałąź do scalenia**

```powershell
git log --oneline main..HEAD
git status --short
```

Expected: czysta gałąź z osobnymi polskimi commitami.
