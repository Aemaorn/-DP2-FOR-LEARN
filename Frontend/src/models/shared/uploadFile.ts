import type { EntrepreneurType } from "@/enums/shared";

export type UploadFile = {
  id?: string;
  type: string;
  other?: string;
  fileList: UploadFileList[];
}

export type UploadFileList = {
  id?: string;
  order: number;
  isPerson?: boolean;
  fileName: string;
  fileId: string;
  hasPermission?: boolean;
}

export type Attachments = {
  sequence: number;
  remark?: string;
  documentTypeCode: string;
  fileAttachments: FileAttachment[];
}

export type EntrepreneurAttachments = {
  sequence: number;
  remark?: string;
  documentTypeCode: string;
  fileAttachments: FileAttachmentWithType[];
}

export type FileAttachmentWithType = {
  id?: string;
  fileId: string;
  fileName: string;
  sequence: number;
  isPublic: boolean;
  createdBy: string;
  type: EntrepreneurType;
}

export type FileAttachment = {
  id?: string;
  fileId: string;
  fileName: string;
  sequence: number;
  isPublic: boolean;
  createdBy: string;
}

export type comparingAttachments = {
  id?: string;
  fileId: string;
  fileName: string;
  sequence: number;
  isPublic: boolean;
}

export type OnlyFileAttachment = {
  id?: string;
  fileId: string;
  fileName: string;
  sequence: number;
}