import type { AgreementField, ContractPerformanceField, DefectLiabilityField, DeliveryField, FineField, PaymentField } from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'รายการสินค้าพัสดุที่ตกลงซื้อขาย' },
  count: { isShow: true, label: 'จำนวน' },
  subCount: { isShow: true },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
} as AgreementField;

const accessFieldDelivery = {
  place: { isShow: true, label: 'สถานที่ส่งมอบที่ปรากฎตามสัญญา' },
  date: { isShow: true, label: 'ผู้ขายจะส่งมอบของภายในวันที่' },
  day: { isShow: true, label: 'ผู้ขายจะส่งมอบของภายใน(วัน)' },
  subDay: { isShow: true },
} as DeliveryField;

const accessFieldPayment = {
  paymentType: { isShow: true, label: 'ประเภทการจ่ายเงิน' },
  detail: { isShow: true },
} as PaymentField;

const accessFieldDefectLiability = {
  warrantySelect: { isShow: true, label: 'มีการรับประกันความชำรุดบกพร่องหรือไม่' },
  year: { isShow: true, label: 'ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง (ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
  correctionPeriod: { isShow: true, label: 'ระยะเวลาให้แก้ไข ภายในกำหนด' },
  correctionPeriodType: { isShow: true },
  subCorrectionPeriodType: { isShow: true },
} as DefectLiabilityField;

const accessFieldContractPerformance = {
  collateralType: { isShow: true, label: 'ประเภทหลักประกัน' },
  price: { isShow: true, label: 'จำนวนเงิน' },
  percentage: { isShow: true, label: 'ร้อยละ (ของราคาทั้งหมดตามสัญญา)' },
} as ContractPerformanceField;

const accessFieldFine = {
  fineType: { isShow: true, label: 'ประเภทค่าปรับ' },
  percentage: { isShow: true, label: 'ค่าปรับอัตราร้อยละ' },
  price: { isShow: true, label: 'จำนวนเงินค่าปรับ' },
  by: { isShow: true, label: 'ต่อ' },
} as FineField;

export {
  accessFieldAgreement,
  accessFieldDelivery,
  accessFieldPayment,
  accessFieldDefectLiability,
  accessFieldContractPerformance,
  accessFieldFine,
}