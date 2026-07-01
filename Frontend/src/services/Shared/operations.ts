import http from '@/configs/axios';
import type { OrganizationLevel } from '@/enums/operations';
import type { CommitteeType } from '@/enums/shared';
import { checkIsEighty } from '@/helpers/supplyMethod';
import type { CommitteeTypeWithReason, defaultAcceptorCriteria, DefaultDepartmentDirectorCriteria, OperationBody } from '@/models/shared/operations';
import type { ParticipantsAssignee } from '@/models/shared/participants';
import type { AxiosResponse } from "axios";

const getJorPorDirectorAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody>> =>
  http.get<OperationBody>('api/operations/default-jorpor-director', { headers: { isDisabledLoad } });

const getSegmentITManagerAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody>> =>
  http.get<OperationBody>('api/operations/default-segment-it-manager', { headers: { isDisabledLoad } });

const getSegmentOtherManagerAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody>> =>
  http.get<OperationBody>('api/operations/default-segment-other-manager', { headers: { isDisabledLoad } });

const getDefaultExpenseDisbursementAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody>> =>
  http.get<OperationBody>('api/operations/default-expense-disbursement-director', { headers: { isDisabledLoad } });

const getContractDirectorAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<Array<ParticipantsAssignee>>> =>
  http.get<Array<ParticipantsAssignee>>('api/operations/default-contract-director', { headers: { isDisabledLoad } });

const getOperationsDefaultDepartmentAsync = async (organizationLevel: OrganizationLevel, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> =>
  await http.get('api/operations/default-department-approver', { params: { organizationLevel }, headers: { isDisabledLoad } });

const getOperationsDefaultAcceptorAsync = async (criteria: defaultAcceptorCriteria, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> => {
  const params: defaultAcceptorCriteria = {
    ...criteria,
    supplyMethodSpecialTypeCode: criteria.supplyMethodCode && checkIsEighty(criteria.supplyMethodCode) ? undefined : criteria.supplyMethodSpecialTypeCode
  }

  return await http.get('api/operations/default-acceptor', { params, headers: { isDisabledLoad } });
}

const getOperationsDefaultDepartmentDirectorAsync = async (criteria: DefaultDepartmentDirectorCriteria, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> => {
  return await http.get('api/operations/default-department-director', { params: criteria, headers: { isDisabledLoad } });
};

const getCommitteAcceptorsAsync = async (procurementId: string, type: CommitteeType) =>
  await http.get<CommitteeTypeWithReason>(`/api/procurement/${procurementId}/commit/${type}`);

const getDefaultDepartmentApproverByUserIdAsync = async (userId: string, organizationLevel: OrganizationLevel, skipCurrentEmployee: boolean = true, isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> =>
  await http.get(`/api/operations/${userId}/default-department-approver`, { params: { organizationLevel, skipCurrentEmployee }, headers: { isDisabledLoad } });

const getSegmentContractManagerAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody>> =>
  http.get<OperationBody>('api/operations/default-segment-contract-manager', { headers: { isDisabledLoad } });

const getSegmentAccountingManagerAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody>> =>
  http.get<OperationBody>('api/operations/default-segment-accounting-manager', { headers: { isDisabledLoad } });

const getSegmentAccountingMembersAsync = async (isDisabledLoad: boolean = false): Promise<AxiosResponse<OperationBody[]>> =>
  http.get<OperationBody[]>('api/operations/default-segment-accounting-members', { headers: { isDisabledLoad } });

const operationService = {
  getOperationsDefaultDepartmentAsync,
  getJorPorDirectorAsync,
  getSegmentITManagerAsync,
  getSegmentOtherManagerAsync,
  getOperationsDefaultAcceptorAsync,
  getCommitteAcceptorsAsync,
  getDefaultDepartmentApproverByUserIdAsync,
  getContractDirectorAsync,
  getDefaultExpenseDisbursementAsync,
  getOperationsDefaultDepartmentDirectorAsync,
  getSegmentContractManagerAsync,
  getSegmentAccountingManagerAsync,
  getSegmentAccountingMembersAsync,
}

export default operationService;