import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  St006Criteria,
  St006DefaultResponse,
  St006Detail,
  St006DetailUpdate,
  St006GroupDDL,
  St006ListResponse,
  St006SubGroupDDL,
} from '@/models/ST/st006';
import type { AxiosResponse } from 'axios';

const createAsync = async (body: St006Detail): Promise<AxiosResponse<string>> => {
  return await http.post<string>('api/st/st006', body);
};

const updateAsync = async (
  id: string,
  body: St006Detail
): Promise<AxiosResponse<St006DetailUpdate>> => {
  return await http.put<St006DetailUpdate>(`api/st/st006/${id}`, body);
};

const getListAsync = async (
  params: St006Criteria
): Promise<AxiosResponse<TDataTableResult<St006ListResponse>>> => {
  return await http.get<TDataTableResult<St006ListResponse>>('api/st/st006', { params });
};

const getDetailAsync = async (id: string): Promise<AxiosResponse<St006DetailUpdate>> => {
  return await http.get<St006DetailUpdate>(`api/st/st006/${id}`);
};

const deleteAsync = async (id: string): Promise<AxiosResponse> => {
  return await http.delete(`api/st/st006/${id}`);
};

const getGroupDropdownAsync = async (): Promise<AxiosResponse<St006GroupDDL[]>> => {
  return await http.get<St006GroupDDL[]>('api/st/st006/group');
};

const getSubGroupDropdownAsync = async (id: string): Promise<AxiosResponse<St006SubGroupDDL[]>> => {
  return await http.get<St006SubGroupDDL[]>(`api/st/st006/subgroup/${id}`);
};

const getDefaultAsync = async (
  id: string,
  parentId?: string | null
): Promise<AxiosResponse<St006DefaultResponse>> => {
  return await http.get<St006DefaultResponse>(`api/st/st006/default/${id}`, {
    params: parentId ? { parentId } : undefined,
  });
};

const ST006Service = {
  createAsync,
  updateAsync,
  getListAsync,
  getDetailAsync,
  deleteAsync,
  getGroupDropdownAsync,
  getSubGroupDropdownAsync,
  getDefaultAsync,
};

export default ST006Service;
