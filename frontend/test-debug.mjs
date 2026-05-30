import { chromium } from 'playwright';

const browser = await chromium.launch({
  executablePath: process.env.CHROME_PATH,
  headless: true,
  args: ['--no-sandbox']
});
const page = await browser.newPage();
await page.setViewportSize({ width: 390, height: 844 });

await page.goto('http://localhost:4200', { waitUntil: 'networkidle' });

// เลือก Stainless
await page.click('input[value="Stainless"]');
await page.waitForTimeout(800);

// ดู sizes ที่มีอยู่
const sizes = await page.locator('select[formcontrolname="sizeInches"] option').allTextContents();
console.log('Available sizes:', sizes);

// เลือก 6
await page.selectOption('select[formcontrolname="sizeInches"]', '6');
await page.waitForTimeout(1000);

// ดู DOM ทั้งหมดหลัง select
const formHTML = await page.locator('form').innerHTML();
console.log('Form HTML contains finish select:', formHTML.includes('formcontrolname="finish"'));

// ดูค่า sizeInches ใน form
const sizeValue = await page.evaluate(() => {
  const el = document.querySelector('select[formcontrolname="sizeInches"]');
  return el ? el.value : 'not found';
});
console.log('sizeInches value in DOM:', sizeValue, typeof sizeValue);

// Check if @if block rendered finish
const allSelects = await page.locator('select').count();
console.log('Total selects on page:', allSelects);
const selectNames = await page.locator('select').evaluateAll(els => els.map(e => e.getAttribute('formcontrolname') || e.id || 'unnamed'));
console.log('Select formcontrolnames:', selectNames);

await page.screenshot({ path: '/tmp/debug-after-stainless-6.png', fullPage: true });
console.log('screenshot: /tmp/debug-after-stainless-6.png');

await browser.close();
