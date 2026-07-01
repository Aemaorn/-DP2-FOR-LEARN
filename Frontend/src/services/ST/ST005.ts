import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TRawEmployeeDialog, TRawEmployeeDialogCriteria, TSt005Criteria, TSt005Detail, TSt005List, TUserDialog, TUserDialogCriteria } from '@/models/ST/st005';
import type { AxiosResponse } from 'axios';

const onGetRawEmployeeDialog = async (params: TRawEmployeeDialogCriteria): Promise<AxiosResponse<TDataTableResult<TRawEmployeeDialog>>> =>
  http.get<TDataTableResult<TRawEmployeeDialog>>("api/employee", { params });

const onGetUserDialogAsync = async (params: TUserDialogCriteria): Promise<AxiosResponse<TDataTableResult<TUserDialog>>> =>
  http.get<TDataTableResult<TUserDialog>>("api/st/st005", { params });

const onGetUserListAsync = async (params: TSt005Criteria): Promise<AxiosResponse<TDataTableResult<TSt005List>>> =>
  http.get<TDataTableResult<TSt005List>>("api/st/st005", { params });

const onGetUserByIdAsync = async (id: string): Promise<AxiosResponse<TSt005Detail>> =>
  http.get<TSt005Detail>(`api/st/st005/${id}`);

const onGetUserInfoAsync = async (): Promise<AxiosResponse<TSt005Detail>> =>
  http.get<TSt005Detail>(`api/st/st005`);

const onCreateUserAsync = async (body: TSt005Detail): Promise<AxiosResponse<string>> =>
  http.post<string>("api/st/st005", body);

const onUpdateUserByIdAsync = async (id: string, body: TSt005Detail): Promise<AxiosResponse<string>> =>
  http.put<string>(`api/st/st005/${id}`, body);

const onUpdateUserInfoAsync = async (body: TSt005Detail): Promise<AxiosResponse<string>> =>
  http.put<string>(`api/st/st005`, body);

const onDeleteUserAsync = async (id: string): Promise<AxiosResponse> =>
  http.delete<void>(`api/st/st005/${id}`);

const onUnlockUserAsync = async (id: string): Promise<AxiosResponse> =>
  http.post<void>(`api/st/st005/${id}/unlock`, {});

const ST005Service = {
  onGetRawEmployeeDialog,
  onGetUserDialogAsync,
  onGetUserListAsync,
  onGetUserByIdAsync,
  onGetUserInfoAsync,
  onCreateUserAsync,
  onUpdateUserByIdAsync,
  onUpdateUserInfoAsync,
  onDeleteUserAsync,
  onUnlockUserAsync,
};

export default ST005Service;