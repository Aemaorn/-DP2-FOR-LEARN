import http from '@/configs/axios';
import { AC01Status } from '@/enums/AC/ac01';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { TA01Detail, TAC01Criteria } from '@/models/ACC/acc001';

const getListAsync = async (params: TAC01Criteria) => {
  const [advancePaymentDateFrom, advancePaymentDateTo] = ConvertStartToEndDate(params.advancePaymentDateFrom, params.advancePaymentDateTo);
  const [dateFrom, dateTo] = ConvertStartToEndDate(params.dateFrom, params.dateTo);

  const parameter = {
    ...params,
    advancePaymentDateFrom,
    advancePaymentDateTo,
    dateFrom,
    dateTo,
    status: params.status === AC01Status.All ? undefined : params.status,
  };

  return await http.get('api/expense-disbursement', { params: parameter });
}

const getByIdAsync = async (id: string) =>
  await http.get(`api/expense-disbursement/${id}`);

const updateByIdAsync = async (id: string, body: TA01Detail) =>
  await http.put(`api/expense-disbursement/${id}`, body);

const approveAsync = async (id: string, remarks?: string) => {
  const body = {
    remarks
  }

  return await http.put(`api/expense-disbursement/${id}/approve`, body);
}

const rejectAsync = async (id: string, remarks?: string) => {
  const body = {
    remarks
  }

  return await http.put(`api/expense-disbursement/${id}/reject`, body);
}

const ac01Service = {
  getListAsync,
  getByIdAsync,
  updateByIdAsync,
  approveAsync,
  rejectAsync
}

export default ac01Service;