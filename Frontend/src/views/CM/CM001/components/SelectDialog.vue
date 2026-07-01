<script setup lang="ts">
import { Pagination } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { InputField, Select } from '@/components/forms';
import { PreProcurementConstants } from '@/constants';
import { souceType } from '@/enums/CM/cm001';
import type { PreProcurementType } from '@/enums/preProcurement';
import { formatCurrency } from '@/helpers/currency';
import type { PlanAndContractVendorData } from '@/models/CM/cm001';
import { useCm001DialogStore } from '@/stores/CM/cm001';
import { TypeBadgeChip } from '@/views/PP/components/PP';
import { Button, Dialog } from 'primevue';
import { Form } from 'vee-validate';
import { onMounted, watch } from 'vue';

const { PreProcurementTypeName } = PreProcurementConstants;

const value = defineModel<boolean>({
  default: false,
});

const dialogStore = useCm001DialogStore();

const onSelect = (item: PlanAndContractVendorData): void => {
  dialogStore.fn.onSelectPlanAndContractVendor(item);
  value.value = false;
};

const getSourceTypeLabel = (sourceType?: string): string => {
  switch (sourceType) {
    case souceType.Plan: return 'แผนฯ จัดซื้อจัดจ้าง';
    case souceType.Procurement: return 'ไม่ทำสัญญา (40) / อื่นๆ';
    case souceType.ContractDraftVendor: return 'ทำสัญญา (41 / 30)';
    case souceType.ContractDraftVendorEdit: return 'สัญญาต่อท้าย';
    default: return '';
  }
};

watch(
  () => [dialogStore.searchCriteria.pageNumber, dialogStore.searchCriteria.pageSize],
  () => {
    dialogStore.fn.getPlanAndContractVendorListAsync();
  }
);

onMounted(async () => {
  await dialogStore.fn.getDepartmentDropdownAsync();
  await dialogStore.fn.getSupplyMethodDropdownAsync();
  await dialogStore.fn.getPlanAndContractVendorListAsync();
  await dialogStore.fn.getSupplyMethodTypeDropdownAsync();
});

watch(value, async (val: boolean) => {
  if (val) {
    await dialogStore.fn.getPlanAndContractVendorListAsync();
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
          <TitleHeader label="ค้นหาข้อมูลแผน/สัญญา"></TitleHeader>
          <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">close</span>
        </div>

        <!-- Scrollable content -->
        <div class="overflow-y-auto px-4">
          <div class="border border-gray-200 rounded-lg p-4 mb-2">
            <Form @submit="dialogStore.fn.getPlanAndContractVendorListAsync()">
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="dialogStore.searchCriteria.keyword" />
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5 mt-5">
                <Select label="ประเภทที่มา" v-model="dialogStore.searchCriteria.sourceType"
                  :options="dialogStore.sourceTypeDropdown"
                  @enter-close="dialogStore.fn.getPlanAndContractVendorListAsync()" />
                <Select label="ฝ่าย/ภาคเขต" v-model="dialogStore.searchCriteria.departmentCode"
                  :options="dialogStore.departmentDropdown"
                  @enter-close="dialogStore.fn.getPlanAndContractVendorListAsync()" />
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5 mt-5">
                <Select label="วิธีการจัดหา" v-model="dialogStore.searchCriteria.supplyMethodCode"
                  :options="dialogStore.supplyMethodCodeDropdown"
                  @enter-close="dialogStore.fn.getPlanAndContractVendorListAsync()" />
                <Select v-model="dialogStore.searchCriteria.supplyMethodTypeCode"
                  :options="dialogStore.supplyMethodTypeCodeDropdown"
                  @enter-close="dialogStore.fn.getPlanAndContractVendorListAsync()"
                  @change="dialogStore.fn.getSupplyMethodSpecialTypeDropdownAsync(dialogStore.searchCriteria.supplyMethodTypeCode)" />
                <Select v-model="dialogStore.searchCriteria.supplyMethodSpecialTypeCode"
                  :options="dialogStore.supplyMethodSpecialTypeCodeDropdown"
                  @enter-close="dialogStore.fn.getPlanAndContractVendorListAsync()" />
              </div>
              <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
                <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
                <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                  @click="() => dialogStore.fn.resetCriteria()" />
              </div>
            </Form>
          </div>

          <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

          <div v-for="(data, index) in (dialogStore.table.data as PlanAndContractVendorData[])" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1 px-3">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 gap-2">
              <div class="lg:col-span-8" v-if="data.sourceType === souceType.Plan">
                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="font-bold">{{ data.planCode }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="ประเภทแผน">
                  <TypeBadgeChip :label="PreProcurementTypeName(data.planType as PreProcurementType)"
                    :color="data.planType as string" class="w-fit" />
                </InfoRow>
                <InfoRow label="โครงการ">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="ปีงบประมาณ">
                  <p>{{ data.budgetYear }}</p>
                </InfoRow>
                <InfoRow label="งบประมาณ">
                  <p>{{ formatCurrency(data.budget ?? 0) }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>{{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}</p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType === souceType.ContractDraftVendor">
                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="font-bold">{{ data.planCode }}</p>
                </InfoRow>
                <InfoRow label="เลขที่สัญญา">
                  <p class="font-bold">{{ data.contractNumber }}</p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>{{ data.poNumber }}</p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="คู่ค้า">
                  <p>{{ data.vendorName }}</p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>{{ formatCurrency(data.contractBudget ?? 0) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>{{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}</p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType === souceType.ContractDraftVendorEdit">
                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="font-bold">{{ data.planCode }}</p>
                </InfoRow>
                <InfoRow label="เลขที่สัญญา">
                  <p class="font-bold">{{ data.contractNumber }}</p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>{{ data.poNumber }}</p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="คู่ค้า">
                  <p>{{ data.vendorName }}</p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>{{ formatCurrency(data.contractBudget ?? 0) }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>{{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}</p>
                </InfoRow>
              </div>
              <div class="lg:col-span-8" v-if="data.sourceType === souceType.Procurement">
                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="font-bold">{{ data.planCode }}</p>
                </InfoRow>
                <InfoRow label="เลขที่โครงการจัดซื้อจัดจ้าง">
                  <p class="font-bold">{{ data.contractNumber }}</p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>{{ data.poNumber }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="ประเภทแผน">
                  <TypeBadgeChip :label="PreProcurementTypeName(data.planType as PreProcurementType)"
                    :color="data.planType as string" class="w-fit" />
                </InfoRow>
                <InfoRow label="โครงการ">
                  <p class="font-bold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="ปีงบประมาณ">
                  <p>{{ data.budgetYear }}</p>
                </InfoRow>
                <InfoRow label="งบประมาณ">
                  <p>{{ formatCurrency(data.budget ?? 0) }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>{{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-1 mr-2">
                  <span class="text-[#1D4ED8] leading-none">&#9679;</span>
                  <p class="whitespace-nowrap text-sm text-[#1D4ED8] font-bold">
                    {{ getSourceTypeLabel(data.sourceType) }}
                  </p>
                </div>
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
