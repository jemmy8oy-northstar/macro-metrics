# MacroMetrics — Frontend User Stories

> Part B of the [3a] spec. BDD-style stories derived from the signed-off Phase 2 designs (`docs/designs/homepage.md`).
> Libraries referenced are from the approved tech decisions (`docs/tech-decisions-frontend.md`).
>
> **BDD (Behaviour-Driven Development)** — a format for writing requirements as human-readable scenarios. Each story follows the pattern: *who* wants to do *what* and *why*, with concrete acceptance criteria that define when the story is done. This makes stories testable and keeps implementation grounded in user intent.

---

## Story 1 — Page loads with skeleton states

**Feature:** F2.1 Homepage Layout & Navigation, F2.3 Preset Ratio Card Grid, F4.2 Indicators Section

> As a visitor, I want the page to render immediately with skeleton placeholders while data loads, so I know the app is working even before any data arrives.

**Acceptance criteria:**

- [ ] On navigation to `/`, the page shell renders instantly: header, sticky mini-nav, section headings, and skeleton versions of all 9 preset ratio cards and all 3 indicator cards
- [ ] All 9 preset ratio fetches fire in parallel (`GET /api/metrics/ratio?numerator=X&denominator=Y`)
- [ ] All 3 indicator fetches fire in parallel (`GET /api/metrics/{id}`)
- [ ] As each response arrives, the corresponding skeleton is replaced by the real card — cards do not wait for all fetches to complete
- [ ] The sticky mini-nav contains three anchor links: **Presets**, **Compare**, **Indicators**
- [ ] Clicking a mini-nav anchor smoothly scrolls to the matching section
- [ ] The mini-nav remains visible (sticky) as the user scrolls

---

## Story 2 — Preset ratio card displays correctly

**Feature:** F2.2 Ratio Chart Component, F2.3 Preset Ratio Card Grid

> As a visitor, I want each preset card to show the current ratio value and a compact chart so I can immediately see where an asset sits relative to its history.

**Acceptance criteria:**

- [ ] Each loaded preset card displays: title, headline ratio value (e.g. `8.3×`), and % above/below long-run average (e.g. `▲ 42% avg`)
- [ ] Chart is rendered using a Recharts `<LineChart>` inside a `<ResponsiveContainer>` at ~160px height (compact mode)
- [ ] A dashed `<ReferenceLine>` is drawn at the long-run average value
- [ ] No axis labels are shown in compact mode
- [ ] Hovering over a data point shows a Recharts custom `<Tooltip>` with: date (formatted via date-fns, e.g. `Jan 2007`), ratio value, and % above/below average
- [ ] Date range buttons (`1y / 2y / 5y / 10y / 20y / Max`) are shown below the chart; default is **Max**
- [ ] Clicking a date range button filters the visible series client-side using date-fns `subYears` / `isAfter` — no re-fetch
- [ ] The long-run average `<ReferenceLine>` recalculates over the visible date range when the range changes
- [ ] The 9 presets are ordered by theme: Affordability (UK, US, UK Real HPI) → Inflation-adjusted (Real Gold, Real S&P 500, Real Oil) → Cross-asset (Gold vs Equities, UK Property/Gold, BTC vs Gold)
- [ ] Cards are displayed in a 3-column responsive grid

---

## Story 3 — Preset card click pre-populates custom comparison

**Feature:** F2.3 Preset Ratio Card Grid, F3.1 Metric Picker UI, F3.2 Custom Comparison Chart

> As a visitor, I want to click a preset card and be taken to the custom comparison section with that ratio pre-loaded, so I can explore it in full detail.

**Acceptance criteria:**

- [ ] Clicking anywhere on a loaded preset card scrolls the page to the `#compare` section
- [ ] The numerator dropdown is set to the preset's numerator metric
- [ ] The denominator dropdown is set to the preset's denominator metric
- [ ] The custom comparison fetch is triggered automatically (as if the user had selected both dropdowns manually)
- [ ] The full-width chart renders with the preset's data

---

## Story 4 — Custom comparison picker selects two metrics

**Feature:** F3.1 Metric Picker UI

> As a visitor, I want to select any two comparable metrics from dropdowns so I can build a custom ratio the preset cards don't cover.

**Acceptance criteria:**

- [ ] Two Radix UI `<Select>` dropdowns are rendered side by side with a ⇄ swap button between them
- [ ] Each dropdown lists the 11 comparable metrics: UK House Prices, US House Prices, UK Wages, US Wages, UK CPI, US CPI, Gold, Oil, FTSE 100, S&P 500, Bitcoin
- [ ] The metric selected as numerator is excluded from the denominator dropdown options, and vice versa
- [ ] Clicking ⇄ swaps the current numerator and denominator selections; if both are set, the chart re-fetches with swapped values
- [ ] When only the numerator is selected, a prompt "Now select a denominator" is shown in place of the chart
- [ ] When neither is selected, a prompt "Select two metrics above to see a custom comparison" is shown
- [ ] When both are selected, the custom comparison chart is rendered (see Story 5)

---

## Story 5 — Custom comparison chart renders

**Feature:** F3.2 Custom Comparison Chart, F2.2 Ratio Chart Component

> As a visitor, when I have selected two metrics I want to see a full-width chart of their ratio so I can analyse the relationship in detail.

**Acceptance criteria:**

- [ ] On both metrics being selected, `GET /api/metrics/ratio?numerator=X&denominator=Y` is fired
- [ ] While fetching, a loading skeleton is shown in the chart area
- [ ] On success, a Recharts `<LineChart>` renders at ~320px height (full mode) with:
  - Chart title: `{Numerator label} / {Denominator label}`
  - Current ratio value and % above/below avg in the top-right
  - Y-axis labels (ratio values)
  - X-axis date labels (year, formatted via date-fns)
  - Ratio line
  - Dashed `<ReferenceLine>` at long-run average, labelled with the avg value (e.g. `avg (5.2×)`)
  - Custom `<Tooltip>` with date, ratio value, % above/below average
- [ ] Date range buttons (`1y / 2y / 5y / 10y / 20y / Max`) below the chart; default **Max**; filtering is client-side
- [ ] On API error, an inline error message is shown: `⚠ Could not load data.` with a **Try again** button that re-fires the fetch

---

## Story 6 — Indicator cards display correctly

**Feature:** F4.1 Indicator Card Component, F4.2 Indicators Section

> As a visitor, I want to see the CAPE ratio, UK 10yr Gilt yield, and US 10yr Treasury yield as cards so I can understand the broader macro context.

**Acceptance criteria:**

- [ ] Three indicator cards are displayed in a row, each narrower than the preset ratio cards (~1/3 page width)
- [ ] Each loaded card shows: title, current value (e.g. `32.4×` or `4.6%`), and % above/below long-run average (e.g. `+90% above long-run avg`)
- [ ] Each card contains a sparkline — a compact Recharts `<LineChart>` with a dashed `<ReferenceLine>` at the long-run average, labelled with the avg value (e.g. `avg (17×)`)
- [ ] Indicator cards are visually distinct from preset ratio cards (muted styling, narrower width) — they provide supporting context, not the primary content
- [ ] While loading, a skeleton shimmer fills the card
- [ ] On error, the card shows `⚠ Unavailable` (no retry for MVP — indicator errors are non-blocking)

---

## Story 7 — Individual card errors do not break the page

**Feature:** F2.3, F4.2 (cross-cutting)

> As a visitor, when one data fetch fails I want only the affected card to show an error state, so the rest of the page still works.

**Acceptance criteria:**

- [ ] Each preset card manages its own RTK Query fetch independently; an error on one card does not affect others
- [ ] Each indicator card manages its own fetch independently
- [ ] A failed preset card shows: `⚠ Could not load data. Retry?` with a **Try again** button
- [ ] A failed indicator card shows: `⚠ Unavailable`
- [ ] Clicking **Try again** on a preset card re-fires that card's fetch only
- [ ] All other cards on the page remain unaffected by the error

---

## API skeleton

The frontend talks to two backend endpoints via RTK Query. During Phase 4 these are mocked; real implementations come in Phase 5.

### Endpoints

#### `GET /api/metrics/ratio`
Returns a time series for the ratio of two comparable metrics.

**Query params:** `numerator` (metric ID), `denominator` (metric ID)

**Response shape:**
```ts
{
  numerator: string        // e.g. "uk-house-prices"
  denominator: string      // e.g. "uk-wages"
  longRunAverage: number   // average over full history
  series: Array<{
    date: string           // ISO date, e.g. "2007-01-01"
    value: number          // ratio at that date
  }>
}
```

#### `GET /api/metrics/indicator/:id`
Returns a time series for a standalone indicator (CAPE, gilt yield, treasury yield).

**Path param:** `id` — one of `cape`, `uk-10yr-gilt`, `us-10yr-treasury`

**Response shape:**
```ts
{
  id: string               // e.g. "cape"
  label: string            // e.g. "CAPE Ratio"
  unit: string             // e.g. "×" or "%"
  longRunAverage: number
  series: Array<{
    date: string
    value: number
  }>
}
```

### RTK Query wiring (`src/api/macroMetricsApi.ts`)

Two endpoints to define on the RTK Query API slice:

| Hook | Endpoint | Used by |
|---|---|---|
| `useGetRatioQuery({ numerator, denominator })` | `GET /api/metrics/ratio` | Preset cards, custom comparison chart |
| `useGetIndicatorQuery(id)` | `GET /api/metrics/indicator/:id` | Indicator cards |

### Phase 4 mock strategy — Faker backend skeleton

During frontend implementation (Phase 4), a **backend skeleton** is created alongside the frontend issues (as part of [3b]). The skeleton implements the two endpoints above but returns **Faker-generated data** (`Bogus` NuGet package) instead of live data sources.

**Why this approach over MSW:**
- Frontend makes real HTTP calls — no network interception or test-only wiring
- OpenAPI spec is auto-generated from the routes (Scalar/OpenAPI already in template)
- RTK Query hooks are generated via `npm run codegen` — not hand-written
- The backend is production-shaped from day one; Phase 5 swaps Faker for real data sources

**Backend skeleton structure:**
- Two minimal API routes in `MacroMetrics.WebApi` matching the contracts above
- `Bogus` generates a seeded, deterministic time series (same data on every run)
- Response shapes match the TypeScript interfaces exactly
- No database, no service layer — Faker data returned directly from the route handler

**Frontend wiring:**
- `frontend/.env.development` sets `VITE_API_BASE_URL=http://localhost:5000`
- RTK Query codegen (`npm run codegen`) generates hooks from the live OpenAPI spec
- No MSW, no fixture files

---

## Frontend TDD

Each acceptance criteria item in the stories above maps directly to a **Vitest + React Testing Library** test. Tests are written **before** the component — the AC defines what "done" looks like.

```tsx
// Example: PresetCard.test.tsx
describe('PresetCard', () => {
  it('shows a skeleton while data is loading', () => {
    render(<PresetCard preset={UK_AFFORDABILITY} />);
    expect(screen.getByTestId('card-skeleton')).toBeInTheDocument();
  });

  it('shows the ratio value and % above average once loaded', async () => {
    render(<PresetCard preset={UK_AFFORDABILITY} />);
    expect(await screen.findByText('8.3×')).toBeInTheDocument();
    expect(screen.getByText('▲ 42% avg')).toBeInTheDocument();
  });

  it('shows an error state with retry button on fetch failure', async () => {
    server.use(ratioErrorHandler);
    render(<PresetCard preset={UK_AFFORDABILITY} />);
    expect(await screen.findByText(/could not load/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });
});
```

**Convention:** one `*.test.tsx` file per component, mirroring the AC checklist. Tests describe behaviour from the user's perspective, not implementation details.

See `docs/specs/testing-strategy.md` for the full frontend testing conventions.

---

## Implementation notes

- The `RatioChart` component (F2.2) is a single reusable component used in both compact mode (preset cards) and full mode (custom comparison): prop `mode: 'compact' | 'full'`
- The `IndicatorCard` (F4.1) reuses the same Recharts sparkline pattern but with fixed dimensions
- Skeleton states are CSS-only shimmer animations (no library)
- Date range filtering (1y/2y/5y/10y/20y/Max) is entirely client-side — full series fetched once, sliced via date-fns on the stored data
