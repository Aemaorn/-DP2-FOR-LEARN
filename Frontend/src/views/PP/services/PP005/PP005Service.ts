import http from '@/configs/axios';
import type { PP005Payload, PP005Detail, Evaluations } from '../../models/PP005/pp005Model';
import type { AxiosResponse } from 'axios';

const onGetJp005ByIdAsync = async (procurementId: string, id?: string) => {
  return http.get<PP005Detail>(`/api/procurement/${procurementId}/jp005/${id ?? ''}`);
};

const onCreateAsync = async (procurementId: string, data: PP005Detail) => {
  const body = MapPP005Payload(data);

  return http.post<string>(`/api/procurement/${procurementId}/jp005`, body);
};

const onUpdateAsync = async (procurementId: string, id: string, data: PP005Detail): Promise<AxiosResponse<{ newApprovalDocumentFileId?: string; newCommandDocumentFileId?: string }>> => {
  const body = MapPP005Payload(data);

  return http.put<{ newApprovalDocumentFileId?: string; newCommandDocumentFileId?: string }>(`/api/procurement/${procurementId}/jp005/${id}`, body);
};

const onApprovedAsync = async (procurementId: string, id: string, body: { remark?: string }) => {
  return http.put(`/api/procurement/${procurementId}/jp005/${id}/approve`, body);
};

const onRejectedAsync = async (procurementId: string, id: string, body: { remark?: string }) => {
  return http.put(`/api/procurement/${procurementId}/jp005/${id}/reject`, body);
}

const MapPP005Payload = (data: PP005Detail): PP005Payload => ({
  purchaseRequisitionId: data.purchaseRequisition.purchaseRequisitionId,
  documentDate: data.documentDate,
  evaluations: {
    ...data.jp005 as Evaluations,
    egpProjectNumber: data.jp005.egpProjectNumber,
  },
  procurementCommittees: data.jp005.procurementCommittees,
  inspectionCommittees: data.jp005.inspectionCommittees,
  maintenanceInspectionCommittee: data.jp005.maintenanceInspectionCommittee,
  constructionSupervisor: data.jp005.constructionSupervisor,
  acceptors: data.jp005.acceptors,
  jp005ApprovalDocumentId: data.jp005.jp005ApprovalDocumentId,
  isJp005ApprovalDocumentIdReplaced: data.jp005.isJp005ApprovalDocumentIdReplaced,
  jp005CommandDocumentId: data.jp005.jp005CommandDocumentId,
  isJp005CommandDocumentIdReplaced: data.jp005.isJp005CommandDocumentIdReplaced,
  status: data.status,
  egpProjectNumber: data.jp005.egpProjectNumber,
  procurementSuppliesDivision: data.jp005.procurementSuppliesDivision,
  jorPorNumber: data.jorPorNumber,
  prNumber: data.purchaseRequisition.requisition.prNumber,
  telephone: data.purchaseRequisition.requisition.telephone,
  description: data.purchaseRequisition.requisition.description,
  priceReasonablenessInfo: data.purchaseRequisition.requisition.priceReasonablenessInfo,
  medianPriceAmount: data.purchaseRequisition.requisition.medianPriceAmount,
} as PP005Payload);

const getReviewDocumentAsync = async (id: string, procurementId: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/jp005/${id}/review-document`, { params: { documentType } });

const onSendEditToPurchaseRequisitionAsync = async (procurementId: string, body: { remark?: string }) => {
  return http.put(`/api/procurement/${procurementId}/jp005/send-edit-to-purchase-requisition`, body);
};

const resetDocumentAsync = async (procurementId: string, id: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/procurement/${procurementId}/jp005/${id}/reset-document`, { documentType });

const PP005Service = {
  onGetJp005ByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onApprovedAsync,
  onRejectedAsync,
  getReviewDocumentAsync,
  onSendEditToPurchaseRequisitionAsync,
  resetDocumentAsync,
};

export default PP005Service;