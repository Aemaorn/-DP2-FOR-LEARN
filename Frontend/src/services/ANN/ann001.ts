import http from '@/configs/axios';
import type {
  TAnnouncementInfoCriteria,
  TAnnouncementInfoResponse,
  TAnnouncementInfoImportRow,
  TAnnouncementInfoImportResponse,
  TAnnouncementInfoBody,
  TAnnouncementInfoDetail,
} from '@/models/ANN/ann001';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TAnnouncementInfoCriteria
): Promise<AxiosResponse<TAnnouncementInfoResponse>> =>
  http.get<TAnnouncementInfoResponse>('/api/announcement-info', { params });

const importAsync = async (
  rows: TAnnouncementInfoImportRow[]
): Promise<AxiosResponse<TAnnouncementInfoImportResponse>> =>
  http.post<TAnnouncementInfoImportResponse>('/api/announcement-info/import', rows);

const getByIdAsync = async (id: string): Promise<AxiosResponse<TAnnouncementInfoDetail>> =>
  http.get<TAnnouncementInfoDetail>(`/api/announcement-info/detail/${id}`);

const toFormData = (body: TAnnouncementInfoBody): FormData => {
  const form = new FormData();
  const append = (key: string, val: string | number | Date | File | null | undefined): void => {
    if (val === undefined || val === null || val === '') return;
    if (val instanceof File) { form.append(key, val); return; }
    if (val instanceof Date) { form.append(key, val.toISOString()); return; }
    form.append(key, String(val));
  };
  append('AnnouncementTitle', body.announcementTitle);
  append('AnnouncementName', body.announcementName);
  append('AnnouncementDate', body.announcementDate);
  append('BudgetAmount', body.budgetAmount);
  append('AnnouncementCategoryCode', body.announcementCategoryCode);
  append('SupplyMethodCode', body.supplyMethodCode);
  append('BudgetYear', body.budgetYear);
  append('Annotation', body.annotation);
  append('Remark', body.remark);
  append('Description', body.description);
  append('ExpectedDate', body.expectedDate);
  append('ReferencePrice', body.referencePrice);
  append('StartDate', body.startDate);
  append('EndDate', body.endDate);
  if (body.documentInfo) form.append('DocumentInfo', body.documentInfo);
  return form;
};

const createAsync = async (body: TAnnouncementInfoBody): Promise<AxiosResponse<string>> =>
  http.post<string>('/api/announcement-info', toFormData(body));

const updateAsync = async (id: string, body: TAnnouncementInfoBody): Promise<AxiosResponse<void>> =>
  http.put<void>(`/api/announcement-info/${id}`, toFormData(body));

const deleteAsync = async (id: string): Promise<AxiosResponse<void>> =>
  http.delete<void>(`/api/announcement-info/${id}`);

const AnnouncementInfoService = {
  onGetListAsync,
  importAsync,
  getByIdAsync,
  createAsync,
  updateAsync,
  deleteAsync,
};

export default AnnouncementInfoService;
