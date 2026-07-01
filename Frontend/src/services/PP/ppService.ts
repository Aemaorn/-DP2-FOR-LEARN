import http from '@/configs/axios';
import type { AxiosResponse } from 'axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import { PreProcurementGroupStep, PreProcurementType } from '@/enums/preProcurement';
import type { TPreProcurementCriteria, TPreProcurementDetail, TPreProcurementDialog, TPreProcurementDialogCriteria, TPreProcurementDialogGroupStepCount, TPreProcurementList } from '@/models/PP/ppModel';
import type { Attachments } from '@/models/shared/uploadFile';

// TODO: Refactor this service to use the new API structure if API is available
const getProcurementListAsync = async (criteria: TPreProcurementCriteria): Promise<AxiosResponse<TPreProcurementList>> => {
  const params = {
    ...criteria,
    step: criteria.step == PreProcurementGroupStep.All ? undefined : criteria.step,
  } as TPreProcurementCriteria;
  return await http.get<TPreProcurementList>('api/procurement', { params, headers: { isDisabledLoad: true, } });
}

const getPlanDialogAsync = async (criteria: TPreProcurementDialogCriteria): Promise<AxiosResponse<{ groupType: TPreProcurementDialogGroupStepCount, data: TDataTableResult<TPreProcurementDialog> }>> => {
  const params = {
    ...criteria,
    type: criteria.type == PreProcurementType.All ? undefined : criteria.type
  } as TPreProcurementDialogCriteria;

  return await http.get<{ groupType: TPreProcurementDialogGroupStepCount, data: TDataTableResult<TPreProcurementDialog> }>('api/plan/dialog', { params })
}

const getProcurementDialogAsync = async (criteria: TPreProcurementDialogCriteria): Promise<AxiosResponse<{ groupType: TPreProcurementDialogGroupStepCount, data: TDataTableResult<TPreProcurementDialog> }>> => {
  const params = {
    ...criteria,
    type: criteria.type == PreProcurementType.All ? undefined : criteria.type
  } as TPreProcurementDialogCriteria;

  return await http.get<{ groupType: TPreProcurementDialogGroupStepCount, data: TDataTableResult<TPreProcurementDialog> }>('api/procurement/dialog', { params })
}

const createProcurementAsync = async (body: TPreProcurementDetail): Promise<AxiosResponse<string>> =>
  await http.post<string>("api/procurement", body);

const updateAsync = async (id: string, body: TPreProcurementDetail) =>
  await http.put(`api/procurement/${id}`, body);

const getProcurementByIdAsync = async (id: string): Promise<AxiosResponse<TPreProcurementDetail>> =>
  await http.get<TPreProcurementDetail>(`api/procurement/${id}`);

const onUpsertAttachmentsAsync = async (id: string, attachments: Array<Attachments>) => {
  const body = {
    attachments: attachments,
  };

  return await http.put(`/api/procurement/${id}/attachment`, body);
};

const deleteAsync = async (id: string) =>
  await http.delete(`api/procurement/${id}`);

const exportExcelProcurementAsync = async (criteria: TPreProcurementCriteria) =>
  await http.get('/api/procurement/export-procurement', {
    params: {
      ...criteria,
      step: criteria.step == PreProcurementGroupStep.All ? undefined : criteria.step,
    },
    responseType: 'blob',
    headers: {
      Accept: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, */*'
    }
});

const PreProcurementService = {
  getProcurementListAsync,
  getPlanDialogAsync,
  getProcurementDialogAsync,
  createProcurementAsync,
  getProcurementByIdAsync,
  updateAsync,
  onUpsertAttachmentsAsync,
  deleteAsync,
  exportExcelProcurementAsync,
};

export default PreProcurementService;
