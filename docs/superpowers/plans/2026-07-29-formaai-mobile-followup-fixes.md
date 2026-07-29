# FormaAI Mobile Follow-up Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the reported mobile UI defects, cross-device photos, dated completed-workout capture, iPhone PWA push, and exercise-library usability.

**Architecture:** Keep presentation fixes in the Blazor web layer, fix private media addressing at the API boundary, and add an explicit completed-workout command instead of simulating a live session. Centralize local-date-to-UTC conversion so both manual and AI-completed workouts use the account time zone. Keep push capability detection in JavaScript and return structured results to Blazor.

**Tech Stack:** .NET 8, ASP.NET Core Web API, EF Core, Blazor WebAssembly, MudBlazor, JavaScript service workers, xUnit.

## Global Constraints

- `Rozpocznij trening` remains the live-session action.
- `Zapisz trening` records a completed session for today or a past date and never leaves an in-progress session.
- Future completed-workout dates are rejected by UI and API.
- Progress photos stay private behind authenticated API endpoints.
- iPhone push requires an installed standalone PWA.
- Preserve existing assistant entry points except the dashboard `Zapytaj asystenta` button.

---

### Task 1: Mobile food and progress navigation polish

**Files:**
- Modify: `src/FormaAI.Web/Pages/Food.razor`
- Modify: `src/FormaAI.Web/Pages/ProgressPhotos.razor`
- Modify: `src/FormaAI.Web/Pages/Home.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: existing MudBlazor menu and icon-button markup.
- Produces: stable `.meal-slot-actions`, `.meal-slot-overflow`, and `.progress-photo-heading` layout hooks.

- [ ] **Step 1: Write failing source regressions**

Add assertions that `Home.razor` does not contain `Zapytaj asystenta`, that
meal actions have named layout hooks, and that the progress-photo header has a
dedicated alignment class.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutNavigationSourceTests"`

Expected: FAIL because the new layout hooks are absent and the dashboard button
still exists.

- [ ] **Step 3: Implement the markup and CSS**

Group the food menu and arrow controls:

```razor
<div class="meal-slot-actions">
    <MudMenu Class="meal-slot-overflow" ... />
    <MudIconButton Class="meal-slot-expand" ... />
</div>
```

Use explicit theme colors and a two-column mobile grid:

```css
.meal-slot-actions { align-items: center; display: flex; gap: 6px; }
.meal-slot-actions .mud-icon-root { color: var(--ink) !important; }
.meal-slot-expand { flex: 0 0 44px; }
.progress-photo-heading > .mud-icon-button { align-self: start; margin-top: 2px; }
```

Remove only the dashboard hero button whose text is `Zapytaj asystenta`.

- [ ] **Step 4: Run the focused tests and verify GREEN**

Run the command from Step 2.

Expected: PASS.

### Task 2: Private progress-photo URL correctness

**Files:**
- Modify: `src/FormaAI.Api/Controllers/CoachingController.cs`
- Modify: `src/FormaAI.Web/Pages/ProgressPhotos.razor`
- Test: `tests/FormaAI.Api.IntegrationTests/ProgressFlowTests.cs`

**Interfaces:**
- Produces: `ProgressPhotoResponse.Url` in the form `/api/v1/coaching/photos/{id}/content`.
- Consumes: the existing authenticated content endpoint and server-side storage.

- [ ] **Step 1: Write a failing API regression**

Upload a photo, assert that its response URL starts with `/api/`, fetch the URL
with the authenticated client, and assert the original media type and bytes.

- [ ] **Step 2: Run the focused test and verify RED**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter "FullyQualifiedName~ProgressFlowTests"`

Expected: FAIL because the response currently returns `api/...` without the
root slash.

- [ ] **Step 3: Fix the API URL and refresh behavior**

Change the response mapping to:

```csharp
new(x.Id, x.LocalDate, x.Pose, $"/api/v1/coaching/photos/{x.Id}/content");
```

After a successful upload batch, reload the collection from the API and clear
the browser file selection. Keep rendering the server URL directly.

- [ ] **Step 4: Run the focused test and verify GREEN**

Run the command from Step 2.

Expected: PASS.

### Task 3: Direct completed-workout capture with a selected date

**Files:**
- Create: `src/FormaAI.Application/Training/WorkoutLocalDate.cs`
- Modify: `src/FormaAI.Contracts/Training/TrainingContracts.cs`
- Modify: `src/FormaAI.Domain/Training/TrainingModels.cs`
- Modify: `src/FormaAI.Application/Training/QuickWorkoutDraft.cs`
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Api/Controllers/AssistantController.cs`
- Modify: `src/FormaAI.Web/Services/TrainingClient.cs`
- Modify: `src/FormaAI.Web/Pages/NewWorkout.razor`
- Test: `tests/FormaAI.Application.Tests/QuickWorkoutDraftTests.cs`
- Test: `tests/FormaAI.Application.Tests/WorkoutLocalDateTests.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`
- Test: `tests/FormaAI.Api.IntegrationTests/AssistantFlowTests.cs`

**Interfaces:**
- Produces: `SaveCompletedWorkoutRequest(LocalDate, Name, Exercises)`.
- Produces: `TrainingClient.SaveCompleted(SaveCompletedWorkoutRequest)`.
- Produces: `WorkoutLocalDate.Resolve(DateOnly, TimeZoneInfo, DateTime utcNow)`.
- Consumes: `CompletedWorkoutSetRequest(WeightKg, Repetitions, Rir)`.

- [ ] **Step 1: Write failing application tests**

Cover local date conversion in `Europe/Warsaw`, future-date rejection, manual
draft conversion to concrete repeated completed sets, and preservation of quick
workout presets.

- [ ] **Step 2: Run application tests and verify RED**

Run: `dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutLocalDateTests|FullyQualifiedName~QuickWorkoutDraftTests"`

Expected: FAIL because the command and conversion helper do not exist.

- [ ] **Step 3: Add contracts and date conversion**

Add:

```csharp
public sealed record CompletedWorkoutSetRequest(decimal WeightKg, int Repetitions, decimal? Rir);
public sealed record CompletedWorkoutExerciseRequest(
    Guid ExerciseId,
    string ExerciseName,
    IReadOnlyList<CompletedWorkoutSetRequest> Sets);
public sealed record SaveCompletedWorkoutRequest(
    DateOnly LocalDate,
    string Name,
    IReadOnlyList<CompletedWorkoutExerciseRequest> Exercises);
```

Resolve the selected local date at noon in the configured time zone and reject
dates after the user's local today.

- [ ] **Step 4: Extend the manual draft**

Add `WeightKg` and `CompletedRepetitions` to each manual exercise. Include them
as quick-session presets and expose `ToCompletedRequest(DateOnly)` for direct
history capture.

- [ ] **Step 5: Add failing API tests**

Assert that saving a completed workout:

- returns `SessionStatus.Completed`;
- stores `StartedAtUtc` and `FinishedAtUtc` on the chosen local date;
- persists concrete sets;
- appears in history;
- leaves `/active` as `404`;
- rejects a future date without inserting a session.

Also assert that AI-confirmed completed workouts honor their payload date.

- [ ] **Step 6: Run API tests and verify RED**

Run: `dotnet test tests/FormaAI.Api.IntegrationTests/FormaAI.Api.IntegrationTests.csproj --filter "FullyQualifiedName~TrainingFlowTests|FullyQualifiedName~AssistantFlowTests"`

Expected: FAIL because direct completed save is unavailable and AI completion
uses the current timestamp.

- [ ] **Step 7: Implement the endpoint and timestamped domain finish**

Add `POST api/v1/workout-sessions/completed`. Validate ownership and all set
values before constructing the session. Add timestamp-aware session creation
and completion while retaining current-time defaults for live workouts. Reuse
`WorkoutLocalDate.Resolve` in the AI confirmation path.

- [ ] **Step 8: Add the date dialog and paired actions**

Both reviewed AI drafts and valid manual drafts show `Zapisz trening` beside
`Rozpocznij trening`. `Zapisz trening` opens a MudBlazor dialog/sheet with a
date picker capped at today. Confirming calls the appropriate completed-save
API and navigates to training history. A failed request keeps the draft and
dialog state intact.

- [ ] **Step 9: Run focused application and API tests**

Run both commands from Steps 2 and 6.

Expected: PASS.

### Task 4: Reliable iPhone PWA push setup

**Files:**
- Modify: `src/FormaAI.Web/wwwroot/js/forma-settings.js`
- Modify: `src/FormaAI.Web/wwwroot/service-worker.published.js`
- Modify: `src/FormaAI.Web/Pages/ProfileSettings.razor`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Produces JavaScript result:
  `{ status: "active"|"install-required"|"unsupported"|"denied"|"error", subscription?, message? }`.
- Consumes: VAPID public key and existing `PushSubscriptionRequest`.

- [ ] **Step 1: Write failing source regressions**

Assert that the published worker registers `push` and `notificationclick`, the
settings module detects standalone iOS, and the profile page handles structured
statuses.

- [ ] **Step 2: Run focused tests and verify RED**

Run the Task 1 test command.

Expected: FAIL because the published worker lacks push handlers and JavaScript
returns `null` on every failure.

- [ ] **Step 3: Implement structured setup**

Check `serviceWorker`, `Notification`, `PushManager`, and standalone display
mode. Request permission only inside the button handler, wait for
`navigator.serviceWorker.ready`, reuse or create a subscription, and catch all
errors into the structured result. Copy push and click handlers into the
published service worker.

- [ ] **Step 4: Update the profile state UI**

Render actionable alerts for installation, denied permission, active push, and
recoverable error. Save the subscription server-side only for `active`.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run the Task 1 test command.

Expected: PASS.

### Task 5: Searchable media-led exercise library

**Files:**
- Modify: `src/FormaAI.Api/Controllers/TrainingController.cs`
- Modify: `src/FormaAI.Web/Pages/Training.razor`
- Modify: `src/FormaAI.Web/wwwroot/css/app.css`
- Modify: `src/FormaAI.Web/wwwroot/css/forma-signal.css`
- Test: `tests/FormaAI.Api.IntegrationTests/TrainingFlowTests.cs`
- Test: `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`

**Interfaces:**
- Consumes: `TrainingClient.GetExercises(query)` and `ExerciseMediaFrame`.
- Produces: server search across name, description, muscle group, and equipment.

- [ ] **Step 1: Write failing API and source tests**

Add API cases for description, muscle, and equipment search. Add source
assertions for a library search field, media frame, empty state, and dedicated
card classes.

- [ ] **Step 2: Run focused tests and verify RED**

Run the Task 1 source test and Task 3 API test commands.

Expected: FAIL because library search is absent and server filtering uses name
only.

- [ ] **Step 3: Implement server and client search**

Extend the contains-mode query predicate to include description and enum text
mapped through normalized Polish labels. Add a debounced library search that
does not interfere with the plan-builder search state.

- [ ] **Step 4: Implement workout-style library cards**

Use `ExerciseMediaFrame` as the card hero, show engagement and equipment in
compact metadata, clamp the technique summary, and keep edit/details actions.
Render a clear no-results state.

- [ ] **Step 5: Run focused tests and verify GREEN**

Run both commands from Step 2.

Expected: PASS.

### Task 6: Full verification and mobile inspection

**Files:**
- Verify all files changed above.

- [ ] **Step 1: Run formatting and diff checks**

Run: `git diff --check`

Expected: no output and exit code `0`.

- [ ] **Step 2: Run the complete test suite**

Run: `dotnet test FormaAI.sln`

Expected: all tests pass with zero failures.

- [ ] **Step 3: Build the release web application**

Run: `dotnet build src/FormaAI.Web/FormaAI.Web.csproj -c Release`

Expected: build succeeds with zero errors.

- [ ] **Step 4: Inspect narrow mobile layouts**

Start the existing application stack and inspect `/food`, `/progress/photos`,
`/workout/new`, `/profile/settings/reminders`, and `/training` at an iPhone-sized
viewport in dark theme. Verify visible copy menus, aligned arrows, the date
dialog, notification states, and exercise cards/search.

- [ ] **Step 5: Review the requirement checklist**

Match every bullet in
`docs/superpowers/specs/2026-07-29-formaai-mobile-followup-fixes-design.md`
to implemented code and fresh verification evidence.
