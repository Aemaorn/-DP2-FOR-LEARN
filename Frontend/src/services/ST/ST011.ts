import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt011Criteria, TSt011Detail, TSt011List } from '@/models/ST/st011';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSt011Criteria
): Promise<AxiosResponse<TDataTableResult<TSt011List>, any>> =>
  http.get<TDataTableResult<TSt011List>>(`/api/st/st011`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt011Detail, any>> =>
  http.get<TSt011Detail>(`/api/st/st011/${id}`);

const onCreateAsync = async (body: TSt011Detail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/st011`, body);

const onUpdateAsync = async (
  id: string,
  body: TSt011Detail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/st011/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st011/${id}`);

const ST011Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default ST011Service;
