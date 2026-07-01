import type { CmCriteria } from '@/models/CM/cm';
import http from '@/configs/axios';

const getListAsync = async (params: CmCriteria) =>
  await http.get('/api/contract', { params });

const cmService = {
  getListAsync,
};

export default cmService