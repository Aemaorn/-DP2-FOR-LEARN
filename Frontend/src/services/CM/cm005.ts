import http from '@/configs/axios';
import { CmContractTerminationStatus } from '@/enums/CM/cm005';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { Cm005ListCriteria, ContractTermination, ContractVendorListCriteria } from '@/models/CM/cm005';
import type { Attachments } from '@/models/shared/uploadFile';

export type UpdateCm005Response = {
  newDocumentFileId?: string;
};

const getListAsync = async (criteria: Cm005ListCriteria) => {
  const [, endDate] = ConvertStartToEndDate(undefined, criteria.contractSignedDate);

  const params = {
    ...criteria,
    contractSignedDate: endDate,
    status: criteria.status === CmContractTerminationStatus.All ? undefined : criteria.status,
  } as Cm005ListCriteria;


  return await http.get('/api/contract/contract-termination', { params });
}

const createAsync = async (contractId: string, body: ContractTermination) =>
  await http.post(`/api/contract/${contractId}/contract-termination`, body);

const updateAsync = async (contractId: string, id: string, body: ContractTermination) =>
  await http.put<UpdateCm005Response>(`/api/contract/${contractId}/contract-termination/${id}`, body);

const getDetailAsync = async (contractId: string, id: string) =>
  await http.get(`/api/contract/${contractId}/contract-termination/${id}`);

const approveAsync = async (contractId: string, id: string, remark?: string) =>
  await http.post(`/api/contract/${contractId}/contract-termination/${id}/approve`, { remark });

const rejectAsync = async (contractId: string, id: string, remark?: string) =>
  await http.post(`/api/contract/${contractId}/contract-termination/${id}/reject`, { remark });

const commentAsync = async (contractId: string, id: string, remark: string) =>
  await http.post(`/api/contract/${contractId}/contract-termination/${id}/comment`, { remark });

const setDutyAsync = async (contractId: string, id: string, payload: { acceptorId: string, isUnableToPerformDuties: boolean, remark?: string }) => {

  const body = {
    isUnableToPerformDuties: payload.isUnableToPerformDuties,
    remark: payload.remark,
  };

  return await http.put(`/api/contract/${contractId}/contract-termination/${id}/acceptor/${payload.acceptorId}/duties-status`, body);
};

const resetDocumentAsync = async (contractId: string, id: string) =>
  await http.post(`/api/contract/${contractId}/contract-termination/${id}/reset-document`, {});

const setIsProposedApproverAsync = async (contractId: string, id: string, isProposedApprover: boolean) => {
  return await http.put(`/api/contract/${contractId}/contract-termination/${id}/proposed-approver`, {
    isProposedApprover,
  });
};

const onUpsertAttachmentsAsync = async (contractId: string, id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/contract/${contractId}/contract-termination/${id}/attachments`, body);
};

const getContractVendorListAsync = async (params: ContractVendorListCriteria) =>
  await http.get('/api/contract-termination/contract-vendor', { params });

const cm005Service = {
  getListAsync,
  getContractVendorListAsync,
  createAsync,
  updateAsync,
  getDetailAsync,
  approveAsync,
  rejectAsync,
  commentAsync,
  setDutyAsync,
  setIsProposedApproverAsync,
  resetDocumentAsync,
  onUpsertAttachmentsAsync,
};

export default cm005Service
