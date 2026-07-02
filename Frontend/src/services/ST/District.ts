import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TDistrictCriteria, TDistrictDetail, TDistrictList } from '@/models/ST/district';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TDistrictCriteria
): Promise<AxiosResponse<TDataTableResult<TDistrictList>, any>> =>
  http.get<TDataTableResult<TDistrictList>>(`/api/st/district`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TDistrictDetail, any>> =>
  http.get<TDistrictDetail>(`/api/st/district/${id}`);

const onCreateAsync = async (body: TDistrictDetail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/district`, body);

const onUpdateAsync = async (
  id: string,
  body: TDistrictDetail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/district/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/district/${id}`);

const DistrictService = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default DistrictService;
