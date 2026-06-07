# CSS Theming Pitfalls & Lessons Learned

## Issue: Hardcoded colours break light-theme legibility

### Background

The app supports light and dark themes via CSS custom properties defined in `index.css`
(e.g. `--text-primary`, `--text-secondary`, `--glass-border`). During the initial dark-theme
build (Phase 4), several component-level CSS files were written with hardcoded hex values
that only look correct in dark mode:

| Hardcoded value | Appearance      | Dark mode | Light mode |
|-----------------|-----------------|-----------|------------|
| `#f1f5f9`       | Near-white       | ✅ Readable | ❌ Invisible (white-on-white) |
| `#cbd5e1`       | Light grey       | ✅ Subtle   | ❌ Barely visible |

Because local development typically runs with the dark theme active in `localStorage`, this
went unnoticed until Playwright ran fresh browser sessions (no `localStorage`) that defaulted
to light theme — screenshots 4 (Custom Comparison) and 5 (Indicators) were completely blank.

**Root issues tracked in:** #34, #35, #39
**Fixed in:** PR #40

### Files fixed

| File | Property/selector | Before | After |
|------|-------------------|--------|-------|
| `IndicatorCard.css` | `.indicator-card__value` | `#f1f5f9` | `var(--text-primary)` |
| `CompareSection.css` | error button, `h3`, current ratio | `#f1f5f9` / `#cbd5e1` | `var(--text-primary)` / `var(--text-secondary)` / `var(--glass-border)` |
| `RatioChart.css` | range button hover | `#f1f5f9` / `#cbd5e1` | `var(--text-primary)` / `var(--glass-border)` |
| `MetricPicker.css` | swap hover, trigger, item colours | `#f1f5f9` / `#cbd5e1` | `var(--text-primary)` / `var(--text-secondary)` / `var(--glass-border)` |
| `PresetCard.css` | title, value, retry button | `#f1f5f9` / `#cbd5e1` | `var(--text-primary)` / `var(--text-secondary)` / `var(--glass-border)` |
| `StickyNav.css` | `--text-primary` fallback | `var(--text-primary, #f1f5f9)` | `var(--text-primary)` |
| `Home.css` | `.home__section-title` | `#f1f5f9` | `var(--text-primary)` |

### Rule going forward

**Never use hardcoded `#f1f5f9` or `#cbd5e1` in component CSS.**
Always reach for the custom properties from `index.css`:

```css
/* Text */
color: var(--text-primary);     /* high-contrast body text */
color: var(--text-secondary);   /* subdued / label text */

/* Borders / glass */
border-color: var(--glass-border);
```

---

## Issue: Playwright mouse click crashes app (Recharts tooltip)

### Symptom

Clicking a `PresetCard` element with Playwright's `locator.click()` or `element.click()`
triggers a `DefaultTooltipContent` error in Recharts that unmounts the entire React app.
This happens because Playwright moves the mouse to the element before clicking, firing a
`mouseover`/`mouseenter` event that activates the Recharts tooltip before the click resolves.

### Workaround

Use a JavaScript `element.click()` via `page.evaluate()` — this fires the click without
synthesising mouse movement, so the tooltip never activates:

```js
await page.evaluate(() => {
  const title = document.querySelector('.preset-card:not(.preset-card--skeleton) .preset-card__title');
  if (title) title.click();
});
```

Click the card **title** element (not the chart area) to avoid the chart's event listeners
entirely.

### Long-term fix

The underlying Recharts crash should be fixed by adding an error boundary around the chart
in `PresetCard`, so a tooltip error cannot unmount the whole app.

---

## Verification script

`scripts/playwright-theme-verify.js` — captures light and dark theme screenshots and logs
computed colour values for key elements, confirming CSS custom properties resolve correctly.

```
node scripts/playwright-theme-verify.js
# requires: backend on :5257, frontend on :5173
```

Screenshots are written to `screenshots/` (git-ignored) and are not committed to the repo.
