import http from '@/configs/axios';
import type {
  TAnnouncementSorKorRorCriteria,
  TAnnouncementSorKorRorResponse,
  TAnnouncementSorKorRorBody,
  TAnnouncementSorKorRorDetail,
  TAnnouncementSorKorRorImportRow,
  TAnnouncementSorKorRorImportResponse,
} from '@/models/ANN/ann003';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TAnnouncementSorKorRorCriteria
): Promise<AxiosResponse<TAnnouncementSorKorRorResponse>> =>
  http.get<TAnnouncementSorKorRorResponse>('/api/announcement-sor-kor-ror', { params });

const getByIdAsync = async (id: string): Promise<AxiosResponse<TAnnouncementSorKorRorDetail>> =>
  http.get<TAnnouncementSorKorRorDetail>(`/api/announcement-sor-kor-ror/detail/${id}`);

const toFormData = (body: TAnnouncementSorKorRorBody): FormData => {
  const form = new FormData();
  const append = (key: string, val: string | number | File | null | undefined): void => {
    if (val === undefined || val === null || val === '') return;
    if (val instanceof File) { form.append(key, val); return; }
    form.append(key, String(val));
  };
  append('Year', body.year);
  append('Month', body.month);
  append('Amount', body.amount);
  append('DepartmentTypeCode', body.departmentTypeCode);
  if (body.documentInfo) form.append('DocumentInfo', body.documentInfo);
  return form;
};

const createAsync = async (body: TAnnouncementSorKorRorBody): Promise<AxiosResponse<string>> =>
  http.post<string>('/api/announcement-sor-kor-ror', toFormData(body));

const updateAsync = async (id: string, body: TAnnouncementSorKorRorBody): Promise<AxiosResponse<void>> =>
  http.put<void>(`/api/announcement-sor-kor-ror/${id}`, toFormData(body));

const deleteAsync = async (id: string): Promise<AxiosResponse<void>> =>
  http.delete<void>(`/api/announcement-sor-kor-ror/${id}`);

const importAsync = async (
  rows: TAnnouncementSorKorRorImportRow[]
): Promise<AxiosResponse<TAnnouncementSorKorRorImportResponse>> =>
  http.post<TAnnouncementSorKorRorImportResponse>('/api/announcement-sor-kor-ror/import', rows);

const AnnouncementSorKorRorService = {
  onGetListAsync,
  getByIdAsync,
  createAsync,
  updateAsync,
  deleteAsync,
  importAsync,
};

export default AnnouncementSorKorRorService;
