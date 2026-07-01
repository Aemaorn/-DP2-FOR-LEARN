import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TGetListRawEmpPosition, TSt001Criteria, TSt001Detail, TSt001List, TUser } from '@/models/ST/st001';
import { HttpStatusCode, type AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSt001Criteria
): Promise<AxiosResponse<TDataTableResult<TSt001List>, any>> =>
  http.get<TDataTableResult<TSt001List>>(`/api/st/st001`, {
    params,
    paramsSerializer: { indexes: null },
  });

const onGetByIdAsync = async (delegatorId: string): Promise<AxiosResponse<TSt001Detail, any>> =>
  http.get<TSt001Detail>(`/api/st/st001/${delegatorId}`);

const onCreateAsync = async (body: TSt001Detail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/st001`, body);

const onUpdateAsync = async (
  delegatorId: string,
  body: TSt001Detail
): Promise<AxiosResponse<void, any>> => http.put<void>(`/api/st/st001/${delegatorId}`, body);

const onDeleteAsync = async (delegatorId: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st001/${delegatorId}`);

const onDeleteDelegateeAsync = async (
  delegatorId: string,
  delegateeId: string
): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st001/${delegatorId}/delegatee/${delegateeId}`);

const onGetUserByIdAsync = async (id: string): Promise<AxiosResponse<TUser>> =>
  http.get<TUser>(`api/st/st001/user/${id}`);

const onGetBuPositionByEmpCodeAsync = async (employeeCode: string): Promise<AxiosResponse<TGetListRawEmpPosition[]>> =>
  http.get<TGetListRawEmpPosition[]>(`/api/st/st001/business-unit-position/${employeeCode}`);

const exportReport = async (fileName: string, params: TSt001Criteria) => {
  const { data, status } = await http.get(`/api/st/st001/export`, {
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
      type: data.contentType ?? 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
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

const ST001Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
  onDeleteDelegateeAsync,
  onGetUserByIdAsync,
  onGetBuPositionByEmpCodeAsync,
  exportReport,
};

export default ST001Service;
