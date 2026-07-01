<script setup lang="ts">
import { Pagination } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { InputField, Select } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { Cm006DialogItem } from '@/models/CM/cm006';
import { useCm006DialogStore } from '@/stores/CM/CM006/cm006Dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import { Button, Dialog } from 'primevue';
import { Form } from 'vee-validate';
import { onMounted, watch } from 'vue';

type Props = {
  defaultDepartment?: boolean;
};

const props = withDefaults(defineProps<Props>(), {
  defaultDepartment: true,
});

const value = defineModel<boolean>({
  default: false,
});

const dialogStore = useCm006DialogStore();
const authStore = useAuthenticationStore();

const onSelect = (item: Cm006DialogItem): void => {
  dialogStore.fn.onSelectDialogItem(item);
  value.value = false;
};

watch(
  () => [dialogStore.searchCriteria.pageNumber, dialogStore.searchCriteria.pageSize],
  () => {
    dialogStore.fn.getDialogListAsync();
  }
);

onMounted(async () => {
  await dialogStore.fn.getDepartmentDropdownAsync();
  await dialogStore.fn.getSupplyMethodDropdownAsync();
  await dialogStore.fn.getDialogListAsync();
  await dialogStore.fn.getSupplyMethodTypeDropdownAsync();
});

watch(value, async (val: boolean) => {
  if (val) {
    dialogStore.searchCriteria.departmentCode = props.defaultDepartment ? authStore.profile.departmentCode : undefined;
    await dialogStore.fn.getDialogListAsync();
  }
});

watch(() => dialogStore.searchCriteria.supplyMethodCode, async (value) => {
  if (value) {
    await dialogStore.fn.getSupplyMethodSpecialTypeDropdownAsync(value);
  }
});
</script>

<template>
  <Dialog v-model:visible="value" modal :draggable="false" :style="{ width: '90vw' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }" @hide="() => (value = false)">
    <template #container="{ closeCallback }">
      <div class="flex flex-col bg-white rounded-2xl max-h-[90vh]">
        <!-- Header -->
        <div class="flex items-center justify-between p-4 shrink-0">
          <TitleHeader label="ค้นหาข้อมูลสัญญา"></TitleHeader>
          <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">close</span>
        </div>

        <!-- Scrollable content -->
        <div class="overflow-y-auto px-4">
          <div class="border border-gray-200 rounded-lg p-4 mb-2">
            <Form @submit="dialogStore.fn.getDialogListAsync()">
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="dialogStore.searchCriteria.keyword" />
                <Select label="ฝ่าย/ภาคเขต" v-model="dialogStore.searchCriteria.departmentCode"
                  :options="dialogStore.departmentDropdown" @enter-close="dialogStore.fn.getDialogListAsync()" />
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5 mt-5">
                <Select class="col-start-1" label="วิธีการจัดหา" v-model="dialogStore.searchCriteria.supplyMethodCode"
                  :options="dialogStore.supplyMethodCodeDropdown" @enter-close="dialogStore.fn.getDialogListAsync()" />
                <Select v-model="dialogStore.searchCriteria.supplyMethodTypeCode"
                  :options="dialogStore.supplyMethodTypeCodeDropdown" @enter-close="dialogStore.fn.getDialogListAsync()"
                  @change="dialogStore.fn.getSupplyMethodSpecialTypeDropdownAsync(dialogStore.searchCriteria.supplyMethodTypeCode)" />
                <Select v-model="dialogStore.searchCriteria.supplyMethodSpecialTypeCode"
                  :options="dialogStore.supplyMethodSpecialTypeCodeDropdown"
                  @enter-close="dialogStore.fn.getDialogListAsync()" />
              </div>
              <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
                <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
                <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                  @click="() => dialogStore.fn.resetCriteria()" />
              </div>
            </Form>
          </div>

          <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

          <div v-for="(data, index) in (dialogStore.table.data as Cm006DialogItem[])" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1 px-3">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 gap-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่สัญญา">
                  <p class="font-bold">{{ data.contractNumber }}</p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>{{ data.poNumber }}</p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">{{ data.contractName }}</p>
                </InfoRow>
                <InfoRow label="คู่ค้า">
                  <p>{{ data.entrepreneurName }}</p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>{{ formatCurrency(data.budget ?? 0) }}</p>
                </InfoRow>
                <InfoRow label="ประเภทสัญญา">
                  <p>{{ data.contractType }}</p>
                </InfoRow>
                <InfoRow label="วันที่ลงนามสัญญา">
                  <p>{{ ToDateOnly(data.contractSignedDate) }}</p>
                </InfoRow>
                <InfoRow label="ระยะเวลาส่งมอบ">
                  <p>{{ data.deliveryLeadTime }} {{ data.deliveryLeadTimeTypeLabel }}</p>
                </InfoRow>
                <InfoRow label="วันที่ส่งมอบ">
                  <p>{{ ToDateOnly(data.deliveryDate) }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end lg:col-span-4 mb-2 lg:mb-0">
                <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                  label="เลือก" @click="() => onSelect(data)" />
              </div>
            </div>
          </div>
          <p v-if="!dialogStore.table.data?.length" class="text-center mt-4">ไม่พบข้อมูล</p>

          <div class="py-2">
            <Pagination :page-number="dialogStore.searchCriteria.pageNumber"
              :page-size="dialogStore.searchCriteria.pageSize" :total-record="dialogStore.table.totalRecords"
              @change="dialogStore.fn.onChangePageSize" />
          </div>
        </div>
      </div>
    </template>
  </Dialog>
</template>
