import type {
  AdvancePaymentField,
  AgreementField,
  ContractPerformanceField,
  DefectLiabilityField,
  FineField,
  PaymentField,
} from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'ผู้รับจ้างตกลงรับจ้างทำงาน' },
  subTitle: { isShow: true, label: 'สถานที่รับจ้างทำงาน' },
  count: { isShow: true, label: 'จำนวน' },
  subCount: { isShow: true },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
} as AgreementField;

const accessFieldContractPerformance = {
  collateralType: { isShow: true, label: 'ประเภทหลักประกัน' },
  price: { isShow: true, label: 'จำนวนเงิน' },
  percentage: { isShow: true, label: 'ร้อยละ (ของราคาทั้งหมดตามสัญญา)' },
} as ContractPerformanceField;

const accessFieldPayment = {
  paymentType: { isShow: true, label: 'ประเภทการจ่ายเงิน' },
  detail: { isShow: true },
} as PaymentField;

const accessFieldAdvancePayment = {
  prepayment: { isShow: true, label: 'การชำระเงินให้แก่ผู้ซื่้อมีการจ่ายเงินล่วงหน้าหรือไม่' },
  price: { isShow: true, label: 'จ่ายเงินล่วงหน้าค่าจ้างล่วงหน้า' },
  percentage: { isShow: true, label: 'อัตราร้อยละ(ของราคาจ้าง)' },
} as AdvancePaymentField;

const accessFieldDefectLiability = {
  warrantySelect: { isShow: true, label: 'มีการรับประกันความชำรุดบกพร่องหรือไม่' },
  year: { isShow: true, label: 'ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง (ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
  correctionPeriod: { isShow: true, label: 'ระยะเวลาให้แก้ไข ภายในกำหนด' },
  correctionPeriodType: { isShow: true },
  subCorrectionPeriodType: { isShow: true },
} as DefectLiabilityField;

const accessFieldFine = {
  fineType: { isShow: true, label: 'ประเภทค่าปรับ' },
  percentage: { isShow: true, label: 'ค่าปรับอัตราร้อยละ' },
  price: { isShow: true, label: 'จำนวนเงินค่าปรับ' },
  by: { isShow: true, label: 'ต่อ' },
} as FineField;

export {
  accessFieldAgreement,
  accessFieldContractPerformance,
  accessFieldPayment,
  accessFieldAdvancePayment,
  accessFieldDefectLiability,
  accessFieldFine,
}