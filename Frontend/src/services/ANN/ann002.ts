import http from '@/configs/axios';
import type {
  TAnnouncementReportCriteria,
  TAnnouncementReportResponse,
  TAnnouncementReportBody,
  TAnnouncementReportDetail,
  TAnnouncementReportImportRow,
  TAnnouncementReportImportResponse,
} from '@/models/ANN/ann002';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TAnnouncementReportCriteria
): Promise<AxiosResponse<TAnnouncementReportResponse>> =>
  http.get<TAnnouncementReportResponse>('/api/announcement-report', { params });

const getByIdAsync = async (id: string): Promise<AxiosResponse<TAnnouncementReportDetail>> =>
  http.get<TAnnouncementReportDetail>(`/api/announcement-report/detail/${id}`);

const toFormData = (body: TAnnouncementReportBody): FormData => {
  const form = new FormData();
  const append = (key: string, val: string | number | File | null | undefined): void => {
    if (val === undefined || val === null || val === '') return;
    if (val instanceof File) { form.append(key, val); return; }
    form.append(key, String(val));
  };
  append('Year', body.year);
  append('Discretion', body.discretion);
  append('AnnouncementReportTypeCode', body.announcementReportTypeCode);
  if (body.documentInfo) form.append('DocumentInfo', body.documentInfo);
  return form;
};

const createAsync = async (body: TAnnouncementReportBody): Promise<AxiosResponse<string>> =>
  http.post<string>('/api/announcement-report', toFormData(body));

const updateAsync = async (id: string, body: TAnnouncementReportBody): Promise<AxiosResponse<void>> =>
  http.put<void>(`/api/announcement-report/${id}`, toFormData(body));

const deleteAsync = async (id: string): Promise<AxiosResponse<void>> =>
  http.delete<void>(`/api/announcement-report/${id}`);

const importAsync = async (
  rows: TAnnouncementReportImportRow[]
): Promise<AxiosResponse<TAnnouncementReportImportResponse>> =>
  http.post<TAnnouncementReportImportResponse>('/api/announcement-report/import', rows);

const AnnouncementReportService = {
  onGetListAsync,
  getByIdAsync,
  createAsync,
  updateAsync,
  deleteAsync,
  importAsync,
};

export default AnnouncementReportService;
