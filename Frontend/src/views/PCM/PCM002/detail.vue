<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { AccordAcceptor } from '@/components/Accordions';
import { ButtonApprove, ButtonConfirm, ButtonRecall, ButtonSave, ButtonSendApprove, ButtonSendEdit } from '@/components/Button';
import ButtonConfirmAssign from '@/components/Button/ButtonConfirmAssign.vue';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import DocumentMapping from '@/components/DocumentMapping.vue';
import { TitleHeader } from '@/components/cosmetic';
import Datepicker from '@/components/forms/Datepicker.vue';
import InputArea from '@/components/forms/InputArea.vue';
import Pcm002Constant from '@/constants/pcm002';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { OrganizationLevelEnum } from '@/enums/shared';
import { Pcm002Status } from '@/enums/pcm002';
import { PreProcurementStep } from '@/enums/preProcurement';
import { ConfirmDialogType } from '@/enums/dialog';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import { ArrayHelper } from '@/helpers/array';
import ToastHelper from '@/helpers/toast';
import type { ProgramMenuType } from '@/models/PCM/PCM005/pcm005';
import type { Option } from '@/models/shared/option';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import pcm002Service from '@/services/PCM/PCM002';
import { usePcm002DetailStore } from '@/stores/PCM/pcm002';
import { useAuthenticationStore } from '@/stores/authentication';
import { useMenuStore } from '@/stores/menu';
import { HttpStatusCode } from 'axios';
import { TabPanel, TabPanels } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UploadFile from './components/UploadFile.vue';

const Detail = defineAsyncComponent(() => import('./components/Detail.vue'));
const ApprovalDocument = defineAsyncComponent(() => import('./components/ApprovalDocument.vue'));
const WinnerDocument = defineAsyncComponent(() => import('./components/WinnerDocument.vue'));
const AccountingAcceptorSection = defineAsyncComponent(() => import('@/views/CM/CM001/components/Period/AccountingAcceptorSection.vue'));

const approvalDocumentRef = ref<InstanceType<typeof ApprovalDocument> | null>(null);
const winnerDocumentRef = ref<InstanceType<typeof WinnerDocument> | null>(null);
const detailRef = ref<HTMLElement | null>(null);

const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);
const menuStore = useMenuStore();
const pcm002Store = usePcm002DetailStore();
const userStore = useAuthenticationStore();
const route = useRoute();
const router = useRouter();
const showReviewDocumentDialog = ref(false);
const disbursementCardRef = ref<HTMLElement | null>(null);
const reviewDocumentId = ref<string>('');
const { BadgeStatusColor } = Pcm002Constant;

const id = ref(route.params?.id);

onMounted(async () => {
  await onInitPageAsync();

  await pcm002Store.getAssignDepartmentDDLAsync();
  await pcm002Store.getDepartmentDDLAsync();
  await pcm002Store.onGetDropdownAsync();

  await nextTick();
  isInitialized.value = true;
});

const defaultDocumentStatuses = [
  Pcm002Status.WaitingApproval,
];

watch(
  () => pcm002Store.detail.status,
  (newStatus: Pcm002Status) => {
    if (defaultDocumentStatuses.includes(newStatus)) {
      currentTab.value = '1';
    }
  },
  { immediate: true }
)

watch(
  () => pcm002Store.detail.status,
  async (newStatus: Pcm002Status) => {
    if (newStatus === Pcm002Status.WaitingDisbursementDate) {
      currentTab.value = '0';
      await nextTick();
      setTimeout(() => {
        const el = (disbursementCardRef.value as any)?.$el as HTMLElement | undefined;
        el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 300);
    }
    if (newStatus === Pcm002Status.WaitingAccountingApproval) {
      currentTab.value = '0';
      await nextTick();
      setTimeout(() => {
        detailRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 300);
    }
  },
);

watch(() => pcm002Store.detail.supplyMethodCode, async (newValue) => {
  await pcm002Store.getSupplyMethodSpecialTypeDDLAsync(newValue);
});

const onInitPageAsync = async (): Promise<void> => {
  if (id.value) {
    await pcm002Store.getByIdAsync(id.value.toString());

    if (!pcm002Store.detail.acceptors || pcm002Store.detail.acceptors.length === 0) {
      await pcm002Store.getMergedAcceptorsAsync(pcm002Store.detail.departmentCode, pcm002Store.detail.budget, pcm002Store.detail.supplyMethodCode, pcm002Store.detail.supplyMethodSpecialTypeCode);
    }

    if (pcm002Store.isEdit
      && !isDocBranchOrZone.value
      && !(pcm002Store.detail.acceptors ?? []).some(a => a.acceptorType === AcceptorType.AccountingApprover)) {
      await pcm002Store.getDefaultDisbursementAcceptor(pcm002Store.detail.budget);
    }
  }
};

watch(
  () => pcm002Store.detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '1' && approvalDocumentRef.value && 'saveDocumentFirst' in approvalDocumentRef.value) {
    await approvalDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '2' && winnerDocumentRef.value && 'saveDocumentFirst' in winnerDocumentRef.value) {
    await winnerDocumentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmit();
};

const handleRestoreApprovalVersion = async (): Promise<void> => {
  const pcm002Id = pcm002Store.detail.id;
  if (!pcm002Id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await pcm002Service.resetDocumentAsync(pcm002Id, 'Approval');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await pcm002Store.getByIdAsync(pcm002Id);
  }
};

const handleRestoreWinnerVersion = async (): Promise<void> => {
  const pcm002Id = pcm002Store.detail.id;
  if (!pcm002Id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await pcm002Service.resetDocumentAsync(pcm002Id, 'Winner');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await pcm002Store.getByIdAsync(pcm002Id);
  }
};

const onSendApprovalWithDocSave = async () => {
  await saveDocumentFirst();

  if (isFormDirty.value) {
    if (pcm002Store.detail.approvalRequestDocumentId) {
      pcm002Store.detail.isApprovalRequestDocumentReplace = true;
    }
    if (pcm002Store.detail.winnerAnnounceDocumentId) {
      pcm002Store.detail.isWinnerAnnounceDocumentReplace = true;
    }
  }

  await pcm002Store.onSendApprovalAsync();
};

const onRecallWithDocSave = async () => {
  await saveDocumentFirst();

  if (isFormDirty.value) {
    if (pcm002Store.detail.approvalRequestDocumentId) {
      pcm002Store.detail.isApprovalRequestDocumentReplace = true;
    }
    if (pcm002Store.detail.winnerAnnounceDocumentId) {
      pcm002Store.detail.isWinnerAnnounceDocumentReplace = true;
    }
  }

  await pcm002Store.onRecallAsync();
};

const onSubmit = async (): Promise<void> => {
  // Save ChEditor content first before calling API
  await saveDocumentFirst();

  // Set flags so backend re-replaces document — only when form data changed
  if (id.value) {
    if (isFormDirty.value
      && (pcm002Store.detail.approvalRequestDocumentId
        || pcm002Store.detail.winnerAnnounceDocumentId) && [Pcm002Status.Draft, Pcm002Status.Edit, Pcm002Status.Rejected].includes(pcm002Store.detail.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      pcm002Store.detail.isApprovalRequestDocumentReplace = saveOption;
      pcm002Store.detail.isWinnerAnnounceDocumentReplace = saveOption;
    }

    isInitialized.value = false;
    if (await pcm002Store.updateAsync(id.value.toString())) {
      await pcm002Store.getByIdAsync(id.value.toString());
    }

    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    setCurrentTab();

    return;
  }

  isInitialized.value = false;
  const newId = await pcm002Store.createAsync();
  isFormDirty.value = false;
  setCurrentTab();
  if (newId) {
    id.value = newId;

    router.replace(`/pcm/pcm002/detail/${newId}`);

    await pcm002Store.getByIdAsync(newId);

    if (!pcm002Store.detail.acceptors || pcm002Store.detail.acceptors.length === 0) {
      await pcm002Store.getMergedAcceptorsAsync(pcm002Store.detail.departmentCode, pcm002Store.detail.budget, pcm002Store.detail.supplyMethodCode, pcm002Store.detail.supplyMethodSpecialTypeCode);
    }

    if (pcm002Store.isEdit
      && !isDocBranchOrZone.value
      && !(pcm002Store.detail.acceptors ?? []).some(a => a.acceptorType === AcceptorType.AccountingApprover)) {
      await pcm002Store.getDefaultDisbursementAcceptor(pcm002Store.detail.budget);
    }
  }
  await nextTick();
  isInitialized.value = true;
};

const setCurrentTab = async () => {
  if (pcm002Store.detail.approvalRequestDocumentId) {
    currentTab.value = '1';
  }
};

const isCurrentUserApprove = computed(() => {
  const acceptors = pcm002Store.detail.acceptors ?? []

  const pendingSorted = acceptors
    .filter(a => a.status === AcceptorStatus.Pending && a.acceptorType === AcceptorType.Approver)
    .sort((a, b) => a.sequence - b.sequence)

  const firstPending = pendingSorted[0]

  return (firstPending?.delegateeUserId ? firstPending?.delegateeUserId : firstPending?.userId) === userStore.profile.id
});

// const isAccountingCanAssign = computed(() => {
//   const acceptors = pcm002Store.detail.acceptors?.filter(x =>
//     x.acceptorType === AcceptorType.AccountingApprover || x.acceptorType === AcceptorType.AccountingOperator
//   ) ?? []

//   return acceptors.some(a => a.userId === userStore.profile.id || a.delegateeUserId === userStore.profile.id);
// });
const isCurrentUserAccountingApprover = computed(() => {
  const acceptors = pcm002Store.detail.acceptors ?? []

  const firstPending = acceptors
    .filter(a => a.status === AcceptorStatus.Pending &&
      (a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator))
    .sort((a, b) => {
      const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
      const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
      if (typeA !== typeB) return typeA - typeB;
      return a.sequence - b.sequence;
    })[0]

  return (firstPending?.delegateeUserId ? firstPending?.delegateeUserId : firstPending?.userId) === userStore.profile.id
});

const isAccountingAcceptorsUnchanged = computed(() => {
  const current = pcm002Store.detail.acceptors?.filter(a =>
    a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
  ) || [];
  return JSON.stringify(current) === JSON.stringify(pcm002Store.cloneAccountingAcceptors);
});

const HeaderItem = ref([
  {
    label: 'รายละเอียด',
    value: '0',
  },
  {
    label: 'เอกสารรายงานขอความเห็นชอบ',
    value: '1',
  }
] as Option[]);

const routeItems = ref(
  [
    { label: 'รายการจัดซื้อจัดจ้าง ว119', url: '/pcm/pcm002' },
    { label: 'ข้อมูลจัดซื้อจัดจ้าง', },
  ] as MenuItem[]);

const canEditDocument = computed(() => {
  return pcm002Store.detail.status === Pcm002Status.Edit
    || pcm002Store.detail.status === Pcm002Status.Draft
    || pcm002Store.detail.status === Pcm002Status.Rejected
});

const showDisbursementCard = computed(() => {
  return pcm002Store.detail.status === Pcm002Status.WaitingDisbursementDate ||
    pcm002Store.detail.status === Pcm002Status.Paid;
});

const { deleteItemAndReSequence, reSequence } = ArrayHelper();

const isAccountingSectionReadonly = computed<boolean>((): boolean => {
  if (!menuStore.hasManage) return true;

  if (pcm002Store.isEdit) return false;

  const hasApprovedAccountingAcceptor = [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => a.status === AcceptorStatus.Approved);
  if (hasApprovedAccountingAcceptor) return true;

  if ([Pcm002Status.WaitingDisbursementDate, Pcm002Status.Paid].includes(pcm002Store.detail.status)) return true;
  if (!pcm002Store.isAccountingMember) return true;

  const isBranchOrZone = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(pcm002Store.detail.departmentOrganizationLevel ?? '');

  const isInAccountingList = [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => (a.delegateeUserId ?? a.userId) === userStore.profile.id);

  if (!isBranchOrZone && !pcm002Store.isCurrentUserAccountingSegmentMember && !isInAccountingList) return true;

  return false;
});

const setAccountingListByType = (type: AcceptorType, newList: ParticipantsAcceptor[]): void => {
  const arr = pcm002Store.detail.acceptors ?? [];
  const others = arr.filter((a): boolean => a.acceptorType !== type);
  pcm002Store.detail.acceptors = [...others, ...newList];
};

const accountingOperators = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (pcm002Store.detail.acceptors ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.AccountingOperator),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.AccountingOperator, val),
});

const accountingApprovers = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (pcm002Store.detail.acceptors ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.AccountingApprover),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.AccountingApprover, val),
});

const isAccountingMember = computed<boolean>((): boolean =>
  [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => a.userId === userStore.profile.id)
);

const isConfirmerSectionReadonly = computed<boolean>((): boolean => {
  if (pcm002Store.detail.status === Pcm002Status.Paid) return true;

  const isInConfirmerList = (pcm002Store.detail.acceptanceConfirmers ?? [])
    .some((a): boolean => (a.delegateeUserId ?? a.userId) === userStore.profile.id);

  if (isInConfirmerList) return false;

  if (!menuStore.hasManage) return true;

  const isBranchOrZone = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(pcm002Store.detail.departmentOrganizationLevel ?? '');

  if (isBranchOrZone) {
    if (pcm002Store.detail.status === Pcm002Status.WaitingDisbursementDate) {
      return userStore.profile.departmentCode !== pcm002Store.detail.departmentCode;
    }
    return isAccountingSectionReadonly.value;
  }

  return !pcm002Store.isCurrentUserAccountingSegmentMember;
});

const isDocBranchOrZone = computed<boolean>((): boolean =>
  [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(pcm002Store.detail.departmentOrganizationLevel ?? ''));

const showAccountingOperatorSection = computed<boolean>((): boolean =>
  !isDocBranchOrZone.value
  && [Pcm002Status.WaitingAccountingApproval, Pcm002Status.WaitingDisbursementDate, Pcm002Status.Paid]
    .includes(pcm002Store.detail.status));

const showAccountingConfirmerSection = computed<boolean>((): boolean => {
  if (!pcm002Store.detail.id) return false;
  if (isDocBranchOrZone.value) return true;
  return [Pcm002Status.WaitingDisbursementDate, Pcm002Status.Paid].includes(pcm002Store.detail.status);
});

const isCurrentAccountingOperator = computed<boolean>((): boolean => {
  const firstPending = (pcm002Store.detail.acceptors ?? [])
    .filter(a => a.status === AcceptorStatus.Pending &&
      (a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator))
    .sort((a, b) => {
      const typeA = a.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
      const typeB = b.acceptorType === AcceptorType.AccountingOperator ? 0 : 1;
      if (typeA !== typeB) return typeA - typeB;
      return a.sequence - b.sequence;
    })[0];
  if (!firstPending) return false;
  return firstPending.acceptorType === AcceptorType.AccountingOperator
    && (firstPending.delegateeUserId ?? firstPending.userId) === userStore.profile.id;
});

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

  if (!pcm002Store.detail.acceptors) {
    pcm002Store.detail.acceptors = [];
  }

  pcm002Store.detail.acceptors.push({
    userId: res.id,
    acceptorType: type,
    departmentName: res.departmentName ?? '',
    fullName: res.name,
    positionName: res.positionName ?? '',
    sequence: current.length + 1,
    status: AcceptorStatus.Draft,
    organizationLevel: res.organizationLevel,
  } as ParticipantsAcceptor);

  const updatedList = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];
  setAccountingListByType(type, reSequence(updatedList));

  if (id.value) {
    await pcm002Store.updateAsync(id.value.toString());
  }
};

const removeAccountingAcceptorAsync = async (type: AcceptorType, index: number): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, deleteItemAndReSequence(list, index));

  if (id.value) {
    await pcm002Store.updateAsync(id.value.toString());
  }
};

const saveConfirmersAsync = async (): Promise<void> => {
  await pcm002Store.saveAcceptorsAsync(route.params.id as string);
};

const onAccountingReSequenceDrag = async (type: AcceptorType): Promise<void> => {
  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, reSequence(list));

  if (id.value) {
    await pcm002Store.updateAsync(id.value.toString());
  }
};

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  let documentType = '';

  if (currentTab.value === '1') {
    documentType = 'Approval';
  }

  if (documentType === 'Approval') {
    pcm002Store.detail.approvalRequestDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

onUnmounted(() => {
  pcm002Store.onResetDetail();
});

const getStatusButton = (step: PreProcurementStep): string => {
  const status = pcm002Store.detail.status;

  const draftStatuses = [Pcm002Status.Draft, Pcm002Status.Edit, Pcm002Status.WaitingApproval, Pcm002Status.Rejected];
  const accountingYellowStatuses = [Pcm002Status.WaitingAccountingApproval, Pcm002Status.WaitingDisbursementDate];

  if (status === Pcm002Status.Paid) {
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (step === PreProcurementStep.W119) {
    if (current.value === PreProcurementStep.Accounting) {
      return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
    }

    if (draftStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
    }

    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (step === PreProcurementStep.Accounting) {
    if (draftStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#9E9E9E] border-none rounded-none text-white';
    }

    if (current.value === PreProcurementStep.W119) {
      return 'w-full mb-5 bg-white border-[#F9A825] rounded-none text-[#F9A825]';
    }

    if (accountingYellowStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
    }
  }

  return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
};

const getDefaultStep = (status: Pcm002Status): PreProcurementStep => {
  const accountingStatuses = [
    Pcm002Status.WaitingAccountingApproval,
    Pcm002Status.WaitingDisbursementDate,
    Pcm002Status.Paid
  ];

  if (accountingStatuses.includes(status)) {
    return PreProcurementStep.Accounting;
  }

  return PreProcurementStep.W119;
};

const current = ref(pcm002Store.detail.currentStep || getDefaultStep(pcm002Store.detail.status));
const menuSelected = ref<PreProcurementStep>(getDefaultStep(pcm002Store.detail.status));

const availableSteps = computed(() => {
  const status = pcm002Store.detail.status;

  if (status === Pcm002Status.WaitingAccountingApproval ||
    status === Pcm002Status.WaitingDisbursementDate ||
    status === Pcm002Status.Paid) {
    return [PreProcurementStep.W119, PreProcurementStep.Accounting];
  }

  return [PreProcurementStep.W119];
});

const isButtonDisabled = (step: string): boolean => {
  return ![...availableSteps.value, menuSelected.value].includes(step as PreProcurementStep);
};

watch(() => pcm002Store.detail.currentStep, (val: PreProcurementStep): void => {
  if (val) {
    onChangeProgram(val);
  };
});

watch((): Pcm002Status => pcm002Store.detail.status, (newStatus: Pcm002Status): void => {
  if (newStatus) {
    const defaultStep = getDefaultStep(newStatus);
    current.value = defaultStep;
    menuSelected.value = defaultStep;

    if (defaultStep === PreProcurementStep.Accounting) {
      currentTab.value = '0';
    }
  }
}, { immediate: true });

const onChangeProgram = (name: PreProcurementStep): void => {
  menuSelected.value = name;
  current.value = name;

  if (name === PreProcurementStep.Accounting) {
    currentTab.value = '0';
  }
};

const programMenu = ref<ProgramMenuType>({
  procurement: [
    { menu: 'รายการจัดซื้อจัดจ้าง ว119', status: 'Waiting', name: PreProcurementStep.W119 },
  ],
  accounting: [
    { menu: 'บัญชีเบิกจ่าย', status: 'Waiting', name: PreProcurementStep.Accounting },
  ],
});

const summaryGlAccountAmount = computed(() => pcm002Store.detail.glAccounts?.reduce((a, b) => a + b.amount, 0) ?? 0);
</script>

<template>
  <Form @submit="onSubmit()" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง ว119" :route-items="routeItems" />
    <div class="mt-5">
      <div class="grid grid-cols-2 text-center ">
        <p class="text-primary text-[30px]">Procurement</p>
        <p class="text-primary text-[30px]">Accounting</p>
      </div>
      <hr class="mt-3 text-primary border-1" />
      <div class="grid grid-cols-2 gap-4 content">
        <div>
          <Button :class="`${getStatusButton(data.name)} w-full mt-2`" v-for="data in programMenu.procurement"
            :key="data.menu" :disabled="isButtonDisabled(data.name)">
            {{ data.menu }}
          </Button>
        </div>
        <div>
          <Button :class="`${getStatusButton(data.name)} w-full mt-2`" v-for="data in programMenu.accounting"
            :key="data.menu" :disabled="isButtonDisabled(data.name)">
            {{ data.menu }}
          </Button>
        </div>
      </div>
    </div>
    <TitleHeader label="" hidden-icon>
      <template #action>
        <div class="flex items-center gap-2">
          <p class="text-sm">สถานะ :</p>
          <BadgeStatus :label="BadgeStatusColor(pcm002Store.detail.status)?.label"
            :color="BadgeStatusColor(pcm002Store.detail.status)?.color" />
        </div>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
          v-if="pcm002Store.detail.id" class="bg-white! hover:bg-red-50!"
          @click="() => showActivityDialog(pcm002Store.detail.id!)" />
      </template>
    </TitleHeader>
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="HeaderItem.filter((f, index) => pcm002Store.detail.id ? f : index === 0)"
            class="sticky top-[58px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <div ref="detailRef">
                <Detail />
              </div>
              <Card class="mt-4" v-if="showDisbursementCard" ref="disbursementCardRef">
                <template #content>
                  <TitleHeader label="ข้อมูลการเบิกจ่าย" />
                  <div class="grid lg:grid-cols-4 gap-2 mt-10">
                    <Datepicker label="วันที่เบิกจ่าย" rules="required" v-model="pcm002Store.detail.disbursementDate"
                      :disabled="!pcm002Store.canConfirm || !menuStore.hasManage" />
                  </div>

                  <div class="grid lg:grid-cols-4 gap-2 mt-8">
                    <InputNumber label="จำนวนเงินเบิกจ่าย" v-model="pcm002Store.detail.disbursementAmount"
                      :disabled="!pcm002Store.canConfirm || !menuStore.hasManage"
                      :rules="`required|max_value:${summaryGlAccountAmount}`" inputClass="text-right"
                      :minFractionDigits="2" grouping />
                  </div>
                  <InputArea label="หมายเหตุ" rules="required" v-model="pcm002Store.detail.disbursementDescription"
                    :disabled="!pcm002Store.canConfirm || !menuStore.hasManage" class="mb-2 mt-12" />
                </template>
              </Card>
            </TabPanel>
            <TabPanel value="1">
              <Card>
                <template #content>
                  <ApprovalDocument v-model="pcm002Store.detail.approvalRequestDocumentId" ref="approvalDocumentRef"
                    :readonly="!canEditDocument" :save="saveDocument"
                    :versions="pcm002Store.detail.approvalRequestDocumentVersions"
                    :canRestoreVersion="pcm002Store.isEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreApprovalVersion" />
                </template>
              </Card>
            </TabPanel>
            <TabPanel value="2">
              <Card>
                <template #content>
                  <WinnerDocument v-model="pcm002Store.detail.winnerAnnounceDocumentId" ref="winnerDocumentRef"
                    :readonly="!canEditDocument" :save="saveDocument"
                    :versions="pcm002Store.detail.winnerAnnounceDocumentVersions"
                    :canRestoreVersion="pcm002Store.isEdit && menuStore.hasManage && canEditDocument"
                    @restore-version="handleRestoreWinnerVersion" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
          <UploadFile />
        </Tabs>
      </div>

      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2" v-if="menuStore.hasManage">
            <template v-if="pcm002Store.isMyDepartment">
              <ButtonSave type="submit" v-if="pcm002Store.isEdit" />
              <ButtonSendApprove @click="handleSubmit(onSendApprovalWithDocSave)"
                v-if="pcm002Store.detail.id && pcm002Store.isEdit" />
            </template>
            <ButtonRecall @click="onRecallWithDocSave()" v-if="pcm002Store.isCanRecall" />

            <template v-if="isCurrentUserApprove">
              <ButtonSendEdit @click="pcm002Store.onRejectedAsync" v-if="pcm002Store.isApproveReject" />
              <ButtonApprove @click="pcm002Store.onApprovedAsync"
                v-if="pcm002Store.isApproveReject && !pcm002Store.isLastApprover" />
              <ButtonConfirm @click="pcm002Store.onApprovedAsync"
                v-if="pcm002Store.isApproveReject && pcm002Store.isLastApprover" />
            </template>
            <ButtonSave type="submit"
              v-if="pcm002Store.detail.status == Pcm002Status.WaitingAccountingApproval && isAccountingMember" />
            <template
              v-if="pcm002Store.detail.status === Pcm002Status.WaitingAccountingApproval && isCurrentUserAccountingApprover">
              <ButtonSendEdit @click="pcm002Store.onRejectedAsync"
                v-if="pcm002Store.isAccountingApproveReject && isAccountingAcceptorsUnchanged" />
              <Button label="ยืนยันตรวจสอบ" icon="pi pi-user-plus" severity="success"
                @click="pcm002Store.onApprovedAsync"
                v-if="pcm002Store.isAccountingApproveReject && isAccountingAcceptorsUnchanged && isCurrentAccountingOperator" />
              <ButtonApprove @click="pcm002Store.onApprovedAsync" v-if="pcm002Store.isAccountingApproveReject &&
                !pcm002Store.isLastAccountingApprover &&
                isAccountingAcceptorsUnchanged && !isCurrentAccountingOperator" />
              <ButtonConfirm @click="pcm002Store.onApprovedAsync" v-if="pcm002Store.isAccountingApproveReject &&
                pcm002Store.isLastAccountingApprover &&
                isAccountingAcceptorsUnchanged" />
            </template>
            <template v-if="pcm002Store.canConfirm">
              <ButtonSave type="submit" label="บันทึกชั่วคราว" />
              <ButtonConfirmAssign @click="handleSubmit(pcm002Store.onConfirmDisbursementAsync)"
                label="ยืนยันเบิกจ่าย" />
            </template>
          </div>
          <Accordion :value="['0', '1', '2']" unstyled multiple>
            <AccordionPanel value="999" class="mb-4" v-if="currentTab !== '0'">
              <AccordHeader label="Dictionary" />
              <AccordionContent>
                <div></div>
                <Card class="rounded-none overflow-auto h-[800px]">
                  <template #content>
                    <DocumentMapping pathToGet="pw119" @on-click-select="
                      (text, hint) => currentTab == '1' ? approvalDocumentRef?.setPlaceholderInDocument(text, hint)
                        : winnerDocumentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="pcm002Store.detail.acceptors"
                :acceptor-type="AcceptorType.Approver" is-approve v-if="pcm002Store.detail.acceptors" is-manage
                :is-disable="!pcm002Store.isEdit" :is-set-default="!!pcm002Store.detail.id"
                @set-default="() => pcm002Store.getMergedAcceptorsAsync(pcm002Store.detail.departmentCode, pcm002Store.detail.budget, pcm002Store.detail.supplyMethodCode, pcm002Store.detail.supplyMethodSpecialTypeCode)" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-5"
              v-if="pcm002Store.detail.id">
              <AccordHeader label="บัญชีเห็นชอบ/อนุมัติ" />
              <AccordionContent>
                <Card class="rounded-none">
                  <template #content>
                    <div class="mb-6" v-if="showAccountingOperatorSection">
                      <AccountingAcceptorSection title="ส่วนบัญชีค่าใช้จ่าย"
                        :acceptor-type="AcceptorType.AccountingOperator"
                        v-model="accountingOperators"
                        :readonly="isAccountingSectionReadonly"
                        :current-user-id="userStore.profile.id"
                        drag-handle-class="drag-acc-operator"
                        group-name="accountingOperatorGroup"
                        inline-add-button
                        @add="addAccountingAcceptorAsync(AcceptorType.AccountingOperator)"
                        @remove="(index) => removeAccountingAcceptorAsync(AcceptorType.AccountingOperator, index)"
                        @drag-end="onAccountingReSequenceDrag(AcceptorType.AccountingOperator)" />
                    </div>
                    <AccountingAcceptorSection title="ผู้มีอำนาจเห็นชอบ/อนุมัติ"
                      :acceptor-type="AcceptorType.AccountingApprover"
                      v-model="accountingApprovers"
                      :readonly="isAccountingSectionReadonly"
                      :current-user-id="userStore.profile.id"
                      drag-handle-class="drag-acc-approver"
                      group-name="accountingApproverGroup"
                      @add="addAccountingAcceptorAsync(AcceptorType.AccountingApprover)"
                      @remove="(index) => removeAccountingAcceptorAsync(AcceptorType.AccountingApprover, index)"
                      @drag-end="onAccountingReSequenceDrag(AcceptorType.AccountingApprover)">
                      <template #leadingButtons>
                        <Button v-if="(pcm002Store.isAccountingCanAssign || pcm002Store.isEdit) && !isDocBranchOrZone" label="กำหนดค่าเริ่มต้น"
                          severity="primary" variant="outlined" icon="pi pi-undo"
                          @click="() => pcm002Store.getDefaultDisbursementAcceptor(pcm002Store.detail.budget)" />
                      </template>
                    </AccountingAcceptorSection>
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-5"
              v-if="showAccountingConfirmerSection">
              <AccordAcceptor title="ส่วนบัญชีค่าใช้จ่าย (จบงาน)" v-model="pcm002Store.detail.acceptanceConfirmers"
                :acceptor-type="AcceptorType.AccountingConfirmer" is-approve
                v-if="pcm002Store.detail.acceptanceConfirmers" is-manage
                :is-disable="isConfirmerSectionReadonly"
                @add="saveConfirmersAsync"
                @remove="saveConfirmersAsync" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviewDocumentId"
    :docName="`pcm002-${new Date().toISOString()}-${reviewDocumentId}`" @on-click-use-document="setDocumentReviewId" />
</template>