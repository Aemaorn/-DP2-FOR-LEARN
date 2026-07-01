import http from '@/configs/axios';
import { Cm007Status } from '@/enums/CM/cm007';
import type { Cm007CreateRequest, Cm007Criteria, Cm007Detail, Cm007DialogCriteria, Cm007DialogItem, Cm007List } from '@/models/CM/cm007';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { Attachments } from '@/models/shared/uploadFile';

const getListAsync = async (criteria: Cm007Criteria) => {
  const params: Cm007Criteria = {
    ...criteria,
    status: criteria.status === Cm007Status.All ? undefined : criteria.status,
  };

  return await http.get<TDataTableResult<Cm007List>>('/api/contract/contract-draft-vendor-edit', { params });
};

const getDialogListAsync = async (criteria: Cm007DialogCriteria) => {
  return await http.get<TDataTableResult<Cm007DialogItem>>('/api/contract/contract-draft-vendor-edit/Dialog', { params: criteria });
};

const getByIdAsync = async (id: string) =>
  await http.get<Cm007Detail>(`/api/contract/contract-draft-vendor-edit/${id}`);

const onCreateAsync = async (body: Cm007CreateRequest) => {
  return await http.post('/api/contract/contract-draft-vendor-edit', body);
};

const onUpdateAsync = async (id: string, body: Cm007Detail) => {
  return await http.put(`/api/contract/contract-draft-vendor-edit/${id}`, body);
};

const onSubmitCommitteeApprovalAsync = async (id: string, documentDate?: Date) => {
  return await http.put(`/api/contract/contract-draft-vendor-edit/${id}/submit-committee-approval`, { documentDate });
};

const onApproveAsync = async (id: string, remark?: string) => {
  return await http.post(`/api/contract/contract-draft-vendor-edit/${id}/approve`, { remark });
};

const onRejectAsync = async (id: string, remark?: string) => {
  return await http.post(`/api/contract/contract-draft-vendor-edit/${id}/reject`, { remark });
};

const onAssignAsync = async (id: string, body: object) => {
  return await http.put(`/api/contract/contract-draft-vendor-edit/${id}/assign`, body);
};

const onCommentAsync = async (id: string, remark?: string) => {
  return await http.post(`/api/contract/contract-draft-vendor-edit/${id}/comment`, { remark });
};

const getReviewDocumentAsync = async (id: string, documentType: string) =>
  await http.get(`/api/contract/contract-draft-vendor-edit/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (id: string, documentType: string) =>
  await http.post(`/api/contract/contract-draft-vendor-edit/${id}/reset-document`, { documentType });

const setDutiesStatusAsync = async (id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) =>
  await http.put(`/api/contract/contract-draft-vendor-edit/${id}/acceptor/${acceptorId}/duties-status`, { isUnableToPerformDuties, remark });

const deleteAsync = async (id: string) =>
  await http.delete(`/api/contract/contract-draft-vendor-edit/${id}`);

const attachmentsAsync = async (id: string, attachments: Attachments[]) =>
  await http.put(`/api/contract/contract-draft-vendor-edit/${id}/attachment`, { attachments });

const Cm007Service = {
  getListAsync,
  getDialogListAsync,
  getByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onSubmitCommitteeApprovalAsync,
  onApproveAsync,
  onRejectAsync,
  onAssignAsync,
  onCommentAsync,
  getReviewDocumentAsync,
  resetDocumentAsync,
  setDutiesStatusAsync,
  deleteAsync,
  attachmentsAsync,
};

export default Cm007Service;
