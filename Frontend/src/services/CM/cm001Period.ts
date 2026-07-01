import http from '@/configs/axios';
import type { AcceptorType } from '@/enums/participants';
import type { CM001PeriodBody } from '@/models/CM/cm001';
import type { Attachments } from '@/models/shared/uploadFile';

const baseEndpoint = (deliveryAcceptanceId: string) => `/api/delivery-acceptance/${deliveryAcceptanceId}/period`;


const onGetByIdAsync = async (deliveryAcceptanceId: string, id?: string) => {
  const endpoint = id
    ? `${baseEndpoint(deliveryAcceptanceId)}/${id}`
    : baseEndpoint(deliveryAcceptanceId);
  return await http.get(endpoint);
};

const onCreateAsync = async (deliveryAcceptanceId: string, body: CM001PeriodBody) => {
  return await http.post(`${baseEndpoint(deliveryAcceptanceId)}`, body);
}

const onUpdateAsync = async (deliveryAcceptanceId: string, id: string, body: CM001PeriodBody) => {
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}`, body);
};

const onApproveAsync = async (deliveryAcceptanceId: string, id: string, body: { group: AcceptorType.AcceptanceCommittee | AcceptorType.Approver, remark?: string }) => {
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}/approve`, body);
};

const onRejectAsync = async (deliveryAcceptanceId: string, id: string, body: { group: AcceptorType.AcceptanceCommittee | AcceptorType.Approver, remark?: string }) => {
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}/reject`, body);
};

const onJorPorRejectedAsync = async (deliveryAcceptanceId: string, id: string, body: { remark?: string }) => {
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}/assignee-reject`, body);
};

const onCommentAsync = async (deliveryAcceptanceId: string, id: string, body: { remark: string }) => {
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}/assignee-comment`, body);
}

const setDutyAsync = async (deliveryAcceptanceId: string, id: string, payload: { acceptorId: string, isUnableToPerformDuties: boolean, remark?: string }) => {

  const body = {
    isUnableToPerformDuties: payload.isUnableToPerformDuties,
    remark: payload.remark,
  };

  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}/acceptor/${payload.acceptorId}/duties-status`, body);
};

const getReviewDocumentAsync = async (deliveryAcceptanceId: string, id: string) =>
  await http.get(`${baseEndpoint(deliveryAcceptanceId)}/${id}/review-document`);

const resetDocumentAsync = async (deliveryAcceptanceId: string, id: string) =>
  await http.post(`${baseEndpoint(deliveryAcceptanceId)}/${id}/reset-document`, {});

const onAccountingApproveAsync = async (id: string, body: { remark?: string }) => {
  return await http.put(`api/delivery-acceptance/period/${id}/approve`, body);
}

const onAccountingRejectAsync = async (deliveryAcceptanceId: string, id: string, body: { remark?: string }) => {
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${id}/accounting-reject`, body);
};

const attachmentsAsync = async (deliveryAcceptanceId: string, periodId: string, attachments: Array<Attachments>) => {
  const body = { attachments };
  return await http.put(`${baseEndpoint(deliveryAcceptanceId)}/${periodId}/attachment`, body);
};

const onDeleteAsync = async (deliveryAcceptanceId: string, id: string) => {
  return await http.delete(`api/delivery-acceptance/${deliveryAcceptanceId}/period/${id}`);
};

const CM001PeriodService = {
  getReviewDocumentAsync,
  onGetByIdAsync,
  onUpdateAsync,
  setDutyAsync,
  onApproveAsync,
  onRejectAsync,
  onCommentAsync,
  onJorPorRejectedAsync,
  onCreateAsync,
  resetDocumentAsync,
  onAccountingApproveAsync,
  onAccountingRejectAsync,
  onDeleteAsync,
  attachmentsAsync
};

export default CM001PeriodService;