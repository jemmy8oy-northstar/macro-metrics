# phase5-metrics-api — Phase 5 Backlog

> **Status: Draft — to be detailed in GitHub Phase 5 (`[5a]`)**

> **Draft** — API contract to be finalised in Phase 5 (Skeleton Backend) once the UI/UX design is signed off. Endpoint shapes here are indicative only.

## Capability

Three endpoints the frontend requires:

- **Metric catalogue** — list all available metrics and their metadata
- **Single metric series** — return a time series for a given metric ID
- **Ratio series** — compute and return numerator ÷ denominator as a time series, including long-run average

## Open questions for Phase 5
- Pagination or full-series responses?
- Date range filtering on the API or handled client-side?
- Response caching headers?

## Phase 2 design issue
_None — backend features are not designed in Phase 2. Design is deferred to Phase 5 (`[5a]`)._

## Stories

### Metric Catalogue
- [#43 — US-B1: Retrieve the full metric catalogue](https://github.com/jemmy8oy/macro-metrics/issues/43)
- [#44 — US-B2: Indicator-only metrics are flagged in the catalogue](https://github.com/jemmy8oy/macro-metrics/issues/44)
- [#45 — US-B3: Each metric exposes its earliest reliable date](https://github.com/jemmy8oy/macro-metrics/issues/45)

### Single Metric Series
- [#46 — US-B4: Retrieve a full time series for a single metric](https://github.com/jemmy8oy/macro-metrics/issues/46)
- [#47 — US-B5: Series data is normalised to monthly end-of-month cadence](https://github.com/jemmy8oy/macro-metrics/issues/47)
- [#48 — US-B6: Date range filter on a single metric series](https://github.com/jemmy8oy/macro-metrics/issues/48)

### Ratio Series
- [#49 — US-B7: Compute a ratio series for two metrics](https://github.com/jemmy8oy/macro-metrics/issues/49)
- [#50 — US-B8/B9: Date range filter on ratio series with stable long-run average](https://github.com/jemmy8oy/macro-metrics/issues/50)

### Caching
- [#56 — US-B15: Metric series are cached in memory for one hour](https://github.com/jemmy8oy/macro-metrics/issues/56)
- [#57 — US-B16: HTTP responses include Cache-Control headers](https://github.com/jemmy8oy/macro-metrics/issues/57)

### Error Handling
- [#58 — US-B17: Unknown metric ID returns 404](https://github.com/jemmy8oy/macro-metrics/issues/58)
- [#59 — US-B18/B19: Ratio endpoint rejects invalid or missing inputs](https://github.com/jemmy8oy/macro-metrics/issues/59)
