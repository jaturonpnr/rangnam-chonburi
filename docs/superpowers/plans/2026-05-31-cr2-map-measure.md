# CR2: Map Measurement Feature — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let customers draw a polyline on a satellite map to measure gutter length, which pre-fills the metres field, and store the measurement data for admin review.

**Architecture:** Standalone `MapMeasureModalComponent` (Leaflet + Geoman + Turf) opens from calculator; result patches `lengthMeters` and is included in the quote payload; backend stores 6 new nullable fields and validates with NetTopologySuite; admin lead-detail renders a Leaflet mini-map from stored GeoJSON.

**Tech Stack:** Angular 18 signals, Leaflet 1.x, @geoman-io/leaflet-geoman-free, @turf/length, .NET 10, NetTopologySuite.IO.GeoJSON4STJ, EF Core PostgreSQL

---

### Task 1: Install npm packages + update angular.json + environment config

**Files:**
- Modify: `frontend/package.json` (via npm install)
- Modify: `frontend/angular.json` — add Leaflet + Geoman CSS to styles array
- Modify: `frontend/src/environments/environment.ts`
- Modify: `frontend/src/environments/environment.prod.ts`

- [ ] **Step 1: Install npm packages**

```bash
cd /Users/pack/Workspace/rangnam-chonburi/frontend
npm install leaflet @types/leaflet @geoman-io/leaflet-geoman-free @turf/length @turf/helpers @types/geojson --legacy-peer-deps
```

Expected: packages added to `node_modules/`, no errors.

- [ ] **Step 2: Add Leaflet and Geoman CSS to angular.json styles array**

In `frontend/angular.json`, find the `"styles"` array under `projects.frontend.architect.build.options`. Replace:
```json
"styles": [
  "src/styles.css"
]
```
With:
```json
"styles": [
  "node_modules/leaflet/dist/leaflet.css",
  "node_modules/@geoman-io/leaflet-geoman-free/dist/leaflet-geoman.css",
  "src/styles.css"
]
```
Also apply the same change to `projects.frontend.architect.test.options.styles` if it exists.

- [ ] **Step 3: Add satellite config to environment.ts**

Replace the contents of `frontend/src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5282',
  satelliteTileUrl: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
  satelliteAttribution: 'Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community'
};
```

- [ ] **Step 4: Add satellite config to environment.prod.ts**

Replace the contents of `frontend/src/environments/environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiBaseUrl: 'https://your-backend.onrender.com',
  satelliteTileUrl: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
  satelliteAttribution: 'Tiles &copy; Esri &mdash; Source: Esri, Maxar, Earthstar Geographics, and the GIS User Community'
};
```

- [ ] **Step 5: Commit**

```bash
git add frontend/package.json frontend/package-lock.json frontend/angular.json \
        frontend/src/environments/environment.ts frontend/src/environments/environment.prod.ts
git commit -m "chore: install leaflet/geoman/turf; add satellite tile config"
```

---

### Task 2: Add types to core/models/index.ts + update api.service.ts

**Files:**
- Modify: `frontend/src/app/core/models/index.ts`
- Modify: `frontend/src/app/core/services/api.service.ts`

- [ ] **Step 1: Add MeasureSource type, MapMeasureResult interface, and update QuoteRequestDetail**

In `frontend/src/app/core/models/index.ts`, add after the last `export`:

```typescript
export type MeasureSource = 'Manual' | 'Map';

export interface MapMeasureResult {
  geojson: GeoJSON.MultiLineString;
  measuredLengthMeters: number;
  centerLat: number;
  centerLng: number;
  zoom: number;
}
```

Also update the existing `QuoteRequestDetail` interface to add the 6 map fields at the end:

```typescript
export interface QuoteRequestDetail extends QuoteRequestSummary {
  address: string | null;
  locationDetail: string | null;
  serviceZoneName: string | null;
  buildingTypeLabel: string | null;
  material: Material;
  sizeInches: number;
  lengthMeters: number;
  downspoutCount: number;
  floors: number;
  removeOld: boolean;
  breakdownJson: string;
  measureSource: MeasureSource;
  measuredLengthMeters: number | null;
  measuredGeoJson: string | null;
  mapCenterLat: number | null;
  mapCenterLng: number | null;
  mapZoom: number | null;
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/app/core/models/index.ts
git commit -m "feat(models): add MeasureSource, MapMeasureResult, map fields to QuoteRequestDetail"
```

---

### Task 3: Create MapMeasureModalComponent

**Files:**
- Create: `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.ts`
- Create: `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.html`
- Create: `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.css`

- [ ] **Step 1: Create component TypeScript file**

Create `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.ts`:

```typescript
import { Component, Output, EventEmitter, AfterViewInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as L from 'leaflet';
import '@geoman-io/leaflet-geoman-free';
import length from '@turf/length';
import { multiLineString } from '@turf/helpers';
import { environment } from '../../../../environments/environment';
import { MapMeasureResult } from '../../../core/models';

@Component({
  selector: 'app-map-measure-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './map-measure-modal.component.html',
  styleUrls: ['./map-measure-modal.component.css']
})
export class MapMeasureModalComponent implements AfterViewInit, OnDestroy {
  @Output() applied = new EventEmitter<MapMeasureResult>();
  @Output() dismissed = new EventEmitter<void>();

  totalMeters = 0;
  private map!: L.Map;
  private drawnLayers: L.Polyline[] = [];

  ngAfterViewInit() {
    // Fix Leaflet default icon paths broken by Angular bundler
    const iconDefault = L.icon({
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      iconSize: [25, 41], iconAnchor: [12, 41]
    });
    L.Marker.prototype.options.icon = iconDefault;

    this.map = L.map('map-measure-container', { center: [13.756, 100.502], zoom: 15 });

    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20
    }).addTo(this.map);

    (this.map as any).pm.addControls({
      drawPolyline: true,
      drawMarker: false,
      drawCircleMarker: false,
      drawPolygon: false,
      drawCircle: false,
      drawRectangle: false,
      drawText: false,
      editMode: true,
      dragMode: false,
      cutPolygon: false,
      removalMode: true,
    });

    this.map.on('pm:create', (e: any) => {
      const layer = e.layer as L.Polyline;
      this.drawnLayers.push(layer);
      this.recalculate();
      layer.on('pm:edit', () => this.recalculate());
    });

    this.map.on('pm:remove', (e: any) => {
      this.drawnLayers = this.drawnLayers.filter(l => l !== e.layer);
      this.recalculate();
    });
  }

  ngOnDestroy() {
    this.map?.remove();
  }

  private recalculate() {
    if (this.drawnLayers.length === 0) { this.totalMeters = 0; return; }
    const coords = this.drawnLayers.map(l =>
      (l.getLatLngs() as L.LatLng[]).map(ll => [ll.lng, ll.lat] as [number, number])
    );
    const km = length(multiLineString(coords), { units: 'kilometers' });
    this.totalMeters = Math.round(km * 1000 * 10) / 10;
  }

  locateMe() {
    navigator.geolocation.getCurrentPosition(
      pos => this.map.setView([pos.coords.latitude, pos.coords.longitude], 18),
      () => {}
    );
  }

  clearAll() {
    (this.map as any).pm.getGeomanLayers().forEach((l: L.Layer) => l.remove());
    this.drawnLayers = [];
    this.totalMeters = 0;
  }

  apply() {
    const center = this.map.getCenter();
    const coords = this.drawnLayers.map(l =>
      (l.getLatLngs() as L.LatLng[]).map(ll => [ll.lng, ll.lat] as [number, number])
    );
    this.applied.emit({
      geojson: { type: 'MultiLineString', coordinates: coords },
      measuredLengthMeters: this.totalMeters,
      centerLat: center.lat,
      centerLng: center.lng,
      zoom: this.map.getZoom()
    });
  }
}
```

- [ ] **Step 2: Create component HTML**

Create `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.html`:

```html
<div class="modal-backdrop" (click)="dismissed.emit()">
  <div class="modal-box" (click)="$event.stopPropagation()">

    <div class="modal-header">
      <span class="modal-title">วัดความยาวรางจากแผนที่</span>
      <button class="modal-close" (click)="dismissed.emit()">✕</button>
    </div>

    <div class="map-wrapper">
      <div id="map-measure-container"></div>
      <div class="map-disclaimer">📍 ค่าประมาณจากแผนที่ ยืนยันความยาวจริงหน้างาน</div>
    </div>

    <div class="modal-footer">
      <div class="length-display">
        ความยาวรวม: <strong>{{ totalMeters | number:'1.1-1' }} เมตร</strong>
      </div>
      <div class="modal-actions">
        <button class="btn btn-secondary btn-sm" type="button" (click)="locateMe()">
          📍 ตำแหน่งปัจจุบัน
        </button>
        <button class="btn btn-secondary btn-sm" type="button" (click)="clearAll()">
          🗑 ล้าง / วาดใหม่
        </button>
        <button class="btn btn-primary" type="button"
                [disabled]="totalMeters === 0" (click)="apply()">
          ใช้ค่านี้ (≈ {{ totalMeters | number:'1.1-1' }} เมตร)
        </button>
      </div>
    </div>

  </div>
</div>
```

- [ ] **Step 3: Create component CSS**

Create `frontend/src/app/features/calculator/map-measure-modal/map-measure-modal.component.css`:

```css
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.55);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
}

.modal-box {
  background: var(--bg-surface);
  border: 1px solid var(--border);
  border-radius: var(--r);
  width: 100%;
  max-width: 720px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 20px 60px rgba(0,0,0,.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 20px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.modal-title {
  font-size: 16px;
  font-weight: 600;
  color: var(--text);
}

.modal-close {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  color: var(--text-2);
  padding: 2px 8px;
  line-height: 1;
}
.modal-close:hover { color: var(--text); }

.map-wrapper {
  position: relative;
  flex: 1;
  min-height: 360px;
}

#map-measure-container {
  width: 100%;
  height: 100%;
  min-height: 360px;
}

.map-disclaimer {
  position: absolute;
  bottom: 28px;
  left: 50%;
  transform: translateX(-50%);
  background: rgba(0, 0, 0, 0.72);
  color: #fff;
  padding: 5px 14px;
  border-radius: 20px;
  font-size: 12px;
  white-space: nowrap;
  pointer-events: none;
  z-index: 400;
}

.modal-footer {
  padding: 14px 20px;
  border-top: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex-shrink: 0;
}

.length-display {
  text-align: center;
  font-size: 15px;
  color: var(--text-2);
}

.length-display strong {
  color: var(--blue);
  font-size: 22px;
  font-weight: 700;
}

.modal-actions {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  justify-content: flex-end;
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/calculator/map-measure-modal/
git commit -m "feat(calculator): add MapMeasureModalComponent (Leaflet + Geoman + Turf)"
```

---

### Task 4: Integrate modal into CalculatorComponent

**Files:**
- Modify: `frontend/src/app/features/calculator/calculator.component.ts`
- Modify: `frontend/src/app/features/calculator/calculator.component.html`

- [ ] **Step 1: Update calculator.component.ts**

Replace the full content of `frontend/src/app/features/calculator/calculator.component.ts`:

```typescript
import { Component, OnInit, signal, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { BuildingType, ServiceZone, EstimateResult, ShopProfilePublic, MapMeasureResult } from '../../core/models';
import { MapMeasureModalComponent } from './map-measure-modal/map-measure-modal.component';

@Component({
  selector: 'app-calculator',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MapMeasureModalComponent],
  templateUrl: './calculator.component.html'
})
export class CalculatorComponent implements OnInit {
  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  buildingTypes = signal<BuildingType[]>([]);
  zones = signal<ServiceZone[]>([]);
  shopProfile = signal<ShopProfilePublic | null>(null);
  estimate = signal<EstimateResult | null>(null);
  loading = signal(false);
  submitLoading = signal(false);
  showContactForm = signal(false);
  showMapModal = signal(false);
  serverError = signal('');

  private measureSource: 'Manual' | 'Map' = 'Manual';
  private mapMeasureResult: MapMeasureResult | null = null;

  calcForm = this.fb.group({
    material: ['Galvanized', Validators.required],
    buildingTypeId: [null as number | null, Validators.required],
    lengthMeters: [null as number | null, [Validators.required, Validators.min(0.1)]],
    downspoutCount: [0, [Validators.required, Validators.min(0)]],
    floors: [1, [Validators.required, Validators.min(1)]],
    removeOld: [false],
    serviceZoneId: [null as number | null]
  });

  contactForm = this.fb.group({
    customerName: ['', [Validators.required, Validators.minLength(2)]],
    phone: ['', [Validators.required, Validators.pattern(/^\d{9,10}$/)]],
    address: [''],
    locationDetail: ['']
  });

  ngOnInit() {
    this.api.getBuildingTypes().subscribe(bt => this.buildingTypes.set(bt));
    this.api.getZones().subscribe(z => this.zones.set(z));
    this.api.getShopProfile().subscribe(s => this.shopProfile.set(s));
    this.calcForm.get('material')?.valueChanges.subscribe(() => { this.estimate.set(null); });
  }

  onLengthMetersInput() {
    // If user manually edits the field after using the map, revert to Manual
    if (this.measureSource === 'Map') {
      this.measureSource = 'Manual';
      this.mapMeasureResult = null;
    }
  }

  onMapApplied(result: MapMeasureResult) {
    this.mapMeasureResult = result;
    this.measureSource = 'Map';
    this.calcForm.patchValue({ lengthMeters: result.measuredLengthMeters });
    this.showMapModal.set(false);
  }

  private coerceForm() {
    const v = this.calcForm.value;
    return {
      ...v,
      buildingTypeId: v.buildingTypeId != null ? +v.buildingTypeId : null,
      lengthMeters: v.lengthMeters != null ? +v.lengthMeters : v.lengthMeters,
      downspoutCount: v.downspoutCount != null ? +v.downspoutCount : v.downspoutCount,
      floors: v.floors != null ? +v.floors : v.floors,
      serviceZoneId: v.serviceZoneId != null ? +v.serviceZoneId : null
    };
  }

  calculate() {
    if (this.calcForm.invalid) { this.calcForm.markAllAsTouched(); return; }
    this.loading.set(true);
    this.estimate.set(null);
    this.api.estimate(this.coerceForm()).subscribe({
      next: r => { this.estimate.set(r); this.loading.set(false); },
      error: e => { this.serverError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.loading.set(false); }
    });
  }

  submitQuote() {
    if (this.contactForm.invalid) { this.contactForm.markAllAsTouched(); return; }
    this.submitLoading.set(true);
    const body = {
      ...this.coerceForm(),
      ...this.contactForm.value,
      measureSource: this.measureSource,
      measuredLengthMeters: this.mapMeasureResult?.measuredLengthMeters ?? null,
      measuredGeoJson: this.mapMeasureResult ? JSON.stringify(this.mapMeasureResult.geojson) : null,
      mapCenterLat: this.mapMeasureResult?.centerLat ?? null,
      mapCenterLng: this.mapMeasureResult?.centerLng ?? null,
      mapZoom: this.mapMeasureResult?.zoom ?? null,
    };
    this.api.createQuoteRequest(body).subscribe({
      next: r => this.router.navigate(['/thank-you', r.quoteNumber], { state: { quoteRequestId: r.quoteRequestId } }),
      error: e => { this.serverError.set(e.error?.error ?? 'เกิดข้อผิดพลาด'); this.submitLoading.set(false); }
    });
  }

  formatNumber(n: number) { return n.toLocaleString('th-TH'); }
  formatPhone(phone: string): string {
    if (phone.length === 10) return `${phone.slice(0, 3)}-${phone.slice(3, 6)}-${phone.slice(6)}`;
    if (phone.length === 9) return `${phone.slice(0, 2)}-${phone.slice(2, 5)}-${phone.slice(5)}`;
    return phone;
  }
}
```

- [ ] **Step 2: Add map button to calculator.component.html**

In `frontend/src/app/features/calculator/calculator.component.html`, find the `lengthMeters` input block. It looks like:
```html
<div class="form-group">
  <label>ความยาวราง (เมตร)</label>
  ...input...
</div>
```

Replace it with:
```html
<div class="form-group">
  <label>ความยาวราง (เมตร)</label>
  <div class="input-with-map">
    <div class="input-unit-wrap">
      <input class="form-control" type="number" formControlName="lengthMeters"
             placeholder="เช่น 15" min="0.1" step="0.1"
             (input)="onLengthMetersInput()">
      <span class="input-unit">ม.</span>
    </div>
    <button type="button" class="btn btn-secondary btn-sm map-btn"
            (click)="showMapModal.set(true)">
      📍 วัดจากแผนที่
    </button>
  </div>
  @if (calcForm.get('lengthMeters')?.invalid && calcForm.get('lengthMeters')?.touched) {
    <span class="error-msg">กรุณาระบุความยาว</span>
  }
</div>
```

- [ ] **Step 3: Add modal to calculator.component.html**

At the very end of `calculator.component.html`, before the closing tag of the outermost element, add:

```html
@if (showMapModal()) {
  <app-map-measure-modal
    (applied)="onMapApplied($event)"
    (dismissed)="showMapModal.set(false)">
  </app-map-measure-modal>
}
```

- [ ] **Step 4: Add .input-with-map CSS to styles.css**

In `frontend/src/styles.css`, add after the `.input-unit-wrap` rule:

```css
.input-with-map { display: flex; gap: 8px; align-items: flex-start; }
.input-with-map .input-unit-wrap { flex: 1; }
.map-btn { white-space: nowrap; flex-shrink: 0; margin-top: 0; }
```

- [ ] **Step 5: Commit**

```bash
git add frontend/src/app/features/calculator/calculator.component.ts \
        frontend/src/app/features/calculator/calculator.component.html \
        frontend/src/styles.css
git commit -m "feat(calculator): integrate map measure modal; patch lengthMeters on apply"
```

---

### Task 5: Backend — QuoteRequest model + AppDbContext + NuGet

**Files:**
- Modify: `backend/src/RainGutter.Api/Models/QuoteRequest.cs`
- Modify: `backend/src/RainGutter.Api/Data/AppDbContext.cs`
- Modify: `backend/src/RainGutter.Api/RainGutter.Api.csproj` (via dotnet add package)

- [ ] **Step 1: Add 6 fields to QuoteRequest.cs**

In `backend/src/RainGutter.Api/Models/QuoteRequest.cs`, add after the `RemoveOld` property:

```csharp
    // Map measurement fields (optional — populated when customer uses map tool)
    public string MeasureSource { get; set; } = "Manual";
    public decimal? MeasuredLengthMeters { get; set; }
    public string? MeasuredGeoJson { get; set; }
    public double? MapCenterLat { get; set; }
    public double? MapCenterLng { get; set; }
    public int? MapZoom { get; set; }
```

- [ ] **Step 2: Configure MeasuredGeoJson as jsonb in AppDbContext.cs**

In `backend/src/RainGutter.Api/Data/AppDbContext.cs`, inside the `OnModelCreating` method, after the existing `.Property(q => q.BreakdownJson).HasColumnType("jsonb")` line, add:

```csharp
            modelBuilder.Entity<QuoteRequest>()
                .Property(q => q.MeasuredGeoJson)
                .HasColumnType("jsonb");
```

- [ ] **Step 3: Install NetTopologySuite.IO.GeoJSON4STJ NuGet**

```bash
cd /Users/pack/Workspace/rangnam-chonburi/backend
dotnet add src/RainGutter.Api/RainGutter.Api.csproj package NetTopologySuite.IO.GeoJSON4STJ
```

Expected: package added, project builds.

- [ ] **Step 4: Verify backend compiles**

```bash
cd /Users/pack/Workspace/rangnam-chonburi/backend
dotnet build src/RainGutter.Api/RainGutter.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/src/RainGutter.Api/Models/QuoteRequest.cs \
        backend/src/RainGutter.Api/Data/AppDbContext.cs \
        backend/src/RainGutter.Api/RainGutter.Api.csproj \
        backend/src/RainGutter.Api/packages.lock.json 2>/dev/null || true
git commit -m "feat(backend): add map measure fields to QuoteRequest; install NTS GeoJSON4STJ"
```

---

### Task 6: Backend — DTOs + QuoteEndpoints + AdminEndpoints

**Files:**
- Modify: `backend/src/RainGutter.Api/Dtos/QuoteRequestDto.cs`
- Modify: `backend/src/RainGutter.Api/Dtos/AdminDtos.cs`
- Modify: `backend/src/RainGutter.Api/Endpoints/QuoteEndpoints.cs`
- Modify: `backend/src/RainGutter.Api/Endpoints/AdminEndpoints.cs`

- [ ] **Step 1: Update CreateQuoteRequest DTO**

In `backend/src/RainGutter.Api/Dtos/QuoteRequestDto.cs`, replace `CreateQuoteRequest`:

```csharp
public record CreateQuoteRequest(
    Material Material,
    int BuildingTypeId,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    int? ServiceZoneId,
    string CustomerName,
    string Phone,
    string? Address,
    string? LocationDetail,
    string MeasureSource = "Manual",
    decimal? MeasuredLengthMeters = null,
    string? MeasuredGeoJson = null,
    double? MapCenterLat = null,
    double? MapCenterLng = null,
    int? MapZoom = null
);
```

- [ ] **Step 2: Update QuoteRequestDetail DTO in AdminDtos.cs**

In `backend/src/RainGutter.Api/Dtos/AdminDtos.cs`, replace the `QuoteRequestDetail` record:

```csharp
public record QuoteRequestDetail(
    int Id,
    string QuoteNumber,
    string CustomerName,
    string Phone,
    string? Address,
    string? LocationDetail,
    string? ServiceZoneName,
    string? BuildingTypeLabel,
    Material Material,
    int SizeInches,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    decimal EstimatedTotal,
    string BreakdownJson,
    QuoteStatus Status,
    DateTime CreatedAt,
    string MeasureSource,
    decimal? MeasuredLengthMeters,
    string? MeasuredGeoJson,
    double? MapCenterLat,
    double? MapCenterLng,
    int? MapZoom
);
```

- [ ] **Step 3: Update QuoteEndpoints.cs to map fields + add NTS validation**

Replace the full content of `backend/src/RainGutter.Api/Endpoints/QuoteEndpoints.cs`:

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Converters;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class QuoteEndpoints
{
    public static void MapQuoteEndpoints(this WebApplication app)
    {
        app.MapPost("/api/quote-requests", async (
            CreateQuoteRequest req,
            AppDbContext db,
            IPricingService pricing,
            ILineNotificationService line,
            ILogger<QuoteEndpoints> logger) =>
        {
            if (string.IsNullOrWhiteSpace(req.CustomerName))
                return Results.BadRequest(new { error = "กรุณาระบุชื่อลูกค้า" });
            if (!System.Text.RegularExpressions.Regex.IsMatch(req.Phone, @"^\d{9,10}$"))
                return Results.BadRequest(new { error = "เบอร์โทรต้องเป็นตัวเลข 9-10 หลัก" });
            if (req.LengthMeters <= 0)
                return Results.BadRequest(new { error = "จำนวนเมตรต้องมากกว่า 0" });

            var buildingType = await db.BuildingTypes.FirstOrDefaultAsync(b => b.Id == req.BuildingTypeId && b.IsActive);
            if (buildingType is null)
                return Results.BadRequest(new { error = "ไม่พบประเภทอาคารที่ระบุ" });

            var product = await db.GutterProducts.FirstOrDefaultAsync(p =>
                p.IsActive && p.Material == req.Material && p.SizeInches == buildingType.SizeInches);
            if (product is null)
                return Results.BadRequest(new { error = "ไม่พบสินค้าที่ตรงกับเงื่อนไข" });

            var config = await db.PricingConfigs.FirstOrDefaultAsync(c => c.Id == 1);
            if (config is null) return Results.StatusCode(500);

            var zone = req.ServiceZoneId.HasValue
                ? await db.ServiceZones.FirstOrDefaultAsync(z => z.Id == req.ServiceZoneId && z.IsActive)
                : null;

            var estimateReq = new EstimateRequest(req.LengthMeters, req.DownspoutCount, req.Floors, req.RemoveOld, req.ServiceZoneId);
            var estimate = pricing.Calculate(estimateReq, config, product, zone);

            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var year = DateTime.UtcNow.Year;
            var maxSeq = await db.QuoteRequests
                .Where(q => q.QuoteNumber.StartsWith($"QT-{year}-"))
                .Select(q => q.QuoteNumber)
                .ToListAsync();
            var nextSeq = maxSeq.Count > 0
                ? maxSeq.Select(n => int.TryParse(n.Split('-').LastOrDefault(), out var v) ? v : 0).Max() + 1
                : 1;
            var quoteNumber = $"QT-{year}-{nextSeq:D4}";

            var lead = new Lead
            {
                CustomerName = req.CustomerName,
                Phone = req.Phone,
                Address = req.Address,
                LocationDetail = req.LocationDetail,
                ServiceZoneId = req.ServiceZoneId
            };
            db.Leads.Add(lead);
            await db.SaveChangesAsync();

            var quoteRequest = new QuoteRequest
            {
                QuoteNumber = quoteNumber,
                LeadId = lead.Id,
                Material = req.Material,
                SizeInches = buildingType.SizeInches,
                BuildingTypeId = buildingType.Id,
                BuildingTypeLabelSnapshot = buildingType.Label,
                LengthMeters = req.LengthMeters,
                DownspoutCount = req.DownspoutCount,
                Floors = req.Floors,
                RemoveOld = req.RemoveOld,
                EstimatedTotal = estimate.Total,
                BreakdownJson = JsonSerializer.Serialize(estimate.Breakdown),
                MeasureSource = req.MeasureSource,
                MeasuredLengthMeters = req.MeasuredLengthMeters,
                MeasuredGeoJson = req.MeasuredGeoJson,
                MapCenterLat = req.MapCenterLat,
                MapCenterLng = req.MapCenterLng,
                MapZoom = req.MapZoom
            };
            db.QuoteRequests.Add(quoteRequest);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            // NetTopologySuite server-side length validation (non-blocking warning)
            if (!string.IsNullOrEmpty(req.MeasuredGeoJson) && req.MeasuredLengthMeters > 0)
            {
                try
                {
                    var ntsOpts = new JsonSerializerOptions();
                    ntsOpts.Converters.Add(new GeoJsonConverterFactory());
                    var geom = JsonSerializer.Deserialize<Geometry>(req.MeasuredGeoJson, ntsOpts);
                    if (geom != null)
                    {
                        double serverMetres = ComputeGeodesicMetres(geom);
                        double clientMetres = (double)req.MeasuredLengthMeters.Value;
                        double diff = Math.Abs(serverMetres - clientMetres) / clientMetres;
                        if (diff > 0.10)
                            logger.LogWarning(
                                "Map length mismatch: client={Client}m server={Server}m diff={Diff:P0} quote={Quote}",
                                Math.Round(clientMetres, 1), Math.Round(serverMetres, 1), diff, quoteNumber);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not validate MeasuredGeoJson for {Quote}", quoteNumber);
                }
            }

            _ = line.SendNewLeadNotificationAsync(quoteRequest, lead);
            return Results.Ok(new CreateQuoteResponse(quoteNumber, quoteRequest.Id));
        });

        app.MapGet("/api/quote-requests/{id:int}/pdf", async (
            int id, AppDbContext db, IPdfService pdf) =>
        {
            var quote = await db.QuoteRequests
                .Include(q => q.Lead)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (quote is null) return Results.NotFound();

            var shop = await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1);
            if (shop is null) return Results.StatusCode(500);

            var pdfBytes = pdf.GenerateQuotePdf(quote, quote.Lead, shop);
            return Results.File(pdfBytes, "application/pdf", $"{quote.QuoteNumber}.pdf");
        });
    }

    private static double ComputeGeodesicMetres(Geometry geom)
    {
        double total = 0;
        if (geom is MultiLineString mls)
            foreach (var g in mls.Geometries)
                total += SumSegments(g.Coordinates);
        else
            total = SumSegments(geom.Coordinates);
        return total;
    }

    private static double SumSegments(Coordinate[] coords)
    {
        double d = 0;
        for (int i = 0; i < coords.Length - 1; i++)
            d += Haversine(coords[i].Y, coords[i].X, coords[i + 1].Y, coords[i + 1].X);
        return d;
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
```

- [ ] **Step 4: Update AdminEndpoints.cs QuoteRequestDetail mapping**

In `backend/src/RainGutter.Api/Endpoints/AdminEndpoints.cs`, find the `GET /api/admin/quote-requests/{id:int}` handler that constructs a `QuoteRequestDetail`. It currently ends with `q.CreatedAt`. Update it to include the 6 new fields.

Find the `new QuoteRequestDetail(` construction and add after `q.CreatedAt`:
```csharp
                    q.MeasureSource,
                    q.MeasuredLengthMeters,
                    q.MeasuredGeoJson,
                    q.MapCenterLat,
                    q.MapCenterLng,
                    q.MapZoom
```

- [ ] **Step 5: Verify backend compiles and tests pass**

```bash
cd /Users/pack/Workspace/rangnam-chonburi/backend
dotnet build src/RainGutter.Api/RainGutter.Api.csproj
dotnet test tests/RainGutter.Tests/RainGutter.Tests.csproj
```

Expected: `Build succeeded.` and all 13 tests pass.

- [ ] **Step 6: Commit**

```bash
git add backend/src/RainGutter.Api/Dtos/ \
        backend/src/RainGutter.Api/Endpoints/QuoteEndpoints.cs \
        backend/src/RainGutter.Api/Endpoints/AdminEndpoints.cs
git commit -m "feat(backend): update DTOs + QuoteEndpoints map fields + NTS validation"
```

---

### Task 7: EF Migration

**Files:**
- Create: `backend/src/RainGutter.Api/Migrations/<timestamp>_AddMapMeasureFields.cs` (generated)
- Modify: `backend/src/RainGutter.Api/Migrations/AppDbContextModelSnapshot.cs` (generated)

- [ ] **Step 1: Generate migration**

```bash
cd /Users/pack/Workspace/rangnam-chonburi/backend
dotnet ef migrations add AddMapMeasureFields --project src/RainGutter.Api
```

Expected: two new files created in `src/RainGutter.Api/Migrations/`.

- [ ] **Step 2: Apply migration**

```bash
dotnet ef database update --project src/RainGutter.Api
```

Expected: `Done.` — migration applied to local PostgreSQL.

- [ ] **Step 3: Verify migration adds correct columns**

```bash
dotnet ef migrations script --project src/RainGutter.Api --idempotent 2>/dev/null | grep -A3 "measure_source\|measured_length\|measured_geo_json\|map_center\|map_zoom"
```

Expected: shows `ALTER TABLE "quote_requests" ADD COLUMN "measure_source"` etc.

- [ ] **Step 4: Commit**

```bash
git add backend/src/RainGutter.Api/Migrations/
git commit -m "feat(db): add AddMapMeasureFields migration"
```

---

### Task 8: Admin lead-detail mini-map

**Files:**
- Modify: `frontend/src/app/features/admin/leads/lead-detail.component.ts`
- Modify: `frontend/src/app/features/admin/leads/lead-detail.component.html`

- [ ] **Step 1: Update lead-detail.component.ts**

Replace the full content of `frontend/src/app/features/admin/leads/lead-detail.component.ts`:

```typescript
import { Component, OnInit, OnDestroy, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { Router } from '@angular/router';
import { QuoteRequestDetail, BreakdownItem } from '../../../core/models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-lead-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './lead-detail.component.html'
})
export class LeadDetailComponent implements OnInit, OnDestroy {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  detail = signal<QuoteRequestDetail | null>(null);
  breakdown = signal<BreakdownItem[]>([]);
  statusUpdating = signal(false);
  selectedStatus = '';

  statuses = ['New', 'Contacted', 'Quoted', 'Won', 'Lost'];
  statusLabels: Record<string, string> = { New: 'ใหม่', Contacted: 'ติดต่อแล้ว', Quoted: 'ส่งใบเสนอราคา', Won: 'ปิดงานได้', Lost: 'ไม่สำเร็จ' };

  private miniMap: any = null;

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.api.getQuoteRequestDetail(id).subscribe(d => {
      this.detail.set(d);
      this.selectedStatus = d.status;
      try { this.breakdown.set(JSON.parse(d.breakdownJson)); } catch { }
      if (d.measuredGeoJson) {
        setTimeout(() => this.initMiniMap(d), 150);
      }
    });
  }

  ngOnDestroy() {
    this.miniMap?.remove();
  }

  private async initMiniMap(d: QuoteRequestDetail) {
    const L = await import('leaflet');
    const el = document.getElementById('admin-mini-map');
    if (!el || this.miniMap) return;

    const map = L.map(el, { zoomControl: true });
    this.miniMap = map;

    L.tileLayer(environment.satelliteTileUrl, {
      attribution: environment.satelliteAttribution,
      maxZoom: 20
    }).addTo(map);

    try {
      const geojson = JSON.parse(d.measuredGeoJson!);
      const layer = L.geoJSON(geojson, { style: { color: '#0284C7', weight: 3, opacity: 0.9 } }).addTo(map);
      map.fitBounds(layer.getBounds(), { padding: [24, 24] });
    } catch {
      if (d.mapCenterLat && d.mapCenterLng) {
        map.setView([d.mapCenterLat, d.mapCenterLng], d.mapZoom ?? 16);
      }
    }
  }

  updateStatus() {
    if (!this.detail()) return;
    this.statusUpdating.set(true);
    this.api.updateQuoteStatus(this.detail()!.id, this.selectedStatus).subscribe({
      next: () => { this.detail.update(d => d ? { ...d, status: this.selectedStatus as any } : d); this.statusUpdating.set(false); },
      error: () => this.statusUpdating.set(false)
    });
  }

  pdfUrl() { return this.api.getAdminQuotePdfUrl(this.detail()!.id); }
  formatNumber(n: number | undefined) { return n != null ? n.toLocaleString('th-TH') : '-'; }
  formatDate(d: string) { return new Date(d).toLocaleString('th-TH'); }
  materialLabel(m: string) { return m === 'Galvanized' ? 'สังกะสี' : 'สแตนเลส'; }
  copyPhone() { navigator.clipboard.writeText(this.detail()!.phone); }
  logout() { this.auth.logout(); this.router.navigate(['/admin/login']); }
}
```

- [ ] **Step 2: Add mini-map section to lead-detail.component.html**

In `frontend/src/app/features/admin/leads/lead-detail.component.html`, after the existing customer info card (the first `.card` block), add:

```html
@if (detail()?.measuredGeoJson) {
  <div class="card">
    <div class="sec-label">วัดจากแผนที่</div>
    <div class="kv-row" style="margin-bottom: 10px;">
      <span class="kv-key">ความยาวที่วัด</span>
      <span class="kv-val">
        <strong>{{ detail()!.measuredLengthMeters | number:'1.1-1' }} เมตร</strong>
        <span class="badge badge-new" style="margin-left: 8px;">Map</span>
      </span>
    </div>
    <div id="admin-mini-map" style="height: 280px; border-radius: 4px; border: 1px solid var(--border);"></div>
  </div>
}
```

- [ ] **Step 3: Take a screenshot to verify mini-map renders**

Start the backend and frontend servers, open a lead that has `measuredGeoJson`, then screenshot. If no such lead exists yet, run a quick smoke test:

```bash
CHROME_PATH="/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" node -e "
const { chromium } = require('./node_modules/playwright');
(async () => {
  const b = await chromium.launch({ executablePath: process.env.CHROME_PATH, headless: true, args: ['--no-sandbox'] });
  const p = await b.newPage();
  await p.setViewportSize({ width: 390, height: 844 });
  await p.goto('http://localhost:4200', { waitUntil: 'networkidle' });
  await p.waitForSelector('button.map-btn', { timeout: 5000 });
  await p.screenshot({ path: '/tmp/cr2-map-btn.png' });
  await b.close();
  console.log('Map button visible');
})();
" 2>&1 | tail -3
```

Expected: `Map button visible`, screenshot shows 📍 button beside metres field.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/app/features/admin/leads/lead-detail.component.ts \
        frontend/src/app/features/admin/leads/lead-detail.component.html
git commit -m "feat(admin): show Leaflet mini-map of customer's drawn polyline in lead detail"
```

---

## Summary

**npm packages added (frontend):**
- `leaflet`, `@types/leaflet` — map rendering
- `@geoman-io/leaflet-geoman-free` — polyline drawing toolbar
- `@turf/length`, `@turf/helpers` — geodesic length calculation
- `@types/geojson` — GeoJSON TypeScript types

**NuGet packages added (backend):**
- `NetTopologySuite.IO.GeoJSON4STJ` — parse GeoJSON + geodesic validation

**Migration to run after pulling:**
```bash
cd backend
dotnet ef database update --project src/RainGutter.Api
```

**Tile provider:** Esri World Imagery — free, no API key required. To switch to Google/Mapbox: update `satelliteTileUrl` in environment files only.
