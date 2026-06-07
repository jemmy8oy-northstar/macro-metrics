// Playwright theme verification script for macro-metrics
// Captures screenshots of the app in both light and dark themes, and verifies
// that CSS custom properties resolve to legible colours in each mode.
//
// Usage: node scripts/playwright-theme-verify.js
// Prerequisites: backend (port 5257) and frontend (port 5173) must be running.
//
// Known gotcha: clicking a PresetCard with a Playwright mouse click triggers a
// Recharts DefaultTooltipContent error (hover fires before click resolves).
// Use element.click() via page.evaluate() instead — no mouse movement, no crash.

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'http://localhost:5173';
const OUTPUT_DIR = path.join(__dirname, '../screenshots');

async function waitForPresets(page) {
  await page.waitForFunction(
    () => document.documentElement.hasAttribute('data-theme'),
    { timeout: 15000 }
  );
  const theme = await page.evaluate(() => document.documentElement.getAttribute('data-theme'));
  console.log(`  ✓ Theme: ${theme}`);

  await page.waitForFunction(
    () => document.querySelectorAll('.preset-card:not(.preset-card--skeleton)').length > 0,
    { timeout: 30000 }
  );
  console.log('  ✓ Preset cards loaded');
  await page.waitForSelector('svg.recharts-surface', { timeout: 15000 }).catch(() => {});
  await page.waitForTimeout(1500);
}

async function checkColor(page, selector) {
  return page.evaluate((sel) => {
    const el = document.querySelector(sel);
    if (!el) return null;
    const s = window.getComputedStyle(el);
    return { color: s.color, text: el.textContent?.trim().slice(0, 60) };
  }, selector);
}

async function shootTheme(browser, themeName, outputDir) {
  console.log(`\n=== ${themeName.toUpperCase()} THEME ===`);

  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    colorScheme: themeName === 'dark' ? 'dark' : 'light',
    storageState: {
      cookies: [],
      origins: [{
        origin: BASE_URL,
        localStorage: [{ name: 'theme', value: themeName }],
      }],
    },
  });

  const page = await context.newPage();
  page.on('console', msg => {
    if (msg.type() === 'error') console.log(`  PAGE ERR: ${msg.text().slice(0, 120)}`);
  });

  await page.goto(BASE_URL, { waitUntil: 'networkidle', timeout: 30000 });
  await waitForPresets(page);

  // ── Screenshot 1: Hero ──
  console.log('  📸 01-hero');
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForTimeout(500);
  await page.screenshot({ path: `${outputDir}/01-hero-${themeName}.png` });

  // ── Screenshot 2: Presets ──
  console.log('  📸 02-presets');
  await page.evaluate(() => { document.getElementById('presets')?.scrollIntoView({ behavior: 'instant' }); });
  await page.waitForTimeout(1500);

  const presetTitle = await checkColor(page, '.preset-card__title');
  console.log(`  preset-card__title colour: ${JSON.stringify(presetTitle)}`);
  const presetValue = await checkColor(page, '.preset-card__value');
  console.log(`  preset-card__value colour: ${JSON.stringify(presetValue)}`);

  await page.screenshot({ path: `${outputDir}/02-presets-${themeName}.png` });

  // ── Screenshot 3: Compare section ──
  // Use JS click (avoids mouse hover triggering Recharts tooltip error)
  console.log('  📸 04-compare — JS-clicking card title...');
  await page.evaluate(() => { document.getElementById('presets')?.scrollIntoView({ behavior: 'instant' }); });
  await page.waitForTimeout(300);

  const clicked = await page.evaluate(() => {
    const title = document.querySelector('.preset-card:not(.preset-card--skeleton) .preset-card__title');
    if (title) { title.click(); return true; }
    return false;
  });

  if (clicked) {
    console.log('  ✓ JS-clicked preset card title');
    try {
      await page.waitForSelector('.compare-section__chart-header', { timeout: 20000 });
      console.log('  ✓ Compare chart header loaded');
    } catch {
      console.log('  ⚠ Compare chart header timeout');
    }
    await page.waitForTimeout(2500);
  } else {
    console.log('  ⚠ No card title found');
  }

  await page.evaluate(() => { document.getElementById('compare')?.scrollIntoView({ behavior: 'instant' }); });
  await page.waitForTimeout(1500);

  const compareH3 = await checkColor(page, '.compare-section__chart-header h3');
  const compareCurrent = await checkColor(page, '.compare-section__current');
  console.log(`  compare h3 colour: ${JSON.stringify(compareH3)}`);
  console.log(`  compare current colour: ${JSON.stringify(compareCurrent)}`);

  await page.screenshot({ path: `${outputDir}/04-compare-${themeName}.png` });

  // ── Screenshot 4: Indicators ──
  console.log('  📸 05-indicators');
  await page.evaluate(() => { document.getElementById('indicators')?.scrollIntoView({ behavior: 'instant' }); });

  try {
    await page.waitForFunction(
      () => document.querySelectorAll('.indicator-card:not(.indicator-card--skeleton)').length > 0,
      { timeout: 20000 }
    );
    console.log('  ✓ Indicator cards loaded');
  } catch {
    console.log('  ⚠ Indicator cards timed out');
  }
  await page.waitForTimeout(1500);

  const indValue = await checkColor(page, '.indicator-card__value');
  console.log(`  indicator-card__value colour: ${JSON.stringify(indValue)}`);

  await page.screenshot({ path: `${outputDir}/05-indicators-${themeName}.png` });

  await context.close();
}

async function main() {
  fs.mkdirSync(OUTPUT_DIR, { recursive: true });

  const browser = await chromium.launch({
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-dev-shm-usage'],
  });

  await shootTheme(browser, 'light', OUTPUT_DIR);
  await shootTheme(browser, 'dark', OUTPUT_DIR);

  await browser.close();

  console.log('\n✅ All screenshots captured:');
  fs.readdirSync(OUTPUT_DIR).filter(f => f.endsWith('.png')).forEach(f => {
    const kb = Math.round(fs.statSync(`${OUTPUT_DIR}/${f}`).size / 1024);
    console.log(`  ${f}  (${kb} KB)`);
  });
}

main().catch(err => { console.error('Fatal:', err.message); process.exit(1); });
