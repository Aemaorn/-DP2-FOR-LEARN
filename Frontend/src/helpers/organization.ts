import { OrganizationLevelEnum } from '@/enums/shared';

/** ระดับหน่วยงานที่ถือว่างานอยู่ที่ "สาขา" (ไม่ใช่สำนักงานใหญ่) */
export const BRANCH_ORGANIZATION_LEVELS = [
  OrganizationLevelEnum.Zone,     // 500
  OrganizationLevelEnum.Segment,  // 600
  OrganizationLevelEnum.Branch,   // 601
].map(String);

export const isBranchOrganizationLevel = (level?: string): boolean =>
  BRANCH_ORGANIZATION_LEVELS.includes(level ?? '');
