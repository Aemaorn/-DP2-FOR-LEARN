import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { ListSuSection, SuSection, SuSectionCriteria } from '@/models/ST/st099';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  criteria: SuSectionCriteria
): Promise<AxiosResponse<TDataTableResult<ListSuSection>, unknown>> =>
  http.get<TDataTableResult<ListSuSection>>(`/api/st/sections`, { params: criteria });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<SuSection, unknown>> =>
  http.get<SuSection>(`/api/st/sections/${id}`);

const onUpdateAsync = async (id: string, payload: SuSection): Promise<AxiosResponse<string, unknown>> =>
  http.put<string>(`/api/st/sections/${id}`, payload);

const onCreateAsync = async (payload: SuSection): Promise<AxiosResponse<string, unknown>> =>
  http.post<string>(`/api/st/sections`, payload);

const onDeleteByIdAsync = async (id: string): Promise<AxiosResponse<SuSection, unknown>> =>
  http.delete<SuSection>(`/api/st/sections/${id}`);

const ST099Service = {
  onGetListAsync,
  onGetByIdAsync,
  onUpdateAsync,
  onCreateAsync,
  onDeleteByIdAsync
};

export default ST099Service;