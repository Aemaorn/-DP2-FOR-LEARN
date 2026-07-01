# GHB Procurement Dashboard

ระบบติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา สำหรับฝ่ายจัดหาและการพัสดุ

## Tech Stack

- **Frontend:** Vue 3 + TypeScript + Vite
- **UI:** PrimeVue 4 (Aura theme) + Tailwind CSS 4
- **Charts:** Chart.js + vue-chartjs
- **State:** Pinia + localStorage (persistedstate)
- **Routing:** Vue Router 4
- **Import/Export:** SheetJS (xlsx)

## Getting Started

```bash
npm install
npm run dev
```

เข้า http://localhost:5173

### Docker

```bash
docker compose up --build
```

เข้า http://localhost:8080

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Vite dev server |
| `npm run build` | Type-check + production build |
| `npm run preview` | Preview production build |
| `docker compose up --build` | Run via Docker |

## Pages

| Route | Description |
|-------|-------------|
| `/` | Overview Dashboard - cards, pie chart, performance trend |
| `/tracking` | ติดตามสถานะ - ตารางแยก tab กลุ่ม 1/2 พร้อมแก้ไข/ลบ |
| `/entry` | กรอกข้อมูล - ฟอร์มเพิ่มรายการใหม่ |

## Features

- กรอกข้อมูล / แก้ไข / ลบรายการจัดซื้อจัดจ้าง
- คำนวณสถานะอัตโนมัติจากวันทำการ
- Import ข้อมูลจาก Excel (.xlsx)
- Export ข้อมูลเป็น Excel / PDF
- Filter ตาม วิธีการ, สถานะ, ช่วงวันที่, ค้นหา
- กราฟแนวโน้มรายเดือน + Doughnut สถานะรวม
- ข้อมูลเก็บใน localStorage (ไม่ต้องติดตั้ง database)

## Status Logic

นับวันทำการ (ไม่รวมเสาร์-อาทิตย์) จากวันอนุมัติ ถึงวันลงนามสัญญา (หรือวันปัจจุบันถ้ายังไม่ลงนาม)

| สถานะ | เงื่อนไข | สี |
|--------|----------|-----|
| ตามแผน | ≤ 10 วันทำการ | เขียว |
| มีแนวโน้มล่าช้า | 11-15 วันทำการ | ส้ม |
| ช้ากว่าแผน | > 15 วันทำการ | แดง |

## Data Groups

- **กลุ่มที่ 1:** การซื้อ/จ้าง/เช่า ที่ใช้สัญญาของธนาคาร
- **กลุ่มที่ 2:** การซื้อ/จ้าง/เช่า ที่ใช้สัญญาของหน่วยงานอื่น (มีคอลัมน์วงเงินเพิ่ม)

## Project Structure

```
src/
├── components/      # Reusable components
├── composables/     # Business logic (status, export, import)
├── router/          # Vue Router config
├── stores/          # Pinia store + seed data
├── types/           # TypeScript interfaces
└── views/           # Page components
```
