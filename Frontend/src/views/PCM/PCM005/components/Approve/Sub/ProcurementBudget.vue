<script setup lang="ts">
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { InputField, InputNumber, Checkbox, Select } from '@/components/forms';
import draggable from 'vuedraggable';
import type { Contract, ContractGroup } from '@/views/PP/models/PP008/pp008model';
import type { Option } from '@/models/shared/option';
import { formatCurrency } from '@/helpers/currency';
import { showWinnerDialogAsync } from '@/helpers/dialog';
import { ArrayHelper } from '@/helpers/array';
import ToastHelper from '@/helpers/toast';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';

type Props = {
  title: string;
  disabled?: boolean;
};

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();
const pcmStore = usePcm005DetailStore();
const { title, disabled } = defineProps<Props>();
const value = defineModel<ContractGroup>({
  required: true,
});

const mockingOption = [
  { value: 'parcel', label: 'ผู้จัดทำ/คณะกรรมการตรวจรับพัสดุ' },
] as Option[];

const onSelectWinnerAsync = async () => {
  if (!(pcmStore.body.id && pcmStore.body.principleApprovalRental?.id)) return;

  const resp = await showWinnerDialogAsync(pcmStore.body.id, pcmStore.body.principleApprovalRental.id, undefined, undefined, true);

  if (!resp) return;

  if (value.value.contracts.some(s => s.principleApprovalRentalEntrepreneursId === resp.id)) {
    return ToastHelper.warning('ข้อมูลซ้ำ', 'ข้อมูลผู้ชนะซ้ำ');
  }

  value.value.contracts = addSequence(value.value.contracts, {
    agreedPrice: resp.agreedPrice,
    principleApprovalRentalEntrepreneursId: resp.id,
    purchaseOrderEntrepreneurName: resp.name,
    purchaseOrderEntrepreneurEmail: resp.email,
  } as Contract);
};

const removeWinner = (index: number) => {
  value.value.contracts = deleteItemAndReSequence(value.value.contracts, index);
};

const onRequence = (): void => {
  value.value.contracts = reSequence(value.value.contracts);
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="title" />
      <TitleHeader
        :label="`ลำดับ ${value.budgetSequence} ${value.budgetDescription} : ${formatCurrency(value.budget)} บาท`"
        :hiddenIcon="true" />
      <div class="flex items-center gap-3">
        <p>ข้อมูลผู้ชนะโครงการ</p>
        <div class="h-px bg-gray-300 flex-1" />
        <Button v-if="!disabled" label="เพิ่มผู้ชนะ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="onSelectWinnerAsync" />
      </div>
      <draggable v-model="value.contracts" handle=".drag-contract" itemKey="sequence" @end="() => onRequence()">
        <template #item="{ element, index }: { element: Contract, index: number }">
          <div class="bg-gray-100 mt-5 p-5">
            <div class="grid grid-cols-3">

              <InfoItem title="ผู้ประกอบการ" :content="element.purchaseOrderEntrepreneurName" />
              <InfoItem title="Email" :content="element.purchaseOrderEntrepreneurEmail ?? '-'" />
              <div class="flex items-center gap-2 justify-end" v-if="!disabled">
                <i class="pi pi-trash text-red-500 cursor-pointer" @click="() => removeWinner(index)" />
                <span class="material-symbols-outlined drag-contract cursor-pointer">
                  drag_indicator
                </span>
              </div>
            </div>
            <div class="grid grid-cols-3 gap-2 mt-8">
              <InputField label="เลขที่สัญญา" v-model="element.contractNumber" rules="required"
                :disabled="disabled || !element.hasEditContractNumber" />
              <InputNumber label="ราคาที่ตกลง" v-model="element.agreedPrice" rules="required" :disabled :min-fraction-digits="2" grouping />
              <InputField label="เลขที่ PO (SAP)" v-model="element.poNumber" rules="required" :disabled />
              <div class="col-span-3 mb-4">
                <Checkbox label="แก้ไขเลขที่สัญญา" v-model="element.hasEditContractNumber" :disabled />
              </div>
              <Select label="ชุดคณะกรรมการตรวจรับ" class="col-span-2 mt-4" v-model="element.committeeType"
                :options="mockingOption" rules="required" :disabled />
            </div>
          </div>
        </template>
      </draggable>
    </template>
  </Card>
</template>