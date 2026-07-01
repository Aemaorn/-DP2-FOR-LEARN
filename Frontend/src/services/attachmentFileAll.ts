import http from '@/configs/axios';
import type { AxiosResponse } from 'axios';
import type { TAttachmentFileAllBody, TAttachmentFileAllCriteria } from '@/models/attachmentFileAll';

const onGetAsync = async (criteria: TAttachmentFileAllCriteria): Promise<AxiosResponse<TAttachmentFileAllBody>> =>
  http.get('/api/attachment-files', { params: criteria });

const AttachmentFileAllService = {
  onGetAsync,
};

export default AttachmentFileAllService;
