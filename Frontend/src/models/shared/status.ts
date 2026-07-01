export type StatusCount = {
  all: number;
  draftPlan: number;
  waitingApprovePlan: number;
  assigned: number;
  draftRecordDocument: number;
  waitingAcceptor: number;
  approvePlan: number;
  waitingAnnouncement: number;
  announcement: number;
  rejectPlan: number;
  cancelPlan: number;
}

export type ContractTypeCount = {
  all: number;
  CMType001: number,
  CMType002: number,
  CMType003: number,
}