import http from '@/configs/axios';
import type { TContractDraftBody } from '../../models/PP0010/ContractDraft';
import type { AxiosResponse } from 'axios';
import type { TContractDraftStatus } from '../../enums/pp010';
import type { EntrepreneurAttachments } from '@/models/shared/uploadFile';

const getContractDraftVendorList = async (procurementId: string) =>
  await http.get(`api/procurement/${procurementId}/contract-draft`);

const getContractDraftByVendorId = async (procurementId: string, contractDraftId: string, vendorId: string) =>
  await http.get(`api/procurement/${procurementId}/contract-draft/${contractDraftId}/vendor/${vendorId}`);

const updateContractDraft = async (procurementId: string, contractDraftId: string, vendorId: string, body: TContractDraftBody, newStatus?: TContractDraftStatus, isSaveDraft?: boolean): Promise<AxiosResponse<{ newContractDraftDocumentFileId?: string; newApprovalContractDraftDocumentFileId?: string; newConfidentialContractDraftDocumentFileId?: string }>> => {
  const newBody = JSON.parse(JSON.stringify(body));
  newBody.isSaveDraft = isSaveDraft;

  if (newStatus) {
    newBody.status = newStatus;
  }

  return await http.put<{ newContractDraftDocumentFileId?: string; newApprovalContractDraftDocumentFileId?: string; newConfidentialContractDraftDocumentFileId?: string }>(`api/procurement/${procurementId}/contract-draft/${contractDraftId}/vendor/${vendorId}`, newBody);
}

const approveContractDraft = async (procurementId: string, contractDraftId: string, vendorId: string, remark?: string) =>
  await http.put(`api/procurement/${procurementId}/contract-draft/${contractDraftId}/vendor/${vendorId}/approve`, { remark });

const rejectContractDraft = async (procurementId: string, contractDraftId: string, vendorId: string, remark?: string) =>
  await http.put(`api/procurement/${procurementId}/contract-draft/${contractDraftId}/vendor/${vendorId}/reject`, { remark });

const getReviewDocumentAsync = async (id: string, procurementId: string, vendorId: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/contractDraft/${id}/vendor/${vendorId}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (procurementId: string, contractDraftId: string, vendorId: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/procurement/${procurementId}/contract-draft/${contractDraftId}/vendor/${vendorId}/reset-document`, { documentType });

const onUpsertAttachmentsAsync = async (id: string, attachments: Array<EntrepreneurAttachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/contractDraft/vendor/${id}/attachments`, body);
};

interface AttachmentGroupRequest {
  documentName: string;
  description?: string;
  sequence: number;
  fileIds: string[];
}

interface MergeAttachmentsRequest {
  contractNumber: string;
  attachments: AttachmentGroupRequest[];
}

const mergeAttachmentsAsync = async (request: MergeAttachmentsRequest): Promise<void> => {
  const response = await http.post(
    '/api/contract-draft/merge-attachments',
    request,
    { responseType: 'blob' }
  );

  const url = window.URL.createObjectURL(new Blob([response.data], { type: 'application/pdf' }));
  window.open(url, '_blank');
  setTimeout(() => window.URL.revokeObjectURL(url), 1000);
};

const contractDraftService = {
  getContractDraftVendorList,
  getContractDraftByVendorId,
  updateContractDraft,
  approveContractDraft,
  rejectContractDraft,
  getReviewDocumentAsync,
  onUpsertAttachmentsAsync,
  mergeAttachmentsAsync,
  resetDocumentAsync,
};

export default contractDraftService;