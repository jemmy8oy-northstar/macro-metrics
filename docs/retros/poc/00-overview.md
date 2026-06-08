# MacroMetrics POC — Retrospective Overview

**Date:** 2026-06-07
**Scope:** Full POC from repository creation to deployment (issues #1–#88)
**Process:** Spec Driven Development (SDD) with AI pair-programming via Claude Code

---

## What Was Built

MacroMetrics is a financial dashboard that displays macroeconomic ratios (e.g. Gold/Wages, S&P 500/US CPI) alongside standalone indicator charts. The POC delivered:

- A fully navigable React + Vite frontend with Recharts visualisations
- A stateless .NET 8 Minimal API proxy that aggregates data from ONS (UK), FRED (US), Robert Shiller's dataset, and a Python yfinance sidecar
- In-memory caching with `IMemoryCache` and `Cache-Control` headers
- A Helm chart deployed to OCI Kubernetes
- A full CI/CD pipeline (GitHub Actions → OCI Container Registry → Kubernetes)
- 200+ tests (unit + integration)

---

## POC Timeline (Phases Completed)

| Phase | GitHub Phase | Status | Key Output |
|---|---|---|---|
| Vision & Planning | Phase 1 | ✅ Complete | Project spec, epics, features |
| UI/UX Design | Phase 2 | ✅ Complete | ASCII mockups, Mermaid diagrams |
| Frontend User Stories | Phase 3 | ✅ Complete | BDD user stories, tech decisions |
| Frontend Implementation | Phase 4 | ✅ Complete | React app with Faker data |
| Backend Design | Phase 5 | ✅ Complete | API contracts, service layer, ADRs |
| Backend Implementation | Phase 6 | ✅ Mostly complete | Real fetchers, normalisation, ratio engine |
| Deployment | Informal | ✅ Complete | Helm + OCI K8s deployment |
| Retro | Phase 7 (new) | ✅ In progress | This document |

### Remaining open items at POC close
- #57 / PR #79 — `Cache-Control` headers PR raised but not merged (ready)
- #58 — Unknown metric ID returns 404 (not implemented)
- #59 — Ratio endpoint rejects invalid/missing inputs (not implemented)
- #39 — Hardcoded dark-theme CSS colours (cosmetic)
- #2 — Pipeline secrets setup (ongoing/operational)
- #11 — Postgres DB hook-up (explicitly deferred, post-MVP)
- #80, #81, #87 — Branch discipline, deploy pipeline, deployment issues

---

## Overall Verdict

The SDD process **worked well as a forcing function for upfront thinking**. The AI successfully produced spec documents, ASCII mockups, BDD user stories, backend design, and implementation — all with minimal human code-writing. The main friction points were around:

1. **Template gaps** — questions the AI kept re-asking because they weren't captured in the spec questionnaire
2. **Branch discipline** — direct-to-main PRs caused a significant `main`/`dev` drift incident
3. **Assumption surfacing** — the AI made assumptions that were sometimes wrong (e.g. FRED hosting CAPE), leading to bug issues
4. **Timeout/chunking** — one issue (#57) timed out because the task was too complex for a single AI run; there was no visibility that the limit was hit
5. **Dependency checking noise** — the AI re-checked dependency conditions multiple times per issue, adding friction
6. **Assignee consistency** — ~65% of Phase 6 issues and PRs were created without assigning the developer, breaking GitHub notifications
7. **action-ready relabelling** — developer had to manually re-apply the label after every AI pass, including timeouts
8. **Multi-pass context** — the AI re-asked questions already answered in previous comments because it used the issue body as its prompt rather than the latest unanswered comment

See the sibling retro documents for detail on each area.

### Follow-on actions raised during retro review

As a result of the post-retro discussion two follow-on issues were raised on the underlying fork:

| Issue | What it fixes |
|---|---|
| [claude-code-telegram-k8s #33](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/33) | per-label `max_turns` config; use latest unanswered comment as task prompt (fixes re-asking pattern) |
| [claude-code-telegram-k8s #34](https://github.com/jemmy8oy/claude-code-telegram-k8s/issues/34) | Post ⚠️ GitHub comment + Telegram alert when iteration limit is hit; raise default `claudeMaxTurns` to 100 |

These complement the template-level fixes in this retro. The ~80% of improvements that only require template / CLAUDE.md changes can be applied independently; the fork changes address the remaining ~20% that require infrastructure changes.

---

## Developer Autonomy Observations

During the retro, the developer raised several questions about increasing AI autonomy. These are captured here with analysis:

### "I often have to keep relabelling issues with action-ready"
**Current friction:** Every AI trigger removes `action-ready`. Multi-pass issues (e.g. #8 required 3 passes, #57 timed out) require repeated manual re-labelling.
**Proposed fix:** AI self-relabels `action-ready` after partial passes. See Recommendation 12.

### "Maybe I need to improve the iteration length — it's 20 rounds currently"
**Analysis:** 20 turns is low for complex orchestrator issues. A `[5a]` backend design spec that reads spec documents, writes ADRs, and raises a PR realistically needs 30–50 turns. Implementation issues `[6]` need 20–40. Recommendation: set `max_turns` per issue type (see Recommendation 12).

### "Wondering whether I can make the bot remember sessions per issue"
**Analysis:** The AI is stateless between triggers. Every pass re-reads the same files and re-checks the same dependencies. For late-phase issues (Phase 6), this added substantial overhead.
**Proposed fix:** Structured pass summary comments — the AI writes a compact summary at the end of each pass that the next pass reads instead of re-discovering everything from scratch. See Recommendation 13.

### "Whether we can have a process where the AI can make sensible suggestions and in the PR the AI can outline all assumptions"
**Analysis:** This is the "Assumptions & Decisions" PR section (Recommendation 2). It lets the developer scan a table of AI decisions in the PR rather than reviewing every line of code. This directly addresses the silent assumption problem (CAPE/FRED, branch targeting, CSS approach). Recommend implementing this as a mandatory PR template section.

---

## Retrospective Document Index

| Document | Focus |
|---|---|
| [00-overview.md](./00-overview.md) | This document — high-level summary |
| [01-sdd-process.md](./01-sdd-process.md) | Phase-by-phase SDD process analysis |
| [02-issue-analysis.md](./02-issue-analysis.md) | Every issue categorised and assessed |
| [03-template-gaps.md](./03-template-gaps.md) | Template gaps and specific improvement proposals |
| [04-technical.md](./04-technical.md) | Technical decisions, bugs, and implementation quality |
| [05-recommendations.md](./05-recommendations.md) | Concrete recommendations for the next project |
| [06-template-audit.md](./06-template-audit.md) | Specific file-by-file changes needed in `web-template` |
