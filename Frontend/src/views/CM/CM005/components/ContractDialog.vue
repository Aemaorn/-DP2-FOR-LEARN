<script setup lang="ts">
import type { ContractVendorData } from '@/models/CM/cm005';
import { Dialog, Button } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select } from '@/components/forms';
import { Pagination } from '@/components';
import { watch, onMounted } from 'vue';
import { useContractDialogStore } from '@/stores/CM/cm005';
import { formatCurrency } from '@/helpers/currency';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { Form } from 'vee-validate';
import { souceType } from '@/enums/CM/cm001';
import { PreProcurementConstants } from '@/constants';
import type { PreProcurementType } from '@/enums/preProcurement';
import { TypeBadgeChip } from '@/views/PP/components/PP';

const { PreProcurementTypeName } = PreProcurementConstants;

const showModal = defineModel('show', { type: Boolean, required: true, default: false });

const emit = defineEmits<{ onSelect: [data: ContractVendorData] }>();

const store = useContractDialogStore();

const onSelectContract = (dataSelected: ContractVendorData) => {
  emit('onSelect', dataSelected);
  showModal.value = false;
};

watch(
  () => [store.dialogCriteria.pageNumber, store.dialogCriteria.pageSize],
  () => {
    store.getContractListAsync();
  }
);

watch(() => showModal.value, async (val) => {
  if (val) {
    await store.getContractListAsync();
  } else {
    store.resetData();
  }
});

watch(() => store.dialogCriteria.supplyMethodCode, async (value) => {
  if (value) {
    await store.getSupplyMethodSpecialTypeDropdownAsync(value);
  }
});

onMounted(async () => {
  await Promise.all([
    store.getDepartmentDropdownAsync(),
    store.getSupplyMethodDropdownAsync(),
    store.getSupplyMethodTypeDropdownAsync(),
    store.getContractListAsync(),
  ]);
});
</script>

<template>
  <Dialog v-model:visible="showModal" modal :style="{ width: '90vw' }" :draggable="false"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }" @hide="() => (showModal = false)">
    <template #container="{ closeCallback }">
      <div class="flex flex-col bg-white rounded-2xl max-h-[90vh]">
        <!-- Header -->
        <div class="flex items-center justify-between p-4 shrink-0">
          <TitleHeader label="ค้นหาสัญญา"></TitleHeader>
          <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">close</span>
        </div>

        <!-- Scrollable content -->
        <div class="overflow-y-auto px-4">
          <div class="border border-gray-200 rounded-lg p-4 mb-2">
            <Form @submit="store.getContractListAsync()">
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="store.dialogCriteria.keyword" />
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5 mt-5">
                <Select label="ประเภทที่มา" v-model="store.dialogCriteria.sourceType"
                  :options="store.sourceTypeDropdown"
                  @enter-close="store.getContractListAsync()" />
                <Select label="ฝ่าย/ภาคเขต" v-model="store.dialogCriteria.departmentCode"
                  :options="store.departmentDropdown"
                  @enter-close="store.getContractListAsync()" />
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5 mt-5">
                <Select label="วิธีการจัดหา" v-model="store.dialogCriteria.supplyMethodCode"
                  :options="store.supplyMethodCodeDropdown"
                  @enter-close="store.getContractListAsync()" />
                <Select v-model="store.dialogCriteria.supplyMethodTypeCode"
                  :options="store.supplyMethodTypeCodeDropdown"
                  @enter-close="store.getContractListAsync()"
                  @change="store.getSupplyMethodSpecialTypeDropdownAsync(store.dialogCriteria.supplyMethodTypeCode)" />
                <Select v-model="store.dialogCriteria.supplyMethodSpecialTypeCode"
                  :options="store.supplyMethodSpecialTypeCodeDropdown"
                  @enter-close="store.getContractListAsync()" />
              </div>
              <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
                <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
                <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                  @click="store.resetCriteria()" />
              </div>
            </Form>
          </div>

          <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

          <div v-for="(data, index) in (store.dialogDataList?.data as ContractVendorData[])" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1 px-3">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 gap-2">
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
              <div class="flex items-start justify-end lg:col-span-4 mb-2 lg:mb-0">
                <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                  label="เลือก" @click="() => onSelectContract(data)" />
              </div>
            </div>
          </div>
          <p v-if="!store.dialogDataList?.data?.length" class="text-center mt-4">ไม่พบข้อมูล</p>

          <div class="py-2">
            <Pagination :page-number="store.dialogCriteria.pageNumber" :page-size="store.dialogCriteria.pageSize"
              @change="store.onChangePageSize" :total-record="store.dialogDataList?.totalRecords ?? 0" />
          </div>
        </div>
      </div>
    </template>
  </Dialog>
</template>
