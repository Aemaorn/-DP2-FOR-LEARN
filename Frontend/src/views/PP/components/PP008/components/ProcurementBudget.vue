<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { showConfirmDialogAsync, showPartnerDialogAsync, showWinnerDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { usePP008DetailStore } from '@/views/PP/stores/PP008/PP008Store';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { InputField, InputNumber } from '@/components/forms';
import { InfoItem } from '@/components/cosmetic';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import ToastHelper from '@/helpers/toast';
import { ref, type PropType } from 'vue';
import PurchaseOrderApprovalBudgetDialog from './PurchaseOrderApprovalBudgetDialog.vue';
import type { ContractGroup } from '@/views/PP/models/PP008/pp008model';
import { PP008Status } from '@/views/PP/enums/pp008';

const props = defineProps({
  title: { type: String, requried: true },
  index: { type: Number, required: true },
  torDraftBudgetId: { type: String, required: true },
  purchaseOrderApprovalBudgets: { type: Object as PropType<ContractGroup>, required: true },
  purchaseRequisitionId: { type: String, required: false },
});

const { deleteItemAndReSequence } = ArrayHelper();
const menuStore = useMenuStore();
const store = usePP008DetailStore();
const procurementStore = usePPDetailStore();
const mockingOption = [
  { value: 'parcel', label: 'ผู้จัดทำ/คณะกรรมการตรวจรับพัสดุ' },
] as Option[];

const showModal = ref(false);
const selectedBudget = ref<ContractGroup | null>(null);

const addWinnerAsync = async (): Promise<void> => {
  if (!procurementStore.procurementDetail.id &&
    procurementStore.procurementDetail.purchaseOrder &&
    !procurementStore.procurementDetail.purchaseOrder.id) return;

  const res = await showWinnerDialogAsync(procurementStore.procurementDetail.id!, procurementStore.procurementDetail.purchaseOrder!.id!);

  if (!res) return;

  if (!store.detail.contractBudgetGroups) return;

  if (store.detail.contractBudgetGroups[props.index].contracts.some((c): boolean => c.purchaseOrderEntrepreneurId === res.id)) {
    return ToastHelper.warning('ข้อมูลซ้ำ', 'ข้อมูลผู้ชนะซ้ำ');
  }

  const data = {
    agreedPrice: res.agreedPrice,
    purchaseOrderEntrepreneurId: res.id,
    sequence: store.detail.contractBudgetGroups[props.index].contracts ? store.detail.contractBudgetGroups[props.index].contracts.length + 1 : 1,
    purchaseOrderEntrepreneurName: res.name,
    purchaseOrderEntrepreneurEmail: res.email,
    budget: res.agreedPrice,
  };

  if (store.detail.contractBudgetGroups[props.index].contracts) {
    store.detail.contractBudgetGroups[props.index].contracts.push(data);
  } else {
    store.detail.contractBudgetGroups[props.index].contracts = [data];
  }
};

const removeWinner = async (contractIndex: number): Promise<void> => {
  if (!store.detail.contractBudgetGroups || !store.detail.contractBudgetGroups[props.index].contracts) return;

  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  store.detail.contractBudgetGroups[props.index].contracts = deleteItemAndReSequence(store.detail.contractBudgetGroups[props.index].contracts, contractIndex);
};

const onShowModal = async (purchaseOrderApprovalBudgets?: ContractGroup): Promise<void> => {
  selectedBudget.value = purchaseOrderApprovalBudgets ?? null;

  showModal.value = true;
};

const onShowPartnerModal = async (): Promise<void> => {
  const res = await showPartnerDialogAsync();

  if (!res) return;

  if (!store.detail.contractBudgetGroups) return;

  if (store.detail.contractBudgetGroups[props.index].contracts.some((c): boolean => c.vendorId === res.id)) {
    return ToastHelper.warning('ข้อมูลซ้ำ', 'ข้อมูลผู้ค้าซ้ำ');
  }

  const data = {
    agreedPrice: store.detail.contractBudgetGroups[props.index].budget,
    purchaseOrderApprovalEntrepreneursId: undefined,
    sequence: store.detail.contractBudgetGroups[props.index].contracts ? store.detail.contractBudgetGroups[props.index].contracts.length + 1 : 1,
    purchaseOrderEntrepreneurName: res.establishmentName,
    purchaseOrderEntrepreneurEmail: res.email,
    purchaseOrderApprovalBudgetId: props.torDraftBudgetId,
    vendorId: res.id,
    budget: store.detail.contractBudgetGroups[props.index].budget,
    entrepreneurs: {
      vendorId: res.id,
      emailSend: false,
      id: undefined,
      sequence: store.detail.entrepreneurs ? store.detail.entrepreneurs.length + 1 : 1
    },
  };

  if (store.detail.contractBudgetGroups[props.index].contracts) {
    store.detail.contractBudgetGroups[props.index].contracts.push(data);
  } else {
    store.detail.contractBudgetGroups[props.index].contracts = [data];
  }
};

</script>

<template>
  <div>
    <div class="flex gap-4 px-5">
      <p class="font-bold">{{ props.title }}</p>
      <Divider class="flex-1" />
      <div
        v-if="!purchaseRequisitionId && store.detail.status === (PP008Status.Draft || PP008Status.Edit || PP008Status.Rejected) && menuStore.hasManage">
        <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
          size="small" variant="text" @click="() => onShowModal(purchaseOrderApprovalBudgets)" />
        <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
          variant="text" @click="() => store.onDeleteBudgetAsync(purchaseOrderApprovalBudgets.budgetId)" />
      </div>
    </div>
    <div class="flex items-center gap-4 px-5 mt-5">
      <p class="font-bold">ข้อมูลผู้ชนะโครงการ</p>
      <Divider class="flex-1" />
      <Button label="เพิ่มผู้ชนะ" icon="pi pi-plus" severity="primary" variant="outlined" @click="addWinnerAsync"
        v-if="store.status.canEdit && menuStore.hasManage && purchaseRequisitionId" />
      <Button label="เพิ่มผู้ค้า" icon="pi pi-plus" severity="primary" variant="outlined"
        class="bg-white! hover:bg-red-50!" @click="() => onShowPartnerModal()"
        v-if="store.status.canEdit && menuStore.hasManage && !purchaseRequisitionId" />
    </div>
    <div class="bg-gray-100 p-5 mt-5"
      v-for="(data, index) in store.detail.contractBudgetGroups?.find(c => c.budgetId === props.torDraftBudgetId)?.contracts"
      :key="data.sequence">
      <div class="grid lg:grid-cols-3 items-center gap-2">
        <InfoItem title="ผู้ประกอบการ" :content="data.purchaseOrderEntrepreneurName" />
        <InfoItem title="Email" :content="data.purchaseOrderEntrepreneurEmail" />
        <div class="text-end" v-if="store.status.canEdit && menuStore.hasManage">
          <i class="pi pi-trash text-red-500 cursor-pointer" @click="() => removeWinner(index)"></i>
        </div>
      </div>
      <div class="grid lg:grid-cols-3 gap-2 gap-y-8 mt-8">
        <InputField label="เลขที่สัญญา" rules="required" v-model="data.contractNumber"
          v-if="store.detail.contractType === 'CType001'"
          :disabled="!store.status.canEdit || !data.hasEditContractNumber || !menuStore.hasManage" />
        <InputNumber label="ราคาที่ตกลง" grouping :min-fraction-digits="2" :max-fraction-digits="3" rules="required|min_value:0.01" v-model="data.agreedPrice"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <InputField label="เลขที่ PO (SAP)" rules="required" v-model="data.poNumber"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </div>
      <Checkbox label="แก้ไขเลขที่สัญญา" v-model="data.hasEditContractNumber"
        :disabled="!store.status.canEdit || !menuStore.hasManage" v-if="store.detail.contractType === 'CType001'" />
      <div class="grid lg:grid-cols-12 mt-8" v-if="store.detail.purchaseRequisitionId">
        <Select class="col-span-8" label="ชุดคณะกรรมการตรวจรับ" :options="mockingOption" v-model="data.committeeType"
          :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required" />
      </div>
    </div>
  </div>
  <PurchaseOrderApprovalBudgetDialog v-model="showModal" :selected="selectedBudget" />
</template>
