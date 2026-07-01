<script setup lang="ts">
import { Pagination, StatusChip } from '@/components';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { BudgetYearSelect, InputField, Select } from '@/components/forms';
import { ProcurementConstants } from '@/constants';
import { PreProcurementGroupStep } from '@/enums/preProcurement';
import { ProcurementType } from '@/enums/procurement';
import { EWorkProcess } from '@/enums/shared';
import { formatCurrency } from '@/helpers/currency';
import type { TPreProcurement, TPreProcurementCriteria } from '@/models/PP/ppModel';
import { usePPListStore } from '@/stores/PP/ppStore';
import { Button, DataView, Dialog } from 'primevue';
import { Form } from 'vee-validate';
import { ref, watch } from 'vue';
import { useRouter } from 'vue-router';

const emit = defineEmits<{
  (event: 'onSelect', data: TPreProcurement): void;
}>();
const { ProcurementTypeName } = ProcurementConstants;
const isShow = defineModel({ type: Boolean, default: false });
const keyword = defineModel<string | undefined>('keyword', { default: '' });

const initCriteria = {
  pageNumber: 1,
  pageSize: 10,
  sort: [],
  workProcess: EWorkProcess.All,
  groupStep: PreProcurementGroupStep.All,
  procurementType: ProcurementType.Procurement,
} as TPreProcurementCriteria;

const ppStore = usePPListStore();
const isExpanded = ref(false);
const router = useRouter();

const onEnter = (el: Element) => {
  const element = el as HTMLElement;
  element.style.height = '0';
  void element.offsetHeight;
  element.style.height = element.scrollHeight + 'px';
};

const onAfterEnter = (el: Element) => {
  const element = el as HTMLElement;
  element.style.height = 'auto';
};

const onLeave = (el: Element) => {
  const element = el as HTMLElement;
  element.style.height = element.scrollHeight + 'px';
  void element.offsetHeight;
  element.style.height = '0';
};

const onSelectData = (data: TPreProcurement) => {
  isShow.value = false;
  keyword.value = data.procurementNumber;

  emit('onSelect', data);
};

const openDetailInNewTab = (id: string) => {
  const routeData = router.resolve({ path: `/pp/detail/${id}` });
  window.open(routeData.href, '_blank');
};

const onClear = () => {
  ppStore.searchCriteria = structuredClone(initCriteria);
  ppStore.onGetPreProcurementListDataAsync();
};

const onChangePageSizeAsync = (pageNumber: number, pageSize: number) => {
  ppStore.searchCriteria.pageNumber = pageNumber;
  ppStore.searchCriteria.pageSize = pageSize;

  ppStore.onGetPreProcurementListDataAsync();
};

watch(() => isShow.value, (newValue) => {
  if (newValue) {
    ppStore.getDepartmentDDLAsync();
    ppStore.getSupplyMethodDDLAsync();
    ppStore.searchCriteria.workProcess = EWorkProcess.All;
    ppStore.onGetPreProcurementListDataAsync();
  }
});

watch(() => ppStore.searchCriteria.supplyMethodCode, async (value) => {
  if (value) {
    await ppStore.getSupplyMethodSpecialTypeDDLAsync(value);
  }
});

watch(() => ppStore.searchCriteria.keyword, (newValue) => {
  keyword.value = newValue;
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal :style="{ width: '80vw' }" :draggable="false"
    :breakpoints="{ '575px': '90vw' }" maximizable>
    <template #container="{ closeCallback }">
      <div class="h-full overflow-y-auto hide-scrollbar">
        <div class="sticky top-0 bg-white z-10 rounded-lg">
          <div class="p-4">
            <div class="flex gap-2 justify-between items-center">
              <div class="flex gap-2 md:gap-4 items-center w-full">
                <div class="h-4 md:h-7 w-3 md:w-6 bg-primary transform -skew-x-12" />
                <h6 class="font-bold">ค้นหารายการจัดซื้อจัดจ้าง</h6>
                <div class="h-px bg-gray-300 flex-1" />
              </div>
              <div class="flex items-center gap-2">
                <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">
                  close
                </span>
              </div>
            </div>
            <Form class="my-4" @submit="ppStore.onGetPreProcurementListDataAsync">
              <div class="relative">
                <Transition name="button-slide" mode="out-in">
                  <div v-show="true" key="top" class="flex justify-end p-2">
                    <button type="button" @click="isExpanded = !isExpanded"
                      class="text-primary hover:text-primary-600 transition-all duration-300 flex items-center gap-2 text-sm font-medium">
                      <i :class="`text-primary pi ${isExpanded ? 'pi-chevron-up' : 'pi-chevron-down'}`"
                        style="font-size: 1.3rem" />
                    </button>
                  </div>
                </Transition>
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-5 gap-2 mt-2 items-start">
                <InputField class="col-span-1 lg:col-span-3" label="คำค้นหา"
                  v-model.trim="ppStore.searchCriteria.keyword" hide-details />
                <Transition name="button-slide" mode="out-in">
                  <div v-if="!isExpanded" key="buttons-top"
                    class="md:col-start-5 md:flex gap-2 justify-end items-start">
                    <ButtonSearch type="submit" />
                    <ButtonClear @click="onClear" />
                  </div>
                </Transition>
              </div>

              <Transition name="expand" @enter="onEnter" @after-enter="onAfterEnter" @leave="onLeave">
                <div v-if="isExpanded" class="overflow-hidden">
                  <div class="pt-4 space-y-10">
                    <div class="grid grid-cols-1 lg:grid-cols-5 gap-2 mt-10">
                      <Select :options="ppStore.departmentDropdown" label="ฝ่าย/ภาคเขต"
                        v-model="ppStore.searchCriteria.departmentCode" hide-details />
                      <BudgetYearSelect label="ปีงบประมาณ" v-model="ppStore.searchCriteria.budgetYear" not-set-default
                        hide-details />
                    </div>
                    <div class="grid grid-cols-1 lg:grid-cols-5 gap-2">
                      <Select :options="ppStore.supplyMethodDropdown" label="วิธีการจัดหา"
                        v-model="ppStore.searchCriteria.supplyMethodCode" hide-details />
                      <Select :options="ppStore.supplyMethodTypeDropdown"
                        v-model="ppStore.searchCriteria.supplyMethodTypeCode" hide-details />
                      <Select :options="ppStore.supplyMethidSpecialTypeDropDown"
                        v-model="ppStore.searchCriteria.supplyMethodSpecialTypeCode" hide-details />
                      <div class="col-span-2" v-if="isExpanded" key="buttons-bottom">
                        <div class="md:col-start-5 md:flex gap-2 justify-end items-start">
                          <ButtonSearch type="submit" />
                          <ButtonClear @click="onClear" />
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </Transition>
            </Form>
          </div>
        </div>
        <div class="p-4">
          <DataView class="mt-5" :value="ppStore.table.data" data-key="id">
            <template #list="{ items }">
              <div v-for="(data, index) in items" :key="index"
                class="border-1 border-gray-300 rounded-sm mb-4 p-1 pl-2 pr-4">
                <div class="grid lg:grid-cols-12 gap-2">
                  <div class="lg:col-span-8">
                    <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                      <p class="underline text-blue-500 cursor-pointer hover:text-blue-600"
                        @click="openDetailInNewTab(data.id)">{{ data.procurementNumber }}</p>
                    </InfoRow>
                    <InfoRow label="ชื่อโครงการ">
                      <p class="font-bold">{{ data.name }}</p>
                    </InfoRow>
                    <InfoRow label="วงเงินงบประมาณ">
                      <p>{{ formatCurrency(data.budget) }}</p>
                    </InfoRow>
                    <InfoRow label="ประเภทแผน">
                      <StatusChip :label="ProcurementTypeName(data.type) ?? ''" size="Medium" color="Info" />
                    </InfoRow>
                    <InfoRow label="ฝ่าย/ภาคเขต">
                      <p>{{ data.departmentName }}</p>
                    </InfoRow>
                    <InfoRow label="วิธีการจัดหา">
                      <p>{{ data.supplyMethod }}</p>
                    </InfoRow>
                  </div>
                  <div class="flex flex-col justify-center items-end lg:col-span-4">
                    <Button class="w-full mb-4 lg:mb-0 lg:max-w-[70px]" label="เลือก" severity="primary"
                      variant="outlined" @click="() => onSelectData(data)" />
                  </div>
                </div>
              </div>
            </template>
            <template #empty>
              <p class="text-center font-bold">ไม่พบข้อมูล</p>
            </template>
          </DataView>
        </div>
      </div>
      <div class="mb-4">
        <Pagination :pageNumber="ppStore.searchCriteria.pageNumber" :pageSize="ppStore.searchCriteria.pageSize"
          :total-record="ppStore.table.totalRecords" @change="onChangePageSizeAsync" />
      </div>
    </template>
  </Dialog>
</template>
