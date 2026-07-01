export enum ProcurementWorkProcess {
  InProcess = 'inProcess',
  Related = 'related',
  Completed = 'completed',
  All = 'all',
}

export enum ProcurementPlanType {
  All = 'all',
  AnnualPlan = 'AnnualPlan',
  InYearPlan = 'InYearPlan',
}

export enum ProcurementProcess {
  All = 'all',
  DuringChange = 'duringChange',
  DuringCancel = 'duringCancel',
}

export enum ProcurementStatus {
  All = 'all',
  Draft = 'Draft',
  InProgress = 'InProgress',
  Completed = 'Completed',
  WaitingApproval = 'WaitingApproval',
  Cancelled = 'Cancelled',
  Closed = 'Closed',
}

export enum ProcurementTab {
  Detail = 'Detail',
  RequestApprove = 'RequestApprove',
  Announcement = 'Announcement',
}

export enum ProcurementStep {
  PreProcurement = "PreProcurement",
  Procurement = "Procurement",
  ContractAgreement = "ContractAgreement",
  ContractManagement = "ContractManagement",
  Accounting = "Accounting",
}

export enum ProcurementType {
  Procurement = "Procurement",
  Rent = "Rent",
}