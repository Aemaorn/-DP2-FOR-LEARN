import http from '@/configs/axios';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { Cm004Criteria, Cm004DisbursementBody } from '@/models/CM/cm004';
import type { Attachments } from '@/models/shared/uploadFile';

const getListAsync = async (criteria: Cm004Criteria) => {
  const [startDate, endDate] = ConvertStartToEndDate(criteria.startContractSignedDate, criteria.endContractSignedDate);

  const params: Cm004Criteria = {
    ...criteria,
    startContractSignedDate: startDate,
    endContractSignedDate: endDate,
  };

  if (params.status === 'all') {
    params.status = undefined;
  }

  return await http.get('api/contract/disbursement-approval', { params });
};

const getContractAsync = async (contractVendorId: string) =>
  await http.get(`/api/contract/${contractVendorId}/disbursement-approval`);

const createDisbursementAsync = async (contractVendorId: string, body: Cm004DisbursementBody) =>
  await http.post(`/api/contract/${contractVendorId}/disbursement-approval`, body);

const updateDisbursementAsync = async (contractVendorId: string, id: string, body: Cm004DisbursementBody) =>
  await http.put(`/api/contract/${contractVendorId}/disbursement-approval/${id}`, body);

const getDetailDisbursementAsync = async (contractVendorId: string, id: string) =>
  await http.get(`/api/contract/${contractVendorId}/disbursement-approval/${id}`);

const getPaymentTermAsync = async (contractVendorId: string) =>
  await http.get(`/api/contract/${contractVendorId}/delivery-acceptance/payment-term`);

const approveAsync = async (contractVendorId: string, id: string, remark?: string) =>
  await http.post(`/api/contract/${contractVendorId}/disbursement-approval/${id}/approve`, { remark });

const rejectAsync = async (contractVendorId: string, id: string, remark?: string) =>
  await http.post(`/api/contract/${contractVendorId}/disbursement-approval/${id}/reject`, { remark });

const getReviewDocumentAsync = async (contractVendorId: string, id: string) =>
  await http.get(`/api/contract/${contractVendorId}/disbursement-approval/${id}/review-document`);

const attachmentsAsync = async (contractVendorId: string, id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/contract/${contractVendorId}/disbursement-approval/${id}/attachment`, body);
};

const resetDocumentAsync = async (contractVendorId: string, id: string) =>
  await http.post(`/api/contract/${contractVendorId}/disbursement-approval/${id}/reset-document`, {});

const cm004Service = {
  getReviewDocumentAsync,
  getListAsync,
  getContractAsync,
  createDisbursementAsync,
  updateDisbursementAsync,
  getDetailDisbursementAsync,
  getPaymentTermAsync,
  approveAsync,
  rejectAsync,
  attachmentsAsync,
  resetDocumentAsync,
};

export default cm004Service
