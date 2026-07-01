import http from '@/configs/axios';
import type { TSearchAllCriteria } from "@/models/searchCriteria";

const getSearchAllAsync = async (params: TSearchAllCriteria) => {
  return http.get('/api/summary-all/programs', { params });
}

export const searchAllService = {
  getSearchAllAsync,
};