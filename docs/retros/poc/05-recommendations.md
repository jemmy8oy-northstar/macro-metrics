# Recommendations — Future SDD Process

**Purpose:** Concrete, actionable recommendations to improve the SDD process for the next project, based on MacroMetrics POC learnings.

---

## Core Philosophy Validation

The SDD process' core principles held up well:

> *"Iterate in chat first — decisions made in chat are cheap; decisions made in code are expensive."*

This was validated: the most expensive rework (CAPE bug, branch drift, ONS stub) all happened because something was **not** iterated in chat/spec first. The prescription is correct; execution gaps were in the template, not the philosophy.

---

## Recommendation 1 — Smarter [1c] Questionnaire

The `[1c]` spec questionnaire is the most important document in the SDD process. It sets the foundation for every subsequent phase. The current questionnaire is good but under-specifies several areas that caused repeated friction.

**Proposed additions:**

```markdown
## Extended Spec Questionnaire

### Foundation (existing)
- What problem does this product solve, and for whom?
- What does a successful MVP look like?
- What is explicitly out of scope for the MVP?

### Database
- [ ] This project requires a persistent database (PostgreSQL + EF Core)
- If yes: any schema hints or domain model notes?
- If no: confirm the backend will be stateless for MVP.

### External Data Sources (new — critical)
For each external data source:
- API name and base URL
- Specific series IDs / endpoint paths needed
- API key required? How should it be stored?
- Data cadence (daily/monthly/quarterly)?
- Earliest data available?
- Have you verified the series ID exists and is accessible?

### Branch & Deployment (new)
- Target deployment environment (Kubernetes / Fly.io / Vercel / other)?
- Container registry in use?
- Should all feature PRs target `dev` with `dev → main` for deployment?

### AI Behaviour Preferences (new)
- Should the AI ask clarifying questions before implementing, or make assumptions and document them in the PR?
- Preferred assumption surfacing: (a) block and ask, (b) implement with assumptions listed in PR, (c) mix
```

---

## Recommendation 2 — "Assumptions & Decisions" PR Section (High Value)

The single highest-leverage improvement is adding a structured **Assumptions & Decisions** section to every AI-raised PR. This addresses:
- Silent assumptions (CAPE on FRED, branch targeting, CSS approach)
- The developer's ability to review and override without reading every line of code

**Proposed PR template addition:**
```markdown
## Assumptions & Decisions

> Any assumption made during implementation that was not explicitly specified in the issue.

| # | Assumption | Rationale | If incorrect... |
|---|---|---|---|
| 1 | [e.g. FRED hosts CAPE data] | [e.g. FRED API is already integrated] | [e.g. a new fetcher service is needed] |
| 2 | | | |

> If you disagree with any assumption above, comment and re-apply `action-ready`. The AI will revise.
```

This is a lightweight, high-value addition. The developer can scan the table in 30 seconds and catch problems before they land in production.

---

## Recommendation 3 — "Phase Guard" at the Top of Every AI Comment

When the AI is triggered on an issue, it should check whether the current phase matches the issue's phase number and flag any mismatch. This replaces the verbose multi-paragraph dependency checks with a concise status block.

**Proposed format:**
```
## 🔍 Phase Check

| Check | Status |
|---|---|
| Phase 5 dependencies ([4] issues) | ⏳ #32, #34 still open |
| Database decision from [1c] | ✅ "No DB for MVP" — will skip EF Core items |
| Data sources validated | ⚠️ CAPE source unverified — will flag in spec |

> Proceeding once #32 and #34 close. Will post one update when they do.
```

This is concise, scannable, and communicates the same information as the current verbose comments — but in 10 lines instead of 50.

---

## Recommendation 4 — Separate [3a] into [3a-tech] and [3a-stories]

The `[3a]` issue was the most friction-heavy orchestrator issue in the POC. It combined five distinct concerns (library choices, API skeleton, fake data strategy, TDD approach, BDD stories) into one PR, which led to 4+ review rounds.

**Proposed split:**

| Issue | Action |
|---|---|
| `[3a]` Frontend tech decisions | AI raises `docs/tech-decisions-frontend.md` covering library choices, API skeleton contracts, fake data strategy, and TDD approach. Human review gate. |
| `[3b]` Frontend BDD user stories | Triggered once [3a] is merged. AI raises `docs/user-stories-frontend.md` with BDD stories derived from the signed-off designs and agreed tech stack. |
| `[3c]` Create frontend issues + backend skeleton | Triggered once [3b] is merged. AI creates `[4]` frontend issues AND raises the Faker backend skeleton PR. |

This reduces any single PR's scope to one clear deliverable and makes the review gate more targeted.

---

## Recommendation 5 — Data Source Validation in [5a]

Add an explicit data source validation step to the `[5a]` backend design spec acceptance criteria:

```markdown
## Data Source Validation (required)

For each external data source listed in [1c]:
- [ ] Confirm the API endpoint / series ID is live and accessible
- [ ] Document the exact URL pattern used
- [ ] Note rate limits and API key requirements
- [ ] Record the earliest available date for each series
- [ ] Flag any sources that could not be validated with a ⚠️ and why
```

This would have caught the CAPE/FRED mismatch during Phase 5, before a single line of fetcher code was written.

---

## Recommendation 6 — Branch Discipline Enforcement (Structural)

The `main`/`dev` drift (issue #80) was the most impactful process failure. It required a dedicated drift-resolution PR and created confusion about which branch was current.

**Structural fix (not just guidance):**

Add `.github/workflows/require-dev-source.yml`:
```yaml
name: Enforce dev-first workflow
on:
  pull_request:
    branches: [main]
jobs:
  check-source-branch:
    runs-on: ubuntu-latest
    steps:
      - name: Branch source check
        run: |
          if [[ "${{ github.head_ref }}" != "dev" && "${{ github.head_ref }}" != hotfix/* ]]; then
            echo "::error::PRs to main must come from dev or hotfix/*. Raise your PR against dev first."
            exit 1
          fi
```

Add this workflow to the web-template repository so every new project inherits it automatically.

---

## Recommendation 7 — Deployment Phase (Phase 8)

Add a formal **Phase 8 — Deployment** to `docs/ai-workflow.md`:

```markdown
### Phase 8 — Deployment

Triggered once all [6] issues are closed (or explicitly signed off).

| Issue | Action |
|---|---|
| [8a] | AI sets up CI/CD pipeline: GitHub Actions → container registry → K8s/hosting |
| [8b] | Developer configures registry secrets and K8s credentials |
| [8c] | AI raises Helm/deployment config PR targeting `dev` |
| [8d] | Developer merges `dev → main` to trigger production deployment |

**Checklist for AI when raising the deployment PR:**
- [ ] Dockerfile builds cleanly
- [ ] nginx.conf (or equivalent) includes all required routes
- [ ] All environment variables documented in `docs/deployment.md`
- [ ] Health check endpoint configured (`/api/status` or equivalent)
- [ ] All secrets referenced by name (never hardcoded)
```

---

## Recommendation 8 — Retro Phase (Phase 9 / Always Last)

Add a mandatory **retro phase** to every project:

```markdown
### Phase 9 — Retrospective

| Issue | Action |
|---|---|
| [9a] | AI generates retrospective in `docs/retros/<project-name>/` covering: process analysis, issue quality, template gaps, technical decisions, and recommendations |
| [9b] | Developer reviews and merges the retro PR |
| [9c] | AI opens PRs on `web-template` to apply gap fixes identified in the retro |
```

The retro should be generated **before** the MVP is considered done. Having it as the last formal step ensures learnings are captured while context is fresh.

---

## Recommendation 9 — Streamlined AI Clarification Flow

The current flow when the AI has questions is ad-hoc. A structured clarification protocol would reduce friction:

**Proposed protocol:**
1. If the AI can make a sensible assumption → implement with the assumption documented in the PR's "Assumptions & Decisions" table. The developer can override in review.
2. If the AI cannot make a sensible assumption (e.g. "which of three equally valid options?") → post a single comment with the question and a recommended default. Remove `action-ready`. Add `waiting-for-human`.
3. If a decision was already made in a previous comment → implement without re-asking. Read all issue comments before posting a new question.

**Anti-patterns to eliminate:**
- Asking the same question multiple times (EF Core issue)
- Re-listing full dependency analysis when only the open-item count has changed
- Blocking on trivial decisions that could reasonably be assumed and noted

---

## Recommendation 10 — "AI Assumptions" Mode vs "AI Asks" Mode

A configurable behaviour preference (captured in `[1c]`) would let the developer choose how they want the AI to handle ambiguity:

| Mode | AI behaviour |
|---|---|
| **Assume & Document** | AI makes sensible assumptions, implements, lists all assumptions in the PR. Developer reviews and overrides. Fastest flow. |
| **Ask First** | AI posts all clarifying questions before starting. Developer answers. Slower but developer retains more control. |
| **Mixed** | AI asks for major architectural decisions; assumes for minor/stylistic choices. |

The MacroMetrics POC showed that "Assume & Document" would have been more efficient — the developer was responsive and happy to guide via PR review rather than pre-implementation Q&A.

---

## Recommendation 11 — Always Assign the Repository Owner

**The gap:** ~60–70% of Phase 6 issues and PRs were created without assigning the developer. GitHub notifications are assignee-driven — no assignee means the developer may silently miss activity.

**Evidence:** Issues #44–#56, #58, #59, #73 (and the PRs that closed them) were all created without an assignee. The developer confirmed this directly: *"the bot does not consistently assign me when it creates an issue or PR, this is essential to ensure that I get notifications."*

**Fix (one line):**
Add to `CLAUDE.md` (and the web-template):
```
## Notification Rule (mandatory)
Always assign the repository owner to every issue and PR:
  gh issue create ... --assignee $(gh repo view --json owner --jq .owner.login)
  gh pr create   ... --assignee $(gh repo view --json owner --jq .owner.login)
```

Add this to every issue factory's `gh issue create` loop — especially `[3b]` (creates frontend issues) and `[5c]` (creates 19 backend issues).

**Secondary fix — PR template:**
Add a `--assignee` line to the `gh pr create` template in `CLAUDE.md` so it's always included by default.

---

## Recommendation 12 — Reduce action-ready Relabelling Friction

**The gap:** The developer had to manually re-apply `action-ready` after every AI pass — including partial passes and timeouts. For issues like #8 `[5a]` (3 passes) and #57 (timeout → second run), this was repeated friction.

The developer raised this directly: *"I often have to keep relabelling issues with ai ready."*

**Fix — self-relabelling on partial completion:**
When the AI does not fully complete a task (no PR raised, or work was explicitly noted as partial), re-apply the `action-ready` label before exiting:
```bash
# Partial/timeout — re-label so the next trigger fires automatically
gh issue edit $ISSUE_NUMBER --add-label "action-ready"
gh issue comment $ISSUE_NUMBER --body "⚡ Pass N complete (partial). Re-labelled for next pass. Remaining: [X]"
```

**Fix — label lifecycle clarity:**
Introduce a two-label pattern:
| Label | Meaning |
|---|---|
| `action-ready` | Human has approved — trigger the AI |
| `action-in-progress` | AI is currently working |

The AI sets `action-in-progress` when it starts, and either:
- Closes the issue/labels `action-complete` on success
- Re-labels `action-ready` on partial completion

This lets the developer see at a glance which issues need a re-trigger vs which are awaiting review.

**Fix — iteration limit per issue type:**
Set `max_turns` hints in issue templates to guide the operator:
- Orchestrator issues `[1c]`, `[3a]`, `[5a]`: ~80 turns (complex, multi-pass)
- Implementation issues `[4]`, `[6]`: ~40 turns (one-pass expected)

---

## Recommendation 13 — Structured Pass Summaries for Multi-Pass Issues

**The gap:** The AI has no persistent memory between triggers. On each new pass it re-reads the same files, re-checks the same dependencies, and sometimes re-asks the same questions. This wastes tokens and contributes to the EF Core re-questioning pattern.

**Fix — structured pass summary comments:**
At the end of every AI run (partial or complete), leave a structured summary comment:
```markdown
## AI Pass 2 Summary — 2026-06-07

**Status:** Partial — PR not yet raised. Timed out at Cache-Control implementation.

**Completed this pass:**
- Read project spec, backend design, and Phase 6 user stories ✅
- Set up `CacheControlEndpointFilter` in `MacroMetrics.WebApi` ✅
- Unit tests written ✅

**Not completed:**
- Integration test wiring for the filter
- PR not raised

**Files to skip re-reading next pass:**
- `docs/specs/project-vision.md` (no DB, stateless proxy)
- `docs/backend-design.md` (service architecture finalised)

**Next trigger:** Re-apply `action-ready`. Implementation is ~70% complete.
```

This dramatically reduces the startup overhead of pass N+1 — the AI reads the summary comment instead of re-crawling the whole spec tree.

---

## Summary Priority Matrix

| Recommendation | Impact | Effort | Priority |
|---|---|---|---|
| 1 — Extended [1c] questionnaire | High | Low | 🔴 Do first |
| 2 — Assumptions PR section | High | Low | 🔴 Do first |
| 5 — Data source validation in [5a] | High | Low | 🔴 Do first |
| 6 — Dev-first branch workflow | High | Low | 🔴 Do first |
| 11 — Always assign repository owner | High | Low | 🔴 Do first |
| 7 — Deployment phase | High | Medium | 🟠 Next sprint |
| 12 — Reduce action-ready relabelling | Medium | Low | 🟠 Next sprint |
| 8 — Retro phase | Medium | Low | 🟠 Next sprint |
| 4 — Split [3a] | Medium | Low | 🟠 Next sprint |
| 13 — Structured pass summaries | Medium | Medium | 🟠 Next sprint |
| 3 — Phase Guard comments | Medium | Low | 🟡 Nice to have |
| 9 — Clarification protocol | Medium | Low | 🟡 Nice to have |
| 10 — Assume vs Ask mode | Low | Low | 🟡 Nice to have |
