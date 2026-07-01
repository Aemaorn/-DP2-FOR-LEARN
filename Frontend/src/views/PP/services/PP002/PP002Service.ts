import type { AxiosResponse } from 'axios';
import type { ActionTorDraft, ApproveTor, PP002Detail } from '../../models/PP002/pp002Model';
import http from '@/configs/axios';

const createAsync = async (procurementId: string, body: PP002Detail) =>
  await http.post(`/api/procurement/${procurementId}/tordraft`, body);

const updateAsync = async (id: string, procurementId: string, body: PP002Detail) =>
  await http.put(`/api/procurement/${procurementId}/tordraft/${id}`, body);

const getByIdAsync = async (id: string, procurementId: string) =>
  await http.get(`/api/procurement/${procurementId}/tordraft/${id}`);

const requestActionAsync = async (id: string, procurementId: string, body: ActionTorDraft) =>
  await http.post(`/api/procurement/${procurementId}/tordraft/${id}/request-action`, body);

const approveAsync = async (procurementId: string, body: ApproveTor) =>
  await http.post(`/api/procurement/${procurementId}/tordraft/${body.torDraftId}/approve`, body);

const rejectAsync = async (procurementId: string, body: ApproveTor) =>
  await http.post(`/api/procurement/${procurementId}/tordraft/${body.torDraftId}/reject`, body);

const updateDutieStatusAsync = async (id: string, procurementId: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) =>
  await http.put(`/api/procurement/${procurementId}/tordraft/${id}/acceptor/${acceptorId}/duties-status`, { isUnableToPerformDuties, remark });

const assigneeCommentAsync = async (id: string, procurementId: string, remark: string) =>
  await http.put(`/api/procurement/${procurementId}/tordraft/${id}/assignee-comment`, { remark });

const getReviewDocumentAsync = async (id: string, procurementId: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/tordraft/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (id: string, procurementId: string, documentType: string) => {
  const body = {
    DocumentType: documentType
  }

  return await http.post(`/api/procurement/${procurementId}/tordraft/${id}/reset-document`, body);
}

const restoreStateAsync = async (id: string, reason: string) => {
  const body = {
    reason: reason
  }

  return http.post(`api/tordraft/${id}/restore-state`, body)
}


const PP002Service = {
  createAsync,
  updateAsync,
  getByIdAsync,
  requestActionAsync,
  approveAsync,
  rejectAsync,
  updateDutieStatusAsync,
  assigneeCommentAsync,
  getReviewDocumentAsync,
  resetDocumentAsync,
  restoreStateAsync
};

export default PP002Service
