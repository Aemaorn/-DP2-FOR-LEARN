<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';

const isCollapsed = ref(false);
import { Button, Dialog } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Radio, Select, StatusGroupButton } from '@/components/forms';
import { YearOptions } from '@/constants/date';
import PlanConstant from '@/constants/plan';
import PreProcurementConstants from '@/constants/preProcurement';
import type { PlanStatus } from '@/enums/plan';
import { usePPListDialogStore } from '../../../../stores/PP/ppStore';
import { TypeBadgeChip } from './index';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { formatCurrency } from '@/helpers/currency';
import { Form } from 'vee-validate';
import { useAuthenticationStore } from '@/stores/authentication';
import { DepartmentId } from '@/enums/businessUnit';

const value = defineModel<boolean>({
  default: false,
});
const listDialogStore = usePPListDialogStore();
const authStore = useAuthenticationStore();
const { PreProcurementTypeOptions, PreProcurementTypeName } =
  PreProcurementConstants;
const isJorPorDepartmentCode = computed(() => {
  return authStore.profile.departmentCode === DepartmentId.JorPor;
});

const onSelectPlan = (index: number, isJorPorDepartmentCode: boolean): void => {
  listDialogStore.onSelectData(index, isJorPorDepartmentCode);

  value.value = false;
};

const { PlanStatusColor, PlanStatusName } = PlanConstant;

const getStatusColor = (status: PlanStatus): string => {
  const bg = PlanStatusColor(status as PlanStatus)?.bgColorClass;
  const text = PlanStatusColor(status as PlanStatus)?.textColorClass;

  return `${bg} ${text}`;
};

watch(
  () => [listDialogStore.searchCriteria.pageNumber, listDialogStore.searchCriteria.pageSize],
  () => {
    listDialogStore.onGetProcurementDialogListDataAsync();
  }
);

onMounted(async () => {
  await listDialogStore.getDepartmentDDLAsync();
  await listDialogStore.getSupplyMethodDDLAsync();
  await listDialogStore.onGetProcurementDialogListDataAsync();
  await listDialogStore.getSupplyMethodTypeDDLAsync();
});

watch(value, async (val: boolean) => {
  if (val) {
    listDialogStore.searchCriteria.departmentCode = authStore.profile.departmentCode;

    await listDialogStore.onGetProcurementDialogListDataAsync();
  }
});

watch(() => listDialogStore.searchCriteria.supplyMethodCode, async (value) => {
  if (value) {
    await listDialogStore.getSupplyMethodSpecialTypeDDLAsync(value);
  }
});

const supplyMethodText = (supplyMethod: string, supplyMethodType?: string, supplyMethodSpecialType?: string) => {
  if (supplyMethodType && supplyMethodSpecialType) {
    return `${supplyMethod} ${supplyMethodType} ${supplyMethodSpecialType}`;
  }

  if (supplyMethodType) {
    return `${supplyMethod} ${supplyMethodType}`;
  }

  if (supplyMethodSpecialType) {
    return `${supplyMethod} ${supplyMethodSpecialType}`;
  }

  return `${supplyMethod}`;
};

const openPlanDetail = (id: string | number) => {
  window.open(`/pl/pl001/detail/${id}`, '_blank');
};
</script>

<template>
  <Dialog v-model:visible="value" modal :draggable="false" :style="{ width: '90vw' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }" @hide="() => (value = false)">
    <template #header>
      <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง"></TitleHeader>
    </template>
    <template #default>
      <Card>
        <template #title>
          <div class="flex items-center justify-end cursor-pointer" @click="isCollapsed = !isCollapsed">
            <i class="pi text-primary transition-transform duration-200"
              :class="isCollapsed ? 'pi-chevron-down' : 'pi-chevron-up'" />
          </div>
        </template>
        <template #content>
          <Form @submit="listDialogStore.onGetProcurementDialogListDataAsync()">
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-2 gap-y-5">
              <InputField label="คำค้นหา" v-model.trim="listDialogStore.searchCriteria.keyword" />
            </div>
            <Radio class="mt-2" v-model="listDialogStore.searchCriteria.type" :options="PreProcurementTypeOptions" />
            <div class="grid transition-all duration-300 ease-in-out mt-5"
              :class="isCollapsed ? 'grid-rows-[0fr]' : 'grid-rows-[1fr]'">
              <div class="overflow-hidden">
                <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                  <Select @enter-close="listDialogStore.onGetProcurementDialogListDataAsync()" label="ฝ่าย/ภาคเขต"
                    v-model="listDialogStore.searchCriteria.departmentCode"
                    :options="listDialogStore.departmentDropdown" :disabled="!isJorPorDepartmentCode" />
                  <Select @enter-close="listDialogStore.onGetProcurementDialogListDataAsync()" label="ปีงบประมาณ"
                    v-model="listDialogStore.searchCriteria.budgetYear" :options="YearOptions" />
                </div>
                <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5 mt-5">
                  <Select @enter-close="listDialogStore.onGetProcurementDialogListDataAsync()" label="วิธีการจัดหา"
                    v-model="listDialogStore.searchCriteria.supplyMethodCode"
                    :options="listDialogStore.supplyMethodDropdown" />
                  <Select @enter-close="listDialogStore.onGetProcurementDialogListDataAsync()"
                    v-model="listDialogStore.searchCriteria.supplyMethodTypeCode"
                    :options="listDialogStore.supplyMethodTypeDropdown" />
                  <Select @enter-close="listDialogStore.onGetProcurementDialogListDataAsync()" label="วิธี"
                    v-model="listDialogStore.searchCriteria.supplyMethodSpecialTypeCode"
                    :options="listDialogStore.supplyMethidSpecialTypeDropDown" />
                </div>
              </div>
            </div>
            <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
              <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
              <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                @click="() => listDialogStore.onResetCriteriaDialog()" />
            </div>
          </Form>
        </template>
      </Card>

      <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

      <StatusGroupButton :optionBadges="listDialogStore.statusOptionBadge"
        v-model="listDialogStore.searchCriteria.groupStep" class="mb-2" />

      <Card v-for="(data, index) in listDialogStore.table.data" :key="data.id" class="mt-2 border border-gray-300">
        <template #content>
          <div class="grid lg:grid-cols-12 gap-x-4 gap-y-2">
            <div class="lg:col-span-8">
              <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                <p class="underline text-blue-400 hover:cursor-pointer" @click="openPlanDetail(data.id)">
                  {{ data.planNumber || '-' }}
                </p>
              </InfoRow>
              <InfoRow label="ชื่อโครงการ">
                <p class="font-bold">{{ data.planName }}</p>
              </InfoRow>
              <InfoRow label="วงเงินงบประมาณ">
                <p>{{ formatCurrency(data.budget) }}</p>
              </InfoRow>
              <InfoRow label="ประเภทแผน">
                <TypeBadgeChip :label="PreProcurementTypeName(data.type)" size="Small" :color="data.type" />
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                <p>{{ data.departmentName }}</p>
              </InfoRow>
              <InfoRow label="วิธีจัดหา">
                <p>{{
                  supplyMethodText(
                    data.supplyMethod,
                    data.supplyMethodType,
                    data.supplyMethodSpecialType
                  )
                }}</p>
              </InfoRow>
            </div>
            <div class="lg:col-span-4 flex flex-col items-end justify-between gap-2">
              <div class="flex items-center gap-2 mt-2">
                <p class="text-sm">สถานะ :</p>
                <Chip :label="PlanStatusName(data.status as unknown as PlanStatus)" class="rounded-4xl text-md"
                  :class="getStatusColor(data.status as unknown as PlanStatus)" />
                <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                  label="เลือก" @click="() => onSelectPlan(index as number, isJorPorDepartmentCode)" />
              </div>
            </div>
          </div>
        </template>
      </Card>
      <p v-if="!listDialogStore.table.data?.length" class="text-center mt-4">ไม่พบข้อมูล</p>
      <Pagination :page-number="listDialogStore.searchCriteria.pageNumber"
        :page-size="listDialogStore.searchCriteria.pageSize" :total-record="listDialogStore.table.totalRecords"
        @change="listDialogStore.onChangePageSizePPListDialog" />
    </template>
  </Dialog>
</template>
