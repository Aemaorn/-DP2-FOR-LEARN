import http from '@/configs/axios'
import type { AxiosResponse } from 'axios'
import type { TP001SendAction, TPP001Detail } from '../../models/PP001/pp001Model'

const createPP001Async = async (body: TPP001Detail): Promise<AxiosResponse<string>> =>
  http.post<string>("api/appointments", body);

const getPP001ByIdAsync = async (id: string): Promise<AxiosResponse<TPP001Detail>> =>
  http.get<TPP001Detail>(`api/appointments/${id}`);

const updatePP001ByIdAsync = async (id: string, body: TPP001Detail): Promise<AxiosResponse<{ newDocumentFileId?: string }>> =>
  http.put<{ newDocumentFileId?: string }>(`api/appointments/${id}`, body);

const approvePP001Async = async (body: TP001SendAction) =>
  http.post(`api/appointments/approve`, body);

const rejectPP001Async = async (body: TP001SendAction) =>
  http.post(`api/appointments/reject`, body);

const requestActionPP001Async = async (id: string, isEdit: boolean, reason: string): Promise<AxiosResponse<string>> => {
  const body = {
    isEdit: isEdit ?? false,
    isCancel: isEdit == false ? true : false,
    reason: reason
  }

  return http.post<string>(`api/appointments/${id}/request-action`, body)
}

const getReviewDocumentAsync = async (id: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/appointments/${id}/review-document`);

const restoreStateAsync = async (id: string, reason: string) => {
  const body = {
    reason: reason
  }

  return http.post(`api/appointments/${id}/restore-state`, body)
}

const resetDocumentAsync = async (id: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`api/appointments/${id}/reset-document`, {});

const appointmentService = {
  createPP001Async,
  getPP001ByIdAsync,
  updatePP001ByIdAsync,
  approvePP001Async,
  rejectPP001Async,
  requestActionPP001Async,
  getReviewDocumentAsync,
  restoreStateAsync,
  resetDocumentAsync,
}

export default appointmentService;
