import http from '@/configs/axios';
import { Pcm002Status } from '@/enums/pcm002';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { Pcm002ActionReq, Pcm002Criteria, Pcm002Detail, Pcm002ListResponse } from '@/models/PCM/pcm002';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import type { Attachments } from '@/models/shared/uploadFile';
import type { AxiosResponse } from 'axios';

const getListAsync = async (params: Pcm002Criteria): Promise<AxiosResponse<Pcm002ListResponse>> => {
  const [startDate, endDate] = ConvertStartToEndDate(params.actionAtFrom, params.actionAtTo);

  const parameter = {
    ...params,
    status: params.status === Pcm002Status.All ? undefined : params.status,
    actionAtFrom: startDate,
    actionAtTo: endDate,
  };

  return await http.get<Pcm002ListResponse>('/api/pw119', { params: parameter });
};

const createAsync = async (body: Pcm002Detail): Promise<AxiosResponse<string>> =>
  await http.post<string>('/api/pw119', body);

const updateAsync = async (id: string, body: Pcm002Detail): Promise<AxiosResponse<{ newApprovalRequestDocumentFileId?: string; newWinnerAnnounceDocumentFileId?: string }>> =>
  await http.put<{ newApprovalRequestDocumentFileId?: string; newWinnerAnnounceDocumentFileId?: string }>(`/api/pw119/${id}`, body);

const getByIdAsync = async (id: string): Promise<AxiosResponse<Pcm002Detail>> =>
  await http.get<Pcm002Detail>(`/api/pw119/${id}`);

const deleteAsync = async (id: string): Promise<AxiosResponse> =>
  await http.delete(`/api/pw119/${id}`);

const getOperationsDefaultAcceptorAsync = async (params: defaultAcceptorCriteria, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> =>
  await http.get('api/operations/default-acceptor', { params, headers: { isDisabledLoad } });

const actionAsync = async (id: string, reqBody: Pcm002ActionReq): Promise<AxiosResponse> =>
  await http.put(`/api/pw119/action/${id}`, reqBody);

const attachmentsAsync = async (planId: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/pw119/${planId}/attachment`, body);
}

const getReviewDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/pw119/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/pw119/${id}/reset-document`, { documentType });

const exportExcelAsync = async (params: Pcm002Criteria): Promise<AxiosResponse<Blob>> => {
  const [startDate, endDate] = ConvertStartToEndDate(params.actionAtFrom, params.actionAtTo);

  return await http.get<Blob>('/api/pw119/export-excel', {
    params: {
      ...params,
      status: params.status === Pcm002Status.All ? undefined : params.status,
      actionAtFrom: startDate,
      actionAtTo: endDate,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*',
    },
  });
};

const pcm002Service = {
  getListAsync,
  createAsync,
  updateAsync,
  getByIdAsync,
  deleteAsync,
  getOperationsDefaultAcceptorAsync,
  actionAsync,
  getReviewDocumentAsync,
  attachmentsAsync,
  resetDocumentAsync,
  exportExcelAsync,
};

export default pcm002Service