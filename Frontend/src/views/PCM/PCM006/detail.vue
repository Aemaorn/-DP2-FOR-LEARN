<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { AccordAcceptor } from '@/components/Accordions';
import { ButtonApprove, ButtonRecall, ButtonSave, ButtonSendApprove, ButtonSendEdit } from '@/components/Button';
import ButtonConfirm from '@/components/Button/ButtonConfirm.vue';
import ButtonConfirmAssign from '@/components/Button/ButtonConfirmAssign.vue';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import { Datepicker, InputArea, InputField, Select } from '@/components/forms';
import Pcm006Constant from '@/constants/pcm006';
import { ConfirmDialogType } from '@/enums/dialog';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { EPcm006Status } from '@/enums/pcm006';
import { OrganizationLevelEnum } from '@/enums/shared';
import { PreProcurementStep } from '@/enums/preProcurement';
import { ArrayHelper } from '@/helpers/array';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { showActivityDialog, showConfirmDialogAsync, showUserDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { ProgramMenuType } from '@/models/PCM/PCM005/pcm005';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import { useAuthenticationStore } from '@/stores/authentication';
import { useMenuStore } from '@/stores/menu';
import { usePcm006DetailStore } from '@/stores/PCM/pcm006';
import type { DataTableRowReorderEvent } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import PettyCashSelectDialog from './PettyCashSelectDialog.vue';

const AccountingAcceptorSection = defineAsyncComponent(() => import('@/views/CM/CM001/components/Period/AccountingAcceptorSection.vue'));

const { BadgeStatusColor } = Pcm006Constant;
const { reSequence, deleteItemAndReSequence } = ArrayHelper();

const routeItems = ref<Array<MenuItem>>([
  { label: 'รายการเบิกชดเชยเงินสดย่อย', url: '/pcm/pcm006' },
  { label: 'ข้อมูลการเบิกชดเชยเงินสดย่อย', },
]);

const route = useRoute();
const router = useRouter();

const id = computed(() => route.params.id);

const store = usePcm006DetailStore();
const userStore = useAuthenticationStore();
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();

const reOrderDataTable = (event: DataTableRowReorderEvent): void => {
  store.body.items = reSequence(event.value);
};

onMounted(async () => {
  await Promise.all([
    store.api.onGetDropdownAsync(),
  ]);

  if (id.value) {
    await store.api.getById(id.value.toString());
  } else {
    store.body.subject = 'ขออนุมัติเบิกชดเชยเงินสดย่อย ของฝ่าย';
    store.body.departmentId = authStore.profile.departmentCode;
    store.body.reimbursementDate = new Date();
  }

  if (!store.body.acceptors || store.body.acceptors.length === 0) {
    console.log(store.body.acceptors)
    store.api.getDefaultAcceptor()
  }

})

onUnmounted(() => {
  store.fn.onClearBody();
});

const onSubmit = async () => {
  if (id.value) {
    await store.api.onUpdate();

    return;
  }

  await store.api.onCreate();

  router.replace({ name: 'pcm006Detail', params: { id: store.body.id } });
}

const sendApprove = async () => {
  if (store.body.acceptors.length == 0) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  if (store.body.items.length == 0) {
    return ToastHelper.errorDescription("กรุณาเพิ่มรายละเอียดการเบิกจ่ายอย่างน้อย 1 รายการ")
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApprove))) return;

  if (!store.body.acceptors?.find(x => x.acceptorType === AcceptorType.AccountingApprover)) {
    const budget = calculateTotalPriceVat();
    await store.api.getDefaultDisbursementAcceptor(budget)
  }

  if (id.value) {
    await store.api.onUpdate(EPcm006Status.WaitingApproval);
    return;
  }

  store.api.onCreate();

  router.replace({ name: 'pcm006Detail', params: { id: store.body.id } });
};

const calculateTotalPriceVat = (): number => {
  return store.body.items.reduce((acc, item) => {
    return acc + (item.amount || 0);
  }, 0);
};

const onConfirmDisbursementAsync = async () => {

  if (!await showConfirmDialogAsync(ConfirmDialogType.ConfirmData)) return;

  if (id.value) {
    await store.api.onUpdate(EPcm006Status.Paid);
    return;
  }

  store.api.onCreate();

  router.replace({ name: 'pcm006Detail', params: { id: store.body.id } });
};

const onRecallAsync = async () => {
  if (!store.body.id || !await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  await store.api.onUpdate(EPcm006Status.Edit);
};

const removeItems = (index: number) => {
  store.body.items = deleteItemAndReSequence(store.body.items, index);
};

const getStatusButton = (step: PreProcurementStep): string => {
  const status = store.body.status;

  const draftStatuses = [EPcm006Status.Draft, EPcm006Status.Edit, EPcm006Status.WaitingApproval, EPcm006Status.Rejected];
  const accountingYellowStatuses = [EPcm006Status.WaitingAccountingApproval, EPcm006Status.WaitingDisbursementDate];

  if (status === EPcm006Status.Paid) {
    return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
  }

  if (step === PreProcurementStep.PettyCashReimbursement) {
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

    if (current.value === PreProcurementStep.PettyCashReimbursement) {
      return 'w-full mb-5 bg-white border-[#F9A825] rounded-none text-[#F9A825]';
    }

    if (accountingYellowStatuses.includes(status)) {
      return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
    }
  }

  return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
};

const getDefaultStep = (status: EPcm006Status): PreProcurementStep => {
  const accountingStatuses = [
    EPcm006Status.WaitingAccountingApproval,
    EPcm006Status.WaitingDisbursementDate,
    EPcm006Status.Paid
  ];

  if (accountingStatuses.includes(status)) {
    return PreProcurementStep.Accounting;
  }

  return PreProcurementStep.PettyCashReimbursement;
};

const current = ref(store.body.currentStep || getDefaultStep(store.body.status));
const menuSelected = ref<PreProcurementStep>(getDefaultStep(store.body.status));

const availableSteps = computed(() => {
  const status = store.body.status;

  if (status === EPcm006Status.WaitingAccountingApproval ||
    status === EPcm006Status.WaitingDisbursementDate ||
    status === EPcm006Status.Paid) {
    return [PreProcurementStep.PettyCashReimbursement, PreProcurementStep.Accounting];
  }

  return [PreProcurementStep.PettyCashReimbursement];
});

const isButtonDisabled = (step: string): boolean => {
  return ![...availableSteps.value, menuSelected.value].includes(step as PreProcurementStep);
};

watch(() => store.body.currentStep, (val: PreProcurementStep): void => {
  if (val) {
    onChangeProgram(val);
  };
});

watch((): EPcm006Status => store.body.status, async (newStatus: EPcm006Status): Promise<void> => {
  if (newStatus) {
    const defaultStep = getDefaultStep(newStatus);
    current.value = defaultStep;
    menuSelected.value = defaultStep;

    if (newStatus === EPcm006Status.WaitingDisbursementDate) {
      await nextTick();
      setTimeout(() => {
        const el = (disbursementCardRef.value as any)?.$el as HTMLElement | undefined;
        el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 300);
    }
    if (newStatus === EPcm006Status.WaitingAccountingApproval) {
      await nextTick();
      setTimeout(() => {
        const el = (disbursementDetailRef.value as any)?.$el as HTMLElement | undefined;
        el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 300);
    }
  }
}, { immediate: true });

const onChangeProgram = (name: PreProcurementStep): void => {
  menuSelected.value = name;
  current.value = name;

};

const programMenu = ref<ProgramMenuType>({
  procurement: [
    { menu: 'รายการจัดซื้อจัดจ้าง กรณีเร่งด่วน', status: 'Waiting', name: PreProcurementStep.PettyCashReimbursement },
  ],
  accounting: [
    { menu: 'บัญชีเบิกจ่าย', status: 'Waiting', name: PreProcurementStep.Accounting },
  ],
});

const isCurrentUserAccountingApprover = computed(() => {
  const acceptors = store.body.acceptors ?? []

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
  const current = store.body.acceptors?.filter(a =>
    a.acceptorType === AcceptorType.AccountingApprover || a.acceptorType === AcceptorType.AccountingOperator
  ) || [];
  return JSON.stringify(current) === JSON.stringify(store.cloneAccountingAcceptors);
});

const disbursementCardRef = ref<HTMLElement | null>(null);
const disbursementDetailRef = ref<HTMLElement | null>(null);

const pettyCashSelectDialogRef = ref<InstanceType<typeof PettyCashSelectDialog> | null>(null);

const showDisbursementCard = computed(() => {
  return store.body.status === EPcm006Status.WaitingDisbursementDate ||
    store.body.status === EPcm006Status.Paid;
});

const totalAmount = computed(() => {
  return store.body.items.reduce((sum, item) => sum + (item.amount || 0), 0);
});

const isAccountingSectionReadonly = computed<boolean>((): boolean => {
  if ([EPcm006Status.WaitingDisbursementDate, EPcm006Status.Paid].includes(store.body.status)) return true;
  if (!store.state.isAccountingMember) return true;
  if (!menuStore.hasManage) return true;
  const isBranchOrZone = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(store.body.departmentOrganizationLevel ?? '');
  const isInAccountingList = [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => (a.delegateeUserId ?? a.userId) === userStore.profile.id);
  if (!isBranchOrZone && !store.state.isCurrentUserAccountingSegmentMember && !isInAccountingList) return true;
  return false;
});

const isConfirmerSectionReadonly = computed<boolean>((): boolean => {
  if (store.body.status === EPcm006Status.Paid) return true;

  const isInConfirmerList = (store.body.acceptanceConfirmers ?? [])
    .some((a): boolean => (a.delegateeUserId ?? a.userId) === userStore.profile.id);

  if (isInConfirmerList) return false;

  if (!menuStore.hasManage) return true;
  const isBranchOrZone = [String(OrganizationLevelEnum.Branch), String(OrganizationLevelEnum.Zone), String(OrganizationLevelEnum.Segment)]
    .includes(store.body.departmentOrganizationLevel ?? '');
  if (isBranchOrZone) {
    return userStore.profile.departmentCode !== store.body.departmentId;
  }
  return !store.state.isCurrentUserAccountingSegmentMember;
});

const saveConfirmersAsync = async (): Promise<void> => {
  await store.api.saveAcceptorsAsync();
};

const setAccountingListByType = (type: AcceptorType, newList: ParticipantsAcceptor[]): void => {
  const arr = store.body.acceptors ?? [];
  const others = arr.filter((a): boolean => a.acceptorType !== type);
  store.body.acceptors = [...others, ...newList];
};

const isAccountingMember = computed<boolean>((): boolean =>
  [...accountingOperators.value, ...accountingApprovers.value]
    .some((a): boolean => a.userId === userStore.profile.id)
);

const accountingOperators = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (store.body.acceptors ?? [])
    .filter((a): boolean => a.acceptorType === AcceptorType.AccountingOperator),
  set: (val: ParticipantsAcceptor[]): void => setAccountingListByType(AcceptorType.AccountingOperator, val),
});

const accountingApprovers = computed<ParticipantsAcceptor[]>({
  get: (): ParticipantsAcceptor[] => (store.body.acceptors ?? [])
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

  if (!store.body.acceptors) {
    store.body.acceptors = [];
  }

  store.body.acceptors.push({
    userId: res.id,
    acceptorType: type,
    departmentName: res.departmentName ?? '',
    fullName: res.name,
    positionName: res.positionName ?? '',
    sequence: current.length + 1,
    status: store.body.status === EPcm006Status.WaitingAccountingApproval
      ? AcceptorStatus.Pending
      : AcceptorStatus.Draft,
    organizationLevel: res.organizationLevel,
  } as ParticipantsAcceptor);

  const updatedList = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];
  setAccountingListByType(type, reSequence(updatedList));

  if (id.value) {
    await store.api.onUpdate();
  }
};

const removeAccountingAcceptorAsync = async (type: AcceptorType, index: number): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, deleteItemAndReSequence(list, index));

  if (id.value) {
    await store.api.onUpdate();
  }
};

const onAccountingReSequenceDrag = async (type: AcceptorType): Promise<void> => {
  const list = type === AcceptorType.AccountingOperator
    ? [...accountingOperators.value]
    : [...accountingApprovers.value];

  setAccountingListByType(type, reSequence(list));

  if (id.value) {
    await store.api.onUpdate();
  }
};
</script>

<template>
  <TitleHeader label="ข้อมูลการเบิกชดเชยเงินสดย่อย" :route-items="routeItems" />
  <Form @submit="onSubmit()" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
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
            การเบิกชดเชยเงินสดย่อย
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
          <BadgeStatus :label="BadgeStatusColor(store.body.status).label"
            :color="BadgeStatusColor(store.body.status).color" />
        </div>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
          class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
      </template>
    </TitleHeader>
    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2">
        <Card class="mt-4">
          <template #content>
            <div class="grid grid-cols-1 md:grid-cols-4 gap-4 gap-y-8 mt-8">
              <InputField label="เลขที่" v-model="store.body.number" disabled />
              <Datepicker label="วันที่" rules="required" v-model="store.body.reimbursementDate"
                :disabled="!menuStore.hasManage || !store.state.canEdit" />
              <Select label="ฝ่าย/ภาคเขต" rules="required" class="md:col-start-1" :options="store.departmentDropDown"
                v-model="store.body.departmentId"
                :disabled="!!store.body.id || !menuStore.hasManage || !store.state.canEdit" />
              <InputArea label="เรื่อง" v-model="store.body.subject" rules="required"
                class="md:col-start-1 md:col-span-4" :disabled="!menuStore.hasManage || !store.state.canEdit" />
              <InputArea label="วัตถุประสงค์" v-model="store.body.description" class="md:col-start-1 md:col-span-4"
                :disabled="!menuStore.hasManage || !store.state.canEdit" />
            </div>
          </template>
        </Card>

        <Card class="mt-4" ref="disbursementDetailRef">
          <template #content>
            <TitleHeader label="รายละเอียดการเบิกจ่าย" />
            <div class="flex justify-end gap-2" v-if="menuStore.hasManage && store.state.canEdit">
              <Button label="เลือกรายการข้อมูลเบิกจ่าย" icon="pi pi-list-check" severity="primary" variant="outlined"
                @click="pettyCashSelectDialogRef?.open()" />
              <Button label="ดึงรายการข้อมูลเบิกจ่ายทั้งหมด" icon="pi pi-file-import" severity="primary" variant="outlined"
                @click="store.api.onPettyCashData" />
            </div>
            <DataTable :value="store.body.items" data-key="id" @row-reorder="(e) => reOrderDataTable(e)">
              <Column field="sequence" bodyStyle="vertical-align: center">
                <template #header>
                  <p class="w-full font-bold text-center">ลำดับ</p>
                </template>
                <template #body="{ data }">
                  <p class="text-center mt-4">
                    {{ data.sequence }}
                  </p>
                </template>
              </Column>
              <Column field="solId">
                <template #header>
                  <p class="w-full font-bold text-center">วันที่</p>
                </template>
                <template #body="{ data }">
                  <p class="text-center mt-4">
                    {{ ToDateOnly(data.pettyCashDate) }}
                  </p>
                </template>
              </Column>
              <Column field="budgetTypeCode">
                <template #header>
                  <p class="w-full font-bold text-center">เลขที่อ้างอิง</p>
                </template>
                <template #body="{ data }">
                  <p class="text-center mt-4">
                    {{ data.pettyCashNumber }}
                  </p>
                </template>
              </Column>
              <Column field="projectNumber">
                <template #header>
                  <p class="w-full font-bold text-center">รายการ</p>
                </template>
                <template #body="{ data }">
                  <p class="text-start mt-4">
                    {{ data.pettyCashSubject }}
                  </p>
                </template>
              </Column>
              <Column field="glAccountCode">
                <template #header>
                  <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
                </template>
                <template #body="{ data }">
                  <p class="text-start mt-4">
                    {{ data.departmentName }}
                  </p>
                </template>
              </Column>
              <Column field="amount">
                <template #header>
                  <p class="w-full font-bold text-center">รหัสบัญชี</p>
                </template>
                <template #body="{ data }">
                  <p class="text-start mt-4">
                    {{ data.glAccountLabel }}
                  </p>
                </template>
              </Column>
              <Column field="amount">
                <template #header>
                  <p class="w-full font-bold text-center">จำนวนเงิน</p>
                </template>
                <template #body="{ data }">
                  <p class="text-center mt-4">
                    {{ formatCurrency(data.amount) }}
                  </p>
                </template>
              </Column>
              <Column>
                <template #body="{ index }">
                  <div class="text-center" v-if="menuStore.hasManage && store.state.canEdit">
                    <Button icon="pi pi-trash"
                      class="text-red-600! text-center hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                      variant="text" @click="() => removeItems(index)" />
                  </div>
                </template>
              </Column>
              <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false">
                <template #rowreordericon>
                  <div class="flex justify-center items-center">
                    <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                      v-if="menuStore.hasManage && store.state.canEdit">
                      drag_indicator
                    </span>
                  </div>
                </template>
              </Column>
              <template #empty>
                <p class="text-center">ไม่พบข้อมูล</p>
              </template>
            </DataTable>
            <div class="flex flex-col w-full items-end mt-8">
              <div class="flex items-center gap-4 text-2xl font-bold mb-2">
                <span class="text-right">รวมจำนวนเงินทั้งสิ้น</span>
                <span class="min-w-[150px] text-right">{{ formatCurrency(totalAmount) }}</span>
              </div>
            </div>
          </template>
        </Card>

        <Card class="mt-4">
          <template #content>
            <TitleHeader label="การรับเงิน" />
            <div class="grid grid-cols-4 gap-4 mt-8">
              <InputField label="ชื่อบัญชี" v-model="store.body.bankAccountName"
                :disabled="!menuStore.hasManage || !store.state.canEdit" rules="required" />
              <InputField label="หมายเลขบัญชี" v-model="store.body.bankAccountNumber"
                :disabled="!menuStore.hasManage || !store.state.canEdit" rules="required" />
            </div>
          </template>
        </Card>
        <Card class="mt-4" v-if="current === PreProcurementStep.Accounting && showDisbursementCard"
          ref="disbursementCardRef">
          <template #content>
            <TitleHeader label="ข้อมูลการเบิกจ่าย" />
            <div class="grid lg:grid-cols-4 gap-2 mt-10">
              <Datepicker label="วันที่เบิกจ่าย" rules="required" v-model="store.body.disbursementDate"
                :disabled="!store.state.canConfirm || !menuStore.hasManage" />
            </div>
            <div class="grid lg:grid-cols-4 gap-2 mt-8">
              <InputNumber label="จำนวนเงินเบิกจ่าย" v-model="store.body.disbursementAmount"
                :disabled="!store.state.canConfirm || !menuStore.hasManage" rules="required" hide-details
                inputClass="text-right" :minFractionDigits="2" grouping />
            </div>
            <InputArea label="หมายเหตุ" rules="required" v-model="store.body.disbursementDescription"
              :disabled="!store.state.canConfirm || !menuStore.hasManage" class="mb-2 mt-12" />
          </template>
        </Card>
        <div class="mt-4">
          <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments" @upload="store.api.onUpsertAttachments"
            @remove-file="store.api.onUpsertAttachments" @remove-group="store.api.onUpsertAttachments"
            @reorder="store.api.onUpsertAttachments" :disabled="!menuStore.hasManage" />
        </div>
      </div>
      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2" v-if="menuStore.hasManage">
            <ButtonSave type="submit" v-if="menuStore.hasManage && store.state.canEdit" />
            <ButtonSendApprove @click="() => handleSubmit(() => sendApprove())"
              v-if="menuStore.hasManage && store.state.canEdit && store.body.id" />
            <ButtonRecall v-if="menuStore.hasManage && store.state.canReCall" @click="onRecallAsync" />
            <ButtonSendEdit v-if="store.state.canAcceptAndRejectApprover" @click="store.api.onRejectAsync" />
            <ButtonConfirm v-if="store.state.canAcceptAndRejectApprover && store.state.isLastApprovalApprover"
              @click="store.api.onApproveAsync" />
            <ButtonApprove v-if="store.state.canAcceptAndRejectApprover && !store.state.isLastApprovalApprover"
              @click="store.api.onApproveAsync" />
            <ButtonSave type="submit"
              v-if="store.body.status == EPcm006Status.WaitingAccountingApproval && isAccountingMember" />
            <template
              v-if="store.body.status === EPcm006Status.WaitingAccountingApproval && isCurrentUserAccountingApprover">
              <ButtonSendEdit @click="store.api.onRejectAsync"
                v-if="store.state.isAccountingApproveReject && isAccountingAcceptorsUnchanged" />
              <ButtonApprove @click="store.api.onApproveAsync" v-if="store.state.isAccountingApproveReject &&
                !store.state.isLastAccountingApprover &&
                isAccountingAcceptorsUnchanged" />
              <ButtonConfirm @click="store.api.onApproveAsync" v-if="store.state.isAccountingApproveReject &&
                store.state.isLastAccountingApprover &&
                isAccountingAcceptorsUnchanged" />
            </template>
            <template v-if="store.state.canConfirm">
              <ButtonSave type="submit" label="บันทึกชั่วคราว" />
              <ButtonConfirmAssign @click="handleSubmit(onConfirmDisbursementAsync)"
                label="ยืนยันเบิกจ่าย" />
            </template>
          </div>
          <Accordion :value="['0', '1', '2']" unstyled multiple>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.body.acceptors"
                :acceptor-type="AcceptorType.Approver" isManage
                :is-disable="!store.state.canEdit || !menuStore.hasManage" isApprove :is-set-default="true"
                @set-default="() => store.api.getDefaultAcceptor()" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-5"
              v-if="[EPcm006Status.WaitingAccountingApproval, EPcm006Status.WaitingDisbursementDate, EPcm006Status.Paid].includes(store.body.status)">
              <AccordHeader label="บัญชีเห็นชอบ/อนุมัติ" />
              <AccordionContent>
                <Card class="rounded-none">
                  <template #content>
                    <div class="mb-6">
                      <AccountingAcceptorSection title="ส่วนบัญชีค่าใช้จ่าย"
                        :acceptor-type="AcceptorType.AccountingOperator"
                        v-model="accountingOperators"
                        :readonly="isAccountingSectionReadonly"
                        :current-user-id="userStore.profile.id"
                        drag-handle-class="drag-acc-operator"
                        group-name="accountingOperatorGroup"
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
                        <Button v-if="store.state.isAccountingCanAssign" label="กำหนดค่าเริ่มต้น"
                          severity="primary" variant="outlined" icon="pi pi-undo"
                          @click="() => store.api.getDefaultDisbursementAcceptor(calculateTotalPriceVat())" />
                      </template>
                    </AccountingAcceptorSection>
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-5"
              v-if="[EPcm006Status.WaitingDisbursementDate, EPcm006Status.Paid].includes(store.body.status)">
              <AccordAcceptor title="ส่วนบัญชีค่าใช้จ่าย (จบงาน)" v-model="store.body.acceptanceConfirmers"
                :acceptor-type="AcceptorType.AccountingConfirmer" is-approve v-if="store.body.acceptanceConfirmers" is-manage
                :is-disable="isConfirmerSectionReadonly"
                @add="saveConfirmersAsync"
                @remove="saveConfirmersAsync" />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>

  <PettyCashSelectDialog ref="pettyCashSelectDialogRef" />
</template>
