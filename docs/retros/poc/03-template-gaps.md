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

## Gap 10 — AI Does Not Consistently Assign Developer to Issues and PRs

**Where it hurt:** Developer notification reliability. The GitHub notification system works through assignees — if the developer is not assigned, they may miss activity on an issue or PR entirely.

**Evidence from the POC:**
Looking at all 50 issues and 38 PRs, assignees were applied inconsistently:

| Category | Issues without assignee |
|---|---|
| Phase 6 implementation issues (#44–#56, #58, #59, #73) | ~70% had no assignee |
| Phase 6 PRs (#41, #42, #61–#63, #66, #67–#73, #75, #79) | ~60% had no assignee |
| Bug issues (#39) | No assignee |
| Template orchestrator issues | Mostly assigned (better) |

The inconsistency appears to be correlated with Phase 6 — issues and PRs created once the "issue factories" were running (after `[5c]`) were often not assigned. Earlier phases (1–4) had better assignment coverage.

**Root cause:** The `CLAUDE.md` AI workflow instructions mention assigning the repository owner in some contexts but not uniformly. Phase 6 issues were created programmatically via the `[5c]` issue factory, which may not have included assignees in its `gh issue create` calls.

**Impact:** The developer has to manually check GitHub or rely on Telegram polling to notice activity on unassigned issues/PRs. The `action-ready` label approach only works if the developer is notified — without assignees, the developer may not see that the AI has left a comment or raised a PR.

**Proposed fix:**

1. In `CLAUDE.md` (both in the project repo and in `web-template`), add:
   ```
   ## Notification Rule
   - ALWAYS assign the repository owner to every issue created or PR raised.
   - Use: `gh issue create --assignee <owner>` and `gh pr create --assignee <owner>`
   - If the owner username is not known from context, retrieve it with: `gh repo view --json owner --jq .owner.login`
   ```

2. In every issue factory script (`[3b]`, `[5c]`) add `--assignee $(gh repo view --json owner --jq .owner.login)` to all `gh issue create` calls.

3. Add a post-creation verification: after creating issues, list them and confirm all have at least one assignee.

---

## Gap 11 — Relabelling Friction: `action-ready` Must Be Manually Reapplied

**Where it hurt:** Developer workflow overhead. After each AI run (successful or timed-out), the `action-ready` label is removed. The developer must manually re-apply it to trigger the next run. For multi-pass issues (like #8 `[5a]` which needed 3 passes, or #57 which timed out), this created repeated relabelling work.

**Evidence:**
- Issue #8 required the developer to re-apply `action-ready` at least 3 times
- Issue #57 timed out on first run — developer had to re-apply the label for the second pass
- The developer raised this directly: *"I often have to keep relabelling issues with ai ready — maybe I need to improve the iteration length"*

**Root cause:** The current architecture removes `action-ready` on every trigger, regardless of whether the task completed. This is intentional (prevents infinite loops) but creates friction for legitimate multi-pass work.

**Proposed fixes:**

**Option A — Self-labelling on partial completion:**
When the AI detects it has not fully completed the task (e.g., timed out, PR not yet raised), it re-applies `action-ready` itself before exiting:
```bash
# At end of partial run
gh issue edit $ISSUE_NUMBER --add-label "action-ready"
```

**Option B — Increase iteration limit per issue type:**
- Orchestrator issues (`[1c]`, `[3a]`, `[5a]`) typically need 2–4 AI rounds. Set `max_turns=80` for these.
- Implementation issues (`[4]`, `[6]`) typically complete in one pass. Keep `max_turns=40`.
- Add the expected pass count to each issue template as a hint to the operator.

**Option C — Label lifecycle management:**
Introduce a `action-in-progress` label that the AI sets when it starts work, and `action-complete` when finished. The `action-ready` label is only re-added manually when the developer wants to trigger a new pass.

**Recommended:** Option A + B together. Option A avoids lost-work scenarios (timeout). Option B reduces how often A is needed.

---

## Gap 12 — Session Memory: AI Re-discovers Context on Every Trigger

**Where it hurt:** Token efficiency and consistency. On every trigger, the AI re-reads the same files (project spec, tech decisions doc, backend design spec, workflow docs). For multi-pass issues, this means the same files are read 2–4 times. For later phases (Phase 6), this context re-read took a significant portion of the available context window and turn budget.

**Evidence:**
- Issue #8 `[5a]`: The EF Core question was re-asked 3 times partly because the AI re-read the `[5a]` issue template (which mentions EF Core) without correctly reconciling it against the project vision on each new pass
- Issue #57: Likely timed out partly due to redundant file exploration on startup
- The dependency check multi-paragraph comments were generated fresh each trigger, re-reading all `[4]` issues from scratch

**Root cause:** Stateless AI triggers — each trigger is a fresh agent run with no memory of previous runs. The AI must re-discover the project state from GitHub each time.

**Proposed fixes:**

1. **Structured issue comments as state:** When an AI pass completes (successfully or not), leave a structured summary comment on the issue:
   ```markdown
   ## Pass N Summary (YYYY-MM-DD)
   **Status:** [completed/partial/blocked]
   **Files read:** [list of key files, skip re-reading next pass]
   **Decisions made:** [list of non-obvious decisions]
   **Remaining work:** [what's left if partial]
   **Next trigger:** [what the developer needs to do, e.g. re-label, answer question]
   ```
   On the next trigger, the AI reads only this summary comment rather than re-reading all files from scratch.

2. **Canonical project state document:** Maintain a `docs/project-state.md` that is updated by the AI after each phase completes, summarising the current state of all decisions. This is faster to read than the full spec chain.

3. **Issue-specific context hints:** In the issue body, add a section:
   ```markdown
   ## Context hints for AI
   - Project spec: `docs/specs/project-vision.md`
   - Key decision: No database (stateless proxy)
   - Tech stack: .NET 8 Minimal API, React + Vite, Python yfinance sidecar
   ```
   This lets the AI read one short section instead of crawling the full docs tree.

---

## Gap 13 — Bot Uses Issue Body as Prompt, Not Latest Unanswered Comment

**Where it hurt:** Issue #88 (this retrospective) — the bot was triggered twice and on the second trigger re-started its full analysis from scratch, re-asking questions that had already been answered in comments. The developer answered the clarifying questions from the first run, but the second trigger ignored those answers entirely.

**Root cause:** The fork's webhook handler passes the **issue body** as the task prompt every time the bot is triggered — it does not check whether there are unanswered questions already in the comment thread, or whether a previous pass left a partial summary. This is a two-part problem:

| Layer | Problem |
|---|---|
| CLAUDE.md (template) | The AI is not instructed to read all existing comments before posting |
| Fork code | The webhook always uses the issue body as the prompt, never the latest unanswered human comment |

**CLAUDE.md fix (addressable now):**
Add a multi-pass behaviour instruction to `CLAUDE.md`:
```markdown
## Multi-pass Issue Behaviour

Before posting any comment on an issue, read ALL existing comments in full.
- If you have already proposed a plan or analysis: do not repeat it — continue from where you left off
- If you have asked clarifying questions and the owner has answered them: proceed with the implementation using those answers
- If a previous pass left a structured summary comment: read that summary first and skip re-reading files already listed there
```

**Fork fix (required for a complete resolution):**
The webhook handler should be updated to:
1. Fetch all comments on the issue
2. Find the latest unanswered human comment (a comment from the repo owner that was posted after the last bot comment)
3. If one exists, use it as the task prompt instead of the issue body

This is tracked in **[claude-code-telegram-k8s #33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33)**.

---

## Gap 14 — No Visibility When max_turns Limit Is Hit

**Where it hurt:** Issue #57 — the implementation timed out mid-task. The developer's only signal was the absence of a completion message. There was no indication of what had been done before the timeout, or what the developer should do next (e.g. re-apply `action-ready`).

**Root cause:** The fork's agent runner does not distinguish between a clean exit (task complete) and a max_turns exit (task incomplete because of iteration limit). Both outcomes post the same completion message — or in some cases, no message at all.

**CLAUDE.md partial fix:**
A self-relabelling rule on partial completion helps (see Gap 11 / Recommendation 12), but the AI may not have an opportunity to run that cleanup code when max_turns is hit abruptly.

**Fork fix (required for meaningful visibility):**
The fork should detect `stop_reason == "max_turns"` from the SDK `ResultMessage` and:
1. Post a distinct ⚠️ GitHub comment: *"Claude reached its iteration limit. Re-apply `waiting-for-ai` to continue from where this pass left off."*
2. Send a Telegram alert (separate from the normal completion notification)
3. Do **not** remove the `waiting-for-ai` label when the limit is hit — the default of removing it means the developer has to re-apply manually with no explanation of why

Additionally, the default `claudeMaxTurns` value should be raised from its current low default (~20–50) to at least **100** to reduce how often orchestrator-level issues hit the limit.

This is tracked in **[claude-code-telegram-k8s #34](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/34)**.

---

## Classification — Template vs Fork Changes

All 14 gaps above fall into one of two categories:

### Addressable via template / CLAUDE.md (no fork code change required)

| Gap | Fix location |
|---|---|
| Gap 1 — DB questionnaire field | `web-template` `[1d]` issue body |
| Gap 2 — Data source validation | `web-template` `[5a]` AC + `[1d]` questionnaire |
| Gap 3 — [3a] broader brief | `web-template` `[3]` issue body |
| Gap 4 — Real vs stub enforcement | `web-template` `[6]` fetcher story template |
| Gap 5 — Deployment phase | `web-template` `docs/ai-workflow.md` + `sdd-workflow.md` |
| Gap 6 — Dependency check noise | `CLAUDE.md` agent conventions section |
| Gap 7 — Assumptions section in PR | `CLAUDE.md` PR template convention |
| Gap 8 — Retro phase | `web-template` `docs/sdd-workflow.md` |
| Gap 9 — CSS standards | `CLAUDE.md` frontend standards section |
| Gap 10 — Assignee not consistently set | `CLAUDE.md` notification rule + `init-issues.mjs` |
| Gap 11 — action-ready relabelling friction (partial) | `CLAUDE.md` multi-pass rule |
| Gap 12 — Session memory (partial) | `CLAUDE.md` pass summary instruction |
| Gap 13 — Multi-pass behaviour (partial) | `CLAUDE.md` multi-pass instruction |

### Requires fork-level changes

| Gap | Fork change |
|---|---|
| Gap 11 — iteration limit per issue type | `values.yaml` `claudeMaxTurnsByLabel` map |
| Gap 13 — latest comment as prompt | `_build_github_prompt()` — fetch comments, find latest unanswered |
| Gap 14 — max_turns limit visibility | Detect `stop_reason == "max_turns"`, post ⚠️ comment + Telegram alert |

Fork issues raised: [#33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33) · [#34](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/34)

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
| Gap 10 — Assignee not consistently set | High (missed notifications) | Low | 🔴 High |
| Gap 11 — action-ready relabelling friction | Medium (developer overhead) | Low | 🟠 Medium |
| Gap 12 — Session memory / context re-discovery | Medium (token waste, repeated questions) | Medium | 🟠 Medium |
| Gap 13 — Bot uses issue body, not latest unanswered comment | High (re-asks answered questions) | Low (CLAUDE.md) / Medium (fork) | 🔴 High |
| Gap 14 — No visibility when max_turns hit | Medium (silent failures) | Low (fork) | 🟠 Medium |
