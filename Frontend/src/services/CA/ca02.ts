import http from '@/configs/axios';
import { CA02Status } from '@/enums/CA/ca02';
import { AcceptorType } from '@/enums/participants';
import type { CA02Body, GetCA02ListResponse, TCA02Criteria, TCA02DialogCriteria } from '@/models/CA/ca02';
import type { Attachments } from '@/models/shared/uploadFile';

export type UpdateCa02Response = {
  newDocumentFileId?: string;
};

const path = `/api/contract-draft-vendor`

const buildPayload = (body: CA02Body) => ({
  status: body.status,
  isManual: body.isManual,
  receiveDate: body.receiveDate,
  sbsDocumentNo: body.sbsDocumentNo,
  documentDate: body.documentDate,
  issuedDate: body.issuedDate,
  requestReason: body.requestReason,
  acceptors: body.acceptors,
  inspectionCommittees: body.inspectionCommittees
    ? {
        committees: body.inspectionCommittees.committees.map(c => ({
          id: c.id,
          userId: c.userId,
          sequence: c.sequence,
          committeePositionsCode: c.committeePositionsCode,
        })),
        isCommittee: body.inspectionCommittees.isCommittee,
      }
    : undefined,
  supplyMethodCode: body.contractVendorInfo.supplyMethodCode,
  supplyMethodTypeCode: body.contractVendorInfo.supplyMethodTypeCode,
  supplyMethodSpecialTypeCode: body.contractVendorInfo.supplyMethodSpecialTypeCode,
  contractDraftVendorId: body.isManual ? undefined : body.contractVendorInfo.id,
  entrepreneurName: body.contractVendorInfo.entrepreneurName,
  entrepreneurId: body.contractVendorInfo.entrepreneurId,
  entrepreneurEmail: body.contractVendorInfo.entrepreneurEmail,
  contractNumber: body.contractVendorInfo.contractNumber,
  poNumber: body.contractVendorInfo.poNumber,
  budget: body.contractVendorInfo.budget,
  contractName: body.contractVendorInfo.contractName,
  contractSignedDate: body.contractVendorInfo.contractSignedDate,
  deliveryDate: body.contractVendorInfo.deliveryDate,
  contractEndDate: body.contractVendorInfo.contractEndDate,
});

const onGetListAsync = async (criteria: TCA02Criteria) => {
  const params: TCA02Criteria = {
    ...criteria,
    status: criteria.status === CA02Status.All ? undefined : criteria.status,
  };

  return await http.get<GetCA02ListResponse>(`/api/certificate-requisition`, { params });
};

const onDeleteAsync = async (_contractDraftVendorId: string, id: string) => {
  return await http.delete(`/api/certificate-requisition/${id}`);
};

const onGetByIdAsync = async (contractDraftVendorId: string | undefined, id?: string) => {
  if (!id) {
    return await http.get(`/api/certificate-requisition/init`, {
      params: contractDraftVendorId ? { contractDraftVendorId } : undefined,
    });
  }
  return await http.get(`/api/certificate-requisition/${id}`);
};

const onGetDialogListAsync = async (params: TCA02DialogCriteria) => {
  return await http.get(`/api/certificate-requisition/delivery-acceptance-dialog`, { params });
};

const onCreateAsync = async (_contractDraftVendorId: string, body: CA02Body) => {
  return await http.post(`/api/certificate-requisition`, buildPayload(body));
};

const onUpdateAsync = async (_contractDraftVendorId: string, id: string, body: CA02Body) => {
  return await http.put<UpdateCa02Response>(`/api/certificate-requisition/${id}`, {
    ...buildPayload(body),
    documentId: body.documentId,
    isReplace: body.isReplace,
    isResetDocument: body.isResetDocument,
  });
};

const onSetDutiesStatusAsync = async (_contractDraftVendorId: string, id: string, acceptorId: string, body: { isUnableToPerformDuties: boolean, remark?: string }) =>
  await http.put(`/api/certificate-requisition/${id}/acceptor/${acceptorId}/duties-status`, body);

const onRecallAsync = async (_contractDraftVendorId: string, id: string) => {
  return await http.put(`/api/certificate-requisition/${id}/recall`);
}

const onApproveAsync = async (_contractDraftVendorId: string, id: string, payload: { remark?: string }) => {
  const body = {
    ...payload,
    group: AcceptorType.AcceptanceCommittee,
  };

  return await http.put(`/api/certificate-requisition/${id}/approve`, body);
};

const onRejectAsync = async (_contractDraftVendorId: string, id: string, payload: { remark?: string }) => {
  const body = {
    ...payload,
    group: AcceptorType.AcceptanceCommittee,
  };

  return await http.put(`/api/certificate-requisition/${id}/reject`, body);
};

const onGetReviewDocumentAsync = async (contractDraftVendorId: string, id: string) => {
  return await http.get(`/api/contract-draft-vendor/${contractDraftVendorId}/certificate-requisition/${id}/review-document`);
};

const resetDocumentAsync = async (_contractDraftVendorId: string, id: string) =>
  await http.post(`/api/certificate-requisition/${id}/reset-document`, {});

const attachmentsAsync = async (_contractDraftVendorId: string, id: string, attachments: Attachments[]) =>
  await http.put(`/api/certificate-requisition/${id}/attachment`, { attachments });

const CA02Service = {
  onGetListAsync,
  onGetByIdAsync,
  onGetDialogListAsync,
  onCreateAsync,
  onUpdateAsync,
  onSetDutiesStatusAsync,
  onRecallAsync,
  onApproveAsync,
  onRejectAsync,
  onDeleteAsync,
  onGetReviewDocumentAsync,
  resetDocumentAsync,
  attachmentsAsync,
};

export default CA02Service;