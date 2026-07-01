import http from '@/configs/axios';
import type { AxiosResponse } from 'axios';

export type TSuVendorShareholder = {
  firstName: string | null;
  lastName: string | null;
  isJuristic: boolean | null;
  isDirector: boolean | null;
  isShareholder: boolean | null;
};

const getByVendorIdAsync = async (vendorId: string): Promise<AxiosResponse<TSuVendorShareholder[]>> =>
  http.get<TSuVendorShareholder[]>(`/api/su/vendor/${vendorId}/shareholders`);

const deleteByVendorIdAsync = async (vendorId: string): Promise<AxiosResponse<void>> =>
  http.delete<void>(`api/st/vendor/${vendorId}/shareholders`);

const suVendorShareholdersService = {
  getByVendorIdAsync,
  deleteByVendorIdAsync,
};

export default suVendorShareholdersService;
