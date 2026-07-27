# Rozszerzenie jedzenia Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Uprościć dziennik jedzenia i dodać bezpieczne kopiowanie posiłku z wybranego dnia i sekcji do innego dnia i sekcji.

**Architecture:** Kopiowanie działa po stronie API i klonuje migawkowe pozycje posiłku w jednej transakcji. Frontend używa jednego dwustopniowego selektora źródła i celu; kliknięcie wiersza prowadzi do istniejącej edycji, kopiowanie trafia do menu, a usuwanie pozostaje bezpośrednie.

**Tech Stack:** .NET 8, ASP.NET Core API, Blazor WebAssembly, MudBlazor, EF Core, SQL Server, xUnit.

## Global Constraints

- Kopiowanie nigdy nie zmienia ani nie usuwa źródła.
- Użytkownik wybiera datę, posiłek źródłowy, datę docelową i sekcję docelową.
- Operacja jest idempotentna.
- Kliknięcie całego wiersza otwiera edycję.
- Usuwanie pozostaje widoczne jak obecnie, kopiowanie znajduje się w menu z trzema kropkami.
- Wielozdjęciowa analiza i skalowanie kalorii pozostają dostępne.

---

### Task 1: Domena i kontrakt kopiowania

**Files:**
- Modify: `src/FormaAI.Domain/Nutrition/NutritionModels.cs`
- Modify: `src/FormaAI.Contracts/Nutrition/NutritionContracts.cs`
- Create: `tests/FormaAI.Domain.Tests/MealCopyTests.cs`

**Interfaces:**
- Produces: `Meal CopyTo(DateTime occurredAtUtc, DateOnly localDate, string targetSlot)`.
- Produces: `CopyMealRequest(DateOnly TargetDate, string TargetSlot, Guid OperationId)`.

- [ ] **Step 1: Write failing clone tests**

```csharp
[Fact]
public void Copy_creates_new_identifiers_and_preserves_snapshots()
{
    var source = MealFixture.Create("Śniadanie · Owsianka");
    var copy = source.CopyTo(DateTime.UtcNow.AddDays(1), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), "Lunch");
    Assert.NotEqual(source.Id, copy.Id);
    Assert.Equal(source.Items.Single().AmountGrams, copy.Items.Single().AmountGrams);
    Assert.StartsWith("Lunch", copy.Name);
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter MealCopyTests`
Expected: FAIL because `CopyTo` does not exist.

- [ ] **Step 3: Implement clone semantics**

Create a new `Meal`, rewrite only the slot prefix, and create new `MealItem` instances from every snapshot. Keep amounts, macro and `IsEstimated`.

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/FormaAI.Domain.Tests/FormaAI.Domain.Tests.csproj --filter MealCopyTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Domain/Nutrition/NutritionModels.cs src/FormaAI.Contracts/Nutrition/NutritionContracts.cs tests/FormaAI.Domain.Tests/MealCopyTests.cs
git commit -m "Dodać kopiowanie posiłku w domenie"
```

### Task 2: Transakcyjne i idempotentne API

**Files:**
- Modify: `src/FormaAI.Api/Controllers/NutritionController.cs`
- Modify: `src/FormaAI.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/FormaAI.Infrastructure/Persistence/Migrations/<timestamp>_AddMealCopyOperations.cs`
- Modify: `tests/FormaAI.Api.IntegrationTests/NutritionFlowTests.cs`

**Interfaces:**
- Produces: `POST /api/v1/meals/{id}/copy`.
- Produces: `MealCopyOperation` keyed by `(UserId, OperationId)`.

- [ ] **Step 1: Add failing integration tests**

```csharp
[Fact]
public async Task Copy_meal_clones_items_to_target_without_changing_source()
{
    var source = await CreateMeal("Śniadanie · Owsianka");
    var target = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    var copy = await Send<CopyMealRequest, MealResponse>(_client, HttpMethod.Post, $"api/v1/meals/{source.Id}/copy", new(target, "Lunch", Guid.NewGuid()));
    Assert.NotEqual(source.Id, copy.Id);
    Assert.StartsWith("Lunch", copy.Name);
    Assert.Equal(source.Items.Single().AmountGrams, copy.Items.Single().AmountGrams);
    var originalDay = await _client.GetFromJsonAsync<NutritionDayResponse>($"api/v1/nutrition/days/{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");
    Assert.Contains(originalDay!.Meals, x => x.Id == source.Id);
}

[Fact]
public async Task Repeated_operation_id_returns_same_copy()
{
    var source = await CreateMeal("Obiad · Ryż");
    var request = new CopyMealRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), "Kolacja", Guid.NewGuid());
    var first = await Send<CopyMealRequest, MealResponse>(_client, HttpMethod.Post, $"api/v1/meals/{source.Id}/copy", request);
    var second = await Send<CopyMealRequest, MealResponse>(_client, HttpMethod.Post, $"api/v1/meals/{source.Id}/copy", request);
    Assert.Equal(first.Id, second.Id);
}

[Fact]
public async Task Cannot_copy_another_users_meal()
{
    var source = await CreateMeal("Obiad · Ryż");
    var response = await _other.SendAsync(await Request(_other, HttpMethod.Post, $"api/v1/meals/{source.Id}/copy",
        new CopyMealRequest(DateOnly.FromDateTime(DateTime.UtcNow), "Obiad", Guid.NewGuid())));
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Copy_meal`
Expected: FAIL because endpoint does not exist.

- [ ] **Step 3: Implement operation entity, endpoint and migration**

```csharp
[HttpPost("meals/{id:guid}/copy")]
[ValidateAntiForgeryToken]
public async Task<ActionResult<MealResponse>> CopyMeal(Guid id, CopyMealRequest request)
```

Validate ownership and target slot, reuse an existing operation result when `OperationId` repeats, clone and save in one database transaction.

Run: `dotnet ef migrations add AddMealCopyOperations --project src/FormaAI.Infrastructure --startup-project src/FormaAI.Api`

- [ ] **Step 4: Verify GREEN**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter Copy_meal`
Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Api/Controllers/NutritionController.cs src/FormaAI.Infrastructure/Persistence src/FormaAI.Domain/Nutrition/NutritionModels.cs tests/FormaAI.Api.IntegrationTests/NutritionFlowTests.cs
git commit -m "Dodać bezpieczne kopiowanie posiłków"
```

### Task 3: Dwustopniowy selektor kopiowania

**Files:**
- Create: `src/FormaAI.Web/Components/Nutrition/MealCopyDialog.razor`
- Modify: `src/FormaAI.Web/Services/NutritionClient.cs`
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`

**Interfaces:**
- Consumes: `NutritionClient.CopyMeal(Guid mealId, CopyMealRequest request)`.
- Produces: workflow źródło -> cel -> podsumowanie -> kopiowanie.

- [ ] **Step 1: Add client method**

```csharp
public Task<MealResponse> CopyMeal(Guid id, CopyMealRequest request) =>
    Send<MealResponse>(HttpMethod.Post, $"api/v1/meals/{id}/copy", request);
```

- [ ] **Step 2: Build the dialog**

Step 1 loads `GetDay(sourceDate)` and selects one source meal. Step 2 selects target date and one profile meal slot. A stable `OperationId` is created when the dialog opens and reused on retry.

- [ ] **Step 3: Wire “Kopiuj do” and “Kopiuj z”**

Put copying in a three-dot menu on the meal/section. Do not move the delete button into the menu.

- [ ] **Step 4: Verify**

Run: `dotnet build FormaAI.sln`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Components/Nutrition/MealCopyDialog.razor src/FormaAI.Web/Services/NutritionClient.cs src/FormaAI.Web/Pages/Home.razor src/FormaAI.Web/wwwroot/css/forma-signal.css
git commit -m "Dodać workflow kopiowania posiłków"
```

### Task 4: Uproszczony dziennik i pełna weryfikacja

**Files:**
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/Pages/AddMeal.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Modify: `tests/FormaAI.Api.IntegrationTests/NutritionFlowTests.cs`

**Interfaces:**
- Produces: klikalny wiersz edycji, bezpośrednie usuwanie, menu kopiowania i zachowane dodawanie AI/wielu zdjęć.

- [ ] **Step 1: Make meal rows directly editable**

Use a semantic button/link covering the copy area of the row and route to `/food/add?date=YYYY-MM-DD&edit={mealId}&slot={slot}`. Stop propagation on copy and delete actions.

- [ ] **Step 2: Simplify hierarchy**

Use separators and spacing instead of nested cards. Keep calories and macro scannable in the section heading and keep touch targets at least 44 px.

- [ ] **Step 3: Regression checks**

Verify manual add, AI text, multiple gallery images, edit, calorie scaling, copy, retry and delete.

- [ ] **Step 4: Full verification**

Run: `dotnet build FormaAI.sln`
Run: `dotnet test FormaAI.sln --no-build`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/FormaAI.Web/Pages/Home.razor src/FormaAI.Web/Pages/AddMeal.razor src/FormaAI.Web/wwwroot/css/forma-signal.css tests/FormaAI.Api.IntegrationTests/NutritionFlowTests.cs
git commit -m "Uprościć dziennik i edycję posiłków"
```

### Task 5: Integracja, merge i publikacja

**Files:**
- Modify only files required by final verification findings.

**Interfaces:**
- Produces: tested `main` deployment and public URL.

- [ ] **Step 1: Run verification**

Run: `dotnet build FormaAI.sln`
Run: `dotnet test FormaAI.sln --no-build`
Expected: build succeeds and every test passes.

- [ ] **Step 2: Review branch diff**

Run: `git diff main...HEAD --check`
Run: `git status --short`
Expected: only intentional branch changes are committed; user logs remain uncommitted.

- [ ] **Step 3: Merge into main**

Switch to `main`, merge `feature/przebudowa-treningu-jedzenia` with a merge commit, and rerun build/tests.

- [ ] **Step 4: Publish**

Restart the FormaAI API with documented LocalDB and demo-admin configuration, expose it through the existing tunnel workflow, verify local and public `/health`, then exercise one authenticated path.

- [ ] **Step 5: Handoff**

Report the `main` commit, test count, deployment URL and any preserved uncommitted user files.
