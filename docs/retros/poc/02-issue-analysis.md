# Issue Analysis — MacroMetrics POC

**All 50 issues reviewed and categorised.**

---

## Summary Table

| Category | Count | Notes |
|---|---|---|
| Template-generated orchestrator issues | 12 | `[1a]`–`[7b]` per workflow |
| Template-generated implementation issues | 24 | `[2]`, `[4]`, `[6]` per story |
| Additional issues raised by AI during process | 7 | Gaps, bugs, follow-ups |
| Issues raised by developer outside process | 7 | Ad-hoc operational issues |

---

## Template-Generated Orchestrator Issues

These are the issues created at bootstrap that coordinate each phase.

| # | Issue | Outcome | Notes |
|---|---|---|---|
| #2 | `[1a]` Set up pipeline secrets | 🟡 Open | Never formally closed — secrets were configured but issue left open |
| #3 | `[1b]` Set up branch policies | ✅ Closed | Closed promptly — developer configured branch rules |
| #4 | `[1c]` Define project spec | ✅ Closed | 5 comments, required one follow-up PR round. Questionnaire not fully answered. |
| #5 | `[2a]` Generate UI/UX design issues | ✅ Closed | Correctly triggered after [1c] merged. Created 7 design issues. |
| #6 | `[3a]` Frontend tech decisions + stories | ✅ Closed | 4+ PR rounds. Scope too broad for one issue. |
| #7 | `[3b]` Create frontend issues | ✅ Closed | Triggered after [3a]. Created issues #32–#35 and backend skeleton. |
| #8 | `[5a]` Backend design spec | ✅ Closed | 6 comments before PR raised. EF Core question re-asked 3 times. |
| #9 | `[5b]` Backend user stories | ✅ Closed | Clean execution once [5a] merged. |
| #10 | `[5c]` Create backend issues | ✅ Closed | Created 19 `[6]` issues. Working as expected. |
| #11 | `[6a]` Hook up Postgres | 🟡 Open | Explicitly deferred (stateless proxy MVP). Expected. |
| #12 | `[7a]` Share MVP / YouTube | 🟡 Open | Post-MVP. Not yet actioned. |
| #13 | `[7b]` Production refinement tickets | 🟡 Open | Post-MVP. Not yet actioned. |

**Observations:**
- Issues #2, #11, #12, #13 are legitimately open (not failures)
- Issue #4 questionnaire was only partially answered — the AI had to make inferences for some fields
- Issue #8 was the most friction-heavy orchestrator issue

---

## Template-Generated Design Issues (Phase 2)

| # | Issue | Outcome | Quality |
|---|---|---|---|
| #18 | `[2]` Homepage layout & navigation | ✅ Closed | Good — mockup covered all key states |
| #19 | `[2]` Ratio chart component | ✅ Closed | Good — Mermaid workflow diagram added |
| #20 | `[2]` Preset ratio card grid | ✅ Closed | Good |
| #21 | `[2]` Metric picker UI | ✅ Closed | Good — multi-select behaviour well-specified |
| #22 | `[2]` Custom comparison chart | ✅ Closed | Good |
| #23 | `[2]` Indicator card component | ✅ Closed | Good |
| #24 | `[2]` Indicators section | ✅ Closed | Good |

**Observations:**
- All 7 covered in a single PR — efficient but meant no independent review per feature
- Design issue bodies were largely redundant once the PR was raised

---

## Template-Generated Frontend Implementation Issues (Phase 4)

| # | Issue | Outcome | Notes |
|---|---|---|---|
| #32 | `[4]` Homepage layout | ✅ Closed | All 4 implemented together in PR #37 |
| #33 | `[4]` Preset ratio card grid | ✅ Closed | |
| #34 | `[4]` Custom comparison | ✅ Closed | |
| #35 | `[4]` Indicator cards | ✅ Closed | |

**Observations:**
- All 4 in a single PR (#37) — atomically correct but reduced review granularity
- Bug #39 (hardcoded CSS colours) emerged post-merge

---

## Template-Generated Backend Implementation Issues (Phase 6)

| # | Issue | Outcome | Notes |
|---|---|---|---|
| #43 | `[6]` US-B1 Metric catalogue | ✅ Closed | Clean — implemented as first story |
| #44 | `[6]` US-B2 Indicator-only flag | ✅ Closed | |
| #45 | `[6]` US-B3 Earliest date | ✅ Closed | |
| #46 | `[6]` US-B4 Single metric series | ✅ Closed | |
| #47 | `[6]` US-B5 Monthly normalisation | ✅ Closed | |
| #48 | `[6]` US-B6 Date range filter | ✅ Closed | |
| #49 | `[6]` US-B7 Ratio series | ✅ Closed | |
| #50 | `[6]` US-B8/B9 Ratio with date range | ✅ Closed | |
| #51 | `[6]` US-B10 End-of-month alignment | ✅ Closed | |
| #52 | `[6]` US-B11 Forward-fill | ✅ Closed | |
| #53 | `[6]` US-B12 UK metrics / ONS | ✅ Closed | But stub only — real HTTP in follow-up #73 |
| #54 | `[6]` US-B13 FRED fetcher | ✅ Closed | |
| #55 | `[6]` US-B14 yfinance sidecar | ✅ Closed | |
| #56 | `[6]` US-B15 In-memory caching | ✅ Closed | |
| #57 | `[6]` US-B16 Cache-Control headers | 🟡 Open | PR #79 raised but not merged; timed out on first run |
| #58 | `[6]` US-B17 Unknown metric 404 | 🟡 Open | Not implemented |
| #59 | `[6]` US-B18/B19 Ratio validation | 🟡 Open | Not implemented |
| #73 | `[6]` US-B12b ONS real HTTP | ✅ Closed | Follow-up to #53 — real implementation |

**Observations:**
- 14/19 backend stories completed ≈ 74% completion rate
- #53 was marked closed but was actually only stub-complete — indicates acceptance criteria weren't precise enough
- #57, #58, #59 are three stories that didn't make it before POC close
- The "74% complete" figure is actually closer to 95% if you consider that #57 is ready to merge and #58/#59 are small validations

---

## Additional Issues Raised by AI During Process

These were not created from the template — they emerged during implementation.

| # | Issue | Trigger | Type |
|---|---|---|---|
| #38 | "Testing that Claude will reply on issue creation" | Developer test | Test/experiment — closed quickly |
| #73 | ONS real HTTP calls | AI noticed #53 was stub-complete only | Follow-up / gap in acceptance criteria |
| #76 | CAPE data source bug | Implementation found FRED doesn't host CAPE | Bug discovered during Phase 6 |
| #39 | Hardcoded dark-theme CSS | Code review after merge | Post-merge quality issue |
| #60 | Add backend issue numbers to backlog docs | AI housekeeping after [5c] | Documentation maintenance |

**Observations:**
- #76 (CAPE bug) is the most significant additional issue — it revealed a data source assumption in the spec that was never validated
- #73 (ONS follow-up) reveals a gap in acceptance criteria — "fetches from ONS" should have explicitly required a real HTTP call, not a stub
- #39 (CSS) reveals a gap in the AI coding standards for CSS

---

## Issues Raised by Developer Outside the Process

| # | Issue | Type | Notes |
|---|---|---|---|
| #2 | `[1a]` Secrets setup | Operational | Never closed |
| #26 | Post-MVP UI enhancements | Backlog | Ideas parking — correct use of issues |
| #80 | Main vs dev drift | Process failure | Caused by direct-to-main PRs in Phase 6 |
| #81 | Add deploy pipeline | Deployment | Not resolved at POC close |
| #85 | Missing nginx.conf | Deployment bug | Discovered post-deploy |
| #87 | Deployment issue | Deployment | OCIR auth format — open |
| #88 | POC retrospective | Meta / process | This document |

**Observations:**
- Issues #80, #81, #85, #87 are all deployment-related — this is a clear signal that deployment is under-specified in the SDD workflow
- Issue #26 (post-MVP UI) is a healthy use of GitHub issues as a product backlog
- Issue #80 (drift) could have been prevented with branch rules enforcing dev-first

---

## Follow-Up Question Analysis

### Types of questions asked by the AI across all issues/PRs

| Question type | Example | Frequency | Was it necessary? |
|---|---|---|---|
| Dependency check | "All [4] issues are still open — I will not proceed" | High (3 per issue #8) | Once per issue is fine; 3x is noise |
| EF Core/Postgres | "Should this include EF Core entities?" | Very high (asked in #8 at least 3 times) | No — should have been in spec questionnaire |
| Data source validation | "Which API provider for CAPE?" (#76) | Once | Yes — but should have been validated during [5a] spec |
| Branch hygiene | N/A | 0 | Was never asked — a gap |
| API key format | "FRED key — env var or appsettings?" | Once | Reasonable — resolved as ADR |
| yfinance approach | "Sidecar vs .NET lib?" | Once | Reasonable — domain-specific decision |
| MSW vs Faker | "What approach for fake data?" | Once | Reasonable — process choice |
| Merge method | "Ready to merge?" | High (multiple issues) | Reasonable — AI correctly defers to human on merge |

**Key finding:** The EF Core question is the single most repeated unnecessary question. It should be captured in the `[1c]` spec questionnaire with an explicit "Does this project require a database?" field that the AI reads before generating `[5a]` acceptance criteria.

---

## Assignee Pattern Analysis

Assigning the developer to issues and PRs is essential for GitHub notifications to fire. The POC showed a clear degradation in assignee coverage as the project progressed:

| Phase | Issues created | Assigned | Not assigned |
|---|---|---|---|
| Phase 1–2 (orchestrator, design) | #2–#24 | ~90% | ~10% |
| Phase 3–4 (frontend) | #32–#40 | ~80% | ~20% |
| Phase 5 (backend design) | #8–#10 | ~100% | 0% |
| Phase 6 (backend impl, factory-created) | #43–#59, #73 | ~30% | ~70% |
| PRs (Phase 6) | #41, #42, #61–#79 | ~35% | ~65% |

**Conclusion:** Issue and PR factory scripts (`[5c]`, and later the individual story PRs) omitted the `--assignee` flag. This is a one-line fix with high impact: the developer cannot reliably govern the process if they are not notified of AI activity.

---

## Issue Template Quality Assessment

| Template | Used for | What was missing |
|---|---|---|
| `[1c]` Spec questionnaire | Project vision | "Is a database required?" (explicitly, as a checkbox), "What external data sources are used and who hosts them?" |
| `[2a]` Design issue generator | UI/UX design | Clear instruction that design issues should focus on UX questions, not technical decisions |
| `[3a]` User story + tech decisions | Frontend spec | API skeleton section, TDD section, BDD definition |
| `[5a]` Backend design | Backend spec | Remove EF Core from AC when DB not required; add data source validation step |
| `[6]` Backend implementation | Per-story implementation | "Real HTTP call required (not stub)" should be explicit in fetcher story ACs |
| All issue factories (`[3b]`, `[5c]`) | Issue creation | `--assignee` flag missing — developer not notified of created issues |
| All PR creation | PR raising | `--assignee` flag inconsistently applied — developer not notified of raised PRs |
