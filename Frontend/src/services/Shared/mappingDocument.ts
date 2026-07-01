import http from '@/configs/axios';
import type { MappingDocumentResponse } from '@/models/shared/mappingDocument';
import type { AxiosResponse } from 'axios';

const onGetMappingDocumentServiceAsync = async (
  pathToGet: string
): Promise<AxiosResponse<MappingDocumentResponse>> => {
  return http.get<MappingDocumentResponse>(`/api/${pathToGet}/mapping-document`);
};

const MappingDocumentService = {
  onGetMappingDocumentServiceAsync,
};

export default MappingDocumentService;
