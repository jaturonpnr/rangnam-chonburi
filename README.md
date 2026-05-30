# ระบบประเมินราคาติดตั้งรางน้ำฝน

ระบบคำนวณและประเมินราคาติดตั้งรางน้ำฝน พร้อมระบบหลังบ้านสำหรับเจ้าของร้าน

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | Angular 18 (standalone components, signals) |
| Backend | .NET 9, ASP.NET Core Minimal API |
| Database | PostgreSQL + Entity Framework Core |
| PDF | QuestPDF (community license) |
| Notifications | LINE Messaging API |
| Deploy | Render.com (backend) + Vercel (frontend) |

## โครงสร้างโปรเจกต์

```
rangnam-chonburi/
├── backend/
│   ├── src/RainGutter.Api/     API + Services
│   ├── tests/RainGutter.Tests/ Unit tests
│   ├── Dockerfile
│   └── render.yaml
└── frontend/
    ├── src/app/
    │   ├── core/               Models + Services
    │   └── features/           Customer + Admin UI
    └── vercel.json
```

---

## Local Development

### ข้อกำหนดเบื้องต้น

- .NET 9 SDK
- Node.js 18+
- PostgreSQL (local หรือ Docker)

### 1. ตั้งค่า Backend

```bash
cd backend
cp .env.example .env
# แก้ไขค่าใน .env ตามต้องการ

cd src/RainGutter.Api
dotnet run
```

Backend จะรันที่ `http://localhost:5000`  
เมื่อ start จะ migrate DB และ seed ข้อมูลอัตโนมัติ

### 2. รัน Unit Tests

```bash
cd backend
dotnet test
```

### 3. ตั้งค่า Frontend

```bash
cd frontend
npm install
ng serve
```

Frontend จะรันที่ `http://localhost:4200`

---

## Environment Variables

### Backend

| Variable | Required | Default | Description |
|---|---|---|---|
| `ConnectionStrings__Default` | ✅ | localhost/raingutter | PostgreSQL connection string |
| `JWT_SECRET` | ✅ | dev-secret | JWT signing secret (ควรยาว ≥ 32 chars) |
| `ADMIN_USERNAME` | ✅ | admin | ชื่อผู้ใช้ admin เริ่มต้น |
| `ADMIN_PASSWORD` | ✅ | changeme123 | รหัสผ่าน admin เริ่มต้น |
| `AllowedOrigins` | ✅ | http://localhost:4200 | CORS origins (comma-separated) |
| `LINE_CHANNEL_ACCESS_TOKEN` | ⬜ | - | LINE Messaging API token (optional) |
| `LINE_CHANNEL_SECRET` | ⬜ | - | LINE channel secret สำหรับ verify webhook |
| `LINE_OWNER_ID` | ⬜ | - | userId หรือ groupId ของเจ้าของร้าน |

### Frontend

แก้ไข `src/environments/environment.prod.ts`:
```ts
export const environment = {
  production: true,
  apiBaseUrl: 'https://your-backend.onrender.com'
};
```

---

## Database Migration

Migration จะรันอัตโนมัติเมื่อ backend start ครั้งแรก  
ถ้าต้องการรันเอง:

```bash
cd backend
dotnet ef database update --project src/RainGutter.Api
```

---

## LINE Official Account Setup

1. สมัคร LINE Developers Console และสร้าง Messaging API channel
2. คัดลอก **Channel Access Token** และ **Channel Secret**
3. ตั้งค่า Webhook URL: `https://your-backend.onrender.com/api/line/webhook`
4. เพิ่ม bot เป็นเพื่อนใน LINE แล้วส่งข้อความ
5. ดู log ของ backend เพื่อรับ `userId` ของเจ้าของร้าน
6. ตั้งค่า `LINE_OWNER_ID` ด้วย userId ที่ได้

> ถ้าไม่ตั้งค่า LINE env vars ระบบยังทำงานได้ปกติ แค่ไม่มีการแจ้งเตือน

---

## Deployment

### Backend → Render.com

1. Push code ขึ้น GitHub
2. สร้าง new Web Service บน Render, เชื่อมกับ repo
3. ตั้งค่า **Root Directory** เป็น `backend`
4. Render จะใช้ `Dockerfile` โดยอัตโนมัติ
5. ตั้งค่า Environment Variables ทั้งหมดในหน้า Dashboard
6. หมายเหตุ: free tier มี cold start ~30-50 วินาที

### Frontend → Vercel

```bash
cd frontend
vercel --prod
```

หรือ import repo บน Vercel Dashboard:
- Framework: Angular
- Build Command: `ng build`
- Output Directory: `dist/frontend/browser`
- ตั้งค่า `ANGULAR_API_BASE_URL` ใน Vercel env ถ้าต้องการ inject ตอน build

---

## หน้าเว็บ

| Route | คำอธิบาย |
|---|---|
| `/` | หน้าคำนวณราคา (สาธารณะ) |
| `/thank-you/:quoteNumber` | หน้ายืนยันคำขอ + ดาวน์โหลด PDF |
| `/admin/login` | เข้าสู่ระบบ admin |
| `/admin/dashboard` | สถิติ + กราฟ |
| `/admin/leads` | รายการ leads ทั้งหมด |
| `/admin/leads/:id` | รายละเอียด lead + เปลี่ยนสถานะ |
| `/admin/pricing` | จัดการราคา / ตั้งค่า / ข้อมูลร้าน |

---

## API Endpoints

### Public
- `GET /api/products` — รายการสินค้า/ราคา
- `GET /api/zones` — โซนพื้นที่ให้บริการ
- `GET /api/shop-profile` — ข้อมูลร้าน
- `POST /api/estimate` — ประเมินราคา (ไม่บันทึก)
- `POST /api/quote-requests` — สร้างใบเสนอราคา
- `GET /api/quote-requests/{id}/pdf` — ดาวน์โหลด PDF

### Admin (JWT required)
- `POST /api/admin/login`
- `GET /api/admin/stats`
- `GET/PUT /api/admin/quote-requests` + status update
- `CRUD /api/admin/products`
- `GET/PUT /api/admin/config`
- `CRUD /api/admin/zones`
- `GET/PUT /api/admin/shop-profile`

### LINE
- `POST /api/line/webhook`
