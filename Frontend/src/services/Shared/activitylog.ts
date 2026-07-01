import type { AxiosResponse } from 'axios';
import http from '@/configs/axios';

const path = `/api/su/logs`;

const onGetActivityLogByIdAsync = async (id: string, programName?: string): Promise<AxiosResponse> => {
  const params = programName ? { programName } : undefined;
  return await http.get(`${path}/${id}`, { params });
}

const ActivitylogService = {
  onGetActivityLogByIdAsync,
}

export default ActivitylogService;