import http from '@/configs/axios';
import type { TUserManualDetail, TUserManualListItem } from '@/models/userManual';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (): Promise<AxiosResponse<TUserManualListItem[]>> =>
  http.get<TUserManualListItem[]>(`/api/user-manuals`);

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TUserManualDetail>> =>
  http.get<TUserManualDetail>(`/api/user-manuals/${id}`);

const UserManualService = {
  onGetListAsync,
  onGetByIdAsync,
};

export default UserManualService;
