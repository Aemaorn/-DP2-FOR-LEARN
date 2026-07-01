import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import type { Option } from '@/models/shared/option';
import type { TPP003Body, TPP003BudgetAllocations } from '../../models/PP003/pp003Model';
import PP003Service from '../../services/PP003/pp003Service';
import { HttpStatusCode } from 'axios';
import ToastHelper from '@/helpers/toast';
import { PP003Status, PP003Template } from '../../enums/pp003';
import { useAuthenticationStore } from '@/stores/authentication';
import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from '@/models/shared/participants';
import { showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import operationService from '@/services/Shared/operations';
import { CommitteeType, TemplateGroup } from '@/enums/shared';
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeStatus, AssigneeType } from '@/enums/participants';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { OrganizationLevel, SectionProcessType } from '@/enums/operations';
import type { CommitteeAcceptor, OperationBody } from '@/models/shared/operations';
import SharedService from '@/services/Shared/dropdown';
import { Template01, Template02, Template03, Template04, Template05, Template06 } from '../../components/PP003/components';
import type { SupplyMethodCode } from '@/enums/supplyMethod';
import { checkIsEighty } from '@/helpers/supplyMethod';
import { CommitteePositions } from '@/enums/PCM005/principle';
import { pp004status } from '../../enums/pp004';
import { isCurrentPendingAcceptor } from '@/helpers/participants';

export const usePP003DetailStore = defineStore('PP-003-detail-store', () => {
  const auth = useAuthenticationStore();
  const procurementStore = usePPDetailStore();

  const initBody = {
    budgetAllocations: {
      budget: procurementStore.procurementDetail.budget ?? 0,
    } as TPP003BudgetAllocations,
    acceptors: [] as Array<ParticipantsCommitteeAcceptor>,
    assignees: [] as Array<ParticipantsAssignee>,
    status: PP003Status.Draft,
  } as TPP003Body;

  const currentTab = ref('detail');

  const medianPriceInfoConsiderOptions = [
    { value: 'ราคาที่ได้มาจากการคำนวณตามหลักเกณฑ์ที่คณะกรรมการราคากลางกำหนด', label: 'ราคาที่ได้มาจากการคำนวณตามหลักเกณฑ์ที่คณะกรรมการราคากลางกำหนด' },
    { value: 'ราคาที่ได้มาจากฐานข้อมูลราคาอ้างอิงของพัสดุที่กรมบัญชีกลางจัดทำ', label: 'ราคาที่ได้มาจากฐานข้อมูลราคาอ้างอิงของพัสดุที่กรมบัญชีกลางจัดทำ' },
    {
      value: 'ราคามาตรฐานที่สำนักงบประมาณหรือหน่วยงานกลางอื่นกำหนด เช่น กระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม กระทรวงสาธารณสุข เป็นต้น',
      label:
        'ราคามาตรฐานที่สำนักงบประมาณหรือหน่วยงานกลางอื่นกำหนด เช่น กระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม กระทรวงสาธารณสุข เป็นต้น',
    },
    { value: 'ราคาที่ได้มาจากการสืบราคาจากท้องตลาด', label: 'ราคาที่ได้มาจากการสืบราคาจากท้องตลาด' },
    { value: 'ราคาที่เคยซื้อหรือจ้างหรือเช่าครั้งสุดท้ายภายในระยะเวลาสองปีงบประมาณ', label: 'ราคาที่เคยซื้อหรือจ้างหรือเช่าครั้งสุดท้ายภายในระยะเวลาสองปีงบประมาณ' },
    { value: 'ราคาอื่นใดตามหลักเกณฑ์ วิธีการ หรือแนวทางปฏิบัติของหน่วยงานของรัฐนั้น ๆ', label: 'ราคาอื่นใดตามหลักเกณฑ์ วิธีการ หรือแนวทางปฏิบัติของหน่วยงานของรัฐนั้น ๆ' },
  ] as Option[];

  const templateOptions = ref<Array<Option>>([] as Array<Option>);

  const TEMPLATE_COMPONENTS: Record<PP003Template, any> = {
    // Template 01
    [PP003Template.MedianPriceBoKor0180]: Template01,
    [PP003Template.MedianPriceBoKor0160]: Template01,
    [PP003Template.MedianPriceBoKor01ForJorporComment80]: Template01,
    [PP003Template.MedianPriceBoKor01ForJorporComment60]: Template01,
    [PP003Template.MedianPriceChangeBoKor0180]: Template01,
    [PP003Template.MedianPriceChangeBoKor0160]: Template01,
    [PP003Template.MedianPriceChangeBoKor01ForJorporComment80]: Template01,
    [PP003Template.MedianPriceChangeBoKor01ForJorporComment60]: Template01,
    [PP003Template.MedianPriceCancelBoKor0180]: Template01,
    [PP003Template.MedianPriceCancelBoKor0160]: Template01,
    [PP003Template.MedianPriceCancelBoKor01ForJorporComment80]: Template01,
    [PP003Template.MedianPriceCancelBoKor01ForJorporComment60]: Template01,

    // Template 02
    [PP003Template.MedianPriceBoKor0280]: Template02,
    [PP003Template.MedianPriceBoKor0260]: Template02,
    [PP003Template.MedianPriceBoKor02ForJorporComment80]: Template02,
    [PP003Template.MedianPriceBoKor02ForJorporComment60]: Template02,
    [PP003Template.MedianPriceChangeBoKor0280]: Template02,
    [PP003Template.MedianPriceChangeBoKor0260]: Template02,
    [PP003Template.MedianPriceChangeBoKor02ForJorporComment80]: Template02,
    [PP003Template.MedianPriceChangeBoKor02ForJorporComment60]: Template02,
    [PP003Template.MedianPriceCancelBoKor0280]: Template02,
    [PP003Template.MedianPriceCancelBoKor0260]: Template02,
    [PP003Template.MedianPriceCancelBoKor02ForJorporComment80]: Template02,
    [PP003Template.MedianPriceCancelBoKor02ForJorporComment60]: Template02,

    // Template 03
    [PP003Template.MedianPriceBoKor0380]: Template03,
    [PP003Template.MedianPriceBoKor0360]: Template03,
    [PP003Template.MedianPriceBoKor03ForJorporComment80]: Template03,
    [PP003Template.MedianPriceBoKor03ForJorporComment60]: Template03,
    [PP003Template.MedianPriceChangeBoKor0380]: Template03,
    [PP003Template.MedianPriceChangeBoKor0360]: Template03,
    [PP003Template.MedianPriceChangeBoKor03ForJorporComment80]: Template03,
    [PP003Template.MedianPriceChangeBoKor03ForJorporComment60]: Template03,
    [PP003Template.MedianPriceCancelBoKor0380]: Template03,
    [PP003Template.MedianPriceCancelBoKor0360]: Template03,
    [PP003Template.MedianPriceCancelBoKor03ForJorporComment80]: Template03,
    [PP003Template.MedianPriceCancelBoKor03ForJorporComment60]: Template03,

    // Template 04
    [PP003Template.MedianPriceBoKor0480]: Template04,
    [PP003Template.MedianPriceBoKor0460]: Template04,
    [PP003Template.MedianPriceBoKor04ForJorporComment80]: Template04,
    [PP003Template.MedianPriceBoKor04ForJorporComment60]: Template04,
    [PP003Template.MedianPriceChangeBoKor0480]: Template04,
    [PP003Template.MedianPriceChangeBoKor0460]: Template04,
    [PP003Template.MedianPriceChangeBoKor04ForJorporComment80]: Template04,
    [PP003Template.MedianPriceChangeBoKor04ForJorporComment60]: Template04,
    [PP003Template.MedianPriceCancelBoKor0480]: Template04,
    [PP003Template.MedianPriceCancelBoKor0460]: Template04,
    [PP003Template.MedianPriceCancelBoKor04ForJorporComment80]: Template04,
    [PP003Template.MedianPriceCancelBoKor04ForJorporComment60]: Template04,

    // Template 05
    [PP003Template.MedianPriceBoKor0580]: Template05,
    [PP003Template.MedianPriceBoKor0560]: Template05,
    [PP003Template.MedianPriceBoKor05ForJorporComment80]: Template05,
    [PP003Template.MedianPriceBoKor05ForJorporComment60]: Template05,
    [PP003Template.MedianPriceChangeBoKor0580]: Template05,
    [PP003Template.MedianPriceChangeBoKor0560]: Template05,
    [PP003Template.MedianPriceChangeBoKor05ForJorporComment80]: Template05,
    [PP003Template.MedianPriceChangeBoKor05ForJorporComment60]: Template05,
    [PP003Template.MedianPriceCancelBoKor0580]: Template05,
    [PP003Template.MedianPriceCancelBoKor0560]: Template05,
    [PP003Template.MedianPriceCancelBoKor05ForJorporComment80]: Template05,
    [PP003Template.MedianPriceCancelBoKor05ForJorporComment60]: Template05,

    // Template 06
    [PP003Template.MedianPriceBoKor0680]: Template06,
    [PP003Template.MedianPriceBoKor0660]: Template06,
    [PP003Template.MedianPriceBoKor06ForJorporComment80]: Template06,
    [PP003Template.MedianPriceBoKor06ForJorporComment60]: Template06,
    [PP003Template.MedianPriceChangeBoKor0680]: Template06,
    [PP003Template.MedianPriceChangeBoKor0660]: Template06,
    [PP003Template.MedianPriceChangeBoKor06ForJorporComment80]: Template06,
    [PP003Template.MedianPriceChangeBoKor06ForJorporComment60]: Template06,
    [PP003Template.MedianPriceCancelBoKor0680]: Template06,
    [PP003Template.MedianPriceCancelBoKor0660]: Template06,
    [PP003Template.MedianPriceCancelBoKor06ForJorporComment80]: Template06,
    [PP003Template.MedianPriceCancelBoKor06ForJorporComment60]: Template06,
  };


  const isChangeTemplate = ref(false);
  const body = ref<TPP003Body>(structuredClone(initBody));

  const resetBody = () => {
    body.value = structuredClone(initBody);
  };

  const acceptorsByType = (acceptorType: AcceptorType) => body.value.acceptors.filter(f => f.acceptorType === acceptorType).sort(s => s.sequence);

  const assigneeByType = (assigneeType: AssigneeType) => body.value.assignees.filter(f => f.assigneeType === assigneeType).sort(s => s.sequence);

  const setIsChangeTemplate = (value: boolean) => {
    isChangeTemplate.value = value;
  };

  const onResetbudgetAllocationsDetail = () => {
    body.value.budgetAllocations = structuredClone({
      budget: procurementStore.procurementDetail.budget ?? 0,
    } as TPP003BudgetAllocations);
  };

  const onGetTemplateOptionsAsync = async (params?: { isCancel?: boolean, isChange?: boolean }) => {
    const { data, status } = await SharedService.onGetTemplateDropdownByGroupCodeAsync([TemplateGroup.Mdp], {
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode as SupplyMethodCode,
      budget: procurementStore.procurementDetail.budget,
      isJorPorComment: procurementStore.procurementDetail.hasMd ? procurementStore.procurementDetail.hasMd : undefined,
      isCancel: params?.isCancel,
      isChange: params?.isChange,
    }, true);

    if (status === HttpStatusCode.Ok) {
      templateOptions.value = data;
    }
  };

  const onGetByIdAsync = async (procurementId: string, id: string) => {
    const { data, status } = await PP003Service.onGetByIdAsync(procurementId, id);

    if (status === HttpStatusCode.Ok) {

      if (data.isCancel) {
        await onGetTemplateOptionsAsync({ isCancel: data.isCancel });
      }

      if (data.isChange) {
        await onGetTemplateOptionsAsync({ isChange: data.isChange });
      }


      body.value = data;
      body.value.isMedianPriceDocumentIdReplaced = false;
      setIsChangeTemplate(false);
    }
  };

  const onSubmitAsync = async (procurementId: string) => {
    if (body.value.id) {
      await onUpdateAsync(procurementId, body.value.id, body.value.status);
      setCurrentTab();
      return;
    }

    await onCreateAsync(procurementId, body.value.status);
    setCurrentTab();
  };

  const setCurrentTab = async () => {
    if (body.value.medianPriceDocumentId) {
      currentTab.value = 'document';
    }
  };

  const onCommitteeSendApproveAsync = async (procurementId: string) => {
    const { hasMd } = procurementStore.procurementDetail;

    const requiredAcceptorType = hasMd
      ? AcceptorType.DepartmentDirectorAgree
      : AcceptorType.Approver;

    const toast = hasMd
      ? () => ToastHelper.departmentAtLeastMessageToast()
      : () => ToastHelper.approvalAtLeastMessageToast();

    if (!body.value.acceptors.some(f => f.acceptorType === requiredAcceptorType)) {
      return toast();
    }

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendApprove)) return;

    if (body.value.id) {
      await onUpdateAsync(procurementId, body.value.id, PP003Status.WaitingCommitteeApproval);

      return;
    }

    await onCreateAsync(procurementId, PP003Status.WaitingCommitteeApproval);
  };

  const onRecallAsync = async (procurementId: string, medianPriceId: string) => {
    if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;
    await onUpdateAsync(procurementId, medianPriceId, body.value.status == PP003Status.WaitingCommitteeApproval ? PP003Status.Edit : PP003Status.WaitingComment);
  };

  const onCreateAsync = async (procurementId: string, medianPriceStatus: PP003Status) => {
    const payload = {
      ...body.value,
      status: medianPriceStatus,
    } as TPP003Body;

    const { data, status } = await PP003Service.onCreateAsync(procurementId, payload);

    if (status === HttpStatusCode.Created) {
      body.value.id = data;
      ToastHelper.createdMessageToast();
      await procurementStore.onGetProcurementById(procurementId);
      await onGetByIdAsync(procurementId, data);
    }
  };

  const onUpdateAsync = async (procurementId: string, medianPriceId: string, medianPriceStatus: PP003Status) => {
    const payload = {
      ...body.value,
      status: medianPriceStatus,
    } as TPP003Body;

    const toast = {
      [PP003Status.WaitingCommitteeApproval]: () => ToastHelper.sendApproveConfirmMessageToast(),
      [PP003Status.Edit]: () => ToastHelper.recallEditMessageToast(),
    }

    const { status, data } = await PP003Service.onUpdateAsync(procurementId, medianPriceId, payload);

    if (status === HttpStatusCode.Ok) {
      // Update document ID immediately if new version was created
      if (data?.newDocumentFileId) {
        body.value.medianPriceDocumentId = data.newDocumentFileId;
      }

      [PP003Status.WaitingCommitteeApproval, PP003Status.Edit].includes(medianPriceStatus) ? toast[medianPriceStatus as PP003Status.WaitingCommitteeApproval | PP003Status.Edit]() : ToastHelper.updatedMessageToast();
    }

    await onGetByIdAsync(procurementId, medianPriceId);
  };

  const setDefaultCommitteeAsync = async (procurementId: string) => {
    const { data, status } = await operationService.getCommitteAcceptorsAsync(procurementId, CommitteeType.MedianPrice);

    if (status === HttpStatusCode.Ok) {
      body.value.object = data.objAndReason.objective;
      body.value.reason = data.objAndReason.reason;
      body.value.specialDescription = data.objAndReason.specificDescription;
      body.value.torTemplate = data.objAndReason.torTemplate;

      body.value.acceptors = data.committees.map((value: CommitteeAcceptor): ParticipantsCommitteeAcceptor => ({
        ...value,
        acceptorType: AcceptorType.MedianPriceCommittee,
        status: AcceptorStatus.Draft,
      }));
    }
  };

  const setDefaultAcceptorAsync = async (isDisabledLoad: boolean = true) => {
    if (
      (procurementStore.procurementDetail.hasMd && assigneeByType(AssigneeType.Assignee).length === 0) ||
      (!procurementStore.procurementDetail.hasMd && acceptorsByType(AcceptorType.MedianPriceCommittee).length === 0)
    ) return;

    const users = procurementStore.procurementDetail.hasMd ?
      assigneeByType(AssigneeType.Assignee)[0] ?? assigneeByType(AssigneeType.Director)[0]
      : acceptorsByType(AcceptorType.MedianPriceCommittee).filter(f => f.departmentCode === procurementStore.procurementDetail.departmentCode)
        .reduce((prev, curr) => prev.sequence > curr.sequence ? prev : curr,
          acceptorsByType(AcceptorType.MedianPriceCommittee).filter(f => f.departmentCode === procurementStore.procurementDetail.departmentCode)[0]);

    if (!users) {
      return;
    }

    let processType: SectionProcessType = procurementStore.procurementDetail.hasMd ? SectionProcessType.MedianPriceHasMD : SectionProcessType.MedianPrice;

    const detail = procurementStore.procurementDetail;
    const is80 = checkIsEighty(detail.supplyMethodCode);
    if (is80) {
      if (detail.isStock) {
        processType = SectionProcessType.MedianPriceStock;
      } else if (detail.isCommercialMaterial) {
        processType = procurementStore.procurementDetail.hasMd ? SectionProcessType.MedianPriceCommercialParcelHasMD : SectionProcessType.MedianPriceCommercialParcel;
      }
    }

    const { data, status } = await operationService.getOperationsDefaultAcceptorAsync({
      processType: processType,
      budget: procurementStore.procurementDetail.budget,
      userId: users.userId,
      supplyMethodCode: procurementStore.procurementDetail.supplyMethodCode,
      supplyMethodSpecialTypeCode: procurementStore.procurementDetail.supplyMethodSpecialTypeCode,
    }, isDisabledLoad);

    if (status === HttpStatusCode.Ok) {
      const temp = body.value.acceptors.filter(r => r.acceptorType !== AcceptorType.Approver).sort(s => s.sequence);

      body.value.acceptors = [
        ...temp,
        ...data.map((item: OperationBody, index: number): ParticipantsCommitteeAcceptor => ({
          acceptorType: AcceptorType.Approver,
          departmentName: item.businessUnitName,
          fullName: item.fullName,
          positionName: item.fullPositionName,
          sequence: index + 1,
          userId: item.userId,
          status: AcceptorStatus.Draft,
          isUnableToPerformDuties: false,
        })),
      ];
    }
  };

  const setDefaultJorPorDirectorAsync = async (isDisabledLoad?: boolean) => {
    const { data, status } = await operationService.getJorPorDirectorAsync(isDisabledLoad);

    if (status === HttpStatusCode.Ok) {
      body.value.assignees.push({
        userId: data.userId,
        fullName: data.fullName,
        positionName: data.fullPositionName,
        sequence: 1,
        departmentName: data.businessUnitName,
        status: AssigneeStatus.Draft,
        assigneeType: AssigneeType.Director,
        assigneeGroup: AssigneeGroup.JorPor,
      } as ParticipantsAssignee);
    }
  };

  const setDefaultUnitAsync = async (isDisabledLoad?: boolean) => {
    const userListData = acceptorsByType(AcceptorType.MedianPriceCommittee).filter(f => f.departmentCode == procurementStore.procurementDetail.departmentCode);

    const { data, status } = await operationService.getDefaultDepartmentApproverByUserIdAsync(
      userListData[userListData.length - 1].userId,
      OrganizationLevel.Group,
      isDisabledLoad);

    if (status === HttpStatusCode.Ok) {
      const temp = body.value.acceptors.filter(r => r.acceptorType !== AcceptorType.DepartmentDirectorAgree).sort(s => s.sequence);

      body.value.acceptors = [
        ...temp,
        ...data.map((item: OperationBody, index: number): ParticipantsCommitteeAcceptor => (
          {
            acceptorType: AcceptorType.DepartmentDirectorAgree,
            departmentName: item.businessUnitName,
            fullName: item.fullName,
            positionName: item.fullPositionName,
            sequence: index + 1,
            userId: item.userId,
            status: AcceptorStatus.Draft,
            isUnableToPerformDuties: false,
          }
        )),
      ];
    }
  };

  const onSetIsUnableToPerformDutiesByIdAsync = async (acceptorId: string, isUnableToPerformDuties: boolean, remark?: string) => {
    if (!body.value.procurementId || !body.value.id) {
      return;
    }

    body.value.isMedianPriceDocumentIdReplaced = false;

    if (body.value.status !== PP003Status.WaitingCommitteeApproval) {
      const payload = {
        ...body.value,
        acceptors: [
          ...body.value.acceptors.filter(f => f.acceptorType != AcceptorType.MedianPriceCommittee),
          ...body.value.acceptors.filter(f => f.acceptorType == AcceptorType.MedianPriceCommittee)
            .map(m => m.id === acceptorId ? { ...m, isUnableToPerformDuties: isUnableToPerformDuties, remark: remark } : m)],
      } as TPP003Body;

      const { status } = await PP003Service.onUpdateAsync(body.value.procurementId, body.value.id, payload);

      if (status === HttpStatusCode.Ok) {
        ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      }
    }

    if (body.value.status === PP003Status.WaitingCommitteeApproval) {
      const { status } = await PP003Service.onSetIsUnableToPerformDutiesAsync(body.value.procurementId, body.value.id, acceptorId, isUnableToPerformDuties, remark);

      if (status === HttpStatusCode.Ok) {
        ToastHelper.success('แก้ไขสถานะการปฏิบัติงาน', 'แก้ไขสถานะการปฏิบัติงานสำเร็จ');
      }
    }

    await onGetByIdAsync(body.value.procurementId, body.value.id);
  };

  const onJorPorCommentAsync = async (procurementId: string, medianPriceId: string, remark?: string) => {
    const { status } = await PP003Service.onJorPorCommentAsync(procurementId, medianPriceId, { remark });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.remarkOfficerMessageToast();
    }

    await onGetByIdAsync(body.value.procurementId!, body.value.id!);
  };

  const onApprovedByTypeAsync = async (procurementId: string, medianPriceId: string, group: AcceptorType, isApprove: boolean = false) => {
    const reasonMap: Record<AcceptorType, ReasonDialogType> = {
      [AcceptorType.DepartmentDirectorAgree]: ReasonDialogType.Accepted,
      [AcceptorType.TorDraftCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.MedianPriceCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.Approver]: isApprove ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
      [AcceptorType.ProcurementCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.RentCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.Jp005Committee]: ReasonDialogType.Accepted,
      [AcceptorType.AcceptanceCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.InspectionCommittee]: ReasonDialogType.Accepted,
      [AcceptorType.AcceptorSign]: ReasonDialogType.Accepted,
      [AcceptorType.Accounting]: ReasonDialogType.Accepted,
      [AcceptorType.AccountingApprover]: ReasonDialogType.Accepted,
      [AcceptorType.AccountingConfirmer]: ReasonDialogType.Accepted,
      [AcceptorType.Reviewer]: ReasonDialogType.Accepted,
      [AcceptorType.AccountingOperator]: ReasonDialogType.Accepted,
    };

    const reasonType = reasonMap[group] ?? ReasonDialogType.RemarkOfficer;

    const resp = await showReasonDialogAsync(reasonType);
    if (!resp.isConfirm) {
      return;
    }

    const { status } = await PP003Service.onApprovedByTypeAsync(procurementId, medianPriceId, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.approvedMessageToast();
    }

    await onGetByIdAsync(procurementId, medianPriceId);
    await procurementStore.onGetProcurementById(procurementId);
  };

  const onRejectByTypeAsync = async (procurementId: string, medianPriceId: string, isCommittee = false) => {
    const resp = await showReasonDialogAsync(isCommittee ? ReasonDialogType.NotAgree : ReasonDialogType.Reject, true);

    if (!resp.isConfirm) return;

    const { status } = await PP003Service.onRejectByTypeAsync(procurementId, medianPriceId, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      isCommittee ? ToastHelper.notAgreeMessageToast() : ToastHelper.sendEditMessageToast();
    }

    await onGetByIdAsync(procurementId, medianPriceId);
  };

  const onAssigneeRejectAsync = async (procurementId: string, medianPriceId: string) => {
    const resp = await showReasonDialogAsync(ReasonDialogType.Reject);

    if (!resp.isConfirm) return;

    const { status } = await PP003Service.onRejectByTypeAsync(procurementId, medianPriceId, { remark: resp.reason });

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendEditMessageToast();
    }

    await onGetByIdAsync(procurementId, medianPriceId);
  }

  const onAssignAsync = async (isConfirm: boolean = false) => {
    if (!isConfirm) {
      const { status } = await PP003Service.onUpdateAsync(body.value.procurementId!, body.value.id!, body.value);

      if (status === HttpStatusCode.Ok) {
        ToastHelper.updatedMessageToast();
      }

      return await onGetByIdAsync(body.value.procurementId!, body.value.id!);
    }

    if (!body.value.assignees.some(x => x.assigneeType === AssigneeType.Assignee)) return ToastHelper.assignAtLeastMessageToast();

    if (isConfirm && !await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

    const payload = {
      ...body.value,
      status: PP003Status.WaitingComment,
    } as TPP003Body;

    const { status } = await PP003Service.onUpdateAsync(body.value.procurementId!, body.value.id!, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.assignedMessageToast();
    }

    await onGetByIdAsync(body.value.procurementId!, body.value.id!);
  };

  const onJorPorSendApprovalAsync = async () => {
    if (!body.value.assignees.some(x => x.assigneeType === AssigneeType.Assignee)) return ToastHelper.assignAtLeastMessageToast();

    if (!body.value.assignees.some(s => s.remark)) {
      return ToastHelper.assignneeCommentAtLeastMessageToast();
    }

    if (!body.value.acceptors.some(x => x.acceptorType === AcceptorType.Approver)) return ToastHelper.approvalAtLeastMessageToast();

    if (!await showConfirmDialogAsync(ConfirmDialogType.SendConfirm)) return;

    const payload = {
      ...body.value,
      status: PP003Status.WaitingApproval,
    } as TPP003Body;

    const { status } = await PP003Service.onUpdateAsync(body.value.procurementId!, body.value.id!, payload);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.sendApproveConfirmMessageToast();
    }

    await onGetByIdAsync(body.value.procurementId!, body.value.id!);
  };

  const onRequestChangeOrCancelled = async (isCancel = false) => {
    const resp = await showReasonDialogAsync(isCancel ? ReasonDialogType.RequestCancel : ReasonDialogType.RequestChange);

    if (!resp.isConfirm || !resp.reason) return;

    const { data, status } = await PP003Service.onRequestChangeOrCancelledAsync(body.value.procurementId!, body.value.id!, { reason: resp.reason, isCancel });

    if (status === HttpStatusCode.Created) {
      await procurementStore.onGetProcurementById(body.value.procurementId!);
      await onGetByIdAsync(body.value.procurementId!, data);

      isCancel ? ToastHelper.canceledMessageToast() : ToastHelper.changedMessageToast();
    }
  }

  /**
   * Committee Section
   * */
  const isEditor = computed(() =>
    [PP003Status.Draft, PP003Status.Edit, PP003Status.Rejected].includes(body.value.status)
    && (acceptorsByType(AcceptorType.MedianPriceCommittee).some(s => s.userId == auth.profile.id)));

  const isCanSetDefaultUnit = computed(() => [PP003Status.Draft, PP003Status.Edit].includes(body.value.status));

  const canCommitteeRecall = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === PP003Status.WaitingCommitteeApproval;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.MedianPriceCommittee && a.committeePositionsCode == CommitteePositions.PosBoard001);

    const isCommittee = body.value.acceptors.some(s => s.acceptorType === AcceptorType.MedianPriceCommittee && s.userId === auth.profile.id);

    return status && !isAnyApprovalAndRejected && isCommittee;
  });

  const isCanRecall = computed(() => {
    if (!body.value.assignees) return false;

    const status = body.value.status === PP003Status.WaitingApproval;
    const checkUser = body.value.assignees.some(a => a.assigneeType === AssigneeType.Assignee && (a.delegateeUserId ? a.delegateeUserId : a.userId) === auth.profile.id);

    if (!body.value.acceptors) return false;

    const isAnyApprovalAndRejected = body.value.acceptors.some(a => [AcceptorStatus.Approved, AcceptorStatus.Rejected].includes(a.status) && a.acceptorType === AcceptorType.Approver);

    return status && checkUser && !isAnyApprovalAndRejected;
  });

  const isCommitteeApproval = computed(() => [PP003Status.WaitingCommitteeApproval].includes(body.value.status)
    && (acceptorsByType(AcceptorType.MedianPriceCommittee).some(s => s.userId == auth.profile.id)));

  const isCommitteeCurrentApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (acceptorsByType(AcceptorType.MedianPriceCommittee).length === 1) {
      return true;
    }

    const isBoss = acceptorsByType(AcceptorType.MedianPriceCommittee)[0].userId === auth.profile.id;

    if (isBoss) {
      return acceptorsByType(AcceptorType.MedianPriceCommittee)
        .filter((value, index) => (index !== 0 && !value.isUnableToPerformDuties))
        .every(s => s.status !== AcceptorStatus.Pending);
    }

    return acceptorsByType(AcceptorType.MedianPriceCommittee)
      .some(s => s.userId === auth.profile.id && !s.isUnableToPerformDuties && s.status === AcceptorStatus.Pending);
  });

  const isBossCommitteeApproval = computed(() => {
    if (!isCommitteeApproval.value) return false;

    if (acceptorsByType(AcceptorType.MedianPriceCommittee).length === 1) {
      return true;
    }

    const isBoss = acceptorsByType(AcceptorType.MedianPriceCommittee)[0].userId === auth.profile.id;

    return isBoss;
  });

  /**
  * Unit Section
  * */
  const isUnitApproval = computed(() => [PP003Status.WaitingUnitApproval].includes(body.value.status)
    && (acceptorsByType(AcceptorType.DepartmentDirectorAgree).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) == auth.profile.id)));

  const isCurrentUnitApproval = computed(() => {
    if (!isUnitApproval.value) return false;

    if (!body.value.acceptors) return false;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.DepartmentDirectorAgree);
    return checkQue;
  });

  const isLastUnitApproval = computed(() => {
    if (acceptorsByType(AcceptorType.DepartmentDirectorAgree).length === 0) return false;

    const unitData = acceptorsByType(AcceptorType.DepartmentDirectorAgree)
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = unitData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        unitData[0]);

    return isCurrentUnitApproval.value && current.sequence === unitData[unitData.length - 1].sequence;
  });

  /**
  * Assignee Section
  * */
  const isJorPorSection = computed(() =>
    [PP003Status.WaitingAssign, PP003Status.WaitingComment, PP003Status.RejectToAssignee].includes(body.value.status) &&
    body.value.assignees.some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isJorPorAssign = computed(() =>
    [PP003Status.WaitingAssign, PP003Status.RejectToAssignee].includes(body.value.status) &&
    body.value.assignees.some(s => s.assigneeType === AssigneeType.Director && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isJorPorAssignByAssignee = computed(() =>
    [PP003Status.WaitingAssign, PP003Status.RejectToAssignee].includes(body.value.status) &&
    body.value.assignees.some(s => s.assigneeType === AssigneeType.Assignee && (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isJorPorComment = computed(() => {
    if (!body.value.assignees) return false;

    const lastAssignee = body.value.assignees.reduce((prev, curr) => curr.sequence > prev.sequence ? curr : prev, body.value.assignees[0]);
    const hasPermissionUser = (lastAssignee?.delegateeUserId ? lastAssignee?.delegateeUserId : lastAssignee?.userId) === auth.profile.id;

    return [PP003Status.WaitingComment].includes(body.value.status) && hasPermissionUser;
  });

  /**
  * Acceptor Section
  * */
  const isAcceptorApproval = computed(() => [PP003Status.WaitingApproval].includes(body.value.status) &&
    acceptorsByType(AcceptorType.Approver).some(s => (s.delegateeUserId ? s.delegateeUserId : s.userId) === auth.profile.id));

  const isCurrentAcceptorApproval = computed(() => {
    if (!isAcceptorApproval.value) {
      return false;
    }

    if (!body.value.acceptors) return false;
    const checkQue = isCurrentPendingAcceptor(body.value.acceptors, auth.profile.id, AcceptorType.Approver);

    return checkQue;
  });

  const isLastAcceptorApproval = computed(() => {
    if (acceptorsByType(AcceptorType.Approver).length === 0) return false;

    const acceptorData = acceptorsByType(AcceptorType.Approver)
      .filter(f => f.status === AcceptorStatus.Pending);

    const current = acceptorData
      .reduce((prev, curr) => curr.sequence < prev.sequence ? curr : prev,
        acceptorData[0]);

    return isCurrentAcceptorApproval.value && current.sequence === acceptorData[acceptorData.length - 1].sequence;
  })

  const isMangeMd = computed(() => {
    if (procurementStore.procurementDetail.hasMd) {
      return isJorPorComment.value || isJorPorAssign.value || isJorPorAssignByAssignee.value;
    }

    return isEditor.value;
  });

  const isCanSetDefaultApprover = computed(() => {
    if (procurementStore.procurementDetail.hasMd) {
      return isJorPorComment.value || isJorPorAssign.value;
    }

    return isCanSetDefaultUnit.value;
  });

  const currentTemplate = computed(() => {
    if (body.value.medianPriceDocumentTemplateCode) {
      const templateSelected = body.value.medianPriceDocumentTemplateCode as PP003Template;
      return TEMPLATE_COMPONENTS[templateSelected];
    }

    return null;
  });

  const isCommittee = computed(() => (acceptorsByType(AcceptorType.MedianPriceCommittee).some(s => s.userId == auth.profile.id)));

  const canCancelOrChange = computed(() => {
    if (!body.value.acceptors) return false;

    const status = body.value.status === PP003Status.Approved && !body.value.isCancel;
    const checkUser = body.value.acceptors.some(a => a.acceptorType === AcceptorType.TorDraftCommittee &&
      a.userId === auth.profile.id);
    return status && checkUser;
  });

  const isCancelOrChange = computed(() => {
    const isMedianPriceNotSubmitted = !procurementStore.procurementDetail.purchaseRequisition?.status || [pp004status.Draft, pp004status.Rejected, pp004status.Edit].includes(procurementStore.procurementDetail.purchaseRequisition?.status as pp004status);

    return isCommittee.value && [PP003Status.Approved].includes(body.value.status) && !body.value.isCancel && procurementStore.procurementDetail.departmentCode === auth.profile.departmentCode && isMedianPriceNotSubmitted
  });

  const conRestoreState = computed(() => {
    return isEditor.value && (body.value.isChange || body.value.isCancel);
  });

  const getReviewDocumentAsync = async (id: string, procurementId: string): Promise<string> => {
    const { data, status } = await PP003Service.getReviewDocumentAsync(id, procurementId);
    if (status === HttpStatusCode.Ok) {
      return data;
    }

    ToastHelper.error("ไม่สามารถดึงเอกสารได้", "เกิดข้อผิดพลาดในการดึงเอกสารการตรวจสอบ");
    return '';
  };

  const onRestoreStateAsync = async () => {
    if (!body.value.id) return;

    const res = await showReasonDialogAsync(ReasonDialogType.Confirm, true, body.value.isChange ? 'ยืนยันการคืนสถานะคำขอเปลี่ยนแปลง' : 'ยืนยันการคืนสถานะคำขอยกเลิก');

    if (!res.isConfirm) return;

    if (!res.reason) return;

    const { data, status } = await PP003Service.restoreStateAsync(body.value.id, res.reason);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();

      await onGetByIdAsync(body.value.procurementId!, data);

      await procurementStore.onGetProcurementById(procurementStore.procurementDetail.id!);
    }
  }

  return {
    body,
    acceptorsByType,
    templateOptions,
    medianPriceInfoConsiderOptions,
    onResetbudgetAllocationsDetail,
    onSubmitAsync,
    onCommitteeSendApproveAsync,
    onRecallAsync,
    onGetByIdAsync,
    resetBody,
    setDefaultCommitteeAsync,
    setDefaultAcceptorAsync,
    setDefaultJorPorDirectorAsync,
    setDefaultUnitAsync,
    onApprovedByTypeAsync,
    onRejectByTypeAsync,
    onAssignAsync,
    onJorPorSendApprovalAsync,
    onSetIsUnableToPerformDutiesByIdAsync,
    onJorPorCommentAsync,
    onGetTemplateOptionsAsync,
    isChangeTemplate,
    setIsChangeTemplate,
    onAssigneeRejectAsync,
    getReviewDocumentAsync,
    onRequestChangeOrCancelled,
    currentTab,
    onRestoreStateAsync,
    states: {
      isEditor,
      isCanSetDefaultUnit,
      isCommitteeApproval,
      isCommitteeCurrentApproval,
      isBossCommitteeApproval,
      isUnitApproval,
      isCurrentUnitApproval,
      isLastUnitApproval,
      isJorPorSection,
      isJorPorAssign,
      isJorPorAssignByAssignee,
      isJorPorComment,
      isAcceptorApproval,
      isCurrentAcceptorApproval,
      isLastAcceptorApproval,
      isMangeMd,
      currentTemplate,
      isCommittee,
      isCancelOrChange,
      isCanRecall,
      isCanSetDefaultApprover,
      canCommitteeRecall,
      canCancelOrChange,
      conRestoreState,
    },
  };
});
