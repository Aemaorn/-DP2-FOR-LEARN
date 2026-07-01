export type TUserManualListItem = {
  id: string;
  code: string;
  name: string;
};

export type TUserManualDetail = {
  id: string;
  code: string;
  name: string;
  previewPdfFileId?: string;
  previewPdfFileName?: string;
};
