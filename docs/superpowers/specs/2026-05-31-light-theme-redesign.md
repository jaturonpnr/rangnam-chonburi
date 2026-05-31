# Light Theme Redesign — ส.จาตุรนต์ รางน้ำ

**Date:** 2026-05-31  
**Scope:** `frontend/src/styles.css` only — CSS variable remap + hardcoded color fixes  
**Approach:** B — full replacement, no dark/light toggle

---

## Goal

Replace the current dark-navy theme with a light blue/silver palette appropriate for a B2C rain gutter installation business. All changes are in a single file (`styles.css`).

---

## New Palette

| Variable | Old | New |
|---|---|---|
| `--bg-base` | `#07101c` | `#EBF4FF` |
| `--bg-panel` | `#0b1829` | `#F0F7FF` |
| `--bg-surface` | `#0f1f32` | `#FFFFFF` |
| `--bg-input` | `#0c1a2e` | `#F4F8FC` |
| `--border` | `#1a3050` | `#C9D8E8` |
| `--border-hi` | `#2d4f72` | `#7BAFD4` |
| `--blue` | `#38bdf8` | `#0284C7` |
| `--amber` | `#f59e0b` | `#D97706` |
| `--teal` | `#2dd4bf` | `#0891B2` |
| `--green` | `#34d399` | `#059669` |
| `--red` | `#f87171` | `#DC2626` |
| `--text` | `#c8d8e8` | `#0D2137` |
| `--text-2` | `#607d8b` | `#4B6A85` |
| `--text-dim` | `#2d4459` | `#94A3B8` |

---

## Hardcoded Color Changes

### Blueprint grid overlay (body::before)
- `rgba(56,189,248,.03)` → `rgba(2,132,199,.04)` — subtle sky-blue grid on light bg

### Button text on colored backgrounds
- `color: #07101c` (on btn-primary, btn-calculate, btn-danger, btn-success, pagination active) → **keep** — dark text on saturated button is correct for contrast

### Disabled button states
- `#122a42` (btn-primary disabled bg) → `#C8DCF0`
- `#20160000` (btn-calculate disabled bg) → `rgba(217,119,6,.08)`
- `#4a3010` (btn-calculate disabled text) → `#92530A`

### Blue rgba values — change RGB from `56,189,248` → `2,132,199`
All occurrences: focus rings, hover states, mat-tile glow, badge-new, table row hover, search borders

### Border rgba values
- `rgba(26,48,80,.6)` (table border) → `rgba(201,216,232,.8)`
- `rgba(26,48,80,.5)` (tab border) → `rgba(201,216,232,.7)`

### Badge inactive
- `rgba(48,68,80,.4)` → `rgba(148,163,184,.15)` — readable silver on light bg

### Amber / green / red rgba — **keep same RGB**, only opacity may change where needed for contrast

---

## Out of Scope

- Component `.ts` / `.html` files — no changes
- Admin component CSS — not affected (admin uses same global styles.css)
- `environment.prod.ts` — separate concern

---

## Verification

After applying: run `CHROME_PATH=... node frontend/test-ui.mjs` — all 11 playwright steps must still pass (layout/functional, not color).  
Manual check: calculator page, admin dashboard, lead detail, pricing page all readable with no invisible text (light-on-light).
