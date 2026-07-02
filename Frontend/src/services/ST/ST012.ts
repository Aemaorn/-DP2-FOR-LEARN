import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt012Criteria, TSt012Detail, TSt012List } from '@/models/ST/st012';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSt012Criteria
): Promise<AxiosResponse<TDataTableResult<TSt012List>, any>> =>
  http.get<TDataTableResult<TSt012List>>(`/api/st/st012`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt012Detail, any>> =>
  http.get<TSt012Detail>(`/api/st/st012/${id}`);

const onCreateAsync = async (body: TSt012Detail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/st012`, body);

const onUpdateAsync = async (
  id: string,
  body: TSt012Detail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/st012/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st012/${id}`);

const ST012Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default ST012Service;
