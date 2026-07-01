import type { Entrepreneurs, EntrepreneursPriceDetailBody, PrincipleApprovalRentalBody } from '@/models/PCM/PCM005/principleApprovalRental';
import http from '@/configs/axios';
import type { PP007GetWinnerCriteria } from '@/views/PP/models/PP007/pp007Model';
import type { AxiosResponse } from 'axios';
import type { EntrepreneurAttachments } from '@/models/shared/uploadFile';

const getByIdAsync = async (procurementId: string, id?: string) => {
  const endpoint = `/api/procurement/${procurementId}/principle-approval-rental`;
  const finalEndpoint = id ? `${endpoint}/${id}` : endpoint;

  return await http.get(finalEndpoint);
};

const updateAsync = async (procurementId: string, body: PrincipleApprovalRentalBody) =>
  await http.put(`/api/procurement/${procurementId}/principle-approval-rental/${body.id}`, body);

const approveAsync = async (procurementId: string, id: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval-rental/${id}/approve`, { remark });

const rejectAsync = async (procurementId: string, id: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval-rental/${id}/reject`, { remark });

const commentAsync = async (procurementId: string, id: string, remark: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval-rental/${id}/comment`, { remark });

const createEntrepreneurAsync = async (procurementId: string, principleApproveId: string, body: Entrepreneurs) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval-rental/${principleApproveId}/entrepreneurs`, body);

const getEntrepreneurAsync = async (procurementId: string, principleApproveId: string, id: string) =>
  await http.get(`/api/procurement/${procurementId}/principle-approval-rental/${principleApproveId}/entrepreneurs/${id}`);

const updateEntrepreneurAsync = async (procurementId: string, principleApproveId: string, id: string, body: Entrepreneurs) =>
  await http.put(`/api/procurement/${procurementId}/principle-approval-rental/${principleApproveId}/entrepreneurs/${id}`, body);

const getPriceDetailAsync = async (procurementId: string, principleApproveId: string, id: string) =>
  await http.get(`/api/procurement/${procurementId}/principle-approval-rental/${principleApproveId}/entrepreneurs/${id}/price-details`);

const createPriceDetailAsync = async (procurementId: string, principleApproveId: string, id: string, body: EntrepreneursPriceDetailBody) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval-rental/${principleApproveId}/entrepreneurs/${id}/price-details`, body);

const getListRentalWinnerAsync = async (params: PP007GetWinnerCriteria) => {
  return await http.get(`/api/procurement/${params.procurementId}/principle-approval-rental/${params.jp006Id}/entrepreneurs`, { params });
};

const getReviewDocumentAsync = async (procurementId: string, id: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/principle-approval-rental/${id}/review-document`, { params: { documentType } });

const setDutyAsync = async (procurementId: string, id: string, payload: { acceptorId: string, isUnableToPerformDuties: boolean, remark?: string }) => {
  const body = {
    isUnableToPerformDuties: payload.isUnableToPerformDuties,
    remark: payload.remark,
  };

  return await http.put(`/api/procurement/${procurementId}/principle-approval-rental/${id}/acceptor/${payload.acceptorId}/duties-status`, body);
};

const onUpsertAttachmentsAsync = async (id: string, attachments: Array<EntrepreneurAttachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/principle-approval-rental/entrepreneurs/${id}/attachments`, body);
};

const resetDocumentAsync = async (procurementId: string, id: string, documentType: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval-rental/${id}/reset-document`, { documentType });

const principleApprovalRentalService = {
  getByIdAsync,
  updateAsync,
  approveAsync,
  rejectAsync,
  commentAsync,
  createEntrepreneurAsync,
  getEntrepreneurAsync,
  updateEntrepreneurAsync,
  getPriceDetailAsync,
  createPriceDetailAsync,
  getListRentalWinnerAsync,
  setDutyAsync,
  getReviewDocumentAsync,
  onUpsertAttachmentsAsync,
  resetDocumentAsync,
};

export default principleApprovalRentalService
