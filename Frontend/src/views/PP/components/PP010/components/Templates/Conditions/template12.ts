import type {
  AgreementField,
  ContractPerformanceField,
  FineField,
  PaymentField,
  ServicePeriodField,
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

const accessFieldFine = {
  fineType: { isShow: true, label: 'ประเภทค่าปรับ' },
  percentage: { isShow: true, label: 'ค่าปรับอัตราร้อยละ' },
  price: { isShow: true, label: 'จำนวนเงินค่าปรับ' },
  by: { isShow: true, label: 'ต่อ' },
} as FineField;

const accessFieldServicePeriod = {
  startDate: { isShow: true, label: 'ผู้รับจ้างตกลงให้บริการระยะเวลา ตั้งแต่วันที่' },
  endDate: { isShow: true, label: 'จนถึง' },
  year: { isShow: true, label: 'ระยะเวลาการให้บริการ(ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
} as ServicePeriodField;

export {
  accessFieldAgreement,
  accessFieldContractPerformance,
  accessFieldPayment,
  accessFieldFine,
  accessFieldServicePeriod,
}