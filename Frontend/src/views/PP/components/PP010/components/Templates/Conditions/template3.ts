import type { AgreementField, ContractPerformanceField, DefectLiabilityField, FineField, PaymentField } from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'รายการสินค้าพัสดุที่ตกลงซื้อขาย' },
  count: { isShow: true, label: 'จำนวน' },
  subCount: { isShow: true },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
} as AgreementField;

const accessFieldPayment = {
  paymentType: { isShow: true, label: 'ประเภทการจ่ายเงิน' },
  paymentAgreement: { isShow: true, label: 'ผู้จะซื้อตกลงจะชำระเงินค่าสิ่งของให้แก่ผู้จะขายภายใน (วัน)' },
  subPaymentAgreement: { isShow: true },
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
  accessFieldPayment,
  accessFieldDefectLiability,
  accessFieldContractPerformance,
  accessFieldFine,
}