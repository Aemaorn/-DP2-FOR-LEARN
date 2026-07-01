import http from '@/configs/axios';
import type { AxiosResponse } from 'axios';
import type { JorPor04Request, JorPor04SendAction } from '../../models/PP004/pp004Model';
import type { pp004status } from '../../enums/pp004';

const onCreateJorpor04Async = async (body: JorPor04Request): Promise<AxiosResponse<string>> =>
  await http.post<string>("/api/JorPor04", body);

const onGetJorpor004ByIdAsync = async (procurementId: string, id?: string): Promise<AxiosResponse<JorPor04Request>> => {
  if (id) {
    return await http.get<JorPor04Request>(`/api/procurement/${procurementId}/JorPor04/${id}`);
  }

  return await http.get<JorPor04Request>(`/api/procurement/${procurementId}/JorPor04/`);
}

const onUpdateJorpor004Async = async (procurementId: string, id: string, body: JorPor04Request, newStatus?: pp004status): Promise<AxiosResponse<{ newDocumentFileId?: string }>> => {
  const newBody = JSON.parse(JSON.stringify(body));

  if (newStatus) {
    newBody.requisition.status = newStatus;
  }

  return await http.put<{ newDocumentFileId?: string }>(`/api/procurement/${procurementId}/JorPor04/${id}`, newBody);
}

const onApproveAsync = async (id: string, body: JorPor04SendAction) =>
  await http.put(`/api/JorPor04/${id}/approve`, body);

const onRejectAsync = async (id: string, body: JorPor04SendAction) =>
  await http.put(`/api/JorPor04/${id}/reject`, body)

const onGetProcurementDataAsync = async (id: string) =>
  await http.get(`/api/JorPor04/Procurement/${id}`);

const getReviewDocumentAsync = async (id: string, procurementId: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/jorPor04/${id}/review-document`);

const resetDocumentAsync = async (procurementId: string, id: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/procurement/${procurementId}/jorPor04/${id}/reset-document`, {});

const PP004Service = {
  onCreateJorpor04Async,
  onGetJorpor004ByIdAsync,
  onUpdateJorpor004Async,
  onApproveAsync,
  onRejectAsync,
  onGetProcurementDataAsync,
  getReviewDocumentAsync,
  resetDocumentAsync,
};

export default PP004Service;