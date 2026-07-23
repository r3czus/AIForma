# Skill Observation Log

Observations captured during task-oriented work.

**Status key:** OPEN = not yet actioned | ACTIONED (YYYY-MM-DD) = skill updated/created | DECLINED (YYYY-MM-DD) = user decided not to pursue

---

## 2026-07-23

### Observation 1: Keep observer state outside audited repositories when writes are out of scope

**Status:** OPEN
**Date:** 2026-07-23
**Session context:** Read-only design audit of an existing frontend before user approval.
**Skill:** task-observer
**Type:** open-source
**Phase/Area:** Session Start Protocol

**Issue:** The mandatory initialization of observation files introduces repository changes during an otherwise read-only audit, even when the task explicitly postpones product code changes.

**Suggested improvement:** Let the Session Start Protocol default to a stable user-level workspace state directory, or explicitly allow deferred initialization when the active task forbids repository writes.

**Principle:** Meta-workflow state should not contaminate a user's working tree during read-only analysis unless the user has chosen repository-local storage.

### Observation 2: Define a visual-only exception path for TDD

**Status:** OPEN
**Date:** 2026-07-23
**Session context:** Comprehensive visual redesign using CSS and declarative UI markup while preserving application behavior.
**Skill:** test-driven-development
**Type:** open-source
**Phase/Area:** When to Use / Exceptions

**Issue:** The iron law requires a failing unit test before any production edit, but pure CSS and declarative visual hierarchy changes often have no meaningful unit-level behavior to test. Creating source-text assertions would add brittle tests without increasing confidence.

**Suggested improvement:** Add an explicit visual-only path requiring a clean behavioral baseline, compilation, deterministic design linting, responsive screenshots, interaction checks, and final regression tests instead of synthetic unit tests.

**Principle:** Verification should match the risk being changed; visual work needs rendered-state evidence, not artificial source-shape tests.

### Observation 3: Treat explicit delegated execution as direction approval

**Status:** OPEN
**Date:** 2026-07-23
**Session context:** The user supplied detailed visual defects, asked the agent to choose the best solution, implement it fully, and merge it without another checkpoint.
**Skill:** brainstorming
**Type:** open-source
**Phase/Area:** User approval gate

**Issue:** The approval hard gate requires a second confirmation even when the user has explicitly delegated the design decision and already authorized end-to-end implementation. Stopping for another approval would contradict the requested autonomous workflow without reducing meaningful risk.

**Suggested improvement:** Define explicit delegated approval as valid when the user asks the agent to select the recommended approach and execute it, provided the agent states the chosen direction and stays within the supplied scope.

**Principle:** Approval gates should distinguish missing consent from clearly delegated judgment; repeated confirmation is friction when authority and scope are already explicit.

---

Checkpoint after 6 completed implementation items: no additional skill observations.

Checkpoint after the approved design batch: no additional skill observations.

Checkpoint after six implementation modules: no additional skill observations.

Final deliverable checkpoint after review fixes, verification, and merge to main: no additional skill observations.

### Observation 4: Define partial application when a named skill excludes the target

**Status:** OPEN
**Date:** 2026-07-23
**Session context:** A user explicitly requested a redesign skill whose own scope excludes dashboards, while the target was a dense product application.
**Skill:** design-taste-frontend
**Type:** open-source
**Phase/Area:** Out of Scope / Redesign Protocol

**Issue:** The skill says to point to a different tool for dashboards, but does not explain how to honor an explicit request to use it alongside a more suitable product-UI skill.

**Suggested improvement:** Add a partial-application protocol: state which sections remain useful, apply only those sections, name the primary skill for the excluded surface, and still run the applicable pre-flight checks.

**Principle:** When a requested methodology only partially fits, preserve its relevant constraints while making scope boundaries and primary decision authority explicit.

### Observation 5: Distinguish spreadsheet ingestion from workbook authoring

**Status:** OPEN
**Date:** 2026-07-23
**Session context:** Importing structured rows from an existing XLSX file into an application database without changing or exporting the workbook.
**Skill:** spreadsheets
**Type:** open-source
**Phase/Area:** Tools + Contract Requirements / Decision Boundary

**Issue:** The skill mandates a workbook authoring runtime and says to report a blocker when it is unavailable, but does not clearly distinguish database ingestion, where the spreadsheet remains unchanged and a standard read-only OOXML parser is sufficient.

**Suggested improvement:** Add a read-only ingestion path that permits platform-native OOXML parsing when no workbook is authored, edited, rendered, or exported, while retaining artifact-tool requirements for workbook deliverables.

**Principle:** Tooling requirements should follow the artifact being changed; reading tabular input into another system does not require an authoring runtime when the source workbook remains immutable.

Checkpoint after backup, transactional migration, integrity checks, test suite, push, and service restart: no additional skill observations.
