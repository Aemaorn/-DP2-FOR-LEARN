<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { Select } from '@/components/forms';
import { AccordAcceptor, AccordAssignee } from '@/components/Accordions';
import {
  ButtonRecall,
  ButtonSave,
  ButtonSendApprove,
  ButtonApprove,
  ButtonSendEdit,
  ButtonConfirmAssign,
  ButtonConfirm,
} from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { AcceptorType, AssigneeGroup, AssigneeType } from '@/enums/participants';
import { Form } from 'vee-validate';
import { usePP008DetailStore } from '../../stores/PP008/PP008Store';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { computed, onBeforeMount, onMounted, ref, watch } from 'vue';
import { formatNumber } from '@/helpers/currency';
import { pp008CommitteeType, PP008Status } from '../../enums/pp008';
import { showActivityDialog, showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { useMenuStore } from '@/stores/menu';
import { useAuthenticationStore } from '@/stores/authentication';
import { DepartmentId } from '@/enums/businessUnit';
import ProcurementBudget from './components/ProcurementBudget.vue';
import PoaConstant from '@/constants/poa';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import PurchaseOrderApprovalBudgetDialog from './components/PurchaseOrderApprovalBudgetDialog.vue';
import CommitteeListSection from './components/CommitteeListSection.vue';

const props = defineProps({ readonly: { type: Boolean, default: false } });

const { poaStatusColor } = PoaConstant;
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const store = usePP008DetailStore();
const procurementStore = usePPDetailStore();
const currentAccordion = ref<string[]>([]);
const dropdown = ref<Array<Option>>([]);

onBeforeMount(() => {
  store.onResetDetail();
});

onMounted(async (): Promise<void> => {
  await Promise.all([onGetContractDropdownAsync(), onInitAsync()]);
  await Promise.all([store.fetchPositionInspOptions(), store.onSetDefaultWinnersAsync()]);
});

const onGetContractDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CType);

  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};

const onInitAsync = async (): Promise<void> => {
  const id = store.detail?.id ?? procurementStore.procurementDetail.purchaseOrderApproval?.id;

  await store.getByIdAsync(id);
};

const hasDepartmentAccess = computed((): boolean => {
  return authStore.profile.departmentCode === DepartmentId.JorPor
    || authStore.profile.departmentCode === procurementStore.procurementDetail.departmentCode;
});

const totalVendorAgreePrice = computed(() =>
  (store.detail.contractBudgetGroups ?? []).reduce(
    (sum, group) =>
      sum +
      (group.contracts ?? []).reduce(
        (subSum, x) => subSum + (x.agreedPrice ?? 0),
        0
      ),
    0
  )
);

watch(totalVendorAgreePrice, async () => {

  if (store.detail.acceptors.length === 0) {
    await store.onSetDefaultAcceptors();
  }
});

const onSubmitAsync = async (): Promise<void> => {

  for (const group of store.detail?.contractBudgetGroups ?? []) {
    const totalAgreedPrice = (group.contracts ?? [])
      .reduce((sum, contract) => sum + (contract.agreedPrice ?? 0), 0);

    if (totalAgreedPrice > group.budget) {
      ToastHelper.warning(
        'เกินวงเงิน',
        `ผลรวมราคาที่ตกลง วงเงินที่ ${group.budgetSequence ?? ''} ต้องไม่เกิน ${formatNumber(group.budget ?? 0)} บาท`
      );
      return;
    }
  }

  if (store.detail.id && !store.detail.purchaseRequisitionId && store.detail.committees.length === 0) {
    ToastHelper.errorDescription('ต้องมีผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุ อย่างน้อย 1 คน');
    return;
  }

  if (store.detail.id) {
    await store.updateAsync(store.detail.id);

    return;
  }

  await store.createAsync();

  if (store.detail.id) {
    onInitAsync();
  }
};

const onSendApproveAsync = async (): Promise<void> => {
  if (store.detail.acceptors.length === 0) {
    return ToastHelper.approvalAtLeastMessageToast();
  }

  if (store.detail.id && !store.detail.purchaseRequisitionId && store.detail.committees.length === 0) {
    ToastHelper.errorDescription('ต้องมีผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุ อย่างน้อย 1 คน');
    return;
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.SendApproveConfirm)) return;

  if (store.detail.id) {
    return await store.updateAsync(store.detail.id, PP008Status.WaitingApproval);
  }

  await store.createAsync(PP008Status.WaitingApproval);
};

const onRecallAsync = async (): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Edit)) return;

  if (store.detail.id) {
    return await store.updateAsync(store.detail.id, PP008Status.Edit);
  }
};

const onSaveAssigneeAsync = async (): Promise<void> => {
  if (store.detail.id) {
    await store.updateAsync(store.detail.id, PP008Status.WaitingAssign);
  }
};

const onConfirmAssigneeAsync = async (): Promise<void> => {
  if (!store.detail.assignees.some(x => x.assigneeType === AssigneeType.Assignee)) {
    return ToastHelper.assignAtLeastMessageToast();
  }

  if (!await showConfirmDialogAsync(ConfirmDialogType.Assigned)) return;

  if (store.detail.id) {
    return await store.updateAsync(store.detail.id, PP008Status.Assigned);
  }
};

const onChangeContractDropdownAsync = async (val?: string) => {
  store.detail.assignees = [];
  store.detail.contractBudgetGroups?.forEach((e) => e.contracts.forEach((c) => c.contractNumber = undefined));

  if (val === "CType001") {
    await store.onSetDefaultAsssignees();
  }
};

watch(() => store.detail.status, (val: PP008Status) => {
  if ([PP008Status.Assigned].includes(val)) {
    currentAccordion.value = ['0', '1'];
  }

  if ([PP008Status.Draft, PP008Status.Edit, PP008Status.Rejected, PP008Status.WaitingApproval].includes(val)) {
    currentAccordion.value = ['0'];
  }

  if ([PP008Status.WaitingAssign].includes(val)) {
    currentAccordion.value = ['1'];
  }
}, { immediate: true });

watch(
  () => store.detail.contractBudgetGroups?.map(g => g.contracts),
  (contractsList) => {
    contractsList?.forEach(contracts => {
      contracts?.forEach(contract => {
        if (contract.hasEditContractNumber == null) {
          contract.hasEditContractNumber = true;
        }
      });
    })
  },
  { immediate: true, deep: true }
);

const showModal = ref(false);

const onShowModal = async (): Promise<void> => {
  showModal.value = true;
};
</script>

<template>
  <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <div class="mt-4">
      <div class="my-2">
        <TitleHeader label="อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา">
          <template #action>
            <BadgeStatus :color="poaStatusColor(store.detail.status, store.detail.contractType === 'CType002').color"
              :label="poaStatusColor(store.detail.status, store.detail.contractType === 'CType002').label"
              v-if="store.detail.status" />
            <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary"
              v-if="store.detail.id" class="bg-white! hover:bg-red-50!"
              @click="() => showActivityDialog(store.detail.id!)" />
          </template>
        </TitleHeader>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-7 gap-2">
        <div class="lg:col-span-5">
          <Card>
            <template #content>
              <TitleHeader label="ข้อมูลอ้างอิง" />
              <div class="grid lg:grid-cols-2 mt-10">
                <Select label="ประเภทอนุมัติใบสั่งซื้อ/จ้าง/เช่า" :options="dropdown"
                  v-model="store.detail.contractType" rules="required"
                  :disabled="!store.status.canEdit || !menuStore.hasManage || props.readonly"
                  @on-select="onChangeContractDropdownAsync" />
              </div>
            </template>
          </Card>
          <Card class="my-5">
            <template #content>
              <TitleHeader label="วงเงินที่จัดซื้อจัดจ้าง">
                <template #action>
                  <Button
                    v-if="store.detail.id && !store.detail.purchaseRequisitionId && store.detail.status === (PP008Status.Draft || PP008Status.Edit || PP008Status.Rejected) && menuStore.hasManage"
                    label="เพิ่มวงเงิน" icon="pi pi-plus" severity="primary" variant="outlined"
                    class="bg-white! hover:bg-red-50!" @click="() => onShowModal()" />
                </template>
              </TitleHeader>
              <div v-if="store.detail.contractBudgetGroups">
                <div v-for="(data, index) in store.detail.contractBudgetGroups" :key="data.budgetId">
                  <ProcurementBudget
                    :title="`วงเงินที่ ${data.budgetSequence} ${data.budgetDescription} : ${formatNumber(data.budget)} บาท`"
                    :index="index" :tor-draft-budget-id="data.budgetId" :class="index !== 0 ? 'mt-15' : ''"
                    :purchase-order-approval-budgets="data"
                    :purchase-requisition-id="store.detail.purchaseRequisitionId" />
                </div>
              </div>
            </template>
          </Card>
          <Card class="my-5" v-if="store.detail.id && !store.detail.purchaseRequisitionId">
            <template #content>
              <CommitteeListSection :disable="store.status.canEdit" :showOption="false"
                label="ผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุ" person="ผู้ตรวจรับพัสดุ"
                v-model:committee="store.detail.committees" v-model:spacialOption="store.positionInspOptions"
                v-model:is-committee="store.detail.isInspectCommittee"
                :groupType="pp008CommitteeType.InspectionCommittee" />
            </template>
          </Card>
        </div>
        <div class="relative lg:col-span-2">
          <div class="flex flex-col gap-4 ml-3 sticky top-[100px]">
            <div class="flex items-center gap-2 justify-end" v-if="menuStore.hasManage && !props.readonly">
              <ButtonSave type="submit" v-if="store.status.canEdit && hasDepartmentAccess" />
              <ButtonSendApprove @click="handleSubmit(() => onSendApproveAsync())"
                v-if="store.status.canEdit && store.detail.id && hasDepartmentAccess" />

              <ButtonRecall @click="onRecallAsync" v-if="store.status.canRecall && hasDepartmentAccess" />

              <ButtonSendEdit @click="store.rejectAsync"
                v-if="store.status.canApproveOrReject || store.status.canAssign" />
              <ButtonApprove @click="store.approveAsync"
                v-if="store.status.canApproveOrReject && !store.status.isLastApproval" />
              <ButtonConfirm @click="store.approveAsync"
                v-if="store.status.canApproveOrReject && store.status.isLastApproval" />
              <ButtonSave :text="'บันทึกผู้รับผิดชอบ'" @click="onSaveAssigneeAsync"
                v-if="(store.status.canAssign || store.status.canAssignedApprove || store.status.canAssignByAssignee)" />
              <ButtonConfirmAssign @click="onConfirmAssigneeAsync" v-if="store.status.canAssignByAssignee" />
            </div>
            <Accordion v-model:value="currentAccordion" unstyled multiple>
              <AccordionPanel value="0">
                <AccordAcceptor title="ผู้มีอำนาจเห็นชอบ/อนุมัติ" v-model="store.detail.acceptors"
                  :acceptor-type="AcceptorType.Approver" isManage isApprove
                  :is-disable="!store.status.canEdit || !menuStore.hasManage || props.readonly" isSetDefault
                  @set-default="store.onSetDefaultAcceptors" />
              </AccordionPanel>
              <AccordionPanel value="1" class="mt-5" v-if="store.detail.contractType === 'CType001'">
                <AccordAssignee title="มอบหมายผู้รับผิดชอบสัญญา" v-model="store.detail.assignees"
                  :disabled="(!store.status.canAssign && !store.status.canAssignedApprove && !store.status.canAssignByAssignee) || !menuStore.hasManage || props.readonly"
                  :group="AssigneeGroup.Contract" />
              </AccordionPanel>
            </Accordion>
          </div>
        </div>
      </div>
    </div>
  </Form>
  <PurchaseOrderApprovalBudgetDialog v-model="showModal" />
</template>
