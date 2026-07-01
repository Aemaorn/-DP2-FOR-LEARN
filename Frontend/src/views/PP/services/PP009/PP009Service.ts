import http from '@/configs/axios';
import type { PP009Detail } from '../../models/PP009/pp009Model';
import { type AxiosResponse, HttpStatusCode } from 'axios';
import type { EntrepreneurAttachments, OnlyFileAttachment } from '@/models/shared/uploadFile';
import ST003Service from '@/services/file';

const onGetByIdAsync = async (procurementId: string, id?: string) => {
  return http.get<PP009Detail>(`/api/procurement/${procurementId}/contractinvitation/${id ?? ''}`);
};

const onCreateAsync = async (procurementId: string, body: PP009Detail) => {
  return http.post<string>(`/api/procurement/${procurementId}/contractInvitation`, body);
};

const onUpdateByIdAsync = async (procurementId: string, id: string, body: PP009Detail) => {
  return http.put<string>(`/api/procurement/${procurementId}/contractInvitation/${id}`, body);
};

const onApprovedAsync = async (procurementId: string, id: string, body: { remark?: string }) => {
  return http.put(`/api/procurement/${procurementId}/contractInvitation/${id}/approve`, body);
};

const onRejectedAsync = async (procurementId: string, id: string, body: { remark?: string }) => {
  return http.put(`/api/procurement/${procurementId}/contractInvitation/${id}/reject`, body);
};

const getReviewDocumentAsync = async (
  id: string,
  procurementId: string,
  vendorId: string
): Promise<AxiosResponse<string>> =>
  await http.get<string>(
    `/api/procurement/${procurementId}/contractInvitation/${id}/vendor/${vendorId}/review-document`
  );

const sendEmailInviteAsync = async (
  procurementId: string,
  inviteId: string,
  vendorId: string,
  email: string,
  emailTemplate: string,
  attachments: Array<OnlyFileAttachment>
) => {
  const body = {
    email: email,
    emailTemplate: emailTemplate,
    emailAttachments: attachments,
  };

  return await http.post(
    `/api/procurement/${procurementId}/contract-invitation/${inviteId}/vendor/${vendorId}/send-email`,
    body
  );
};

const onUpsertAttachmentsAsync = async (
  id: string,
  attachments: Array<EntrepreneurAttachments>
) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/contractInvitation/${id}/attachment`, body);
};

const restoreVersionAsync = async (
  procurementId: string,
  contractInvitationId: string,
  vendorId: string,
  sourceFileId: string
) => {
  return http.post<{ fileId: string; version: string }>(
    `/api/procurement/${procurementId}/contract-invitation/${contractInvitationId}/vendor/${vendorId}/restore-version/${sourceFileId}`
  );
};

const downloadContractInvitationPdfAsync = async (
  procurementId: string,
  contractInvitationId: string,
  vendorId: string
): Promise<{ fileId: string; fileName: string } | null> => {
  try {
    const { data, status } = await http.get(
      `/api/procurement/${procurementId}/contract-invitation/${contractInvitationId}/vendor/${vendorId}/download-pdf`,
      { responseType: 'blob' }
    );

    if (status === HttpStatusCode.Ok) {
      const file = new File([data], 'หนังสือเชิญชวนทำสัญญา.pdf', { type: 'application/pdf' });
      const uploadResponse = await ST003Service.uploadFile(file);

      if (uploadResponse.status === HttpStatusCode.Ok) {
        return {
          fileId: uploadResponse.data.id,
          fileName: 'หนังสือเชิญชวนทำสัญญา.pdf',
        };
      }
    }

    return null;
  } catch (error) {
    console.error('Error downloading contract invitation PDF:', error);
    return null;
  }
};

const resetDocumentAsync = async (
  procurementId: string,
  contractInvitationId: string,
  vendorId: string,
) => {
  return await http.post(
    `/api/procurement/${procurementId}/contract-invitation/${contractInvitationId}/vendor/${vendorId}/reset-document`,
    {}
  );
};

const PP009Service = {
  onGetByIdAsync,
  onCreateAsync,
  onUpdateByIdAsync,
  onApprovedAsync,
  onRejectedAsync,
  getReviewDocumentAsync,
  sendEmailInviteAsync,
  onUpsertAttachmentsAsync,
  restoreVersionAsync,
  downloadContractInvitationPdfAsync,
  resetDocumentAsync,
};

export default PP009Service;
