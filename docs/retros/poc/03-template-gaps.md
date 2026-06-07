# Template Gaps & Improvement Proposals

**Purpose:** Identify concrete gaps in the SDD template and workflow documents, and propose specific improvements.

---

## Gap 1 — Spec Questionnaire Doesn't Capture Database Need

**Where it hurt:** Issue #8 (`[5a]`) — the EF Core/Postgres question was asked in comments **three separate times** across different triggered runs. The project vision already said "no database for MVP" but the `[5a]` acceptance criteria template included EF Core entities and Mermaid ER diagrams as required outputs regardless.

**Root cause:** The `[1c]` questionnaire captures "Is Postgres required? If so, any schema/domain hints?" — but this answer doesn't propagate to the `[5a]` issue template. Each time `[5a]` triggers, the AI reads the `[5a]` issue body (which mentions EF Core) without cross-referencing the project spec.

**Proposed fix:**
1. Add an explicit checkbox to the `[1c]` questionnaire: `- [ ] This project requires a persistent database (EF Core / PostgreSQL)`
2. In the `[5a]` issue template, add an AI note: *"Before writing acceptance criteria, check `docs/specs/project-vision.md` for the database decision. If no database is required, remove the EF Core / ER diagram items from the AC below."*
3. In `docs/specs/sdd-workflow.md` Phase 6, add: *"If `[1c]` confirmed no database, the design spec covers service architecture and data flow only — no ER diagram."*

---

## Gap 2 — Data Source Validation Not Part of Phase 5

**Where it hurt:** Issue #76 — the AI assumed FRED hosted the Shiller CAPE ratio series. It does not. This was a correctness error in the spec that led to a bug in production (FRED returned an error for the `CAPE` series ID).

**Root cause:** Phase 5 (`[5a]`) design spec asks for service architecture and ADRs, but doesn't include a step to **validate external data sources** — i.e., confirm that the intended API actually hosts the required data series before the implementation issues are written.

**Proposed fix:**
1. Add a "Data Source Validation" section to the `[5a]` acceptance criteria:
   ```
   - [ ] For each external data source listed in [1c]:
     - Confirm the API endpoint and series ID exist and are accessible
     - Note any API key requirements
     - Note the data cadence (daily, monthly, etc.) and earliest available date
   ```
2. Add to the `[6]` fetcher story template: *"Before implementing, confirm the series ID and API endpoint from the Phase 5 data source validation notes."*

---

## Gap 3 — [3a] Issue Scope Too Broad / Brief Too Thin

**Where it hurt:** Issue #6 (`[3a]`) — the PR required 4+ rounds of review because key sections (API skeleton, TDD approach, BDD definition, MSW vs Faker decision) were all discovered through review comments rather than being built into the brief.

**Root cause:** The `[3a]` issue template says:
> *"Raise a single PR with both `docs/tech-decisions-frontend.md` (library proposals) and `docs/user-stories-frontend.md` (BDD stories)."*

This omits:
- The API skeleton (endpoint contracts + response shapes)
- The TDD section (Vitest + RTL, test-first approach)
- The fake data strategy (Faker backend vs MSW)
- A definition of BDD for readers unfamiliar with the term

**Proposed fix:** Update the `[3a]` issue template to explicitly require five sections in the PR:
1. **Library choices** — UI components, charts, date handling, state management
2. **API skeleton contracts** — endpoint shapes, RTK Query hooks, TypeScript response types
3. **Fake data strategy** — how the frontend gets data before the backend is real (Faker sidecar is the answer from this POC)
4. **Frontend TDD approach** — Vitest + RTL, test-per-AC, test-before-component
5. **BDD user stories** — with a one-sentence BDD definition at the top

---

## Gap 4 — No "Acceptance Criteria for Real vs Stub" Enforcement

**Where it hurt:** Issue #53 (`[6]` US-B12 ONS) — the issue was closed and PR merged, but the implementation was a stub. A follow-up issue #73 was needed to add the real HTTP implementation.

**Root cause:** The acceptance criteria for #53 said "UK macroeconomic metrics fetched from ONS" but didn't explicitly state "the fetcher must make a real HTTP call to the ONS API". The AI wrote a stub implementation that satisfied the letter of the AC.

**Proposed fix:** Add to the `[6]` fetcher story template:
```
## Implementation note
This story requires a **real HTTP call** to the external API — a stub or in-memory fake does not satisfy the acceptance criteria. The service must be wired with `AddHttpClient<>` and must use a real `BaseAddress`.
```

---

## Gap 5 — No Deployment Phase in the SDD Workflow

**Where it hurt:** Issues #80 (drift), #81 (pipeline), #83 (Helm), #85 (nginx), #87 (OCIR auth) — five separate issues arose from deployment being unplanned. PRs went to `main` directly, secrets were an afterthought, nginx config was missing.

**Root cause:** The SDD workflow document ends at Phase 7 (MVP). There is no deployment phase, no deployment checklist, and no branch discipline rules tied to deployment.

**Proposed fix:** Add a **Phase 8 — Deployment** to `docs/ai-workflow.md`:
```
### Phase 8 — Deployment

| Issue | Action |
|---|---|
| [8a] | Set up CI/CD pipeline (GitHub Actions → container registry → orchestrator) |
| [8b] | Deploy to scratch/staging — verify all routes, check nginx/ingress config |
| [8c] | Deploy to production — confirm secrets, domain, TLS |

Branch discipline:
- All Phase 6/7 implementation PRs target `dev`
- Only `dev` → `main` PRs trigger the production deploy pipeline
```

Also add a GitHub Actions workflow to enforce dev-first on `main`:
```yaml
# .github/workflows/require-dev-source.yml
on:
  pull_request:
    branches: [main]
jobs:
  check-source-branch:
    runs-on: ubuntu-latest
    steps:
      - name: Enforce dev-first workflow
        run: |
          if [[ "${{ github.head_ref }}" != "dev" && "${{ github.head_ref }}" != "hotfix/"* ]]; then
            echo "::error::PRs to main must come from dev or a hotfix/* branch."
            exit 1
          fi
```

---

## Gap 6 — Dependency Check Noise

**Where it hurt:** Issue #8 (`[5a]`) — the AI produced a full multi-paragraph dependency analysis comment every time it was triggered while dependencies were unmet. This happened 3 times, creating notification noise and long issue threads with repeated content.

**Root cause:** No guidance tells the AI to give a minimal response when dependencies are unmet.

**Proposed fix:** Add to `docs/ai-workflow.md` under "Agent Conventions":
```
### Dependency check responses
When an issue is triggered before its dependencies are met, respond with a single short comment only:
> ⏳ Dependencies not yet met: [list open blocking issues]. Will proceed once they close.

Do not repeat the full issue analysis. Do not re-list acceptance criteria. Post one comment per trigger maximum.
```

---

## Gap 7 — No "Assumptions Made" Section in PRs

**Where it hurt:** Multiple PRs — the AI made assumptions (CAPE on FRED, CSS colour strategy, direct-to-main branch) that weren't surfaced until they caused problems. The developer had no structured way to review assumptions at PR time.

**Root cause:** PR descriptions focused on "what was done" but didn't call out assumptions explicitly.

**Proposed fix:** Add a mandatory **"Assumptions & Decisions"** section to the PR template:
```markdown
## Assumptions & Decisions

List any assumption made during implementation that was not explicitly specified in the issue or spec. For each:
- **Assumption:** [what was assumed]
- **Rationale:** [why this seemed reasonable]
- **Alternative:** [what else could have been done]
- **Action if wrong:** [what needs to change if this assumption is incorrect]
```

This makes it easy for the developer to scan the PR for implicit decisions and either validate or override them — without needing to read every line of code.

---

## Gap 8 — No Retro Phase in the Workflow

**Where it hurt:** Issue #88 (this retrospective) — the developer had to create a retrospective request manually as an ad-hoc issue. The SDD workflow has no defined retro phase.

**Proposed fix:** Add a **Phase 7 — Retro** (renaming the current informal "MVP" phase):
```
### Phase 7 — Retrospective

| Issue | Action |
|---|---|
| [7a] | AI generates POC retrospective in `docs/retros/<project>/` covering process, template gaps, technical decisions, and recommendations |
| [7b] | Developer reviews and merges the retro PR |
| [7c] | AI applies template improvements from retro findings to `web-template` repository |
```

---

## Gap 9 — CSS Standards Not Explicit in AI Guidance

**Where it hurt:** Issue #39 — the AI used hardcoded dark-theme colour values instead of CSS custom properties. This was caught post-merge.

**Root cause:** The AI coding standards in `CLAUDE.md` didn't include CSS conventions.

**Proposed fix:** Add to the frontend implementation guidance:
```
- Always use CSS custom properties (`var(--colour-name)`) for colours, spacing, and typography. Never hardcode colour values in component CSS.
- Define custom properties in `:root` or a `:global` scope. Reference them everywhere.
```

---

## Summary of Proposed Template Changes

| Gap | Impact | Effort | Priority |
|---|---|---|---|
| Gap 1 — DB questionnaire field | High (repeated questions) | Low | 🔴 High |
| Gap 2 — Data source validation | High (CAPE bug) | Medium | 🔴 High |
| Gap 3 — [3a] broader brief | High (4+ review rounds) | Low | 🔴 High |
| Gap 4 — Real vs stub enforcement | Medium (one follow-up issue) | Low | 🟠 Medium |
| Gap 5 — Deployment phase | High (5 issues) | Medium | 🔴 High |
| Gap 6 — Dependency check noise | Low (notification noise) | Low | 🟡 Low |
| Gap 7 — Assumptions section in PR | Medium (3+ silent assumptions) | Low | 🟠 Medium |
| Gap 8 — Retro phase | Medium (process completeness) | Low | 🟠 Medium |
| Gap 9 — CSS standards | Low (one bug) | Low | 🟡 Low |
