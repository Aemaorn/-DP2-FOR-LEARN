<script setup lang="ts">
import type { MenuItem } from 'primevue/menuitem';
import { TitleHeader, TabHeader, AccordHeader } from '@/components/cosmetic';
import { Tabs, TabPanels, TabPanel, Popover } from 'primevue';
import { ProcurementPlanType, ProcurementTab } from '@/enums/procurement';
import { computed, defineAsyncComponent, nextTick, onBeforeMount, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import { usePL001DetailStore } from '@/stores/PL/pl001';
import { UploadFileGroup } from '@/components/forms';
import { useRoute, useRouter } from 'vue-router';
import { PlanAction, PlanStatus } from '@/enums/plan';
import {
  showActivityDialog,
  showConfirmDialogAsync,
  showReasonDialogAsync,
  showSaveOptionDialogAsync,
} from '@/helpers/dialog';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';
import { AcceptorStatus, AcceptorType, AssigneeType } from '@/enums/participants';
import {
  ButtonApprove,
  ButtonConfirm,
  ButtonConfirmAssign,
  ButtonPublish,
  ButtonRecall,
  ButtonSave,
  ButtonSendApprove,
  ButtonSendCancel,
  ButtonSendChange,
  ButtonSendEdit,
  ButtonApproveConfirm,
} from '@/components/Button';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import ProcurementConstants from '@/constants/procurement';
import PlanConstant from '@/constants/plan';
import ToastHelper from '@/helpers/toast';
import DocumentMapping from '@/components/DocumentMapping.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import { useMenuStore } from '@/stores/menu';
import { useAuthenticationStore } from '@/stores/authentication';
import { BadgeStatus } from '@/components';
import { ProgramHistoryName } from '@/enums/programHistoryName';
import planService from '@/services/PL/pl001';
import ppService from '@/services/PP/ppService';
import { HttpStatusCode } from 'axios';
import { SupplyMethodCode } from '@/enums/supplyMethod';
import { AssignDepartmentCodeEnum } from '@/enums/shared';
import { ProcurementType } from '@/enums/procurement';

const Detail = defineAsyncComponent(() => import('@/views/PL/PL001/components/Detail.vue'));
const RequestApprove = defineAsyncComponent(
  () => import('@/views/PL/PL001/components/RequestApprove.vue')
);
const Announcement = defineAsyncComponent(
  () => import('@/views/PL/PL001/components/Announcement.vue')
);

const requestApproveDocumentRef = ref<InstanceType<typeof RequestApprove> | null>(null);
const announcementDocumentRef = ref<InstanceType<typeof Announcement> | null>(null);
const store = usePL001DetailStore();
const authStore = useAuthenticationStore();
const route = useRoute();
const router = useRouter();
const menStore = useMenuStore();
const { PlanStatusColor, PlanStatusName } = PlanConstant;

const routeItems = ref([
  { label: 'รายการจัดซื้อจัดจ้าง', url: '/pl/pl001' },
  { label: 'ข้อมูลจัดซื้อจัดจ้าง' },
] as MenuItem[]);
const currentTab = ref(ProcurementTab.Detail);
const HeaderItem = ref(ProcurementConstants.ProcurementTabOptions);
const currentAccordion = ref<Array<string>>([]);
const showReviewDocumentDialog = ref(false);
const reviwDocumentId = ref<string>('');
const isFormDirty = ref(false);
const isInitialized = ref(false);

const id = computed(() => route.params.id?.toString());
const headerItemCondition = computed(() => {
  const duplicate = [...HeaderItem.value];

  if (!store.body.id) {
    return duplicate.filter((_, i) => i === 0);
  }

  if (store.body.budget <= 500000) {
    return duplicate.filter((_, i) => i === 0);
  }

  if (store.body.type === ProcurementPlanType.AnnualPlan && !(store.body.isCancel || store.body.isChange)) {
    return duplicate.filter((_, i) => i === 0);
  }

  if (store.body.budget > 500000 && store.body.type === ProcurementPlanType.InYearPlan && store.conditionBudget) {
    return duplicate;
  }

  if (store.body.budget > 500000 && (store.body.isCancel || store.body.isChange) && store.conditionBudget) {
    return duplicate;
  }

  return duplicate.filter((_, i) => i === 0);
});

onBeforeMount(async () => {
  await onInitPageAsync();
});

watch(
  () => store.body.status,
  (newVal: PlanStatus) => {
    if (headerItemCondition.value.length <= 1) {
      currentTab.value = ProcurementTab.Detail;
      return;
    }

    switch (newVal) {
      case PlanStatus.WaitingAcceptor:
        currentTab.value = ProcurementTab.RequestApprove
        break;
      case PlanStatus.WaitingAnnouncement:
      case PlanStatus.Announcement:
        currentTab.value = ProcurementTab.Announcement
        break;
      default:
        currentTab.value = ProcurementTab.Detail;
    }
  }, { immediate: true });

const onInitPageAsync = async (): Promise<void> => {
  isInitialized.value = false;
  store.clearBody();

  await store.getJorPorAsync();

  if (id.value) {
    await store.getByIdAsync(id.value);

  }

  if (store.body.acceptors.filter(f => f.acceptorType === AcceptorType.DepartmentDirectorAgree).length === 0) {
    await store.getOperDefaultDepartmentApproveAsync();
  }

  if (store.body.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length === 0) {
    await store.getDefaultApproverAsync();
  }

  await nextTick();
  isInitialized.value = true;
};

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === ProcurementTab.RequestApprove && requestApproveDocumentRef.value && 'saveDocumentFirst' in requestApproveDocumentRef.value) {
    await requestApproveDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === ProcurementTab.Announcement && announcementDocumentRef.value && 'saveDocumentFirst' in announcementDocumentRef.value) {
    await announcementDocumentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmitAsync();
};

const handleRestorePlanVersion = async (): Promise<void> => {
  const planId = store.body.id;
  if (!planId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await planService.resetDocumentAsync(planId, 'plan');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.getByIdAsync(planId);
  }
};

const handleRestoreAnnouncementVersion = async (): Promise<void> => {
  const planId = store.body.id;
  if (!planId) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await planService.resetDocumentAsync(planId, 'announcement');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await store.getByIdAsync(planId);
  }
};

const saveDocumentInEditor = async (): Promise<void> => {
  // Only save if we're on a document tab (RequestApprove or Announcement)
  if (currentTab.value === ProcurementTab.RequestApprove || currentTab.value === ProcurementTab.Announcement) {
    // Call ChEditor save method first to save document content to WOPI
    if (currentTab.value === ProcurementTab.RequestApprove && requestApproveDocumentRef.value) {
      requestApproveDocumentRef.value.clickSave();
    } else if (currentTab.value === ProcurementTab.Announcement && announcementDocumentRef.value) {
      announcementDocumentRef.value.clickSave();
    }

    // Wait a moment for the document save to complete
    await new Promise(resolve => setTimeout(resolve, 500));
  }
};

const onSubmitAsync = async (): Promise<void> => {
  // Save document in editor before updating (only on document tabs)
  await saveDocumentInEditor();

  if (id.value && store.body.status) {
    return await store.updateAsync(id.value, store.body.status);
  }

  await store.createAsync(PlanStatus.DraftPlan);
};

const sendApproveAsync = async (): Promise<void> => {
  if (!store.body.acceptors.some(x => x.acceptorType === AcceptorType.DepartmentDirectorAgree)) {
    return ToastHelper.factionAtLeastMessageToast();
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApprove))) return;

  await saveDocumentInEditor();

  if (id.value) {
    return await store.updateAsync(id.value, PlanStatus.WaitingApprovePlan);
  }
};

const onRejectPlanAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

  if (res.isConfirm && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, { action: PlanAction.RejectPlan, remark: res.reason });
  }
};

const onRecallPlanAsync = async (): Promise<void> => {
  const res = await showConfirmDialogAsync(ConfirmDialogType.Edit);

  if (res && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, { action: PlanAction.EditPlan });
  }
};

const onRecallDocumentAsync = async (): Promise<void> => {
  const res = await showConfirmDialogAsync(ConfirmDialogType.Edit);

  if (res && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, { action: PlanAction.RecallDocument });
  }
};

const onApprovePlanAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

  if (res.isConfirm && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    const jorporId = await store.getJorPorDirectorIdAsync();

    await store.actionAsync(store.body.id, {
      action: PlanAction.ApprovePlan,
      jorporId,
      remark: res.reason,
    });
  }
};

const onSaveAssignAssigneeAsync = async (): Promise<void> => {
  // Save document in editor before updating (only on document tabs)
  await saveDocumentInEditor();

  if (store.body.id) {
    await store.actionAsync(store.body.id, {
      action: PlanAction.AssignAssignee,
      assignees: store.body.assignees,
      assignSegmentCode: store.body.assignSegmentCode,
      egpNumber: store.body.egpNumber,
      groupEgpNumber: store.body.groupEgpNumber,
    });
  }
};

const onConfirmAssigneeAsync = async (): Promise<void> => {
  if (
    !store.body.assignees ||
    !store.body.assignees.some((a) => a.assigneeType === AssigneeType.Assignee)
  ) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  const res = await showConfirmDialogAsync(ConfirmDialogType.Assigned);

  if (res && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, {
      action: PlanAction.ApprovedAssignee,
      assignees: store.body.assignees,
      assignSegmentCode: store.body.assignSegmentCode,
      egpNumber: store.body.egpNumber,
      groupEgpNumber: store.body.groupEgpNumber,
    });

    if (store.body.acceptors.filter(f => f.acceptorType === AcceptorType.Approver).length === 0) {
      await store.getDefaultApproverAsync();
    }
  }
};

const onAssignSegmentApproverAsync = async (assignSegmentCode: string) => {
  switch (assignSegmentCode) {
    case AssignDepartmentCodeEnum.SegmentJorPorOther:
      await store.getDefaultSegmentOtherManagerApproverAsync();
      break;
    case AssignDepartmentCodeEnum.SegmentJorPorIT:
      await store.getDefaultSegmentITManagerApproverAsync();
      break;
    default:
      return;
  }

  await store.onUpsertAssignees();
};

const focusEgpFieldAsync = async (): Promise<void> => {
  currentTab.value = ProcurementTab.Detail;
  await nextTick();
  const targetId = !store.body.groupEgpNumber ? 'pl001-group-egp-number' : 'pl001-egp-number';
  const el = document.getElementById(targetId) as HTMLInputElement | null;
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    el.focus();
  }
};

const onSaveAssignAcceptorAsync = async (): Promise<void> => {
  if (
    !store.body.acceptors ||
    store.body.acceptors.filter((a) => a.acceptorType === AcceptorType.Approver).length <= 0
  ) {
    return ToastHelper.warning(
      'เพิ่มรายชื่อผู้มีอำนาจเห็นชอบ/อนุมัติ',
      'กรุณาเลือกผู้มีอำนาจเห็นชอบ/อนุมัติ'
    );
  }

  const shouldValidateEgp = store.body.supplyMethodCode == SupplyMethodCode.sixty
    && store.body.type == ProcurementPlanType.InYearPlan
    && store.body.budget > 500000;

  if (shouldValidateEgp && (!store.body.egpNumber || !store.body.groupEgpNumber)) {
    ToastHelper.warning(
      'กรุณาระบุข้อมูล',
      'กรุณาระบุเลขกลุ่ม e-GP/เลขที่ e-GP'
    );
    await focusEgpFieldAsync();
    return;
  }
  if (isFormDirty.value
      && (store.body.planDocumentId
          || store.body.planAnnouncementDocumentId) && store.body.status === PlanStatus.DraftRecordDocument && shouldValidateEgp) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.isPlanDocumentIdReplace = saveOption;
    store.body.isPlanAnnouncementDocumentIdReplace = saveOption;
  }

  // Save document in editor before updating (only on document tabs)
  await saveDocumentInEditor();

  if (store.body.id) {
    await store.actionAsync(store.body.id, {
      action: PlanAction.AssignAcceptor,
      acceptors: store.body.acceptors,
      assignSegmentCode: store.body.assignSegmentCode,
      assignees: store.body.assignees,
      groupEgpNumber: store.body.groupEgpNumber,
      egpNumber: store.body.egpNumber,
      isPlanAnnouncementDocumentIdReplace: store.body.isPlanAnnouncementDocumentIdReplace,
      isPlanDocumentIdReplace: store.body.isPlanDocumentIdReplace,
      documentDate: store.body.documentDate,
    });

    if (store.body.status === PlanStatus.DraftRecordDocument) {
      currentTab.value = ProcurementTab.RequestApprove;
    }
  }
};

const onConfirmAcceptorAsync = async (): Promise<void> => {
  if (
    !store.body.assignees ||
    !store.body.assignees.some((a) => a.assigneeType === AssigneeType.Assignee)
  ) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (
    !store.body.acceptors ||
    !store.body.acceptors.some((a) => a.acceptorType === AcceptorType.Approver)
  ) {
    return ToastHelper.warning(
      'เพิ่มรายชื่อผู้มีอำนาจเห็นชอบ/อนุมัติ',
      'กรุณาเลือกผู้มีอำนาจเห็นชอบ/อนุมัติ'
    );
  }

  const shouldValidateEgp = store.body.supplyMethodCode == SupplyMethodCode.sixty
    && store.body.type == ProcurementPlanType.InYearPlan
    && store.body.budget > 500000;

  if (shouldValidateEgp && (!store.body.egpNumber || !store.body.groupEgpNumber)) {
    ToastHelper.warning(
      'กรุณาระบุข้อมูล',
      'กรุณาระบุเลขกลุ่ม e-GP/เลขที่ e-GP'
    );
    await focusEgpFieldAsync();
    return;
  }

  const res = await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm);

  if (res && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, {
      action: PlanAction.ConfirmAcceptor,
      acceptors: store.body.acceptors,
      assignees: store.body.assignees,
      groupEgpNumber: store.body.groupEgpNumber,
      egpNumber: store.body.egpNumber,
      documentDate: store.body.documentDate,
    });
  }
};

const onRejectDocumentAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Reject, true);

  if (res.isConfirm && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, {
      action: PlanAction.RejectedAcceptor,
      remark: res.reason,
    });
  }
};

const onApproveDocumentAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(
    store.isLastApproveDocument ? ReasonDialogType.Approve : ReasonDialogType.Accepted,
  );

  if (res.isConfirm && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, {
      action: PlanAction.ApprovedAcceptor,
      remark: res.reason,
    });
  }

  if (store.isLastApproveDocument) {
    currentTab.value = ProcurementTab.Announcement;
  }
};

const onAnnouncementPlanAsync = async (): Promise<void> => {
  const res = await showConfirmDialogAsync(ConfirmDialogType.AnnouncementPlan);

  if (res && store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, { action: PlanAction.Announcement });
  }
};

const onAssigneeRejected = async () => {
  const res = await showReasonDialogAsync(ReasonDialogType.Reject);

  if (!res.isConfirm) return;

  if (store.body.id) {
    // Save document in editor before updating (only on document tabs)
    await saveDocumentInEditor();

    await store.actionAsync(store.body.id, {
      action: PlanAction.AssigneeRejected,
      remark: res.reason,
    });
  }
};

const onTabChange = (tab: string): void => {
  switch (tab) {
    case ProcurementTab.Detail:
      currentTab.value = ProcurementTab.Detail;
      break;
    case ProcurementTab.RequestApprove:
      currentTab.value = ProcurementTab.RequestApprove;
      break;
    case ProcurementTab.Announcement:
      currentTab.value = ProcurementTab.Announcement;
      break;
    default:
      currentTab.value = ProcurementTab.Detail;
  }
};


const onSendCancelAsync = async (): Promise<void> => {
  const resp = await showReasonDialogAsync(ReasonDialogType.RequestCancel);

  if (!resp.isConfirm) return;

  // Save document in editor before updating (only on document tabs)
  await saveDocumentInEditor();

  await store.requestActionAsync(id.value, resp.reason);
};

const onSendChangeAsync = async (): Promise<void> => {
  const resp = await showReasonDialogAsync(ReasonDialogType.RequestChange);

  if (!resp.isConfirm) return;

  // Save document in editor before updating (only on document tabs)
  await saveDocumentInEditor();

  await store.requestActionAsync(id.value, resp.reason, true);
};

const getReviewDocumentAsync = async (): Promise<void> => {
  if (!store.body.id) return;

  const documentType = currentTab.value === ProcurementTab.RequestApprove ? 'plan' : 'announcement';
  const idDocument = await store.getReviewDocumentAsync(store.body.id, documentType);

  reviwDocumentId.value = idDocument;
  setDocumentReviewId(idDocument);
};

const setDocumentReviewId = (id: string): void => {
  const documentType = currentTab.value === ProcurementTab.RequestApprove ? 'plan' : 'announcement';

  if (documentType === 'plan') {
    store.body.planDocumentId = id;
    store.body.isPlanDocumentIdReplace = true;
  } else {
    store.body.planAnnouncementDocumentId = id;
    store.body.isPlanAnnouncementDocumentIdReplace = true;
  }
};

const canEditDocument = computed(() => {
  if (store.body.type === ProcurementPlanType.InYearPlan) {
    return [PlanStatus.WaitingAssign, PlanStatus.Assigned, PlanStatus.DraftRecordDocument, PlanStatus.RejectToAssignee].includes(store.body.status) && store.body.assignees.some(s => s.userId == authStore.profile.id);
  }

  return store.body.status === PlanStatus.WaitingAssign
    || store.body.status === PlanStatus.Assigned
    || store.body.status === PlanStatus.DraftRecordDocument
    || store.body.status === PlanStatus.RejectToAssignee
});

watch((): PlanStatus => store.body.status, (val: PlanStatus): void => {
  if (store.conditionBudget && [PlanStatus.Announcement].includes(val)) {
    currentAccordion.value = ['0', '1', '2', '3', '4'];
  }

  if (!store.conditionBudget && [PlanStatus.ApprovePlan].includes(val)) {
    currentAccordion.value = ['0', '1', '2', '3', '4'];
  }

  if ([PlanStatus.DraftPlan, PlanStatus.EditPlan, PlanStatus.RejectPlan, PlanStatus.WaitingApprovePlan].includes(val)) {
    currentAccordion.value = ['0'];
  }

  if ([PlanStatus.WaitingAssign, PlanStatus.Assigned, PlanStatus.DraftRecordDocument, PlanStatus.RejectToAssignee].includes(val)) {
    currentAccordion.value = ['1', '2'];
  }

  if ([PlanStatus.WaitingAcceptor].includes(val)) {
    currentAccordion.value = ['2'];
  }

  if ([PlanStatus.WaitingAnnouncement].includes(val)) {
    currentAccordion.value = ['3'];
  }
}, { immediate: true });

watch(() => store.body.type, (val) => {
  if (val === ProcurementPlanType.AnnualPlan && !(store.body.isCancel || store.body.isChange)) {
    currentAccordion.value.push("4");
  }
}, { immediate: true });

watch(
  () => store.body,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const canAssignDepartment = computed(() => {
  const checkAllApprove =
    store.body.acceptors && store.body.acceptors.length > 0 &&
    store.body.acceptors
      .filter(a => a.acceptorType === AcceptorType.DepartmentDirectorAgree)
      .every(a => a.status === AcceptorStatus.Approved);

  const isJpSecetion = [PlanStatus.DraftPlan, PlanStatus.EditPlan, PlanStatus.RejectPlan, PlanStatus.WaitingApprovePlan].includes(store.body.status);

  return store.budgetCondition && checkAllApprove && !isJpSecetion;
});

const canEditEGP = computed(() => {
  const status = [PlanStatus.WaitingAssign, PlanStatus.RejectToAssignee].includes(store.body.status);
  const isJorPor = authStore.profile.isJorPor;

  return status && isJorPor;
});

const isReadonly = computed(() => store.body.status === PlanStatus.Closed);

// --- Procurement navigation ---
const procurementOverlay = ref();
const procurements = ref<{ id: string; procurementNumber: string }[]>([]);

const onNavigateToProcurementAsync = async (event: Event): Promise<void> => {
  if (!store.body.id) return;

  const target = event.currentTarget;

  if (!procurements.value.length) {
    const { data, status } = await ppService.getProcurementListAsync({
      planId: store.body.id,
      pageNumber: 1,
      pageSize: 100,
      procurementType: ProcurementType.Procurement,
    });

    if (status === HttpStatusCode.Ok) {
      procurements.value = (data.data?.data ?? [])
        .map(p => ({ id: p.id, procurementNumber: p.procurementNumber ?? p.id }));
      await nextTick();
    }
  }

  if (procurements.value.length === 1) {
    await router.push(`/pp/detail/${procurements.value[0].id}`);
    return;
  }

  procurementOverlay.value?.toggle({ currentTarget: target } as Event);
};
</script>

<template>
  <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง" :routeItems="routeItems">
    <template #action>
      <div class="flex items-center gap-4">
        <p class="text-sm">สถานะ :</p>
        <BadgeStatus :label="PlanStatusName(store.body.status)"
          :bg-color-class="PlanStatusColor(store.body.status).bgColorClass"
          :text-color-class="PlanStatusColor(store.body.status).textColorClass" />
        <BadgeStatus v-if="store.body.isChange" label="ขอเปลี่ยนแปลง" color="amber" />
        <BadgeStatus v-if="store.body.isCancel" label="ขอยกเลิก" color="red" />
      </div>

      <div class="w-px self-stretch min-h-[30px] bg-gray-300 mx-1" v-if="store.body.id" />

      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!, ProgramHistoryName.Plan)" />
      <Button
        v-if="store.body.isProcurement && [PlanStatus.Announcement, PlanStatus.ApprovePlan].includes(store.body.status)"
        label="ไปยังข้อมูลจัดซื้อจัดจ้าง"
        icon="pi pi-arrow-right"
        iconPos="right"
        variant="outlined"
        class="bg-white! border-(--color-blue-600)! text-(--color-blue-600)! hover:bg-blue-50!"
        @click="onNavigateToProcurementAsync"
      />
    </template>
  </TitleHeader>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="mt-4 pb-5">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5 order-2 lg:order-1">
          <div>
            <Tabs :value="currentTab" unstyled @update:value="(tab) => onTabChange(tab.toString())">
              <TabHeader :items="headerItemCondition" class="sticky top-14 z-3 bg-[#F7F7F7] pt-2" />
              <TabPanels>
                <TabPanel :value="ProcurementTab.Detail">
                  <Detail />
                </TabPanel>
                <TabPanel :value="ProcurementTab.RequestApprove" @click="currentTab = ProcurementTab.RequestApprove">
                  <Card>
                    <template #content>
                      <RequestApprove ref="requestApproveDocumentRef" :readonly="!canEditDocument" :save="saveDocument"
                        :versions="store.body.planDocumentVersions"
                        :canRestoreVersion="store.canEditDocument && menStore.hasManage && canEditDocument"
                        @restore-version="handleRestorePlanVersion" />
                    </template>
                  </Card>
                </TabPanel>
                <TabPanel :value="ProcurementTab.Announcement" @click="currentTab = ProcurementTab.Announcement">
                  <Card>
                    <template #content>
                      <Announcement ref="announcementDocumentRef" :readonly="!canEditDocument" :save="saveDocument"
                        :versions="store.body.planAnnouncementDocumentVersions"
                        :canRestoreVersion="store.canEditDocument && menStore.hasManage && canEditDocument"
                        @restore-version="handleRestoreAnnouncementVersion" />
                    </template>
                  </Card>
                </TabPanel>
                <div class="mt-4">
                  <UploadFileGroup v-if="!store.body.id" v-model="store.body.attachments"
                    :disabled="!menStore.hasManage" />
                  <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments"
                    @upload="store.onUpsertAttachments" @remove-file="store.onUpsertAttachments"
                    @remove-group="store.onUpsertAttachments" @reorder="store.onUpsertAttachments"
                    :disabled="!menStore.hasManage" />

                </div>
              </TabPanels>
            </Tabs>
          </div>
        </div>

        <div class="relative lg:col-span-2 order-1 lg:order-2">
          <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
            <div class="flex items-center gap-2 justify-end" v-if="menStore.hasManage && !isReadonly">
              <ButtonRecall @click="onRecallPlanAsync" v-if="store.canRecall" />
              <ButtonSave type="submit" v-if="store.canEdit" />
              <ButtonSendApprove @click="handleSubmit(sendApproveAsync)" v-if="store.canEdit && store.body.id" />

              <ButtonSendEdit @click="onRejectPlanAsync" v-if="store.canApproveReject" />
              <ButtonApprove @click="onApprovePlanAsync" v-if="store.canApproveReject" />

              <ButtonSendEdit @click="onAssigneeRejected"
                v-if="store.canAssignAssigneeTypeDirector || store.canAssignAssigneeTypeAssignee" />
              <ButtonSave @click="onSaveAssignAssigneeAsync" v-if="store.canAssignAssignee" />
              <ButtonConfirmAssign @click="onConfirmAssigneeAsync()" v-if="store.canAssignAssignee" />

              <ButtonSave @click="onSaveAssignAcceptorAsync"
                v-if="store.canAssignAssigneeTypeDirector || store.canAssignAssigneeTypeAssignee" />
              <ButtonApproveConfirm v-if="store.canConfirmAcceptor" @click="onConfirmAcceptorAsync" />

              <ButtonRecall @click="onRecallDocumentAsync" v-if="store.canRecallDocument" />
              <ButtonSendEdit @click="onRejectDocumentAsync" v-if="store.canApproveDocument" />
              <ButtonApprove @click="onApproveDocumentAsync"
                v-if="store.canApproveDocument && !store.isLastApproveDocument" />
              <ButtonConfirm v-if="store.canApproveDocument && store.isLastApproveDocument"
                @click="onApproveDocumentAsync" />
              <ButtonPublish v-if="store.canAnnouncement" @click="onAnnouncementPlanAsync" />

              <ButtonSendChange
                v-if="store.canSendCancelAndChange && !store.body.isCurrentCancelOrChange && !store.body.isProcurement"
                @click="onSendChangeAsync" />
              <ButtonSendCancel
                v-if="store.canSendCancelAndChange && !store.body.isCurrentCancelOrChange && !store.body.isProcurement"
                @click="onSendCancelAsync" />
            </div>
            <Accordion v-model:value="currentAccordion" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab !== ProcurementTab.Detail">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <div></div>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <DocumentMapping pathToGet="plan" @on-click-review="getReviewDocumentAsync" @on-click-select="
                        (text, hint) =>
                          currentTab === ProcurementTab.RequestApprove
                            ? requestApproveDocumentRef?.setPlaceholderInDocument(text, hint)
                            : announcementDocumentRef?.setPlaceholderInDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel value="0">
                <AccordAcceptor title="ฝ่ายเห็นชอบ" v-model="store.body.acceptors" is-manage
                  :isSetDefault="store.isCanSetDefaultUnit" :is-disable="!store.canEdit || !menStore.hasManage || isReadonly"
                  :acceptor-type="AcceptorType.DepartmentDirectorAgree" v-if="store.body.acceptors"
                  @set-default="() => store.getOperDefaultDepartmentApproveAsync()" />
              </AccordionPanel>

              <AccordionPanel value="1" class="mt-4" v-if="store.isShowAssignee">
                <AccordAssignee title="ผู้รับผิดชอบ" v-model="store.body.assignees"
                  :disabled="!(store.canAssignAssignee || store.canAssignAcceptor) || !menStore.hasManage"
                  v-if="store.body.assignees" :is-dropdown="canAssignDepartment" dropdown-rules='required'
                  :dropdown="store.assignDepartmentDDL" v-model:select-data="store.body.assignSegmentCode"
                  :disabled-dropdown="!(store.canAssignAssignee || store.canAssignAcceptor) || !canEditEGP || !menStore.hasManage"
                  v-on:change="onAssignSegmentApproverAsync" @on-change="store.onUpsertAssignees" />
              </AccordionPanel>

              <AccordionPanel value="2" class="mt-4" v-if="store.conditionBudget">
                <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.body.acceptors"
                  :acceptor-type="AcceptorType.Approver" v-if="store.body.acceptors" is-approve
                  :is-disable="!store.canAssignAcceptor || !menStore.hasManage || isReadonly" is-manage
                  :isSetDefault="store.isCanSetDefaultApprover" @set-default="() => store.getDefaultApproverAsync()" />
              </AccordionPanel>

              <AccordionPanel value="3" class="mt-4" v-if="store.conditionBudget">
                <AccordHeader label="ผอ. จพ. อนุมัติเผยแพร่แผน" />
                <AccordionContent>
                  <Card class="rounded-none!">
                    <template #content>
                      <div>
                        <p>{{ store.body.assigneeAnnouncement?.fullName }}</p>
                        <small class="text-gray-300">{{ store.body.assigneeAnnouncement?.positionName }}</small>
                      </div>
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviwDocumentId"
    :docName="`plan-${new Date().toISOString()}-${store.body.planDocumentId}`"
    @on-click-use-document="setDocumentReviewId" />

  <Popover ref="procurementOverlay">
    <div class="flex flex-col gap-1 min-w-48">
      <p class="text-sm font-semibold text-gray-600 mb-1">รายการจัดซื้อจัดจ้าง</p>
      <Button
        v-for="p in procurements"
        :key="p.id"
        :label="p.procurementNumber"
        variant="text"
        size="small"
        icon="pi pi-arrow-right"
        iconPos="right"
        class="justify-between! text-(--color-blue-600)! hover:bg-blue-50!"
        @click="router.push(`/pp/detail/${p.id}`)"
      />
    </div>
  </Popover>
</template>
