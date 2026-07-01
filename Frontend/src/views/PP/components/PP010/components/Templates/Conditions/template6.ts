import type {
  AgreementField,
  ContractPerformanceField,
  DefectLiabilityField,
  DeliveryField,
  FineField,
} from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  radio: { isShow: true },
  mainTitle: { isShow: true, label: 'ผู้ขายตกลงขายและติดตั้งเครื่องคอมพิวเตอร์ฯ ซึ่งเป็นผลิตภัณฑ์ของ' },
  count: { isShow: true, label: 'จำนวน' },
  subCount: { isShow: true },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
} as AgreementField;

const accessFieldDelivery = {
  place: { isShow: true, label: 'สถานที่ส่งมอบที่ปรากฎตามสัญญา' },
  date: { isShow: true, label: 'ผู้รับแลกเปลี่ยนจะส่งมอบของภายในวันที่' },
  detail: { isShow: true },
} as DeliveryField;

const accessFieldDelivery2 = {
  place: { isShow: true, label: 'สถานที่ส่งมอบที่ปรากฎตามสัญญา' },
  day: { isShow: true, label: 'ผู้รับแลกเปลี่ยนต้องมารับมอบสิ่งของภายในกำหนด (ปี)' },
  subDay: { isShow: true, label: '(เดือน)' },
  superSubDay: { isShow: true, label: '(วัน)' },
} as DeliveryField;

const accessFieldDefectLiability = {
  warrantySelect: { isShow: true, label: 'มีการรับประกันความชำรุดบกพร่องหรือไม่' },
  year: { isShow: true, label: 'ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง (ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
  correctionPeriod: { isShow: true, label: 'ระยะเวลาให้แก้ไข ภายในกำหนด' },
  correctionPeriodType: { isShow: true },
  subCorrectionPeriodType: { isShow: true },
} as DefectLiabilityField;

const accessFieldDefectLiability2 = {
  repairObligation: { isShow: true, label: 'หากชำรุดบกพร่อง ต้องซ่อมแชมหรือติดตั้งใหม่ ภายในกำหนด' },
  subRepairObligation: { isShow: true },
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
  accessFieldDelivery2,
  accessFieldDefectLiability,
  accessFieldDefectLiability2,
  accessFieldContractPerformance,
  accessFieldFine,
}