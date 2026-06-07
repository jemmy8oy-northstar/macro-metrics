# MacroMetrics — Frontend Tech Decisions

> Part A of the [3a] spec. Proposed library choices for human approval before implementation begins.

---

## 1. Chart / Visualisation Library — **Recharts**

**Recommendation: Recharts**

| Candidate | Notes |
|---|---|
| **Recharts** ✅ | React-native composable API. `LineChart`, `ReferenceLine` (dashed avg), `Tooltip` (custom hover), `ResponsiveContainer` — all map directly to the design. Strong TypeScript support. |
| Nivo | More opinionated and heavier. Good for dashboards but overkill for a focused ratio chart. |
| Victory | Similar API to Recharts but less actively maintained. |
| Chart.js (react-chartjs-2) | Imperative DOM mutation under the hood — less natural in React. Canvas-based makes custom tooltips harder. |
| D3 directly | Too low-level for MVP. Would require building all React integration from scratch. |

**Why Recharts wins for MacroMetrics:**
- `<ReferenceLine>` renders the dashed long-run average line out of the box
- `<ResponsiveContainer>` handles the two chart modes (compact ~160px, full ~320px)
- Custom `<Tooltip>` content component covers the "date / ratio / % above avg" hover state
- No imperative code — purely declarative JSX

---

## 2. UI Component Library — **Radix UI (Select primitive only)**

**Recommendation: No heavy component library — pure SCSS for most UI, Radix UI Select for metric dropdowns**

The design is custom and the template already uses SCSS with design tokens. Pulling in MUI or Mantine would add significant bundle weight for components we won't use.

The one place a headless primitive adds real value is the **metric picker dropdowns** (F3.1). A native `<select>` cannot be styled to match the design (custom option rows, tick marks, exclusion logic). Radix UI Select provides:
- Full keyboard navigation and ARIA roles for free
- Completely unstyled — styled via co-located SCSS
- Tiny bundle impact (only the Select package is installed)

| Candidate | Notes |
|---|---|
| **Radix UI Select** ✅ | Headless, accessible, SCSS-friendly. Only the `@radix-ui/react-select` package needed. |
| shadcn/ui | Builds on Radix — adds Tailwind styling we don't need. Would conflict with existing SCSS approach. |
| MUI / Mantine | Full component libraries — significant bundle size, opinionated styling that would fight the SCSS design tokens. |
| Native `<select>` | Cannot match the design (custom rows, swap button integration). |

---

## 3. Date Handling — **date-fns**

**Recommendation: date-fns**

Used for two things:
1. Formatting axis labels (e.g. `Jan 2007`, `2010`)
2. Client-side date range filtering (`subYears`, `isAfter`, `isBefore`)

| Candidate | Notes |
|---|---|
| **date-fns** ✅ | Tree-shakeable (only imported functions are bundled). Great TypeScript types. `format`, `subYears`, `isAfter` cover all our needs. |
| dayjs | Similar size, good API. Slightly less TypeScript-native. Either would work fine. |
| Native `Intl` / `Temporal` | `Intl.DateTimeFormat` covers formatting. `Temporal` not yet in all browsers. Would require manual date arithmetic for range filtering. Viable but more verbose. |

---

## 4. Other Runtime Dependencies

| Need | Decision |
|---|---|
| API client / state | RTK Query — already in template. No change. |
| Global state | Redux Toolkit — already in template. No change. |
| Icons | No icon library. The design uses only ⇄ (Unicode swap) and ⚠ (Unicode warning). No icon package needed. |
| Loading skeletons | CSS-only skeleton shimmer via SCSS animation. No library needed. |
| URL sharing of picker state | Post-MVP (deferred to #26). No routing library needed for MVP. |

---

## Summary

| Layer | Decision |
|---|---|
| Charts | Recharts |
| Component primitives | Radix UI `@radix-ui/react-select` (metric picker only) |
| Date utilities | date-fns |
| Styling | SCSS (existing template approach — no change) |
| API / State | RTK Query + Redux Toolkit (existing — no change) |

**New packages to install:**
```bash
npm install recharts @radix-ui/react-select date-fns
```
