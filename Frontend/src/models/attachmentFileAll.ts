export type TAttachmentFileItem = {
  fileId: string;
  fileName: string;
  isPublic: boolean;
  createdBy: string;
  sequence: number;
};

export type TAttachmentRefGroup = {
  id: string | null;
  refNumber: string;
  files: TAttachmentFileItem[];
};

export type TAttachmentSubSection = {
  label: string;
  groups: TAttachmentRefGroup[];
};

export type TAttachmentStage = {
  stageName: string;
  subSections: TAttachmentSubSection[];
};

export type TAttachmentFileAllBody = {
  planId: string | null;
  planNumber: string;
  procurementId: string;
  procurementNumber: string;
  projectName: string;
  departmentName: string;
  budgetYear: number | null;
  budget: number;
  supplyMethod: string;
  supplyMethodType: string;
  supplyMethodSpecialType: string;
  stages: TAttachmentStage[];
};

export type TAttachmentFileAllCriteria = {
  procurementId?: string;
};
