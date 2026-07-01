import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt007Criteria, TSt007Detail, TSt007List } from '@/models/ST/st007';
import { HttpStatusCode, type AxiosResponse } from 'axios';

const createFromFormData = (body: TSt007Detail): FormData => {
  const formData = new FormData();

  // Required fields
  formData.append('fileId', body.file.id);
  formData.append('group', body.group);
  formData.append('code', body.code);
  formData.append('name', body.name);
  formData.append('isActive', String(body.isActive));

  // Optional file
  if (body.previewPdfFile?.file) {
    formData.append('file', body.previewPdfFile.file);
  }

  // Helper to append when defined (for string/number fields)
  const appendIfDefined = (
    key: string,
    value: string | number | boolean | undefined | null
  ): void => {
    if (value !== undefined && value !== null && value !== '') {
      formData.append(key, String(value));
    }
  };

  // Optional scalar fields
  appendIfDefined('supplyMethodCode', body.supplyMethodCode);
  appendIfDefined('budgetMax', body.budgetMax);
  appendIfDefined('budgetMin', body.budgetMin);
  appendIfDefined('contractTemplateCode', body.contractTemplateCode);
  appendIfDefined('supplyMethodTypeCode', body.supplyMethodTypeCode);
  appendIfDefined('contractAmendmentDocumentType', body.contractAmendmentDocumentType);

  // Boolean flags
  const booleanFlags: Array<[keyof TSt007Detail, string]> = [
    ['isCancel', 'isCancel'],
    ['isEdit', 'isEdit'],
    ['isChange', 'isChange'],
    ['isJorPorComment', 'isJorPorComment'],
    ['isFine', 'isFine'],
    ['isWinnerAnnounced', 'isWinnerAnnounced'],
    ['isEvaluationReport', 'isEvaluationReport'],
    ['isAppointmentOrdered', 'isAppointmentOrdered'],
    ['isApproval', 'isApproval'],
    ['isInYear', 'isInYear'],
    ['isPublished', 'isPublished'],
    ['hasGuarantee', 'hasGuarantee'],
    ['isConfidential', 'isConfidential'],
  ];

  for (const [prop, key] of booleanFlags) {
    if (body[prop]) {
      formData.append(key, String(body[prop] as boolean));
    }
  }

  return formData;
};

const onGetListAsync = async (
  params: TSt007Criteria
): Promise<AxiosResponse<TDataTableResult<TSt007List>, any>> =>
  http.get<TDataTableResult<TSt007List>>(`/api/st/st007`, { params });

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt007Detail, any>> =>
  http.get<TSt007Detail>(`/api/st/st007/${id}`);

const onCreateAsync = async (body: TSt007Detail): Promise<AxiosResponse<{ id: string }, any>> => {
  const formData = createFromFormData(body);

  return http.postForm<{ id: string }>(`/api/st/st007`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

const onUpdateAsync = async (id: string, body: TSt007Detail): Promise<AxiosResponse<void, any>> => {
  const formData = createFromFormData(body);

  return http.put<void>(`/api/st/st007/${id}`, formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
  });
};

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st007/${id}`);

const downloadFilePdfAsync = async (id: string) => {
  const { data, status } = await http.get(`/api/st/st007/${id}/pdf/`, {
    responseType: 'blob',
  });

  if (status === HttpStatusCode.Ok) {
    const blob = new Blob([data], { type: 'application/pdf' });
    const url = window.URL.createObjectURL(blob);
    
    window.open(url, '_blank');
    
    // Clean up the object URL after a short delay to ensure the PDF loads
    setTimeout(() => {
      window.URL.revokeObjectURL(url);
    }, 1000);
  }
};

const ST007Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
  downloadFilePdfAsync,
};

export default ST007Service;
