import http from '@/configs/axios';
import type { WorklistCriteria } from '@/models/WorkList/worklist';

const getListAsync = async (params: WorklistCriteria) =>
  await http.get('api/worklist', { params });

const worklistService = {
  getListAsync,
};

export default worklistService
