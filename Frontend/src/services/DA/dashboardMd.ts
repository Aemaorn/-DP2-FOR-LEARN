import http from '@/configs/axios';
import type { DAMdCriteria, DaMdTableCriteria } from '@/models/DA/dashboardMd';

const getDashBoardAsync = async (params: DAMdCriteria) =>
  await http.get(`api/dashboard/summary`, { params });

const getDashBoardTableAsync = async (params: DaMdTableCriteria) =>
  await http.get(`api/dashboard/tables`, { params });

export const dashBoardService = {
  getDashBoardAsync,
  getDashBoardTableAsync
}