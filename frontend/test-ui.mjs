import { chromium } from 'playwright';

const BASE = 'http://localhost:4200';
const SS = (name) => `/tmp/ss-raingutter-${name}.png`;

const browser = await chromium.launch({
  executablePath: process.env.CHROME_PATH,
  headless: true,
  args: ['--no-sandbox', '--disable-dev-shm-usage']
});

const page = await browser.newPage();
await page.setViewportSize({ width: 390, height: 844 });

const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(e.message));

// ─── 1. หน้าแรก ────────────────────────────────────────────────────────────
console.log('\n[1] หน้าแรก — Calculator');
await page.goto(BASE, { waitUntil: 'networkidle' });
await page.screenshot({ path: SS('01-home'), fullPage: true });
const h1 = await page.textContent('h1');
console.log('  H1:', h1);

// ─── 2. กรอกฟอร์ม (Stainless + บ้านพักอาศัย / ทาวน์เฮ้าส์) ──────────────
console.log('\n[2] เลือกวัสดุ Stainless');
await page.click('input[value="Stainless"]');
await page.waitForTimeout(600);

console.log('  เลือกประเภทอาคาร (option แรก)');
await page.waitForSelector('select[formcontrolname="buildingTypeId"]', { timeout: 5000 });
await page.selectOption('select[formcontrolname="buildingTypeId"]', { index: 1 });

await page.fill('input[formcontrolname="lengthMeters"]', '8');
await page.fill('input[formcontrolname="downspoutCount"]', '2');
await page.locator('input[formcontrolname="floors"]').nth(1).click();
await page.locator('label.toggle-row').click();
await page.screenshot({ path: SS('02-form-filled'), fullPage: true });
console.log('  screenshot:', SS('02-form-filled'));

// ─── 3. คำนวณราคา ──────────────────────────────────────────────────────────
console.log('\n[3] กด "คำนวณราคา"');
await page.click('button:has-text("คำนวณ")');
await page.waitForSelector('tfoot', { timeout: 8000 });
await page.screenshot({ path: SS('03-estimate-result'), fullPage: true });

const totalText = await page.textContent('tfoot tr td:last-child');
console.log('  ยอดรวม:', totalText?.trim());
console.log(totalText?.includes('9,980') ? '  ✅ 9,980 บาท ถูกต้อง' : '  ❌ ยอดไม่ถูกต้อง');

const disclaimer = await page.locator('.disclaimer').first().textContent();
console.log('  Disclaimer:', disclaimer?.trim().slice(0, 50));

// ─── 4. ฟอร์มติดต่อ ─────────────────────────────────────────────────────────
console.log('\n[4] ขอใบเสนอราคา');
await page.click('button:has-text("ขอใบเสนอราคา")');
await page.waitForSelector('input[formcontrolname="customerName"]', { timeout: 3000 });

await page.fill('input[formcontrolname="customerName"]', 'สมชาย ใจดี');
await page.fill('input[formcontrolname="phone"]', '0891234567');
await page.fill('textarea[formcontrolname="address"]', '99 ถ.ลาดพร้าว กรุงเทพฯ');
await page.screenshot({ path: SS('04-contact-form'), fullPage: true });
console.log('  screenshot:', SS('04-contact-form'));

console.log('  ส่งใบเสนอราคา');
await page.click('button:has-text("ส่งขอใบเสนอราคา")');
await page.waitForURL('**/thank-you/**', { timeout: 8000 });

// ─── 5. Thank You ───────────────────────────────────────────────────────────
console.log('\n[5] หน้า Thank You');
await page.waitForTimeout(800);
await page.screenshot({ path: SS('05-thank-you'), fullPage: true });
const url = page.url();
console.log('  URL:', url);
console.log(url.includes('/thank-you/QT-') ? '  ✅ redirect ถูกต้อง' : '  ❌ redirect ผิด');

// ─── 6. Admin Login ─────────────────────────────────────────────────────────
console.log('\n[6] Admin Login');
await page.setViewportSize({ width: 1280, height: 900 });
await page.goto(`${BASE}/admin/login`, { waitUntil: 'networkidle' });
await page.screenshot({ path: SS('06-admin-login') });

await page.fill('input[formcontrolname="username"]', 'admin');
await page.fill('input[formcontrolname="password"]', 'admin1234');
await page.click('button[type="submit"]');
await page.waitForURL('**/admin/dashboard', { timeout: 6000 });
console.log('  ✅ Login สำเร็จ');

// ─── 7. Dashboard ───────────────────────────────────────────────────────────
console.log('\n[7] Dashboard');
await page.waitForSelector('.stat-card', { timeout: 5000 });
await page.screenshot({ path: SS('07-dashboard') });
const statCards = await page.locator('.stat-card').count();
console.log('  stat cards:', statCards);
const firstVal = await page.locator('.stat-card .value').first().textContent();
console.log('  Total leads:', firstVal?.trim());

// ─── 8. Leads List ──────────────────────────────────────────────────────────
console.log('\n[8] Leads list');
await page.click('a:has-text("Leads")');
await page.waitForSelector('tbody tr', { timeout: 5000 });
await page.screenshot({ path: SS('08-leads-list') });
const rows = await page.locator('tbody tr').count();
console.log('  rows:', rows, rows > 0 ? '✅' : '❌ ไม่มีข้อมูล');

// ─── 9. Lead Detail ─────────────────────────────────────────────────────────
console.log('\n[9] Lead Detail');
await page.click('a:has-text("ดูรายละเอียด")');
await page.waitForSelector('.card', { timeout: 5000 });
await page.screenshot({ path: SS('09-lead-detail') });
const hasStatus = await page.locator('select').count();
console.log('  status dropdown:', hasStatus > 0 ? '✅ แสดง' : '❌ ไม่มี');

// ─── 10. Pricing Management ─────────────────────────────────────────────────
console.log('\n[10] Pricing Management');
await page.click('a:has-text("ราคา/ตั้งค่า")');
await page.waitForSelector('table', { timeout: 5000 });
await page.screenshot({ path: SS('10-pricing') });
const productRows = await page.locator('tbody tr').count();
console.log('  product rows:', productRows, productRows === 6 ? '✅ 6 สินค้า' : `(expected 6, got ${productRows})`);

// ─── 11. Validation test ────────────────────────────────────────────────────
console.log('\n[11] Validation — เบอร์โทรผิดรูปแบบ');
await page.goto(BASE, { waitUntil: 'networkidle' });
await page.setViewportSize({ width: 390, height: 844 });
await page.click('input[value="Galvanized"]');
await page.waitForTimeout(400);
await page.waitForSelector('select[formcontrolname="buildingTypeId"]', { timeout: 5000 });
await page.selectOption('select[formcontrolname="buildingTypeId"]', { index: 1 });
await page.fill('input[formcontrolname="lengthMeters"]', '10');
await page.click('button:has-text("คำนวณ")');
await page.waitForSelector('tfoot', { timeout: 5000 });
await page.click('button:has-text("ขอใบเสนอราคา")');
await page.waitForSelector('input[formcontrolname="customerName"]', { timeout: 3000 });
await page.fill('input[formcontrolname="customerName"]', 'ทดสอบ');
await page.fill('input[formcontrolname="phone"]', '123');
await page.click('button:has-text("ส่งขอใบเสนอราคา")');
await page.waitForTimeout(500);
const errMsg = await page.locator('.error-msg').first().textContent().catch(() => '');
console.log('  error message:', errMsg?.trim());
await page.screenshot({ path: SS('11-validation'), fullPage: true });

// ─── Summary ────────────────────────────────────────────────────────────────
console.log('\n=== Console errors ===');
const jsErrors = errors.filter(e => !e.includes('favicon') && !e.includes('ExpressionChanged'));
jsErrors.length === 0
  ? console.log('  ✅ ไม่มี JS error')
  : jsErrors.forEach(e => console.log('  ❌', e.slice(0, 120)));

await browser.close();
console.log('\n✅ ทดสอบ UI ครบแล้ว — screenshots อยู่ที่ /tmp/ss-raingutter-*.png');
