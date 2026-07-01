import type { AxiosResponse } from 'axios';
import type { PP007Detail, PP007Entrepreneurs, PP007GetWinnerCriteria } from '../../models/PP007/pp007Model';
import http from '@/configs/axios';
import type { EntrepreneurAttachments } from '@/models/shared/uploadFile';

const getJp006ByidAsync = async (procurementId: string, id?: string) => {
  if (id) {
    return await http.get(`/api/procurement/${procurementId}/jp006/${id}`);
  }

  return await http.get(`/api/procurement/${procurementId}/jp006`);
}

const getListWinnerAsync = async (params: PP007GetWinnerCriteria) =>
  await http.get(`/api/procurement/${params.procurementId}/jp006/${params.jp006Id}/getlistwinner`, { params });

const updateDutiesStatusAsync = async (procurementId: string, jp006Id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
  const body = {
    isUnableToPerformDuties,
    remark
  }

  return await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/acceptor/${acceptorId}/duties-status`, body);
}

const createJp006Async = async (procurementId: string, body: PP007Detail) =>
  await http.post(`api/procurement/${procurementId}/jp006`, body);

const onGetJp006ByIdAsync = async (procurementId: string, jp006Id: string) =>
  await http.get(`api/procurement/${procurementId}/jp006/${jp006Id}`);

const updateJp006Async = async (procurementId: string, jp006Id: string, body: PP007Detail): Promise<AxiosResponse<{ newJp006DocumentFileId?: string; newWinnerDocumentFileId?: string }>> =>
  await http.put<{ newJp006DocumentFileId?: string; newWinnerDocumentFileId?: string }>(`api/procurement/${procurementId}/jp006/${jp006Id}`, body);

const approveJp006Async = async (procurementId: string, jp006Id: string, operationUserId: string, remark?: string) => {
  const body = {
    remark,
    operationUserId,
  };

  return await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/approve`, body);
};

const rejectJp006Async = async (procurementId: string, jp006Id: string, remark?: string) => {
  const body = {
    remark
  };

  return await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/reject`, body);
};

const sendApproveJp006Async = async (procurementId: string, jp006Id: string, body: PP007Detail) =>
  await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/send-approve`, body);

const recallToCommentJp006Async = async (procurementId: string, jp006Id: string, body: PP007Detail) =>
  await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/recall-to-comment`, body);

const assigneeCommentJp006Async = async (procurementId: string, jp006Id: string, remark?: string) => {
  const body = {
    remark
  };

  return await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/assignee-comment`, body);
}

const getReviewDocumentAsync = async (id: string, procurementId: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/jp006/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (procurementId: string, jp006Id: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/procurement/${procurementId}/jp006/${jp006Id}/reset-document`, { documentType });

const onUpsertAttachmentsAsync = async (id: string, attachments: Array<EntrepreneurAttachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/jp006/${id}/attachment`, body);
};

const createEntrepreneursAsync = async (purchaseOrderId: string, procurementId: string, body: PP007Entrepreneurs) =>
  await http.post(`/api/procurement/${procurementId}/jp006/${purchaseOrderId}/entrepreneurs`, body);

const updateEntrepreneurCheckAsync = async (procurementId: string, jp006Id: string, id: string, body: PP007Entrepreneurs) =>
  await http.put(`api/procurement/${procurementId}/jp006/${jp006Id}/jp006-entrepreneurs/${id}`, body);

const pp007service = {
  getJp006ByidAsync,
  getListWinnerAsync,
  updateDutiesStatusAsync,
  createJp006Async,
  onGetJp006ByIdAsync,
  updateJp006Async,
  sendApproveJp006Async,
  recallToCommentJp006Async,
  approveJp006Async,
  rejectJp006Async,
  assigneeCommentJp006Async,
  updateEntrepreneurCheckAsync,
  getReviewDocumentAsync,
  onUpsertAttachmentsAsync,
  createEntrepreneursAsync,
  resetDocumentAsync,
}

export default pp007service;