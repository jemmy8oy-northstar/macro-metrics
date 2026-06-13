# SDD Process Retrospective — Phase-by-Phase Analysis

**Document:** Phase-by-phase analysis of the Spec Driven Development workflow applied to the MacroMetrics POC.

---

## Phase 1 — Vision & Planning (Issues #2, #3, #4)

### What happened
- Issue #4 (`[1c]`) used a spec questionnaire to gather product vision from the developer
- The AI raised PR #14 (`spec/project-vision`) covering vision statement, epics, features, and dependencies
- PR #14 review identified a key issue: **E1 (backend) features jumped too far ahead** — they contained implementation detail (fetcher class names, API route shapes) that belonged in Phase 5/6, not Phase 1

### What worked
- The questionnaire format forced the developer to articulate the product vision before any code was written
- The epics + features file structure (`docs/epics/`, `docs/features/`) gave a clean, navigable artefact
- The developer's review comment ("should have been UI/UX design tickets, not implementation tickets") was exactly the right feedback to get at this stage — the PR was the right mechanism to surface it

### What didn't work
- **Backend feature detail too early**: The AI included route names, class names, and API contracts in the Phase 1 spec. This should have been placeholders. The template's `[1c]` prompt didn't sufficiently constrain the AI to stay high-level for backend features.
- **EF Core assumption baked in**: The acceptance criteria for `[1c]` mentioned "EF Core entities" and "Mermaid ER diagram" as expected outputs. For a stateless proxy product, this was irrelevant and caused the AI to produce artefacts it then had to walk back.
- **No explicit "what phase does this belong in?" guidance**: The AI didn't have a clear signal that implementation-level detail should be deferred to Phase 5/6.

### Phase verdict
✅ Mostly successful — caught the main error in PR review. The PR review loop was the right mechanism.

---

## Phase 2 — UI/UX Design (Issues #18–#24, #5)

### What happened
- Issue #5 (`[2a]`) triggered the AI to create one design issue per feature area (issues #18–#24)
- All seven design issues were covered in a single PR #27 (`design/homepage-phase2`)
- PR #27 included ASCII mockups, Mermaid workflow diagrams, a data flow section, and a UX decisions table

### What worked
- **ASCII mockups were effective**: They forced a conversation about layout, component placement, and user flow without needing any build step. The developer could critique specific layout decisions in PR comments.
- **UX decisions table**: The PR author flagged this as valuable ("I like how we have included the question resolutions here") — capturing why alternatives were rejected alongside the chosen approach.
- **All seven features in one PR**: Kept the design phase cohesive and allowed cross-feature consistency to be reviewed at once.

### What didn't work
- **Design issues were somewhat redundant**: Once the AI raised PR #27, the individual `[2]` issues became mostly reference items. The actual design conversation happened on the PR, not on the issues. The issue → PR linkage worked, but the issue bodies themselves added less value than expected.
- **No explicit "non-obvious interaction" prompt**: The mockups covered static states well but workflow diagrams could have gone deeper on edge cases (e.g. what happens when a metric has no data for the requested date range?).

### Phase verdict
✅ Successful — the ASCII mockup approach was validated as effective for a data-heavy dashboard.

---

## Phase 3 — Frontend User Stories (Issues #6, #7)

### What happened
- Issue #6 (`[3a]`) triggered the AI to raise PR #30 covering tech decisions + BDD user stories
- This phase had the most iterative PR review of any phase — the PR required multiple rounds:
  - Round 1: Developer asked "what does BDD stand for?" and flagged missing API skeleton
  - Round 2: Developer rejected MSW in favour of a Faker backend skeleton
  - Round 3: Developer asked about frontend TDD and whether it was in the template
  - Round 4: Developer approved and asked about template updates
  - Round 5: Discussion about whether [3b] should also include the backend skeleton PR
- PR #30 required a PR split (PRs #29 and #31 were also raised to manage scope)

### What worked
- **Iterative refinement in PR comments**: The PR comment loop was effective for discovering what was missing (API skeleton, TDD section, BDD definition)
- **Tech decisions document**: `docs/tech-decisions-frontend.md` was a useful artefact — it forced explicit library choices (Recharts, date-fns, etc.) before any component was written
- **Faker backend as part of [3b]**: The decision to have [3b] create the backend skeleton alongside frontend issues was a good one — it meant the frontend could make real HTTP calls from day one

### What didn't work
- **[3a] scope was under-specified**: The initial issue template didn't mention the API skeleton or TDD section — both were discovered through review. This created 4+ rounds of back-and-forth that could have been designed upfront.
- **PR had to be split into three**: PR #29 (3a tech decisions split), PR #30 (combined stories + tech), PR #31 (guidance updates) — the splitting overhead was avoidable with a clearer initial brief.
- **BDD not defined in the template**: A reviewer had to ask "what does BDD stand for?" — suggesting the template's vocabulary assumed more developer familiarity with BDD than was warranted.

### Phase verdict
🟡 Partially successful — too much was discovered iteratively. The [3a] issue template needs a much more explicit brief.

---

## Phase 4 — Frontend Implementation (Issues #32–#35)

### What happened
- Four `[4]` issues were created by `[3b]` and implemented in a single large PR #37 (`feat/frontend-phase4`)
- The backend skeleton was also raised as PR #36 (`feat/backend-skeleton`) with Bogus/Faker data
- One additional issue #39 (CSS hardcoded dark-theme colours) was raised post-implementation as a bug

### What worked
- **Faker backend + OpenAPI codegen**: Having the Faker backend generate the OpenAPI spec, which then drove RTK Query codegen, was a smooth end-to-end pipeline. The frontend could be built with real API contracts.
- **Single implementation PR for all frontend stories**: All four `[4]` issues in one PR kept the frontend coherent.

### What didn't work
- **All four issues in one PR was too broad**: PR #37 was very large. Individual story PRs would have given finer-grained review ability.
- **CSS bug #39**: The AI used hardcoded dark-theme colour values rather than CSS custom properties, which was caught after merge. This suggests the CSS coding standard (use custom properties) was not explicit enough in the AI guidance or acceptance criteria.
- **No explicit "close issues on merge" per-story**: The four `[4]` issues weren't individually linked with `Closes #N` in the PR body, relying on manual closure instead.

### Phase verdict
✅ Mostly successful — delivered a navigable frontend with real API contracts. The all-in-one PR and CSS bug were minor friction points.

---

## Phase 5 — Backend Design (Issues #8, #9, #10)

### What happened
- Issue #8 (`[5a]`) was triggered repeatedly before dependencies were met
  - The AI checked [4] issues 3 separate times before they finally closed
  - Each check produced a detailed dependency analysis comment
- Once [4] issues closed, the AI asked about EF Core/Postgres (again) — the project vision already said "no DB" but the `[5a]` template included it as a required output
- PR #41 was raised with a comprehensive backend design spec including ADRs
- The developer pushed back on "open questions" in the spec, and the AI resolved them as ADR-006–ADR-011

### What worked
- **The ADR format**: Architectural Decision Records (ADR-001 to ADR-011) were a valuable addition to the spec. They captured the "why not" alternatives alongside the chosen approach.
- **Open questions as a section**: Having unresolved questions explicitly listed in the spec doc meant they got resolved systematically rather than being ignored.
- **Service layer outline**: The SRP-compliant service architecture diagram proved accurate — the implementation closely followed the design.

### What didn't work
- **EF Core question asked 3 times**: The `[5a]` template acceptance criteria included "EF Core entities" and "Mermaid ER diagram" even though the project vision explicitly excluded a database. Each time the AI was triggered on this issue, it re-asked the same question. This was a significant source of friction.
- **Dependency checking was noisy**: The AI's multi-paragraph "dependency not met" comments were thorough but created a lot of notification noise for the developer. A simpler flag would suffice.
- **No "close questions before PR" prompt**: The AI raised the PR, then the developer had to comment "are you able to make progress on the open questions?" — the AI should have resolved open questions as ADRs before the PR was considered complete.

### Phase verdict
🟡 Partially successful — the spec artefact was high quality, but the EF Core re-questioning and dependency noise were avoidable.

---

## Phase 6 — Backend Implementation (Issues #43–#59, plus #73, #76)

### What happened
- 19 `[6]` user story issues were created and most were implemented via individual PRs
- Implementation followed a consistent pattern: new fetcher service → unit tests → integration → PR
- Two additional issues were created outside the planned spec:
  - #73: ONS real HTTP calls (a follow-up to the initially incomplete #53 implementation)
  - #76: CAPE data source bug — the AI had incorrectly assumed FRED hosted the Shiller CAPE series
- Issue #57 (Cache-Control headers) timed out (600s) on first run and required a second trigger

### What worked
- **TDD discipline**: Implementation PRs consistently followed the test-first approach. Unit tests, integration tests, and live API tests (with env-based skip) were all present.
- **Individual PRs per story**: Each `[6]` issue got its own PR branch, making review and history clean.
- **FetcherException pattern**: The error handling abstraction was designed in Phase 5 and implemented consistently across all three fetchers.
- **Feature flags for live tests**: The pattern of skipping live API tests via an environment variable was established early and used consistently.
- **Sidecar for yfinance**: The Python sidecar design (decided in Phase 5) was well-executed and avoided the need for a .NET yfinance wrapper.

### What didn't work
- **CAPE data source assumption (#76)**: The AI assumed FRED hosted the Shiller CAPE ratio series (it does not). This was not validated during spec and led to a production bug requiring a new fetcher service. The spec should have included explicit source validation.
- **ONS implementation incomplete on first pass (#73)**: Issue #53 was closed but the ONS fetcher still used stub data — the real HTTP implementation was done in a follow-up issue #73. The acceptance criteria didn't catch this.
- **Timeout on #57**: The Cache-Control implementation timed out at 600 seconds. This is a signal that either the task was too broad for a single run or the AI was doing unnecessary exploration. The PR was eventually raised correctly on the second trigger.
- **Branch targeting main**: Multiple Phase 6 PRs went directly to `main` instead of `dev`, causing the #80 drift incident.

### Phase verdict
🟡 Mostly successful on implementation quality, but with avoidable bugs and branch discipline failures.

---

## Deployment Phase (Issues #80, #81, #83, #85, #87)

### What happened
- Deployment was not a formal phase in the SDD workflow — it emerged organically once backend implementation was mostly done
- PR #83 introduced Helm chart, secrets config, imagePullSecrets
- PR #82 resolved the main/dev drift
- PR #86 fixed missing nginx.conf and logo
- Issues #81 and #87 remain open (deploy pipeline and deployment issues respectively)

### What worked
- **Helm chart approach**: Using Helm for Kubernetes deployment was the right choice for the OCI environment.
- **Sidecar included in Helm**: The Python yfinance sidecar was wired into the Helm chart alongside the .NET backend.

### What didn't work
- **No deployment phase in the SDD workflow**: Deployment was ad-hoc. The SDD workflow document doesn't mention deployment at all. This led to:
  - PRs targeting main directly (instead of dev)
  - Multiple small fix PRs for nginx, logo, page title
  - Secrets configuration happening as an afterthought
- **OCIR authentication not clearly documented**: The OCI Container Registry username format issue (#81 / #87) required back-and-forth that would have been caught with a deployment checklist.

### Phase verdict
🔴 Needs a formal deployment phase in the SDD workflow.
