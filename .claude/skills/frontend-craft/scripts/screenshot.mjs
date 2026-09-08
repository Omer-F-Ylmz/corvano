import puppeteer from 'puppeteer';
import fs from 'node:fs';
import path from 'node:path';

const [, , url = 'http://localhost:3000', label = ''] = process.argv;
const widths = (process.env.WIDTHS || '390,768,1440').split(',').map(Number);
const dir = path.resolve('.screens');
fs.mkdirSync(dir, { recursive: true });
const n = fs.readdirSync(dir).reduce((m, f) => Math.max(m, Number((f.match(/^shot-(\d+)-/) || [0, 0])[1])), 0) + 1;
const browser = await puppeteer.launch({ args: ['--ignore-certificate-errors'] });
for (const w of widths) {
  const page = await browser.newPage();
  await page.setViewport({ width: w, height: 900 });
  await page.goto(url, { waitUntil: 'networkidle0', timeout: 30000 });
  await page.evaluate(() => document.fonts.ready);
  const file = path.join(dir, `shot-${String(n).padStart(3, '0')}-${w}${label ? '-' + label : ''}.png`);
  await page.screenshot({ path: file, fullPage: true });
  console.log(file);
  await page.close();
}
await browser.close();
