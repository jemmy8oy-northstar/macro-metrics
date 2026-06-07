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
4. **Timeout/chunking** — one issue (#57) timed out because the task was too complex for a single AI run
5. **Dependency checking noise** — the AI re-checked dependency conditions multiple times per issue, adding friction

See the sibling retro documents for detail on each area.

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
