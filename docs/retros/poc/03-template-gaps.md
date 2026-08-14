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

## Gap 15 — Testing Strategy Exists But Is Not Surfaced in Issue Templates

**Where it hurt:** Testing coverage was inconsistent across phases. The `[3]` (Frontend MVP) issue body had no testing AC, and the `[5]` backend issues mentioned TDD but didn't link to `docs/specs/testing-strategy.md`. It was left to the AI's discretion whether tests were written.

**Root cause:** The `web-template` has a `docs/specs/testing-strategy.md` with detailed guidance (Vitest + RTL for frontend; xUnit + Moq for backend; top-down TDD). But this document is only mentioned in a single table row in `CLAUDE.md` — it is never referenced from any issue body or acceptance criteria. `init-issues.mjs` issue bodies have no explicit testing ACs.

**Evidence from MacroMetrics:**
- Phase 4 frontend issues (#31–#38) were closed without confirming test coverage
- The only CI reference is a generic "build + test on every PR" in the Phase 1b setup doc
- 200+ tests were ultimately produced, but this relied on the AI choosing to write them — not on ACs requiring them

**Proposed fix:**

1. **In every `[3]` frontend issue body**, add:
   ```markdown
   ## Testing (mandatory — see `docs/specs/testing-strategy.md`)
   - [ ] Each component has at least one Vitest test covering its key behaviour
   - [ ] Tests are written spec-first (test before component)
   - [ ] `npm test` passes with no failures before the PR is raised
   ```

2. **In every `[5]` backend feature issue body**, add:
   ```markdown
   ## Testing (mandatory — see `docs/specs/testing-strategy.md`)
   - [ ] Unit test written first (TDD) for each new service method
   - [ ] Integration test scenario defined (Phase 5) or implemented (end of Phase 6)
   - [ ] `dotnet test` passes with no failures before the PR is raised
   ```

3. **In `CLAUDE.md`**, add:
   ```markdown
   ## Testing Standards (mandatory)
   Before raising any PR:
   - Backend: `dotnet test` must pass. Write tests before implementation (TDD).
   - Frontend: `npm test` must pass. Vitest + RTL, one test per AC.
   - Full strategy: `docs/specs/testing-strategy.md`
   ```

---

## Gap 16 — No Claude Code Hooks (AI Guards) Configured in Web-Template

**Where it hurt:** Several quality problems in the POC could have been caught automatically:
- Hardcoded CSS colours (issue #39) — a post-write linter hook would have flagged or auto-fixed the pattern
- Direct-to-main PRs (#80) — a pre-bash git hook could have blocked `git push origin main`
- Missing `--assignee` — a post-create hook could verify the assignee was applied

**What Claude Code hooks are:** Claude Code supports `PreToolUse` and `PostToolUse` hooks configured in `.claude/settings.json`. These are shell commands that run automatically before or after specific tool calls — the direct equivalent of Cursor/Windsurf command hooks. They run outside the AI's context window and cannot be overridden by AI instructions. This is the correct mechanism for structural enforcement that doesn't rely on the AI "remembering" a rule.

**Example hooks for web-template (`.claude/settings.json`):**
```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [{
          "type": "command",
          "command": "FILE=$CLAUDE_TOOL_INPUT_FILE_PATH; case \"$FILE\" in *.ts|*.tsx) npx prettier --write \"$FILE\" 2>/dev/null;; *.cs) dotnet format --include \"$FILE\" 2>/dev/null;; esac"
        }]
      }
    ],
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [{
          "type": "command",
          "command": "if echo \"$CLAUDE_TOOL_INPUT_COMMAND\" | grep -qE 'git push.*(origin )?main'; then echo 'AI guard: direct push to main blocked. Target dev instead.' && exit 2; fi"
        }]
      }
    ]
  }
}
```

**High-value hooks to add to web-template:**

| Hook type | Trigger | Guard |
|---|---|---|
| PostToolUse | Write/Edit on `.ts`/`.tsx`/`.css` | Auto-run prettier |
| PostToolUse | Write/Edit on `.cs` | Auto-run `dotnet format` |
| PreToolUse | Bash matching `git push.*main` | Block direct-to-main push |
| PostToolUse | Bash matching `gh issue create` without `--assignee` | Warn if assignee absent |

**Proposed fix:**
1. Add `.claude/settings.json` to `web-template` with linter and branch-guard hooks
2. Add an "AI Guards" section to `CLAUDE.md` explaining the hook mechanism and listing active guards

---

## Gap 17 — `waiting-for-ai` vs `action-ready` Workflow Not Documented

**Where it hurt:** The two labels trigger fundamentally different bot modes, but this distinction is not written down in the SDD workflow or any developer-facing guidance. The developer re-triggered issues with `waiting-for-ai` when they wanted continued implementation — but `waiting-for-ai` places the bot into discussion-only mode ("respond but do NOT start implementing"). The bot's response was correct per its programming, but the developer had no visibility that the wrong mode had been selected.

**Root cause:** `docs/ai-workflow.md` / `sdd-workflow.md` lists the labels but does not explain what mode each triggers or when to use which label at each stage.

**Label modes:**

| Label | Bot mode | When to use |
|---|---|---|
| `waiting-for-ai` | **Discussion** — bot answers questions, proposes plans, asks clarifying questions. Does NOT implement or raise PRs. | First contact on a new issue; Q&A passes; asking for analysis without implementation |
| `action-ready` | **Implementation** — bot implements the issue, writes code, raises a PR. | When you've reviewed/approved a plan and want the bot to build it; re-triggering after a partial pass to continue implementation |

**Proposed fix:**
1. Add a "Label Reference" table to `docs/ai-workflow.md` with the two modes above.
2. Add a callout box to the SDD workflow doc before Phase 4: *"Use `action-ready` when you want implementation to start. Use `waiting-for-ai` for discussion, planning, or analysis only."*
3. Add to `CLAUDE.md`: when the AI finishes an implementation pass and the next trigger should continue implementation, it must explicitly note *"Re-apply `action-ready` (not `waiting-for-ai`) to continue."*

This is a documentation-only change — no code or template logic changes required.

---

## Gap 18 — No Screenshot Gate for Frontend PRs / No Headless Browser in Bot Container

**Where it hurt:** All frontend PRs in MacroMetrics were merged without any visual evidence. Several frontend issues (e.g. #39 — hardcoded CSS colours) were only discovered post-merge because there was no requirement to include a screenshot in the PR. The developer reviewing the PR had to manually run the app locally to see what the output looked like.

**Root cause — two linked problems:**

**1. Template gap:** The `[3]` (Frontend MVP) and `[4]` (Frontend implementation) issue templates have no AC requiring a screenshot of the rendered UI. There is also no guidance in `CLAUDE.md` about capturing a screenshot before raising a PR for frontend changes.

**2. Infrastructure gap:** The k8s bot Dockerfile (`jemmy8oy/claude-code-telegram-k8s`) does not install Playwright or any headless browser. The bot cannot capture a screenshot of a running frontend app even if instructed to. Current Dockerfile analysis:
- ✅ Node.js 20, npm, `serve` (static server)
- ✅ .NET 10 SDK, Python 3.11
- ❌ No Chromium / Playwright browser binaries
- ❌ No `libatk-bridge2.0-0`, `libgbm-dev`, `libasound2` or other headless browser system deps

**Proposed fix — Template (immediate):**
Add to `[3]` and `[4]` frontend issue ACs:
```markdown
## Visual Evidence (mandatory for frontend changes)
- [ ] Screenshot of the rendered UI attached to the PR (use `npx serve build` or `npm run dev`)
- [ ] If running headless: capture via `playwright screenshot` or equivalent
- [ ] Screenshot shows the feature working at ≥1280×800 desktop viewport
```

Add to `CLAUDE.md`:
```markdown
## Frontend PR Screenshots (mandatory)
For any PR that modifies React components or CSS:
- Capture a screenshot of the relevant page/component before raising the PR.
- Attach it to the PR body using a markdown image link.
- If Playwright is available: `npx playwright screenshot --full-page http://localhost:5173 screenshot.png`
```

**Proposed fix — k8s Bot Dockerfile:**
Add Playwright + browser dependencies to the bot container so the AI can take screenshots automatically:
```dockerfile
# Install Playwright system dependencies
RUN apt-get update && apt-get install -y \
  libatk-bridge2.0-0 libdrm2 libgbm1 libglib2.0-0 libnss3 libxss1 \
  libasound2 libx11-xcb1 libxcb-dri3-0 libxcomposite1 libxcursor1 \
  libxdamage1 libxfixes3 libxrandr2 libxtst6 fonts-liberation \
  && rm -rf /var/lib/apt/lists/*

# Install Playwright + Chromium
RUN npm install -g playwright \
  && npx playwright install chromium
```

This is tracked as a new issue to be raised on `claude-code-telegram-k8s`.

---

## Gap 19 — CI/CD Pipelines Not Included in Template; `deploy.sh` Manual Workaround

**Severity:** 🔴 High

**Observed behaviour:** The `ci.yml` (unit tests, integration tests, E2E tests) and `docker-build-push.yml` (OCIR image push on merge to `main`) workflows were created manually during the POC rather than being part of the project scaffold. There was no CI from day one — tests passed or failed locally with no automated gate on PRs. Deployment was handled by `deploy.sh`, a local shell script requiring the developer's workstation to have `docker buildx`, `oci` CLI, and `kubectl` configured. This script is now fully superseded by `docker-build-push.yml`.

**Root cause:** `web-template` ships no `.github/workflows/` directory.

**Impact:**
- Tests weren't run automatically on PRs for the first half of the POC; bugs that tests would have caught were merged.
- Deployment required the developer to remember to run `deploy.sh` manually; the pipeline enforces this on every merge to `main`.
- `deploy.sh` contains project-specific hardcoded values (registry namespace, compartment ID, app name) that need updating per project — the GitHub Actions workflow parameterises these as repo vars/secrets, making it safer.

**Proposed fix — `web-template`:**

Add both workflows to `web-template/.github/workflows/` as part of the initial scaffold:

```
web-template/
  .github/
    workflows/
      ci.yml               ← test gate on all PRs and pushes
      docker-build-push.yml ← image build + OCIR push on merge to main
```

The deployment workflow references secrets (`OCIR_USERNAME`, `OCIR_AUTH_TOKEN`) and vars (`OCIR_REGISTRY`, `OCIR_NAMESPACE`) that won't exist until the operator configures the new project's repository settings. This is intentional — the workflow will fail with a clear "secret not found" error until the operator adds them. This is a better failure mode than having no pipeline at all.

**`deploy.sh` removal:**

Once the `docker-build-push.yml` pipeline is in the template, `deploy.sh` should be removed from the template entirely:
- Everything it does is covered by the pipeline (build, tag, push, registry purge, rollout restart).
- It requires local toolchain setup (docker buildx arm64, oci CLI, kubectl) that the pipeline handles in the GitHub Actions runner.
- Keeping it alongside the pipeline creates two diverging code paths for the same operation.

A note in `CLAUDE.md` and/or `docs/ai-workflow.md` should state: *"Deployment is handled by the `docker-build-push.yml` workflow on merge to `main`. Do not create or reference `deploy.sh` — this file is not part of the scaffold."*

---

## Gap 20 — Helm Template Names Not Updated at Scaffold Time

**Severity:** 🔴 High

**Where it hurt:** Deployment — PRs #83, #86, #90, #100, #101, #102. Six separate PRs were required across three days (2026-06-06 to 2026-06-09) to get the Helm deployment working. The root cause was that the `web-template` Helm chart contained hardcoded template-level names (`web-app-helm`, `balenthiran-helm`) that were not replaced when the project was scaffolded via `dotnet new web-template`.

**Root cause:** The Helm `_helpers.tpl` defined helper macros such as `web-app-helm.fullname` and `balenthiran-helm.fullname`. The `service.yaml` template referenced `balenthiranhelm.fullname` (a typo variant). At scaffold time, only the C# project names (`SolutionName → MacroMetrics`) were substituted — the Helm template references were not. The fix was discovered only once deployment was attempted and the AI corrected them via a manual debug commit ("fix: correct Helm config bugs ahead of deployment", 2026-06-06).

**Additional scaffolding gaps found:**
- `registryPrefix` defaulted to `lhr.ocir.io/balenthiran` rather than the project-specific namespace
- `fullnameOverride` was initially set to `web-app-helm` rather than the project name
- No standard ingress path convention was documented — the pattern `balenthiran.co.uk/{app-name}/` emerged through iteration
- App-specific secrets (`FRED_API_KEY`, `ConnectionStrings__DefaultConnection`) were added ad-hoc rather than being scaffolded out with clear "replace me" markers

**Evidence from POC git history:**
```
2026-06-06  "fix: correct Helm config bugs ahead of deployment"
            - web-app-helm.fullname → correct helper (service.yaml)
            - balenthiranhelm.fullname reference removed (typo in _helpers.tpl)
            - registryPrefix corrected to actual OCIR namespace
2026-06-09  PRs #100, #101, #102 — further ingress path / routing corrections
```

**Proposed fix:**

1. **In `helm/_helpers.tpl`** — replace all `web-app-helm` and `balenthiran-helm` references with `{{ .Values.fullnameOverride | default .Release.Name }}`. The `fullnameOverride` is the single scaffold-time variable the developer sets.

2. **In `helm/values.yaml`** — ship with a documented default ingress configuration:
   ```yaml
   # Set this to your app name at scaffold time (e.g. "macro-metrics")
   fullnameOverride: "your-app-name"

   ingress:
     enabled: true
     className: nginx
     hosts:
       - host: balenthiran.co.uk
     annotations:
       cert-manager.io/cluster-issuer: "letsencrypt-prod"
     tls:
       - hosts:
           - balenthiran.co.uk
         secretName: balenthiran-tls   # Reuse the shared wildcard cert

   apps:
     - name: backend
       ingress:
         path: /your-app-name/api      # Replace with fullnameOverride + /api
         pathType: Prefix
     - name: frontend
       ingress:
         path: /your-app-name          # Replace with fullnameOverride
         pathType: Prefix
   ```

   This establishes `balenthiran.co.uk/{app-name}/` as the standard URL pattern and reuses the existing `balenthiran-tls` cert — no per-project TLS setup required.

3. **App-specific secrets:** Do NOT include project-specific secret references (e.g. `FRED_API_KEY`, database connection strings) in the template `values.yaml`. Add a commented placeholder:
   ```yaml
   # App-specific secrets — add per project as needed:
   # env:
   #   - name: MY_SECRET
   #     valueFrom:
   #       secretKeyRef:
   #         name: <app-name>-secrets
   #         key: MY_SECRET_KEY
   ```

4. **Scaffold-time instruction in `CLAUDE.md`:**
   ```markdown
   ## Helm Scaffold Setup
   After scaffolding, update `helm/values.yaml`:
   1. Set `fullnameOverride` to your app name (lowercase, hyphenated)
   2. Update `ingress.apps[*].path` to match: `/{app-name}` and `/{app-name}/api`
   3. The `balenthiran-tls` cert and `balenthiran.co.uk` host are shared — do not change them
   4. Add app-specific secrets/env vars as needed — no secrets are pre-scaffolded
   ```

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
| Gap 15 — Testing strategy not in issue ACs | `web-template` `[3]` + `[5]` issue bodies; `CLAUDE.md` testing standards |
| Gap 16 — No Claude Code hooks | `web-template` `.claude/settings.json` + `CLAUDE.md` AI Guards section |
| Gap 17 — `waiting-for-ai` vs `action-ready` not documented | `web-template` `docs/ai-workflow.md`; `CLAUDE.md` label reference |
| Gap 18 (template part) — No screenshot gate for frontend PRs | `web-template` `[3]`/`[4]` issue ACs; `CLAUDE.md` screenshot rule |
| Gap 19 — CI/CD pipelines not in template; `deploy.sh` redundant | `web-template` `.github/workflows/ci.yml` + `docker-build-push.yml`; remove `deploy.sh`; `CLAUDE.md` deployment note |
| Gap 20 — Helm names not updated at scaffold time | `web-template` `helm/_helpers.tpl` (remove hardcoded names); `helm/values.yaml` (parameterised ingress defaults with `balenthiran.co.uk/{app-name}/` + `balenthiran-tls`); `CLAUDE.md` Helm scaffold section |

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
| Gap 15 — Testing strategy not surfaced in issue ACs | Medium (inconsistent test coverage) | Low | 🟠 Medium |
| Gap 16 — No Claude Code hooks / AI guards | Medium (preventable quality issues) | Low | 🟠 Medium |
| Gap 17 — `waiting-for-ai` vs `action-ready` not documented | Medium (wrong mode selected silently) | Low | 🟠 Medium |
| Gap 18 — No screenshot gate for frontend PRs / no headless browser in bot | Medium (visual bugs merged undetected) | Low (template) / Medium (Dockerfile) | 🟠 Medium |
| Gap 19 — CI/CD pipelines not in template; `deploy.sh` redundant | High (no automated test gate or deployment on day one) | Low | 🔴 High |
| Gap 20 — Helm template names not updated at scaffold time | High (5+ deployment iteration PRs) | Low | 🔴 High |
