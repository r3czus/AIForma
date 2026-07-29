# FormaAI Mobile Follow-up Fixes Design

## Goal

Resolve the reported mobile usability and cross-device defects in the food diary,
progress photos, workout capture, iPhone notifications, and exercise library
without changing the product's existing visual language.

## Scope

### Food diary actions

- Keep meal-slot copy actions in the existing overflow menus.
- Give overflow icons and expand/collapse icons an explicit theme-aware color so
  that they remain visible in the dark theme.
- Keep the expand/collapse control inside the meal heading action group and
  prevent it from overlapping macro values on narrow screens.
- Align both expanded and collapsed arrow states consistently.

### Progress-photo navigation and cross-device display

- Align the progress-photo back action with the page heading instead of letting
  it appear as a detached centered arrow.
- Return root-relative authenticated photo content URLs from the API.
- Render those URLs directly in the web client and reload the server-backed
  collection after uploads.
- Keep photo files in server storage and metadata in the database, so every
  signed-in device reads the same records rather than relying on browser-local
  object URLs.
- Preserve the current private, authenticated content endpoint.

### Dashboard simplification

- Remove the `Zapytaj asystenta` button from the dashboard hero.
- Keep the global assistant entry point and other AI-assisted capture flows
  unchanged.

### Historical completed-workout capture

- Keep the existing `Rozpocznij trening` action for live sessions.
- Add a neighboring `Zapisz trening` action for manually completed workouts.
- Reuse the existing manual workout builder for exercise and set entry.
- In historical-save mode, the final action opens a date dialog.
- Allow today or any earlier date; reject future dates in both the web client
  and API.
- Saving creates a completed workout directly. It must not leave an in-progress
  session or redirect the user into the live workout flow.
- Persist the selected local date as the workout's completed date using the
  user's configured time zone.
- Show the saved workout in workout history and progress summaries.
- Keep the normal live-session start flow unchanged.

## iPhone PWA notifications

- Treat iOS web push as available only when the app is running in installed
  standalone mode and the platform exposes the required service-worker,
  notification, and push APIs.
- Wait for the service worker to be ready before requesting a subscription.
- Request notification permission from an explicit user gesture.
- Reuse a valid existing subscription or create one with the configured VAPID
  public key, then persist it through the authenticated coaching endpoint.
- Return structured JavaScript results instead of allowing interop exceptions to
  surface as the generic application error banner.
- Show one of four actionable states: install the PWA, grant permission,
  notifications active, or a specific recoverable error.
- Keep the test-notification action explicit and never claim success unless the
  browser subscription and server save both succeed.

## Exercise library

- Add a case-insensitive search field above the exercise list.
- Search exercise name, primary muscle group, equipment, and description.
- Show an empty-search state when no exercises match.
- Replace the dense text-only rows with media-led cards derived from the active
  workout presentation: exercise image or animation, name, muscle engagement,
  equipment, and a concise technique summary.
- Keep full details and editing available through the existing exercise details
  flow.
- Preserve the add-exercise action and ownership rules for global and
  user-created exercises.

## Architecture and data flow

- UI-only food, navigation, dashboard, and library changes remain in the Blazor
  web project and shared CSS.
- Photo URL construction remains centralized in `CoachingController`.
- Historical workout capture receives an explicit API contract and controller
  endpoint rather than simulating a live session in the browser.
- Date conversion is centralized in application code and uses the saved user
  time zone.
- Notification capability detection and subscription orchestration stay in the
  existing JavaScript settings module; persistence stays in the coaching API.

## Error handling

- Photo content returns `404` when metadata or storage content is missing and
  never exposes filesystem paths.
- Historical workout saves validate ownership, exercise availability, set
  values, and future dates before creating any records.
- Failed historical saves leave the draft intact and show a concise message.
- Notification setup maps unsupported platform, non-standalone iOS, denied
  permission, missing VAPID configuration, and subscription failure to distinct
  user-facing messages.

## Testing

- Add API regression coverage for root-relative photo URLs and authenticated
  photo retrieval.
- Add application and API tests for direct completed-workout saving, selected
  local dates, future-date rejection, history visibility, and absence of an
  in-progress session.
- Add source-level web regressions for the paired workout actions, dashboard
  assistant removal, exercise search, card presentation, and notification state
  handling where rendered component tests are not available.
- Run focused tests after each change, then the full solution tests and a release
  web build.
- Verify the affected pages at a narrow iPhone-sized viewport, including dark
  theme contrast and control alignment.

## Out of scope

- Scheduling future workouts.
- Replacing the existing assistant feature.
- Public progress-photo sharing or third-party object storage.
- Redesigning live workout execution, which was handled separately.
