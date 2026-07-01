export enum Cm006Status {
  All = 'All',
  Draft = 'Draft',
  WaitingCommitteeApproval = 'WaitingCommitteeApproval',
  WaitingAssigned = 'WaitingAssigned',
  Assigned = 'Assigned',
  WaitingAcceptance = 'WaitingAcceptance',
  Approved = 'Approved',
  Rejected = 'Rejected',
  WaitingAccountingApproval = 'WaitingAccountingApproval',
  AccountingRejected = 'AccountingRejected',
  WaitingDisbursementDate = 'WaitingDisbursementDate',
  Paid = 'Paid',
};

export enum Cm006AccordionTab {
  Committee = 'Committee',
  Assignee = 'Assignee',
  Acceptor = 'Acceptor',
  Accounting = 'Accounting',
  AccountingConfirmer = 'AccountingConfirmer',
};