export enum TContractDraftStatus {
  /// <summary>แบบร่าง</summary>
  Draft = "Draft",

  /// <summary>รออนุมัติ</summary>
  Pending = "Pending",

  /// <summary>อนุมัติแล้ว</summary>
  Approved = "Approved",

  /// <summary>ปฏิเสธ</summary>
  Rejected = "Rejected",

  Edit = "Edit",
}

export enum TAgreementBaseType {
  General = "General",
  ExchangeGiver = "ExchangeGiver",
  Workplace = "Workplace",
  WorkplaceSerialNumber = "WorkplaceSerialNumber",
  RentalDuration = "RentalDuration",
  RentalDurationWorkplace = "RentalDurationWorkplace",
  Lease = "Lease",
  LeaseCar = "LeaseCar",
  LeaseComputer = "LeaseComputer",
}

export enum TPaymentBaseType {
  Contract = "Contract",
  Term = "Term",
  TremNotType = "TremNotType",
}

export enum TRedeliveryBaseType {
  Acceptance = "Acceptance",
  Redelivery = "Redelivery",
}
