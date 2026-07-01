import http from '@/configs/axios';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { ST008Criteria } from '@/models/ST/st008';

const onGetListAsync = async (criteria: ST008Criteria) => {
  const [start, end] = ConvertStartToEndDate(criteria.startDate, criteria.endDate);

  const params: ST008Criteria = {
    ...criteria,
    startDate: start,
    endDate: end,
  };
  return await http.get(`/api/st/st008`, { params });
};

const ST008Service = {
  onGetListAsync,
};

export default ST008Service;