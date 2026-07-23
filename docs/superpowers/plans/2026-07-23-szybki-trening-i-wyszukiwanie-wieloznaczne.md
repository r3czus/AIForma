# Szybki trening i wyszukiwanie wieloznaczne Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dodać wyszukiwanie z `*` dla produktów i ćwiczeń, poprawić selektor porcji, umożliwić szybki trening bez planu oraz aktywować plan góra/dół.

**Architecture:** Wspólny parser wzorca pozostaje w warstwie Application, a kontrolery przekładają jego tryb na zapytania EF Core. Szybka sesja używa istniejących encji `WorkoutSession` i `WorkoutExercise`, lecz nie zapisuje identyfikatora planu ani dnia. Interfejs strony głównej korzysta z osadzonego formularza i istniejącego ekranu aktywnej sesji.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, Entity Framework Core, xUnit, SQL Server LocalDB.

## Global Constraints

- Bez wywołań API AI.
- Bez zmian schematu bazy danych.
- Zachować istniejący styl Forma Signal i obszary dotykowe co najmniej 44×44 px.
- Na końcu uruchomić `dotnet build FormaAI.sln` i `dotnet test FormaAI.sln --no-build`.

---

### Task 1: Parser wyszukiwania wieloznacznego

**Files:**
- Create: `src/FormaAI.Application/Common/WildcardSearch.cs`
- Create: `tests/FormaAI.Application.Tests/WildcardSearchTests.cs`
- Modify: `src/FormaAI.Api/Controllers/NutritionController.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`

**Interfaces:**
- Produces: `WildcardSearch.Parse(string?)` zwracające `WildcardSearchPattern` z `Value` i `Mode`.
- Consumes: kontrolery produktów i ćwiczeń używają `Contains`, `StartsWith` albo `EndsWith`.

- [ ] **Step 1: Write the failing parser tests**

```csharp
[Theory]
[InlineData("wycisk", "wycisk", WildcardSearchMode.Contains)]
[InlineData("wycisk*", "wycisk", WildcardSearchMode.StartsWith)]
[InlineData("*wycisk", "wycisk", WildcardSearchMode.EndsWith)]
[InlineData("*wycisk*", "wycisk", WildcardSearchMode.Contains)]
public void ParseRecognizesEdgeWildcards(string query, string value, WildcardSearchMode mode)
{
    Assert.Equal(new WildcardSearchPattern(value, mode), WildcardSearch.Parse(query));
}
```

- [ ] **Step 2: Run the parser tests and verify RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WildcardSearchTests`

Expected: FAIL because `WildcardSearch` does not exist.

- [ ] **Step 3: Implement the parser and use it in both controllers**

```csharp
public enum WildcardSearchMode { Contains, StartsWith, EndsWith }
public readonly record struct WildcardSearchPattern(string Value, WildcardSearchMode Mode);
```

Trim whitespace, remove only leading and trailing `*`, return an empty
`Contains` pattern for `*`, then select the matching translatable string
operation in both catalog endpoints. Product matching covers name and brand.

- [ ] **Step 4: Run application and integration tests**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Application/Common/WildcardSearch.cs tests/FormaAI.Application.Tests/WildcardSearchTests.cs src/FormaAI.Api/Controllers/NutritionController.cs src/FormaAI.Api/Controllers/TrainingController.cs
git commit -m "Dodaj wyszukiwanie z gwiazdką w katalogach"
```

### Task 2: Szybka sesja bez planu

**Files:**
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Services/TrainingClient.cs`
- Modify: `tests/FormaAI.Domain.Tests/ExerciseMuscleEngagementTests.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`

**Interfaces:**
- Produces: `WorkoutSession(string userId, string name, int? timeLimitMinutes)`.
- Produces: `POST api/v1/workout-sessions/quick` przyjmujący `StartQuickWorkoutRequest`.
- Consumes: lista `QuickWorkoutExerciseRequest(Guid ExerciseId, int Sets)`.

- [ ] **Step 1: Write failing domain and integration tests**

```csharp
var session = new WorkoutSession("user", "Trening własny", 45);
Assert.Null(session.TrainingPlanId);
Assert.Null(session.TrainingDayId);
Assert.Equal(45, session.TimeLimitMinutes);
```

Test integracyjny tworzy ćwiczenie, rozpoczyna szybką sesję z czterema seriami,
sprawdza nazwę, brak powiązań pośrednio przez sukces odpowiedzi, limit 45 minut
i cztery planowane serie.

- [ ] **Step 2: Run focused tests and verify RED**

Run: `dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj`

Expected: FAIL because the quick-session constructor does not exist.

- [ ] **Step 3: Implement contract, entity constructor and endpoint**

```csharp
public sealed record QuickWorkoutExerciseRequest(Guid ExerciseId, [Range(1, 10)] int Sets);
public sealed record StartQuickWorkoutRequest(
    [Required, MaxLength(120)] string Name,
    [Range(5, 300)] int TimeLimitMinutes,
    [MinLength(1)] IReadOnlyList<QuickWorkoutExerciseRequest> Exercises);
```

Endpoint rejects an existing in-progress session, rejects missing or inactive
exercise IDs, preserves request order, and creates workout exercises with
8–12 repetitions, 2 RIR and 90 seconds rest.

- [ ] **Step 4: Add client methods and run focused tests**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter TrainingFlowTests`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Domain/Training/TrainingModels.cs src/FormaAI.Contracts/Training/TrainingContracts.cs src/FormaAI.Api/Controllers/TrainingController.cs src/FormaAI.Web/Services/TrainingClient.cs tests/FormaAI.Domain.Tests/ExerciseMuscleEngagementTests.cs tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs
git commit -m "Dodaj szybki trening bez planu"
```

### Task 3: Interfejs jedzenia i szybkiego treningu

**Files:**
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Consumes: `TrainingClient.GetExercises(string?)`.
- Consumes: `TrainingClient.StartQuick(StartQuickWorkoutRequest)`.

- [ ] **Step 1: Add wildcard guidance to both search controls**

Place helper copy below product and exercise search:

```text
*fraza — końcówka · fraza* — początek · *fraza* — zawiera
```

- [ ] **Step 2: Fix product selector position and typography**

Align the desktop backdrop to the top with a safe top inset. Keep the mobile
panel full-screen. Set the product-name font explicitly to Onest, clamp it to
two lines and keep brand/nutrition metadata smaller.

- [ ] **Step 3: Build the inline quick-workout form**

Add `Wpisz trening` to the home workout card. The expanded section contains
duration choices, wildcard-aware exercise search, add/remove actions, a series
numeric field per selected exercise, loading/error/empty states and
`Rozpocznij trening`. On success navigate to `/workout/{session.Id}`.

- [ ] **Step 4: Run the Web build**

Run: `dotnet build src/FormaAI.Web/FormaAI.Web.csproj`

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Pages/AddMeal.razor src/FormaAI.Web/Pages/Home.razor src/FormaAI.Web/wwwroot/css/app.css src/FormaAI.Web/wwwroot/css/forma-signal.css
git commit -m "Usprawnij wybór jedzenia i szybkie wpisanie treningu"
```

### Task 4: Aktywny plan góra/dół i weryfikacja

**Files:**
- Modify runtime data only: LocalDB `FormaAI`, demo user.

**Interfaces:**
- Consumes: existing authenticated training-plan API.
- Produces: active plan `Góra/Dół 4 dni · rekompozycja`.

- [ ] **Step 1: Resolve catalog exercise IDs**

Read the LocalDB exercise catalog and map every requested movement to the
closest existing Polish exercise name. Prefer hack squat over barbell squat,
neutral/podchwyt pull-up over unrelated vertical pulls, and machine/dumbbell
variants named by the specification.

- [ ] **Step 2: Create or replace and activate the plan**

Use the authenticated FormaAI API with the demo account. Save Monday `Góra A`,
Tuesday `Dół A`, Thursday `Góra B`, Saturday `Dół B`, then call the activation
endpoint. Do not call the assistant or any AI endpoint.

- [ ] **Step 3: Run design detector once**

Run:

```powershell
node C:\Users\Jannu\.codex\skills\impeccable\scripts\detect.mjs --json src/FormaAI.Web/Pages/AddMeal.razor src/FormaAI.Web/Pages/Home.razor src/FormaAI.Web/wwwroot/css/app.css src/FormaAI.Web/wwwroot/css/forma-signal.css
```

Expected: no unresolved high-impact findings.

- [ ] **Step 4: Run full verification**

Run:

```powershell
dotnet build FormaAI.sln
dotnet test FormaAI.sln --no-build
```

Expected: build succeeds and all tests pass.

- [ ] **Step 5: Commit, push, restart and smoke-test**

```powershell
git add docs/superpowers/specs/2026-07-23-szybki-trening-i-wyszukiwanie-wieloznaczne.md docs/superpowers/plans/2026-07-23-szybki-trening-i-wyszukiwanie-wieloznaczne.md
git commit -m "Opisz szybki trening i wyszukiwanie wieloznaczne"
git push origin main
```

Restart the local app on port 5082, keep the existing Cloudflare Quick Tunnel,
verify `/health/live`, login, wildcard product/exercise searches, active plan
and the quick-session endpoint without contacting the AI API.
