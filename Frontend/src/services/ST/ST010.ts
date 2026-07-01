import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt010Criteria, TSt010Detail, TSt010List } from '@/models/ST/st010';
import type { Attachments } from '@/models/shared/uploadFile';
import type { TUser } from '@/models/ST/st001';
import { HttpStatusCode, type AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSt010Criteria
): Promise<AxiosResponse<TDataTableResult<TSt010List>, any>> =>
  http.get<TDataTableResult<TSt010List>>(`/api/st/st010`, {
    params,
    paramsSerializer: { indexes: null },
  });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt010Detail, any>> =>
  http.get<TSt010Detail>(`/api/st/st010/${id}`);

const onCreateAsync = async (body: TSt010Detail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/st010`, body);

const onUpdateAsync = async (
  id: string,
  body: TSt010Detail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/st010/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st010/${id}`);

const onDeleteSecretaryAsync = async (
  id: string,
  secretaryId: string
): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st010/${id}/secretary/${secretaryId}`);

const onGetUserByIdAsync = async (id: string): Promise<AxiosResponse<TUser>> =>
  http.get<TUser>(`/api/st/st010/user/${id}`);

const onUpsertAttachmentsAsync = async (
  id: string,
  attachments: Attachments[]
): Promise<AxiosResponse<void, any>> =>
  http.put<void>(`/api/st/st010/${id}/attachment`, { attachments });

const exportReport = async (fileName: string, params: TSt010Criteria) => {
  const { data, status } = await http.get(`/api/st/st010/export`, {
    params,
    paramsSerializer: { indexes: null },
  });

  if (status === HttpStatusCode.Ok) {
    const base64Content = data;
    const binaryString = window.atob(base64Content);
    const bytes = new Uint8Array(binaryString.length);

    for (let i = 0; i < binaryString.length; i++) {
      bytes[i] = binaryString.charCodeAt(i);
    }

    const blob = new Blob([bytes], {
      type: data.contentType ?? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    });

    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');

    a.href = url;
    a.download = `${fileName}.xlsx`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    a.remove();
  }
};

const onGetAllPositionsByBusinessUnitAsync = async (): Promise<
  AxiosResponse<{ businessUnitId: string; positionId: string; label: string }[], any>
> => http.get(`/api/positions-by-business-unit`);

const ST010Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
  onDeleteSecretaryAsync,
  onGetUserByIdAsync,
  onUpsertAttachmentsAsync,
  exportReport,
  onGetAllPositionsByBusinessUnitAsync,
};

export default ST010Service;
