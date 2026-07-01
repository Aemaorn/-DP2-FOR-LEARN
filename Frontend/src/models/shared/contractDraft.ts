export type AccessField = {
  isShow?: boolean;
  label?: string;
};

export type AgreementField = {
  radio: AccessField;
  mainTitle: AccessField;
  subTitle: AccessField;
  brand: AccessField;
  model: AccessField;
  machineNo: AccessField;
  province: AccessField;
  district: AccessField;
  subDistrict: AccessField;
  period: AccessField;
  month: AccessField;
  day: AccessField;
  startDate: AccessField;
  endDate: AccessField;
  count: AccessField;
  subCount: AccessField;
  vatRate: AccessField;
  agreedPrice: AccessField;
  vat: AccessField;
  priceTotal: AccessField;
  fineType: AccessField;
  finePercentage: AccessField;
  finePrice: AccessField;
  by: AccessField;
  deliveryDetail: AccessField;
}

export type ContractPerformanceField = {
  collateralType: AccessField;
  price: AccessField;
  percentage: AccessField;
}

export type PaymentField = {
  paymentType: AccessField;
  paymentAgreement: AccessField;
  subPaymentAgreement: AccessField;
  placeDate: AccessField;
  deliveryDate: AccessField;
  detail: AccessField;
}

export type AdvancePaymentField = {
  prepayment: AccessField;
  price: AccessField;
  percentage: AccessField;
  advanceReimbursement: AccessField;
}

export type RetentionField = {
  retention: AccessField;
  price: AccessField;
  percentage: AccessField;
}

export type ContractTerminationField = {
  year: AccessField;
  month: AccessField;
  day: AccessField;
  startDate: AccessField;
  endDate: AccessField;
}

export type DefectLiabilityField = {
  warrantySelect: AccessField;
  year: AccessField;
  month: AccessField;
  day: AccessField;
  correctionPeriod: AccessField;
  correctionPeriodType: AccessField;
  subCorrectionPeriodType: AccessField;
  repairObligation: AccessField;
  subRepairObligation: AccessField;
}

export type FineField = {
  fineType: AccessField;
  percentage: AccessField;
  price: AccessField;
  by: AccessField;
}

export type DeliveryField = {
  place: AccessField;
  date: AccessField;
  day: AccessField;
  subDay: AccessField;
  superSubDay: AccessField;
  designPlace: AccessField;
  subDesignPlace: AccessField;
  detail: AccessField;
}

export type CredentialField = {
  date: AccessField;
  year: AccessField;
  month: AccessField;
  day: AccessField;
}

export type CarRentalField = {
  price: AccessField;
  by: AccessField;
}

export type InspectionField = {
  redelivery: AccessField;
  subRedelivery: AccessField;
  selfCorrection: AccessField;
  subSelfCorrection: AccessField;
}

export type PrinterRentalField = {
  unitPerMonth: AccessField;
  quantity: AccessField;
  monthlyRental: AccessField;
  document: AccessField;
  documentNotEnough: AccessField;
  copyRatePerSheet: AccessField;
}

export type ServicePeriodField = {
  startDate: AccessField;
  endDate: AccessField;
  year: AccessField;
  month: AccessField;
  day: AccessField;
}