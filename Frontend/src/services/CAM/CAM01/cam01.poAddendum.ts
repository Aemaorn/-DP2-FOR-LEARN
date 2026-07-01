import http from '@/configs/axios';
import type { Cam01PoAddendumBody, Cam01PoAddendumPayload } from '@/models/CAM/CAM01/cam01.poAddendum';

const onCreateAsync = async (amendmentId: string, payload: Cam01PoAddendumBody) => {
  const body = mapToPayload(payload);

  return await http.post(`/api/contract-amendment/${amendmentId}/po-addendum`, body);
};

const onUpdateAsync = async (amendmentId: string, id: string, payload: Cam01PoAddendumBody) => {
  const body = mapToPayload(payload);

  return await http.put(`/api/contract-amendment/${amendmentId}/po-addendum/${id}`, body);
};

const onGetByIdAsync = async (amendmentId: string, id?: string) => {
  return await http.get<Cam01PoAddendumBody>(`/api/contract-amendment/${amendmentId}/po-addendum/${id ?? ''}`);
};

const onUpdateAcceptorDutiesAsync = async (amendmentId: string, id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
  return await http.put(`/api/contract-amendment/${amendmentId}/po-addendum/${id}/acceptor/${acceptorId}/duties-status`, { IsUnableToPerformDuties: isUnableToPerformDuties, remark: remark });
};

const onRejectedAsync = async (amendmentId: string, id: string, body: { remark?: string }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/po-addendum/${id}/reject`, body);
};

const onApprovedAsync = async (amendmentId: string, id: string, body: { remark?: string }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/po-addendum/${id}/approve`, body);
};

const onAssigneeCommentAsync = async (amendmentId: string, id: string, remark: string) =>
  await http.put(`/api/contract-amendment/${amendmentId}/po-addendum/${id}/assignee-comment`, { remark });

const getReviewDocumentAsync = async (amendmentId: string, id: string, documentType: string) =>
  await http.get(`/api/contract-amendment/${amendmentId}/po-addendum/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (amendmentId: string, id: string, documentType: 'ContractAddendum' | 'ContractAmendmentRequest') =>
  await http.post(`/api/contract-amendment/${amendmentId}/po-addendum/${id}/reset-document`, { documentType });

const mapToPayload = (data: Cam01PoAddendumBody): Cam01PoAddendumPayload => {
  return {
    camContractAmendmentId: data.camContractAmendmentId,
    contractNumber: data.newContract.contractNo,
    paymentTerms: data.newPaymentTerms,
    sapNumber: data.newContract.sapNumber,
    poNumber: data.newContract.poNumber,
    vendorId: data.newContract.vendorId,
    status: data.status,
    acceptors: data.acceptors,
    assignees: data.assignees,
  } as Cam01PoAddendumPayload;
};

const Cam01PoAddendumService = {
  getReviewDocumentAsync,
  resetDocumentAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onUpdateAcceptorDutiesAsync,
  onRejectedAsync,
  onApprovedAsync,
  onAssigneeCommentAsync,
};

export default Cam01PoAddendumService;