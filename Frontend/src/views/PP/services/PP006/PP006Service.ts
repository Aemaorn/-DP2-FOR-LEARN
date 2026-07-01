import type { AxiosResponse } from 'axios';
import type { InvitedEntrepreneurs, PP006Detail } from '../../models/PP006/pp006Model';
import http from '@/configs/axios';
import type { OnlyFileAttachment } from '@/models/shared/uploadFile';
import ST003Service from '@/services/file';
import { HttpStatusCode } from 'axios';

const createAsync = async (procurementId: string, body: PP006Detail) =>
  await http.post(`/api/procurement/${procurementId}/invite`, body);

const updateAsync = async (id: string, procurementId: string, body: PP006Detail) =>
  await http.put<{ newDocumentFileId?: string }>(`/api/procurement/${procurementId}/invite/${id}`, body);

const getByIdAsync = async (procurementId: string, id?: string) => {
  if (id) {
    return await http.get(`/api/procurement/${procurementId}/invite/${id}`);
  }
  return await http.get(`/api/procurement/${procurementId}/invite`);
};

const invitedEntrepreneursAsync = async (
  inviteId: string,
  procurementId: string,
  body: InvitedEntrepreneurs
) =>
  await http.post(
    `/api/procurement/${procurementId}/invite/${inviteId}/invite-entrepreneurs`,
    body
  );

const updateEntrepreneurAsync = async (
  inviteId: string,
  id: string,
  procurementId: string,
  body: InvitedEntrepreneurs
) =>
  await http.put(
    `/api/procurement/${procurementId}/invite/${inviteId}/invite-entrepreneurs/${id}`,
    body
  );

const approveAsync = async (id: string, procurementId: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/invite/${id}/approve`, { remark });

const rejectAsync = async (id: string, procurementId: string, remark?: string) =>
  await http.post(`/api/procurement/${procurementId}/invite/${id}/reject`, { remark });

const getReviewDocumentAsync = async (
  id: string,
  procurementId: string
): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/invite/${id}/review-document`);

const getReviewDocumentByEntrepreneurAsync = async (
  procurementId: string,
  inviteId: string,
  entrepreneurId: string
): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/procurement/${procurementId}/invite/${inviteId}/entrepreneur/${entrepreneurId}/review-document`);

const updateDutieStatusAsync = async (
  id: string,
  procurementId: string,
  acceptorId: string,
  isUnableToPerformDuties: boolean,
  remark?: string
) =>
  await http.put(
    `/api/procurement/${procurementId}/invite/${id}/acceptor/${acceptorId}/duties-status`,
    { isUnableToPerformDuties, remark }
  );

const sendEmailInviteAsync = async (
  procurementId: string,
  inviteId: string,
  entrepreneursId: string,
  email: string,
  emailTemplate: string,
  attachments: OnlyFileAttachment[]
) => {
  const body = {
    email,
    emailTemplate,
    attachments,
  };

  return await http.post(
    `/api/procurement/${procurementId}/invite/${inviteId}/entrepreneurs/${entrepreneursId}/send-email`,
    body
  );
};

const getDefaultEmailTemplateAsync = async (
  procurementId: string,
  inviteId: string,
  entrepreneursId: string
) => {
  return await http.get(
    `/api/procurement/${procurementId}/invite/${inviteId}/entrepreneurs/${entrepreneursId}/default-email-template`
  );
};

const downloadTorDraftPdfAsync = async (
  procurementId: string,
  torDraftId: string
): Promise<{ fileId: string; fileName: string } | null> => {
  try {
    const { data, status } = await http.get(
      `/api/procurement/${procurementId}/tordraft/${torDraftId}/download-pdf`,
      { responseType: 'blob' }
    );

    if (status === HttpStatusCode.Ok) {
      const file = new File([data], 'TOR.pdf', { type: 'application/pdf' });
      const uploadResponse = await ST003Service.uploadFile(file);

      if (uploadResponse.status === HttpStatusCode.Ok) {
        return {
          fileId: uploadResponse.data.id,
          fileName: 'TOR.pdf',
        };
      }
    }

    return null;
  } catch (error) {
    console.error('Error downloading TOR draft PDF:', error);
    return null;
  }
};

const downloadMedianPricePdfAsync = async (
  procurementId: string,
  medianPriceId: string
): Promise<{ fileId: string; fileName: string } | null> => {
  try {
    const { data, status } = await http.get(
      `/api/procurement/${procurementId}/median-price/${medianPriceId}/download-pdf`,
      { responseType: 'blob' }
    );

    if (status === HttpStatusCode.Ok) {
      const file = new File([data], 'ราคากลาง.pdf', { type: 'application/pdf' });
      const uploadResponse = await ST003Service.uploadFile(file);

      if (uploadResponse.status === HttpStatusCode.Ok) {
        return {
          fileId: uploadResponse.data.id,
          fileName: 'ราคากลาง.pdf',
        };
      }
    }

    return null;
  } catch (error) {
    console.error('Error downloading median price PDF:', error);
    return null;
  }
};

const restoreVersionAsync = async (
  procurementId: string,
  inviteId: string,
  sourceFileId: string
) => {
  return http.post<{ fileId: string; version: string }>(
    `/api/procurement/${procurementId}/invite/${inviteId}/restore-version/${sourceFileId}`
  );
};

const downloadInvitePdfAsync = async (
  procurementId: string,
  inviteId: string,
  entrepreneurId: string
): Promise<{ fileId: string; fileName: string } | null> => {
  try {
    const { data, status } = await http.get(
      `/api/procurement/${procurementId}/invite/${inviteId}/entrepreneur/${entrepreneurId}/download-pdf`,
      { responseType: 'blob' }
    );

    if (status === HttpStatusCode.Ok) {
      const file = new File([data], 'หนังสือเชิญชวน.pdf', { type: 'application/pdf' });
      const uploadResponse = await ST003Service.uploadFile(file);

      if (uploadResponse.status === HttpStatusCode.Ok) {
        return {
          fileId: uploadResponse.data.id,
          fileName: 'หนังสือเชิญชวน.pdf',
        };
      }
    }

    return null;
  } catch (error) {
    console.error('Error downloading invite PDF:', error);
    return null;
  }
};

const notInviteAsync = async (procurementId: string, inviteId: string, userId: string) => {

  const body = {
    userId,
    procurementId,
    inviteId,
  }

  return await http.put(`/api/procurement/${procurementId}/invite/${inviteId}/not-invite`, body);
}
const resetDocumentAsync = async (procurementId: string, inviteId: string) => {
  return await http.post(`/api/procurement/${procurementId}/invite/${inviteId}/reset-document`, {});
};

const resetDocumentByEntrepreneurAsync = async (
  procurementId: string,
  inviteId: string,
  entrepreneurId: string,
  userId: string
) => {
  return await http.post(
    `/api/procurement/${procurementId}/invite/${inviteId}/entrepreneur/${entrepreneurId}/reset-document`,
    { userId }
  );
};

const PP006Service = {
  createAsync,
  updateAsync,
  getByIdAsync,
  invitedEntrepreneursAsync,
  updateEntrepreneurAsync,
  approveAsync,
  rejectAsync,
  getReviewDocumentAsync,
  getReviewDocumentByEntrepreneurAsync,
  updateDutieStatusAsync,
  sendEmailInviteAsync,
  getDefaultEmailTemplateAsync,
  restoreVersionAsync,
  downloadTorDraftPdfAsync,
  downloadMedianPricePdfAsync,
  downloadInvitePdfAsync,
  notInviteAsync,
  resetDocumentAsync,
  resetDocumentByEntrepreneurAsync,
};

export default PP006Service;
