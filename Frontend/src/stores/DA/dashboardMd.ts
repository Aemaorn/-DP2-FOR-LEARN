import { EGroupCode, OrganizationLevelEnum } from "@/enums/shared";
import { type DaMdTableCriteria, type DAMdBody, type DAMdCriteria, type DAMdDetail, type DaMdDetailItems, type DashBoardData, type DaMdDetailSummry } from "@/models/DA/dashboardMd";
import type { Option } from "@/models/shared/option";
import { dashBoardService } from "@/services/DA/dashboardMd";
import SharedService from "@/services/Shared/dropdown";
import { HttpStatusCode } from "axios";
import { defineStore } from "pinia";
import { ref, type Ref } from "vue";
import { useAuthenticationStore } from "../authentication";

const getParameterByGroupCodeAsync = async (target: Ref<Option[]>, groupCode: EGroupCode, parentCode?: string) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(groupCode, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const mapApiToHierarchical = (api: any): DAMdDetail => {
  const summary: DaMdDetailSummry = {
    totalPlanAmount: api?.summary?.totalPlanAmount ?? 0,
    totalProcurementAmount: api?.summary?.totalProcurementAmount ?? 0,
    totalContractAmount: api?.summary?.totalContractAmount ?? 0,
    // totalDisbursementAmount: api?.summary?.totalDisbursementAmount ?? 0,
  };

  const rows = api?.rows ?? [];

  const makeItem = (r: any): DaMdDetailItems => ({
    groupCode: r.groupCode,
    divisionCode: r.divisionCode,
    divisionName: r.divisionName,
    departmentCode: r.departmentCode ?? undefined,
    departmentName: r.departmentName ?? "",
    isDivisionHeader: !!r.isDivisionHeader,
    orgLevel: r.orgLevel ?? 0,
    planAmount: Number(r.planAmount ?? 0),
    planPercent: Number(r.planPercent ?? 0),
    procurementAmount: Number(r.procurementAmount ?? 0),
    procurementPercent: Number(r.procurementPercent ?? 0),
    contractAmount: Number(r.contractAmount ?? 0),
    contractPercent: Number(r.contractPercent ?? 0),
    disbursement: Number(r.disbursementAmount ?? r.disbursement ?? 0),
    disbursementPercent: Number(r.disbursementPercent ?? 0),
    showExpand: false,
    childenDetail: []
  });

  // key: groupCode → level-200 item
  const level200Map = new Map<string, DaMdDetailItems>();
  // key: divisionCode → level-300 item
  const level300Map = new Map<string, DaMdDetailItems>();
  const topLevel: DaMdDetailItems[] = [];

  // Pass 1: level-200 (orgLevel 200 หรือ isDivisionHeader ที่ถูก adjust)
  for (const r of rows) {
    if (r.orgLevel === 200 || (r.isDivisionHeader && r.orgLevel !== 300 && r.orgLevel !== 400)) {
      const item = makeItem(r);
      topLevel.push(item);
      level200Map.set(r.groupCode, item);
    }
  }

  // Pass 2: level-300 → nest under level-200 (match by groupCode)
  for (const r of rows) {
    if (r.orgLevel === 300 && !r.isDivisionHeader) {
      const item = makeItem(r);
      const parent = level200Map.get(r.groupCode);
      if (parent) {
        parent.childenDetail.push(item);
      } else {
        topLevel.push(item);
      }
      level300Map.set(r.divisionCode, item);
    }
  }

  // Pass 3: level-400 → nest under level-300 (match by divisionCode = LineCode)
  for (const r of rows) {
    if (r.orgLevel === 400 && !r.isDivisionHeader) {
      const item = makeItem(r);
      const parent = level300Map.get(r.divisionCode);
      if (parent) {
        parent.childenDetail.push(item);
      } else {
        const grandParent = level200Map.get(r.groupCode);
        if (grandParent) grandParent.childenDetail.push(item);
        else topLevel.push(item);
      }
    }
  }

  // Pass 4: isDivisionHeader rows ที่ถูก adjust (level 300 หรือ 400 เป็น header)
  for (const r of rows) {
    if (r.isDivisionHeader && (r.orgLevel === 300 || r.orgLevel === 400)) {
      const item = makeItem(r);
      topLevel.push(item);
    }
  }

  return { summary, rows: topLevel };
};

export const useDashboardMDStore = defineStore("dashboardMDStore", () => {
  const authStore = useAuthenticationStore();

  const initCriteria = {
    budgetYear: new Date().getFullYear() + 543,
  } as DAMdCriteria;
  const criteria = ref<DAMdCriteria>(structuredClone(initCriteria));

  const tableCriteria = ref<DaMdTableCriteria>({
    userOrgLevel: authStore.profile.organizationLevel,
    supplyMethodCode: "SMethod004",
  });

  const clearCriteria = async () => {
    criteria.value = structuredClone(initCriteria);

    await getDashBoardAsync()
  }

  const clearTableCriteria = async () => {
    tableCriteria.value = {
      userOrgLevel: authStore.profile.organizationLevel,
      supplyMethodCode: tableCriteria.value.supplyMethodCode,
    } as DaMdTableCriteria;

    await getDashBoardTableAsync();
  }

  const supplyMethodDropdown = ref<Option[]>([] as Option[]);
  const groupDropdown = ref<Option[]>([] as Option[]);
  const lineDropdown = ref<Option[]>([] as Option[]);
  const departmentDropdown = ref<Option[]>([] as Option[]);

  const body = ref<DAMdBody>({
    charts: {
      bySupplyMethodContract: [],
      bySupplyMethodDisbursement: [],
      bySupplyMethodPlan: [],
      bySupplyMethodPrinciple: [],
      bySupplyMethodProcurement: [],
      combinedBudgetPlanPw119P79PettyCash: [],
      planBudgetBySupplyMethod: [],
      planBudgetBySupplyMethodType: []
    } as DashBoardData
  } as DAMdBody);

  const historyTable = ref<DAMdDetail>({
    rows: [] as DaMdDetailItems[],
  } as DAMdDetail);

  const fetchOrgDropdown = async (level: OrganizationLevelEnum, target: Ref<Option[]>) => {
    const { data, status } = await SharedService.onGetBusinessUnitAsync(level);
    if (status === HttpStatusCode.Ok) target.value = data;
  };

  const onGetDropdownAsync = async () => {
    await Promise.all([
      getParameterByGroupCodeAsync(supplyMethodDropdown, EGroupCode.SMethod),
      fetchOrgDropdown(OrganizationLevelEnum.Group, groupDropdown),
      fetchOrgDropdown(OrganizationLevelEnum.Line, lineDropdown),
      fetchOrgDropdown(OrganizationLevelEnum.Department, departmentDropdown),
    ]);
  };

  const getDashBoardAsync = async () => {
    const params: DAMdCriteria = {
      ...criteria.value,
      groupCode: tableCriteria.value.groupCode,
      lineCode: tableCriteria.value.lineCode,
      departmentCode: tableCriteria.value.departmentCode,
    };

    const { data, status } = await dashBoardService.getDashBoardAsync(params);

    if (status === HttpStatusCode.Ok) {
      body.value = data;
    }
  }

  const getDashBoardTableAsync = async () => {
    const c = tableCriteria.value;

    let userOrgLevel: number | undefined = Number(authStore.profile.organizationLevel) || undefined;
    let userOrhId: string | undefined = c.userOrhId;

    if (c.groupCode) {
      userOrgLevel = 200;
      userOrhId = c.groupCode;
    } else if (c.lineCode) {
      userOrgLevel = 300;
      userOrhId = c.lineCode;
    } else if (c.departmentCode) {
      userOrgLevel = 400;
      userOrhId = c.departmentCode;
    }

    const params: DaMdTableCriteria = {
      ...c,
      userOrgLevel: userOrgLevel?.toString(),
      userOrhId,
    };

    const { data, status } = await dashBoardService.getDashBoardTableAsync(params);

    if (status === HttpStatusCode.Ok) {
      historyTable.value = mapApiToHierarchical(data);
    }
  }

  return {
    criteria,
    tableCriteria,
    body,
    historyTable,
    supplyMethodDropdown,
    groupDropdown,
    lineDropdown,
    departmentDropdown,
    clearCriteria,
    clearTableCriteria,
    api: {
      getDashBoardAsync,
      onGetDropdownAsync,
      getDashBoardTableAsync
    }
  }
})