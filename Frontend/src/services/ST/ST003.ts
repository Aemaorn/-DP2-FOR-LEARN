import http from '@/configs/axios';
import type { checkType } from '@/enums/RP/rp004';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt003Criteria, TSt003Detail, CustomFileSt003, TSt003List } from '@/models/ST/st003';
import type { AxiosResponse } from 'axios';

const onGetListAsync = async (
  params: TSt003Criteria
): Promise<AxiosResponse<TDataTableResult<TSt003List>, any>> =>
  http.get<TDataTableResult<TSt003List>>(`/api/st/st003`, { params });

const onGetLookupAsync = async (
  params: TSt003Criteria
): Promise<AxiosResponse<TDataTableResult<TSt003List>, any>> =>
  http.get<TDataTableResult<TSt003List>>(`/api/st/st003/lookup`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt003Detail, any>> =>
  http.get<TSt003Detail>(`/api/st/st003/${id}`);

const onCreateAsync = async (body: TSt003Detail): Promise<AxiosResponse<{ id: string }, any>> =>
  http.post<{ id: string }>(`/api/st/st003`, body);

const onUpdateAsync = async (id: string, body: TSt003Detail): Promise<AxiosResponse<void, any>> =>
  http.put<void>(`/api/st/st003/${id}`, body);

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st003/${id}`);

const onUploadFileAsync = async (
  id: string,
  files: CustomFileSt003[]
): Promise<AxiosResponse<void, any>> => {
  const formData = new FormData();

  files.forEach((f, index) => {
    if (f.file) {
      formData.append(`Attachments[${index}].File`, f.file);
      formData.append(`Attachments[${index}].IsPrivate`, 'false');
    }
  });

  return http.post<void>(`/api/st/st003/${id}/attachment`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

interface UpdateFileSequence {
  fileId: string;
  sequence: number;
  isPrivate: boolean;
}

const onUpdateFileSequenceAsync = async (id: string, attachment: UpdateFileSequence[]) => {
  const formData = new FormData();

  attachment.forEach((f, index) => {
    formData.append(`Attachments[${index}].FileId`, f.fileId);
    formData.append(`Attachments[${index}].Sequence`, f.sequence.toString());
    formData.append(`Attachments[${index}].IsPrivate`, f.isPrivate.toString());
  });

  return http.put(`/api/st/st003/${id}/attachment`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

const onDeleteFileAsync = async (id: string, attachmentId: string, fileId: string) =>
  http.delete(`/api/st/st003/${id}/attachment/${attachmentId}/file/${fileId}`);

const checkQualificationAsync = async (vendorId: string, checkType: checkType) => {
  const params = {
    checkType
  }

  return http.get(`/api/st/st003/${vendorId}/check-qualification`, { params });
}

const ST003Service = {
  onGetListAsync,
  onGetLookupAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
  onUploadFileAsync,
  onUpdateFileSequenceAsync,
  onDeleteFileAsync,
  checkQualificationAsync
};

export default ST003Service;
