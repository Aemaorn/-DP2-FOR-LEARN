import type { PrincipleAnalysisImportResp, PrincipleBody, PrincipleConsiderationImportResp } from '@/models/PCM/PCM005/principle';
import http from '@/configs/axios';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { PrincipleStatus } from '@/enums/PCM005/principle';

const MapModel = (payload: PrincipleBody) => {
  const [start, end] = ConvertStartToEndDate(payload.rentalStartDate, payload.rentalEndDate);
  return {
    ...payload,
    rentalStartDate: start,
    rentalEndDate: end,
  } as PrincipleBody;
};

const createAsync = async (procurementId: string, payload: PrincipleBody) => {
  const body = MapModel(payload);

  return await http.post(`/api/procurement/${procurementId}/principle-approval`, body);
};

const updateAsync = async (procurementId: string, payload: PrincipleBody, statusBody?: PrincipleStatus) => {
  const body = MapModel(payload);

  const bodyWithStatus = {
    ...body,
    status: statusBody
  }

  return await http.put(`/api/procurement/${procurementId}/principle-approval/${bodyWithStatus.id}`, bodyWithStatus);
};

const getByIdAsync = async (procurementId: string, id?: string) => {
  const endpoint = `/api/procurement/${procurementId}/principle-approval`;
  const finalEndpoint = id ? `${endpoint}/${id}` : endpoint;

  return await http.get(finalEndpoint);
};

const approveAsync = async (procurementId: string, id: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval/${id}/approve`, { remark });

const rejectAsync = async (procurementId: string, id: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval/${id}/reject`, { remark });

const commentAsync = async (procurementId: string, id: string, remark: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval/${id}/comment`, { remark });

const getReviewDocumentAsync = async (procurementId: string, id: string) =>
  await http.get<string>(`/api/procurement/${procurementId}/principle-approval/${id}/review-document`);

const exportConsiderationAsync = async (procurementId: string, id: string | undefined) =>
  id
    ? await http.get(`/api/procurement/${procurementId}/principle-approval/${id}/export-consideration`, {
      responseType: 'blob'
    })
    : await http.get(`/api/procurement/${procurementId}/principle-approval/export-consideration`, {
      responseType: 'blob'
    });

const importConsiderationsAsync = async (procurementId: string, file: File) => {
  const formData = new FormData();
  formData.append('file', file);

  return await http.post<PrincipleConsiderationImportResp>(`/api/procurement/${procurementId}/principle-approval/import-consideration`, formData);
};

const importAnalysisBuildingAsync = async (procurementId: string, file: File) => {
  const formData = new FormData();
  formData.append('file', file);

  return await http.post<PrincipleAnalysisImportResp>(`/api/procurement/${procurementId}/principle-approval/import-analysis`, formData);
};

const exportAnalysisAsync = async (procurementId: string, id: string | undefined) =>
  id
    ? await http.get(`/api/procurement/${procurementId}/principle-approval/${id}/export-analysis`, {
      responseType: 'blob'
    })
    : await http.get(`/api/procurement/${procurementId}/principle-approval/export-analysis`, {
      responseType: 'blob'
    });

const resetDocumentAsync = async (procurementId: string, id: string) =>
  await http.post(`/api/procurement/${procurementId}/principle-approval/${id}/reset-document`, {});

const principleService = {
  createAsync,
  updateAsync,
  getByIdAsync,
  approveAsync,
  rejectAsync,
  commentAsync,
  getReviewDocumentAsync,
  exportConsiderationAsync,
  importConsiderationsAsync,
  exportAnalysisAsync,
  importAnalysisBuildingAsync,
  resetDocumentAsync,
};

export default principleService
