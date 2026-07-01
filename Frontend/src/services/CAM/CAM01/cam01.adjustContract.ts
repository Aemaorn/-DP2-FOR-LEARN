import http from '@/configs/axios';
import type { Cam01AdjustContractBody } from '@/models/CAM/CAM01/cam.adjustContract';


const onGetByIdAsync = async (amendmentId: string, id?: string) => {
  const hasId = id ? `/${id}` : '';
  return http.get<Cam01AdjustContractBody>(`/api/contract-amendment/${amendmentId}/adjust-contract-duration${hasId}`);
};

const onCreateAsync = async (amendmentId: string, body: Cam01AdjustContractBody) => {

  return await http.post(`/api/contract-amendment/${amendmentId}/adjust-contract-duration`, body);
};

const onUpdateAsync = async (amendmentId: string, id: string, body: Cam01AdjustContractBody) => {
  return await http.put(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}`, body);
};

const onUpdateAcceptorDutiesAsync = async (amendmentId: string, id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
  return await http.put(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}/acceptor/${acceptorId}/duties-status`, { IsUnableToPerformDuties: isUnableToPerformDuties, remark: remark });
};

const onRejectedAsync = async (amendmentId: string, id: string, body: { remark?: string }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}/reject`, body);
};

const onApprovedAsync = async (amendmentId: string, id: string, body: { remark?: string }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}/approve`, body);
};

const onAssigneeCommentAsync = async (amendmentId: string, id: string, remark: string) =>
  await http.put(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}/assignee-comment`, { remark });

const getReviewDocumentAsync = async (amendmentId: string, id: string, documentType: string) =>
  await http.get(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (amendmentId: string, id: string, documentType: 'ExtendChange' | 'Approved') =>
  await http.post(`/api/contract-amendment/${amendmentId}/adjust-contract-duration/${id}/reset-document`, { documentType });

const Cam01AdjustContractService = {
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onUpdateAcceptorDutiesAsync,
  onRejectedAsync,
  onApprovedAsync,
  onAssigneeCommentAsync,
  getReviewDocumentAsync,
  resetDocumentAsync,
};

export default Cam01AdjustContractService;
