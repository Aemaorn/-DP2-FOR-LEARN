import http from '@/configs/axios';
import { RP002Status } from '@/enums/RP/rp002';
import type { RP002Body, RP002Criteria, RP002ListResponse } from '@/models/RP/rp002';
import type { Attachments } from '@/models/shared/uploadFile';

export type UpdateRp002Response = {
  newDocumentFileId?: string;
};

const GetListAsync = async (criteria: RP002Criteria) => {
  const params: RP002Criteria = {
    ...criteria,
    status: criteria.status === RP002Status.All ? undefined : criteria.status,
  };

  return await http.get<RP002ListResponse>(`/api/report/contract-completion-by-quarter`, { params });
};

const DeleteByIdAsync = async (id: string) =>
  await http.delete(`/api/report/contract-completion-by-quarter/${id}`);

const getByIdAsync = async (id: string) =>
  await http.get(`api/report/contract-completion-by-quarter/${id}`);

const createAsync = async (body: RP002Body) =>
  await http.post('api/report/contract-completion-by-quarter', body);

const updateAsync = async (id: string, body: RP002Body) =>
  await http.put<UpdateRp002Response>(`api/report/contract-completion-by-quarter/${id}`, body);

const getContractCompleteAsync = async (contractSignedStartDate?: Date, contractSignedEndDate?: Date) =>
  await http.get('api/report/contract-completion-by-quarter/contract-draft-vendor-completion', { params: { contractSignedStartDate, contractSignedEndDate } });

const getContractSummaryAsync = async (id: string) =>
  await http.get(`api/report/contract-completion-by-quarter/summary-completion-by-quarter/${id}`);

const approveAsync = async (id: string, remark?: string) =>
  await http.post(`api/report/contract-completion-by-quarter/${id}/approve`, { remark });

const rejectAsync = async (id: string, remark?: string) =>
  await http.post(`api/report/contract-completion-by-quarter/${id}/reject`, { remark });

const getReviewDocumentAsync = async (id: string, type: string) => {
  return await http.get(`api/report/contract-completion-by-quarter/${id}/review-document`, { params: { type } });
};

const deleteDetailByIdAsync = async (id: string, detailId: string) =>
  await http.delete(`api/report/contract-completion-by-quarter/${id}/detail/${detailId}`);

const attachmentsAsync = async (id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/report/contract-completion-by-quarter/${id}/attachment`, body);
};

const exportDetailAsync = async (id: string) =>
  await http.get(`api/report/contract-completion-by-quarter/${id}/export`, { responseType: 'blob' });

const resetDocumentAsync = async (id: string) =>
  await http.post(`api/report/contract-completion-by-quarter/${id}/reset-document`, {});

const RP002Service = {
  GetListAsync,
  DeleteByIdAsync,
  getByIdAsync,
  createAsync,
  updateAsync,
  getContractCompleteAsync,
  getContractSummaryAsync,
  approveAsync,
  rejectAsync,
  getReviewDocumentAsync,
  deleteDetailByIdAsync,
  attachmentsAsync,
  exportDetailAsync,
  resetDocumentAsync,
};

export default RP002Service;