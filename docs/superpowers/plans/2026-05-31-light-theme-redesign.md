# Light Theme Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the dark-navy theme with a light blue/silver palette in `frontend/src/styles.css`.

**Architecture:** Single-file change — update 14 CSS variables in `:root`, then fix every hardcoded dark-color reference throughout the same file. No component files touched.

**Tech Stack:** CSS custom properties, Angular 18 (global stylesheet)

---

### Task 1: Update CSS Variables in `:root`

**Files:**
- Modify: `frontend/src/styles.css:3-22`

- [ ] **Step 1: Replace the entire `:root` block**

```css
:root {
  --bg-base:      #EBF4FF;
  --bg-panel:     #F0F7FF;
  --bg-surface:   #FFFFFF;
  --bg-input:     #F4F8FC;
  --border:       #C9D8E8;
  --border-hi:    #7BAFD4;
  --blue:         #0284C7;
  --amber:        #D97706;
  --teal:         #0891B2;
  --green:        #059669;
  --red:          #DC2626;
  --text:         #0D2137;
  --text-2:       #4B6A85;
  --text-dim:     #94A3B8;
  --mono:         'IBM Plex Mono', monospace;
  --thai:         'Sarabun', sans-serif;
  --head:         'Barlow Condensed', sans-serif;
  --r:            4px;
}
```

- [ ] **Step 2: Verify dev server still compiles**

```bash
cd frontend && npm start 2>&1 | grep -E "error|Error" | head -5
```
Expected: no compilation errors (Angular compiles CSS without type-checking).

---

### Task 2: Fix Blueprint Grid Overlay

**Files:**
- Modify: `frontend/src/styles.css` — `body::before` rule

- [ ] **Step 1: Change grid line colour from old blue to new blue**

Replace:
```css
  background-image:
    linear-gradient(rgba(56,189,248,.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(56,189,248,.03) 1px, transparent 1px);
```
With:
```css
  background-image:
    linear-gradient(rgba(2,132,199,.05) 1px, transparent 1px),
    linear-gradient(90deg, rgba(2,132,199,.05) 1px, transparent 1px);
```

---

### Task 3: Fix Hardcoded Dark Button Colours

**Files:**
- Modify: `frontend/src/styles.css` — button rules

- [ ] **Step 1: Fix `btn-primary` disabled state (dark bg → light blue)**

Replace:
```css
  background: #122a42;
  color: var(--text-dim);
  border-color: var(--border);
```
With:
```css
  background: #C8DCF0;
  color: var(--text-dim);
  border-color: var(--border);
```

- [ ] **Step 2: Fix `btn-calculate` disabled state**

Replace:
```css
  background: #20160000;
  color: #4a3010;
```
With:
```css
  background: rgba(217,119,6,.08);
  color: #92530A;
```

- [ ] **Step 3: Verify `color: #07101c` on coloured buttons stays as-is**

These lines provide dark text on saturated blue/green/red buttons — correct contrast on light bg, no change needed:
```
.btn-primary        { color: #07101c }
.btn-calculate      { color: #07101c }
.btn-success        { color: #07101c }
.btn-danger         { color: #07101c }
.pagination .active { color: #07101c }
```

---

### Task 4: Fix Blue `rgba()` Values (RGB 56,189,248 → 2,132,199)

**Files:**
- Modify: `frontend/src/styles.css` — all occurrences of `rgba(56,189,248,…)`

- [ ] **Step 1: Replace every `rgba(56,189,248,` with `rgba(2,132,199,`**

This covers: focus rings, hover glows, mat-tile selected glow, btn-secondary hover, table row hover, badge-new, search border, input focus shadow. Use replace-all in the Edit tool.

All 14 occurrences:
| Old | New |
|---|---|
| `rgba(56,189,248,.35)` | `rgba(2,132,199,.35)` |
| `rgba(56,189,248,.07)` | `rgba(2,132,199,.07)` |
| `rgba(56,189,248,.12)` | `rgba(2,132,199,.12)` |
| `rgba(56,189,248,.06)` | `rgba(2,132,199,.06)` |
| `rgba(56,189,248,.1)` | `rgba(2,132,199,.1)` |
| `rgba(56,189,248,.15)` | `rgba(2,132,199,.15)` |
| `rgba(56,189,248,.025)` | `rgba(2,132,199,.025)` |
| `rgba(56,189,248,.3)` | `rgba(2,132,199,.3)` |
| `rgba(56,189,248,.08)` | `rgba(2,132,199,.08)` |
| `rgba(56,189,248,.28)` | `rgba(2,132,199,.28)` |

---

### Task 5: Fix Border `rgba()` Values

**Files:**
- Modify: `frontend/src/styles.css` — table and tab border rules

- [ ] **Step 1: Fix dark-navy border rgba values**

Replace:
```css
border-bottom: 1px solid rgba(26,48,80,.6);
```
With:
```css
border-bottom: 1px solid rgba(201,216,232,.9);
```

Replace (second occurrence — tab underline):
```css
border-bottom: 1px solid rgba(26,48,80,.5);
```
With:
```css
border-bottom: 1px solid rgba(201,216,232,.8);
```

---

### Task 6: Fix Badge Inactive

**Files:**
- Modify: `frontend/src/styles.css` — `.badge-inactive` rule

- [ ] **Step 1: Replace dark tinted background with light silver**

Replace:
```css
.badge-inactive  { background: rgba(48,68,80,.4);   color: var(--text-2); border: 1px solid var(--border); }
```
With:
```css
.badge-inactive  { background: rgba(148,163,184,.15); color: var(--text-2); border: 1px solid var(--border); }
```

---

### Task 7: Screenshot & Commit

**Files:**
- `frontend/src/styles.css` (already modified in Tasks 1–6)

- [ ] **Step 1: Take a Playwright screenshot to verify no invisible text**

```bash
CHROME_PATH="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" node -e "
const { chromium } = require('./node_modules/playwright');
(async () => {
  const b = await chromium.launch({ executablePath: process.env.CHROME_PATH, headless: true, args: ['--no-sandbox'] });
  const p = await b.newPage();
  await p.setViewportSize({ width: 390, height: 844 });
  await p.goto('http://localhost:4200', { waitUntil: 'networkidle' });
  await p.screenshot({ path: '/tmp/light-theme.png', fullPage: true });
  await b.close();
  console.log('screenshot saved');
})();
" 2>&1 | tail -3
```

Open `/tmp/light-theme.png` and verify: white/light background, dark readable text, no invisible elements.

- [ ] **Step 2: Commit**

```bash
git add frontend/src/styles.css
git commit -m "feat: replace dark-navy theme with light blue/silver palette"
```
