import http from '@/configs/axios';
import { Cm006Status } from '@/enums/CM/cm006';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { Cm006Criteria, Cm006Detail, Cm006DialogCriteria, Cm006DialogItem, Cm006GuaranteeReturn, Cm006List } from '@/models/CM/cm006';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { Attachments, OnlyFileAttachment } from '@/models/shared/uploadFile';

export type UpdateCm006Response = {
  newApprovalDocumentFileId?: string;
  newReturnDocumentFileId?: string;
};

const getListAsync = async (criteria: Cm006Criteria) => {
  const params: Cm006Criteria = {
    ...criteria,
    status: criteria.status === Cm006Status.All ? undefined : criteria.status,
  };

  return await http.get<TDataTableResult<Cm006List>>('/api/contract/contract-guarantee-return', { params });
};

const getDialogListAsync = async (criteria: Cm006DialogCriteria) => {
  return await http.get<TDataTableResult<Cm006DialogItem>>('/api/contract/contract-guarantee-return/Dialog', { params: criteria });
};

const getByIdAsync = async (contractVendorId: string, id?: string) =>
  await http.get<Cm006Detail>(`/api/contract/${contractVendorId}/contract-guarantee-return/${id ?? ''}`);

const onCreateAsync = async (contractVendorId: string, payload: Cm006GuaranteeReturn) => {
  const [startDate] = ConvertStartToEndDate(new Date())

  const body = {
    ...payload,
    guaranteeReturnDate: startDate,

  } as Cm006GuaranteeReturn;

  return await http.post(`/api/contract/${contractVendorId}/contract-guarantee-return`, body);
};

const onUpdateAsync = async (contractVendorId: string, id: string, body: Cm006GuaranteeReturn) => {
  return await http.put<UpdateCm006Response>(`/api/contract/${contractVendorId}/contract-guarantee-return/${id}`, body);
};

const onApproveAsync = async (contractVendorId: string, id: string, remark?: string) => {
  return await http.post(`/api/contract/${contractVendorId}/contract-guarantee-return/${id}/approve`, { remark });
};

const onRejectedAsync = async (contractVendorId: string, id: string, remark?: string) => {
  return await http.post(`/api/contract/${contractVendorId}/contract-guarantee-return/${id}/reject`, { remark });
};

const attachmentsAsync = async (id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/contract/contract-guarantee-return/${id}/attachment`, body);
};

const getReviewDocumentAsync = async (contractId: string, id: string, documentType: string) =>
  await http.get(`/api/contract/${contractId}/contract-guarantee-return/${id}/review-document`, { params: { documentType } });

const onSetDutiesAsync = async (id: string, body: { acceptorId: string, isUnableToPerformDuties: boolean, remark?: string }) => {
  return await http.put(`/api/contract/contract-guarantee-return/${id}/acceptor/${body.acceptorId}/set-duties`, body);
};

const resetDocumentAsync = async (contractVendorId: string, id: string, documentType: string) =>
  await http.post(`/api/contract/${contractVendorId}/contract-guarantee-return/${id}/reset-document`, { documentType });

const onAccountingApproveAsync = async (contractVendorId: string, id: string, body: { remark?: string }) => {
  return await http.put(`/api/contract/${contractVendorId}/contract-guarantee-return/${id}/accounting-approve`, body);
};

const onAccountingRejectAsync = async (contractVendorId: string, id: string, body: { remark?: string }) => {
  return await http.put(`/api/contract/${contractVendorId}/contract-guarantee-return/${id}/accounting-reject`, body);
};

const sendEmailAsync = async (
  contractVendorId: string,
  id: string,
  email: string,
  emailTemplate: string,
  attachments: OnlyFileAttachment[]
) => {
  const body = {
    email,
    emailTemplate,
    emailAttachments: attachments,
  };

  return await http.post(
    `/api/contract/${contractVendorId}/contract-guarantee-return/${id}/send-email`,
    body
  );
};

const Cm006Service = {
  getReviewDocumentAsync,
  getListAsync,
  getDialogListAsync,
  getByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onApproveAsync,
  onRejectedAsync,
  attachmentsAsync,
  onSetDutiesAsync,
  resetDocumentAsync,
  onAccountingApproveAsync,
  onAccountingRejectAsync,
  sendEmailAsync,
};

export default Cm006Service;