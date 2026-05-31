# Prompt สำหรับ Claude Code — ระบบประเมินราคาติดตั้งรางน้ำฝน "ส.จาตุรนต์ รางน้ำ"


---

## 0. บทบาทและวิธีทำงาน

คุณคือ senior full-stack engineer ช่วยผมสร้างระบบ **เครื่องคำนวณ/ประเมินราคาติดตั้งรางน้ำฝน** สำหรับร้าน "ส.จาตุรนต์ รางน้ำ" (ลูกค้าทั่วไป) พร้อมระบบหลังบ้านสำหรับเจ้าของร้าน

ขอให้:
- ทำเป็น **monorepo** โครงสร้าง `/backend` (.NET) และ `/frontend` (Angular)
- ทำทีละ milestone ตามหัวข้อ "12. ลำดับการ build" จบ milestone ไหนให้สรุปสั้น ๆ ก่อนไปต่อ
- เขียน **unit test** สำหรับ business logic การคำนวณราคา (สำคัญสุด ห้ามพลาด)
- ถ้ามีจุดต้องตัดสินใจ (assumption) ให้เลือก default ที่สมเหตุสมผล แล้วเขียน comment/note บอกไว้ ไม่ต้องหยุดถาม เว้นแต่เป็นเรื่อง breaking
- เขียน `README.md` วิธี run dev + วิธี deploy + รายการ env vars ทั้งหมด

---

## 1. Tech Stack (บังคับ)

| Layer | Technology |
|---|---|
| Frontend | Angular (standalone components, signals, reactive forms) |
| Backend | .NET 9, ASP.NET Core Minimal API |
| Database | PostgreSQL + Entity Framework Core |
| PDF | QuestPDF (community license — ฟรีสำหรับรายได้ < $1M/ปี) |
| Messaging | LINE Messaging API (LINE Official Account) |
| Deploy | Render.com (backend) + Vercel (frontend) |

---

## 2. ภาพรวมธุรกิจ (Domain Context)

ร้าน "ส.จาตุรนต์ รางน้ำ" รับติดตั้งรางน้ำฝน คิดราคา **ต่อเมตร** ตามวัสดุและขนาดราง use case หลัก: ลูกค้าเข้าหน้าเว็บ → กรอกความต้องการ → ระบบประเมินราคาเบื้องต้น → ลูกค้ากดขอใบเสนอราคา (ฝั่งร้านเก็บเบอร์ + ได้แจ้งเตือนทาง LINE)

**สำคัญ 2 ข้อ:**
1. ราคาที่แสดงเป็น "ราคาประเมินเบื้องต้น" เท่านั้น ราคาจริงยืนยันหลังสำรวจหน้างาน ต้องแสดง disclaimer นี้ชัดเจนทุกครั้ง
2. **ร้านยังไม่ได้จดทะเบียน VAT** เอกสารที่ออกคือ **"ใบเสนอราคา" ทั่วไป ห้ามมีบรรทัด VAT และห้ามเรียกว่าใบกำกับภาษี** ใน PDF ใส่หมายเหตุ "เอกสารนี้เป็นใบเสนอราคา ไม่ใช่ใบกำกับภาษี"

---

## 3. Business Rules การคำนวณราคา

> ค่าทั้งหมดต้องเก็บใน config/ตารางที่แก้ไขได้ผ่านหน้า admin ห้าม hardcode ในโค้ดคำนวณ

### 3.1 ขนาดรางมาจากประเภทอาคาร (ลูกค้าไม่ต้องรู้ขนาดเอง)
ลูกค้า **ไม่ต้องเลือกขนาดราง** เพราะส่วนใหญ่ไม่รู้ ให้เลือก **"ประเภทสถานที่ติดตั้ง"** แทน แล้วระบบ map เป็นขนาดราง (นิ้ว) อัตโนมัติผ่านตาราง `BuildingType` ที่แก้ได้ในหน้า admin

Mapping เริ่มต้น (seed):
| ประเภทสถานที่ติดตั้ง | ขนาดราง |
|---|---|
| ทาวน์โฮม / บ้านแฝด | 6 นิ้ว |
| บ้านเดี่ยว / ร้านค้า | 8 นิ้ว |

> หมายเหตุอ้างอิง (สำหรับ dev): หลักมาตรฐานจริงขนาดรางขึ้นกับความยาว/พื้นที่รับน้ำของหลังคา (4"≤5ม., 5"=5–15ม., 6">15ม. หรืออาคารพาณิชย์/รับน้ำมาก) แต่ร้านใช้ mapping ตามประเภทอาคารด้านบนเป็น business rule — จึงทำให้ตารางนี้ "แก้ได้ในหน้า admin"

### 3.2 สูตรคำนวณ
1. **ราคาฐาน** = `pricePerMeter (ตามวัสดุ+ขนาดที่ map จากประเภทอาคาร)` × `จำนวนเมตร`
2. **ขั้นต่ำ 10 เมตร** — ถ้าใส่ < `minimumMeters` (default = 10) คิดเสมือน 10 เมตร และแจ้งใน breakdown ว่า "คิดราคาขั้นต่ำ 10 เมตร"
3. **ท่อน้ำลง (downspout)** — `จำนวนจุด` × `downspoutPricePerPoint` (default seed = 500 บาท/จุด)
4. **ค่าความสูงอาคาร** — ราคามาตรฐานครอบคลุมไม่เกิน 2 ชั้น / สูงไม่เกิน 8 เมตร ถ้า `floors > 2` บวก surcharge แบบ % บนค่าราง (default `heightSurchargePercent` = 20%)
5. **รื้อถอนของเดิม** — ถ้าเลือก บวก `removalPricePerMeter` × เมตร (default seed = 60 บาท/เมตร)
6. **ค่าเดินทาง** — ตาม service zone แต่ละ zone มี `travelSurcharge` (default = 0)
7. **ค่าสำรวจหน้างาน** — แสดงเป็นหมายเหตุ (default `surveyFee` = 1,000 บาท หักเป็นส่วนลดได้) ยังไม่บวกในยอดประเมิน

```
baseGutter   = max(length, minimumMeters) × pricePerMeter
heightFee    = floors > 2 ? baseGutter × heightSurchargePercent/100 : 0
downspoutFee = downspoutCount × downspoutPricePerPoint
removalFee   = removeOld ? length × removalPricePerMeter : 0
travelFee    = zone.travelSurcharge
TOTAL        = baseGutter + heightFee + downspoutFee + removalFee + travelFee
```

> ⚠️ การคำนวณราคาต้องทำที่ **backend เท่านั้น** (authoritative) รวมถึงการ resolve ประเภทอาคาร → ขนาด → ราคา frontend แค่เก็บ input แล้วเรียก API

---

## 4. ราคา Seed (ราคาตลาดอ้างอิง — เจ้าของแก้เป็นราคาจริงผ่านหน้า admin ได้)

> **ไม่มีเรื่องผิวราง (finish) แล้ว** — ตัดทิ้ง ไม่มีผลต่อราคา

### รางสังกะสี (Galvanized) — บาท/เมตร พร้อมติดตั้ง
| ขนาด | ราคา/เมตร |
|---|---|
| 4 นิ้ว | 400 |
| 5 นิ้ว | 450 |
| 6 นิ้ว | 500 |
| 8 นิ้ว | 600 |

### รางสแตนเลส (Stainless) — บาท/เมตร พร้อมติดตั้ง
| ขนาด | ราคา/เมตร |
|---|---|
| 6 นิ้ว | 850 |
| 8 นิ้ว | 1,350 |

> ลูกค้าเลือกแค่ "วัสดุ + ประเภทอาคาร" เช่น สแตนเลส + บ้านเดี่ยว → สแตนเลส 8 นิ้ว = 1,350 บาท/เมตร (ขนาด 4"/5" สังกะสีเก็บไว้ในระบบเผื่อ admin เพิ่ม building type ในอนาคต)

---

## 5. Data Model (PostgreSQL + EF Core)

ตั้งชื่อ table/field เป็นภาษาอังกฤษ ใช้ snake_case ใน DB

**GutterProduct** — `Id`, `Material` (enum `Galvanized|Stainless`), `SizeInches` (4,5,6,8), `PricePerMeter` (decimal), `IsActive` (bool) — unique (Material, SizeInches)

**BuildingType** — `Id`, `Label` (เช่น "ทาวน์โฮม / บ้านแฝด"), `SizeInches` (ขนาดที่ map ไป), `DisplayOrder`, `IsActive`

**PricingConfig** (single-row) — `MinimumMeters` (10), `DownspoutPricePerPoint` (500), `HeightSurchargePercent` (20), `RemovalPricePerMeter` (60), `SurveyFee` (1000)

**ServiceZone** — `Id`, `Name`, `TravelSurcharge` (decimal), `IsActive`

**ShopProfile** (single-row — หัวกระดาษ PDF) — `ShopName`, `Phone`, `Address`, `LogoUrl` (nullable), `LineOaLink`, `QuoteValidityDays` (30), `QuoteFooterNote`

**Lead** — `Id`, `CustomerName`, `Phone`, `ServiceZoneId` (FK nullable), `LocationDetail` (string nullable — รายละเอียดเพิ่มเติม เช่น ชื่อหมู่บ้าน), `CreatedAt` (timestamptz)

**QuoteRequest** — `Id`, `QuoteNumber` (เช่น `QT-2026-0001` รันอัตโนมัติ), `LeadId` (FK), input snapshot (`Material`, `BuildingTypeLabel`, `SizeInches`, `LengthMeters`, `DownspoutCount`, `Floors`, `RemoveOld`), `EstimatedTotal` (decimal), `BreakdownJson` (jsonb), `Status` (enum `New|Contacted|Quoted|Won|Lost`, default `New`), `CreatedAt`

**AdminUser** — `Id`, `Username`, `PasswordHash` (BCrypt)

### Seed data
- GutterProduct: ตามตารางข้อ 4
- BuildingType: "ทาวน์โฮม / บ้านแฝด" → 6", "บ้านเดี่ยว / ร้านค้า" → 8"
- ServiceZone: **เมืองชลบุรี, ศรีราชา, พัทยา** (travelSurcharge เริ่มต้น = 0 ทั้งหมด)
- PricingConfig: ตาม default ข้อ 3
- ShopProfile: `ShopName = "ส.จาตุรนต์ รางน้ำ"`, `Phone = "0814569272"`, ที่เหลือเว้นว่าง/แก้ทีหลัง
- AdminUser: 1 คน อ่าน username/password เริ่มต้นจาก env

---

## 6. API (ASP.NET Core Minimal API)

ทุก endpoint return JSON, ใส่ validation, ใส่ CORS อนุญาต origin ของ Vercel
แยกกลุ่ม **public** (ลูกค้า) และ **admin** (ต้อง auth ด้วย JWT)

### Public
| Method | Path | Description |
|---|---|---|
| GET | `/api/products` | active products (วัสดุ/ขนาด/ราคา) |
| GET | `/api/building-types` | ประเภทอาคาร (สำหรับ dropdown ลูกค้า) |
| GET | `/api/zones` | service zones (ชลบุรี/ศรีราชา/พัทยา) |
| GET | `/api/shop-profile` | ข้อมูลร้าน (ชื่อ, เบอร์, LINE link) |
| POST | `/api/estimate` | รับ input → resolve ประเภทอาคาร→ขนาด→ราคา → คืน breakdown + total + disclaimer (ไม่บันทึก DB) |
| POST | `/api/quote-requests` | รับ input + ข้อมูลลูกค้า → สร้าง Lead + QuoteRequest → push LINE → คืน `quoteNumber` |
| GET | `/api/quote-requests/{id}/pdf` | สร้าง & ดาวน์โหลดใบเสนอราคา PDF |

### Admin (JWT required)
| Method | Path | Description |
|---|---|---|
| POST | `/api/admin/login` | username/password → JWT |
| GET | `/api/admin/stats` | สถิติ dashboard (ข้อ 9) |
| GET | `/api/admin/quote-requests` | list + filter by status/date + pagination |
| GET | `/api/admin/quote-requests/{id}` | รายละเอียด |
| PUT | `/api/admin/quote-requests/{id}/status` | อัปเดตสถานะ |
| GET/POST/PUT/DELETE | `/api/admin/products` | จัดการราคา/สินค้า |
| GET/POST/PUT/DELETE | `/api/admin/building-types` | จัดการ mapping ประเภทอาคาร→ขนาด |
| GET/PUT | `/api/admin/config` | แก้ PricingConfig |
| GET/POST/PUT/DELETE | `/api/admin/zones` | จัดการ service zone |
| GET/PUT | `/api/admin/shop-profile` | แก้ข้อมูลร้าน |

### LINE
| Method | Path | Description |
|---|---|---|
| POST | `/api/line/webhook` | รับ event จาก LINE (verify signature) — ดึง `userId`/`groupId` เจ้าของ + เผื่อ auto-reply |

**POST /api/estimate — request / response:**
```json
// request
{ "material":"Stainless","buildingTypeId":2,"lengthMeters":8,
  "downspoutCount":2,"floors":2,"removeOld":true,"serviceZoneId":1,
  "locationDetail":"หมู่บ้านสุขใจ ซอย 3" }
// response
{ "breakdown":[
    {"label":"ค่ารางสแตนเลส 8\" (บ้านเดี่ยว/ร้านค้า, คิดขั้นต่ำ 10 ม.)","amount":13500},
    {"label":"ท่อน้ำลง 2 จุด","amount":1000},
    {"label":"รื้อถอนของเดิม 8 ม.","amount":480}],
  "total":14980,
  "disclaimer":"ราคาประเมินเบื้องต้น ราคาจริงยืนยันหลังสำรวจหน้างาน" }
```

---

## 7. ฟีเจอร์: ใบเสนอราคา PDF

- ใช้ **QuestPDF** (community license)
- **ต้อง embed ฟอนต์ไทย** (เช่น Sarabun / TH Sarabun New) register ใน QuestPDF ไม่งั้นภาษาไทยไม่ขึ้น
- เนื้อหา: หัวกระดาษ (ชื่อร้าน "ส.จาตุรนต์ รางน้ำ" + เบอร์ 081-456-9272 + ที่อยู่ + โลโก้) / เลขที่ใบเสนอราคา + วันที่ + ยืนราคา X วัน / ข้อมูลลูกค้า (ชื่อ/เบอร์/พื้นที่/รายละเอียดเพิ่มเติม) / ตาราง line items + ยอดรวม / disclaimer + **"เอกสารนี้เป็นใบเสนอราคา ไม่ใช่ใบกำกับภาษี"** / footer note
- สร้าง on-demand ที่ `GET /api/quote-requests/{id}/pdf` (stream `application/pdf`)
- ปุ่มดาวน์โหลดอยู่ทั้งหน้า thank-you และหน้า admin

---

## 8. ฟีเจอร์: LINE Official Account

> **อย่าใช้ LINE Notify** (ปิดบริการ มี.ค. 2025) ใช้ **LINE Messaging API**

- **Push แจ้ง lead ใหม่:** เมื่อ `POST /api/quote-requests` สำเร็จ push หาเจ้าของผ่าน `POST https://api.line.me/v2/bot/message/push` (header `Authorization: Bearer {LINE_CHANNEL_ACCESS_TOKEN}`, `to = LINE_OWNER_ID`) ข้อความสรุป: ชื่อ/เบอร์/วัสดุ/ประเภทอาคาร/เมตร/ยอดประเมิน/เลขที่ใบเสนอราคา (แนะนำ Flex Message) ทำเป็น `ILineNotificationService` แยก
- **Webhook:** `POST /api/line/webhook` verify `x-line-signature` ใช้ดึง `userId`/`groupId` ตอน setup
- **ปุ่มแชท (frontend):** ปุ่ม "แชทกับช่างทาง LINE" เปิด `ShopProfile.LineOaLink`
- **Env:** `LINE_CHANNEL_ACCESS_TOKEN`, `LINE_CHANNEL_SECRET`, `LINE_OWNER_ID`
- LINE เป็น optional — ถ้าไม่ตั้ง env ระบบทำงานต่อได้ ไม่ crash

---

## 9. ฟีเจอร์: Admin Dashboard (Angular)

เข้าที่ route `/admin` หลัง login (JWT + interceptor แนบ token)

- **Dashboard (สถิติ)** จาก `GET /api/admin/stats`: จำนวน lead ทั้งหมด/เดือนนี้/สัปดาห์นี้, funnel ตามสถานะ + conversion, มูลค่าประเมินรวม + เฉลี่ย, วัสดุ/ประเภทอาคารที่ขอบ่อยสุด, lead แยกตาม zone, กราฟ lead ต่อสัปดาห์ (~12 สัปดาห์)
- **Leads / Quote Requests:** ตาราง + filter สถานะ/วันที่ + pagination, ดูรายละเอียด (input + breakdown + ปุ่ม PDF), เปลี่ยนสถานะ, กดโทร/คัดลอกเบอร์
- **จัดการราคา:** CRUD `GutterProduct`, CRUD `BuildingType` (mapping ประเภทอาคาร→ขนาด), แก้ `PricingConfig`, CRUD `ServiceZone`, แก้ `ShopProfile`
- กราฟใช้ library เบา ๆ (ng2-charts/Chart.js)

---

## 10. Frontend (Angular) — ฝั่งลูกค้า

- **Mobile-first**, UI ภาษาไทยทั้งหมด, standalone components + reactive forms + signals, API base URL จาก `environment.ts`
- **Header/Footer:** แสดงชื่อร้าน **"ส.จาตุรนต์ รางน้ำ"** และเบอร์โทรเป็น **tag กดโทรได้** → `<a href="tel:0814569272">โทร 081-456-9272</a>`

**Flow:**
1. ฟอร์ม:
   - เลือก **วัสดุ** (สังกะสี / สแตนเลส)
   - เลือก **ประเภทสถานที่ติดตั้ง** (ทาวน์โฮม/บ้านแฝด, บ้านเดี่ยว/ร้านค้า) — มี helper text "ไม่ต้องรู้ขนาดราง ระบบเลือกขนาดที่เหมาะให้ตามอาคาร" (**ไม่มี dropdown ขนาด, ไม่มี dropdown ผิวราง**)
   - **จำนวนเมตร**
   - **จุดท่อน้ำลง**
   - **จำนวนชั้น**
   - toggle **"มีของเดิมต้องรื้อ"**
   - เลือก **พื้นที่ติดตั้ง** (เมืองชลบุรี / ศรีราชา / พัทยา)
   - ช่อง **"รายละเอียดเพิ่มเติม"** (ระบุชื่อหมู่บ้าน ฯลฯ — optional)
2. กด "คำนวณราคา" → `POST /api/estimate` → แสดง breakdown + ยอดรวม + disclaimer
3. ปุ่ม "ขอใบเสนอราคา / ให้ช่างติดต่อกลับ" → ฟอร์ม ชื่อ+เบอร์ (พื้นที่+รายละเอียดดึงจากด้านบน) → `POST /api/quote-requests` → thank-you (ปุ่มดาวน์โหลด PDF + ปุ่มแชท LINE + ปุ่มกดโทร)

**Validation:** เมตร > 0, เบอร์ 9–10 หลัก, ฟิลด์บังคับครบ

---

## 11. Deployment

**Backend → Render.com:** .NET 9 web service, PostgreSQL ผ่าน env `ConnectionStrings__Default`, รัน EF migrations ตอน startup/release command, CORS รับ origin Vercel (`AllowedOrigins`), ตั้ง LINE webhook URL ใน LINE console ชี้มาที่ `/api/line/webhook`, หมายเหตุ free tier มี cold start

**Frontend → Vercel:** build production, API base URL ผ่าน environment.prod.ts, `vercel.json` rewrite รองรับ SPA routing

ทำไฟล์ config: `render.yaml`/`Dockerfile` (ถ้าเหมาะ), `vercel.json`

---

## 12. ลำดับการ build (Milestones — ทำทีละอัน)

1. **Scaffold + DB:** monorepo, .NET 9 Minimal API, EF models ทั้งหมด, migration, seed (ราคา + building type + zone ชลบุรี/ศรีราชา/พัทยา + config + shop profile "ส.จาตุรนต์ รางน้ำ" + admin user)
2. **Estimate logic + tests:** service คำนวณ + resolve ประเภทอาคาร→ขนาด→ราคา + **unit tests** ครบเคส → `POST /api/estimate`
3. **Public APIs:** `/products`, `/building-types`, `/zones`, `/shop-profile`
4. **Lead capture:** `POST /api/quote-requests` (บันทึก Lead+QuoteRequest, LocationDetail, gen QuoteNumber)
5. **PDF:** QuestPDF + ฟอนต์ไทย → `GET /quote-requests/{id}/pdf`
6. **LINE:** push แจ้ง lead ใหม่ + webhook + ปุ่มแชท
7. **Auth + Admin APIs:** login (JWT), stats, quote-requests admin, CRUD products/building-types/config/zones/shop-profile
8. **Angular ลูกค้า:** ฟอร์ม (วัสดุ + ประเภทอาคาร + รายละเอียดเพิ่มเติม, ไม่มีขนาด/ผิว) + breakdown + thank-you (PDF + LINE + กดโทร) + header เบอร์กดโทร
9. **Angular admin:** login, dashboard, จัดการ leads, จัดการราคา/building-type/config/zone/shop
10. **Deployment:** Render + Vercel + CORS + env + README

---

## 13. Non-functional

- การคำนวณราคา + resolve ขนาดจากประเภทอาคาร authoritative ที่ backend เสมอ
- validate input ทั้ง client/server, ใช้ `decimal` กับเงินทุกที่ ห้าม float
- เก็บความลับใน env เท่านั้น (DB conn, JWT secret, LINE tokens) ห้าม commit, hash password ด้วย BCrypt
- LINE notification optional — ไม่ตั้ง env ก็ทำงานต่อได้
- logging พื้นฐาน, แยก layer (endpoints/services/data)
- README ครบ: run dev, env vars, migration, deploy, setup LINE OA

---

## 14. สรุป Business Rules ที่ปรับล่าสุด (กันพลาด)

1. **ไม่มี dropdown ขนาดราง** ลูกค้าเลือก "ประเภทสถานที่ติดตั้ง" → ระบบ map เป็นขนาด (ทาวน์โฮม/บ้านแฝด=6", บ้านเดี่ยว/ร้านค้า=8") ผ่านตาราง `BuildingType` ที่แก้ได้ใน admin
2. **ตัด dropdown ผิวราง (finish) ออก** ไม่ใช้ ไม่มีผลต่อราคา
3. **พื้นที่ติดตั้งมีแค่ 3 ตัวเลือก:** เมืองชลบุรี, ศรีราชา, พัทยา + ช่อง "รายละเอียดเพิ่มเติม" (เช่น ชื่อหมู่บ้าน) เก็บลง `Lead.LocationDetail`
4. **ชื่อร้าน "ส.จาตุรนต์ รางน้ำ"** เบอร์ **081-456-9272** แสดงเป็น tag กดโทรได้ (`tel:0814569272`) ทั้งบนเว็บและใน PDF


# Change Request #1 — ปรับ UX และข้อมูลร้าน (ส.จาตุรนต์ รางน้ำ)

ระบบถูก build เสร็จแล้วตาม spec เดิม ให้ **แก้เฉพาะส่วนที่ระบุด้านล่างเท่านั้น** อ่านโค้ดที่มีอยู่ในโปรเจกต์เพื่อหาจุดที่ต้องแก้ **ห้าม rebuild หรือแตะส่วนที่ไม่เกี่ยวข้อง**

ข้อกำหนดทั่วไป:
- สร้าง **EF migration ใหม่** สำหรับการเปลี่ยน schema (อย่าแก้ migration เดิม)
- อัปเดต unit test ที่กระทบ และรัน test ทั้งหมดให้ผ่าน
- การ resolve ขนาดราง + คำนวณราคา ต้องอยู่ที่ backend เหมือนเดิม
- จบงานแล้วสรุปรายการไฟล์ที่แก้ + คำสั่ง migration/reseed ที่ต้องรัน

---

## 1. ขนาดราง → เลือกจาก "ประเภทอาคาร" แทนการเลือกขนาดตรง ๆ
- เพิ่ม entity/ตาราง `BuildingType { Id, Label, SizeInches, DisplayOrder, IsActive }`
- seed: `"ทาวน์โฮม / บ้านแฝด" → 6`, `"บ้านเดี่ยว / ร้านค้า" → 8`
- `POST /api/estimate` และ `POST /api/quote-requests`: เปลี่ยนจากรับ `sizeInches` (และ `finish`) เป็นรับ `buildingTypeId` แล้ว backend resolve เป็น `sizeInches` เพื่อ lookup ราคา
- เพิ่ม endpoint `GET /api/building-types` (public)
- frontend (ฟอร์มลูกค้า): เปลี่ยน dropdown "ขนาดราง" เป็น dropdown **"ประเภทสถานที่ติดตั้ง"** + helper text "ไม่ต้องรู้ขนาดราง ระบบเลือกขนาดที่เหมาะสมให้ตามอาคาร"
- admin: เพิ่มหน้า CRUD `building-types` (`/api/admin/building-types`)
- เก็บ snapshot `BuildingTypeLabel` + `SizeInches` ไว้ใน `QuoteRequest` เหมือนเดิม

## 2. ลบผิวราง (finish) ออกทั้งหมด
- ลบ field `Finish` จาก `GutterProduct` และทุกที่ที่อ้างถึง (API, estimate input, UI, seed, PDF, LINE message)
- ปรับ seed รางสแตนเลส: `6" = 850`, `8" = 1350` (ราคาเดียวต่อขนาด)

## 3. พื้นที่ติดตั้ง
- เปลี่ยน seed `ServiceZone` เป็น 3 รายการ: **เมืองชลบุรี, ศรีราชา, พัทยา** (`travelSurcharge = 0` ทั้งหมด)
- เพิ่ม field `LocationDetail` (string, nullable) ใน `Lead`
- frontend: เพิ่มช่อง **"รายละเอียดเพิ่มเติม (เช่น ชื่อหมู่บ้าน)"** (optional) ส่งค่าไปกับ quote request
- แสดง `LocationDetail` ในใบเสนอราคา PDF และในข้อความแจ้งเตือน LINE

## 4. ชื่อร้าน + เบอร์โทร
- อัปเดต seed `ShopProfile`: `ShopName = "ส.จาตุรนต์ รางน้ำ"`, `Phone = "0814569272"`
- frontend (header + thank-you) และ PDF: แสดงเบอร์เป็น **ลิงก์กดโทรได้** → `<a href="tel:0814569272">โทร 081-456-9272</a>`