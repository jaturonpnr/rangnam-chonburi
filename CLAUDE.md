# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Monorepo for a rain gutter installation business. Two sub-projects:
- `backend/` — .NET 10 ASP.NET Core Minimal API (PostgreSQL via EF Core)
- `frontend/` — Angular 18 standalone components (signals-first)

---

## Common Commands

### Backend

```bash
cd backend/src/RainGutter.Api

# Run dev server (reads .env in working directory)
dotnet run

# Run all unit tests
cd ../../tests/RainGutter.Tests && dotnet test

# Run a single test by name
dotnet test --filter "FullyQualifiedName~SpecExample"

# Add EF migration
dotnet ef migrations add <MigrationName> --project src/RainGutter.Api

# Apply migrations manually
dotnet ef database update --project src/RainGutter.Api
```

Migrations run automatically at startup via `SeedData.SeedAsync`. The `.env` file (at `backend/src/RainGutter.Api/.env`) is loaded by Program.cs on startup — copy from `backend/.env.example`.

Local PostgreSQL via Docker:
```bash
docker run -d --name raingutter-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine
```

### Frontend

```bash
cd frontend

npm install
npm start          # ng serve → http://localhost:4200
npm run build      # production build

# Run Playwright UI tests (requires backend on :5000 and frontend on :4200)
CHROME_PATH=$(which chromium || which google-chrome) node test-ui.mjs
```

---

## Architecture

### Backend

**Entry point:** `Program.cs` — bootstraps QuestPDF Community license, registers Sarabun fonts, wires DI, runs migrations/seed, maps all endpoints.

**Endpoint groups** (each in `Endpoints/`):
| File | Routes |
|---|---|
| `PublicEndpoints.cs` | `GET /api/products`, `/api/zones`, `/api/shop-profile`, `POST /api/estimate` |
| `QuoteEndpoints.cs` | `POST /api/quote-requests`, `GET /api/quote-requests/{id}/pdf` |
| `AdminEndpoints.cs` | All `/api/admin/*` routes (require JWT) |
| `LineEndpoints.cs` | `POST /api/line/webhook` (HMAC-SHA256 verified) |

**Pricing logic** lives entirely in `Services/PricingService.cs`. Formula:
```
effectiveMeters = max(lengthMeters, minimumMeters)   // 10m minimum
baseGutter      = effectiveMeters × pricePerMeter
heightFee       = floors > 2 ? baseGutter × heightSurcharge% : 0
removalFee      = removeOld ? lengthMeters × removalPricePerMeter : 0  // uses actual, not effective
total           = baseGutter + heightFee + downspoutFee + removalFee + travelFee
```
The backend is the **authoritative** price calculator — `POST /api/estimate` does not write to the DB; `POST /api/quote-requests` re-runs the calculation server-side before persisting.

**DB conventions:**
- `UseSnakeCaseNamingConvention()` — all columns are snake_case in PostgreSQL
- Enums stored as strings via `.HasConversion<string>()` in `AppDbContext`
- All enums also serialized as strings in JSON via `JsonStringEnumConverter` in `ConfigureHttpJsonOptions`
- `PricingConfig` and `ShopProfile` are singleton rows (Id=1)
- `QuoteRequest.BreakdownJson` is `jsonb` column
- `QuoteNumber` format: `QT-{year}-{zero-padded-seq}`, generated with `SELECT MAX+1` inside a serializable transaction

**LINE notification** is optional and fire-and-forget. If `LINE_CHANNEL_ACCESS_TOKEN` or `LINE_OWNER_ID` env vars are missing, `LineNotificationService` logs a warning and returns without throwing.

**PDF** uses QuestPDF Community (free). Thai font (Sarabun) TTFs in `Assets/Fonts/` are copied to output dir via `.csproj`. Register via `QuestPDF.Drawing.FontManager.RegisterFont()` (not `QuestPDF.Infrastructure.FontManager`).

### Frontend

**Signal + Reactive Forms integration pattern** (critical, non-obvious):
`computed()` only tracks Angular signals — it cannot react to `FormControl.value` changes directly. The pattern used throughout is:
```typescript
private formValue = toSignal(this.calcForm.valueChanges, { initialValue: this.calcForm.value });
availableSizes = computed(() => { const mat = this.formValue().material; ... });
```
This bridges RxJS observables into signals. `toSignal` comes from `@angular/core/rxjs-interop`.

**Form value coercion:** `<select>` DOM values are always strings. Use `+(v.sizeInches ?? 0)` to coerce before comparing with numeric product fields (e.g., `p.sizeInches === size` needs both sides to be numbers).

**Charts:** Uses `ng2-charts` v10 — import `BaseChartDirective` (not `NgChartsModule`) and add `provideCharts(withDefaultRegisterables())` to `app.config.ts`.

**Auth:** JWT stored in `sessionStorage`. `auth.interceptor.ts` (functional `HttpInterceptorFn`) appends `Authorization: Bearer` header. `auth.guard.ts` redirects unauthenticated requests to `/admin/login`.

**API base URL:** `src/environments/environment.ts` → `http://localhost:5000` (dev), `environment.prod.ts` → update with actual Render.com URL before deploying.

---

## Environment Variables

```
ConnectionStrings__Default=Host=localhost;Database=raingutter;Username=postgres;Password=postgres
JWT_SECRET=<32+ char secret>
ADMIN_USERNAME=admin
ADMIN_PASSWORD=<password>
LINE_CHANNEL_ACCESS_TOKEN=   # optional
LINE_CHANNEL_SECRET=         # optional
LINE_OWNER_ID=               # optional — LINE userId or groupId for push messages
AllowedOrigins=http://localhost:4200  # comma-separated for prod
```

---

## Deployment

- **Backend:** `backend/Dockerfile` (multi-stage .NET 10), `backend/render.yaml` for Render.com. Migrations run at startup.
- **Frontend:** `frontend/vercel.json` rewrites all non-asset paths to `index.html` for SPA routing. Run `vercel deploy` from `frontend/`.
- Update `frontend/src/environments/environment.prod.ts` with the actual backend URL before deploying frontend.
