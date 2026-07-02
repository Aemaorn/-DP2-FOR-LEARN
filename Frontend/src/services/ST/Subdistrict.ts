import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSubdistrictCriteria, TSubdistrictDetail, TSubdistrictList } from '@/models/ST/subdistrict';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSubdistrictCriteria
): Promise<AxiosResponse<TDataTableResult<TSubdistrictList>, any>> =>
  http.get<TDataTableResult<TSubdistrictList>>(`/api/st/subdistrict`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSubdistrictDetail, any>> =>
  http.get<TSubdistrictDetail>(`/api/st/subdistrict/${id}`);

const onCreateAsync = async (body: TSubdistrictDetail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/subdistrict`, body);

const onUpdateAsync = async (
  id: string,
  body: TSubdistrictDetail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/subdistrict/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/subdistrict/${id}`);

const SubdistrictService = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default SubdistrictService;
