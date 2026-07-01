import http from '@/configs/axios';
import type { Cam01FineBody } from '@/models/CAM/CAM01/cam.fine';

const onGetByIdAsync = async (amendmentId: string, id?: string) => {
  const hasId = id ? `/${id}` : '';
  return http.get<Cam01FineBody>(`/api/contract-amendments/${amendmentId}/waive-or-reduce-penalty${hasId}`);
};

const onCreateAsync = async (amendmentId: string, body: Cam01FineBody) => {

  return await http.post(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty`, body);
};

const onUpdateAsync = async (amendmentId: string, id: string, body: Cam01FineBody) => {
  return await http.put(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty/${id}`, body);
};

const onUpdateAcceptorDutiesAsync = async (amendmentId: string, id: string, acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
  return await http.put(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty/${id}/acceptor/${acceptorId}/duties-status`, { IsUnableToPerformDuties: isUnableToPerformDuties, remark: remark });
};

const onRejectedAsync = async (amendmentId: string, id: string, body: { remark?: string }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty/${id}/reject`, body);
};

const onApprovedAsync = async (amendmentId: string, id: string, body: { remark?: string }) => {
  return await http.post(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty/${id}/approve`, body);
};

const onAssigneeCommentAsync = async (amendmentId: string, id: string, remark: string) =>
  await http.put(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty/${id}/assignee-comment`, { remark });

const getReviewDocumentAsync = async (amendmentId: string, id: string, documentType: string) =>
  await http.get(`/api/contract-amendment/${amendmentId}/waive-or-reduce-penalty/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (amendmentId: string, id: string, documentType: 'WaiveOrReducePenalty' | 'Approved') =>
  await http.post(`/api/contract-amendments/${amendmentId}/waive-or-reduce-penalty/${id}/reset-document`, { documentType });

const Cam01FineService = {
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

export default Cam01FineService;
