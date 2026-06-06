# Backend Data Ingestion — Phase 5 Backlog

> **Status: Draft backlog — to be designed in GitHub Phase 5 (`[5a]`)**
> This is not an epic. It is a holding document for backend capability requirements captured during Phase 1 spec. The full design (service architecture, data models, API contract) will be produced in the `[5a]` backend design PR once the frontend skeleton is signed off.

---

## Capability summary

The backend must fetch time-series data from three external public APIs, normalise all series to a consistent format, and expose them to the frontend via a REST API. No database is required for MVP — the backend is a stateless proxy.

## Data sources

| Provider | Covers |
|---|---|
| **yfinance** | Gold, Oil, FTSE 100, S&P 500, Bitcoin |
| **FRED API** (free key) | US CPI, US Wages, US House Prices, CAPE, US 10yr Yield |
| **ONS API** (no key) | UK CPI, UK Wages, UK House Prices |

## Metric catalogue

| Metric ID | Label | Unit | Source |
|---|---|---|---|
| `uk-house-prices` | UK House Prices | Index (£) | ONS HPI |
| `us-house-prices` | US House Prices | Index | FRED CSUSHPISA |
| `uk-wages` | UK Average Wages | £/year | ONS EARN01 |
| `us-wages` | US Average Hourly Earnings | $/hr | FRED |
| `uk-cpi` | UK CPI | Index | ONS |
| `us-cpi` | US CPI | Index | FRED |
| `gold` | Gold Price | $/oz | yfinance `GC=F` |
| `oil` | Oil Price (Brent) | $/barrel | yfinance `BZ=F` |
| `ftse100` | FTSE 100 | Index pts | yfinance `^FTSE` |
| `sp500` | S&P 500 | Index pts | yfinance `^GSPC` |
| `bitcoin` | Bitcoin | USD | yfinance `BTC-USD` |
| `cape` | Shiller CAPE Ratio | Ratio | FRED `CAPE` |
| `uk-10yr-gilt` | UK 10yr Gilt Yield | % | yfinance / FRED |
| `us-10yr-treasury` | US 10yr Treasury Yield | % | FRED `DGS10` |

Note: `cape`, `uk-10yr-gilt`, and `us-10yr-treasury` are standalone indicators — not available as comparison candidates.

## Sub-capabilities

- [phase5-data-fetchers.md](./phase5-data-fetchers.md) — fetching from yfinance, FRED, ONS
- [phase5-data-normalisation.md](./phase5-data-normalisation.md) — aligning series to common cadence
- [phase5-metrics-api.md](./phase5-metrics-api.md) — REST API endpoints

## Phase 6 implementation issues (created by `[5c]`)

See sub-capability files for full story lists:
- [phase5-data-fetchers.md](./phase5-data-fetchers.md) — #53, #54, #55
- [phase5-data-normalisation.md](./phase5-data-normalisation.md) — #51, #52
- [phase5-metrics-api.md](./phase5-metrics-api.md) — #43–#50, #56–#59
