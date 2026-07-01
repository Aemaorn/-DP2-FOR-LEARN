<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { StatusChip } from '@/components';
import { InputArea } from '@/components/forms';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useRp001DetailStore } from '@/stores/RP/rp001';
import { ToDateOnly } from '@/helpers/dateTime';
import { ContractType } from '@/enums/RP/rp001';
import { formatCurrency } from '@/helpers/currency';
import { useRouter } from 'vue-router';

const store = useRp001DetailStore();
const router = useRouter();

const openPpDetail = (procurementId: string): void => {
  const url = router.resolve({ path: `/pp/detail/${procurementId}` }).href;
  window.open(url, '_blank');
};

const isNormalContract = (docDate: Date, signDate: Date): boolean => {
  if (!docDate || !signDate) return false;

  const diffTime = new Date(docDate).getTime() - new Date(signDate).getTime();

  const diffDays = diffTime / (1000 * 60 * 60 * 24);

  return diffDays <= 30;
};
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="รายการสัญญา" />
      <div v-if="store.body.id" class="flex justify-end">
        <Button v-if="store.state.canEdit" label="ดึงรายการสัญญาเพิ่มเติม" severity="primary" variant="outlined"
          icon="pi pi-undo" @click="store.api.getContractDraftVendorList()" />
      </div>

      <div v-if="store.statusOptionBadge.length > 0"
        class="bg-gray-200 mt-5 py-4 px-5 rounded-sm flex justify-between items-center">
        <SelectButton v-model="store.contractType" :allowEmpty="false" :options="store.statusOptionBadge"
          option-label="label" option-value="value" unstyled>
          <template #option="slotProps">
            <div class="cursor-pointer duration-100 flex! flex-nowrap! items-center gap-2 mr-3"
              :class="`${slotProps.option.value == store.contractType ? 'border-b-4 border-primary/90 font-bold py-2' : 'border-b-4 border-transparent'}`">
              <small>
                {{ slotProps.option.label }}
              </small>
              <Badge :value="slotProps.option.count"
                :class="`${slotProps.option.bgColorClass} ${slotProps.option.textColorClass}`" />
            </div>
          </template>
        </SelectButton>
      </div>

      <DataView :value="store.contractType === ContractType.All
        ? store.body.details
        : store.body.details.filter(x => x.contractTypeCode === store.contractType)" data-key="id" class="mt-5">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid grid-cols-3 lg:grid-cols-12 px-2">
              <div class="col-span-2 lg:col-span-8">
                <InfoRow label="เลขที่สัญญา">
                  <p class="underline text-blue-400 cursor-pointer" @click="openPpDetail(data.procurementId)">
                    {{ data.contractNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">
                    {{ data.contractName }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ลงนามใบสัญญา">
                  <p>
                    {{ ToDateOnly(data.contractSignedDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="คู่สัญญา">
                  <p>
                    {{ data.entrepreneurName }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <StatusChip v-if="isNormalContract(store.body.documentDate, data.contractSignedDate)"
                    :label="'สัญญาปกติ'" size="Small" color="Success" />
                  <StatusChip v-else :label="'สัญญาเกิน 30 วัน'" size="Small" color="Error" />
                </div>
                <Button v-if="store.state.canEdit" icon="pi pi-trash"
                  class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small" variant="text"
                  @click="store.onRemoveDetail(index as number)" />
              </div>
            </div>

            <InputArea :disabled="!store.state.canEdit" label="หมายเหตุ" v-model="data.description" class="mt-8 px-2"
              hide-details />
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
    </template>
  </Card>
</template>