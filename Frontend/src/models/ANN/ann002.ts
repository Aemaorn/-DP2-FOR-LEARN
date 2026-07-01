import type { TDataTableResult, TPaginated } from '../shared/paginated';

export type TAnnouncementReportCriteria = {
  keyword?: string;
  announcementReportTypeCode?: string;
  year?: number;
} & TPaginated;

export type TAnnouncementReportList = {
  id: string;
  year?: number;
  discretion?: string;
  announcementReportTypeCode?: string;
  announcementReportType?: string;
  isActive?: boolean;
  isDp?: boolean;
  documentId?: string;
  documentName?: string;
  documentUrl?: string;
};

export type TAnnouncementReportResponse = TDataTableResult<TAnnouncementReportList>;

export type TAnnouncementReportBody = {
  year?: number;
  discretion?: string;
  announcementReportTypeCode?: string;
  documentInfo?: File | null;
  documentName?: string;
  documentId?: string;
  documentUrl?: string;
};

export type TAnnouncementReportDetail = {
  id: string;
  year?: number;
  discretion?: string;
  announcementReportTypeCode?: string;
  announcementReportType?: string;
  isActive?: boolean;
  isDp?: boolean;
  documentId?: string;
  documentName?: string;
  documentUrl?: string;
};

export type TAnnouncementReportImportRow = {
  oldId?: number;
  year?: number;
  discretion?: string;
  announcementReportTypeCode?: string;
  documentUrl?: string;
};

export type TAnnouncementReportImportResponse = {
  totalRows: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  failedRows: {
    rowIndex: number;
    discretion?: string;
    errorMessage: string;
  }[];
  skippedRows: {
    rowIndex: number;
    discretion?: string;
    year?: number;
    announcementReportTypeCode?: string;
  }[];
};
