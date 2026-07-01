import type { TPaginated } from '../shared/paginated';

export type TSt007Criteria = {
  name?: string;
  group?: string;
} & TPaginated;

export type TSt007List = {
  id: string;
  group: string;
  code: string;
  name: string;
  isActive: boolean;
};

export type TSt007Detail = {
  id: string;
  group: string;
  code: string;
  name: string;
  previewPdfFile?: CustomFileSt007;
  file: CustomFileSt007;
  isActive: boolean;

  supplyMethodCode?: string;
  budgetMin?: number;
  budgetMax?: number;
  isCancel?: boolean;
  isEdit?: boolean;
  isChange?: boolean;
  isJorPorComment?: boolean;
  isFine?: boolean;
  contractTemplateCode?: string;
  contractTemplateType?: string;
  principleApprovalTemplateCode?: string;

  isWinnerAnnounced?: boolean;
  isEvaluationReport?: boolean;
  isAppointmentOrdered?: boolean;
  isApproval?: boolean;
  isInYear?: boolean;
  isPublished?: boolean;

  supplyMethodTypeCode?: string;
  hasGuarantee?: boolean;
  isConfidential?: boolean;

  contractAmendmentDocumentType?: string;
};

export type CustomFileSt007 = {
  id: string;
  file?: File;
  fileName: string;
};
