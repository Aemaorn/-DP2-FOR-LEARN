import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt013Criteria, TSt013Detail, TSt013List } from '@/models/ST/st013';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSt013Criteria
): Promise<AxiosResponse<TDataTableResult<TSt013List>, any>> =>
  http.get<TDataTableResult<TSt013List>>(`/api/st/st013`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt013Detail, any>> =>
  http.get<TSt013Detail>(`/api/st/st013/${id}`);

const onCreateAsync = async (body: TSt013Detail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/st013`, body);

const onUpdateAsync = async (
  id: string,
  body: TSt013Detail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/st013/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st013/${id}`);

const ST013Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default ST013Service;
