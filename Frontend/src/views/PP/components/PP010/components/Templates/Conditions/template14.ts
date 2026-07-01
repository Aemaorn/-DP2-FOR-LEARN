import type {
  AdvancePaymentField,
  AgreementField,
  ContractPerformanceField,
  FineField,
  PaymentField,
  RetentionField,
} from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'ลักษณะงานที่จ้างที่ปรึกษา' },
  count: { isShow: true, label: 'จำนวน' },
  subCount: { isShow: true },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
  startDate: { isShow: true, label: 'เริ่มลงมือทำงานภายในวันที่' },
  endDate: { isShow: true, label: 'ดำเนินการให้แล้วเสร็จภายในวันที่' },
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
  advanceReimbursement: { isShow: true, label: 'ผู้ว่าจ้างจะหักเงินค่าจ้างในแต่ละเดือนเพื่อชดใช้คืนเงินค่าจ้างล่วงหน้าไว้จำนวนร้อยละ (ของจำนวนเงินค่าจ้างในแต่ละเดือน)' },
  prepayment: { isShow: true, label: 'การชำระเงินให้แก่ผู้ซื้อมีการจ่ายเงินล่วงหน้าหรือไม่' },
  percentage: { isShow: true, label: 'อัตราร้อยละ(ของราคาจ้าง)' },
  price: { isShow: true, label: 'จ่ายเงินล่วงหน้าค่าจ้างล่วงหน้า' },
} as AdvancePaymentField;

const accessFieldFine = {
  fineType: { isShow: true, label: 'ประเภทค่าปรับ' },
  percentage: { isShow: true, label: 'ค่าปรับอัตราร้อยละ' },
  price: { isShow: true, label: 'จำนวนเงินค่าปรับ' },
  by: { isShow: true, label: 'ต่อ' },
} as FineField;

const accessFieldRetention = {
  retention: { isShow: true, label: 'ต้องการหักเงินประกันผลงานหรือไม่' },
  percentage: { isShow: true, label: 'จำนวนร้อยละ ของเงินที่ต้องจ่ายในงวดนั้น' },
  price: { isShow: true, label: 'จำนวนเงิน(บาท)' }
} as RetentionField;

export {
  accessFieldAgreement,
  accessFieldContractPerformance,
  accessFieldPayment,
  accessFieldAdvancePayment,
  accessFieldFine,
  accessFieldRetention,
}