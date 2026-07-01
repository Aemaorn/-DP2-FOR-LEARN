import type { TDataTableResult, TPaginated } from '../shared/paginated';

export type TAnnouncementSorKorRorCriteria = {
  year?: number;
  month?: number;
  departmentTypeCode?: string;
} & TPaginated;


export type TAnnouncementSorKorRorList = {
  id: string;
  year?: number;
  month?: number;
  amount?: number;
  departmentTypeCode?: string;
  departmentType?: string;
  isActive?: boolean;
  documentId?: string;
  documentName?: string;
  documentUrl?: string;
};

export type TAnnouncementSorKorRorResponse = TDataTableResult<TAnnouncementSorKorRorList>;

export type TAnnouncementSorKorRorBody = {
  year?: number;
  month?: number;
  amount?: number;
  departmentTypeCode?: string;
  documentInfo?: File | null;
  documentName?: string;
  documentId?: string;
  documentUrl?: string;
};

export type TAnnouncementSorKorRorImportRow = {
  oldId?: number;
  year?: number;
  month?: number;
  amount?: number;
  departmentTypeCode?: string;
  documentUrl?: string;
};

export type TAnnouncementSorKorRorImportResponse = {
  totalRows: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  failedRows: {
    rowIndex: number;
    errorMessage: string;
  }[];
  skippedRows: {
    rowIndex: number;
    year?: number;
    month?: number;
    departmentTypeCode?: string;
  }[];
};

export type TAnnouncementSorKorRorDetail = {
  id: string;
  oldId?: number;
  year?: number;
  month?: number;
  amount?: number;
  departmentTypeCode?: string;
  departmentType?: string;
  isDp?: boolean;
  isActive?: boolean;
  documentId?: string;
  documentName?: string;
  documentUrl?: string;
};
