# CR3: Digital Warranty + QR and Portfolio Map — Design Spec

**Date:** 2026-06-01
**Constraint:** Add only this feature. Do not touch unrelated code. No rebuild.

---

## Goal

Two trust-building features that share the `Job` data model:

1. **Digital Warranty + QR** — each completed installation gets a warranty card accessible via a secret URL (`/w/{token}`), with a scannable QR code on the printed PDF sticker.
2. **Public Portfolio Map** — a Leaflet map at `/portfolio` showing approximate pin locations (deliberately offset 150–300 m) of completed jobs, with before/after photos for jobs with photo consent.

---

## Architecture

```
[Admin: complete Won → Job]
  POST /api/admin/quote-requests/{id}/complete
  → creates Job (WarrantyNumber, PublicToken, ApproxLat/Lng, material snapshot)
  → stores photos via StorageService → R2

[Public: warranty page]
  GET /api/warranty/{token}        → warranty card (no PII)
  POST /api/warranty/{token}/service-request → creates ServiceRequest → LINE push

[Public: portfolio map]
  GET /api/portfolio               → ApproxLat/Lng pins + consented photos
  GET /api/portfolio/summary       → total count + breakdown by area
```

**Not touched:** `POST /api/estimate`, `PricingService`, all existing migrations.

---

## Data Models

### Job

```csharp
public class Job {
    public int Id { get; set; }
    public int QuoteRequestId { get; set; }
    public QuoteRequest QuoteRequest { get; set; } = null!;

    // Warranty identity
    public string WarrantyNumber { get; set; } = "";      // WR-{year}-{seq:D4}
    public string PublicToken { get; set; } = "";          // Guid.NewGuid().ToString("N") — 32 hex chars

    // Installation details
    public DateTime InstalledDate { get; set; }
    public int WarrantyMonths { get; set; } = 12;
    // WarrantyExpiry is derived: InstalledDate.AddMonths(WarrantyMonths)

    // Material snapshot (denormalised from QuoteRequest)
    public Material Material { get; set; }
    public int SizeInches { get; set; }
    public decimal LengthMeters { get; set; }
    public int DownspoutCount { get; set; }

    // Location — private
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    // Location — public (computed once on create, never recomputed)
    public double? ApproxLat { get; set; }
    public double? ApproxLng { get; set; }
    public string? AreaName { get; set; }

    // Portfolio flags
    public bool ShowInPortfolio { get; set; } = false;
    public bool PhotoConsent { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobPhoto> Photos { get; set; } = new List<JobPhoto>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
```

### JobPhoto

```csharp
public class JobPhoto {
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string Url { get; set; } = "";
    public PhotoType Type { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
}

public enum PhotoType { Before, After, Other }
```

### ServiceRequest

```csharp
public class ServiceRequest {
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string ContactPhone { get; set; } = "";
    public string? CustomerNote { get; set; }
    public ServiceRequestType Type { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ServiceRequestType { WarrantyClaim, Maintenance, Other }
public enum ServiceRequestStatus { New, Contacted, Done }
```

---

## ApproxLat/Lng Algorithm

Computed **once** at `POST /api/admin/quote-requests/{id}/complete`. Never recomputed.

```csharp
static (double approxLat, double approxLng) Jitter(double lat, double lng) {
    int metres = RandomNumberGenerator.GetInt32(150, 301); // 150–300 m inclusive
    double bearing = Random.Shared.NextDouble() * 2 * Math.PI;
    double dLat = (metres * Math.Cos(bearing)) / 111320.0;
    double dLng = (metres * Math.Sin(bearing)) / (111320.0 * Math.Cos(lat * Math.PI / 180));
    return (Math.Round(lat + dLat, 5), Math.Round(lng + dLng, 5));
}
```

If `QuoteRequest.MapCenterLat/Lng` exists → use as `Lat/Lng` and jitter for `ApproxLat/Lng`.
If not → admin manually enters `Lat/Lng` → same jitter.
If no coordinates at all → `ApproxLat/Lng` remain null (pin not shown on portfolio).

---

## WarrantyNumber Generation

Same pattern as `QuoteNumber` — serializable transaction, `SELECT MAX+1`:
```
WR-{year}-{nextSeq:D4}
```

---

## Storage Service

```csharp
public interface IStorageService {
    Task<string> UploadAsync(string fileName, Stream content, string contentType);
    Task DeleteAsync(string fileName);
}
```

Implementation uses `AWSSDK.S3` with `AmazonS3Config { ServiceURL = STORAGE_ENDPOINT, ForcePathStyle = true }`.

Returns `{STORAGE_PUBLIC_BASE_URL}/{fileName}` — stored as `JobPhoto.Url`.

**Env vars:**
```
STORAGE_ENDPOINT=https://<accountid>.r2.cloudflarestorage.com
STORAGE_BUCKET=raingutter-photos
STORAGE_ACCESS_KEY=...
STORAGE_SECRET=...
STORAGE_PUBLIC_BASE_URL=https://pub-xxx.r2.dev
```

---

## QR Code

`QRCoder` NuGet generates a `PngByteQRCode` of URL `{FRONTEND_PUBLIC_URL}/w/{token}`.

- `GET /api/admin/jobs/{id}/qr` → returns PNG bytes (`image/png`)
- Embedded in warranty PDF via QuestPDF as inline image

**Env var:**
```
FRONTEND_PUBLIC_URL=https://your-frontend.vercel.app
```

---

## Warranty PDF

QuestPDF (existing library, existing Thai fonts). A4 sticker/card layout:
- Shop logo + name
- WarrantyNumber, InstalledDate, WarrantyExpiry
- Material / size / length
- QR code (right column)
- Footer: shop phone + LINE OA

`GET /api/admin/jobs/{id}/warranty-pdf` → `application/pdf`

---

## API Endpoints

### Public (no auth — no PII ever returned)

| Method | Path | Returns |
|---|---|---|
| GET | `/api/warranty/{token}` | WarrantyNumber, InstalledDate, WarrantyExpiry, Material, SizeInches, LengthMeters, DownspoutCount, photos (all types), shop contact info |
| POST | `/api/warranty/{token}/service-request` | Creates ServiceRequest; pushes LINE to existing `LINE_OWNER_ID` |
| GET | `/api/portfolio` | Jobs where `ShowInPortfolio=true`: `ApproxLat`, `ApproxLng`, `AreaName`, Material, InstalledDate, photos where `PhotoConsent=true` |
| GET | `/api/portfolio/summary` | `{ total: N, byArea: [{name, count}] }` |

**Privacy gate (enforced in endpoint, not DTO):** No `Lat`, `Lng`, `CustomerName`, `Phone`, `Address` fields in any public response.

### Admin (JWT required)

| Method | Path | Description |
|---|---|---|
| POST | `/api/admin/quote-requests/{id}/complete` | Won → Job: InstalledDate, WarrantyMonths, Lat/Lng, AreaName; auto-gen WarrantyNumber + PublicToken + ApproxLat/Lng |
| GET | `/api/admin/jobs` | Paginated job list |
| GET | `/api/admin/jobs/{id}` | Full job detail (includes Lat/Lng) |
| PUT | `/api/admin/jobs/{id}` | Edit WarrantyMonths, InstalledDate, AreaName, Lat/Lng, ShowInPortfolio, PhotoConsent |
| POST | `/api/admin/jobs/{id}/photos` | Upload photo (multipart) → R2 → creates JobPhoto |
| DELETE | `/api/admin/jobs/{id}/photos/{photoId}` | Delete photo from R2 + DB |
| GET | `/api/admin/jobs/{id}/qr` | QR PNG |
| GET | `/api/admin/jobs/{id}/warranty-pdf` | Warranty PDF |
| GET | `/api/admin/service-requests` | All service requests (filterable by status) |
| PUT | `/api/admin/service-requests/{id}/status` | Update status (New→Contacted→Done) |

---

## Frontend

### New Routes

| Route | Component | Description |
|---|---|---|
| `/w/:token` | `WarrantyComponent` | Public warranty card |
| `/portfolio` | `PortfolioComponent` | Public portfolio map |
| `/admin/jobs` | `JobsListComponent` | Admin job list |
| `/admin/jobs/:id` | `JobDetailComponent` | Admin job detail |
| `/admin/service-requests` | `ServiceRequestsComponent` | Admin claims list |

### Warranty Page (`/w/:token`) — mobile-first

- Warranty number + expiry countdown ("เหลืออีก X วัน" / "หมดอายุแล้ว")
- Material / size / length
- Before/after photo gallery
- Shop contact: `<a href="tel:...">` + LINE OA button
- "แจ้งเคลม / ขอให้ช่างมาดู" → inline form (note + phone) → POST service-request → success message

### Portfolio Page (`/portfolio`) — desktop + mobile

- Leaflet map, Esri satellite tile (reuse `environment.satelliteTileUrl` + `maxNativeZoom: 18` from CR2)
- Pins at `ApproxLat/Lng` — click → popup with AreaName, Material, InstalledDate, consented photos
- Banner: "ติดตั้งแล้ว X จุดในพื้นที่ชลบุรี/ศรีราชา/พัทยา"
- No pins shown if `ApproxLat/Lng` is null

### Admin Job Detail

- "ปิดงาน" button on Won leads → form: InstalledDate, WarrantyMonths, Lat/Lng, AreaName → POST complete
- Photo upload section (Before / After / Other type, drag or file input)
- Toggle ShowInPortfolio + PhotoConsent
- Download QR PNG button + Download Warranty PDF button
- Service requests tab: list of claims for this job

---

## LINE Notification (Service Request)

Reuse existing `LineNotificationService` pattern (fire-and-forget, same `LINE_OWNER_ID`):

```
🔧 แจ้งเคลม/บริการ
📋 ใบรับประกัน: WR-2026-0001
📞 เบอร์ติดต่อ: 08x-xxx-xxxx
📝 หมายเหตุ: ...
🕐 {DateTime}
```

---

## Migration

Name: `AddJobWarrantyPortfolio`

Operations:
1. `CreateTable("jobs", ...)` — all Job columns
2. `CreateTable("job_photos", ...)` — JobPhoto columns
3. `CreateTable("service_requests", ...)` — ServiceRequest columns
4. Add FK constraints

Generate with: `dotnet ef migrations add AddJobWarrantyPortfolio --project src/RainGutter.Api`

---

## NuGet Packages Added

- `AWSSDK.S3` — S3-compatible storage (Cloudflare R2)
- `QRCoder` — QR PNG generation

---

## Environment Variables Added

```
STORAGE_ENDPOINT=https://<accountid>.r2.cloudflarestorage.com
STORAGE_BUCKET=raingutter-photos
STORAGE_ACCESS_KEY=...
STORAGE_SECRET=...
STORAGE_PUBLIC_BASE_URL=https://pub-xxx.r2.dev
FRONTEND_PUBLIC_URL=https://your-frontend.vercel.app
```

---

## Verification

```bash
# Backend
dotnet ef migrations add AddJobWarrantyPortfolio --project src/RainGutter.Api
dotnet ef database update --project src/RainGutter.Api
dotnet run --project src/RainGutter.Api

# Frontend
cd frontend && npm start
```

**Manual test flow:**
1. Admin: lead ที่ Won → "ปิดงาน" → กรอก InstalledDate + พิกัด → สร้าง Job
2. ดาวน์โหลด PDF → สแกน QR → เปิดหน้า `/w/{token}` บนมือถือ → เห็นข้อมูลถูก ไม่มีชื่อ/เบอร์ลูกค้า
3. กด "แจ้งเคลม" → ส่ง ServiceRequest → LINE push ถึง owner
4. Admin: อัปโหลดรูป + toggle PhotoConsent=true + ShowInPortfolio=true
5. เปิด `/portfolio` → เห็นหมุด → คลิก popup → รูปขึ้น
6. ตรวจสอบ: `/api/portfolio` response ไม่มี Lat/Lng จริง, ไม่มีชื่อลูกค้า
