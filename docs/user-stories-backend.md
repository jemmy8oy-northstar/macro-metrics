# Backend User Stories — MacroMetrics Phase 5

> **Status:** Proposed — awaiting developer review.
> **Derived from:** `docs/specs/backend-design.md` (API contracts, ADRs, integration test scenarios).
> **Format:** BDD (Behaviour-Driven Development) — Given / When / Then.

---

## Contents

1. [Metric Catalogue](#1-metric-catalogue)
2. [Single Metric Series](#2-single-metric-series)
3. [Ratio Series](#3-ratio-series)
4. [Data Normalisation](#4-data-normalisation)
5. [Data Sourcing](#5-data-sourcing)
6. [Caching](#6-caching)
7. [Error Handling](#7-error-handling)

---

## 1. Metric Catalogue

### US-B1 — Retrieve the full metric catalogue

**As a** frontend consumer,
**I want to** call `GET /api/metrics`,
**So that** I can display all available metrics in the UI and know which ones are valid inputs for ratio comparisons.

```gherkin
Scenario: Catalogue returns all 14 metrics
  Given the application has started
  When GET /api/metrics is called
  Then the response status is 200
  And the response body contains exactly 14 metric entries
  And each entry contains: id, label, unit, source, isIndicatorOnly, earliestDate
  And all id values are non-empty kebab-case strings
  And all label, unit, and source values are non-empty strings
```

---

### US-B2 — Indicator-only metrics are flagged in the catalogue

**As a** frontend consumer,
**I want** CAPE, UK 10-year gilt yield, and US 10-year treasury yield to be flagged `isIndicatorOnly: true`,
**So that** the UI can exclude them from the ratio metric picker while still displaying them as standalone indicator cards.

```gherkin
Scenario: Indicator-only metrics are correctly flagged
  Given the application has started
  When GET /api/metrics is called
  Then the entries with id "cape", "uk-10yr-gilt", and "us-10yr-treasury" have isIndicatorOnly = true
  And all other 11 entries have isIndicatorOnly = false
```

---

### US-B3 — Each metric exposes its earliest reliable date

**As a** frontend consumer,
**I want** each metric catalogue entry to include an `earliestDate` field,
**So that** the UI can inform users of the available date range before they request a time series.

```gherkin
Scenario: Catalogue entries include earliestDate
  Given the application has started
  When GET /api/metrics is called
  Then each entry includes an earliestDate value in ISO date format (YYYY-MM-DD)
  And the earliestDate for "bitcoin" is no earlier than 2014-09-30
  And the earliestDate for "cape" is no later than 1881-01-31
```

---

## 2. Single Metric Series

### US-B4 — Retrieve a full time series for a single metric

**As a** frontend consumer,
**I want to** call `GET /api/metrics/{id}`,
**So that** I can render a chart of a metric's historical values.

```gherkin
Scenario: Full series returned for a valid metric
  Given a valid metric id "gold" exists in the catalogue
  When GET /api/metrics/gold is called
  Then the response status is 200
  And the response body contains: id, label, unit, and a points array
  And all points have a date in ISO format (YYYY-MM-DD) and a numeric value
  And dates are in chronological ascending order
```

---

### US-B5 — Series data is normalised to monthly end-of-month cadence

**As a** frontend consumer,
**I want** all series responses to use monthly end-of-month data points,
**So that** charts have a consistent time axis regardless of the underlying source cadence.

```gherkin
Scenario: Daily source data is normalised to monthly cadence
  Given the yfinance sidecar returns raw daily closing data for "gold"
  When GET /api/metrics/gold is called
  Then the response DataPoints are at monthly cadence
  And no two DataPoints share the same calendar month
  And each DataPoint date is the last day of its calendar month (e.g. 2024-01-31, 2024-02-29)
```

---

### US-B6 — Date range filter on a single metric series

**As a** frontend consumer,
**I want to** pass optional `from` and `to` query parameters to `GET /api/metrics/{id}`,
**So that** I can request only the data range I need and keep response payloads small.

```gherkin
Scenario: Series is filtered to the requested date range
  Given a valid metric id "uk-house-prices" exists with data from 1968
  When GET /api/metrics/uk-house-prices?from=2000-01-01&to=2010-12-31 is called
  Then the response status is 200
  And all DataPoints have dates between 2000-01-31 and 2010-12-31 (inclusive, end-of-month aligned)
  And no DataPoints outside this range are returned

Scenario: Series without date filter returns the full available history
  Given a valid metric id "sp500" exists
  When GET /api/metrics/sp500 is called with no from or to parameters
  Then the response status is 200
  And DataPoints start from the metric's earliestDate
```

---

## 3. Ratio Series

### US-B7 — Compute a ratio series for two metrics

**As a** frontend consumer,
**I want to** call `GET /api/metrics/ratio?numerator={id}&denominator={id}`,
**So that** I can display a ratio chart comparing two macroeconomic series.

```gherkin
Scenario: Ratio series computed correctly for two valid metrics
  Given aligned monthly series exist for "us-house-prices" and "us-wages"
  When GET /api/metrics/ratio?numerator=us-house-prices&denominator=us-wages is called
  Then the response status is 200
  And the response body contains: numeratorId, denominatorId, points, and longRunAverage
  And each DataPoint.value equals the numerator value divided by the denominator value for that date
  And DataPoints only cover dates present in both series (the intersection of their date ranges)
  And dates are in chronological ascending order
```

---

### US-B8 / US-B9 — Date range filter on a ratio series, with long-run average stability

**As a** frontend consumer,
**I want to** pass optional `from` and `to` parameters to `GET /api/metrics/ratio`,
**And I want** the `longRunAverage` to always reflect the full historical record,
**So that** the ratio chart can be zoomed to a specific date range while the reference line remains stable regardless of the viewing window.

```gherkin
Scenario: Ratio series is filtered to the requested date range
  Given a ratio series for "ftse100" and "uk-wages" has history from 1984
  When GET /api/metrics/ratio?numerator=ftse100&denominator=uk-wages&from=2000-01-01&to=2020-12-31 is called
  Then the response status is 200
  And all DataPoints have dates between 2000-01-31 and 2020-12-31 (end-of-month aligned)
  And longRunAverage is still computed from the full history since 1984

Scenario: Date filter does not affect longRunAverage
  Given a ratio series with 20+ years of history exists for "gold" and "us-wages"
  When GET /api/metrics/ratio?numerator=gold&denominator=us-wages&from=2020-01-01 is called
  Then the response DataPoints only cover 2020-01-01 onwards
  And longRunAverage equals the arithmetic mean of ALL ratio points across the full historical intersection
  And longRunAverage is not recalculated from only the filtered DataPoints
```

---

## 4. Data Normalisation

### US-B10 — Monthly end-of-month alignment for all source cadences

**As a** backend service,
**I want** `DataNormalisationService` to map any input cadence to monthly end-of-month dates,
**So that** ratio computation can always align two series point-by-point.

```gherkin
Scenario: Daily series is downsampled to monthly end-of-month
  Given a raw daily series with multiple data points per calendar month
  When DataNormalisationService normalises the series
  Then the output contains exactly one DataPoint per calendar month
  And each date is the last calendar day of that month
  And the retained value is the last available value within the month (closing price)

Scenario: Monthly series is aligned to end-of-month dates
  Given a raw monthly series with mid-month dates (e.g. 2024-01-15)
  When DataNormalisationService normalises the series
  Then each date is shifted to the end of its calendar month (e.g. 2024-01-31)
  And values are unchanged
```

---

### US-B11 — Missing months are forward-filled

**As a** backend service,
**I want** `DataNormalisationService` to forward-fill any calendar month with no source data,
**So that** all normalised series have a continuous monthly sequence without gaps.

```gherkin
Scenario: A gap month is filled with the preceding value
  Given a raw series that has data for 2024-01 and 2024-03 but not 2024-02
  When DataNormalisationService normalises the series
  Then the output contains a DataPoint for 2024-02-29
  And that DataPoint's value equals the 2024-01-31 value (carry-forward)

Scenario: A leading gap at the start of the series is dropped, not filled
  Given a raw series where the first available data point is 2024-03-31
  When DataNormalisationService normalises the series
  Then no DataPoints exist before 2024-03-31
  And no zero-value placeholder DataPoints are inserted
```

---

## 5. Data Sourcing

### US-B12 — UK macroeconomic metrics fetched from ONS

**As a** backend service,
**I want** `OnsFetcherService` to be used exclusively for `uk-house-prices`, `uk-wages`, and `uk-cpi`,
**So that** the correct source API is always called for UK data.

```gherkin
Scenario: UK metric routes to ONS fetcher
  Given real fetcher implementations are wired via DI
  When MetricSeriesOrchestrator.GetSeriesAsync("uk-house-prices") is called
  Then IOnsFetcherService.FetchRawAsync is invoked
  And IFredFetcherService.FetchRawAsync is not invoked
  And IYFinanceFetcherService.FetchRawAsync is not invoked
```

---

### US-B12b — OnsFetcherService makes real HTTP calls to the ONS API

**As a** backend service,
**I want** `OnsFetcherService` to call the ONS REST API with the correct dataset and series IDs for each UK metric,
**So that** real historical data is returned rather than a stub response.

```gherkin
Scenario: OnsFetcherService maps metric IDs to correct ONS series identifiers
  Given the ONS API is reachable at https://api.ons.gov.uk
  When OnsFetcherService.FetchRawAsync("uk-house-prices") is called
  Then a GET request is made to /v1/datasets/housepriceindex/timeseries/AVHP/data
  And when called with "uk-wages", the request targets /v1/datasets/averageweeklyearnings/timeseries/KAC3/data
  And when called with "uk-cpi", the request targets /v1/datasets/cpih01/timeseries/L55O/data

Scenario: ONS response is correctly parsed into a raw data series
  Given the ONS API returns a JSON body with a "months" array of {date, value} objects
  When OnsFetcherService processes the response
  Then each value string is parsed to a decimal
  And each date string (e.g. "2024 JAN") is parsed to a DateOnly representing the first day of that month
  And the resulting raw series is returned as an IEnumerable<RawDataPoint>

Scenario: ONS API unavailability surfaces as a typed exception
  Given the ONS API returns a non-200 status code (e.g. 503)
  When OnsFetcherService.FetchRawAsync is called
  Then a FetcherException is thrown with a message indicating the ONS source and the HTTP status code
  And the exception is not swallowed silently

Scenario: In-process integration — DI-wired OnsFetcherService parses a stubbed ONS response correctly
  Given OnsFetcherService is registered via AddHttpClient<IOnsFetcherService, OnsFetcherService> in a ServiceCollection
  And the HttpClient is configured with a FakeHttpMessageHandler returning a valid ONS months payload
  When IOnsFetcherService.FetchRawAsync("uk-cpi") is resolved from the container and called
  Then the returned list contains one IMetricPoint per month in the stub payload
  And each point's Date is in "yyyy-MM-dd" format representing the first day of the month
  And each point's Value matches the decimal in the stub payload
  And no FetcherException is thrown

Scenario: In-process integration — DI-wired OnsFetcherService propagates FetcherException on non-200 stub
  Given OnsFetcherService is registered via AddHttpClient<IOnsFetcherService, OnsFetcherService> in a ServiceCollection
  And the HttpClient is configured with a FakeHttpMessageHandler returning HTTP 503
  When IOnsFetcherService.FetchRawAsync("uk-wages") is resolved from the container and called
  Then a FetcherException is thrown containing the HTTP status code
```

---

### US-B13 — US macroeconomic metrics fetched from FRED

**As a** backend service,
**I want** `FredFetcherService` to be used for `us-house-prices`, `us-wages`, `us-cpi`, `cape`, and `us-10yr-treasury`,
**So that** the FRED REST API is called for all US macroeconomic data.

```gherkin
Scenario: US macroeconomic metric routes to FRED fetcher
  Given real fetcher implementations are wired via DI
  When MetricSeriesOrchestrator.GetSeriesAsync("us-wages") is called
  Then IFredFetcherService.FetchRawAsync is invoked
  And IOnsFetcherService.FetchRawAsync is not invoked
  And IYFinanceFetcherService.FetchRawAsync is not invoked

Scenario: FRED API key is read from environment, not appsettings
  Given the FRED API key is absent from appsettings.json
  And the environment variable FRED__ApiKey is set to a valid key
  When the application starts
  Then FredFetcherService reads the key via IConfiguration["Fred:ApiKey"]
  And no startup exception is thrown

Scenario: Missing FRED API key causes fail-fast on startup
  Given neither appsettings.json nor the environment supplies a FRED API key
  When the application starts
  Then a startup validation exception is thrown
  And the application does not serve requests
```

---

### US-B14 — Market and equity metrics fetched via yfinance Python sidecar

**As a** backend service,
**I want** `YFinanceFetcherService` to call the Python sidecar HTTP endpoint for `gold`, `oil`, `ftse100`, `sp500`, `bitcoin`, and `uk-10yr-gilt`,
**So that** yfinance data is retrieved without subprocess calls or unofficial wrappers.

```gherkin
Scenario: Market metric routes to yfinance sidecar via HTTP
  Given the yfinance sidecar is available at the configured base URL
  When MetricSeriesOrchestrator.GetSeriesAsync("gold") is called
  Then YFinanceFetcherService calls GET /series/GC=F on the sidecar
  And IOnsFetcherService.FetchRawAsync is not invoked
  And IFredFetcherService.FetchRawAsync is not invoked

Scenario: YFinanceFetcherService maps metric IDs to correct tickers
  Given the yfinance sidecar is available
  When YFinanceFetcherService.FetchRawAsync("ftse100") is called
  Then the sidecar is called with ticker "^FTSE"
  And when called with "bitcoin", the sidecar ticker is "BTC-USD"
  And when called with "oil", the sidecar ticker is "CL=F"

Scenario: Sidecar response shape is correctly consumed
  Given the sidecar returns a JSON body with "ticker" and a "points" array of {date, close} objects
  When YFinanceFetcherService processes the response
  Then each close value is mapped to a DataPoint value
  And each date string is parsed to a DateOnly
```

---

## 6. Caching

### US-B15 — Metric series are cached in memory for one hour

**As a** backend service,
**I want** `MetricSeriesOrchestrator` to cache each normalised series in `IMemoryCache` with a 1-hour TTL,
**So that** repeated API calls within an hour do not trigger redundant fetches to external sources.

```gherkin
Scenario: Cache hit on second request
  Given GET /api/metrics/gold has already been called once and the result is cached
  When GET /api/metrics/gold is called a second time within the same hour
  Then YFinanceFetcherService.FetchRawAsync is not called again
  And the response is served from the in-memory cache
  And the response status is 200

Scenario: Cache miss triggers a fresh fetch
  Given no cached entry exists for metric "oil"
  When GET /api/metrics/oil is called
  Then YFinanceFetcherService.FetchRawAsync is called exactly once
  And the result is stored in the cache with a 1-hour TTL
```

---

### US-B16 — HTTP responses include Cache-Control headers

**As a** frontend consumer and CDN operator,
**I want** all `/api/metrics` responses to include `Cache-Control: public, max-age=3600`,
**So that** CDNs and browsers can cache responses for up to one hour without serving data staler than the backend would.

```gherkin
Scenario: Metric catalogue response includes correct Cache-Control header
  When GET /api/metrics is called
  Then the response includes the header: Cache-Control: public, max-age=3600

Scenario: Single metric series response includes correct Cache-Control header
  When GET /api/metrics/gold is called
  Then the response includes the header: Cache-Control: public, max-age=3600

Scenario: Ratio series response includes correct Cache-Control header
  When GET /api/metrics/ratio?numerator=gold&denominator=us-wages is called
  Then the response includes the header: Cache-Control: public, max-age=3600
```

---

## 7. Error Handling

### US-B17 — Unknown metric ID returns 404

**As a** frontend consumer,
**I want** `GET /api/metrics/{id}` to return `404 Not Found` when the metric ID does not exist,
**So that** the UI can display a meaningful error instead of showing blank or broken data.

```gherkin
Scenario: Unknown metric ID returns 404
  Given no metric with id "not-a-metric" exists in the catalogue
  When GET /api/metrics/not-a-metric is called
  Then the response status is 404
  And the response body contains an error message indicating the metric was not found

Scenario: Valid metric ID returns 200
  Given metric "sp500" exists in the catalogue
  When GET /api/metrics/sp500 is called
  Then the response status is 200
```

---

### US-B18 / US-B19 — Ratio endpoint rejects invalid or missing inputs

**As a** frontend consumer,
**I want** `GET /api/metrics/ratio` to return `400 Bad Request` when required parameters are missing or an `isIndicatorOnly` metric is supplied,
**So that** the API enforces its business rules up-front and fails fast with a clear error message rather than computing a meaningless or forbidden result.

```gherkin
Scenario: Missing numerator parameter returns 400
  When GET /api/metrics/ratio?denominator=us-wages is called (no numerator)
  Then the response status is 400
  And the error message indicates the "numerator" parameter is required

Scenario: Missing denominator parameter returns 400
  When GET /api/metrics/ratio?numerator=gold is called (no denominator)
  Then the response status is 400
  And the error message indicates the "denominator" parameter is required

Scenario: Indicator-only metric as ratio numerator returns 400
  Given "cape" is flagged isIndicatorOnly = true in the catalogue
  When GET /api/metrics/ratio?numerator=cape&denominator=us-wages is called
  Then the response status is 400
  And the error message indicates that "cape" is an indicator-only metric and cannot be used as a ratio input

Scenario: Indicator-only metric as ratio denominator returns 400
  Given "uk-10yr-gilt" is flagged isIndicatorOnly = true in the catalogue
  When GET /api/metrics/ratio?numerator=gold&denominator=uk-10yr-gilt is called
  Then the response status is 400
  And the error message indicates that "uk-10yr-gilt" is an indicator-only metric and cannot be used as a ratio input

Scenario: Both parameters supplied with valid, non-indicator metrics returns 200
  Given "gold" and "us-wages" are both isIndicatorOnly = false
  When GET /api/metrics/ratio?numerator=gold&denominator=us-wages is called
  Then the response status is 200
```

---

*Generated from `docs/specs/backend-design.md` — API contracts (§2), service layer design (§5), ADR decisions (§6, §9), and integration test scenarios (§7).*
