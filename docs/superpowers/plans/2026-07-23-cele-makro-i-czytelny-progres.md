# Cele, makro i czytelny progres — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Poprawić kreator celu, algorytm makro, daty i procenty progresu oraz skróty dodawania posiłku zgodnie z zaakceptowaną specyfikacją.

**Architecture:** Obliczenia celu i kwalifikujących się dni trafiają do testowalnych klas w warstwie Application, a API pozostaje jedynym źródłem procentów i daty obowiązywania celu. Komponenty Blazor odpowiadają wyłącznie za natychmiastową prezentację danych, dostępne stany rozwinięcia i nawigację do asystenta.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, Entity Framework Core, xUnit.

## Global Constraints

- Zachować istniejący kierunek wizualny Forma Signal.
- Projektować krótkie, czytelne przepływy odpowiednie dla telefonu.
- Nie zapisywać kluczy API w repozytorium ani kodzie frontendu.
- Automatyczne wyliczenie zachowuje `2 g/kg` białka, `0,9 g/kg` tłuszczu i co najmniej 20% energii z węglowodanów.
- Brak wpisu obniża wynik tylko dla dnia, który już nastąpił i miał obowiązujący cel.
- Przyszłe dni oraz dni sprzed pierwszego celu nie wchodzą do mianownika.
- Propozycja asystenta zawsze wymaga zatwierdzenia użytkownika.
- Każdy zamknięty moduł kończy się polskim commitem.

---

### Task 1: Testowalne wyliczanie celu żywieniowego

**Files:**
- Create: `src/FormaAI.Application/Nutrition/NutritionGoalCalculator.cs`
- Modify: `tests/FormaAI.Application.Tests/NutritionCalculatorTests.cs`
- Modify: `src/FormaAI.Web/Pages/Profile.razor`

**Interfaces:**
- Produces: `NutritionGoalCalculator.Calculate(decimal weightKg, decimal heightCm, int age, BiologicalSex sex, ActivityLevel activity, BodyGoal goal, decimal weeklyWeightChangeKg) : Macro`
- Consumes: `Macro` z `FormaAI.Application.Nutrition`.

- [ ] **Step 1: Dodać testy RED dla trzech celów i przypadku zerowych węglowodanów**

```csharp
[Theory]
[InlineData(BodyGoal.Reduction)]
[InlineData(BodyGoal.Maintenance)]
[InlineData(BodyGoal.MuscleGain)]
public void GoalCalculationAlwaysLeavesEnergyForCarbohydrates(BodyGoal goal)
{
    var result = NutritionGoalCalculator.Calculate(150m, 181m, 35, BiologicalSex.Male, ActivityLevel.Low, goal, .7m);

    Assert.True(result.CarbohydratesG > 0);
    Assert.True(result.CarbohydratesG * 4m >= result.CaloriesKcal * .2m - 4m);
}
```

- [ ] **Step 2: Uruchomić test i potwierdzić oczekiwaną porażkę**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter GoalCalculationAlwaysLeavesEnergyForCarbohydrates`

Expected: FAIL, ponieważ `NutritionGoalCalculator` jeszcze nie istnieje.

- [ ] **Step 3: Zaimplementować minimalny kalkulator**

```csharp
public static Macro Calculate(decimal weightKg, decimal heightCm, int age, BiologicalSex sex, ActivityLevel activity, BodyGoal goal, decimal weeklyWeightChangeKg)
{
    var bmr = 10m * weightKg + 6.25m * heightCm - 5m * age + (sex == BiologicalSex.Male ? 5m : -161m);
    var factor = activity switch { ActivityLevel.Low => 1.2m, ActivityLevel.Light => 1.32m, ActivityLevel.Moderate => 1.45m, _ => 1.6m };
    var weeklyEnergy = weeklyWeightChangeKg * 7700m;
    var adjustment = goal switch { BodyGoal.Reduction => -weeklyEnergy / 7m, BodyGoal.MuscleGain => weeklyEnergy / 7m, _ => 0m };
    var protein = decimal.Round(weightKg * 2m);
    var fat = decimal.Round(weightKg * .9m);
    var macroFloor = decimal.Ceiling((protein * 4m + fat * 9m) / .8m);
    var calories = decimal.Round(Math.Max(Math.Max(1200m, bmr * factor + adjustment), macroFloor));
    var carbs = decimal.Round((calories - protein * 4m - fat * 9m) / 4m);
    return new Macro(calories, protein, fat, carbs);
}
```

- [ ] **Step 4: Podłączyć kreator profilu do kalkulatora**

```csharp
var macro = NutritionGoalCalculator.Calculate(
    _currentWeightKg!.Value, _heightCm!.Value, age, _sex!.Value,
    _workActivity!.Value, _goal!.Value, _weeklyWeightChangeKg);
_targetCalories = macro.CaloriesKcal;
_proteinG = macro.ProteinG;
_fatG = macro.FatG;
_carbsG = macro.CarbohydratesG;
```

- [ ] **Step 5: Uruchomić testy aplikacyjne**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/FormaAI.Application/Nutrition/NutritionGoalCalculator.cs tests/FormaAI.Application.Tests/NutritionCalculatorTests.cs src/FormaAI.Web/Pages/Profile.razor
git commit -m "Popraw wyliczanie kalorii i makro"
```

### Task 2: Data celu według profilu i odświeżenie wyniku

**Files:**
- Modify: `src/FormaAI.Api/Controllers/NutritionController.cs`
- Modify: `src/FormaAI.Web/Pages/Profile.razor`
- Modify: `tests/FormaAI.Api.IntegrationTests/NutritionFlowTests.cs`

**Interfaces:**
- Produces: `POST api/v1/nutrition-targets` zapisujący `EffectiveFrom` jako lokalne „dzisiaj” użytkownika.
- Consumes: `NutritionTargetResponse` zwracany przez `NutritionClient.SaveTarget`.

- [ ] **Step 1: Dodać test integracyjny RED dla strefy `Europe/Warsaw`**

Test rejestruje użytkownika w `Europe/Warsaw`, wysyła cel z datą różną od lokalnego dnia i potwierdza, że odpowiedź zawiera datę wyliczoną z profilu.

```csharp
var expected = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
    DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw")));
var saved = await Send<SaveNutritionTargetRequest, NutritionTargetResponse>(
    client, "api/v1/nutrition-targets", new(expected.AddDays(-5), 2200, 160, 70, 230));
Assert.Equal(expected, saved.EffectiveFrom);
```

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter NutritionTargetUsesProfileLocalDate`

Expected: FAIL — API zachowuje datę z żądania.

- [ ] **Step 3: Normalizować datę po stronie API**

W `SaveTarget` pobrać `var effectiveFrom = await LocalToday(userId);` i przekazać ją do konstruktora `NutritionTarget` zamiast `request.EffectiveFrom`.

- [ ] **Step 4: Odświeżyć stan profilu po zapisie**

```csharp
var saved = await Nutrition.SaveTarget(new SaveNutritionTargetRequest(
    DateOnly.FromDateTime(DateTime.Today), _targetCalories!.Value, _proteinG, _fatG, _carbsG));
_targetCalories = saved.CaloriesKcal;
_proteinG = saved.ProteinG;
_fatG = saved.FatG;
_carbsG = saved.CarbohydratesG;
_targetEffectiveFrom = saved.EffectiveFrom;
```

W wyniku kreatora pokazać `_targetEffectiveFrom` jako „Cel obowiązuje od …”.

- [ ] **Step 5: Uruchomić test integracyjny**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter NutritionTargetUsesProfileLocalDate`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/FormaAI.Api/Controllers/NutritionController.cs src/FormaAI.Web/Pages/Profile.razor tests/FormaAI.Api.IntegrationTests/NutritionFlowTests.cs
git commit -m "Zapisuj cel od lokalnej daty użytkownika"
```

### Task 3: Kwalifikujące się dni i prawdziwy procent progresu

**Files:**
- Create: `src/FormaAI.Application/Progress/ProgressPeriodCalculator.cs`
- Create: `tests/FormaAI.Application.Tests/ProgressPeriodCalculatorTests.cs`
- Modify: `src/FormaAI.Contracts/Progress/ProgressContracts.cs`
- Modify: `src/FormaAI.Api/Controllers/ProgressController.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/ProgressFlowTests.cs`
- Modify: `src/FormaAI.Web/Pages/Progress.razor`

**Interfaces:**
- Produces: `ProgressPeriodCalculator.EligibleRange(DateOnly from, DateOnly to, DateOnly today, DateOnly? firstTargetDate) : (DateOnly From, DateOnly To)?`
- Produces: `NutritionWeekSummaryResponse.EligibleDays`.
- Consumes: pierwszą datę celu i lokalne „dzisiaj” użytkownika.

- [ ] **Step 1: Dodać testy RED zakresu**

```csharp
[Fact]
public void ExcludesFutureDaysAndDaysBeforeFirstTarget()
{
    var range = ProgressPeriodCalculator.EligibleRange(
        new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31),
        new DateOnly(2026, 7, 18), new DateOnly(2026, 7, 5));

    Assert.Equal(new DateOnly(2026, 7, 5), range!.Value.From);
    Assert.Equal(new DateOnly(2026, 7, 18), range.Value.To);
}
```

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter ProgressPeriodCalculatorTests`

Expected: FAIL, ponieważ kalkulator nie istnieje.

- [ ] **Step 3: Zaimplementować minimalny kalkulator zakresu**

```csharp
public static (DateOnly From, DateOnly To)? EligibleRange(DateOnly from, DateOnly to, DateOnly today, DateOnly? firstTargetDate)
{
    if (firstTargetDate is null) return null;
    var start = DateOnly.FromDayNumber(Math.Max(from.DayNumber, firstTargetDate.Value.DayNumber));
    var end = DateOnly.FromDayNumber(Math.Min(to.DayNumber, today.DayNumber));
    return start > end ? null : (start, end);
}
```

- [ ] **Step 4: Rozszerzyć kontrakt podsumowania**

```csharp
public sealed record NutritionWeekSummaryResponse(
    int LoggedDays, int EligibleDays, int DaysOnCalories, decimal CalorieAdherencePercent,
    decimal AverageCalories, decimal AverageTargetCalories, int DaysOnProtein,
    decimal AverageProteinG, decimal AverageCalorieDifference);
```

- [ ] **Step 5: Poprawić agregację API**

W `WeekSummary`:

- wyliczyć lokalne `today` ze strefy profilu;
- znaleźć pierwszą datę celu;
- utworzyć zakres kwalifikujący się;
- ustawić `eligibleDays` jako liczbę dni zakresu;
- liczyć `CalorieAdherencePercent = DaysOnCalories * 100 / EligibleDays`;
- obciąć wyliczenie `PlannedWorkouts` do lokalnego dzisiaj dla bieżącego okresu.

- [ ] **Step 6: Dodać test integracyjny `1 / 18`**

Test ustawia cel od 1 lipca, zapisuje jeden dzień w tolerancji, pobiera zakres 1–18 lipca i sprawdza:

```csharp
Assert.Equal(18, summary.Nutrition.EligibleDays);
Assert.Equal(1, summary.Nutrition.DaysOnCalories);
Assert.Equal(5.6m, summary.Nutrition.CalorieAdherencePercent);
```

- [ ] **Step 7: Zmienić licznik w interfejsie**

Wyświetlać `DaysOnCalories / EligibleDays`, pozostawiając `LoggedDays` jako osobny opis. Szerokość paska nadal korzysta z `CalorieAdherencePercent`.

```razor
<strong>@_summary.Nutrition.DaysOnCalories / @_summary.Nutrition.EligibleDays dni w celu</strong>
<p>@_summary.Nutrition.LoggedDays zapisanych dni · @NutritionNote</p>
```

- [ ] **Step 8: Uruchomić testy progresu**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter ProgressPeriodCalculatorTests`

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Progress`

Expected: PASS.

- [ ] **Step 9: Commit**

```powershell
git add src/FormaAI.Application/Progress/ProgressPeriodCalculator.cs tests/FormaAI.Application.Tests/ProgressPeriodCalculatorTests.cs src/FormaAI.Contracts/Progress/ProgressContracts.cs src/FormaAI.Api/Controllers/ProgressController.cs tests/FormaAI.Api.IntegrationTests/ProgressFlowTests.cs src/FormaAI.Web/Pages/Progress.razor
git commit -m "Licz progres tylko dla dostępnych dni"
```

### Task 4: Natychmiastowa podpowiedź masy

**Files:**
- Create: `src/FormaAI.Application/Progress/WeightGoalCopy.cs`
- Create: `tests/FormaAI.Application.Tests/WeightGoalCopyTests.cs`
- Modify: `src/FormaAI.Web/Pages/Profile.razor`

**Interfaces:**
- Produces: `WeightGoalCopy.For(decimal currentKg, decimal targetKg) : string`.
- Consumes: poprawnie sparsowane wartości pól masy.

- [ ] **Step 1: Dodać testy RED treści**

```csharp
[Theory]
[InlineData(80, 75, "Do zrzucenia 5 kg")]
[InlineData(75, 80, "Do przybrania 5 kg")]
[InlineData(80, 80, "Cel zakłada utrzymanie obecnej masy")]
public void DescribesWeightDifference(decimal current, decimal target, string expected) =>
    Assert.Equal(expected, WeightGoalCopy.For(current, target));
```

- [ ] **Step 2: Uruchomić test i potwierdzić RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WeightGoalCopyTests`

Expected: FAIL.

- [ ] **Step 3: Zaimplementować formatowanie**

```csharp
public static string For(decimal currentKg, decimal targetKg)
{
    var difference = decimal.Round(targetKg - currentKg, 1);
    if (Math.Abs(difference) < .05m) return "Cel zakłada utrzymanie obecnej masy";
    return difference < 0
        ? $"Do zrzucenia {Math.Abs(difference):0.#} kg"
        : $"Do przybrania {difference:0.#} kg";
}
```

- [ ] **Step 4: Parsować oba pola podczas renderowania podpowiedzi**

```csharp
private string? LiveWeightDifference
{
    get
    {
        if (!TryDecimal(_currentWeightInput, 20, 500, out var current)) return null;
        if (!TryDecimal(_targetWeightInput, 20, 500, out var target)) return null;
        return WeightGoalCopy.For(current!.Value, target!.Value);
    }
}
```

W widoku renderować komunikat tylko wtedy, gdy `LiveWeightDifference` nie jest pusty.

- [ ] **Step 5: Uruchomić testy i commit**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter WeightGoalCopyTests`

Expected: PASS.

```powershell
git add src/FormaAI.Application/Progress/WeightGoalCopy.cs tests/FormaAI.Application.Tests/WeightGoalCopyTests.cs src/FormaAI.Web/Pages/Profile.razor
git commit -m "Dodaj podpowiedź zmiany masy na żywo"
```

### Task 5: Czytelny progres i pojedynczy stan kalendarza

**Files:**
- Modify: `src/FormaAI.Web/Pages/Progress.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Consumes: `EligibleDays` i poprawione procenty z Task 3.
- Produces: czytelne stany wizualne bez zmiany API.

- [ ] **Step 1: Ustalić bazową weryfikację wizualną**

Uruchomić aplikację i zapisać zrzuty szerokości około 390 px oraz 1280 px dla zakładek „Na dziś”, „Dieta” i „Trening”.

- [ ] **Step 2: Poprawić kontrast wartości**

```css
.summary-card-top strong,
.metric-row strong { color: var(--ink); }
.progress-section-metrics strong { color: var(--inverse-ink); }
.progress-section-metrics span,
.progress-section-metrics small { color: color-mix(in srgb, var(--inverse-ink) 75%, transparent); }
```

- [ ] **Step 3: Usunąć nakładające się obramowania dnia**

```css
.nutrition-day.today { box-shadow: inset 0 0 0 2px var(--fuel); }
.nutrition-day.selected,
.nutrition-day.today.selected { border-color: var(--action); box-shadow: inset 0 0 0 2px var(--action); }
.nutrition-day:focus-visible { outline: 3px solid var(--focus); outline-offset: 2px; }
```

- [ ] **Step 4: Zweryfikować paski**

Sprawdzić w narzędziach przeglądarki, że `1 / 18` renderuje element `<i>` z szerokością `5.6%`, a `18 / 18` z `100%`.

- [ ] **Step 5: Build i commit**

Run: `dotnet build src/FormaAI.Web/FormaAI.Web.csproj`

Expected: PASS.

```powershell
git add src/FormaAI.Web/Pages/Progress.razor src/FormaAI.Web/wwwroot/css/forma-signal.css
git commit -m "Popraw czytelność i stany progresu"
```

### Task 6: Zwinięte „Dodaj ponownie” i rozmowa o posiłku

**Files:**
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Produces: `_recentMealsOpen` sterujący `aria-expanded`.
- Produces: `MealSuggestionUrl` wskazujący `/assistant` z zakodowanym parametrem `prompt`.
- Consumes: `_day.Remaining` i `_day.Date`.

- [ ] **Step 1: Dodać domyślnie zamknięty stan listy**

```razor
<button type="button" class="saved-meals-toggle"
        aria-expanded="@_recentMealsOpen"
        @onclick="() => _recentMealsOpen = !_recentMealsOpen">
    <span><strong>Dodaj ponownie</strong><small>@_recentMeals.Count zapisanych dań</small></span>
    <MudIcon Icon="@(_recentMealsOpen ? Icons.Material.Filled.KeyboardArrowUp : Icons.Material.Filled.KeyboardArrowDown)" />
</button>
@if (_recentMealsOpen)
{
    <div class="recent-meal-list saved-meal-list">
        @foreach (var recent in _recentMeals.Take(6))
        {
            <button type="button" @onclick="() => CopyMeal(recent)">
                <MudIcon Icon="@Icons.Material.Outlined.RestaurantMenu" />
                <span>
                    <strong>@RecentName(recent.Name)</strong>
                    <small>B @recent.Macro.ProteinG.ToString("0") g · T @recent.Macro.FatG.ToString("0") g · W @recent.Macro.CarbohydratesG.ToString("0") g</small>
                </span>
                <strong>@recent.Macro.CaloriesKcal.ToString("0") kcal</strong>
                <MudIcon Icon="@Icons.Material.Outlined.Add" />
            </button>
        }
    </div>
}
```

Pole `private bool _recentMealsOpen;` pozostaje domyślnie `false`.

- [ ] **Step 2: Zbudować prompt asystenta z pozostałego makro**

```csharp
private string MealSuggestionUrl
{
    get
    {
        var remaining = _day?.Remaining;
        var prompt = $"Zaproponuj mi posiłek na {_day?.Date:dd.MM.yyyy}. " +
            $"Pozostało około {Math.Max(0, remaining?.CaloriesKcal ?? 0):0} kcal, " +
            $"B {Math.Max(0, remaining?.ProteinG ?? 0):0} g, " +
            $"T {Math.Max(0, remaining?.FatG ?? 0):0} g i " +
            $"W {Math.Max(0, remaining?.CarbohydratesG ?? 0):0} g. " +
            "Najpierw porozmawiaj ze mną i pokaż propozycję. Niczego nie zapisuj bez mojego zatwierdzenia.";
        return $"/assistant?prompt={Uri.EscapeDataString(prompt)}";
    }
}
```

W `CoachUrl` użyć `MealSuggestionUrl` dla akcji „Zaproponuj posiłek”.

- [ ] **Step 3: Dodać style nagłówka rozwijanego**

`.saved-meals-toggle` ma pełną szerokość, minimalną wysokość 52 px, jawny fokus i nie udaje osobnej karty. Na telefonie tekst oraz ikona pozostają w jednym wierszu.

- [ ] **Step 4: Build i ręczna weryfikacja**

Run: `dotnet build src/FormaAI.Web/FormaAI.Web.csproj`

Expected: PASS.

Sprawdzić:

- „Dodaj ponownie” jest zamknięte po wejściu;
- `aria-expanded` zmienia się przy kliknięciu;
- CTA otwiera `/assistant`;
- prompt rozpoczyna rozmowę i zawiera datę oraz pozostałe makro.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Pages/AddMeal.razor src/FormaAI.Web/Pages/Home.razor src/FormaAI.Web/wwwroot/css/forma-signal.css
git commit -m "Połącz propozycje posiłków z asystentem"
```

### Task 7: Pełna weryfikacja rozwiązania

**Files:**
- Verify: `FormaAI.sln`
- Verify: wszystkie pliki zmienione w Tasks 1–6.

**Interfaces:**
- Consumes: komplet zmian.
- Produces: potwierdzenie gotowości bez nowych zmian funkcjonalnych.

- [ ] **Step 1: Uruchomić pełny build**

Run: `dotnet build FormaAI.sln`

Expected: exit code 0.

- [ ] **Step 2: Uruchomić pełny zestaw testów**

Run: `dotnet test FormaAI.sln --no-build`

Expected: exit code 0, zero nieudanych testów.

- [ ] **Step 3: Sprawdzić zakres zmian**

Run: `git status --short`

Run: `git diff c5395fb --check`

Expected: brak błędów whitespace; pliki użytkownika spoza zakresu pozostają nietknięte.

- [ ] **Step 4: Zweryfikować widoki telefonu i komputera**

Sprawdzić kreator profilu, wynik makro, progres `1 / 18`, zaznaczenie dnia, zakładki diety i treningu, „Dodaj ponownie” i przejście do rozmowy.

- [ ] **Step 5: Zamknąć wdrożenie**

Jeżeli weryfikacja wymaga wyłącznie drobnej poprawki regresji, dodać ją do odpowiadającego modułu i ponownie uruchomić pełny build oraz testy. Nie tworzyć osobnego commita bez zmian.
