<script setup lang="ts">
import { Pagination } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { InputField, Select } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { Cm007DialogItem } from '@/models/CM/cm007';
import { useCm007DialogStore } from '@/stores/CM/CM007/cm007Dialog';
import { useAuthenticationStore } from '@/stores/authentication';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { Button, Dialog } from 'primevue';
import { Form } from 'vee-validate';
import { onMounted, ref, watch } from 'vue';

const isCollapsed = ref(false);

type Props = {
  defaultDepartment?: boolean;
};

const props = withDefaults(defineProps<Props>(), {
  defaultDepartment: true,
});

const value = defineModel<boolean>({
  default: false,
});

const dialogStore = useCm007DialogStore();
const authStore = useAuthenticationStore();

const onSelect = (item: Cm007DialogItem): void => {
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
    <template #header>
      <TitleHeader label="ค้นหาข้อมูลสัญญา"></TitleHeader>
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
          <Form @submit="dialogStore.fn.getDialogListAsync()">
            <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
              <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="dialogStore.searchCriteria.keyword" />
            </div>
            <div class="grid transition-all duration-300 ease-in-out mt-5"
              :class="isCollapsed ? 'grid-rows-[0fr]' : 'grid-rows-[1fr]'">
              <div class="overflow-hidden">
                <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                  <Select label="ฝ่าย/ภาคเขต" v-model="dialogStore.searchCriteria.departmentCode"
                    :options="dialogStore.departmentDropdown" @enter-close="dialogStore.fn.getDialogListAsync()" />
                  <Select label="วิธีการจัดหา" v-model="dialogStore.searchCriteria.supplyMethodCode"
                    :options="dialogStore.supplyMethodCodeDropdown"
                    @enter-close="dialogStore.fn.getDialogListAsync()" />
                  <Select label="ประเภทวิธีการจัดหา" v-model="dialogStore.searchCriteria.supplyMethodTypeCode"
                    :options="dialogStore.supplyMethodTypeCodeDropdown"
                    @enter-close="dialogStore.fn.getDialogListAsync()"
                    @change="dialogStore.fn.getSupplyMethodSpecialTypeDropdownAsync(dialogStore.searchCriteria.supplyMethodTypeCode)" />
                  <Select v-model="dialogStore.searchCriteria.supplyMethodSpecialTypeCode"
                    :options="dialogStore.supplyMethodSpecialTypeCodeDropdown"
                    @enter-close="dialogStore.fn.getDialogListAsync()" />
                </div>
              </div>
            </div>
            <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
              <ButtonSearch type="submit" class="lg:w-fit w-full" />
              <ButtonClear @click="() => dialogStore.fn.resetCriteria()" class="lg:w-fit w-full" />
            </div>
          </Form>
        </template>
      </Card>

      <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

      <Card v-for="(data, index) in (dialogStore.table.data as Cm007DialogItem[])" :key="index" class="mt-2 border border-gray-300">
        <template #content>
          <div class="grid lg:grid-cols-12 gap-x-4 gap-y-2">
            <div class="lg:col-span-8">
              <InfoRow label="เลขที่ร่างสัญญา">
                {{ data.contractDraftNumber }}
              </InfoRow>
              <InfoRow label="เลขที่สัญญา">
                {{ data.contractNumber }}
              </InfoRow>
              <InfoRow label="เลขที่ PO (SAP)">
                {{ data.poNumber }}
              </InfoRow>
              <InfoRow label="ชื่อสัญญา">
                {{ data.contractName }}
              </InfoRow>
              <InfoRow label="คู่ค้า">
                {{ data.entrepreneurName }}
              </InfoRow>
              <InfoRow label="เลขประจำตัวผู้เสียภาษี">
                {{ data.taxId }}
              </InfoRow>
              <InfoRow label="วงเงินตามสัญญา">
                {{ formatCurrency(data.budget ?? 0) }}
              </InfoRow>
              <InfoRow label="วันที่ลงนามสัญญา">
                {{ ToDateOnly(data.contractSignedDate) }}
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                {{ data.departmentName ?? '-' }}
              </InfoRow>
              <InfoRow label="วิธีการจัดหา">
                {{ data.supplyMethodName ?? '-' }}
              </InfoRow>
            </div>
            <div class="lg:col-span-4 flex flex-col items-end justify-center gap-2">
              <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white" label="เลือก"
                @click="() => onSelect(data)" />
            </div>
          </div>
        </template>
      </Card>
      <p v-if="!dialogStore.table.data?.length" class="text-center mt-4">ไม่พบข้อมูล</p>
      <Pagination :page-number="dialogStore.searchCriteria.pageNumber" :page-size="dialogStore.searchCriteria.pageSize"
        :total-record="dialogStore.table.totalRecords" @change="dialogStore.fn.onChangePageSize" />
    </template>
  </Dialog>
</template>
