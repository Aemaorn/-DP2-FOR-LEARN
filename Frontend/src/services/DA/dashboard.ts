import http from '@/configs/axios';

const getDataAsync = async (id: string) =>
  await http.get(`api/dashboard/procurements/${id}/timeline`);

const dashboardService = {
  getDataAsync,
};

export default dashboardService;
