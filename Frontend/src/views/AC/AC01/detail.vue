<script setup lang="ts">
import { ButtonSave, ButtonSendEdit } from '@/components/Button';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import StatusChip from '@/components/StatusChip.vue';
import { AC01Status, sourceType } from '@/enums/AC/ac01';
import { AccordAcceptor } from '@/components/Accordions';
import AC01Helper from '@/helpers/AC/ac01';
import { ToDateOnly } from '@/helpers/dateTime';
import { showActivityDialog, showReasonDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import { useAc01DetailStore } from '@/stores/AC/ac01';
import { useMenuStore } from '@/stores/menu';
import { Button, Card } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { computed, defineAsyncComponent, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { BadgeStatus as BadgeComponent } from '@/components';
import type { GlAccounts, SourceDataClause79_2, SourceDataContractGuaranteeReturn, SourceDataDisbursement, SourceDataPettyCashReimbursement, SourceDataW119 } from '@/models/ACC/acc001';
import { ArrayHelper } from '@/helpers/array';
import { Select, UploadFileGroup } from '@/components/forms';
import { usePcm002DetailStore } from '@/stores/PCM/pcm002';
import { usePcm003DetailStore } from '@/stores/PCM/pcm003';
import { usePcm004DetailStore } from '@/stores/PCM/pcm004';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import { AcceptorType } from '@/enums/participants';
import { ButtonApprove, ButtonConfirm, ButtonRecall } from '@/components/Button';
import { ReasonDialogType } from '@/enums/dialog';

const routeItems = ref<Array<MenuItem>>([
  { label: 'การเบิกจ่าย', url: '/ac/ac01' },
  { label: 'รายละเอียดการเบิกจ่าย', },
]);

const { BadgeStatus } = AC01Helper;
const { deleteItemAndReSequence } = ArrayHelper();

const menuStore = useMenuStore();

const { SourceTypeName } = AC01Helper;

const rount = useRoute();

const id = computed(() => rount.params.id.toString())

const store = useAc01DetailStore();

const Type119Component = defineAsyncComponent(() => import("./components/Type119.vue"));
const Type79Component = defineAsyncComponent(() => import('./components/Type79.vue'));
const TypeContractComponent = defineAsyncComponent(() => import('./components/TypeContract.vue'));
const TypeDebtComponent = defineAsyncComponent(() => import('./components/TypeDebt.vue'));
const TypePattyCashComponent = defineAsyncComponent(() => import('./components/TypePattyCash.vue'));

onMounted(async () => {
  await Promise.all([
    store.api.getDetailById(id.value),
    store.api.onGetDropdownAsync(),
  ]);

  initAsync();

  if (store.body.acceptors.length == 0) {
    await getDefaultAcceptor();
  }
});

const initAsync = async (): Promise<void> => {
  if ((store.body.assignees && store.body.assignees.length === 0) || store.body.assignees == undefined) {
    await store.api.getDefaultExpenseDisbursementAsync();
  }
};

const onConfirmAssignApprovAsync = async (): Promise<void> => {
  if (store.body.advancePaymentDate === undefined) {
    return ToastHelper.warning("กรุณาระบุข้อมูลการเบิกจ่าย", "กรุณาระบุข้อมูล วันที่เบิกจ่าย");
  }

  const reason = await showReasonDialogAsync(ReasonDialogType.Confirm, true);

  if (!reason.isConfirm) return;

  store.body.status = AC01Status.Approved;
  store.body.remarks = reason.reason;

  await store.api.updateById();
};

const onSubmit = async () => {
  if (!id.value) return;

  await store.api.updateById();
}

const addGlAccount = () => {
  const newAccounts = {
    sequence: store.body.glAccounts.length + 1,
  } as GlAccounts;

  store.body.glAccounts.push(newAccounts);
}

const removeGlAccount = (index: number) => {
  store.body.glAccounts = deleteItemAndReSequence(store.body.glAccounts, index)
};
// Handle Expense Disbursement Source Type
// if Source Type is W119 use usePcm002DetailStore
// if Source Type is Clause79_2 use usePcm003DetailStore
// if Source Type is PettyCashReimbursement use usePcm004DetailStore
// if Source Type is ContractGuaranteeReturn use useCm006DetailStore

const pcm002Store = usePcm002DetailStore();
const pcm003Store = usePcm003DetailStore();
const pcm004Store = usePcm004DetailStore();
const cm006Store = useCm006DetailStore();

const onUpsertAttachmentsAsync = async (expenseDisbursementSourceType: sourceType) => {
  switch (expenseDisbursementSourceType) {
    case sourceType.W119:
      await pcm002Store.onUpsertAttachmentsFromExpenseDisbursement(store.body.sourceId, store.body.attachments);
      break;
    case sourceType.Clause79_2:
      await pcm003Store.onUpsertAttachmentsFromExpenseDisbursement(store.body.sourceId, store.body.attachments);
      break;
    case sourceType.PettyCashReimbursement:
      await pcm004Store.onUpsertAttachmentsFromExpenseDisbursement(store.body.sourceId, store.body.attachments);
      break;
    case sourceType.ContractGuaranteeReturn:
      await cm006Store.onUpsertAttachmentsFromExpenseDisbursement(store.body.sourceId, store.body.attachments);
      break;
    default:
      break;
  }
}

const sourceData = computed<SourceDataDisbursement>((): SourceDataDisbursement => store.body.source.data as SourceDataDisbursement);

const summary = computed<number>(() => {

  if (store.body.sourceType === sourceType.W119 || store.body.sourceType === sourceType.Clause79_2) {
    const source = store.body.source.data as SourceDataClause79_2 | SourceDataW119;

    const totalBeforeVat = source.vendors.reduce((vendorSum, vendor) => {
      const vendorTotal = vendor.parcels.reduce((parcelSum, parcel) => {
        return parcelSum + parcel.totalPrice * parcel.quantity;
      }, 0);
      return vendorSum + vendorTotal;
    }, 0);

    const totalVat = totalBeforeVat * 0.07;
    return totalBeforeVat + totalVat;
  }

  if (store.body.sourceType === sourceType.ContractGuaranteeReturn) {
    const sourceContractGuarantee = computed(() => store.body.source.data as SourceDataContractGuaranteeReturn);

    if (!sourceContractGuarantee.value) return 0
    return sourceContractGuarantee.value.returnAmount;
  }

  if (store.body.sourceType === sourceType.PettyCashReimbursement) {
    const sourcePettyCash = computed(() => store.body.source.data as SourceDataPettyCashReimbursement);

    if (!sourcePettyCash.value.items || sourcePettyCash.value.items.length === 0) return 0
    return sourcePettyCash.value.items.reduce((sum, item) => sum + (item.amount ?? 0), 0)
  }

  // Disbursement
  if (!sourceData.value.installments || sourceData.value.installments.length === 0)
    return 0;

  return sourceData.value.installments.reduce(
    (sum: number, s: { amount?: number | null }) => sum + (s.amount || 0),
    0
  );
});

const getDefaultAcceptor = async () => {
  await store.api.getDefaultAcceptor(
    summary.value ?? 0
  );
};


</script>

<template>
  <TitleHeader label="การเบิกจ่าย" :route-items="routeItems">
    <template #action>
      <div class="flex items-center gap-2">
        <p class="text-sm">สถานะ :</p>
        <BadgeComponent :color="BadgeStatus(store.body.status).color" :label="BadgeStatus(store.body.status).label" />
      </div>
      <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
        class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
    </template>
  </TitleHeader>
  <Form @submit="onSubmit" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="mt-4">
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5 p-4">
          <Card class="mb-4">
            <template #content>
              <TitleHeader label="รายละเอียด" />
              <div class="px-4 mt-2 grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                <InfoItem title="เลขที่อ้างอิง" :content="store.body.source.refCode" />
                <InfoItem title="ฝ่าย/สำนัก" :content="store.body.source.departmentName" />
                <InfoItem title="วันที่ส่งเอกสาร" :content="ToDateOnly(store.body.date)" />
                <InfoItem title="เรื่อง" :content="store.body.advanceName" class="col-span-2 md:col-span-1" />
                <InfoItem title="วงเงินงบประมาณ" :content="Intl.NumberFormat('th', { minimumFractionDigits: 2 })
                  .format(store.body.glAccounts.reduce((sum, x) => sum + (x.amount ?? 0), 0))" />
                <InfoItem title="ประเภทงาน" :content="SourceTypeName(store.body.sourceType)" class="col-start-1">
                  <template #content="{ item }">
                    <StatusChip color="Info" :label="(item as string) ?? ''" class="w-fit text-center justify-center" />
                  </template>
                </InfoItem>
              </div>
              <div v-if="store.body.sourceType != sourceType.PettyCashReimbursement" class=" mt-6 px-4">
                <div class="flex justify-between items-center">
                  <p class="font-bold">ข้อมูลรหัสบัญชีและการใช้งานงบประมาณของฝ่าย</p>
                  <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                    @click="addGlAccount" v-if="store.state.canEdit" />
                </div>
                <DataTable :value="store.body.glAccounts">
                  <Column bodyStyle="vertical-align: top">
                    <template #header>
                      <p class="w-full font-bold text-center">ลำดับ</p>
                    </template>
                    <template #body="{ data }">
                      <p class="text-center">{{ data.sequence }}</p>
                    </template>
                  </Column>
                  <Column bodyStyle="vertical-align: top" body-class="min-w-[200px]">
                    <template #header>
                      <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
                    </template>
                    <template #body="{ data }">
                      <Select v-model="data.soId" :options="store.departmentDropdown" rules="required"
                        :disabled="!store.state.canEdit || !menuStore.hasManage" hide-details />
                    </template>
                  </Column>
                  <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
                    <template #header>
                      <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
                    </template>
                    <template #body="{ data }">
                      <Select v-model="data.budgetTypeCode" :options="store.budgetTypeDropdown" rules="required"
                        :disabled="!store.state.canEdit || !menuStore.hasManage" hide-details />
                    </template>
                  </Column>
                  <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
                    <template #header>
                      <p class="w-full font-bold text-center">รหัสโครงการ</p>
                    </template>
                    <template #body="{ data }">
                      <InputField v-model="data.projectNumber" hide-details
                        :disabled="!store.state.canEdit || data.budgetTypeCode === 'BudgetType001' || !menuStore.hasManage" />
                    </template>
                  </Column>
                  <Column bodyStyle="vertical-align: top" body-class="min-w-[175px]">
                    <template #header>
                      <p class="w-full font-bold text-center">รหัสบัญชี</p>
                    </template>
                    <template #body="{ data }">
                      <Select v-model="data.glAccountCode" hide-details :options="store.accountCodeDropdown"
                        rules="required" :disabled="!store.state.canEdit || !menuStore.hasManage" />
                    </template>
                  </Column>
                  <Column bodyStyle="vertical-align: top" body-class="min-w-[200px]">
                    <template #header>
                      <p class="w-full font-bold text-center">จำนวนเงิน</p>
                    </template>
                    <template #body="{ data }">
                      <InputNumber v-model="data.amount" rules="required" :disabled="!store.state.canEdit" grouping
                        :min-fraction-digits="2" hide-details />
                    </template>
                  </Column>
                  <Column field="control" v-if="store.state.canEdit && menuStore.hasManage">
                    <template #body="{ index }">
                      <div class="flex items-center mb-5">
                        <i v-if="store.body.glAccounts.length > 1" class="pi pi-trash mt-4 text-red-600 cursor-pointer"
                          @click="() => removeGlAccount(index)" />
                      </div>
                    </template>
                  </Column>
                  <template #empty>
                    <p class="text-center font-bold">ไม่พบข้อมูล</p>
                  </template>
                </DataTable>
              </div>
            </template>
          </Card>
          <Type119Component v-if="store.body.sourceType == sourceType.W119" />
          <Type79Component v-if="store.body.sourceType == sourceType.Clause79_2" />
          <TypeContractComponent v-if="store.body.sourceType == sourceType.ContractGuaranteeReturn" />
          <TypeDebtComponent v-if="store.body.sourceType == sourceType.Disbursement" />
          <TypePattyCashComponent v-if="store.body.sourceType == sourceType.PettyCashReimbursement" />
          <UploadFileGroup v-if="store.body.id" v-model="store.body.attachments"
            @upload="onUpsertAttachmentsAsync(store.body.sourceType)"
            @remove-file="onUpsertAttachmentsAsync(store.body.sourceType)"
            @remove-group="onUpsertAttachmentsAsync(store.body.sourceType)"
            @reorder="onUpsertAttachmentsAsync(store.body.sourceType)" :disabled="!menuStore.hasManage" />
        </div>

        <div class="lg:col-span-2 relative">
          <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
            <div class="flex items-center gap-2 justify-end">
              <ButtonSave type="submit" v-if="(store.state.canEdit || store.state.canConfirm) && menuStore.hasManage" />
              <ButtonSendApprove @click="handleSubmit(store.api.onSendApprove)"
                v-if="store.body.id && store.state.canEdit" />
              <ButtonRecall @click="store.api.onRecallAsync()" v-if="store.state.canReCall" />

              <div class="wating-approve flex gap-2 items-center" v-if="store.state.canApprove">
                <ButtonSendEdit @click="store.api.onRejectAsync()" v-if="store.state.isCurrentApproval" />
                <ButtonApprove @click="store.api.onApproveAsync()"
                  v-if="store.state.isCurrentApproval && !store.state.isLastApprover" />
                <ButtonConfirm @click="store.api.onApproveAsync()"
                  v-if="store.state.isCurrentApproval && store.state.isLastApprover" />
              </div>

              <Button v-if="store.state.canConfirm" label="ยินยันเบิกจ่าย" icon="pi pi-user-plus" severity="warn"
                @click="handleSubmit(onConfirmAssignApprovAsync)" />

            </div>
            <Accordion :value="['0']" unstyled multiple>
              <AccordionPanel value="0">
                <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.body.acceptors"
                  @set-default="() => getDefaultAcceptor()" :acceptor-type="AcceptorType.Approver" isManage
                  :is-disable="!store.state.canEdit || !menuStore.hasManage" isApprove
                  :is-set-default="store.state.isCanSetDefaultApprover" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </Form>
</template>