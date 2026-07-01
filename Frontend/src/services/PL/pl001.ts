import http from '@/configs/axios';
import { PlanStatus } from '@/enums/plan';
import { ProcurementPlanType } from '@/enums/procurement';
import type { PlanActionReq, PlanBody, TPL001Criteria, TPL001ListResponse } from '@/models/PL/pl001';
import type { Attachments } from '@/models/shared/uploadFile';
import type { AxiosResponse } from 'axios';

const getListAsync = async (params: TPL001Criteria): Promise<AxiosResponse<TPL001ListResponse>> => {
  const parameter = {
    ...params,
    type: params.type === ProcurementPlanType.All ? undefined : params.type,
    status: params.status === PlanStatus.All ? undefined : params.status,
  };

  return await http.get<TPL001ListResponse>('/api/plan', { params: parameter });
};

const createAsync = async (body: PlanBody): Promise<AxiosResponse<string>> =>
  await http.post<string>('/api/plan', body);

const updateAsync = async (id: string, body: PlanBody): Promise<AxiosResponse<{ newPlanDocumentFileId?: string; newPlanAnnouncementDocumentFileId?: string }>> =>
  await http.put<{ newPlanDocumentFileId?: string; newPlanAnnouncementDocumentFileId?: string }>(`/api/plan/${id}`, body);

const getReviewDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/plan/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/plan/${id}/reset-document`, { documentType });

const getByIdAsync = async (id: string): Promise<AxiosResponse<PlanBody>> =>
  await http.get<PlanBody>(`/api/plan/${id}`);

const deleteAsync = async (id: string): Promise<AxiosResponse> =>
  await http.delete(`/api/plan/${id}`);

const actionAsync = async (id: string, reqBody: PlanActionReq): Promise<AxiosResponse> =>
  await http.put(`/api/plan/action/${id}`, reqBody);

const requestActionAsync = async (id: string, reason?: string, isChange: boolean = false) =>
  await http.post(`/api/plan/${id}/request-action`, { reason, isChange });

const attachmentsAsync = async (planId: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/plan/${planId}/attachment`, body);
};

const exportExcelPlanAsync = async (criteria: TPL001Criteria) =>
  await http.get('/api/plan/export-plan', {
    params: {
      ...criteria,
      type: criteria.type === ProcurementPlanType.All ? undefined : criteria.type,
      status: criteria.status === PlanStatus.All ? undefined : criteria.status,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*'
    }
  });

const exportExcelEGPAsync = async (criteria: TPL001Criteria) =>
  await http.get('/api/plan/export-egp', {
    params: {
      ...criteria,
      type: criteria.type === ProcurementPlanType.All ? undefined : criteria.type,
      status: criteria.status === PlanStatus.All ? undefined : criteria.status,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*'
    }
  });

const planService = {
  getListAsync,
  createAsync,
  updateAsync,
  getReviewDocumentAsync,
  getByIdAsync,
  deleteAsync,
  actionAsync,
  requestActionAsync,
  attachmentsAsync,
  exportExcelPlanAsync,
  exportExcelEGPAsync,
  resetDocumentAsync,
};

export default planService
