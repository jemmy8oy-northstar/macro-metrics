# Technical Retrospective — MacroMetrics POC

**Purpose:** Assess the quality of technical decisions made during the POC, bugs encountered, and areas of implementation quality to carry forward or improve.

---

## Architecture Assessment

### What was decided (Phase 5 ADRs)

| ADR | Decision | Verdict |
|---|---|---|
| ADR-001 | Stateless proxy — no database for MVP | ✅ Correct. Kept complexity low without meaningful loss. |
| ADR-002 | Python yfinance sidecar for market data | ✅ Correct. No production-quality .NET yfinance equivalent exists. The sidecar was well-isolated. |
| ADR-003 | In-memory caching via `IMemoryCache` (1-hour TTL) | ✅ Correct for MVP. Avoids hammering external APIs. |
| ADR-004 | Monthly end-of-month normalisation cadence | ✅ Correct. Macro data is inherently low-frequency; monthly is the right granularity. |
| ADR-005 | Forward-fill for missing months | ✅ Correct. Appropriate for economic data where gaps indicate "no change". |
| ADR-006 | yfinance sidecar API contract (`GET /series/{ticker}`) | ✅ Clean REST contract, easily extensible. |
| ADR-007 | FRED API key as env var / K8s secret | ✅ Correct — secrets should never be in appsettings.json. |
| ADR-008 | Gap-fill strategy: forward-fill, drop leading NaN | ✅ Correct. No interpolation needed for monthly economic data. |
| ADR-009 | Per-metric `EarliestDate` for ratio intersection | ✅ Correct. Required for Bitcoin-era metrics vs century-old FRED series. |
| ADR-010 | Cache invalidation: TTL-only, no manual flush | ✅ Correct for MVP. |
| ADR-011 | `Cache-Control: public, max-age=3600` on all endpoints | ✅ Correct — aligns CDN/browser cache with in-memory TTL. |

**Overall:** The Phase 5 design was accurate and the implementation followed it closely. The ADR format was a success — it made the "why not" visible alongside the "what".

---

## Technical Decisions That Worked Well

### 1. OpenAPI → RTK Query codegen pipeline
The `GET /api/metrics` and `GET /api/metrics/{id}` routes generated a clean OpenAPI spec that drove RTK Query hook generation. The frontend could use type-safe hooks from day one. This is a strong pattern to carry forward.

### 2. `FetcherException` as a typed boundary
Creating a `FetcherException` in `MacroMetrics.Abstractions.Exceptions` gave a clean error boundary between the fetcher layer and the orchestrator. The orchestrator could catch `FetcherException` without needing to know which external API failed.

### 3. `IEndpointFilter` for cross-cutting concerns
Using `IEndpointFilter` for the `Cache-Control` header (PR #79) was a clean approach — it kept the header logic out of individual route handlers and applied it at the route group level. This is the correct .NET Minimal API pattern.

### 4. Env-gated live tests
The pattern of skipping live API tests when API key environment variables are absent (`Skip = condition`) was established early and used consistently across ONS, FRED, Shiller, and yfinance. This prevents CI from failing when secrets aren't present.

### 5. Shared test fixtures (`IClassFixture<HttpClientFixture>`)
Sharing a single `HttpClient` fixture across FRED live tests (to avoid 429 rate-limit errors) was a good pragmatic solution. Established in PR #74 and used consistently after.

### 6. 7-project solution structure
The separation of `Abstractions`, `DomainModels`, `DataModels`, `Services`, `Services.Tests`, `WebApi`, `WebApi.Tests` followed the template's SRP architecture. The dependency direction enforcement (no cross-layer violations) was consistently maintained.

---

## Technical Decisions That Created Problems

### 1. CAPE data source assumption (Issue #76)
**What happened:** The AI spec assumed FRED hosted the Shiller CAPE ratio. FRED's `SHILLER_PE_RATIO_MONTH` series was deprecated/removed. The implementation used `MULTPL/SHILLER_PE_RATIO_MONTH` on Nasdaq Data Link, then switched to `multpl.com` HTML scraping.

**Root cause:** The data source was not validated during Phase 5. The spec was written with the assumption unverified.

**Lesson:** Data sources for financial data should be explicitly validated (test a real API call) during Phase 5, not assumed during Phase 6 implementation.

### 2. ONS stub implementation (#53 → #73)
**What happened:** The `OnsFetcherService` in PR #71 only did routing — the actual HTTP calls to the ONS API were a stub. The issue was closed. A follow-up issue #73 was needed for the real implementation.

**Root cause:** The acceptance criteria didn't distinguish between "routes UK metrics to the ONS fetcher" (routing only) and "fetches data from the ONS API via HTTP" (real implementation).

**Lesson:** Fetcher implementation stories must explicitly state "real HTTP call required". Two separate stories for routing and implementation may be cleaner.

### 3. Main/dev branch drift (Issue #80)
**What happened:** Phase 6 implementation PRs (PRs #41–#78) all targeted `main` directly instead of `dev`. By the time the drift was noticed, `main` was 46 commits ahead of `dev` with all the real backend code.

**Root cause:** No branch protection rule or workflow enforced dev-first. The AI's default was to target `main` (possibly inherited from the template).

**Lesson:** Branch protection must be enforced at the repo level, not assumed from AI guidance. The `require-dev-source` GitHub Actions workflow (proposed in issue #80 analysis) would have caught this from PR #41 onward.

### 4. Timeout on Issue #57
**What happened:** The Cache-Control implementation (issue #57) caused a 600-second agent timeout on first trigger. The PR was eventually raised correctly on the second trigger.

**Root cause:** Likely the AI was doing redundant exploration (re-reading files it had already read) or the task involved too many files for a single run.

**Lesson:** If an implementation task spans more than ~5 files, consider breaking it into sub-tasks or providing more targeted file hints in the issue body.

---

## Code Quality Assessment

### Test coverage
- **Unit tests**: Comprehensive across all service classes. Every fetcher, normalisation service, and computation service has unit tests with mocked dependencies.
- **Integration tests**: In-process tests (using `WebApplicationFactory`) added for DI wiring verification.
- **Live API tests**: Env-gated live tests for ONS, FRED, Shiller, and yfinance — useful for catching API contract changes.
- **E2E tests**: A basic E2E script (`e2e-test.sh`) was created for Docker Compose stack testing.
- **Frontend tests**: The Vitest/RTL setup was defined in Phase 3 spec but frontend test coverage level wasn't explicitly verified at POC close.

**Gap:** Frontend test coverage not verified at POC close. The Phase 4 implementation PR (#37) should have included tests per the TDD approach agreed in [3a].

### Error handling
The fetcher layer uses `FetcherException` consistently. The API layer maps errors to appropriate HTTP status codes. The orchestrator handles `ArgumentException` for unknown metric IDs and invalid ratio requests. This is a clean error handling model.

**Gap:** Issues #58 and #59 (404 for unknown metric, 400 for invalid ratio inputs) are not yet implemented. The error handling design is correct but not fully wired in the API layer.

### Performance
- 1-hour in-memory cache prevents repeated external API calls
- `Cache-Control: public, max-age=3600` aligns client/CDN caching with server caching
- No request coalescing — two simultaneous requests for a cold metric will both hit the external API (potential stampede). Acceptable for MVP.

---

## Technical Debt at POC Close

| Item | Severity | Description |
|---|---|---|
| #57 PR not merged | Low | Cache-Control headers PR ready but not merged |
| #58 not implemented | Medium | Unknown metric ID returns 500 instead of 404 |
| #59 not implemented | Medium | Invalid ratio inputs return 500 instead of 400 |
| #39 CSS | Low | Hardcoded dark-theme colours in some components |
| No cache stampede protection | Low | Concurrent cold requests may hit external APIs multiple times |
| Frontend test coverage unknown | Medium | Vitest tests may be incomplete from Phase 4 |
| OCIR auth not resolved | High | Deployment pipeline broken (#87) |
| [1a] secrets not documented | Low | Issue #2 never formally closed |

---

## Things to Carry Forward to Next Project

1. **ADR format** — Document every non-obvious technical decision with rationale and alternatives
2. **Env-gated live tests** — Skip live tests when secrets absent; always included alongside unit tests
3. **Typed exception boundaries** — One exception type per infrastructure layer, caught at the orchestrator
4. **OpenAPI → codegen pipeline** — Generate types and hooks from the running app, not manually
5. **Faker/Bogus skeleton before real implementation** — Front-end development against real contract from day one
6. **IEndpointFilter for cross-cutting concerns** — Cache-Control, correlation IDs, logging
7. **Shared test fixtures for rate-limited APIs** — Prevent 429s in test suites
