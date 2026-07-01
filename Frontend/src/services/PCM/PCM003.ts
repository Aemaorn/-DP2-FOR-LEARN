import http from '@/configs/axios';
import { Pcm003Status } from '@/enums/pcm003';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { Pcm003ActionReq, Pcm003Criteria, Pcm003Detail, Pcm003ListResponse } from '@/models/PCM/pcm003';
import type { defaultAcceptorCriteria, OperationBody } from '@/models/shared/operations';
import type { Attachments } from '@/models/shared/uploadFile';
import type { AxiosResponse } from 'axios';

const getListAsync = async (params: Pcm003Criteria): Promise<AxiosResponse<Pcm003ListResponse>> => {
  const [startDate, endDate] = ConvertStartToEndDate(params.actionAtFrom, params.actionAtTo);

  const parameter = {
    ...params,
    status: params.status === Pcm003Status.All ? undefined : params.status,
    actionAtFrom: startDate,
    actionAtTo: endDate,
  };

  return await http.get<Pcm003ListResponse>('/api/p79Clause2', { params: parameter });
};

const createAsync = async (body: Pcm003Detail): Promise<AxiosResponse<string>> =>
  await http.post<string>('/api/p79Clause2', body);

const updateAsync = async (id: string, body: Pcm003Detail): Promise<AxiosResponse<{ newApprovalRequestDocumentFileId?: string; newWinnerAnnounceDocumentFileId?: string }>> =>
  await http.put<{ newApprovalRequestDocumentFileId?: string; newWinnerAnnounceDocumentFileId?: string }>(`/api/p79Clause2/${id}`, body);

const getByIdAsync = async (id: string, isDisabledLoad: boolean = false): Promise<AxiosResponse<Pcm003Detail>> =>
  await http.get<Pcm003Detail>(`/api/p79Clause2/${id}`, {
    headers: {
      isDisabledLoad,
    }
  });

const deleteAsync = async (id: string): Promise<AxiosResponse> =>
  await http.delete(`/api/p79Clause2/${id}`);

const getOperationsDefaultAcceptorAsync = async (params: defaultAcceptorCriteria, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> =>
  await http.get('api/operations/default-acceptor', { params, headers: { isDisabledLoad } });

const actionAsync = async (id: string, reqBody: Pcm003ActionReq): Promise<AxiosResponse> =>
  await http.put(`/api/p79Clause2/action/${id}`, reqBody);

const attachmentsAsync = async (planId: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/p79Clause2/${planId}/attachment`, body);
}

const getReviewDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/P79Clause2/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/p79Clause2/${id}/reset-document`, { documentType });

const exportExcelAsync = async (params: Pcm003Criteria): Promise<AxiosResponse<Blob>> => {
  const [startDate, endDate] = ConvertStartToEndDate(params.actionAtFrom, params.actionAtTo);

  return await http.get<Blob>('/api/p79Clause2/export-excel', {
    params: {
      ...params,
      status: params.status === Pcm003Status.All ? undefined : params.status,
      actionAtFrom: startDate,
      actionAtTo: endDate,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*',
    },
  });
};

const Pcm003Service = {
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
  exportExcelAsync,
};

export default Pcm003Service