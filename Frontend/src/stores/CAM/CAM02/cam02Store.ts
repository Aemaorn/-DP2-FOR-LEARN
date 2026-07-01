import Cam02Constants from '@/constants/CAM/CAM02/cam02';
import { DepartmentId } from '@/enums/businessUnit';
import { Cam02Status } from '@/enums/CAM/CAM02/cam02';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { OrganizationLevel, SectionProcessType } from '@/enums/operations';
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { CommitteePositions } from '@/enums/PCM005/principle';
import { EGroupCode, EWorkProcess, OrganizationLevelEnum } from '@/enums/shared';
import { SupplyMethodCode } from '@/enums/supplyMethod';
import { showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import { isCurrentPendingAcceptor } from '@/helpers/participants';
import { checkIsEighty } from '@/helpers/supplyMethod';
import ToastHelper from '@/helpers/toast';
import {
  type TCam02Body,
  type TCam02Criteria,
  type SourceTypeList,
  type TCam02ListData,
  type TCam02StatusCount,
  type TChangeCommitteeSendAction,
} from '@/models/CAM/CAM02/cam02';
import type { defaultAcceptorCriteria, DefaultDepartmentDirectorCriteria } from '@/models/shared/operations';
import type { Option, OptionBadge } from '@/models/shared/option';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { ParticipantsAcceptor, ParticipantsAssignee, ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import type { Attachments } from '@/models/shared/uploadFile';
import Cam02Service from '@/services/CAM/CAM02/cam02';
import SharedService from '@/services/Shared/dropdown';
import operationService from '@/services/Shared/operations';
import { useAuthenticationStore } from '@/stores/authentication';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { computed, nextTick, ref, type Ref } from 'vue';

const getDropDownCommitteeAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await Cam02Service.onDropDownCommitteeAsync();

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getDepartmentAsync = async (target: Ref<Option[]>): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(
    OrganizationLevelEnum.Department,
    undefined,
    true
  );

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodAsync = async (target: Ref<Option[]>, parentCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(
    EGroupCode.SMethod,
    parentCode,
    true
  );

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const getSupplyMethodTypeAsync = async (
  target: Ref<Option[]>,
  parentCode?: string
): Promise<void> => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(
    EGroupCode.SMethodType,
    parentCode,
    true
  );

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const useCam02ListStore = defineStore('cam02-list-store', () => {
  const { Cam02StatusName, Cam02ListStatusColor } = Cam02Constants;

  const initCriteria: TCam02Criteria = {
    pageNumber: 1,
    pageSize: 10,
    workProcess: EWorkProcess.InProcess,
    status: Cam02Status.All,
  };

  const initTable = {
    data: [],
    totalRecords: 0,
  } as TDataTableResult<TCam02ListData>;

  const criteria = ref<TCam02Criteria>(structuredClone(initCriteria));
  const table = ref<TDataTableResult<TCam02ListData>>(structuredClone(initTable));
  const options = ref<{ rentalType: Array<Option> }>({
    rentalType: [] as Array<Option>,
  });

  const statusOptionBadge = ref([] as OptionBadge[]);
  const dropDownCommittee = ref<Option[]>([]);
  const departmentDDL = ref<Option[]>([]);
  const supplyMethodCodeDDL = ref<Option[]>([]);
  const supplyMethodTypeCodeDDL = ref<Option[]>([]);
  const supplyMethodSpecialTypeCodeDDL = ref<Option[]>([]);

  const getDepartmentDDLAsync = async (): Promise<void> => {
    await getDepartmentAsync(departmentDDL);
  };

  const getSupplyMethodDDLAsync = async (): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodCodeDDL);
  };

  const getSupplyMethodTypeDDLAsync = async (): Promise<void> => {
    await getSupplyMethodTypeAsync(supplyMethodTypeCodeDDL);
  };

  const getSupplyMethodSpecialTypeDDlAsync = async (parentCode: string): Promise<void> => {
    await getSupplyMethodAsync(supplyMethodSpecialTypeCodeDDL, parentCode);
  };

  const onDropDownCommitteeAsync = async () => {
    return getDropDownCommitteeAsync(dropDownCommittee);
  };

  const mapStatusCount = (count: TCam02StatusCount) => {
    return Object.entries(Cam02Status).map(
      ([, value]) =>
        ({
          label: Cam02StatusName(value),
          value: value,
          bgColorClass: Cam02ListStatusColor(value).bgColorClass,
          textColorClass: Cam02ListStatusColor(value).textColorClass,
          count: getCount(count, value),
        }) as OptionBadge
    );
  };

  const getCount = (countAll: TCam02StatusCount, status: Cam02Status): number => {
    let count = 0;
    const convertStatus = (status[0]?.toLowerCase() ?? '') + status.slice(1);
    count = countAll[convertStatus as keyof TCam02StatusCount];

    return count;
  };

  const onResetCriteria = () => {
    criteria.value = structuredClone(initCriteria);
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    criteria.value.pageNumber = pageNumber;
    criteria.value.pageSize = pageSize;
  };

  const countData = ref({} as TCam02StatusCount);

  const onGetList = async () => {
    const { data, status } = await Cam02Service.onGetListAsync(criteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data.data;

      countData.value = {
        all: data.all,
        draft: data.draft,
        waitingApproval: data.waitingApproval,
        approved: data.approved,
        rejected: data.rejected,
        cancelled: data.cancelled,
        edit: data.edit,
      };
    }

    statusOptionBadge.value = mapStatusCount(countData.value);
  };

  const onGetDropdown = async () => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CMType);

    if (status === HttpStatusCode.Ok) {
      options.value.rentalType = data;
    }
  };

  return {
    // Variable
    criteria,
    table,
    options,
    statusOptionBadge,
    dropDownCommittee,
    departmentDDL,
    supplyMethodCodeDDL,
    supplyMethodTypeCodeDDL,
    supplyMethodSpecialTypeCodeDDL,

    // Functions
    onResetCriteria,
    onChangePageSize,
    onGetList,
    onGetDropdown,
    onDropDownCommitteeAsync,
    getDepartmentDDLAsync,
    getSupplyMethodDDLAsync,
    getSupplyMethodTypeDDLAsync,
    getSupplyMethodSpecialTypeDDlAsync,
  };
});

export const useCam02DetailStore = defineStore('cam02-detail-store', () => {
  const initData = {
    status: Cam02Status.Draft,
    attachments: [] as Array<Attachments>,
  } as TCam02Body;

  const auth = useAuthenticationStore();

  const committeeGroupTypeList = ref<SourceTypeList[]>([]);
  const procurementDetail = ref(structuredClone(initData));
  const isLoadingData = ref(false);
  const procurementDetailStore = usePPDetailStore();

  const mergedProcurement = computed(() => ({
    supplyMethodCode: procurementDetailStore.procurementDetail.supplyMethodCode ?? procurementDetail.value.procurement?.supplyMethodCode,
    supplyMethodSpecialTypeCode: procurementDetailStore.procurementDetail.supplyMethodSpecialTypeCode ?? procurementDetail.value.procurement?.supplyMethodSpecialTypeCode,
    isStock: procurementDetailStore.procurementDetail.isStock ?? procurementDetail.value.procurement?.isStock,
    isCommercialMaterial: procurementDetailStore.procurementDetail.isCommercialMaterial ?? procurementDetail.value.procurement?.isCommercialMaterial,
  }));

  const resetDetail = (): void => {
    procurementDetail.value = {
      status: Cam02Status.Draft,
      acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
      attachments: [] as Array<Attachments>,
    } as TCam02Body;
  };

  const getDefaultAcceptor = async (): Promise<void> => {
    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(
      auth.profile.id,
      OrganizationLevel.Department,
      true
    );

    if (status == HttpStatusCode.Ok) {
      procurementDetail.value.acceptors = [];

      data.forEach((item) =>
        procurementDetail.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: procurementDetail.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
        } as ParticipantsCommitteeAcceptor)
      );
    }
  };

  const getDefaultAcceptorAppointAsync = async (): Promise<void> => {
    let processType: SectionProcessType = SectionProcessType.AppointPreProcurement;

    const is80 = checkIsEighty(mergedProcurement.value.supplyMethodCode);
    if (is80) {
      if (mergedProcurement.value.isStock) {
        processType = SectionProcessType.AppointPreProcurementStock;
      } else if (mergedProcurement.value.isCommercialMaterial) {
        processType = SectionProcessType.AppointPreProcurementCommercialParcel;
      }
    }

    let userId = auth.profile.id;

    const assignees = procurementDetail.value.assignees?.filter(f => f.assigneeType === AssigneeType.Assignee);

    if(procurementDetail.value.isJorPorComment && assignees.length > 0)
    {
      const assignee = assignees.reduce((prev, curr) =>
        curr.sequence > prev.sequence ? curr : prev
        , assignees[0]);

        userId = assignee.userId
    }

    const params = {
      supplyMethodCode: mergedProcurement.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: mergedProcurement.value.supplyMethodSpecialTypeCode,
      processType,
      budget: procurementDetailStore.procurementDetail.budget,
      userId: userId,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status == HttpStatusCode.Ok) {
      procurementDetail.value.acceptors = procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach((item) =>
        procurementDetail.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: procurementDetail.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
      } as ParticipantsCommitteeAcceptor));
    }
  };

  const getDefaultAcceptorJp004Async = async (): Promise<void> => {
    if (auth.profile.departmentCode !== procurementDetailStore.procurementDetail.departmentCode) {
      return;
    }

    let userId = auth.profile.id;

    const assignees = procurementDetail.value.assignees?.filter(f => f.assigneeType === AssigneeType.Assignee);

    if(procurementDetail.value.isJorPorComment && assignees.length > 0)
    {
      const assignee = assignees.reduce((prev, curr) =>
        curr.sequence > prev.sequence ? curr : prev
        , assignees[0]);

        userId = assignee.userId
    }

    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(userId, OrganizationLevel.Department, true);

    if (status == HttpStatusCode.Ok) {
      procurementDetail.value.acceptors = procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach((item) =>
        procurementDetail.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: procurementDetail.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
      } as ParticipantsCommitteeAcceptor));

      if(procurementDetailStore.procurementDetail.departmentCode !== DepartmentId.JorPor)
      {
        const params = {
          businessUnitId: DepartmentId.JorPor,
        } as DefaultDepartmentDirectorCriteria;

        const directorResponse = await operationService.getOperationsDefaultDepartmentDirectorAsync(params);

        if (directorResponse.status === HttpStatusCode.Ok) {
          const directors = Array.isArray(directorResponse.data) ? directorResponse.data : [directorResponse.data];

         directors.forEach((item) =>
          procurementDetail.value.acceptors.push({
            acceptorType: AcceptorType.Approver,
            fullName: item.fullName,
            positionName: item.fullPositionName,
            sequence: procurementDetail.value.acceptors.length + 1,
            status: AcceptorStatus.Draft,
            userId: item.userId,
            departmentName: item.businessUnitName,
            departmentCode: item.businessUnitId,
          } as ParticipantsCommitteeAcceptor));
        }
      }
    }
  };

  const getDefaultAcceptorJp005Async = async () => {
    let processType: SectionProcessType = SectionProcessType.ApprovePurchaseRequest;

    if (mergedProcurement.value.isCommercialMaterial) {
      processType = SectionProcessType.ApprovePurchaseRequestCommercialParcel;
    }

    let userId = auth.profile.id;

    const assignees = procurementDetail.value.assignees?.filter(f => f.assigneeType === AssigneeType.Assignee);

    if(procurementDetail.value.isJorPorComment && assignees.length > 0)
    {
      const assignee = assignees.reduce((prev, curr) =>
        curr.sequence > prev.sequence ? curr : prev
        , assignees[0]);

        userId = assignee.userId
    }

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync({
      budget: procurementDetailStore.procurementDetail.budget,
      processType: processType,
      supplyMethodCode: mergedProcurement.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: mergedProcurement.value.supplyMethodSpecialTypeCode,
      userId: userId,
    }, true);

    if (status === HttpStatusCode.Ok) {
      procurementDetail.value.acceptors = procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach((item) =>
        procurementDetail.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: procurementDetail.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
      } as ParticipantsCommitteeAcceptor));
    }
  };

  const getDefaultAcceptorPurchaseOrderApprovalAsync = async (): Promise<void> => {

    let processType: SectionProcessType = SectionProcessType.ApprovePurchaseOrder;

    const is80 = checkIsEighty(mergedProcurement.value.supplyMethodCode);
    if (is80 && mergedProcurement.value.isCommercialMaterial) {
      if (mergedProcurement.value.isStock) {
         processType = SectionProcessType.ApprovePurchaseOrderCommercialParcel;
      }
    }

    let userId = auth.profile.id;
    const assignees = procurementDetail.value.assignees?.filter(f => f.assigneeType === AssigneeType.Assignee);

    if(procurementDetail.value.isJorPorComment && assignees.length > 0)
    {
      const assignee = assignees.reduce((prev, curr) =>
        curr.sequence > prev.sequence ? curr : prev
        , assignees[0]);

        userId = assignee.userId
    }

    const params = {
      supplyMethodCode: mergedProcurement.value.supplyMethodCode,
      supplyMethodSpecialTypeCode: mergedProcurement.value.supplyMethodSpecialTypeCode,
      processType,
      budget: procurementDetailStore.procurementDetail.budget,
      userId: userId,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(params);

    if (status === HttpStatusCode.Ok) {
      procurementDetail.value.acceptors = procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach((item) =>
        procurementDetail.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: procurementDetail.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
      } as ParticipantsCommitteeAcceptor));
    }
  };

  const getDefaultAcceptorPrincipleApprovalAsync = async (): Promise<void> => {

    let userId = auth.profile.id;

    const assignees = procurementDetail.value.assignees?.filter(f => f.assigneeType === AssigneeType.Assignee);

    if(procurementDetail.value.isJorPorComment && assignees.length > 0)
    {
      const assignee = assignees.reduce((prev, curr) =>
        curr.sequence > prev.sequence ? curr : prev
        , assignees[0]);

        userId = assignee.userId
    }

    const params = {
      processType: SectionProcessType.PrincipleRentalApproval,
      userId: userId,
      supplyMethodCode: SupplyMethodCode.eighty,
      budget: 1,
    } as defaultAcceptorCriteria;

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync(
      params,
      true);

    if (status === HttpStatusCode.Ok && data.length > 0) {
      procurementDetail.value.acceptors = procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver);

      data.forEach((item) =>
        procurementDetail.value.acceptors.push({
          acceptorType: AcceptorType.Approver,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: procurementDetail.value.acceptors.length + 1,
          status: AcceptorStatus.Draft,
          userId: item.userId,
          departmentName: item.businessUnitName,
          departmentCode: item.businessUnitId,
      } as ParticipantsCommitteeAcceptor));
    }
  };

  const getDefaultJorporAsync = async (): Promise<void> => {
    const { data, status } = await operationService.getJorPorDirectorAsync();

    if (status === HttpStatusCode.Ok) {
      procurementDetail.value.assignees = [{
        assigneeGroup: AssigneeGroup.JorPor,
        assigneeType: AssigneeType.Director,
        departmentName: data.businessUnitName,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        status: AssigneeStatus.Pending,
        userId: data.userId,
      } as ParticipantsAssignee];
    }
  };

  const onCreateCommitteeChange = async (cam02Status: Cam02Status) => {
    const bodySave = { ...procurementDetail.value };
    bodySave.status = cam02Status;

    const { data, status } = await Cam02Service.onCreateAsync(bodySave);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.createdMessageToast();

      return data;
    }
  }

  const onUpdateCommitteeChange = async (id: string, cam02Status: Cam02Status): Promise<void> => {
    const bodySave = { ...procurementDetail.value };
    bodySave.status = cam02Status;

    const { status } = await Cam02Service.onUpdateAsync(id, bodySave);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast()
    }

    if (status == HttpStatusCode.Conflict) {
      ToastHelper.warning("ขออนุมัติเผยแพร่แผนจัดซื้อจัดจ้าง", "ต้องมีผู้รับผิดชอบอย่างน้อย 1 คน");
    }
  }

  const onGetCommitteeChangeById = async (id: string) => {
    const { data, status } = await Cam02Service.onGetByIdAsync(id);

    if (status != HttpStatusCode.Ok) {
      ToastHelper.error('ไม่พบข้อมูล', 'ไม่พบข้อมูล ขอแก้ไขคณะกรรมการ')
    }

    isLoadingData.value = true;
    procurementDetail.value = data;
    await nextTick();
    isLoadingData.value = false;
  };

  const onUpsertAttachments = async () => {
    if (!procurementDetail.value.id) return;

    const { status } = await Cam02Service.onUpsertAttachmentsAsync(
      procurementDetail.value.id,
      procurementDetail.value.attachments
    );

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await onGetCommitteeChangeById(procurementDetail.value.id);
    }
  };

  const onGetCommitteeGroupTypeAsync = async (procurementId: string) => {
    if (!procurementId) return;

    const { data, status } = await Cam02Service.onGetCommitteeGroupTypeAsync(procurementId);

    if (status === HttpStatusCode.Ok) {
      committeeGroupTypeList.value = data;
    }
  };

  const onGetCommitteeBySourceTypeAsync = async (
    sourceId: string,
    sourceType: string,
    committeeGroupType: string
  ) => {
    if (!procurementDetail.value.procurementId) return;

    const { data, status } = await Cam02Service.onGetCommitteeBySourceTypeAsync(
      procurementDetail.value.procurementId,
      sourceId,
      sourceType,
      committeeGroupType
    );

    if (status === HttpStatusCode.Ok) {
      procurementDetail.value.sourceId = sourceId;
      procurementDetail.value.sourceType = sourceType;
      procurementDetail.value.committeeType = committeeGroupType;
      procurementDetail.value.oldCommittees = data.committees;
      procurementDetail.value.newCommittees = JSON.parse(JSON.stringify(data.committees));
    }
  };

  const onApprovedAsync = async () => {
    if (!procurementDetail.value.id) return;

    const acceptor = procurementDetail.value.acceptors.find(
      (a: ParticipantsAcceptor) => a.status === AcceptorStatus.Pending
    );

    if (!acceptor) return;

    const mapTypeReasonDialog = ReasonDialogType.Approve;

    const result = await showReasonDialogAsync(mapTypeReasonDialog);

    if (!result.isConfirm) return;

    const Sendbody = {
      acceptorId: auth.profile.id,
      remark: result.reason
    } as TChangeCommitteeSendAction;

    const { status } = await Cam02Service.onApproveAsync(procurementDetail.value.id, Sendbody);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();
      await onGetCommitteeChangeById(procurementDetail.value.id);
    }
  };

  const onRecallAsync = async (recallStatus: Cam02Status) => {
    if (!procurementDetail.value.id) return;

    const bodySave = { ...procurementDetail.value };
    bodySave.status = recallStatus;

    const { status } = await Cam02Service.onUpdateAsync(procurementDetail.value.id, bodySave);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.recallEditMessageToast();

      await onGetCommitteeChangeById(procurementDetail.value.id);
    }
  };

  const onRejectedAsync = async () => {
    if (!procurementDetail.value.id) return;

    const acceptor = procurementDetail.value.acceptors.find(
      (a: ParticipantsAcceptor) => a.status === AcceptorStatus.Pending
    );

    if (!acceptor) return;

    const result = await showReasonDialogAsync(procurementDetail.value.status === Cam02Status.WaitingCommitteeApproval ? ReasonDialogType.NotAgree : ReasonDialogType.Reject, true);

    if (!result.isConfirm) return;

    const Sendbody = {
      acceptorId: auth.profile.id,
      remark: result.reason
    } as TChangeCommitteeSendAction;

    const { status } = await Cam02Service.onRejectAsync(procurementDetail.value.id, Sendbody);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await onGetCommitteeChangeById(procurementDetail.value.id);
    }
  };

   const onAssigneeRejectedAsync = async () => {
    if (!procurementDetail.value.id) return;

    const result = await showReasonDialogAsync(ReasonDialogType.Reject, true);

    if (!result.isConfirm) return;

    const Sendbody = {
      acceptorId: auth.profile.id,
      remarks: result.reason
    } as TChangeCommitteeSendAction;

    const { status } = await Cam02Service.onRejectAsync(procurementDetail.value.id, Sendbody);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();

      await onGetCommitteeChangeById(procurementDetail.value.id);
    }
  };

  const isCommitteeApproval = computed(() => [Cam02Status.WaitingCommitteeApproval].includes(procurementDetail.value.status!)
      && (procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver).some(s => s.userId == auth.profile.id)));

  const canCommitteeAcceptAndReject = computed(() => {
    if (!procurementDetail.value) return false;

    if (procurementDetail.value.status !== Cam02Status.WaitingCommitteeApproval) return false;

    const acceptorsByType = procurementDetail.value.acceptors.filter(f => f.acceptorType !== AcceptorType.Approver).sort(s => s.sequence);

    if (acceptorsByType.length === 1 && acceptorsByType[0].userId === auth.profile.id) {
      return true;
    }

    const isBoss = acceptorsByType[0].userId === auth.profile.id;

    if (isBoss) {
      return acceptorsByType
        .filter((value, index) => (index !== 0 && !value.isUnableToPerformDuties))
        .every(s => s.status !== AcceptorStatus.Pending);
    }

    return acceptorsByType.some(s => s.userId === auth.profile.id && !s.isUnableToPerformDuties && s.status === AcceptorStatus.Pending);
  });


  const isJorPorSection = computed(() =>
      [Cam02Status.WaitingAssign, Cam02Status.WaitingComment, Cam02Status.RejectToAssignee].includes(procurementDetail.value.status!) &&
      procurementDetail.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isJorPorAssign = computed(() =>
      [Cam02Status.WaitingAssign, Cam02Status.RejectToAssignee].includes(procurementDetail.value.status!) &&
      procurementDetail.value.assignees.some(s => s.assigneeType === AssigneeType.Director && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isJorPorAssignByAssignee = computed(() =>
      [Cam02Status.WaitingAssign, Cam02Status.RejectToAssignee].includes(procurementDetail.value.status!) &&
      procurementDetail.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isJorPorComment = computed(() => {
      if (!procurementDetail.value.assignees) return false;

      const lastAssignee = procurementDetail.value.assignees.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, procurementDetail.value.assignees[0]);
      const hasPermissionUser = (lastAssignee?.delegateeUserId ? lastAssignee?.delegateeUserId : lastAssignee?.userId) === auth.profile.id;

      return [Cam02Status.WaitingComment].includes(procurementDetail.value.status!) && hasPermissionUser;
  });

  const onAssigneeCommentAsync = async (reason: string): Promise<void> => {
    if (!procurementDetail.value.id) return;

    const { status } = await Cam02Service.onAssigneeCommentAsync(procurementDetail.value.id, reason);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.remarkOfficerMessageToast();
      await onGetCommitteeChangeById(procurementDetail.value.id);
    }
  };

  const isCanEdit = computed(() => {
    return (
      [Cam02Status.Draft, Cam02Status.Edit, Cam02Status.Rejected].includes(procurementDetail.value.status!)
    );
  });

  const isCanComment = computed(() => {
    return (
      [Cam02Status.WaitingComment].includes(procurementDetail.value.status!)
    );
  });

  const canRecallCommittee = computed(() => {
    if (!procurementDetail.value.acceptors) return false;

    const status = procurementDetail.value.status === Cam02Status.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = procurementDetail.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType !== AcceptorType.Approver && a.committeePositionsCode == CommitteePositions.PosBoard001);

    return status && !isAnyApprovalAndRejected;
  });

  const isCanReCall = computed(() => {
    if (!procurementDetail.value.assignees) return false;

    const status = procurementDetail.value.status === Cam02Status.WaitingApproval;
    const checkUser = procurementDetail.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (!procurementDetail.value.acceptors) return false;

    const isAnyApprovalAndRejected = procurementDetail.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const isCurrentApprover = computed(() => {
    const isWaitingApproval = [Cam02Status.WaitingApproval].includes(
      procurementDetail.value.status!
    );

    const currentAcceptor = isCurrentPendingAcceptor(procurementDetail.value.acceptors, auth.profile.id);
    return isWaitingApproval && currentAcceptor;
  });

  const isLastApprover = computed(() => {
    const lastApprover =
      procurementDetail.value.acceptors!.filter(
        (s) => s.acceptorType === AcceptorType.Approver && s.status === AcceptorStatus.Pending
      ).length === 1;
    const isWaitingApproval = [Cam02Status.WaitingApproval].includes(
      procurementDetail.value.status!
    );

    return lastApprover && isWaitingApproval;
  });

  const onSetIsUnableToPerformDutiesByIdAsync = async (acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
    if (!procurementDetail.value.id) {
      return;
    }

    if (procurementDetail.value.status === Cam02Status.WaitingCommitteeApproval) {
      const { status } = await Cam02Service.onSetIsUnableToPerformDutiesAsync(procurementDetail.value.id, acceptorId, isUnableToPerformDuties, remark);

      if (status === HttpStatusCode.Ok) {
        ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      }
    }

   await onGetCommitteeChangeById(procurementDetail.value.id);
  };

  const onAssignAsync = async (isConfirm: boolean = false) => {
    if (!procurementDetail.value.id) {
      return;
    }

    if (!isConfirm) {
      const { status } = await Cam02Service.onUpdateAsync(procurementDetail.value.id, procurementDetail.value);

      if (status === HttpStatusCode.Ok) {
        ToastHelper.updatedMessageToast();
      }

      return await onGetCommitteeChangeById(procurementDetail.value.id);
    }

    if (!procurementDetail.value.assignees.some(x => x.assigneeType === AssigneeType.Assignee)) return ToastHelper.assignAtLeastMessageToast();

    if (isConfirm && !await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    const payload = {
      ...procurementDetail.value,
      status: Cam02Status.WaitingComment,
    };

    const { status } = await Cam02Service.onUpdateAsync(procurementDetail.value.id, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
    }

    await onGetCommitteeChangeById(procurementDetail.value.id);
  };

  const onJorPorSendApprovalAsync = async () => {
    if (!procurementDetail.value.id) {
      return;
    }

    if (!procurementDetail.value.assignees.some(x => x.assigneeType === AssigneeType.Assignee)) return ToastHelper.assignAtLeastMessageToast();

    if (!procurementDetail.value.assignees.some(s => s.remark)) {
      return ToastHelper.assignneeCommentAtLeastMessageToast();
    }

    if (!procurementDetail.value.acceptors.some(x => x.acceptorType === AcceptorType.Approver)) return ToastHelper.approvalAtLeastMessageToast();

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendConfirm)) return;

    const payload = {
      ...procurementDetail.value,
      status: Cam02Status.WaitingApproval,
    };

    const { status } = await Cam02Service.onUpdateAsync(procurementDetail.value.id!, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
    }

    await onGetCommitteeChangeById(procurementDetail.value.id);
  };

  return {
    isCanEdit,
    isCanReCall,
    isCurrentApprover,
    isLastApprover,
    isLoadingData,
    procurementDetail,
    committeeGroupTypeList,
    onCreateCommitteeChange,
    onUpdateCommitteeChange,
    resetDetail,
    getDefaultAcceptor,
    getDefaultAcceptorAppointAsync,
    getDefaultAcceptorJp004Async,
    getDefaultAcceptorJp005Async,
    getDefaultAcceptorPrincipleApprovalAsync,
    getDefaultAcceptorPurchaseOrderApprovalAsync,
    getDefaultJorporAsync,
    onGetCommitteeChangeById,
    onUpsertAttachments,
    onGetCommitteeGroupTypeAsync,
    onGetCommitteeBySourceTypeAsync,
    onApprovedAsync,
    onRecallAsync,
    onRejectedAsync,
    isJorPorSection,
    isJorPorAssign,
    isJorPorAssignByAssignee,
    isJorPorComment,
    onAssigneeCommentAsync,
    onSetIsUnableToPerformDutiesByIdAsync,
    onAssignAsync,
    onJorPorSendApprovalAsync,
    onAssigneeRejectedAsync,
    canRecallCommittee,
    canCommitteeAcceptAndReject,
    isCanComment,
    isCommitteeApproval,
  };
});
