<script setup lang="ts">
import type { CmData, StatusCount } from '@/models/CM/cm';
import { BadgeStatus, Pagination } from '@/components';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { CriteriaGroupButton, InputField, Select, Datepicker, StatusGroupButton } from '@/components/forms';
import { SharedConstants } from '@/constants';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { Card, DataView } from 'primevue';
import { useCm004ListStore } from '@/stores/CM/cm004';
import { onMounted, ref, watch } from 'vue';
import { Form } from 'vee-validate';
import router from '@/router';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import type { ColorLabel } from '@/models/shared/color';

const store = useCm004ListStore();
const { WorkProcessOptions } = SharedConstants;

const badgeOptions = ref([
  { bgColorClass: 'bg-gray-400', count: 0, label: 'ทั้งหมด', textColorClass: 'text-black', value: 'all' },
  { bgColorClass: 'bg-yellow-400', count: 0, label: 'อยู่ระหว่างดำเนินการ', textColorClass: 'text-white', value: 'inProgress' },
  { bgColorClass: 'bg-green-400', count: 0, label: 'ดำเนินการแล้วเสร็จ', textColorClass: 'text-white', value: 'completed' },
]);

onMounted(() => {
  initAsync();
});

const initAsync = async (): Promise<void> => {
  await store.getDepartmentDDLAsync();
  await store.getListAsync();

  updateBadgeCounts();
};

const updateBadgeCounts = (): void => {
  badgeOptions.value = badgeOptions.value.map(option => ({
    ...option,
    count: store.dataList.statusCount[option.value as keyof StatusCount] ?? 0,
  }));
};

const routeToDetail = (id: string): void => {
  const route = `/cm/cm004/detail/${id}`;

  router.push(route);
};

const getStatus = (status: string): ColorLabel => {
  if (status === 'InProgress') {
    return { color: 'yellow', label: 'อยู่ระหว่างดำเนินการ' };
  }

  return { color: 'green', label: 'ดำเนินการแล้วเสร็จ' };
};

const onChangeStartDate = (date?: Date) => {
  if (!date) {
    store.criteria.endContractSignedDate = undefined;
  }
};

const onRouteToProcurement = (selectedItem: CmData) => {
  if (selectedItem.procurementType === 'Rent') {
    router.push({ name: 'pcm005Detail', params: { id: selectedItem.procurementId } });

    return;
  }

  router.push({ name: 'ppDetail', params: { id: selectedItem.procurementId } });
};

watch(() => [store.criteria.workProcess, store.criteria.pageSize, store.criteria.pageNumber, store.criteria.pageSize, store.criteria.status], async () => {
  await store.getListAsync();
});
</script>

<template>
  <TitleHeader label="ขออนุมัติเบิกจ่าย" />
  <Card class="mb-4">
    <template #content>
      <Form @submit="store.getListAsync">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="store.criteria.workProcess" />
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-2">
            <InputField label="คำค้นหา" class="lg:col-span-3" v-model.trim="store.criteria.keyword"
              hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select :options="store.departmentDDL" label="ฝ่าย"
              v-model="store.criteria.departmentCode" hide-details @enterClose="store.getListAsync" />
            <Datepicker label="วันที่ลงนามในสัญญา ตั้งแต่"
              v-model="store.criteria.startContractSignedDate" hide-details @on-selected="onChangeStartDate" />
            <Datepicker label="ถึงวันที่" v-model="store.criteria.endContractSignedDate"
              hide-details :disabled="!store.criteria.startContractSignedDate"
              :min-date="store.criteria.startContractSignedDate" />
          </div>
          <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end">
            <ButtonSearch class="lg:w-fit w-full" type="submit" />
            <ButtonClear class="lg:w-fit w-full" @click="store.onResetAsync" />
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <StatusGroupButton :optionBadges="badgeOptions" v-model="store.criteria.status"
        @update:model-value="store.getListAsync" />
      <DataView :value="store.dataList?.data?.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as CmData[])" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1 px-3">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 gap-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่สัญญา">
                  <p class="underline text-blue-400 cursor-pointer" @click="() => onRouteToProcurement(data)">
                    {{ data.contractNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p class="font-bold">
                    {{ data.poNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">
                    {{ data.contractName }}
                  </p>
                </InfoRow>
                <InfoRow label="คู่สัญญา">
                  <p>
                    {{ data.entrepreneurName === '' ? '-' : data.entrepreneurName }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ลงนามในสัญญา">
                  <p>
                    {{ ToDateOnly(data.contractSignedDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>
                <InfoRow label="ประเภทสัญญา">
                  <p>
                    {{ data.contractTypeLabel }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus :color="getStatus(data.status).color" :label="getStatus(data.status).label" />
                </div>
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  variant="text" @click="() => routeToDetail(data.id ?? '')" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
        :total-record="store.dataList.data?.totalRecords" @change="store.onChangePageSizeAsync" />
    </template>
  </Card>
</template>
