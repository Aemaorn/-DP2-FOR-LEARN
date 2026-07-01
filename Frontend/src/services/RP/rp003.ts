import http from '@/configs/axios';
import { rp003SupplyMethod } from '@/enums/RP/rp003';
import type { TRP003Criteria } from '@/models/RP/rp003';

const getListAsync = (criteria: TRP003Criteria) => {
  const parameter = {
    ...criteria,
    supplyMethodCode: criteria.supplyMethodCode === rp003SupplyMethod.ALL ? undefined : criteria.supplyMethodCode,
  }

  return http.get('/api/contract-draft/list-with-supply-method-counts', { params: parameter });
}

const exporeExcelAsync = async (criteria: TRP003Criteria, columns: number[]) =>
  await http.get('/api/contract-draft/export-vendors', {
    params: {
      ...criteria,
      supplyMethodCode:
        criteria.supplyMethodCode === rp003SupplyMethod.ALL
          ? undefined : criteria.supplyMethodCode,
      columns: columns.join(','),
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*'
    }
  });

const rp003Service = {
  getListAsync,
  exporeExcelAsync
}

export default rp003Service;