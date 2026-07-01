import axiosInstance from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { St009CreateApproverRequest, St009CreateSectionRequest, St009Criteria, St009Detail, St009ListItem, St009UpdateApproversRequest, St009UpdateSectionRequest, St009UpdateSingleApproverRequest } from '@/models/ST/st009';
import type { AxiosResponse } from 'axios';

const getListAsync = async (
  params: St009Criteria
): Promise<AxiosResponse<TDataTableResult<St009ListItem>>> => {
  return await axiosInstance.get<TDataTableResult<St009ListItem>>('api/st/section-approver', { params });
};

const getBySuSectionIdAsync = async (
  suSectionId: string
): Promise<AxiosResponse<St009Detail>> => {
  return await axiosInstance.get<St009Detail>(`api/st/section-approver/by-section/${suSectionId}`);
};

const updateApproversAsync = async (
  suSectionId: string,
  body: St009UpdateApproversRequest
): Promise<AxiosResponse> => {
  return await axiosInstance.put(`api/st/section-approver/${suSectionId}`, body);
};

const createSectionAsync = async (
  body: St009CreateSectionRequest
): Promise<AxiosResponse<string>> => {
  return await axiosInstance.post<string>('api/st/section-approver/sections', body);
};

const updateSectionAsync = async (
  body: St009UpdateSectionRequest
): Promise<AxiosResponse> => {
  return await axiosInstance.put(`api/st/section-approver/sections/${body.id}`, body);
};

const createApproverAsync = async (
  body: St009CreateApproverRequest
): Promise<AxiosResponse<string>> => {
  return await axiosInstance.post<string>(`api/st/section-approver/${body.suSectionId}/approvers`, body);
};

const updateApproverAsync = async (
  suSectionId: string,
  approverId: string,
  body: St009UpdateSingleApproverRequest
): Promise<AxiosResponse> => {
  return await axiosInstance.put(`api/st/section-approver/${suSectionId}/approvers/${approverId}`, body);
};

const deleteAsync = async (id: string): Promise<AxiosResponse<void>> => {
  return await axiosInstance.delete<void>(`api/st/section-approver/sections/${id}`);
};

const deleteApproverAsync = async (suSectionId: string, approverId: string): Promise<AxiosResponse<void>> => {
  return await axiosInstance.delete<void>(`api/st/section-approver/${suSectionId}/approvers/${approverId}`);
};

const ST009Service = {
  getListAsync,
  getBySuSectionIdAsync,
  updateApproversAsync,
  createSectionAsync,
  updateSectionAsync,
  createApproverAsync,
  updateApproverAsync,
  deleteAsync,
  deleteApproverAsync,
};

export default ST009Service;
