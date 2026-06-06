# phase5-data-normalisation — Phase 5 Backlog

> **Status: Draft — to be detailed in GitHub Phase 5 (`[5a]`)**

> **Draft** — implementation details to be designed in Phase 6. This file captures the high-level capability requirement only.

## Capability

Raw data arrives at different frequencies (daily, monthly, quarterly) and in different units. The backend must normalise all series to a common cadence and align date ranges so any two metrics can be divided point-by-point to produce a ratio.

## Open questions for Phase 6
- Target frequency: monthly end-of-month?
- Gap-filling strategy for missing data points?
- How far back does each source reliably go — what's the minimum shared history?

## Phase 2 design issue
_None — backend features are not designed in Phase 2. Design is deferred to Phase 5 (`[5a]`)._

## Stories

- [#51 — US-B10: Monthly end-of-month alignment for all source cadences](https://github.com/jemmy8oy/macro-metrics/issues/51)
- [#52 — US-B11: Missing months are forward-filled](https://github.com/jemmy8oy/macro-metrics/issues/52)
