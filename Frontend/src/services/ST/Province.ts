import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TProvinceCriteria, TProvinceDetail, TProvinceList } from '@/models/ST/province';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TProvinceCriteria
): Promise<AxiosResponse<TDataTableResult<TProvinceList>, any>> =>
  http.get<TDataTableResult<TProvinceList>>(`/api/st/province`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TProvinceDetail, any>> =>
  http.get<TProvinceDetail>(`/api/st/province/${id}`);

const onCreateAsync = async (body: TProvinceDetail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/province`, body);

const onUpdateAsync = async (
  id: string,
  body: TProvinceDetail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/province/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/province/${id}`);

const ProvinceService = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default ProvinceService;
