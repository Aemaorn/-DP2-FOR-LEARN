<script setup lang="ts">
import type { TaskItem } from '@/models/DA/da';
import type { TPreProcurement } from '@/models/PP/ppModel';
import { StatusChip } from '@/components';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import { Card } from 'primevue';
import { ref, watch } from 'vue';
import { HttpStatusCode } from 'axios';

import { PreProcurementConstants } from '@/constants';
import GanttChart from './GanttChart.vue';
import Dialog from './Dialog.vue';
import dashboardService from '@/services/DA/dashboard';
import { formatCurrency } from '@/helpers/currency';

const { PreProcurementTypeName } = PreProcurementConstants;

const procurementSelected = ref<TPreProcurement>({} as TPreProcurement);
const showModal = ref(false);
const hasData = ref(false);
const keyword = ref();
const monthList = ref<string[]>([]);
const planData = ref<TaskItem[]>([]);
const preprocurementData = ref<TaskItem[]>([]);
const procurementData = ref<TaskItem[]>([]);
const caData = ref<TaskItem[]>([]);
const cmData = ref<TaskItem[]>([]);

const onSetData = (data: TPreProcurement) => {
  procurementSelected.value = data;

  hasData.value = true;
};

const getDataAsync = async (id: string) => {
  const { data, status } = await dashboardService.getDataAsync(id);

  if (status === HttpStatusCode.Ok) {
    monthList.value = data.axisMonths;
    planData.value = data.phases[0].tasks.map((t: any) => ({ title: t.name, startDate: t.actualStart ? new Date(t.actualStart) : undefined, endDate: t.actualEnd ? new Date(t.actualEnd) : undefined, startAt: t.startIndex } as TaskItem));
    preprocurementData.value = data.phases[1].tasks.map((t: any) => ({ title: t.name, startDate: t.actualStart ? new Date(t.actualStart) : undefined, endDate: t.actualEnd ? new Date(t.actualEnd) : undefined, startAt: t.startIndex } as TaskItem));
    procurementData.value = data.phases[2].tasks.map((t: any) => ({ title: t.name, startDate: t.actualStart ? new Date(t.actualStart) : undefined, endDate: t.actualEnd ? new Date(t.actualEnd) : undefined, startAt: t.startIndex } as TaskItem));
    caData.value = data.phases[3].tasks.map((t: any) => ({ title: t.name, startDate: t.actualStart ? new Date(t.actualStart) : undefined, endDate: t.actualEnd ? new Date(t.actualEnd) : undefined, startAt: t.startIndex } as TaskItem));
    cmData.value = data.phases[4].tasks.map((t: any) => ({ title: t.name, startDate: t.actualStart ? new Date(t.actualStart) : undefined, endDate: t.actualEnd ? new Date(t.actualEnd) : undefined, startAt: t.startIndex } as TaskItem));
  }
};

watch(() => procurementSelected.value, async (newValue) => {
  await getDataAsync(newValue.id);
});
</script>

<template>
  <TitleHeader label="ภาพรวมระยะเวลาดำเนินการจัดซื้อจัดจ้าง" />
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง" />
      <div class="grid lg:grid-cols-3 items-center mt-10 gap-5">
        <InputField label="เลขที่อ้างอิงในระบบ" v-model="keyword" hide-details>
          <template #appendAction>
            <InputGroupAddon>
              <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! w-full h-full"
                @click="() => showModal = true" />
            </InputGroupAddon>
          </template>
        </InputField>
        <InfoItem title="ฝ่าย/สำนัก" :content="procurementSelected.departmentName" v-if="hasData" />
        <div class="flex flex-col" v-if="hasData">
          <small class="text-gray-400">
            {{ procurementSelected.departmentName }}
          </small>
          <div>
            <StatusChip :label="PreProcurementTypeName(procurementSelected.type) ?? ''" size="Medium" color="Info" />
          </div>
        </div>
        <InfoItem title="โครงการ" :content="procurementSelected.name" v-if="hasData" />
        <InfoItem title="ปีงบประมาณ" :content="procurementSelected.budgetYear" v-if="hasData" />
        <InfoItem title="วงเงินงบประมาณ" :content="formatCurrency(procurementSelected.budget)" v-if="hasData" />
        <InfoItem title="วิธีจัดหา" :content="procurementSelected.supplyMethod" v-if="hasData" />
        <InfoItem :content="procurementSelected.supplyMethodType" v-if="hasData" />
        <InfoItem :content="procurementSelected.supplyMethodSpecialType" v-if="hasData" />
      </div>
    </template>
  </Card>
  <div v-if="hasData">
    <div class="flex justify-end mt-5">
      <div class="flex items-center gap-2 px-4 py-1.5 bg-blue-50 border border-blue-200 rounded-full text-base text-blue-800">
        <i class="pi pi-calendar text-blue-500" />
        <span class="font-bold">ข้อมูล ณ วันที่</span>
        <span>{{ new Date().toLocaleDateString('th-TH', { calendar: 'buddhist', year: 'numeric', month: 'long', day: 'numeric' }) }}</span>
      </div>
    </div>
    <GanttChart title="รายการจัดซื้อจัดจ้าง" :data-list="planData" :month-list="monthList" />
    <GanttChart title="Pre-Procurement" :data-list="preprocurementData" :month-list="monthList" />
    <GanttChart title="Procurement" :data-list="procurementData" :month-list="monthList" />
    <GanttChart title="Contract Agreement" :data-list="caData" :month-list="monthList" />
    <GanttChart title="Contract Mangement" :data-list="cmData" class="mb-30" :month-list="monthList" />
  </div>
  <Dialog v-model="showModal" v-model:keyword="keyword" @on-select="(value) => onSetData(value)" />
</template>
