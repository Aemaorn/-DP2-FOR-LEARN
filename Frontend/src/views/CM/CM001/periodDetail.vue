<script setup lang="ts">
import { BadgeStatus as BadgeComponent } from '@/components';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import { ButtonApprove, ButtonApproveConfirm, ButtonConfirm, ButtonConfirmAssign, ButtonNotAgree, ButtonRecall, ButtonSave, ButtonSendEdit } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import ChEditor from '@/components/Document/ChEditor.vue';
import DocumentMapping from '@/components/DocumentMapping.vue';
import { CM001PeriodAccordion, CM001PeriodStatus, CM001PeriodTabHeader, CmDeliveryAcceptancePeriodAccountStatus, souceType } from '@/enums/CM/cm001';
import { PlanDepartmentCode } from '@/enums/plan';
import { checkIsEighty } from '@/helpers/supplyMethod';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorStatus, AcceptorType, AssigneeGroup, AssigneeType } from '@/enums/participants';
import { EGroupCode } from '@/enums/shared';
import { ArrayHelper } from '@/helpers/array';
import { CM001PeriodHelper } from '@/helpers/CM/cm001';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import SharedService from '@/services/Shared/dropdown';
import { useAuthenticationStore } from '@/stores/authentication';
import { useCm001DetailStore } from '@/stores/CM/cm001';
import { useCM001PeriodStore as usePeriodStore } from '@/stores/CM/cm001Period';
import { useLoadingStore } from '@/stores/loading';
import { useMenuStore } from '@/stores/menu';
import { HttpStatusCode } from 'axios';
import { Button, Card } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form as VeeForm } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, onBeforeMount, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import CommercialMaterialSection from './components/CommercialMaterialSection.vue';
import BudgetSection from './components/Period/BudgetSection.vue';
import DisbursementSection from './components/Period/DisbursementSection.vue';
import InspectionCommittees from './components/Sub/InspectionCommittees.vue';
import { UploadFileGroup } from '@/components/forms';

const route = useRoute();
const { PeriodBadgeStatus, PeriodTabHeaderItem, PeriodAccordionName, PeriodAccountStatus } = CM001PeriodHelper;
const routeItems = ref<Array<MenuItem>>([
  { label: 'บันทึกรายงานผลการตรวจรับ (จพ.008)', url: '/cm/cm001' },
  { label: 'ข้อมูลส่งมอบ ตรวจรับ และขออนุมัติเบิกจ่าย', url: `/cm/cm001/detail/${route.params.id}` },
  { label: 'จัดการรายงานผลการตรวจรับ' },
]);

const id = computed<string>(() => route.params.id as string)

const menuStore = useMenuStore();
const loadingStore = useLoadingStore();
const store = usePeriodStore();
const storeCm01 = useCm001DetailStore();
const authStore = useAuthenticationStore();

const accountingOperatorDefaults = ref<ParticipantsAcceptor[]>([]);

const documentRef = ref<InstanceType<typeof ChEditor> | null>(null);
const disbursementSectionRef = ref<HTMLElement | null>(null);
const currentTab = ref(CM001PeriodTabHeader.Detail);
const dropdown = ref<Array<Option>>([]);
const isSaving = ref(false);
const hasLoadedReviewDocument = ref(false);
const currentAccordion = ref<string[]>([]);
const isInitialLoad = ref(true);
const isFormDirty = ref(false);
const inspectionSectionRef = ref<HTMLElement | null>(null);

const PeriodInfo = defineAsyncComponent(() => import('@/views/CM/CM001/components/Period/PeriodInfo.vue'));
const InspectionSection = defineAsyncComponent(() => import('@/views/CM/CM001/components/Period/InspectionSection.vue'));
const AccountingAcceptorSection = defineAsyncComponent(() => import('@/views/CM/CM001/components/Period/AccountingAcceptorSection.vue'));

onBeforeMount(async () => {
  store.fn.resetBody();

  const periodId = route.params.periodId as string;
  await store.fn.onGetByIdAsync(id.value, periodId);

  if (id.value) {
    await storeCm01.fn.onGetByIdAsync(id.value);
  }

  if (store.body.id && store.body.acceptors.length == 0) {
    await store.fn.setDefaultAcceptor()

    if (!store.checkAssignee && store.body.acceptors.length > 0 && store.states.isEdit) {
      const lastAcceptor = store.body.acceptors[store.body.acceptors.length - 1];
      lastAcceptor.sequence = 1;
      store.body.acceptors = [lastAcceptor];
    }
  } else if (store.body.id) {
    await store.fn.getDefaultAcceptor();
  }

  if (store.body.documentId) {
    if (store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate
      || store.body.status === CM001PeriodStatus.Rejected
      || store.body.status === CM001PeriodStatus.Draft) {
      currentTab.value = CM001PeriodTabHeader.Detail;
    } else {
      currentTab.value = CM001PeriodTabHeader.Document;
    }
  }

  isInitialLoad.value = false;
});

onMounted(async () => {
  await onGetContractDropdownAsync();
});

const onGetContractDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CType);

  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};


const onTabChange = (tab: string): void => {
  switch (tab) {
    case CM001PeriodTabHeader.Detail:
      currentTab.value = CM001PeriodTabHeader.Detail
      break;
    case CM001PeriodTabHeader.Document:
      currentTab.value = CM001PeriodTabHeader.Document
      break;
    default:
      currentTab.value = CM001PeriodTabHeader.Detail
      break;
  }
};

const getLabelFromDropdown = (value?: string) => {
  if (value === undefined) {
    return '-';
  }

  return dropdown.value.find(d => d.value === value)?.label;
}

const onSubmitAsync = async (id: string) => {
  await store.fn.onSubmitAsync(id);

  if (store.body.acceptors.length == 0 && store.body.id && store.body.contractBudgetAmount && store.body.contractBudgetAmount > 0) {
    await store.fn.setDefaultAcceptor()
  }
};

const saveDocument = async () => {
  store.body.isDocumentReplaced = false;
  await onSubmitAsync(id.value);
};

const reloadAsync = async (): Promise<void> => {
  const periodId = route.params.periodId as string;
  await store.fn.onGetByIdAsync(id.value, periodId);

  if (store.body.documentId) {
    if (store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate
      || store.body.status === CM001PeriodStatus.Rejected
      || store.body.status === CM001PeriodStatus.Draft) {
      currentTab.value = CM001PeriodTabHeader.Detail;
    } else {
      currentTab.value = CM001PeriodTabHeader.Document;
    }
  }
};

const handleSubmitWithValidation = async (callback: () => Promise<void>) => {
  const savePromise = saveDocumentFirst();
  store.body.isDocumentReplaced = false;
  await callback();
  await savePromise;
  await reloadAsync();
};

const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    if (documentRef.value?.saveAndWait && canEditDocument.value) {
      documentRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

const handleSaveClick = async () => {
  if (isSaving.value) return;

  if (store.body.budgetDetails?.length > 0) {
    const totalPayment = store.body.paymentTerms.reduce((sum, item) => sum + (item.amount || 0), 0);
    const totalDeductions = store.body.hasInvoiceSlip ? (store.body.invoiceSlipAmount || 0) : 0;
    const finalAmount = totalPayment - totalDeductions;
    const totalBudget = store.body.budgetDetails.reduce((sum, item) => sum + (item.budget || 0), 0);

    if (totalBudget !== finalAmount) {
      return ToastHelper.error('ไม่สามารถบันทึกได้', 'ผลรวมจำนวนเงินของรหัสบัญชีไม่เท่ากับรวมจำนวนเงินทั้งสิ้น');
    }
  }

  store.body.isDocumentReplaced = false;

  if (isFormDirty.value
    && id.value && store.body.documentId && canEditDocument.value) {
    const saveOption = await showSaveOptionDialogAsync();
    if (saveOption === undefined) return;
    store.body.isDocumentReplaced = saveOption;
  }

  isSaving.value = true;
  loadingStore.setIsLoading(true);

  try {
    if (documentRef.value && store.body.documentId) {
      await saveDocumentFirst();
    }
    await onSubmitAsync(id.value);

    currentTab.value = CM001PeriodTabHeader.Document;
  } finally {
    store.body.isDocumentReplaced = false;
    isSaving.value = false;
    loadingStore.setIsLoading(false);
    await nextTick();
    isFormDirty.value = false;
  }
};

const canEditDocument = computed(() => {
  return store.body.status == CM001PeriodStatus.Draft
    || store.body.status == CM001PeriodStatus.Rejected
    || store.body.status == CM001PeriodStatus.Edit
    || store.states.jorporCanAssignByAssignee
});

watch(() => store.body.id, () => {
  hasLoadedReviewDocument.value = false;
});

const getDefaultAcceptor = async () => {
  if (store.body.assignees.filter(a => a.assigneeType === AssigneeType.Assignee).length > 0) {
    await store.fn.assigneeDefaultAcceptor();
    return;
  }

  await store.fn.setDefaultAcceptor();

  if (!store.checkAssignee && store.body.acceptors.length > 0 && store.states.isEdit) {
    const lastAcceptor = store.body.acceptors[store.body.acceptors.length - 1];
    lastAcceptor.sequence = 1;
    store.body.acceptors = [lastAcceptor];
  }
};

const showAcceptor = computed(() => {
  if (store.body.status === CM001PeriodStatus.Approved || store.body.status === CM001PeriodStatus.WaitingAcceptance) return true;

  if (!store.body.documentId) return false;

  const hasAssigneeSection = store.checkAssignee || store.states.canAssignee;
  const hasNonDirectorAssignee = store.body.assignees.some(a => a.assigneeType === AssigneeType.Assignee);

  if (hasAssigneeSection && !hasNonDirectorAssignee) return false;

  if (hasAssigneeSection && hasNonDirectorAssignee) return true;

  if (!hasAssigneeSection) return true;

  return ![CM001PeriodStatus.Draft, CM001PeriodStatus.Edit, CM001PeriodStatus.Rejected, CM001PeriodStatus.WaitingCommitteeApproval].includes(store.body.status);
});

const contractManagementButtonSeverity = computed(() => {
  if (store.body.status === CM001PeriodStatus.Approved) {
    return 'success';
  }
  return 'warn';
});

const accountingButtonSeverity = computed(() => {
  if (store.body.status !== CM001PeriodStatus.Approved) {
    return 'secondary';
  }

  if (store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.Paid) {
    return 'success';
  }

  if (
    store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval ||
    store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected ||
    store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate
  ) {
    return 'warn';
  }

  return 'secondary';
});

const isContractManagementDone = computed(() =>
  store.body.status === CM001PeriodStatus.Approved
);

const isAccountingDone = computed(() =>
  store.body.status === CM001PeriodStatus.Approved &&
  store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.Paid
);

const getActiveAccordions = computed(() => {
  const accordions: string[] = [];
  const status = store.body.status;
  const accountStatus = store.body.accountStatus;

  if ([CM001PeriodStatus.Draft, CM001PeriodStatus.Rejected, CM001PeriodStatus.Edit].includes(status)) {
    accordions.push(CM001PeriodAccordion.Committee);
    if (store.checkAssignee || store.states.canAssignee) {
      accordions.push(CM001PeriodAccordion.Assignee);
    }
    accordions.push(CM001PeriodAccordion.Acceptor);
    accordions.push(CM001PeriodAccordion.Accounting);
    accordions.push(CM001PeriodAccordion.AccountingConfirmer);
  }

  if (status === CM001PeriodStatus.WaitingCommitteeApproval) {
    accordions.push(CM001PeriodAccordion.Committee);
  }

  if ([CM001PeriodStatus.WaitingAssign, CM001PeriodStatus.WaitingComment, CM001PeriodStatus.RejectToAssignee].includes(status)) {
    if (store.checkAssignee || store.states.canAssignee) {
      accordions.push(CM001PeriodAccordion.Assignee);
    }
    accordions.push(CM001PeriodAccordion.Acceptor);
  }

  if (status === CM001PeriodStatus.WaitingAcceptance) {
    accordions.push(CM001PeriodAccordion.Acceptor);
  }

  if (status === CM001PeriodStatus.Approved && accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval) {
    if (store.checkAssignee || store.states.canAssignee) {
      accordions.push(CM001PeriodAccordion.Assignee);
    }
    accordions.push(CM001PeriodAccordion.Acceptor);
    accordions.push(CM001PeriodAccordion.Accounting);
    accordions.push(CM001PeriodAccordion.AccountingConfirmer);
  }

  if (status === CM001PeriodStatus.Approved && accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate) {
    if (store.checkAssignee || store.states.canAssignee) {
      accordions.push(CM001PeriodAccordion.Assignee);
    }
    accordions.push(CM001PeriodAccordion.Acceptor);
    accordions.push(CM001PeriodAccordion.AccountingConfirmer);
  }

  if (status === CM001PeriodStatus.Approved && accountStatus === CmDeliveryAcceptancePeriodAccountStatus.Paid) {
    if (store.checkAssignee || store.states.canAssignee) {
      accordions.push(CM001PeriodAccordion.Assignee);
    }
    accordions.push(CM001PeriodAccordion.Acceptor);
    accordions.push(CM001PeriodAccordion.AccountingConfirmer);
  }

  return accordions;
});

const checkPlanDepartmentCode = computed(() => {
  return [PlanDepartmentCode.CCD, PlanDepartmentCode.MCD, PlanDepartmentCode.NMD, PlanDepartmentCode.OABAD, PlanDepartmentCode.RDMD1, PlanDepartmentCode.RDMD2].includes(storeCm01.body.departmentId as PlanDepartmentCode) && checkIsEighty(storeCm01.body.supplyMethodCode);
});

const { deleteItemAndReSequence, reSequence } = ArrayHelper();

// กล่อง "ส่วนบัญชีค่าใช้จ่าย" (operator) แสดงเฉพาะตอนเข้าสู่ขั้นบัญชีอนุมัติ
const showAccountingOperatorSection = computed<boolean>((): boolean =>
  !store.states.isBranchDepartment
  && store.body.status === CM001PeriodStatus.Approved
  && [
    CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval,
    CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected,
    CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate,
    CmDeliveryAcceptancePeriodAccountStatus.Paid,
  ].includes(store.body.accountStatus));

const showAccountingConfirmerSection = computed<boolean>((): boolean => {
  if (!store.body.id) return false;
  if (store.states.isBranchDepartment) return true;
  return store.body.status === CM001PeriodStatus.Approved
    && [CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate, CmDeliveryAcceptancePeriodAccountStatus.Paid]
      .includes(store.body.accountStatus);
});

const isAccountingSectionReadonly = computed<boolean>((): boolean => {
  if (!menuStore.hasManage) return true;

  // ช่วงร่าง/เรียกคืน/ส่งกลับแก้ไข (Draft/Edit/Rejected) → ผู้แก้ไขรายงานจัดการกล่องบัญชีได้
  // (แม้มี acceptor ที่ status=Approved ค้างอยู่ก็ปลดล็อกให้แก้ไขได้)
  if (store.states.isEdit) return false;

  // มีผู้เห็นชอบ/อนุมัติในกล่องบัญชีกด "อนุมัติ" ไปแล้วอย่างน้อย 1 คน → ล็อกการจัดการ
  const hasApprovedAccountingAcceptor = (store.body.acceptanceOfAccounting ?? [])
    .some((a): boolean => a.status === AcceptorStatus.Approved);
  if (hasApprovedAccountingAcceptor) return true;

  const validStatus = [CM001PeriodStatus.Approved].includes(store.body.status)
    && [CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected, CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval]
      .includes(store.body.accountStatus);

  if (!validStatus) return true;

  if (store.states.isBranchDepartment) {
    // สาขา → คณะกรรมการตรวจรับ (AcceptanceCommittee) เพิ่ม/แก้กล่องบัญชีเห็นชอบ/อนุมัติได้ด้วย
    const isCommittee = (store.body.acceptanceCommittees ?? []).some((a): boolean =>
      a.acceptorType === AcceptorType.AcceptanceCommittee && (a.delegateeUserId ?? a.userId) === authStore.profile.id);
    return authStore.profile.departmentCode !== store.body.departmentCode && !isCommittee;
  }

  const currentUserId = authStore.profile.id;
  const isInAccountingList =
    accountingOperators.value.some((a): boolean => (a.delegateeUserId ?? a.userId) === currentUserId) ||
    accountingApprovers.value.some((a): boolean => (a.delegateeUserId ?? a.userId) === currentUserId);

  return !store.states.isCurrentUserAccountingSegmentMember && !isInAccountingList;
});

// จัดการกล่อง confirmer (เพิ่ม/ลบ/แก้ว่าใครเป็น confirmer) — สาขา = committee
const isConfirmerSectionManage = computed<boolean>((): boolean =>
  (menuStore.hasManage && store.states.isDisbursementManageable)
  || (store.states.isBranchDepartment && !isAccountingSectionReadonly.value));

// กรอก/บันทึก/ยืนยัน "ข้อมูลเบิกจ่าย" — เฉพาะ confirmer (คนในกล่องจบงาน)
const isDisbursementInputManage = computed<boolean>((): boolean =>
  menuStore.hasManage && store.states.isDisbursementInputManageable);

const isAccountingAcceptorsUnchanged = computed<boolean>((): boolean => {
  const current = store.body.acceptanceOfAccounting ?? [];
  return JSON.stringify(current) === JSON.stringify(store.cloneAccountingAcceptors);
});

const setAccountingListByType = (type: AcceptorType, newList: ParticipantsAcceptor[]): void => {
  const arr = store.body.acceptanceOfAccounting ?? [];
  const others = arr.filter((a): boolean => a.acceptorType !== type);
  store.body.acceptanceOfAccounting = [...others, ...newList];
};

const accountingOperators = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (store.body.acceptanceOfAccounting ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.AccountingOperator),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.AccountingOperator, val),
});

const accountingApprovers = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (store.body.acceptanceOfAccounting ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.Accounting),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.Accounting, val),
});

const autoSaveAccountingOperatorAsync = async (type: AcceptorType): Promise<void> => {
  if (type !== AcceptorType.AccountingOperator && type !== AcceptorType.Accounting) return;
  if (!store.body.id) return;

  await store.fn.onSubmitAsync(id.value);
};

const addAccountingAcceptorAsync = async (type: AcceptorType): Promise<void> => {
  const res = await showUserDialogAsync();
  if (!res) return;

  const current = type === AcceptorType.AccountingOperator
    ? accountingOperators.value
    : accountingApprovers.value;

  if (current.some((a): boolean => a.userId === res.id)) {
    ToastHelper.warning('เพิ่มรายชื่อ', 'ไม่สามารถเพิ่มได้เนื่องจากผู้ใช้งานซ้ำ');
    return;
  }

  if (res.delegateeId && current.some((a): boolean => a.userId === res.delegateeId)) {
    ToastHelper.warning('เพิ่มรายชื่อ', 'คุณเลือกผู้ปฏิบัติหน้าที่แทนตำแหน่งนี้แล้ว');
    return;
  }

  if (!store.body.acceptanceOfAccounting) {
    store.body.acceptanceOfAccounting = [];
  }

  store.body.acceptanceOfAccounting.push({
    userId: res.id,
    acceptorType: type,
    departmentName: res.departmentName ?? '',
    fullName: res.name,
    positionName: res.positionName ?? '',
    sequence: current.length + 1,
    status: AcceptorStatus.Draft,
    organizationLevel: res.organizationLevel,
  } as ParticipantsAcceptor);

  await autoSaveAccountingOperatorAsync(type);
};

const removeAccountingAcceptorAsync = async (type: AcceptorType, index: number): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, deleteItemAndReSequence(list, index));

  await autoSaveAccountingOperatorAsync(type);
};

const onAccountingReSequenceDrag = async (type: AcceptorType): Promise<void> => {
  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, reSequence(list));

  await autoSaveAccountingOperatorAsync(type);
};

const populateAccountingOperatorDefaults = (): void => {
  if (!authStore.profile) {
    accountingOperatorDefaults.value = [];
    return;
  }

  accountingOperatorDefaults.value = [{
    sequence: 1,
    userId: authStore.profile.id,
    fullName: authStore.profile.name,
    positionName: authStore.profile.positionName,
    acceptorType: AcceptorType.AccountingOperator,
    status: AcceptorStatus.Draft,
  } as ParticipantsAcceptor];
};

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const getDefaultAccountingOperator = (): void => {
  populateAccountingOperatorDefaults();

  const reviewers: ParticipantsAcceptor[] = accountingOperatorDefaults.value
    .slice()
    .sort((a, b): number => a.sequence - b.sequence)
    .map((c, idx): ParticipantsAcceptor => ({
      userId: c.userId,
      acceptorType: AcceptorType.AccountingOperator,
      departmentName: c.departmentName ?? '',
      fullName: c.fullName,
      positionName: c.positionName ?? '',
      sequence: idx + 1,
      status: AcceptorStatus.Draft,
      organizationLevel: c.organizationLevel,
    } as ParticipantsAcceptor));

  setAccountingListByType(AcceptorType.AccountingOperator, reviewers);
};

watch(() => [store.body.status, store.body.accountStatus], () => {
  currentAccordion.value = getActiveAccordions.value;

  if (store.body.status === CM001PeriodStatus.Approved &&
    store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate) {
    currentTab.value = CM001PeriodTabHeader.Detail;
    nextTick(() => {
      setTimeout(() => {
        disbursementSectionRef.value?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 300);
    });
  }

  if (store.body.status === CM001PeriodStatus.Approved &&
    store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval) {
    currentTab.value = CM001PeriodTabHeader.Detail;
    nextTick(() => {
      setTimeout(() => {
        inspectionSectionRef.value?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      }, 300);
    });
  }
}, { immediate: true });

watch(() => store.checkAssignee, async (newValue, oldValue) => {
  if (isInitialLoad.value) return;

  if (newValue !== oldValue && store.body.id && store.body.contractBudgetAmount && store.body.contractBudgetAmount > 0) {
    await store.fn.setDefaultAcceptor();

    if (!store.checkAssignee && store.body.acceptors.length > 0 && store.states.isEdit) {
      const lastAcceptor = store.body.acceptors[store.body.acceptors.length - 1];
      lastAcceptor.sequence = 1;
      store.body.acceptors = [lastAcceptor];
    }
  }
}, { flush: 'post' });

watch(() => store.body.assignees.filter(a => a.assigneeType === AssigneeType.Assignee), async (newValue, oldValue) => {
  if (isInitialLoad.value) return;

  if (newValue.length !== oldValue.length && newValue.length > 0) {
    await store.fn.assigneeDefaultAcceptor();
  }
}, { deep: true, flush: 'post' });

watch(
  () => store.body,
  () => {
    if (!!id.value && !isInitialLoad.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

</script>

<template>
  <TitleHeader label="จัดการรายงานผลการตรวจรับ" />
  <PeriodInfo :data="store.body" :contractType="getLabelFromDropdown(storeCm01.body.contractType)" />
  <CommercialMaterialSection
    v-if="checkPlanDepartmentCode && storeCm01.body.sourceType === souceType.Manual"
    :model-value="storeCm01.body.isCommercialMaterial"
    :department-id="storeCm01.body.departmentId"
    :supply-method-code="storeCm01.body.supplyMethodCode"
    disabled />
  <div class="grid grid-cols-2 gap-4">
    <div class="center">
      <h5 class="text-primary"> Contract Management</h5>
    </div>
    <div class="center">
      <h5 class="text-primary">Accounting</h5>
    </div>
  </div>
  <hr />
  <div class="grid grid-cols-2 gap-4">
    <div class="center">
      <Button class="w-full rounded-none" :class="{ 'bg-white!': isContractManagementDone }"
        :severity="contractManagementButtonSeverity"
        :variant="isContractManagementDone ? 'outlined' : undefined">
        <span class="text-wrap">
          รายงานผลการตรวจรับ
        </span>
      </Button>
    </div>
    <div class="center">
      <Button class="w-full rounded-none" :class="{ 'bg-white!': isAccountingDone }"
        :severity="accountingButtonSeverity"
        :variant="isAccountingDone ? 'outlined' : undefined">
        <span>บัญชีเบิกจ่าย</span>
      </Button>
    </div>
  </div>

  <TitleHeader label="รายงานผลการตรวจรับ" :routeItems>
    <template #action>
      <BadgeComponent
        :label="store.body.status === CM001PeriodStatus.Approved ? PeriodAccountStatus(store.body.accountStatus, store.body.status).label : PeriodBadgeStatus(store.body.status).label"
        :color="store.body.status === CM001PeriodStatus.Approved ? PeriodAccountStatus(store.body.accountStatus, store.body.status).color : PeriodBadgeStatus(store.body.status).color" />
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>
  <VeeForm @submit="handleSaveClick" @invalidSubmit="ToastHelper.invalidMessageToast()"
    v-slot="{ handleSubmit: submit }">
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => onTabChange(tab.toString())">
          <TabHeader
            :items="(store.body.documentId || (store.body.documentVersions && store.body.documentVersions.length > 0)) ? PeriodTabHeaderItem : PeriodTabHeaderItem.filter(item => item.value !== CM001PeriodTabHeader.Document)"
            class="sticky top-14 z-3 pt-2 bg-[#F7F7F7]" />
          <TabPanel :value="CM001PeriodTabHeader.Detail">
            <div ref="inspectionSectionRef">
              <InspectionSection :disabled="!store.states.isEdit || !menuStore.hasManage" />
            </div>
            <InspectionCommittees class="mb-4"
              v-if="store.states.isEdit"
              label="ผูู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ" v-model="store.body.inspectionCommittees"
              person="ผู้ตรวจรับพัสดุ" :is-disabled="!store.states.isEdit || !menuStore.hasManage"
              v-model:spacial-option="store.positionInspOptions" />
            <BudgetSection class="mb-4" v-model="store.body.budgetDetails"
              :disabled="(!store.states.isEdit && !store.states.isAccountingCanEdit) || !menuStore.hasManage" />
            <div ref="disbursementSectionRef" class="mb-4">
              <DisbursementSection
                v-if="isDisbursementInputManage || store.body.accountStatus === CmDeliveryAcceptancePeriodAccountStatus.Paid" />
            </div>
            <UploadFileGroup class="mb-4" v-if="store.body.id" v-model="store.body.attachments"
              @upload="() => store.fn.onUpsertAttachments(id)" @remove-file="() => store.fn.onUpsertAttachments(id)"
              @remove-group="() => store.fn.onUpsertAttachments(id)" @reorder="() => store.fn.onUpsertAttachments(id)"
              :disabled="!menuStore.hasManage" />
          </TabPanel>
          <TabPanel :value="CM001PeriodTabHeader.Document">
            <Card class="mb-4">
              <template #content>
                <ChEditor :docId="store.body.documentId" :docName="new Date().toISOString()" :showRemark="false"
                  :readonly="!store.states.canEditDocument" ref="documentRef" :key="`${store.body.documentId}-${store.body.status}`"
                  v-if="store.body.documentId && menuStore.hasManage" :save="saveDocument"
                  :versions="store.body.documentVersions ?? []" :canRestoreVersion="store.states.canRestoreVersion"
                  @restore-version="() => store.fn.resetDocumentAsync(id)" />
              </template>
            </Card>
          </TabPanel>
        </Tabs>
      </div>

      <div class="lg:col-span-2 relative">
        <div class="flex flex-col gap-4 ml-3 sticky top-14 pt-2">
          <div id="button-section" class="flex items-center gap-2 justify-end">

            <div id="committee-edit" class="flex items-center gap-2" v-if="store.states.isEdit">
              <ButtonSave type="submit" :disabled="isSaving" />
              <ButtonApproveConfirm v-if="store.body.id && store.body.documentId"
                @click="() => submit(() => handleSubmitWithValidation(() => store.fn.onSendCommitteeApprovalAsync(id)))" />
            </div>
            <div id="committee-approve" class="flex items-center gap-2" v-if="store.states.canCommitteeRecall">
              <ButtonRecall @click="() => handleSubmitWithValidation(() => store.fn.onRecallAsync(id))" />
            </div>
            <div id="committee-approve" class="flex items-center gap-2" v-if="store.states.isCommitteeCurrentApproval">
              <ButtonNotAgree v-if="store.states.isCommitteeCurrentApproval"
                @click="() => handleSubmitWithValidation(() => store.fn.onSendApproveOrRejectAsync(id, 'Reject', AcceptorType.AcceptanceCommittee))" />
              <ButtonApprove v-if="store.states.isCommitteeCurrentApproval"
                @click="() => handleSubmitWithValidation(() => store.fn.onSendApproveOrRejectAsync(id, 'Approve', AcceptorType.AcceptanceCommittee))" />
            </div>
            <div id="assignee" class="flex items-center gap-2" v-if="store.states.isAssignee">
              <ButtonSendEdit @click="() => store.fn.onRejectAssigneeAsync(id)" v-if="store.states.isAssigned" />
              <ButtonSave label="บันทึกผู้รับผิดชอบ"
                @click="() => submit(() => handleSubmitWithValidation(() => store.fn.onSubmitAsync(id)))"
                v-if="(store.states.isAssigned || store.states.isComment) && store.body.documentId" />
              <ButtonConfirmAssign @click="() => handleSubmitWithValidation(() => store.fn.onConfirmAssignedAsync(id))"
                v-if="store.states.jorporCanAssignByAssignee" />
              <ButtonApproveConfirm
                @click="() => submit(() => handleSubmitWithValidation(() => store.fn.onSendApproveAsync(id)))"
                v-if="store.states.isComment" />
            </div>
            <div id="approval-aprove" class="flex items-center gap-2" v-if="store.states.isCurrentApprover">
              <ButtonSendEdit v-if="store.states.isCurrentApprover"
                @click="() => handleSubmitWithValidation(() => store.fn.onSendApproveOrRejectAsync(id, 'Reject', AcceptorType.Approver))" />
              <ButtonApprove v-if="store.states.isCurrentApprover && !store.states.isLastApprover"
                @click="() => handleSubmitWithValidation(() => store.fn.onSendApproveOrRejectAsync(id, 'Approve', AcceptorType.Approver))" />
              <ButtonConfirm v-if="store.states.isCurrentApprover && store.states.isLastApprover"  label="รับทราบผลการตรวจรับ"
                @click="() => handleSubmitWithValidation(() => store.fn.onSendApproveOrRejectAsync(id, 'Approve', AcceptorType.Approver))" />
            </div>
            <div id="accounting" class="flex items-center gap-2"
              v-if="store.states.isAccountingCanEdit || store.states.isAccountingCanAssign || store.states.isAccountingApprover || isConfirmerSectionManage || isDisbursementInputManage">
              <ButtonSave @click="() => submit(() => handleSubmitWithValidation(() => store.fn.onSubmitAsync(id)))"
                v-if="store.states.isAccountingCanAssign && !isAccountingSectionReadonly" />
              <ButtonSave @click="() => handleSubmitWithValidation(() => store.fn.onSubmitAsync(id))"
                v-if="isDisbursementInputManage" label="บันทึกชั่วคราว" />
              <ButtonSendEdit
                @click="() => handleSubmitWithValidation(() => store.fn.onAccountingApproveOrRejectAsync(id, 'Reject'))"
                v-if="store.states.isAccountingApprover && isAccountingAcceptorsUnchanged" />
              <Button label="ยืนยันตรวจสอบ" icon="pi pi-user-plus" severity="success"
                @click="() => handleSubmitWithValidation(() => store.fn.onAccountingApproveOrRejectAsync(id, 'Approve'))"
                v-if="store.states.isAccountingApprover && isAccountingAcceptorsUnchanged && store.states.isCurrentAccountingOperator" />
              <ButtonApprove
                @click="() => handleSubmitWithValidation(() => store.fn.onAccountingApproveOrRejectAsync(id, 'Approve'))"
                v-if="store.states.isAccountingApprover && isAccountingAcceptorsUnchanged && !store.states.isCurrentAccountingOperator && !store.states.isLastAccountingApprover" />
              <ButtonConfirm
                @click="() => handleSubmitWithValidation(() => store.fn.onAccountingApproveOrRejectAsync(id, 'Approve'))"
                v-if="store.states.isAccountingApprover && isAccountingAcceptorsUnchanged && !store.states.isCurrentAccountingOperator && store.states.isLastAccountingApprover" />
              <Button severity="warn" v-if="isDisbursementInputManage" label="บันทึกยืนยันวันที่เบิกจ่าย"
                @click="() => submit(() => handleSubmitWithValidation(() => store.fn.onSetDisburmentDateAsync(id)))" />
            </div>
          </div>

          <div id="accordion-section">
            <Accordion v-model:value="currentAccordion" unstyled multiple>
              <AccordionPanel value="999" class="mb-4" v-if="currentTab == CM001PeriodTabHeader.Document">
                <AccordHeader label="Dictionary" />
                <AccordionContent>
                  <Card class="rounded-none overflow-auto h-[800px]">
                    <template #content>
                      <!-- Dictionary mapping for delivery-acceptance period document. -->
                      <DocumentMapping pathToGet="delivery-acceptance/period" @on-click-select="
                        (text, hint) => documentRef?.setPlaceholderInDocument(text, hint)
                      " />
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel class="mb-4" :value="CM001PeriodAccordion.Committee">
                <AccordAcceptor :title="PeriodAccordionName(CM001PeriodAccordion.Committee)"
                  v-model="store.body.acceptanceCommittees" :acceptor-type="AcceptorType.AcceptanceCommittee" is-approve
                  @set-is-unable-to-perform-duties="(status: boolean, acceptorId: string, remark?: string) => submit(() => store.fn.onUpdateDutiesStatusAsync(id, status, remark, acceptorId))"
                  :is-disable="(!store.states.isEdit && !store.states.isCommitteeApproval) || !menuStore.hasManage" />
              </AccordionPanel>
              <AccordionPanel class="mb-4" :value="CM001PeriodAccordion.Assignee"
                v-if="store.checkAssignee && store.states.canAssignee && (store.body.documentId || store.body.status === CM001PeriodStatus.Approved)">
                <AccordAssignee :title="PeriodAccordionName(CM001PeriodAccordion.Assignee)"
                  v-model="store.body.assignees" :group="AssigneeGroup.Contract"
                  :disabled="!store.states.isAssigned || !menuStore.hasManage" :is-comment="store.states.isComment"
                  @on-comment="async (e) => { await saveDocumentFirst(); await store.fn.onAssigneeCommentAsync(id, e.reason); }" />
              </AccordionPanel>
              <AccordionPanel v-if="showAcceptor" class="mb-4" :value="CM001PeriodAccordion.Acceptor">
                <AccordAcceptor :title="PeriodAccordionName(CM001PeriodAccordion.Acceptor)"
                  v-model="store.body.acceptors" :acceptor-type="AcceptorType.Approver" isManage
                  @set-default="() => getDefaultAcceptor()"
                  :is-disable="(!store.states.isEdit && !store.states.isComment && !store.states.isAssigned) || !menuStore.hasManage"
                  :is-set-default="store.states.isCanSetDefaultApprover" isApprove />
              </AccordionPanel>
              <AccordionPanel class="mb-4" v-if="store.body.id"
                :value="CM001PeriodAccordion.Accounting">
                <AccordHeader :label="PeriodAccordionName(CM001PeriodAccordion.Accounting)" />
                <AccordionContent>
                  <Card class="rounded-none">
                    <template #content>
                      <div class="mb-6" v-if="showAccountingOperatorSection">
                        <AccountingAcceptorSection title="ส่วนบัญชีค่าใช้จ่าย"
                          :acceptor-type="AcceptorType.AccountingOperator"
                          v-model="accountingOperators"
                          :readonly="isAccountingSectionReadonly"
                          :current-user-id="authStore.profile.id"
                          drag-handle-class="drag-acc-operator"
                          group-name="accountingOperatorGroup"
                          can-remove-self
                          inline-add-button
                          @add="addAccountingAcceptorAsync(AcceptorType.AccountingOperator)"
                          @remove="(index) => removeAccountingAcceptorAsync(AcceptorType.AccountingOperator, index)"
                          @drag-end="onAccountingReSequenceDrag(AcceptorType.AccountingOperator)" />
                      </div>

                      <AccountingAcceptorSection title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                        :acceptor-type="AcceptorType.Accounting"
                        v-model="accountingApprovers"
                        :readonly="isAccountingSectionReadonly"
                        :current-user-id="authStore.profile.id"
                        drag-handle-class="drag-acc-approver"
                        group-name="accountingApproverGroup"
                        can-remove-self
                        @add="addAccountingAcceptorAsync(AcceptorType.Accounting)"
                        @remove="(index) => removeAccountingAcceptorAsync(AcceptorType.Accounting, index)"
                        @drag-end="onAccountingReSequenceDrag(AcceptorType.Accounting)">
                        <template #leadingButtons>
                          <Button v-if="(store.states.isAccountingCanAssign || store.states.isEdit) && !store.states.isBranchDepartment" label="กำหนดค่าเริ่มต้น"
                            severity="primary" variant="outlined" icon="pi pi-undo"
                            @click="() => store.fn.getDefaultDisbursementAcceptor()" />
                        </template>
                      </AccountingAcceptorSection>
                    </template>
                  </Card>
                </AccordionContent>
              </AccordionPanel>
              <AccordionPanel class="mb-4"
                v-if="showAccountingConfirmerSection"
                :value="CM001PeriodAccordion.AccountingConfirmer">
                <AccordAcceptor :title="'ส่วนบัญชีค่าใช้จ่าย (จบงาน)'" v-model="store.body.acceptanceConfirmers"
                  :acceptor-type="AcceptorType.AccountingConfirmer"
                  :is-manage="isConfirmerSectionManage"
                  isApprove />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </VeeForm>
</template>

<style scoped lang="scss">
.center {
  text-align: center;
}

hr {
  display: block;
  height: 1px;
  border: 0;
  border-top: 2px solid var(--color-primary);
  margin: 1em 0;
  padding: 0;
}

.disabled-btn {
  cursor: not-allowed;
}

.text-wrap {
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
</style>