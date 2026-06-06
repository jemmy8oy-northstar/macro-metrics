# phase5-data-fetchers — Phase 5 Backlog

> **Status: Draft — to be detailed in GitHub Phase 5 (`[5a]`)**

> **Draft** — implementation details to be designed in Phase 6 (Backend Architecture). This file captures the high-level capability requirement only.

## Capability

The backend must be able to fetch time-series data from three external providers:

- **yfinance** — equities, commodities, crypto
- **FRED API** — US macroeconomic series (free API key required)
- **ONS API** — UK macroeconomic series (no key required)

Each fetcher returns data in a common internal format for downstream normalisation.

## Open questions for Phase 6
- Use a .NET yfinance wrapper, Python sidecar, or third-party finance library?
- FRED key storage — appsettings or environment variable?
- Cache strategy — in-memory, file, or fetch-on-demand for MVP?

## Phase 2 design issue
_None — backend features are not designed in Phase 2. Design is deferred to Phase 5 (`[5a]`)._

## Stories

- [#53 — US-B12: UK macroeconomic metrics fetched from ONS](https://github.com/jemmy8oy/macro-metrics/issues/53)
- [#54 — US-B13: US macroeconomic metrics fetched from FRED](https://github.com/jemmy8oy/macro-metrics/issues/54)
- [#55 — US-B14: Market and equity metrics fetched via yfinance Python sidecar](https://github.com/jemmy8oy/macro-metrics/issues/55)
