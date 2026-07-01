<script setup lang="ts">
import { Dialog, Button } from 'primevue';
import { InputField, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '../Pagination.vue';
import { useUserDialogStore } from '@/stores/Shared/userDialog';
import { watch, ref, computed } from 'vue';
import { useRoute } from 'vue-router';
import { Form as VeeForm } from 'vee-validate';
import InfoRow from '../cosmetic/InfoRow.vue';
import { storeToRefs } from 'pinia';
import { PlanDepartmentCode } from '@/enums/plan';

const store = useUserDialogStore();
const { searchCriteria, table, isShow, isByDepartment } = storeToRefs(store);
const { onGetUserListDataAsync, onResetCriteria, onChangePageSize, onSelected, onClosed } = store;
const route = useRoute();
const isCollapsed = ref(false);

const filteredData = computed(() =>
  table.value.data?.filter((x: any) => x.departmentCode !== PlanDepartmentCode.InActive)
);

watch(() => searchCriteria.value.groupCode, (newVal) => {
  if (newVal) {
    searchCriteria.value.lineCode = undefined;
    searchCriteria.value.departmentCode = undefined;
  }
});

watch(() => searchCriteria.value.lineCode, (newVal) => {
  if (newVal) {
    searchCriteria.value.groupCode = undefined;
    searchCriteria.value.departmentCode = undefined;
  }
});

watch(() => searchCriteria.value.departmentCode, (newVal) => {
  if (newVal) {
    searchCriteria.value.groupCode = undefined;
    searchCriteria.value.lineCode = undefined;
  }
});

watch(() => [route.path], () => {
  onClosed();
});

watch(() => [searchCriteria.value.pageNumber, searchCriteria.value.pageSize], async () => {
  if (isShow.value) {
    await onGetUserListDataAsync();
  }
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal :style="{ width: '80vw' }" :draggable="false"
    :breakpoints="{ '575px': '90vw' }" maximizable @after-hide="onClosed">
    <template #container="{ closeCallback, maximizeCallback }">
      <div class="flex flex-col bg-white rounded-2xl max-h-[90vh] overflow-hidden">
        <!-- Header -->
        <div class="flex items-center justify-between p-4 shrink-0">
          <TitleHeader label="รายชื่อ"></TitleHeader>
          <div class="flex items-center gap-2">
            <span
              class="material-symbols-outlined text-gray-500 border-[0.5px] border-gray-500 rounded-md cursor-pointer"
              @click="maximizeCallback">
              expand_content
            </span>
            <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">
              close
            </span>
          </div>
        </div>

        <!-- Criteria (fixed) -->
        <div class="px-4 shrink-0">
          <Card>
            <template #title>
              <div v-if="!isByDepartment" class="flex items-center justify-end cursor-pointer"
                @click="isCollapsed = !isCollapsed">
                <i class="pi text-primary transition-transform duration-200"
                  :class="isCollapsed ? 'pi-chevron-down' : 'pi-chevron-up'" />
              </div>
            </template>
            <template #content>
              <VeeForm @submit="onGetUserListDataAsync">
                <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                  <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="searchCriteria.searchText" />
                </div>
                <div v-if="!isByDepartment" class="grid transition-all duration-300 ease-in-out mt-5"
                  :class="isCollapsed ? 'grid-rows-[0fr]' : 'grid-rows-[1fr]'">
                  <div class="overflow-hidden">
                    <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                      <Select label="กลุ่มงาน" v-model="searchCriteria.groupCode" :options="store.dropdowns.group"
                        @enter-close="onGetUserListDataAsync" />
                      <Select label="สายงาน" v-model="searchCriteria.lineCode" :options="store.dropdowns.line"
                        @enter-close="onGetUserListDataAsync" />
                      <Select label="ฝ่าย/ภาคเขต" v-model="searchCriteria.departmentCode"
                        :options="store.dropdowns.department" @enter-close="onGetUserListDataAsync" />
                    </div>
                  </div>
                </div>
                <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
                  <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
                  <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                    @click="onResetCriteria" />
                </div>
              </VeeForm>
            </template>
          </Card>
        </div>

        <!-- Results (scrollable) -->
        <div class="flex-1 overflow-y-auto px-4 min-h-0">
          <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

          <Card v-for="(item, index) in filteredData" :key="index" class="mt-2 border border-gray-300">
            <template #content>
              <div class="grid lg:grid-cols-12 gap-x-4 gap-y-2">
                <div class="lg:col-span-8">
                  <InfoRow label="ชื่อ-นามสกุล">
                    <p class="font-bold">{{ item.name }}</p>
                  </InfoRow>
                  <InfoRow label="ตำแหน่ง">
                    <p>{{ item.positionName }}</p>
                  </InfoRow>
                  <InfoRow label="ฝ่าย/ภาคเขต">
                    <p>{{ item.departmentName }}</p>
                  </InfoRow>
                </div>
                <div class="lg:col-span-4 flex flex-col items-end justify-center gap-2">
                  <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                    label="เลือก" @click="() => onSelected(item)" />
                </div>
              </div>
            </template>
          </Card>
          <p v-if="!filteredData?.length" class="text-center mt-4">ไม่พบข้อมูล</p>
        </div>

        <!-- Pagination (fixed) -->
        <div class="px-4 py-2 shrink-0">
          <Pagination :page-number="searchCriteria.pageNumber" :page-size="searchCriteria.pageSize"
            :total-record="table.totalRecords" @change="onChangePageSize" />
        </div>
      </div>
    </template>
  </Dialog>
</template>
