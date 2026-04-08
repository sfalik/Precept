/**
 * Captures readme-hero-dsl.png from readme-hero-dsl.html.
 *
 * Regeneration contract (mirrored in the HTML source comment):
 *  - GitHub README images cap at 830px display width.
 *  - Source type is 14px mono (set in the HTML's <pre> font-size).
 *  - Capture at 2× (deviceScaleFactor: 2) → 1660px-wide retina PNG.
 *    GitHub scales it to 830px; text lands at ~14.5px effective.
 *
 * Run:  node design/brand/capture-hero-dsl.mjs
 */

import { chromium } from 'playwright';
import { fileURLToPath } from 'url';
import path from 'path';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const htmlPath = path.join(__dirname, 'readme-hero-dsl.html');
const outputPath = path.join(__dirname, 'readme-hero-dsl.png');

// GitHub's max image display width in repo README view
const GITHUB_MAX_IMAGE_WIDTH = 830;

async function capture() {
  const browser = await chromium.launch();
  const page = await browser.newPage({
    viewport: { width: GITHUB_MAX_IMAGE_WIDTH, height: 1200 },
    deviceScaleFactor: 2,
  });

  await page.goto(`file:///${htmlPath.replace(/\\/g, '/')}`);
  await page.waitForLoadState('networkidle');

  // Ensure the <pre> fills the viewport width for correct whitespace padding
  await page.addStyleTag({
    content: `pre { min-width: ${GITHUB_MAX_IMAGE_WIDTH}px; box-sizing: border-box; }`
  });

  await page.evaluate(() => document.fonts.ready);

  const pre = page.locator('pre');
  await pre.screenshot({
    path: outputPath,
    omitBackground: true,
    type: 'png',
  });

  const box = await pre.boundingBox();
  const imgWidth = Math.round(box.width * 2); // deviceScaleFactor
  const imgHeight = Math.round(box.height * 2);
  console.log(`Captured: ${imgWidth}×${imgHeight}px (2× retina) → ${outputPath}`);
  console.log(`  Display width on GitHub: ${GITHUB_MAX_IMAGE_WIDTH}px`);
  console.log(`  Effective code font-size: ~${(14 * GITHUB_MAX_IMAGE_WIDTH / imgWidth * 2).toFixed(1)}px`);

  await browser.close();
}

capture().catch(err => {
  console.error(err);
  process.exit(1);
});
