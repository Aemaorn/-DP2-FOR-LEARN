import http from '@/configs/axios';
import { Pcm007Status } from '@/enums/pcm007';
import type { Pcm007ActionReq, Pcm007Criteria, Pcm007Detail, Pcm007ListResponse } from '@/models/PCM/pcm007';
import type { Attachments } from '@/models/shared/uploadFile';
import type { AxiosResponse } from 'axios';

const getListAsync = async (params: Pcm007Criteria): Promise<AxiosResponse<Pcm007ListResponse>> => {
  const parameter = {
    ...params,
    status: params.status === Pcm007Status.All ? undefined : params.status,
  };

  return await http.get<Pcm007ListResponse>('/api/pw184', { params: parameter });
};

const createAsync = async (body: Pcm007Detail): Promise<AxiosResponse<string>> =>
  await http.post<string>('/api/Pw184', body);

const updateAsync = async (id: string, body: Pcm007Detail): Promise<AxiosResponse> =>
  await http.put(`/api/pw184/${id}`, body);

const getByIdAsync = async (id: string): Promise<AxiosResponse<Pcm007Detail>> =>
  await http.get<Pcm007Detail>(`/api/Pw184/${id}`);

const deleteAsync = async (id: string): Promise<AxiosResponse> =>
  await http.delete(`/api/pw184/${id}`);

const actionAsync = async (id: string, reqBody: Pcm007ActionReq): Promise<AxiosResponse> =>
  await http.put(`/api/pw184/action/${id}`, reqBody);

const attachmentsAsync = async (id: string, attachments: Attachments[]): Promise<AxiosResponse> => {
  return await http.put(`/api/pw184/${id}/attachment`, { attachments });
};

const Pcm007Service = {
  getListAsync,
  createAsync,
  updateAsync,
  getByIdAsync,
  deleteAsync,
  actionAsync,
  attachmentsAsync,
};

export default Pcm007Service;
