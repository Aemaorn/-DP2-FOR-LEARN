import type { TDataTableResult, TPaginated } from '../shared/paginated';

export type TAnnouncementInfoCriteria = {
  keyword?: string;
  supplyMethodCode?: string;
  announcementCategoryCode?: string;
  announcementDateFrom?: Date;
  announcementDateTo?: Date;
  budgetYear?: number;
} & TPaginated;

export type TAnnouncementInfoList = {
  id: string;
  announcementName?: string;
  announcementTitle?: string;
  announcementCategory?: string;
  supplyMethod?: string;
  announcementDate?: string;
  budgetAmount?: number;
  budgetYear?: number;
  expectedDate?: string;
  documentId?: string;
  documentUrl?: string;
};

export type TAnnouncementInfoSupplyMethodCount = {
  code: string;
  count: number;
};

export type TAnnouncementInfoResponse = {
  allCount: number;
  counts: TAnnouncementInfoSupplyMethodCount[];
  data: TDataTableResult<TAnnouncementInfoList>;
};

export type TAnnouncementInfoBody = {
  announcementTitle: string;
  announcementName?: string;
  announcementDate?: Date;
  budgetAmount?: number;
  announcementCategoryCode: string;
  supplyMethodCode?: string;
  budgetYear?: number;
  annotation?: string;
  description?: string;
  expectedDate?: Date;
  referencePrice?: number;
  startDate?: Date;
  endDate?: Date;
  documentInfo?: File | null;
  documentId?: string;
  documentName?: string;
  documentUrl?: string;
  remark?: string;
};

export type TAnnouncementInfoDetail = {
  id: string;
  oldId?: number;
  announcementName?: string;
  announcementTitle?: string;
  announcementCategoryCode?: string;
  announcementDate?: string;
  budgetAmount?: number;
  budgetYear?: number;
  expectedDate?: string;
  startDate?: string;
  endDate?: string;
  referencePrice?: number;
  annotation?: string;
  remark?: string;
  description?: string;
  documentName?: string;
  documentId?: string;
  documentUrl?: string;
  supplyMethodCode?: string;
  isDp?: boolean;
  isActive?: boolean;
};

export type TAnnouncementInfoImportRow = {
  oldId?: number;
  announcementName?: string;
  postContent?: string;
  status?: string;
  announcementDate?: string;
  lastModifiedAt?: string;
  createdBy?: string;
  announcementCategoryCode?: string;
  supplyMethodCode?: string;
  budgetAmount?: number;
  announcementTitle?: string;
  email?: string;
  description?: string;
  referencePrice?: number;
  expectedDate?: number;
  budgetYear?: number;
  startDate?: string;
  endDate?: string;
  documentUrl?: string;
};

export type TAnnouncementInfoImportRowResult = {
  rowIndex: number;
  announcementName?: string;
  errorMessage: string;
};

export type TAnnouncementInfoImportResponse = {
  totalRows: number;
  successCount: number;
  failedCount: number;
  failedRows: TAnnouncementInfoImportRowResult[];
};
