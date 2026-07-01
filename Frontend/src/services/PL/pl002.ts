import http from '@/configs/axios';
import { PlanAnnouncementAction, PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { ConvertStartToEndDate } from '@/helpers/dateTime';
import type { pl002Criteria, PlanAnnouncementBody, planSelected, tableCountResponse } from '@/models/PL/pl002';
import type { AxiosResponse } from 'axios';

const getListAsync = async (searchCriteria: pl002Criteria): Promise<AxiosResponse<tableCountResponse, any>> => {
  const [fromDate, toDate] = ConvertStartToEndDate(searchCriteria.fromAnnouncementDate, searchCriteria.toAnnouncementDate);

  const params = {
    ...searchCriteria,
    status: searchCriteria.status == PlanAnnouncementStatus.All ? undefined : searchCriteria.status,
    fromAnnouncementDate: fromDate,
    toAnnouncementDate: toDate,
  } as pl002Criteria;

  return await http.get<tableCountResponse>('api/plan/announcement', { params });
};

const deleteByIdAsync = async (id: string) =>
  await http.delete(`api/plan/announcement/${id}`);

const getAnnualPlanAsync = async (year: number, supplyMethodCode: string): Promise<AxiosResponse<planSelected[]>> => {
  const params = {
    year,
    supplyMethodCode
  }

  return await http.get<planSelected[]>('api/plan/announcement/get-list-annual-plan', { params });
}

const createPlanAnnouncementAsync = async (body: PlanAnnouncementBody): Promise<AxiosResponse<string>> =>
  await http.post<string>("api/plan/announcement", body);

const updatePlanAnnouncementAsync = async (id: string, body: PlanAnnouncementBody): Promise<AxiosResponse<{ newApproveDocumentFileId?: string; newAnnouncementDocumentFileId?: string }>> =>
  await http.put<{ newApproveDocumentFileId?: string; newAnnouncementDocumentFileId?: string }>(`api/plan/announcement/${id}`, body);

const getPlanAnnouncementAsync = async (id: string): Promise<AxiosResponse<PlanAnnouncementBody>> =>
  await http.get<PlanAnnouncementBody>(`api/plan/announcement/${id}`);

const rejectAnnualPlanAsync = async (planId: string): Promise<AxiosResponse<void>> =>
  await http.put(`api/plan/announcement/reject-annual-plan/${planId}`);

const deletePlanAnnouncementById = async (id: string): Promise<AxiosResponse<void>> =>
  await http.delete(`api/plan/announcement/plan-announcement-selected/${id}`);

const actionPlanAnnouncementAsync = async (id: string, action: PlanAnnouncementAction, remark?: string, announcementTitle?: string, announcementDate?: Date) =>
  await http.put(`api/plan/announcement/action/${id}`, { action: action, remark: remark, announcementTitle: announcementTitle, announcementDate: announcementDate });

const getReviewDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<string>> =>
  await http.get<string>(`/api/plan/announcement/${id}/review-document`, { params: { documentType } });

const resetDocumentAsync = async (id: string, documentType: string): Promise<AxiosResponse<void>> =>
  http.post<void>(`/api/plan/announcement/${id}/reset-document`, { documentType });

const PlanAnnouncementService = {
  getListAsync,
  deleteByIdAsync,
  getAnnualPlanAsync,
  createPlanAnnouncementAsync,
  getPlanAnnouncementAsync,
  rejectAnnualPlanAsync,
  deletePlanAnnouncementById,
  updatePlanAnnouncementAsync,
  actionPlanAnnouncementAsync,
  getReviewDocumentAsync,
  resetDocumentAsync,
};

export default PlanAnnouncementService;