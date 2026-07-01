import type {
  AgreementField,
  ContractPerformanceField,
  PaymentField,
} from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'สถานที่รักษาความปลอดภัย' },
  subTitle: { isShow: true, label: 'สถานที่รับจ้างทำงาน' },
  province: { isShow: true, label: 'จังหวัด' },
  district: { isShow: true, label: 'อำเภอ/เขต' },
  subDistrict: { isShow: true, label: 'ตำบล/แขวง' },
  period: { isShow: true, label: 'ระยะเวลาการจ้าง(ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
  startDate: { isShow: true, label: 'นับตั้งแต่วันที่' },
  endDate: { isShow: true, label: 'จนถึง' },
  count: { isShow: true, label: 'จำนวน' },
  subCount: { isShow: true },
  vatRate: { isShow: true, label: 'อัตราภาษีมูลค่าเพิ่ม' },
  agreedPrice: { isShow: true, label: 'ราคาที่ตกลงกัน (บาท)' },
  vat: { isShow: true, label: 'ภาษีมูลค่าเพิ่ม' },
  priceTotal: { isShow: true, label: 'รวมราคาทั้งหมด' },
  fineType: { isShow: true, label: 'ประเภทค่าปรับ' },
  finePercentage: { isShow: true, label: 'ค่าปรับอัตราร้อยละ' },
  by: { isShow: true, label: 'ต่อ' },
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

export {
  accessFieldAgreement,
  accessFieldContractPerformance,
  accessFieldPayment,
}