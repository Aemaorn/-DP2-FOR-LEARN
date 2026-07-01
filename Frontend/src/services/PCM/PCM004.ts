import http from '@/configs/axios';
import { Pcm004Status } from '@/enums/pcm004';
import type { Pcm004ActionReq, Pcm004Criteria, Pcm004Detail, Pcm004ListResponse } from '@/models/PCM/pcm004';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import type { Attachments } from '@/models/shared/uploadFile';
import type { AxiosResponse } from 'axios';

const getListAsync = async (params: Pcm004Criteria): Promise<AxiosResponse<Pcm004ListResponse>> => {
  const parameter = {
    ...params,
    status: params.status === Pcm004Status.All ? undefined : params.status,
  };

  return await http.get<Pcm004ListResponse>('/api/pPettyCash', { params: parameter });
};

const createAsync = async (body: Pcm004Detail): Promise<AxiosResponse<string>> =>
  await http.post<string>('/api/pPettyCash', body);

const updateAsync = async (id: string, body: Pcm004Detail): Promise<AxiosResponse<{ newApprovalRequestDocumentFileId?: string }>> =>
  await http.put<{ newApprovalRequestDocumentFileId?: string }>(`/api/pPettyCash/${id}`, body);

const getByIdAsync = async (id: string): Promise<AxiosResponse<Pcm004Detail>> =>
  await http.get<Pcm004Detail>(`/api/pPettyCash/${id}`);

const deleteAsync = async (id: string): Promise<AxiosResponse> =>
  await http.delete(`/api/pPettyCash/${id}`);

const getOperationsDefaultAcceptorAsync = async (params: defaultAcceptorCriteria, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> =>
  await http.get('api/operations/default-acceptor', { params, headers: { isDisabledLoad } });

const actionAsync = async (id: string, reqBody: Pcm004ActionReq): Promise<AxiosResponse> =>
  await http.put(`/api/pPettyCash/action/${id}`, reqBody);

const attachmentsAsync = async (planId: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/pPettyCash/${planId}/attachment`, body);
}

const getReviewDocumentAsync = async (id: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/PPettyCash/${id}/review-document`);

const resetDocumentAsync = async (id: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/pPettyCash/${id}/reset-document`, {});

const Pcm004Service = {
  getListAsync,
  createAsync,
  updateAsync,
  getByIdAsync,
  deleteAsync,
  getOperationsDefaultAcceptorAsync,
  actionAsync,
  attachmentsAsync,
  getReviewDocumentAsync,
  resetDocumentAsync,
};

export default Pcm004Service