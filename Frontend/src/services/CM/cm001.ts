import http from '@/configs/axios';
import { CM001Status } from '@/enums/CM/cm001';
import type { CM001Criteria, CM001Detail, CMTableResponse, PlanAndContractVendorCriteria, PlanAndContractVendorData } from '@/models/CM/cm001';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { OnlyFileAttachment } from '@/models/shared/uploadFile';

const baseEndpoint = `/api/delivery-acceptance`;

const onGetListAsync = async (criteria: CM001Criteria) => {
  const params: CM001Criteria = {
    ...criteria,
    status: criteria.status === CM001Status.All ? undefined : criteria.status,
  };
  return await http.get<CMTableResponse>(`${baseEndpoint}`, { params });
};

const onGetByIdAsync = async (id: string) => {
  return await http.get<CM001Detail>(`${baseEndpoint}/${id}`);
};

const onGetPlanAndContractVendorListAsync = async (criteria: PlanAndContractVendorCriteria) => {
  return await http.get<TDataTableResult<PlanAndContractVendorData>>(`${baseEndpoint}/plan-and-contract-vendor`, { params: criteria });
};

type CreateBody = {
  refId?: string;
  sourceType: string;
  departmentId?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  name?: string;
  budget?: number;
  isCommercialMaterial?: boolean;
};

const onCreateAsync = async (body: CreateBody) => {
  return await http.post<string>(`${baseEndpoint}`, body);
};

type UpdateManualBody = {
  departmentId?: string;
  supplyMethodCode?: string;
  supplyMethodTypeCode?: string;
  supplyMethodSpecialTypeCode?: string;
  name?: string;
  budget?: number;
  isCommercialMaterial?: boolean;
};

const onUpdateManualAsync = async (id: string, body: UpdateManualBody) => {
  return await http.put(`${baseEndpoint}/${id}/manual`, body);
};

const onApproveAsync = async (id: string) => {
  return await http.put(`${baseEndpoint}/${id}/approve`);
};

const onDeleteByIdAsync = async (id: string) => {
  return await http.delete(`${baseEndpoint}/${id}`);
};

const sendWarrantyPeriodEmailAsync = async (
  id: string,
  email: string,
  emailTemplate: string,
  attachments: OnlyFileAttachment[]
) => {
  const body = {
    email,
    emailTemplate,
    emailAttachments: attachments,
  };

  return await http.post(`${baseEndpoint}/${id}/send-warranty-period-email`, body);
};

const CM001Service = {
  onGetListAsync,
  onGetByIdAsync,
  onGetPlanAndContractVendorListAsync,
  onCreateAsync,
  onUpdateManualAsync,
  onApproveAsync,
  onDeleteByIdAsync,
  sendWarrantyPeriodEmailAsync,
};

export default CM001Service;