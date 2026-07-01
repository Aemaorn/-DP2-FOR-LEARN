<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { AccordAcceptor } from '@/components/Accordions';
import { ButtonApprove, ButtonRecall, ButtonSave, ButtonSendApprove, ButtonSendEdit } from '@/components/Button';
import ButtonConfirmAssign from '@/components/Button/ButtonConfirmAssign.vue';
import { TitleHeader } from '@/components/cosmetic';
import InputArea from '@/components/forms/InputArea.vue';
import InputNumber from '@/components/forms/InputNumber.vue';
import Datepicker from '@/components/forms/Datepicker.vue';
import Pcm007Constant from '@/constants/pcm007';
import { AcceptorStatus, AcceptorType } from '@/enums/participants';
import { Pcm007Action, Pcm007CommitteeType, Pcm007Status } from '@/enums/pcm007';
import { showActivityDialog, showConfirmDialogAsync, showReasonDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import { usePcm007DetailStore } from '@/stores/PCM/pcm007';
import { useAuthenticationStore } from '@/stores/authentication';
import { useMenuStore } from '@/stores/menu';
import { TabPanel, TabPanels } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, nextTick, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import UploadFile from './components/UploadFile.vue';
import { ConfirmDialogType, ReasonDialogType } from '@/enums/dialog';

const Detail = defineAsyncComponent(() => import('./components/Detail.vue'));

const currentTab = ref('0');
const isFormDirty = ref(false);
const isInitialized = ref(false);

const menuStore = useMenuStore();
const pcm007Store = usePcm007DetailStore();
const authenStore = useAuthenticationStore();
const route = useRoute();
const router = useRouter();
const { BadgeStatusColor } = Pcm007Constant;

const id = ref(route.params?.id);

const HeaderItem = ref([
  { label: 'รายละเอียด', value: '0' },
] as Option[]);

const routeItems = ref([
  { label: 'รายการจัดซื้อจัดจ้าง ว 804', url: '/pcm/pcm007' },
  { label: 'ข้อมูลจัดซื้อจัดจ้าง' },
] as MenuItem[]);

onMounted(async () => {
  await onInitPageAsync();

  pcm007Store.detail.departmentCode = authenStore.profile.departmentCode;
  await Promise.all([
    pcm007Store.getDepartmentDDLAsync(),
    pcm007Store.getSupplyMethodDDLAsync(),
    pcm007Store.getPaymentMethodDDLAsync(),
    pcm007Store.getBankDDLAsync(),
    pcm007Store.getVatTypeDDLAsync(),
    pcm007Store.getUnitOfMeasureDDLAsync(),
    pcm007Store.getSolIdDDLAsync(),
    pcm007Store.getBudgetTypeDDLAsync(),
    pcm007Store.getGlAccountDDLAsync(),
    pcm007Store.getBillTypeDDLAsync(),
    pcm007Store.getPositionProcOptions(),
    pcm007Store.getPositionInspOptions(),
  ]);

  await nextTick();
  isInitialized.value = true;
});

watch(
  () => pcm007Store.detail,
  () => {
    if (isInitialized.value) {
      isFormDirty.value = true;
    }
  },
  { deep: true }
);

watch(() => pcm007Store.detail.supplyMethodCode, async (newValue) => {
  if (newValue) {
    await pcm007Store.getSupplyMethodSpecialTypeDDLAsync(newValue);
  }
});

const onInitPageAsync = async (): Promise<void> => {
  if (id.value) {
    await pcm007Store.getByIdAsync(id.value.toString());
  }
};

const onSubmit = async (): Promise<void> => {
  if (id.value) {
    isInitialized.value = false;
    await pcm007Store.updateAsync(id.value.toString());
    isFormDirty.value = false;
    await nextTick();
    isInitialized.value = true;
    return;
  }

  isInitialized.value = false;
  const newId = await pcm007Store.createAsync();
  isFormDirty.value = false;

  if (newId) {
    id.value = newId;
    router.replace({ name: 'pcm007Detail', params: { id: newId } });
  }
  await nextTick();
  isInitialized.value = true;
};

const onSendApproveAsync = async (): Promise<void> => {
  if (!pcm007Store.detail.acceptors?.some(x => x.acceptorType === AcceptorType.Approver)) {
    return ToastHelper.errorDescription('กรุณาเพิ่มผู้มีอำนาจเห็นชอบ/อนุมัติ อย่างน้อย 1 คน');
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.SendApprove))) return;

  if (pcm007Store.detail.id) {
    await pcm007Store.updateAsync(
      pcm007Store.detail.id,
      Pcm007Status.WaitingApproval,
      'ส่งขออนุมัติ',
      'ส่งขออนุมัติสำเร็จ',
    );
  }
};

const onRecallAsync = async (): Promise<void> => {
  if (!(await showConfirmDialogAsync(ConfirmDialogType.Edit))) return;

  if (pcm007Store.detail.id) {
    await pcm007Store.updateAsync(
      pcm007Store.detail.id,
      Pcm007Status.Edit,
      'เรียกคืนแก้ไข',
      'เรียกคืนแก้ไขสำเร็จ',
    );
  }
};

const onApproveAcceptorAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

  if (res.isConfirm && pcm007Store.detail.id) {
    await pcm007Store.actionAsync(
      pcm007Store.detail.id,
      { action: Pcm007Action.ApproveAcceptor, remark: res.reason },
      'เห็นชอบ/อนุมัติ',
      'เห็นชอบ/อนุมัติสำเร็จ',
    );
  }
};

const onRejectAcceptorAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.NotAgree, true);

  if (res.isConfirm && pcm007Store.detail.id) {
    await pcm007Store.actionAsync(
      pcm007Store.detail.id,
      { action: Pcm007Action.RejectAcceptor, remark: res.reason },
      'ส่งกลับแก้ไข',
      'ส่งกลับแก้ไขสำเร็จ',
    );
  }
};

const onCommitteeApproveAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

  if (res.isConfirm && pcm007Store.detail.id) {
    await pcm007Store.actionAsync(
      pcm007Store.detail.id,
      { action: Pcm007Action.CommitteeApprove, remark: res.reason },
      'ตรวจรับ',
      'ตรวจรับสำเร็จ',
    );
  }
};

const onCommitteeRejectAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.NotAgree, true);

  if (res.isConfirm && pcm007Store.detail.id) {
    await pcm007Store.actionAsync(
      pcm007Store.detail.id,
      { action: Pcm007Action.CommitteeReject, remark: res.reason },
      'ส่งกลับแก้ไข',
      'ส่งกลับแก้ไขสำเร็จ',
    );
  }
};

const onAccountingApproveAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.Accepted);

  if (res.isConfirm && pcm007Store.detail.id) {
    await pcm007Store.actionAsync(
      pcm007Store.detail.id,
      { action: Pcm007Action.AccountingApprove, remark: res.reason },
      'บัญชีเห็นชอบ',
      'บัญชีเห็นชอบสำเร็จ',
    );
  }
};

const onAccountingRejectAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.NotAgree, true);

  if (res.isConfirm && pcm007Store.detail.id) {
    await pcm007Store.actionAsync(
      pcm007Store.detail.id,
      { action: Pcm007Action.AccountingReject, remark: res.reason },
      'ส่งกลับแก้ไข',
      'ส่งกลับแก้ไขสำเร็จ',
    );
  }
};

const onConfirmDisbursementAsync = async (): Promise<void> => {
  if (!pcm007Store.detail.disbursementDate) {
    return ToastHelper.errorDescription('กรุณาระบุวันที่เบิกจ่าย');
  }

  if (!(await showConfirmDialogAsync(ConfirmDialogType.ConfirmChange))) return;

  if (pcm007Store.detail.id) {
    await pcm007Store.updateAsync(
      pcm007Store.detail.id,
      Pcm007Status.Paid,
      'ยืนยันเบิกจ่าย',
      'ยืนยันเบิกจ่ายสำเร็จ',
    );
  }
};

const showDisbursementCard = computed(() =>
  pcm007Store.detail.status === Pcm007Status.WaitingDisbursementDate ||
  pcm007Store.detail.status === Pcm007Status.Paid
);

const showCommitteeAcceptor = computed(() =>
  [Pcm007Status.WaitingCommitteeApprove, Pcm007Status.WaitingAccounting, Pcm007Status.WaitingDisbursementDate, Pcm007Status.Paid]
    .includes(pcm007Store.detail.status)
);

const committeeStatusIcon = (sequence: number): 'approved' | 'pending' | 'draft' => {
  const status = pcm007Store.detail.status;
  const currentSeq = pcm007Store.detail.currentCommitteeSequence;

  // All done (past committee stage)
  if ([Pcm007Status.WaitingAccounting, Pcm007Status.WaitingDisbursementDate, Pcm007Status.Paid].includes(status)) {
    return 'approved';
  }
  if (sequence < currentSeq) return 'approved';
  if (sequence === currentSeq) return 'pending';
  return 'draft';
};

const inspectionCommittees = computed(() =>
  (pcm007Store.detail.committees ?? [])
    .filter(c => c.groupType === Pcm007CommitteeType.InspectionCommittee)
    .sort((a, b) => a.sequence - b.sequence)
);

const showAccountingAcceptor = computed(() =>
  [Pcm007Status.WaitingAccounting, Pcm007Status.WaitingDisbursementDate, Pcm007Status.Paid]
    .includes(pcm007Store.detail.status)
);

const canAccountingSave = computed(() => {
  const accountingAcceptors = pcm007Store.detail.acceptors?.filter(
    a => a.acceptorType === AcceptorType.AccountingApprover,
  ) ?? [];
  return accountingAcceptors.every(a => a.status === AcceptorStatus.Pending);
});

onUnmounted(() => {
  pcm007Store.onResetDetail();
});
</script>

<template>
  <Form @submit="onSubmit()" v-slot="{ handleSubmit }" @invalid-submit="() => ToastHelper.invalidMessageToast()">
    <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง ว 804" :route-items="routeItems">
      <template #action>
        <div class="flex items-center gap-2">
          <p class="text-sm">สถานะ :</p>
          <BadgeStatus :label="BadgeStatusColor(pcm007Store.detail.status).label"
            :color="BadgeStatusColor(pcm007Store.detail.status).color" />
        </div>
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" severity="primary" variant="outlined"
          v-if="pcm007Store.detail.id" class="bg-white! hover:bg-red-50!"
          @click="() => showActivityDialog(pcm007Store.detail.id!)" />
      </template>
    </TitleHeader>

    <div class="grid lg:grid-cols-7 gap-2">
      <div class="lg:col-span-5 order-2 lg:order-1">
        <Tabs :value="currentTab" unstyled @update:value="(tab) => currentTab = tab.toString()">
          <TabHeader :items="HeaderItem" class="sticky top-[58px] z-99 bg-[#F7F7F7]" />
          <TabPanels>
            <TabPanel value="0">
              <Detail />
              <Card class="mt-4" v-if="showDisbursementCard">
                <template #content>
                  <TitleHeader label="ข้อมูลการเบิกจ่าย" />
                  <div class="grid lg:grid-cols-4 gap-2 mt-10">
                    <Datepicker label="วันที่เบิกจ่าย" rules="required" v-model="pcm007Store.detail.disbursementDate"
                      :disabled="!pcm007Store.canConfirmDisbursement || !menuStore.hasManage" />
                  </div>
                  <div class="grid lg:grid-cols-4 gap-2 mt-8">
                    <InputNumber label="จำนวนเงินเบิกจ่าย" v-model="pcm007Store.detail.disbursementAmount"
                      :disabled="!pcm007Store.canConfirmDisbursement || !menuStore.hasManage" rules="required"
                      inputClass="text-right" :minFractionDigits="2" grouping />
                  </div>
                  <InputArea label="หมายเหตุ" v-model="pcm007Store.detail.disbursementDescription"
                    :disabled="!pcm007Store.canConfirmDisbursement || !menuStore.hasManage" class="mb-2 mt-12" />
                </template>
              </Card>
            </TabPanel>
          </TabPanels>
          <UploadFile v-if="pcm007Store.detail.id" />
        </Tabs>
      </div>

      <div class="relative lg:col-span-2 order-1 lg:order-2">
        <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
          <div class="flex items-center justify-end gap-2 flex-wrap" v-if="menuStore.hasManage">
            <template v-if="pcm007Store.isEdit">
              <ButtonSave type="submit" />
              <ButtonSendApprove @click="handleSubmit(onSendApproveAsync)" v-if="pcm007Store.detail.id" />
            </template>
            <ButtonRecall @click="onRecallAsync" v-if="pcm007Store.canRecall" />

            <template v-if="pcm007Store.isWaitingApproval">
              <ButtonSendEdit @click="onRejectAcceptorAsync" />
              <ButtonApprove @click="onApproveAcceptorAsync" v-if="!pcm007Store.isLastApprover" />
              <ButtonApprove label="อนุมัติ" @click="onApproveAcceptorAsync" v-if="pcm007Store.isLastApprover" />
            </template>

            <template v-if="pcm007Store.isCommitteeApprove">
              <ButtonSendEdit label="ไม่เห็นชอบ" @click="onCommitteeRejectAsync" />
              <ButtonApprove label="ตรวจรับ" @click="onCommitteeApproveAsync" />
            </template>

            <template v-if="pcm007Store.isAccounting">
              <ButtonSave type="submit" label="บันทึก" v-if="canAccountingSave" />
              <ButtonSendEdit label="ส่งกลับแก้ไข" @click="onAccountingRejectAsync" />
              <ButtonApprove label="เห็นชอบ" @click="onAccountingApproveAsync"
                v-if="!pcm007Store.isLastAccountingApprover" />
              <ButtonApprove label="เห็นชอบ" @click="onAccountingApproveAsync"
                v-if="pcm007Store.isLastAccountingApprover" />
            </template>

            <template v-if="pcm007Store.canConfirmDisbursement">
              <ButtonSave type="submit" label="บันทึก" />
              <ButtonConfirmAssign label="ยืนยันเบิกจ่าย" @click="handleSubmit(onConfirmDisbursementAsync)" />
            </template>
          </div>

          <Accordion :value="['0', '1', '2', '3']" unstyled multiple>
            <AccordionPanel value="0">
              <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="pcm007Store.detail.acceptors"
                :acceptor-type="AcceptorType.Approver" is-approve v-if="pcm007Store.detail.acceptors" is-manage
                :is-disable="!pcm007Store.isEdit" />
            </AccordionPanel>
            <AccordionPanel value="1" class="mt-4" v-if="showCommitteeAcceptor">
              <AccordHeader label="ผู้ตรวจรับพัสดุ" />
              <AccordionContent>
                <Card class="rounded-none">
                  <template #content>
                    <div v-for="(member, idx) in inspectionCommittees" :key="member.id ?? idx">
                      <div class="flex items-center justify-between">
                        <div class="flex items-center gap-1">
                          <span class="material-symbols-outlined text-green-500 text-[20px]"
                            v-if="committeeStatusIcon(member.sequence) === 'approved'">
                            check_circle
                          </span>
                          <span class="material-symbols-outlined text-yellow-400 text-[20px]"
                            v-else-if="committeeStatusIcon(member.sequence) === 'pending'">
                            schedule
                          </span>
                          <Divider layout="vertical" class="mx-1"
                            v-if="committeeStatusIcon(member.sequence) !== 'draft'" />
                          <div>
                            <p>{{ member.fullName }}</p>
                            <small class="text-gray-400 text-base">{{ member.fullPositionName }}</small>
                          </div>
                        </div>
                        <p class="text-sm text-gray-500">{{ member.committeePositionsName }}</p>
                      </div>
                      <Divider class="my-2" />
                    </div>
                    <p v-if="inspectionCommittees.length === 0" class="text-center text-gray-500 text-sm">ไม่มีข้อมูล
                    </p>
                  </template>
                </Card>
              </AccordionContent>
            </AccordionPanel>
            <AccordionPanel value="2" class="mt-4" v-if="showAccountingAcceptor">
              <AccordAcceptor title="บัญชีเห็นชอบ" v-model="pcm007Store.detail.acceptors"
                :acceptor-type="AcceptorType.AccountingApprover" is-approve v-if="pcm007Store.detail.acceptors"
                is-manage :is-disable="!pcm007Store.isAccounting && !pcm007Store.isEdit"
                :is-set-default="!!pcm007Store.detail.id" :default-status="AcceptorStatus.Pending"
                @set-default="() => pcm007Store.getDefaultAccountingAcceptorAsync()" />
            </AccordionPanel>
            <AccordionPanel value="3" class="mt-4"
              v-if="[Pcm007Status.WaitingDisbursementDate, Pcm007Status.Paid].includes(pcm007Store.detail.status)">
              <AccordAcceptor title="กลุ่มงานบัญชี" v-model="pcm007Store.detail.acceptanceConfirmers"
                :acceptor-type="AcceptorType.AccountingConfirmer" is-approve
                v-if="pcm007Store.detail.acceptanceConfirmers" is-manage is-disable />
            </AccordionPanel>
          </Accordion>
        </div>
      </div>
    </div>
  </Form>
</template>
