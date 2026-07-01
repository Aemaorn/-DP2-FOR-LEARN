export type TDocument = {
  id: string;
  name: string;
  readonly: boolean;
}

export interface DocumentVersion {
  fileId: string;
  version: string;
  createdAt: string;
  createdByName: string;
  isCurrent: boolean;
}