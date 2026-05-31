# CR2: Map Measurement Feature — Design Spec

**Date:** 2026-05-31  
**Constraint:** Add only this feature. Do not touch unrelated code. No rebuild.

---

## Goal

Customers who don't know how many metres of gutter they need can draw a polyline along their roofline on a satellite map. The measured length pre-fills the "length in metres" field (still editable). The measurement data is stored with the quote request so the admin can review it.

---

## Architecture

Three independent units connected through `QuoteRequest`:

```
[MapMeasureModalComponent]          [POST /api/quote-requests]       [Admin lead-detail]
  Leaflet + Geoman draw polyline  →  6 new optional fields stored  →  Leaflet mini-map
  @turf/length real-time metres      NetTopologySuite recompute         render GeoJSON
  returns MapMeasureResult           log warning if >10% diff           show measuredLength
```

**Not touched:** `POST /api/estimate`, `PricingService`, all existing migrations, existing DTO fields.

---

## Frontend

### New Files

| File | Responsibility |
|---|---|
| `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.ts` | Standalone modal — Leaflet map, Geoman draw mode, Turf.js length |
| `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.html` | Map container, toolbar (locate / clear / apply), disclaimer |
| `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.css` | Map height, overlay, button layout |

### Modified Files

| File | Change |
|---|---|
| `calculator.component.ts` | Open modal signal; receive `MapMeasureResult`; patch `lengthMeters`; set `measureSource`; include map fields in quote payload |
| `calculator.component.html` | "📍 วัดจากแผนที่ (ไม่รู้กี่เมตร?)" button beside lengthMeters input |
| `core/models/index.ts` | Add `MeasureSource = 'Manual' \| 'Map'`, `MapMeasureResult` interface |
| `core/services/api.service.ts` | Update `CreateQuoteRequestPayload` type with 6 new optional fields |
| `environments/environment.ts` | Add `satelliteTileUrl`, `satelliteAttribution` |
| `environments/environment.prod.ts` | Same keys (same Esri defaults) |

### MapMeasureResult interface
```typescript
interface MapMeasureResult {
  geojson: GeoJSON.MultiLineString;   // all drawn lines combined
  measuredLengthMeters: number;       // total geodesic length, 1 decimal
  centerLat: number;
  centerLng: number;
  zoom: number;
}
```

### Modal Behaviour
- Opens as overlay (no router navigation)
- Default centre: Bangkok (`13.756, 100.502`, zoom 15) — overridden by geolocation if granted
- Tile: `environment.satelliteTileUrl` (default: Esri World Imagery)
- Attribution: `environment.satelliteAttribution` (required by Esri ToS)
- Draw mode: Geoman polyline only (`pm.addControls({ drawPolyline: true, ... all others false }`)
- Each vertex addition → recompute total via `@turf/length` → update live display
- Multiple lines supported — all merged into one MultiLineString before returning
- "ใช้ตำแหน่งปัจจุบัน": `navigator.geolocation.getCurrentPosition` → `map.setView`
- "ล้าง / วาดใหม่": remove all Geoman layers, reset length to 0
- "ใช้ค่านี้ (≈ X.X เมตร)": emit result, close modal — only enabled when totalLength > 0
- Disclaimer text (shown as map overlay): `"ค่าประมาณจากแผนที่ ยืนยันความยาวจริงหน้างาน"`
- On close without applying: no change to form

### Calculator Integration
```typescript
// After modal returns result:
this.calcForm.patchValue({ lengthMeters: result.measuredLengthMeters });
this.measureSource = 'Map';
this.mapMeasureResult = result;

// In quote request payload (contactForm submit):
measureSource: this.measureSource,
measuredLengthMeters: this.mapMeasureResult?.measuredLengthMeters ?? null,
measuredGeoJson: this.mapMeasureResult ? JSON.stringify(this.mapMeasureResult.geojson) : null,
mapCenterLat: this.mapMeasureResult?.centerLat ?? null,
mapCenterLng: this.mapMeasureResult?.centerLng ?? null,
mapZoom: this.mapMeasureResult?.zoom ?? null,
```

### npm Packages Added
- `leaflet` + `@types/leaflet` (includes `@types/geojson` transitively)
- `@geoman-io/leaflet-geoman-free`
- `@turf/length` + `@turf/helpers`
- `@types/geojson` (explicit, for `GeoJSON.*` types in component)

### Environment Config (default Esri — free, no API key)
```typescript
satelliteTileUrl: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
satelliteAttribution: 'Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community',
```

---

## Backend

### QuoteRequest.cs — 6 new fields
```csharp
public string MeasureSource { get; set; } = "Manual";   // "Manual" | "Map"
public decimal? MeasuredLengthMeters { get; set; }
public string? MeasuredGeoJson { get; set; }             // stored as jsonb
public double? MapCenterLat { get; set; }
public double? MapCenterLng { get; set; }
public int? MapZoom { get; set; }
```

### AppDbContext.cs
- `MeasuredGeoJson` column type: `.HasColumnType("jsonb")`

### CreateQuoteRequest DTO (QuoteRequestDto.cs)
Add 6 optional parameters to the record:
```csharp
string MeasureSource = "Manual",
decimal? MeasuredLengthMeters = null,
string? MeasuredGeoJson = null,
double? MapCenterLat = null,
double? MapCenterLng = null,
int? MapZoom = null
```

### QuoteEndpoints.cs — NetTopologySuite validation
After saving, if `MeasuredGeoJson != null`:
```csharp
try {
    var reader = new GeoJsonReader();
    var geom = reader.Read<Geometry>(req.MeasuredGeoJson);
    double serverMetres = geom.Length * (Math.PI / 180) * 6371000; // approximate
    double clientMetres = (double)(req.MeasuredLengthMeters ?? 0);
    if (clientMetres > 0 && Math.Abs(serverMetres - clientMetres) / clientMetres > 0.10)
        logger.LogWarning("Map length mismatch: client={Client}m server={Server}m quote={Quote}",
            clientMetres, Math.Round(serverMetres, 1), quote.QuoteNumber);
} catch (Exception ex) {
    logger.LogWarning(ex, "Could not parse MeasuredGeoJson for {Quote}", quote.QuoteNumber);
}
```

### AdminDtos.cs — QuoteRequestDetail
Add to response DTO:
```csharp
string MeasureSource,
decimal? MeasuredLengthMeters,
string? MeasuredGeoJson,
double? MapCenterLat,
double? MapCenterLng,
int? MapZoom
```

### Migration
Name: `AddMapMeasureFields`  
Command: `dotnet ef migrations add AddMapMeasureFields --project src/RainGutter.Api`

### NuGet Package Added
- `NetTopologySuite.IO.GeoJSON` (includes `GeoJsonReader`)

---

## Admin

### lead-detail.component.ts
- Import Leaflet dynamically (to avoid SSR issues if any)
- Add `initMiniMap()` method called after view init if `detail().measuredGeoJson != null`
- Fit bounds to polyline + show length label

### lead-detail.component.html
Add section after customer info card:
```html
@if (detail()?.measuredGeoJson) {
  <div class="card">
    <div class="card-label">วัดจากแผนที่</div>
    <div id="admin-mini-map" style="height:280px"></div>
    <p>ความยาวที่วัด: {{ detail()!.measuredLengthMeters | number:'1.1-1' }} เมตร</p>
  </div>
}
```

---

## Verification

**Run after implementation:**
```bash
cd backend
dotnet ef migrations add AddMapMeasureFields --project src/RainGutter.Api
dotnet ef database update --project src/RainGutter.Api

cd ../frontend
npm install
ng serve
```

**Manual test flow:**
1. Calculator → "📍 วัดจากแผนที่" → map opens
2. Draw polyline on roofline → real-time metres shown
3. "ใช้ค่านี้" → closes modal → lengthMeters field filled (editable)
4. Complete quote request form → submit
5. Admin → lead detail → mini-map shows polyline + length
6. Manual path: type metres directly → `measureSource = Manual`, map fields null

**Regression check:** Manual entry (no map) still works; `measureSource` defaults to `"Manual"`.
