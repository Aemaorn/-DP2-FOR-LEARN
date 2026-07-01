import type {
  AdvancePaymentField,
  AgreementField,
  CarRentalField,
  ContractPerformanceField,
  FineField,
  InspectionField,
  PaymentField,
} from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'รถยนต์ที่เช่า' },
  brand: { isShow: true, label: 'ยี่ห้อ' },
  model: { isShow: true, label: 'รุ่น' },
  machineNo: { isShow: true, label: 'ขนาดเครื่องยนต์(ซีซี)' },
  period: { isShow: true, label: 'กำหนดระยะเวลาการเช่ารถยนต์(ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
  startDate: { isShow: true, label: 'นับตั้งแต่วันที่' },
  endDate: { isShow: true, label: 'จนถึง' },
  count: { isShow: true, label: 'จำนวน' },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
} as AgreementField;

const accessFieldCarRental = {
  price: { isShow: true, label: 'ค่าเช่า' },
  by: { isShow: true, label: 'ต่อ' }
} as CarRentalField;

const accessFieldPayment = {
  paymentType: { isShow: true, label: 'ประเภทการจ่ายเงิน' },
  detail: { isShow: true },
} as PaymentField;

const accessFieldContractPerformance = {
  collateralType: { isShow: true, label: 'ประเภทหลักประกัน' },
  price: { isShow: true, label: 'จำนวนเงิน' },
  percentage: { isShow: true, label: 'ร้อยละ (ของราคาทั้งหมดตามสัญญา)' },
} as ContractPerformanceField;

const accessFieldAdvancePayment = {
  advanceReimbursement: { isShow: true, label: 'ผู้ว่าจ้างจะหักเงินค่าจ้างในแต่ละเดือนเพื่อชดใช้คืนเงินค่าจ้างล่วงหน้าไว้จำนวนร้อยละ (ของจำนวนเงินค่าจ้างในแต่ละเดือน)' },
  prepayment: { isShow: true, label: 'การชำระเงินให้แก่ผู้ซื้อมีการจ่ายเงินล่วงหน้าหรือไม่' },
  percentage: { isShow: true, label: 'อัตราร้อยละ(ของราคาจ้าง)' },
  price: { isShow: true, label: 'จ่ายเงินล่วงหน้าค่าจ้างล่วงหน้า' },
} as AdvancePaymentField;

const accessFieldInspection = {
  selfCorrection: { isShow: true, label: 'ส่งมอบไม่ถูกต้อง ต้องนำรถยนต์คันอื่นมาส่งมอบให้ใหม่ภายใน' },
  subSelfCorrection: { isShow: true },
  redelivery: { isShow: true, label: 'หรือต้องทำการแก้ไขให้ถูกต้องด้วยค่าใช่จ่ายผู้ให้เช่าเองภายใน' },
  subRedelivery: { isShow: true },
} as InspectionField;

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
  accessFieldFine,
  accessFieldCarRental,
  accessFieldInspection,
}