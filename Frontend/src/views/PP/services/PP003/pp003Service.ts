import http from '@/configs/axios';
import type { TPP003Body } from '../../models/PP003/pp003Model';
import type { AxiosResponse } from 'axios';

const onGetByIdAsync = async (procurementId: string, id: string): Promise<AxiosResponse<TPP003Body, any>> => {
  return http.get<TPP003Body>(`/api/procurement/${procurementId}/median-price/${id}`);
};

const onCreateAsync = async (procurementId: string, body: TPP003Body) => {
  return http.post(`/api/procurement/${procurementId}/median-price`, body);
};

const onUpdateAsync = async (procurementId: string, id: string, body: TPP003Body): Promise<AxiosResponse<{ newDocumentFileId?: string }>> => {
  return http.put<{ newDocumentFileId?: string }>(`/api/procurement/${procurementId}/median-price/${id}`, body);
};

const onApprovedByTypeAsync = async (procurementId: string, id: string, body: { remark?: string }) =>
  http.put(`/api/procurement/${procurementId}/median-price/${id}/approve`, body);

const onRejectByTypeAsync = async (procurementId: string, id: string, body: { remark?: string }) =>
  http.put(`/api/procurement/${procurementId}/median-price/${id}/reject`, body);

const onSetIsUnableToPerformDutiesAsync = async (procurementId: string, id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
  const body = {
    isUnableToPerformDuties,
    remark,
  }

  return http.put(`/api/procurement/${procurementId}/median-price/${id}/acceptor/${acceptorId}/duties-status`, body);
};

const onJorPorCommentAsync = async (procurementId: string, id: string, body: { remark?: string }) => {
  return http.put(`/api/procurement/${procurementId}/median-price/${id}/assignee-comment`, body);
};

const getReviewDocumentAsync = async (id: string, procurementId: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/medianPrice/${id}/review-document`);

const onRequestChangeOrCancelledAsync = async (procurementId: string, id: string, body: { reason: string, isCancel?: boolean }) => {
  return await http.post(`/api/procurement/${procurementId}/median-price/${id}/request-action`, body);
};

const restoreStateAsync = async (id: string, reason: string) => {
  const body = {
    reason: reason
  }

  return http.post(`api/median-price/${id}/restore-state`, body)
}

const resetDocumentAsync = async (procurementId: string, id: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/procurement/${procurementId}/median-price/${id}/reset-document`, {});

const PP003Service = {
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onApprovedByTypeAsync,
  onRejectByTypeAsync,
  onSetIsUnableToPerformDutiesAsync,
  onJorPorCommentAsync,
  getReviewDocumentAsync,
  onRequestChangeOrCancelledAsync,
  restoreStateAsync,
  resetDocumentAsync,
};

export default PP003Service;
