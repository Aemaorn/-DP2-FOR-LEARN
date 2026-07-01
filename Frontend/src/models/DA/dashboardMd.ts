export type DashboardValue = {
  label: string;
  value: number;
};

export type DAMdCriteria = {
  departmentGroup?: string;
  unitCode?: string;
  budgetYear?: number;
  supplyMethodCode?: string;
  groupCode?: string;
  lineCode?: string;
  departmentCode?: string;
}

export type DaMdTableCriteria = {
  budgetYear?: string;
  supplyMethodCode?: string;
  groupCode?: string;
  lineCode?: string;
  departmentCode?: string;
  userOrgLevel?: string;
  userOrhId?: string
}

export type DAMdBody = {
  items: ProjectCountInfo[];
  charts: DashBoardData;
  yearSumary: DashboardValue[];
  procurementSummary: DashboardValue[];
  workGroupSummary: DashboardValue[];
}

export type DashBoardData = {
  bySupplyMethodPlan: DashboardValue[],
  bySupplyMethodProcurement: DashboardValue[],
  bySupplyMethodContract: DashboardValue[],
  bySupplyMethodDisbursement: DashboardValue[],
  bySupplyMethodPrinciple: DashboardValue[],
  planBudgetBySupplyMethod: DashboardValue[],
  planBudgetBySupplyMethodType: DashboardValue[],
  combinedBudgetPlanPw119P79PettyCash: DashboardValue[],
}

export type ProjectCountInfo = {
  supplyMethodCode: string;
  supplyMethodName: string;
  planCount: number;
  procurementCount: number;
  contractDraftVendorCount: number;
  disbursementApprovalCount: number;
  principleApprovalCount: number;
}

export type DashboardHistory = {
  showExpand?: boolean;
  departmentName: string;
  lastedHistory: HistoryInfo;
  history: HistoryInfo[];
}

export type HistoryInfo = {
  departmentName: string;
  procurementBudget: number;
  procurementPercent: number;
  buyBudget: number;
  buyPercent: number;
  contractBudget: number;
  contractPercent: number;
  paymentBudget: number;
  paymentPercent: number;
}

export type DAMdDetail = {
  summary: DaMdDetailSummry;
  rows: DaMdDetailItems[];
}

export type DaMdDetailSummry = {
  totalPlanAmount: number;
  totalProcurementAmount: number;
  totalContractAmount: number;
}

export type DaMdDetailItems = {
  groupCode: string;
  divisionCode: string;
  divisionName: string;
  departmentCode?: string,
  departmentName: string;
  isDivisionHeader: boolean;
  showExpand: boolean;
  orgLevel: number;
  planAmount: number;
  planPercent: number;
  procurementAmount: number;
  procurementPercent: number;
  contractAmount: number;
  contractPercent: number;
  disbursement: number;
  disbursementPercent: number;
  childenDetail: DaMdDetailItems[];
}