import http from '@/configs/axios';
import { rp001Status } from '@/enums/RP/rp001';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { rp001Body, rp001Criteria } from '@/models/RP/rp001';
import type { Attachments } from '@/models/shared/uploadFile';
import type { AxiosResponse } from 'axios';

const createAuditAndRevenueAsync = async (body: rp001Body) =>
  await http.post('api/report/audit-revenue', body);

const getListAuditAndRevenueAsync = async (criteria: rp001Criteria) => {
  const [, endDate] = ConvertStartToEndDate(undefined, criteria.documentDate);

  const params = {
    ...criteria,
    documentDate: endDate,
    status: criteria.status === rp001Status.All ? undefined : criteria.status,
  };

  return await http.get(`api/report/audit-revenue`, { params });
};

const getContractDraftVendorOver1mAsync = async (startDate: Date, endDate: Date) => {
  const params = {
    ContractSignedStartDate: startDate,
    ContractSignedEndDate: endDate
  };

  return await http.get("api/report/audit-revenue/contract-draft-vendor-over-1m", { params });
}

const getAuditAndRevenueByIdAsync = async (id: string) =>
  await http.get(`api/report/audit-revenue/${id}`);

const updateAuditAndRevenueAsync = async (id: string, body: rp001Body) =>
  await http.put(`api/report/audit-revenue/${id}`, body);

const deleteAuditAndRevenueByIdAsync = async (id: string) =>
  await http.delete(`api/report/audit-revenue/${id}`);

const approveAuditAndRevenueAsync = async (id: string, remark?: string) => {
  const body = {
    Remark: remark
  };

  return await http.post(`api/report/audit-revenue/${id}/approve`, body);
};

const rejectAuditAndRevenueAsync = async (id: string, remark: string) => {
  const body = {
    Remark: remark
  };

  return await http.post(`api/report/audit-revenue/${id}/reject`, body);
};

const deleteDetailByIdAsync = async (id: string, detailId: string) =>
  await http.delete(`api/report/audit-revenue/${id}/detail/${detailId}`);

const getReviewDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`api/report/audit-revenue/${id}/review-document`, { params: { documentType } });

const attachmentsAsync = async (id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/report/audit-revenue/${id}/attachment`, body);
};

const resetDocumentAsync = async (id: string, documentType: string) =>
  await http.post(`api/report/audit-revenue/${id}/reset-document`, { documentType });

const rp001Service = {
  getReviewDocumentAsync,
  createAuditAndRevenueAsync,
  getListAuditAndRevenueAsync,
  getContractDraftVendorOver1mAsync,
  getAuditAndRevenueByIdAsync,
  updateAuditAndRevenueAsync,
  deleteAuditAndRevenueByIdAsync,
  approveAuditAndRevenueAsync,
  rejectAuditAndRevenueAsync,
  deleteDetailByIdAsync,
  attachmentsAsync,
  resetDocumentAsync
};

export default rp001Service;