import http from '@/configs/axios';
import type { Shareholder } from '@/views/PP/models/PP006/pp006Model';
import type { AxiosResponse } from "axios";

const getDefaultShareholdersAsync = async (vendorId: string): Promise<AxiosResponse<Shareholder[]>> =>
  http.get<Shareholder[]>(`api/st/vendor/${vendorId}/shareholder`);

const shareholdersService = {
  getDefaultShareholdersAsync,
}

export default shareholdersService;