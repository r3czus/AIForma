# FormaAI mobile live-workout final fix report

Date: 2026-07-29

Branch: `feature/mobile-live-workout`

Review range: `28d99e87424e919c6649e77d8a2038dabd239e01..4d3e79c`

## Status

All eight Important findings and all four Minor findings were implemented and verified. No finding was skipped.

## TDD evidence

### RED — missing helper bootstrap

Command:

```text
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutSwipeDecisionTests|FullyQualifiedName~WorkoutNavigationSourceTests"
```

Exact outcome:

```text
Exit code: 1
WorkoutSwipeDecisionTests.cs(12,22): error CS0103: Nazwa „WorkoutSwipeDecision” nie istnieje w bieżącym kontekście
WorkoutSwipeDecisionTests.cs(24,22): error CS0103: Nazwa „WorkoutSwipeDecision” nie istnieje w bieżącym kontekście
WorkoutSwipeDecisionTests.cs(35,22): error CS0103: Nazwa „WorkoutSwipeDecision” nie istnieje w bieżącym kontekście
WorkoutSwipeDecisionTests.cs(45,22): error CS0103: Nazwa „WorkoutSwipeDecision” nie istnieje w bieżącym kontekście
```

Only a compile-time shell returning the current index was then added so the tests could demonstrate a behavioral failure rather than stop at a missing symbol.

### RED — behavioral and source-contract failures

Command:

```text
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutSwipeDecisionTests|FullyQualifiedName~WorkoutNavigationSourceTests"
```

Exact outcome:

```text
Exit code: 1
Niepowodzenie! — niepowodzenie: 8, powodzenie: 19, pominięto: 0, łącznie: 27, czas trwania: 229 ms
```

Expected failures:

- `HorizontalSwipeMovesExactlyOneExercise` failed for all four direction/threshold cases because the shell returned index `2` instead of `1` or `3`.
- `LiveWorkoutUsesGestureAwareExerciseHero` failed because the hero did not use `WorkoutSwipeDecision.TargetIndex`.
- `LiveWorkoutProvidesAnInPlaceFullHistorySheet` failed because isolated `LoadHistoryAsync` state/retry behavior did not exist.
- `LiveWorkoutKeepsSecondarySessionControlsInTheFocusedOptionsSheet` failed because secondary mutation guards/busy states did not exist.
- `LiveWorkoutIsolatesAuxiliaryFailuresAndKeepsPerformanceStateTruthful` failed because catalog/history isolation, truthful e1RM, scroll locking, and related contracts did not exist.

### GREEN — focused regression suite

Command:

```text
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --filter "FullyQualifiedName~WorkoutSwipeDecisionTests|FullyQualifiedName~WorkoutNavigationSourceTests"
```

Exact outcome:

```text
Exit code: 0
Powodzenie! — niepowodzenie: 0, powodzenie: 27, pominięto: 0, łącznie: 27, czas trwania: 476 ms
```

The swipe tests exercise:

- movement below the 56 px threshold;
- exact-threshold acceptance;
- horizontal movement in both directions;
- very large movement still moving exactly one exercise;
- vertical/diagonal rejection;
- first/last exercise edge rejection.

The source contracts cover:

- non-blocking catalog/history initialization and explicit error/retry states;
- custom media controls and stopped pointer propagation;
- per-operation busy guards, loading controls, catches, and finally recovery;
- positive-only live/history e1RM rendering;
- full history ARIA tab semantics and exact technique route;
- body scroll locking, contained sheet overscroll, dark swap surface, active-set outline;
- mixed-session cardio placement and swap completed/planned state.

## Finding-by-finding resolution

1. **Dark swap sheet**
   - Replaced `.workout-swap-sheet { background: #fff; }` with `var(--surface)` and `var(--ink)`.
   - Applied theme-aware text colors to swap inputs, labels, selects, title, rows, and footer through existing palette variables.
   - Existing `:root[data-theme="dark"]` and system-dark variables now flow through the entire sheet.

2. **Auxiliary loading isolation**
   - `OnParametersSetAsync` now awaits only the essential session request.
   - `GetExercises` runs through caught `LoadCatalogAsync`; failure keeps active set logging available and exposes retry UI in swap and add-exercise surfaces.
   - History runs through caught `LoadHistoryAsync` with per-exercise `NotLoaded/Loading/Loaded/Error` state.
   - The full history sheet renders explicit loading, error, retry, empty, and data states without failing the route.
   - Last-performance data and safe initial form seeding are restored when history arrives; seeding only occurs if the user has not changed the fields.
   - A catalog fallback `ExerciseResponse` keeps the hero/history route usable even when the catalog is unavailable.

3. **Media/swipe interaction**
   - Removed native video controls.
   - Added an explicit custom reversible play/pause control for video and GIF.
   - Added `formaMotion.setMediaPlayback` using the native media API; no dependency was added.
   - Video/image hit testing passes through to the swipe surface, while media controls and hero action controls stop pointer/click propagation.
   - Hero swipe behavior delegates to the production `WorkoutSwipeDecision` helper.

4. **e1RM header**
   - Combines current working sets with loaded history.
   - Ignores zero/negative weight and invalid repetition data.
   - Returns nullable output and renders the label only for a positive estimate, including the completion list; `0` is never presented as a result.

5. **Background scroll**
   - `body:has(.workout-sheet-backdrop)` locks overflow and disables body overscroll for every workout sheet.
   - History content, swap results, options content, and the superset builder use contained overscroll.
   - Stable scrollbar gutters prevent desktop layout/scroll-context drift while a backdrop is mounted.

6. **Secondary mutation guards/recovery**
   - `AddExercise`, `SaveNotes`, `Complete`, and `Abandon` each have an early busy return, per-action busy flag, disabled/loading controls, caught `HttpRequestException`, snackbar feedback, and `finally` reset.
   - Add-exercise selection and workout notes are preserved on failure.
   - Completion and abandonment cross-disable each other to prevent duplicate or competing terminal requests.
   - Replacement also gained the missing `_swapping` early return.
   - Post-completion progression/calorie requests are isolated from the successful completion mutation.

7. **Meaningful tests**
   - Added production `FormaAI.Application.Training.WorkoutSwipeDecision`.
   - Added real xUnit behavioral tests for threshold, vertical rejection, both edges, direction, and one-step movement.
   - Updated the hero to use the helper.
   - Added source-contract assertions for auxiliary states, retry UI, error catches, busy/finally recovery, e1RM truthfulness, media controls, scroll lock, and the Minor findings.
   - RED/GREEN evidence is recorded above.

8. **Mixed cardio/strength**
   - Removed the active-session cardio summary from above the hero.
   - Mixed-session cardio now appears in the ellipsis/options sheet.
   - Completion and cardio-only surfaces retain cardio details and guarded completion/abandon controls.

9. **Technique**
   - Added a visible button linking exactly to `/training/exercises/{Exercise.Id}`.

10. **History tabs**
    - Added `tablist`, `tab`, `aria-selected`, `aria-controls`, matching tab IDs, `tabpanel`, matching panel IDs, and `aria-labelledby`.

11. **Swap summary**
    - Current exercise summary now shows `completed/planned` sets plus the repetition range.

12. **Active set row**
    - Added a flat action-colored inset outline: `box-shadow: inset 0 0 0 2px var(--action)`.

## Files changed

- `src/FormaAI.Application/Training/WorkoutSwipeDecision.cs`
- `src/FormaAI.Web/Components/Training/ExerciseMediaFrame.razor`
- `src/FormaAI.Web/Components/Training/WorkoutExerciseHero.razor`
- `src/FormaAI.Web/Components/Training/WorkoutHistorySheet.razor`
- `src/FormaAI.Web/Pages/Workout.razor`
- `src/FormaAI.Web/wwwroot/css/app.css`
- `src/FormaAI.Web/wwwroot/index.html`
- `tests/FormaAI.Application.Tests/WorkoutSwipeDecisionTests.cs`
- `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`
- `.superpowers/sdd/final-fix-report.md`

## Full verification

### Build

Command:

```text
dotnet build FormaAI.sln
```

Exact outcome:

```text
Exit code: 0
Kompilacja powiodła się.
Ostrzeżenia: 0
Liczba błędów: 0
Czas: 00:00:09.11
```

### Full solution tests with normal build semantics

Command:

```text
dotnet test FormaAI.sln
```

Exact outcome:

```text
Exit code: 0
FormaAI.Domain.Tests: 19 passed, 0 failed, 0 skipped
FormaAI.Application.Tests: 99 passed, 0 failed, 0 skipped
FormaAI.Api.IntegrationTests: 33 passed, 0 failed, 0 skipped
Total: 151 passed, 0 failed, 0 skipped
```

The in-memory integration host emitted its existing request-body-size-feature warnings; they did not fail any test.

### Diff checks

Commands:

```text
git diff --check
git diff --cached --check
```

Exact outcome:

```text
Exit code: 0
No whitespace errors.
```

## Self-review

- Re-read all 12 findings against the final source; each maps to a concrete implementation and a contract/test assertion.
- Confirmed no new package or third-party dependency.
- Confirmed native video controls are absent and both media/hero controls stop pointer propagation.
- Confirmed catalog/history tasks are not awaited by route initialization and both catch `HttpRequestException`.
- Confirmed history states distinguish loading/error/data and retry does not close/reset the workout.
- Confirmed last-performance display and history-based e1RM refresh after asynchronous history completion.
- Confirmed all four secondary mutations have early return, busy flag, disabled/loading UI, catch, local feedback, and reset.
- Confirmed form/notes preservation on failure and completion/abandon cross-guarding.
- Confirmed active mixed cardio is only in the ellipsis sheet, while cardio-only and completion surfaces remain.
- Confirmed dark swap surface and inputs use theme variables.
- Confirmed every workout sheet scroll region contains overscroll and any backdrop locks body scrolling.
- Confirmed technique URL and ARIA tab/panel ID mappings are exact.
- Confirmed swap completed/planned state and active action-colored inset outline.
- Confirmed fresh focused tests, full build, and full normal-semantics solution tests all pass.

## Concerns

- This worker session did not launch an authenticated browser session, so physical pointer feel, iOS overscroll, and visual dark-theme rendering were verified by compiled Razor/CSS contracts rather than an end-to-end browser pass.
- The integration-test request-body-size warnings come from the in-memory test host and predate this change; all integration tests pass.

## Follow-up: raw-input versus delayed-history race

The final re-review found one additional Important race: a `MudNumericField`
with deferred model updates could contain focused, in-progress text while the
bound `SetForm` still matched the defaults captured before an asynchronous
history request. The completing request could therefore treat the form as
untouched and seed over the input.

### Follow-up TDD evidence

The tests were added before the production decision helper and Razor changes.

First RED:

```text
dotnet test tests/FormaAI.Application.Tests/FormaAI.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~WorkoutSetSeedDecisionTests|FullyQualifiedName~LiveWorkoutMarksEverySetNumberInputDirtyBeforeDelayedSeeding" --verbosity minimal
Exit code: 1
WorkoutSetSeedDecisionTests.cs(7,29): error CS0246: WorkoutSetSeedSnapshot could not be found
```

After adding only a compile shell whose decisions returned `false`, the
behavioral/source-contract RED was:

```text
Exit code: 1
3 failed, 3 passed, 6 total
```

The failures proved that:

- untouched defaults were not yet eligible for delayed history;
- a pre-interaction preset was not yet eligible;
- none of the three numeric controls marked raw input interaction.

Focused GREEN:

```text
Exit code: 0
0 failed, 6 passed, 6 total
```

### Follow-up implementation

- Added `WorkoutSetSeedDecision` and `WorkoutSetSeedSnapshot` as a real
  application-level policy with behavioral xUnit coverage.
- Added sticky `SetForm.UserInteracted` state. It is set by a bubbling raw
  `@oninput` handler around each of the weight, repetitions, and RIR controls,
  so even invalid or not-yet-parsed text blocks later seeding.
- Set `Immediate="true"` on all three controls as supplemental model
  freshness; the dirty guard remains authoritative.
- Delayed history now requires: seeding requested, no user interaction, no
  completed set, and an unchanged captured model snapshot.
- AI preset application now refuses after user interaction, while initial
  presets and untouched history defaults remain eligible.
- Added a source contract that counts every numeric control in the active set
  row and requires a matching raw-input dirty wrapper and `Immediate="true"`.

### Follow-up full verification

```text
dotnet build FormaAI.sln --no-restore --verbosity minimal
Exit code: 0
Build succeeded: 0 errors, 0 warnings

dotnet test FormaAI.sln --no-build --verbosity minimal
Exit code: 0
FormaAI.Domain.Tests: 19 passed
FormaAI.Application.Tests: 105 passed
FormaAI.Api.IntegrationTests: 33 passed
Total: 157 passed, 0 failed, 0 skipped

git diff --check
Exit code: 0
```

Follow-up files:

- `src/FormaAI.Application/Training/WorkoutSetSeedDecision.cs`
- `src/FormaAI.Web/Pages/Workout.razor`
- `src/FormaAI.Web/wwwroot/css/app.css`
- `tests/FormaAI.Application.Tests/WorkoutSetSeedDecisionTests.cs`
- `tests/FormaAI.Application.Tests/WorkoutNavigationSourceTests.cs`
