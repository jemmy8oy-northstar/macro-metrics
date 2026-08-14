# Web-Template Audit — MacroMetrics POC Findings

**Purpose:** Map each retrospective finding to the specific file and line in `web-template` that needs to change. This is the actionable output of the POC retro — a concrete diff list for the template maintainer.

**Template repo:** `jemmy8oy/web-template`
**Reviewed files:** `CLAUDE.md`, `docs/ai-workflow.md`, `docs/specs/sdd-workflow.md`, `scripts/init-issues.mjs`

---

## Finding 1 — init-issues.mjs creates issues without `--assignee`

**File:** `web-template/scripts/init-issues.mjs`
**Line:** The `gh issue create` call in the `run()` loop (near the bottom)

**Current:**
```js
gh(`issue create --title "${issue.title}" --body-file "${bodyFile}" --milestone "${milestone}"`);
```

**Required:**
```js
// Get owner once at the top of run()
const owner = gh(`repo view --json owner --jq .owner.login`);

// Then in the loop:
gh(`issue create --title "${issue.title}" --body-file "${bodyFile}" --milestone "${milestone}" --assignee "${owner}"`);
```

**Why:** The `CLAUDE.md` convention says all issues and PRs must be assigned to the repo owner. The script doesn't implement this. Phase 6 issues created during MacroMetrics were ~70% unassigned, meaning the developer missed notifications.

---

## Finding 2 — CLAUDE.md assignee rule is stated but not enforced structurally

**File:** `web-template/CLAUDE.md`
**Section:** "GitHub Conventions"

**Current:**
```
- **PR assignment**: Every PR the AI raises must be assigned to `the repo owner`.
- **Issue assignment**: Every issue the AI acts on must be assigned to `the repo owner`.
```

**Issue:** The rule exists but there is no guidance on _how_ to find the owner username programmatically, which causes inconsistent application.

**Proposed addition:**
```
- **Finding owner**: Use `gh repo view --json owner --jq .owner.login` to get the owner login. Cache it — don't call it multiple times.
- **Verification**: After creating issues or PRs, run `gh issue view <N> --json assignees` to confirm the assignee was applied.
```

---

## Finding 3 — [1d] questionnaire doesn't capture database requirement

**File:** `web-template/docs/specs/sdd-workflow.md`, `web-template/docs/ai-workflow.md`, `web-template/scripts/init-issues.mjs` (the `[1d]` issue body)

**Current `[1d]` questionnaire:**
```
- What problem does this product solve, and for whom?
- What does a successful MVP look like?
- What are the key user workflows?
- Are there external APIs, data sources, or third-party integrations?
- Auth requirements?
- What is explicitly out of scope for MVP?
```

**Missing:**
```
- **Database**: Does this project require a persistent database?
  - [ ] Yes — PostgreSQL + EF Core (the template default)
  - [ ] No — stateless proxy or in-memory only (skip [4])
  If yes, note any domain hints (entities, relationships, key lookups).
```

**Why:** MacroMetrics was a stateless proxy. Every time the `[5a]` backend design issue was triggered, the AI re-asked "should I include EF Core entities?" because the template's `[4]` issue body assumes a DB. The question was asked 3 times across different passes.

**Also add** to the `[4]` issue body in `init-issues.mjs`:
```
## Note to AI

Before starting this issue, check `docs/specs/proposal.md` (from [1e]).
If the [1d] discussion confirmed **no database required**, close this issue immediately — it is not applicable to a stateless proxy project.
```

---

## Finding 4 — [1d] questionnaire doesn't capture external data sources

**File:** Same as Finding 3 (`[1d]` questionnaire)

**Missing:**
```
- **External data sources**: For each external API or data feed:
  - API name and base URL
  - Specific series IDs or endpoint paths needed
  - API key required? How stored (environment variable / K8s secret)?
  - Data cadence (daily / monthly / quarterly)?
  - Have you confirmed the series ID is accessible? (Paste a sample response if available)
```

**Why:** MacroMetrics assumed FRED hosted the Shiller CAPE ratio (it didn't). This assumption was not validated until Phase 6 implementation, causing a production bug (issue #76). Capturing and validating data sources in [1d] → [1e] would have caught this 2+ phases earlier.

---

## Finding 5 — sdd-workflow.md has no deployment phase

**File:** `web-template/docs/specs/sdd-workflow.md`

**Current:** Workflow ends at Phase [5] — "Backend — feature by feature". There is no deployment phase.

**Proposed addition** (after the Phase [5] section):

```markdown
---

### [8] — Deployment

**Triggered when:** All [5] issues are closed or explicitly signed off.

**AI action.** Apply `action-ready` to trigger.

AI raises a deployment PR containing:
- `Dockerfile` verified for production (multi-stage, non-root user)
- `nginx.conf` (or equivalent ingress config) with all required routes
- Helm chart values — image registry, ingress host, secret references
- GitHub Actions workflow for container build + push
- `docs/deployment.md` — all required environment variables documented

**Pre-deployment checklist for AI:**
- [ ] All routes from [1e] are accessible via the nginx/ingress config
- [ ] All environment variables (API keys, DB connection strings) are referenced by name only — never hardcoded
- [ ] Health check endpoint configured (`/api/status` or equivalent)
- [ ] Container registry path matches `helm/values.yaml` and the GitHub Actions workflow

**Branch discipline note:** The deployment PR targets `dev`. The human merges `dev → main` to trigger the production deployment.
```

**Why:** MacroMetrics had 5 ad-hoc deployment issues (#80, #81, #83, #85, #87) caused by deployment being unplanned. nginx.conf was missing, OCIR auth format was wrong, secrets were an afterthought.

---

## Finding 6 — sdd-workflow.md has no retro phase

**File:** `web-template/docs/specs/sdd-workflow.md`

**Proposed addition** (after the deployment phase):

```markdown
---

### [9] — Retrospective

**Triggered when:** Deployment is confirmed working or the project is at a natural stopping point.

**AI action.** Apply `action-ready` to trigger.

AI generates a retrospective in `docs/retros/<project-slug>/` covering:

| Document | Content |
|---|---|
| `00-overview.md` | POC timeline, what was built, open items at close |
| `01-process.md` | Phase-by-phase SDD assessment |
| `02-issues.md` | All issues categorised and assessed |
| `03-gaps.md` | Template gaps identified with proposed fixes |
| `04-technical.md` | Architecture decisions, bugs, technical debt |
| `05-recommendations.md` | Prioritised improvements for the next project |

The AI also opens PRs on `web-template` to apply any template fixes identified in the retro.

Human reviews and merges the retro PR.
```

---

## Finding 7 — No branch protection enforcement for dev-first workflow

**File:** `web-template` (missing file: `.github/workflows/require-dev-source.yml`)

**What happened:** MacroMetrics Phase 6 PRs all targeted `main` directly. By the time this was noticed, `main` was 46 commits ahead of `dev` — all backend implementation in the wrong branch (issue #80).

**The sdd-workflow.md does say:** "All PRs target `dev`. Never target `main`." But guidance alone wasn't sufficient.

**Proposed new file** in `web-template/.github/workflows/require-dev-source.yml`:
```yaml
name: Enforce dev-first workflow
on:
  pull_request:
    branches: [main]
jobs:
  check-source-branch:
    runs-on: ubuntu-latest
    steps:
      - name: Block direct-to-main PRs
        run: |
          if [[ "${{ github.head_ref }}" != "dev" && "${{ github.head_ref }}" != hotfix/* ]]; then
            echo "::error::PRs to main must come from dev or a hotfix/* branch."
            echo "::error::Raise your PR against dev first, then merge dev → main."
            exit 1
          fi
```

Adding this to the web-template means every project bootstrapped from it inherits the enforcement automatically — without any additional configuration.

---

## Finding 8 — No "Assumptions & Decisions" section in PR template

**File:** `web-template` (the AI-generated PR convention in `CLAUDE.md` or `docs/ai-workflow.md`)

**Current PR format** (from `docs/ai-workflow.md` under "PR ↔ Issue Linking"):
> "Includes `Closes #N` in the PR body"
> "Posts a comment on the issue: 🤖 PR raised: #N — please review when ready."
> "Assigns the PR to the repo owner"

**Proposed addition** to every AI-raised PR body:
```markdown
## Assumptions & Decisions

> Any assumption made during implementation that was not explicitly specified in the issue or spec.
> If you disagree with any item, comment and re-apply `action-ready` — the AI will revise.

| # | Assumption | Rationale | If incorrect... |
|---|---|---|---|
| 1 | | | |
```

**Why:** Three silent assumptions caused bugs or rework in MacroMetrics:
- FRED hosts the Shiller CAPE ratio → it doesn't (issue #76)
- PRs target `main` → should have been `dev` (issue #80)
- CSS can use hardcoded colours → should use CSS custom properties (issue #39)

A structured assumptions table gives the developer a 30-second scan path to catch these before merge.

**Where to add it:** In `CLAUDE.md` under "GitHub Conventions", add:
```
- **Assumptions table**: Every AI-raised PR body must include a populated "Assumptions & Decisions" table.
  Minimum one row. Use "N/A — no ambiguous assumptions" only if genuinely nothing was assumed.
```

---

## Finding 9 — action-ready relabelling friction not addressed

**File:** `web-template/CLAUDE.md` and `web-template/docs/ai-workflow.md`

**Current behaviour:** When the AI finishes a pass (complete or partial), it removes `action-ready` and posts a comment. The developer must manually re-apply `action-ready` to trigger the next pass.

**Problem:** For multi-pass issues (orchestrator issues that require Q&A before implementation), and for timed-out passes, this creates repeated manual steps with no notification to the developer that a re-trigger is needed.

**Proposed additions to CLAUDE.md:**

```markdown
## Multi-pass Issues

When a task is **partially complete** (e.g., you ran out of time before raising the PR, or you've asked a blocking question):
1. Post a comment clearly labelling it as a partial pass: `## Pass N — Partial`
2. List what was completed and what remains
3. If you asked a blocking question: remove `action-ready`, wait for developer's answer
4. If the partial is due to scope/time (no blocking question): re-apply `action-ready` yourself so the developer doesn't need to manually trigger the next pass

When a task is **complete** (PR raised):
1. Post the standard `🤖 PR raised: #N — please review when ready.` comment
2. Remove `action-ready` — the ball is in the developer's court
```

---

## Finding 10 — [3a] equivalent needs a clearer brief

**File:** `web-template/docs/specs/sdd-workflow.md` and `web-template/scripts/init-issues.mjs` (the `[3]` Frontend MVP issue body)

**Current `[3]` issue ACs:**
```
- All user workflows from [1e] implemented as React pages/components
- Wired to RTK Query hooks (Faker data from [2] skeleton backend)
- Every workflow is functional end to end
- Minimal styling — MVP, not a polished product
- PR raised and merged
```

**Missing sections** (discovered through 4+ PR review rounds in MacroMetrics):
```
- [ ] Tech decision doc updated with: chart library choice, date library, state management approach
- [ ] API skeleton contracts reviewed — if any endpoint shapes differ from [2], flag in PR
- [ ] Frontend TDD approach confirmed: Vitest + React Testing Library, one test file per component
- [ ] Fake data strategy confirmed: [2] backend skeleton (preferred) or local mocks
```

The MacroMetrics `[3a]` required 4+ review rounds because these sections were all discovered through PR comments rather than being in the brief.

---

## Finding 11 — CLAUDE.md Has No Multi-pass Context Instruction

**File:** `web-template/CLAUDE.md`
**Section:** Agent conventions (new section to add)

**Context from POC post-retro discussion:** Issue #88 demonstrated that the AI re-started its full analysis from scratch on the second trigger because there is no instruction to read existing comments before posting. The bot read the issue body ("generate a retrospective") and re-asked questions the developer had already answered.

**Proposed addition to CLAUDE.md:**
```markdown
## Multi-pass Issue Behaviour

Before posting any comment on an issue, read ALL existing comments in full.
- If you have already asked clarifying questions and the owner has answered them: proceed with the implementation. Do not re-ask.
- If you have already proposed a plan or analysis: do not repeat it — continue from where you left off.
- If a previous pass left a structured summary comment: read that summary first and skip re-reading files already listed there.
```

**Why:** This is a zero-effort, high-impact change. It won't be a perfect fix (the full fix requires the fork to use the latest unanswered comment as the task prompt — see [fork #33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33)), but it eliminates the most common failure mode: re-asking questions that were already answered.

---

## Finding 12 — Fork Issues Raised as Follow-ons From This Retro

The following issues were raised directly on the `claude-code-telegram-k8s` fork as a result of the retro discussion. They are not template changes but are documented here for traceability.

| Issue | What it addresses | Template equivalent |
|---|---|---|
| [claude-code-telegram-k8s #33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33) | per-label `max_turns` config; use latest unanswered comment as task prompt | Reduces re-asking pattern; enables per-issue-type turn budgets |
| [claude-code-telegram-k8s #34](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/34) | Post ⚠️ GitHub comment + Telegram alert when `max_turns` hit; raise default limit to 100 | Turns silent failures into actionable signals; reduces how often the limit is hit |

These fork changes complement the template changes in this audit: the template changes improve the AI's behaviour via instruction; the fork changes improve the infrastructure that triggers and constrains the AI.

---

## Finding 13 — Testing Strategy Not Referenced in Issue ACs

**File:** `web-template/scripts/init-issues.mjs` (the `[3]` and `[5]` issue bodies)

**Context from post-retro discussion:** The developer noted the web template "does not talk about testing at all." The `docs/specs/testing-strategy.md` exists and is comprehensive, but:
- It is referenced in only one `CLAUDE.md` table row
- The `[3]` (Frontend MVP) issue body has no testing AC
- The `[5]` (Backend feature) issue body mentions TDD but doesn't link the strategy doc
- No issue AC includes a "tests must pass before PR is raised" gate

**Proposed changes to `init-issues.mjs`:**

Add to the `[3]` issue body template:
```markdown
## Testing (mandatory — see `docs/specs/testing-strategy.md`)
- [ ] Each component has at least one Vitest + RTL test covering its key behaviour
- [ ] Tests are written spec-first (test before component)
- [ ] `npm test` runs with zero failures before the PR is raised
```

Add to each `[5]` issue body template:
```markdown
## Testing (mandatory — see `docs/specs/testing-strategy.md`)
- [ ] Unit test written first (TDD) for each new service method — tests green before implementation proceeds
- [ ] Integration test scenario defined (Phase 5) or implemented (end of Phase 6)
- [ ] `dotnet test` runs with zero failures before the PR is raised
```

Add to `CLAUDE.md` (new "Testing Standards" section):
```markdown
## Testing Standards (mandatory)

Before raising any PR, all tests must pass. Testing is never optional.
- Backend: Write tests first (TDD). `dotnet test` — zero failures.
- Frontend: Write tests spec-first. `npm test` — zero failures.
- Strategy + examples: `docs/specs/testing-strategy.md`
```

---

## Finding 14 — Claude Code Hooks (AI Guards) Not Configured

**File:** `web-template` (missing file: `.claude/settings.json`)

**Context from post-retro discussion:** The developer asked whether "command hooks in Windsor/Cursor" exist in Claude Code — they do. Claude Code supports `PreToolUse` and `PostToolUse` hooks in `.claude/settings.json`. These are shell commands that run automatically before/after specific tool calls, outside the AI's context window, and cannot be overridden by AI instructions.

**POC problems this would have prevented:**

| Issue | Hook type | Guard |
|---|---|---|
| Hardcoded CSS colours (#39) | PostToolUse on Write/Edit `.css`/`.tsx` | Auto-prettier — flags or fixes formatting |
| Direct-to-main push (#80) | PreToolUse on Bash | Block `git push.*main` |
| Missing `--assignee` | PostToolUse on Bash | Warn when `gh issue create` lacks `--assignee` |

**Proposed new file:** `web-template/.claude/settings.json`
```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [{
          "type": "command",
          "command": "FILE=$CLAUDE_TOOL_INPUT_FILE_PATH; case \"$FILE\" in *.ts|*.tsx|*.css) npx prettier --write \"$FILE\" 2>/dev/null || true;; *.cs) dotnet format --include \"$FILE\" 2>/dev/null || true;; esac"
        }]
      }
    ],
    "PreToolUse": [
      {
        "matcher": "Bash",
        "hooks": [{
          "type": "command",
          "command": "if echo \"$CLAUDE_TOOL_INPUT_COMMAND\" | grep -qE 'git push.*(origin )?main'; then echo 'AI guard: direct push to main is blocked. Raise a PR against dev instead.' && exit 2; fi"
        }]
      }
    ]
  }
}
```

**Proposed addition to `CLAUDE.md`:**
```markdown
## AI Guards (Claude Code Hooks)

The `.claude/settings.json` in this repo defines shell hooks that run automatically before/after Claude's tool calls. These are structural guards — they enforce rules that cannot be overridden by AI instructions.

Active guards:
- **Linter (PostToolUse):** `prettier` runs after every `.ts`/`.tsx`/`.css` write. `dotnet format` runs after every `.cs` write.
- **Branch guard (PreToolUse):** `git push` to `main` is blocked. Target `dev`.

Do not modify or remove these guards. If a guard is triggering incorrectly, raise an issue.
```

---

## Finding 15 — Bot Prompt Audit: Issue Body Used as Prompt, Comments Not Fetched

**File:** `claude-code-telegram` source — `src/events/handlers.py` (`_build_github_prompt`)

**Context from post-retro discussion:** The developer asked for a review of the claude-code-telegram bot repo to identify friction sources. A full audit of the prompts and workflow was completed.

**Key findings from `_build_github_prompt()` (the core prompt builder):**

| Finding | Code evidence | Impact |
|---|---|---|
| Issue prompt passes only `issue.get('body')` | `handlers.py` line — `f"Body:\n{issue.get('body')...}"` | AI doesn't see comments from previous passes — explains re-asking pattern |
| PR prompt passes only `pr.get('body')` | `handlers.py` — `f"Description:\n{pr.get('body')...}"` | AI must self-discover review comments via `gh pr review` |
| No comments fetched from GitHub API | Entire `_build_github_prompt()` constructs prompt from payload only | Root cause of multi-pass context loss |
| `force_new=True` on every trigger | `_run_github_task()` call | Each trigger is a fully fresh session — no continuity |
| No `stop_reason` detection | Not present in `handlers.py` or `sdk_integration.py` | Silent `max_turns` failures — no ⚠️ comment posted |

**`waiting-for-ai` vs `action-ready` mode:**
- `waiting-for-ai` on issues → discussion mode: "respond with a comment... do NOT start implementing"
- `action-ready` on issues → implementation mode: "implement the task... raise a PR"
- **Potential friction:** If the developer uses `waiting-for-ai` to ask a follow-up on an in-progress issue (common), the bot goes into discussion mode and will not implement — the developer should use `action-ready` to trigger continued implementation

**Confirmed fork issues from audit:**
- [#33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33) — fetch comments + use latest unanswered as prompt (fixes re-asking)
- [#34](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/34) — detect `stop_reason == "max_turns"` + post ⚠️ comment (fixes silent failures)

**Also noted — `waiting-for-ai` mode clarification:**
The prompt currently says "do NOT start implementing" for `waiting-for-ai` issues. This is correct for first-contact (discussion phase) but may be wrong for re-triggers during multi-pass implementation. Consider adding a label variant (e.g., `waiting-for-ai-continue`) that triggers implementation mode without requiring `action-ready`. This is a fork-level change.

---

## Finding 16 — `waiting-for-ai` vs `action-ready` Mode Not Documented

**File:** `web-template/docs/ai-workflow.md`, `web-template/CLAUDE.md`

**Context from post-retro discussion:** The developer used `waiting-for-ai` on re-triggers where they expected continued implementation. The bot is programmed to refuse implementation in `waiting-for-ai` mode — this is correct but undocumented, creating invisible friction. The developer had no quick reference to know which label to apply and when.

**Current state:** Both labels appear in the workflow but their mode difference is not called out anywhere as a distinct rule.

**Label mode summary:**

| Label | Bot mode | Use when |
|---|---|---|
| `waiting-for-ai` | Discussion only — no code written, no PR raised | New issues, Q&A, planning passes |
| `action-ready` | Full implementation — code written, PR raised | You want the bot to build something |

**Proposed additions:**

1. **`docs/ai-workflow.md`** — add a "Label Quick Reference" section near the top:
```markdown
### Label Quick Reference

| Label | Effect | When to apply |
|---|---|---|
| `waiting-for-ai` | Bot enters **discussion mode** — answers questions and proposes plans. Will NOT write code or raise a PR. | New issues, Q&A rounds, requesting analysis |
| `action-ready` | Bot enters **implementation mode** — writes code, runs tests, raises a PR. | After you've reviewed a plan and want implementation to begin or continue |

> **Re-trigger tip:** If you want to continue implementation after a partial pass, use `action-ready` — not `waiting-for-ai`. The bot will note this at the end of each partial pass.
```

2. **`CLAUDE.md`** — add to the multi-pass instruction:
```markdown
At the end of every partial pass, state explicitly: "Re-apply `action-ready` (not `waiting-for-ai`) to continue implementation."
```

---

## Finding 17 — No Screenshot Gate for Frontend PRs / No Headless Browser in Bot Container

**Files:** `web-template/scripts/init-issues.mjs` (`[3]`/`[4]` issue bodies), `web-template/CLAUDE.md`, `claude-code-telegram-k8s/Dockerfile`

**Context from post-retro discussion:** The developer raised that screenshot requirements for frontend PRs are missing, and that testing dependencies (specifically frontend rendering / screenshot tooling) need to be added to the k8s bot.

**Current Dockerfile analysis (`claude-code-telegram-k8s`):**

| Dependency | Present? | Notes |
|---|---|---|
| Node.js 20 | ✅ | via `nodesource` setup |
| `serve` (static server) | ✅ | via `npm install -g serve` |
| .NET 10 SDK | ✅ | via dotnet-install.sh |
| Python 3.11 | ✅ | base image |
| Playwright / Chromium | ❌ | Not installed |
| Headless browser system libs | ❌ | `libgbm1`, `libnss3`, etc. absent |

Without Playwright, the bot cannot capture a screenshot of a running React app even if instructed to do so in CLAUDE.md.

**Proposed template change — `[3]`/`[4]` issue bodies:**
```markdown
## Visual Evidence (mandatory for frontend changes)
- [ ] Screenshot of the rendered page/component attached to the PR body
- [ ] Viewport: ≥1280×800 (desktop)
- [ ] If Playwright available: `npx playwright screenshot --full-page http://localhost:5173 pr-screenshot.png`
```

**Proposed `CLAUDE.md` addition:**
```markdown
## Frontend PR Screenshots (mandatory)
For any PR that modifies React components, pages, or CSS:
1. Start the dev server: `npm run dev` or `npx serve dist`
2. Capture: `npx playwright screenshot --full-page http://localhost:5173 pr-screenshot.png`
3. Attach to the PR body.
If Playwright is not available in the environment, note this in the PR and add a TODO to install it.
```

**Proposed k8s Dockerfile change:**
```dockerfile
# Headless browser for frontend screenshot capture
RUN apt-get update && apt-get install -y \
  libatk-bridge2.0-0 libdrm2 libgbm1 libglib2.0-0 libnss3 libxss1 \
  libasound2 libx11-xcb1 libxcb-dri3-0 libxcomposite1 libxcursor1 \
  libxdamage1 libxfixes3 libxrandr2 libxtst6 fonts-liberation \
  && rm -rf /var/lib/apt/lists/*

RUN npm install -g playwright && npx playwright install chromium
```

This enables the bot to take automated screenshots of frontend changes before raising a PR, closing the visual evidence gap completely.

---

## Finding 18 — CI/CD Pipelines Absent from `web-template`; `deploy.sh` Is a Manual Workaround

**Files:** `web-template/.github/workflows/` (missing), `web-template/deploy.sh` (present but redundant once pipeline is added)

**Context from post-retro discussion (2026-06-10):** The developer noted that the testing and deployment pipelines had to be set up manually during the POC. The observation was that if they were part of the template they would exist by default, that the deployment pipeline can simply fail until the operator adds the required secrets/vars, and that once the pipeline is in place `deploy.sh` should be removed.

**Current state (`macro-metrics` as reference implementation):**

| Workflow | Status | What it does |
|---|---|---|
| `.github/workflows/ci.yml` | ✅ Exists (added manually mid-POC) | Backend xUnit tests, Python sidecar pytest, E2E via docker compose |
| `.github/workflows/docker-build-push.yml` | ✅ Exists (added manually mid-POC) | Builds all three Docker images; pushes to OCIR with GitVersion semver tags |
| `deploy.sh` | ✅ Exists (original manual workaround) | Local shell script: docker buildx build → push → OCI registry purge → kubectl rollout restart |

**The gap:** Neither `ci.yml` nor `docker-build-push.yml` exist in `web-template`. Every project bootstrapped from the template starts with no automated test gate and no pipeline-based deployment.

**Why `deploy.sh` should be removed once the pipeline is in the template:**

1. **Functional duplication** — `docker-build-push.yml` builds, tags, and pushes all images; `deploy.sh` does the same thing via local `docker buildx` commands.
2. **Toolchain requirement** — `deploy.sh` requires the developer's machine to have `docker buildx` configured for ARM64 cross-compilation, the OCI CLI authenticated, and `kubectl` pointing at the production cluster. The GitHub Actions workflow runs on a native ARM64 runner with no local toolchain dependency.
3. **Hardcoded values** — `deploy.sh` contains project-specific constants (`REGISTRY_NAMESPACE`, `COMPARTMENT_ID`, `APP_NAME`, `KUBERNETES_NAMESPACE`) that need editing per project. The pipeline externalises these as repo vars/secrets.
4. **No semver** — `deploy.sh` tags images with `git rev-parse --short HEAD` only. The pipeline uses GitVersion for proper semver tagging.

**Proposed fix:**

Add to `web-template/.github/workflows/`:
- `ci.yml` — parameterised test runner for backend (xUnit), sidecar (pytest), and E2E (docker compose); runs on all PRs targeting `main`
- `docker-build-push.yml` — image build + OCIR push on merge to `main`; fails gracefully until `OCIR_USERNAME`, `OCIR_AUTH_TOKEN`, `OCIR_REGISTRY`, `OCIR_NAMESPACE` are configured in repo settings

Remove `deploy.sh` from `web-template`.

Add to `CLAUDE.md`:
```markdown
## Deployment
Deployment is automated via `.github/workflows/docker-build-push.yml` on merge to `main`.
Do NOT create or reference `deploy.sh` — this file is not part of the scaffold.
The deployment workflow requires four repository secrets/vars to be configured:
- Secret: `OCIR_USERNAME`, `OCIR_AUTH_TOKEN`
- Variable: `OCIR_REGISTRY`, `OCIR_NAMESPACE`
Until these are set, the deployment workflow will fail — this is expected and the correct signal to the operator.
```

---

## Finding 19 — Helm Template Names Not Replaced at Scaffold Time

**Files affected:**
- `web-template/helm/_helpers.tpl` — contains `web-app-helm.fullname` and `balenthiran-helm.fullname` definitions
- `web-template/helm/templates/service.yaml` — referenced `balenthiranhelm.fullname` (typo; neither template name was updated)
- `web-template/helm/values.yaml` — `fullnameOverride`, `registryPrefix`, and `ingress.path` values are template-specific, not project-generic

**Context from post-retro discussion (2026-06-10):** The developer noted that some Helm variables didn't change from `web-app-helm` to `macro-metrics`, and that there may be a `balenthiran-helm` reference still present. A review of the deployment PRs (#83 through #102) and git log confirmed the issue.

**Evidence from git history:**
```
2026-06-06  "fix: correct Helm config bugs ahead of deployment"
            - Fix YFINANCE__SidecarBaseUrl: was 'macro-metrics-sidecar' but Helm generates
              'macro-metrics-yfinance-sidecar' (fullnameOverride + app.name)
            - Fix service.yaml: referenced 'balenthiranhelm.fullname' instead of
              'web-app-helm.fullname' — would break helm template rendering entirely
            - Fix registryPrefix to match actual OCIR repos

2026-06-09  PRs #100, #101, #102 — further ingress path and routing corrections
            (frontend base path `/macro-metrics` not wired in vite.config.ts + Program.cs)
```

**Total deployment iteration PRs:** #83, #86, #90, #100, #101, #102 — six PRs across three days to get deployment working from scratch.

**Required changes to `web-template`:**

**`helm/_helpers.tpl`:**
```
Current: {{- define "web-app-helm.fullname" -}}
         {{- define "balenthiran-helm.fullname" -}}

Required: {{- define "app.fullname" -}}
          {{- .Values.fullnameOverride | default .Release.Name | trunc 63 | trimSuffix "-" }}
          {{- end }}
```
Remove all `web-app-helm.*` and `balenthiran-helm.*` named helpers. Replace with a single `app.fullname` helper that uses `fullnameOverride` (set by the developer at scaffold time).

**`helm/values.yaml`:**
```yaml
# Developer sets this at scaffold time — all naming flows from it
fullnameOverride: "your-app-name"      # e.g. "macro-metrics"

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
      secretName: balenthiran-tls      # shared wildcard cert — no per-project TLS setup

registryPrefix: "lhr.ocir.io/lr7uc6l49odc"   # shared OCIR namespace — constant

apps:
  - name: backend
    ingress:
      path: /your-app-name/api         # developer replaces 'your-app-name'
      pathType: Prefix
  - name: frontend
    ingress:
      path: /your-app-name             # developer replaces 'your-app-name'
      pathType: Prefix
```

**Note on secrets:** App-specific secrets (`FRED_API_KEY`, `ConnectionStrings__DefaultConnection`) should NOT appear in the template `values.yaml`. Ship a commented placeholder only:
```yaml
# App-specific secrets — add per project:
# env:
#   - name: MY_API_KEY
#     valueFrom:
#       secretKeyRef:
#         name: {{ .Values.fullnameOverride }}-secrets
#         key: MY_API_KEY
```

**`CLAUDE.md` addition (new "Helm Scaffold Setup" section):**
```markdown
## Helm Scaffold Setup

After creating a new project from this template, update `helm/values.yaml`:
1. Set `fullnameOverride` to your app name (lowercase, hyphens — e.g. `my-app`)
2. Set `apps[*].ingress.path` to `/{app-name}` (frontend) and `/{app-name}/api` (backend)
3. The `balenthiran.co.uk` host, `balenthiran-tls` cert, and `registryPrefix` are shared — do not change them
4. Add app-specific env vars / secrets as needed — none are pre-scaffolded
5. Do NOT add `deploy.sh` — deployment is via `docker-build-push.yml` pipeline (see Deployment section)
```

---

## Retro Follow-on Repo Map

When applying the fixes from this retrospective, the following repositories need changes. This table is intended to guide the bot (or developer) running retro follow-on tasks so no repo is missed.

| Repo | Track | What changes |
|---|---|---|
| `jemmy8oy/web-template` | A (template) | `CLAUDE.md` (all template instruction gaps + deployment note + Helm scaffold section), `docs/ai-workflow.md` (label quick ref, deployment + retro phases), `docs/specs/sdd-workflow.md` (phase 8+9), `scripts/init-issues.mjs` (assignee, testing ACs, screenshot ACs), `.github/workflows/require-dev-source.yml` (new — branch guard), `.github/workflows/ci.yml` (new — test gate), `.github/workflows/docker-build-push.yml` (new — OCIR deploy), `.claude/settings.json` (new — AI guard hooks), `helm/_helpers.tpl` (parameterise — remove `web-app-helm`/`balenthiran-helm`), `helm/values.yaml` (ingress defaults: `balenthiran.co.uk/{app-name}/`, `balenthiran-tls`); **remove** `deploy.sh` |
| `jemmy8oy/claude-code-telegram-k8s` | B (fork infra) | `Dockerfile` (Playwright + headless browser), `values.yaml` (claudeMaxTurnsByLabel), plus any config consumed by issues [#33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33) and [#34](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/34) |
| `jemmy8oy/claude-code-telegram` | B (fork source) | `src/events/handlers.py` — `_build_github_prompt()` to include comments and use latest unanswered; add `stop_reason` detection |
| `jemmy8oy/macro-metrics` | Project-specific | No further changes post-merge of this PR; CLAUDE.md already reflects current project state |

> **Bot instruction:** When actioning retro follow-on tasks, read this table first. Check each repo for relevant open issues or PRs before creating duplicates.

---

## Summary — File Change Map

| File | Change type | Priority |
|---|---|---|
| `scripts/init-issues.mjs` | Add `--assignee` to `gh issue create` | 🔴 Critical |
| `CLAUDE.md` | Add owner lookup guidance; add Assumptions table rule | 🔴 Critical |
| `CLAUDE.md` | Add multi-pass behaviour instruction (Finding 11) | 🔴 Critical |
| `docs/ai-workflow.md` | Add deployment phase, retro phase, multi-pass behaviour | 🔴 Critical |
| `docs/specs/sdd-workflow.md` | Add deployment phase, retro phase; add "no DB" path | 🔴 Critical |
| `.github/workflows/require-dev-source.yml` | New file — enforce dev-first for all bootstrapped projects | 🔴 Critical |
| `scripts/init-issues.mjs` (`[1d]` body) | Add database + data source fields | 🟠 High |
| `scripts/init-issues.mjs` (`[4]` body) | Add "skip if no DB" note | 🟠 High |
| `scripts/init-issues.mjs` (`[3]` body) | Add tech decisions + TDD + fake data sections | 🟠 High |
| `CLAUDE.md` | Add multi-pass / self-relabelling guidance | 🟠 High |
| All PR templates | Add Assumptions & Decisions table | 🟠 High |
| Fork: `claude-code-telegram-k8s` | [#33] per-label max_turns + latest-comment-as-prompt | 🔴 Critical |
| Fork: `claude-code-telegram-k8s` | [#34] max_turns visibility + raise default limit | 🔴 Critical |
| `scripts/init-issues.mjs` (`[3]` + `[5]` body) | Add testing ACs + link to `testing-strategy.md` | 🟠 High |
| `CLAUDE.md` | Add Testing Standards section | 🟠 High |
| `web-template` `.claude/settings.json` | New file — linter + branch-guard hooks | 🟠 High |
| `CLAUDE.md` | Add AI Guards section describing active hooks | 🟠 High |
| `docs/ai-workflow.md` | Add Label Quick Reference (`waiting-for-ai` vs `action-ready`) | 🟠 High |
| `CLAUDE.md` | Add re-trigger instruction to partial-pass closings | 🟠 High |
| `scripts/init-issues.mjs` (`[3]`/`[4]` body) | Add screenshot AC | 🟠 High |
| `CLAUDE.md` | Add Frontend PR Screenshots section | 🟠 High |
| Fork: `claude-code-telegram-k8s` `Dockerfile` | Install Playwright + Chromium + system headless deps | 🟠 High |
| `.github/workflows/ci.yml` | New file — test gate (backend xUnit, sidecar pytest, E2E) on all PRs | 🔴 Critical |
| `.github/workflows/docker-build-push.yml` | New file — OCIR image push on merge to `main` (fails until secrets configured) | 🔴 Critical |
| `deploy.sh` | **Remove** — superseded by `docker-build-push.yml` pipeline | 🔴 Critical |
| `CLAUDE.md` | Add Deployment section directing to pipeline; note `deploy.sh` is not scaffolded | 🔴 Critical |
| `helm/_helpers.tpl` | Parameterise — replace `web-app-helm.fullname` + `balenthiran-helm.fullname` with `app.fullname` using `fullnameOverride` | 🔴 Critical |
| `helm/values.yaml` | Ship with `balenthiran.co.uk/{app-name}/` ingress defaults + `balenthiran-tls` cert; remove hardcoded template names; no app-specific secrets | 🔴 Critical |
| `CLAUDE.md` | Add "Helm Scaffold Setup" section with the three post-scaffold steps | 🔴 Critical |
