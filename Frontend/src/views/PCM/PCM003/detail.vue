<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { computed, defineAsyncComponent, nextTick, onMounted, ref, watch } from 'vue';
import { TabPanel, TabPanels } from 'primevue';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import UploadFile from './components/UploadFile.vue';
import { BadgeStatus } from '@/components';
import { ButtonApprove, ButtonSendApprove, ButtonSave, ButtonSendEdit, ButtonRecall, ButtonConfirm } from '@/components/Button';
import { useRoute } from 'vue-router';
import { usePcm003DetailStore } from '@/stores/PCM/pcm003';
import { useAuthenticationStore } from '@/stores/authentication';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { OrganizationLevelEnum } from '@/enums/shared';
import Pcm003Constant from '@/constants/pcm003';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import ToastHelper from '@/helpers/toast';
import { Pcm003Status } from '@/enums/pcm003';
import DialogReviewDocument from '@/components/Document/DialogReviewDocument.vue';
import DocumentMapping from '@/components/DocumentMapping.vue';
import { ConfirmDialogType } from '@/enums/dialog';
import { showActivityDialog, showConfirmDialogAsync, showSaveOptionDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import { ArrayHelper } from '@/helpers/array';
import AccordAcceptor from '@/components/Accordions/AccordAcceptor.vue';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import Pcm003Service from '@/services/PCM/PCM003';
import { HttpStatusCode } from 'axios';
import { PreProcurementStep } from '@/enums/preProcurement';
import type { ProgramMenuType } from '@/models/PCM/PCM005/pcm005';
import ButtonConfirmAssign from '@/components/Button/ButtonConfirmAssign.vue';

const Detail = defineAsyncComponent(() => import('./components/Detail.vue'));
const ApprovalDocument = defineAsyncComponent(() => import('./components/ApprovalDocument.vue'));
const WinnerDocument = defineAsyncComponent(() => import('./components/WinnerDocument.vue'));
const AccountingAcceptorSection = defineAsyncComponent(() => import('@/views/CM/CM001/components/Period/AccountingAcceptorSection.vue'));

const approvalDocumentRef = ref<InstanceType<typeof ApprovalDocument> | null>(null);
const winnerDocumentRef = ref<InstanceType<typeof WinnerDocument> | null>(null);
const detailRef = ref<HTMLElement | null>(null);

const route = useRoute();


const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);
const menuStore = useMenuStore();
const { hasManage } = storeToRefs(menuStore);
const userStore = useAuthenticationStore();
const pcm003Store = usePcm003DetailStore();
const { detail } = storeToRefs(pcm003Store);
const { onResetBody, createAsync, updateAsync, onSendApprovalAsync, onRecallAsync, onRejectedAsync, onApprovedAsync } = pcm003Store;
const { BadgeStatusColor } = Pcm003Constant;
const showReviewDocumentDialog = ref(false);
const reviewDocumentId = ref<string>('');

const routeItems = ref(
  [
    { label: 'รายการจัดซื้อจัดจ้าง กรณีเร่งด่วน', url: '/pcm/pcm003' },
    { label: 'ข้อมูลจัดซื้อจัดจ้าง', },
  ] as MenuItem[]);

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

const routeId = computed(() => route.params?.id as string | undefined);
const disbursementCardRef = ref<HTMLElement | null>(null);

onMounted(async () => {
  onInitPageAsync();

  await pcm003Store.getAssignDepartmentDDLAsync();
  await Promise.all([
    pcm003Store.getDepartmentDDLAsync(),
    pcm003Store.onGetDropdownAsync(),
  ]);
});

const defaultDocumentStatuses = [
  Pcm003Status.WaitingApproval,
];

watch(
  () => pcm003Store.detail.status,
  (newStatus: Pcm003Status) => {
    if (defaultDocumentStatuses.includes(newStatus)) {
      currentTab.value = '1';
    }
  },
  { immediate: true }
)

watch(
  () => pcm003Store.detail.status,
  async (newStatus: Pcm003Status) => {
    if (newStatus === Pcm003Status.WaitingDisbursementDate) {
      currentTab.value = '0';
      await nextTick();
      setTimeout(() => {
        const el = (disbursementCardRef.value as any)?.$el as HTMLElement | undefined;
        el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 300);
    }
    if (newStatus === Pcm003Status.WaitingAccountingApproval) {
      currentTab.value = '0';
      await nextTick();
      setTimeout(() => {
        detailRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 300);
    }
  },
);

const onInitPageAsync = async (): Promise<void> => {
  onResetBody();

  if (routeId.value) {
    await pcm003Store.getByIdAsync(routeId.value.toString());

    if (!pcm003Store.detail.acceptors || pcm003Store.detail.acceptors.length === 0) {
      await pcm003Store.getMergedAcceptorsAsync(pcm003Store.detail.departmentCode, pcm003Store.detail.budget, pcm003Store.detail.supplyMethodCode, pcm003Store.detail.supplyMethodSpecialTypeCode);
    }

    if (pcm003Store.isEdit
      && !isDocBranchOrZone.value
      && !(pcm003Store.detail.acceptors ?? []).some(a => a.acceptorType === AcceptorType.AccountingApprover)) {
      await pcm003Store.getDefaultDisbursementAcceptor(pcm003Store.detail.budget);
    }

  }

  await nextTick();
  isInitialized.value = true;
};

const saveDocumentFirst = async (): Promise<void> => {
  if (currentTab.value === '1' && approvalDocumentRef.value && 'saveDocumentFirst' in approvalDocumentRef.value) {
    await approvalDocumentRef.value.saveDocumentFirst();
  } else if (currentTab.value === '2' && winnerDocumentRef.value && 'saveDocumentFirst' in winnerDocumentRef.value) {
    await winnerDocumentRef.value.saveDocumentFirst();
  }
};

const saveDocument = async () => {
  await saveDocumentFirst();
  await onSubmitAsync();
};

const handleRestoreApprovalVersion = async (): Promise<void> => {
  const pcm003Id = detail.value.id;
  if (!pcm003Id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await Pcm003Service.resetDocumentAsync(pcm003Id, 'Approval');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await pcm003Store.getByIdAsync(pcm003Id);
  }
};

const handleRestoreWinnerVersion = async (): Promise<void> => {
  const pcm003Id = detail.value.id;
  if (!pcm003Id) {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถรีเซ็ตเอกสารได้ ข้อมูลไม่ครบถ้วน');
    return;
  }
  const { status } = await Pcm003Service.resetDocumentAsync(pcm003Id, 'Winner');
  if (status === HttpStatusCode.Ok) {
    ToastHelper.success('สำเร็จ', 'ทำการรีเซ็ตเอกสารสำเร็จ');
    await pcm003Store.getByIdAsync(pcm003Id);
  }
};

const onSendApprovalWithDocSave = async () => {
  await saveDocumentFirst();

  detail.value.isApprovalRequestDocumentReplace = false;

  await onSendApprovalAsync();
};

const onRecallWithDocSave = async () => {
  await saveDocumentFirst();

  detail.value.isApprovalRequestDocumentReplace = false;

  await onRecallAsync();
};

const onSubmitAsync = async () => {
  // Save ChEditor content first before calling API
  await saveDocumentFirst();

  if (routeId.value) {

    const draftStatuses = [Pcm003Status.Draft, Pcm003Status.Edit, Pcm003Status.Rejected];

    if (isFormDirty.value
      && (detail.value.approvalRequestDocumentId || detail.value.winnerAnnounceDocumentId) && draftStatuses.includes(detail.value.status)) {
      const saveOption = await showSaveOptionDialogAsync();
      if (saveOption === undefined) return;
      detail.value.isApprovalRequestDocumentReplace = saveOption;
    }

    isInitialized.value = false;
    await updateAsync(routeId.value);
    await nextTick();
    isFormDirty.value = false;
    isInitialized.value = true;
    setCurrentTab();
    return;
  }

  isInitialized.value = false;
  await createAsync();
  isFormDirty.value = false;

  setCurrentTab();

  if (!pcm003Store.detail.acceptors || pcm003Store.detail.acceptors.length === 0) {
    await pcm003Store.getMergedAcceptorsAsync(pcm003Store.detail.departmentCode, pcm003Store.detail.budget, pcm003Store.detail.supplyMethodCode, pcm003Store.detail.supplyMethodSpecialTypeCode);
  }

  if (pcm003Store.isEdit
    && !isDocBranchOrZone.value
    && !(pcm003Store.detail.acceptors ?? []).some(a => a.acceptorType === AcceptorType.AccountingApprover)) {
    await pcm003Store.getDefaultDisbursementAcceptor(pcm003Store.detail.budget);
  }
  await nextTick();
  isInitialized.value = true;
};

const setCurrentTab = async () => {
  if (detail.value.approvalRequestDocumentId) {
    currentTab.value = '1';
  }
};

const canEditDocument = computed(() => {
  return detail.value.status === Pcm003Status.Edit
    || detail.value.status === Pcm003Status.Draft
    || detail.value.status === Pcm003Status.Rejected
});

const setDocumentReviewId = async (id: string): Promise<void> => {
  isInitialized.value = false;

  let documentType = '';

  if (currentTab.value === '1') {
    documentType = 'Approval';
  }

  if (documentType === 'Approval') {
    detail.value.approvalRequestDocumentId = id;
  }

  await nextTick();
  isInitialized.value = true;
};

const isCurrentUserApprove = computed(() => {
  const acceptors = pcm003Store.detail.acceptors ?? []

  const pendingSorted = acceptors
    .filter(a => a.status === AcceptorStatus.Pending && a.acceptorType === AcceptorType.Approver)
    .sort((a, b) => a.sequence - b.sequence)

  const firstPending = pendingSorted[0]

  return (firstPending?.delegateeUserId ? firstPending?.delegateeUserId : firstPending?.userId) === userStore.profile.id
});

// const isAccountingCanAssign = computed(() => {
//   const acceptors = pcm003Store.detail.acceptors?.filter(x =>
//     x.acceptorType === AcceptorType.AccountingApprover || x.acceptorType === AcceptorType.AccountingOperator
//   ) ?? []

//   return acceptors.some(a => a.userId === userStore.profile.id || a.delegateeUserId === userStore.profile.id);
// });
const isCurrentUserAccountingApprover = computed(() => {
  const acceptors = pcm003Store.detail.acceptors ?? []

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
  const current = pcm003Store.detail.acceptors?.filter(a =>
    a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
  ) || [];
  return JSON.stringify(current) === JSON.stringify(pcm003Store.cloneAccountingAcceptors);
});

const getStatusButton = (step: PreProcurementStep): string => {
  const status = pcm003Store.detail.status;

  const draftStatuses = [Pcm003Status.Draft, Pcm003Status.Edit, Pcm003Status.WaitingApproval, Pcm003Status.Rejected];
  const accountingYellowStatuses = [Pcm003Status.WaitingAccountingApproval, Pcm003Status.WaitingDisbursementDate];

  if (status === Pcm003Status.Paid) {
    return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
  }

  if (step === PreProcurementStep.P79Clause2) {
    if (current.value === PreProcurementStep.Accounting) {
      return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
    }

    if (draftStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
    }

    return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
  }

  if (step === PreProcurementStep.Accounting) {
    if (draftStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#9E9E9E] border-none rounded-none text-white';
    }

    if (current.value === PreProcurementStep.P79Clause2) {
      return 'w-full mb-5 bg-white border-[#F9A825] rounded-none text-[#F9A825]';
    }

    if (accountingYellowStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
    }
  }

  return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
};

const getDefaultStep = (status: Pcm003Status): PreProcurementStep => {
  const accountingStatuses = [
    Pcm003Status.WaitingAccountingApproval,
    Pcm003Status.WaitingDisbursementDate,
    Pcm003Status.Paid
  ];

  if (accountingStatuses.includes(status)) {
    return PreProcurementStep.Accounting;
  }

  return PreProcurementStep.P79Clause2;
};

const current = ref(pcm003Store.detail.currentStep || getDefaultStep(pcm003Store.detail.status));
const menuSelected = ref<PreProcurementStep>(getDefaultStep(pcm003Store.detail.status));

const availableSteps = computed(() => {
  const status = pcm003Store.detail.status;

  if (status === Pcm003Status.WaitingAccountingApproval ||
    status === Pcm003Status.WaitingDisbursementDate ||
    status === Pcm003Status.Paid) {
    return [PreProcurementStep.P79Clause2, PreProcurementStep.Accounting];
  }

  return [PreProcurementStep.P79Clause2];
});

const isButtonDisabled = (step: string): boolean => {
  return ![...availableSteps.value, menuSelected.value].includes(step as PreProcurementStep);
};

watch(() => pcm003Store.detail.currentStep, (val: PreProcurementStep): void => {
  if (val) {
    onChangeProgram(val);
  };
});

watch((): Pcm003Status => pcm003Store.detail.status, (newStatus: Pcm003Status): void => {
  if (newStatus) {
    const defaultStep = getDefaultStep(newStatus);
    current.value = defaultStep;
    menuSelected.value = defaultStep;

    if (defaultStep === PreProcurementStep.Accounting) {
      currentTab.value = '0';
    }
  }
}, { immediate: true });

watch(
  () => pcm003Store.detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

const onChangeProgram = (name: PreProcurementStep): void => {
  menuSelected.value = name;
  current.value = name;

  if (name === PreProcurementStep.Accounting) {
    currentTab.value = '0';
  }
};

const programMenu = ref<ProgramMenuType>({
  procurement: [
    { menu: 'รายการจัดซื้อจัดจ้าง กรณีเร่งด่วน', status: 'Waiting', name: PreProcurementStep.P79Clause2 },
  ],
  accounting: [
    { menu: 'บัญชีเบิกจ่าย', status: 'Waiting', name: PreProcurementStep.Accounting },
  ],
});

const showDisbursementCard = computed(() => {
  return pcm003Store.detail.status === Pcm003Status.WaitingDisbursementDate ||
    pcm003Store.detail.status === Pcm003Status.Paid;
});

const { deleteItemAndReSequence, reSequence } = ArrayHelper();

const isAccountingMember = computed<boolean>((): boolean =>
  [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => a.userId === userStore.profile.id)
);

const isAccountingSectionReadonly = computed<boolean>((): boolean => {
  if (!menuStore.hasManage) return true;

  if (pcm003Store.isEdit) return false;

  const hasApprovedAccountingAcceptor = [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => a.status === AcceptorStatus.Approved);
  if (hasApprovedAccountingAcceptor) return true;

  if ([Pcm003Status.WaitingDisbursementDate, Pcm003Status.Paid].includes(pcm003Store.detail.status)) return true;
  if (!pcm003Store.isAccountingMember) return true;

  const isBranchOrZone = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(pcm003Store.detail.departmentOrganizationLevel ?? '');

  const isInAccountingList = [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => (a.delegateeUserId ?? a.userId) === userStore.profile.id);

  if (!isBranchOrZone && !pcm003Store.isCurrentUserAccountingSegmentMember && !isInAccountingList) return true;

  return false;
});

const isConfirmerSectionReadonly = computed<boolean>((): boolean => {
  if (pcm003Store.detail.status === Pcm003Status.Paid) return true;

  const isInConfirmerList = (pcm003Store.detail.acceptanceConfirmers ?? [])
    .some((a): boolean => (a.delegateeUserId ?? a.userId) === userStore.profile.id);

  if (isInConfirmerList) return false;

  if (!menuStore.hasManage) return true;

  const isBranchOrZone = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(pcm003Store.detail.departmentOrganizationLevel ?? '');

  if (isBranchOrZone) {
    if (pcm003Store.detail.status === Pcm003Status.WaitingDisbursementDate) {
      return userStore.profile.departmentCode !== pcm003Store.detail.departmentCode;
    }
    return isAccountingSectionReadonly.value;
  }

  return !pcm003Store.isCurrentUserAccountingSegmentMember;
});

const isDocBranchOrZone = computed<boolean>((): boolean =>
  [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(pcm003Store.detail.departmentOrganizationLevel ?? ''));

const showAccountingOperatorSection = computed<boolean>((): boolean =>
  !isDocBranchOrZone.value
  && [Pcm003Status.WaitingAccountingApproval, Pcm003Status.WaitingDisbursementDate, Pcm003Status.Paid]
    .includes(pcm003Store.detail.status));

const showAccountingConfirmerSection = computed<boolean>((): boolean => {
  if (!pcm003Store.detail.id) return false;
  if (isDocBranchOrZone.value) return true;
  return [Pcm003Status.WaitingDisbursementDate, Pcm003Status.Paid].includes(pcm003Store.detail.status);
});

const isCurrentAccountingOperator = computed<boolean>((): boolean => {
  const firstPending = (pcm003Store.detail.acceptors ?? [])
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

const setAccountingListByType = (type: AcceptorType, newList: ParticipantsAcceptor[]): void => {
  const arr = pcm003Store.detail.acceptors ?? [];
  const others = arr.filter((a): boolean => a.acceptorType !== type);
  pcm003Store.detail.acceptors = [...others, ...newList];
};

const accountingOperators = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (pcm003Store.detail.acceptors ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.AccountingOperator),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.AccountingOperator, val),
});

const accountingApprovers = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (pcm003Store.detail.acceptors ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.AccountingApprover),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.AccountingApprover, val),
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

  if (!pcm003Store.detail.acceptors) {
    pcm003Store.detail.acceptors = [];
  }

  pcm003Store.detail.acceptors.push({
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

  if (routeId.value) {
    await pcm003Store.updateAsync(routeId.value.toString());
  }
};

const removeAccountingAcceptorAsync = async (type: AcceptorType, index: number): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, deleteItemAndReSequence(list, index));

  if (routeId.value) {
    await pcm003Store.updateAsync(routeId.value.toString());
  }
};

const saveConfirmersAsync = async (): Promise<void> => {
  await pcm003Store.saveAcceptorsAsync(route.params.id as string);
};

const onAccountingReSequenceDrag = async (type: AcceptorType): Promise<void> => {
  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, reSequence(list));

  if (routeId.value) {
    await pcm003Store.updateAsync(routeId.value.toString());
  }
};

const summaryGlAccountAmount = computed(() => (pcm003Store.detail.glAccounts?.reduce((a, b) => a + b.amount, 0) ?? 0));
</script>

<template>
  <Form @submit="onSubmitAsync" v-slot="{ handleSubmit }" @invalid-submit="() => ToastHelper.invalidMessageToast()">
    <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง กรณีเร่งด่วน" :route-items="routeItems" />
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
          <BadgeStatus :label="BadgeStatusColor(detail.status)?.label" :color="BadgeStatusColor(detail.status)?.color" />
        </div>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="primary" variant="outlined"
          class="bg-white hover:bg-yellow-50" @click="() => showActivityDialog(detail.id!)" />
      </template>
    </TitleHeader>
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="HeaderItem.filter((f, index) => detail.id ? f : index === 0)"
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
                    <Datepicker label="วันที่เบิกจ่าย" rules="required" v-model="pcm003Store.detail.disbursementDate"
                      :disabled="!pcm003Store.canConfirm || !menuStore.hasManage" />
                  </div>
                  <div class="grid lg:grid-cols-4 gap-2 mt-8">
                    <InputNumber label="จำนวนเงินเบิกจ่าย" v-model="pcm003Store.detail.disbursementAmount"
                      :disabled="!pcm003Store.canConfirm || !menuStore.hasManage"
                      :rules="`required|max_value:${summaryGlAccountAmount}`" inputClass="text-right"
                      :minFractionDigits="2" grouping />
                  </div>
                  <InputArea label="หมายเหตุ" rules="required" v-model="pcm003Store.detail.disbursementDescription"
                    :disabled="!pcm003Store.canConfirm || !menuStore.hasManage" class="mb-2 mt-12" />
                </template>
              </Card>
            </TabPanel>
            <TabPanel value="1">
              <Card>
                <template #content>
                  <ApprovalDocument v-model="detail.approvalRequestDocumentId" :readonly="!canEditDocument"
                    ref="approvalDocumentRef" :save="saveDocument" :versions="detail.approvalRequestDocumentVersions"
                    :canRestoreVersion="pcm003Store.isEdit && hasManage && canEditDocument"
                    @restore-version="handleRestoreApprovalVersion" />
                </template>
              </Card>
            </TabPanel>
            <TabPanel value="2">
              <Card>
                <template #content>
                  <WinnerDocument v-model="detail.winnerAnnounceDocumentId" :readonly="!canEditDocument"
                    ref="winnerDocumentRef" :save="saveDocument" :versions="detail.winnerAnnounceDocumentVersions"
                    :canRestoreVersion="pcm003Store.isEdit && hasManage && canEditDocument"
                    @restore-version="handleRestoreWinnerVersion" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
          <UploadFile />
        </Tabs>
      </div>
      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 lg:ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2" v-if="hasManage">
            <ButtonSave type="submit" v-if="pcm003Store.isEdit" />
            <ButtonSendApprove @click="handleSubmit(onSendApprovalWithDocSave)"
              v-if="pcm003Store.detail.id && pcm003Store.isEdit" />
            <ButtonRecall @click="onRecallWithDocSave()" v-if="pcm003Store.isCanRecall" />

            <template v-if="isCurrentUserApprove">
              <ButtonSendEdit @click="onRejectedAsync" v-if="pcm003Store.isApproveReject" />
              <ButtonApprove @click="onApprovedAsync"
                v-if="pcm003Store.isApproveReject && !pcm003Store.isLastApprover" />
              <ButtonConfirm @click="onApprovedAsync"
                v-if="pcm003Store.isApproveReject && pcm003Store.isLastApprover" />
            </template>
            <ButtonSave type="submit"
              v-if="pcm003Store.detail.status == Pcm003Status.WaitingAccountingApproval && isAccountingMember" />
            <template
              v-if="pcm003Store.detail.status === Pcm003Status.WaitingAccountingApproval && isCurrentUserAccountingApprover">
              <ButtonSendEdit @click="pcm003Store.onRejectedAsync"
                v-if="pcm003Store.isAccountingApproveReject && isAccountingAcceptorsUnchanged" />
              <Button label="ยืนยันตรวจสอบ" icon="pi pi-user-plus" severity="success"
                @click="pcm003Store.onApprovedAsync"
                v-if="pcm003Store.isAccountingApproveReject && isAccountingAcceptorsUnchanged && isCurrentAccountingOperator" />
              <ButtonApprove @click="pcm003Store.onApprovedAsync" v-if="pcm003Store.isAccountingApproveReject &&
                !pcm003Store.isLastAccountingApprover &&
                isAccountingAcceptorsUnchanged && !isCurrentAccountingOperator" />
              <ButtonConfirm @click="pcm003Store.onApprovedAsync" v-if="pcm003Store.isAccountingApproveReject &&
                pcm003Store.isLastAccountingApprover &&
                isAccountingAcceptorsUnchanged" />
            </template>
            <template v-if="pcm003Store.canConfirm">
              <ButtonSave type="submit" label="บันทึกชั่วคราว" />
              <ButtonConfirmAssign @click="handleSubmit(pcm003Store.onConfirmDisbursementAsync)"
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
                    <DocumentMapping pathToGet="p79Clause2" @on-click-select="
                      (text, hint) => currentTab == '1' ? approvalDocumentRef?.setPlaceholderInDocument(text, hint)
                        : winnerDocumentRef?.setPlaceholderInDocument(text, hint)
                    " />
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="pcm003Store.detail.acceptors"
                :acceptor-type="AcceptorType.Approver" is-approve v-if="pcm003Store.detail.acceptors" is-manage
                :is-disable="!pcm003Store.isEdit" :is-set-default="!!pcm003Store.detail.id"
                @set-default="() => pcm003Store.getMergedAcceptorsAsync(pcm003Store.detail.departmentCode, pcm003Store.detail.budget, pcm003Store.detail.supplyMethodCode, pcm003Store.detail.supplyMethodSpecialTypeCode)" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-5"
              v-if="pcm003Store.detail.id">
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
                        <Button v-if="(pcm003Store.isAccountingCanAssign || pcm003Store.isEdit) && !isDocBranchOrZone" label="กำหนดค่าเริ่มต้น"
                          severity="primary" variant="outlined" icon="pi pi-undo"
                          @click="() => pcm003Store.getDefaultDisbursementAcceptor(pcm003Store.detail.budget)" />
                      </template>
                    </AccountingAcceptorSection>
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-5"
              v-if="showAccountingConfirmerSection">
              <AccordAcceptor title="ส่วนบัญชีค่าใช้จ่าย (จบงาน)" v-model="pcm003Store.detail.acceptanceConfirmers"
                :acceptor-type="AcceptorType.AccountingConfirmer" is-approve v-if="pcm003Store.detail.acceptanceConfirmers"
                is-manage :is-disable="isConfirmerSectionReadonly"
                @add="saveConfirmersAsync"
                @remove="saveConfirmersAsync" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
  <DialogReviewDocument v-model="showReviewDocumentDialog" :docId="reviewDocumentId"
    :docName="`pcm003-${new Date().toISOString()}-${reviewDocumentId}`" @on-click-use-document="setDocumentReviewId" />
</template>