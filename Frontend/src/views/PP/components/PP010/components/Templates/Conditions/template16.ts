import type {
  AdvancePaymentField,
  AgreementField,
  ContractPerformanceField,
  DeliveryField,
  FineField,
  ServicePeriodField,
} from "@/models/shared/contractDraft";

const accessFieldAgreement = {
  mainTitle: { isShow: true, label: 'ผู้ให้เช่าตกลงให้เช่าเครื่องคอมพิวเตอร์' },
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

const accessFieldServicePeriod = {
  year: { isShow: true, label: 'ระยะเวลาการคำนวนค่าเช่าคอมพิวเตอร์ (ปี)' },
  month: { isShow: true, label: '(เดือน)' },
  day: { isShow: true, label: '(วัน)' },
} as ServicePeriodField;

const accessFieldDelivery = {
  place: { isShow: true, label: 'สถานที่ส่งมอบที่ปรากฏตามสัญญา' },
  date: { isShow: true, label: 'ผู้ให้เช่าต้องส่งมอบและติดตั้งภายในวันที่' },
} as DeliveryField;

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

export {
  accessFieldAgreement,
  accessFieldContractPerformance,
  accessFieldDelivery,
  accessFieldAdvancePayment,
  accessFieldFine,
  accessFieldServicePeriod,
}