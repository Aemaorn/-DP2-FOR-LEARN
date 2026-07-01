<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { BadgeStatus as BadgeComponent } from '@/components';
import { CriteriaGroupButton, InputField, Select, Datepicker, StatusGroupButton } from '@/components/forms';
import Pagination from '@/components/Pagination.vue';
import { Card, Button } from 'primevue';
import SharedConstants from '@/constants/shared';
import { useAc01ListStore } from '@/stores/AC/ac01';
import type { TAC01List } from '@/models/ACC/acc001';
import { ToDateOnly } from '@/helpers/dateTime';
import { useRouter } from 'vue-router';
import { onBeforeMount, onMounted, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import AC01Helper from '@/helpers/AC/ac01';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import type { Option } from '@/models/shared/option';
import type { sourceType } from '@/enums/AC/ac01';
import { formatCurrency } from '@/helpers/currency';
import { Form } from 'vee-validate';

const router = useRouter();
const listStore = useAc01ListStore();
const { dataResponse, searchCriteria, statusOptionBadge, deparmentDropdown } = storeToRefs(listStore);
const { onChangePageSize, onClearCriteria, onGetListData, onGetDropdownAsync } = listStore;
const { BadgeStatus, SourceTypeName } = AC01Helper;

const typeDropdown = ref<Option[]>([
  {
    label: 'ว 119',
    value: 'W119'
  },
  {
    label: '79 วรรค 2',
    value: 'Clause79_2'
  },
  {
    label: 'การเบิกจ่าย',
    value: 'Disbursement'
  },
  {
    label: 'คืนหลักประกันสัญญา',
    value: 'ContractGuaranteeReturn'
  },
  {
    label: 'เงินสดย่อย',
    value: 'PettyCashReimbursement'
  },
]);

onBeforeMount(async () => {
  await onGetDropdownAsync();
})

onMounted(async () => {
  await onGetListData();
  onWatchCriteria();
});

const onWatchCriteria = () => {
  watch(() => [
    searchCriteria.value.pageNumber,
    searchCriteria.value.pageSize,
    searchCriteria.value.workProcess,
    searchCriteria.value.status], async () => {
      await onGetListData();
    });
};
</script>

<template>
  <TitleHeader label="การเบิกจ่าย">
  </TitleHeader>
  <Form @submit="onGetListData">
    <Card class="my-4">
      <template #content>
        <CriteriaGroupButton :options="SharedConstants.WorkProcessOptions"
          v-model="listStore.searchCriteria.workProcess" />
        <div class="mt-8 space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="คำค้นหา" v-model.trim="searchCriteria.keyword" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-2">
            <Select label="ฝ่าย/ภาคเขต" v-model="searchCriteria.departmentCode" :options="deparmentDropdown"
              hide-details />
            <Select label="ประเภทงาน" v-model="searchCriteria.sourceType" :options="typeDropdown" hide-details />
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-2">
            <Datepicker label="วันที่ส่งเอกสาร ตั้งแต่" v-model="searchCriteria.dateFrom" hide-details />
            <Datepicker label="ถึง" v-model="searchCriteria.dateTo" hide-details />

            <Datepicker label="วันที่เบิกจ่าย ตั้งแต่" v-model="searchCriteria.advancePaymentDateFrom" hide-details />
            <Datepicker label="ถึง" v-model="searchCriteria.advancePaymentDateTo" hide-details />
          </div>
        </div>
        <div class="flex gap-2 items-center justify-start md:justify-end">
          <Button label="ค้นหา" icon="pi pi-search" type="submit" />
          <Button label="ล้าง" icon="pi pi-eraser" variant="outlined" @click="() => onClearCriteria()" />
        </div>
      </template>
    </Card>
    <Card>
      <template #content>
        <StatusGroupButton :optionBadges="statusOptionBadge" v-model="searchCriteria.status"
          @update:model-value="onGetListData" />
        <DataView :value="dataResponse.data.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in (items as TAC01List[])" :key="index"
              class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่อ้างอิง">
                    <p class="underline text-blue-400"> {{ data.sourceCode }} </p>
                  </InfoRow>
                  <InfoRow label="เรื่อง">
                    <p class="font-bold"> {{ data.sourceName }} </p>
                  </InfoRow>
                  <InfoRow label="จำนวนเงิน">
                    <p> {{ formatCurrency(data.budget) }} </p>
                  </InfoRow>
                  <InfoRow label="ฝ่าย/ภาคเขต">
                    <p> {{ data.departmentName }} </p>
                  </InfoRow>
                  <InfoRow label="ประเภทงาน">
                    <p>
                      <StatusChip color="gray" :label="SourceTypeName(data.sourceType as sourceType) ?? ''"
                        class="w-fit text-center justify-center" />
                    </p>
                  </InfoRow>
                  <InfoRow label="วันที่จัดส่งเอกสาร">
                    <p> {{ ToDateOnly(data.date) }} </p>
                  </InfoRow>
                  <InfoRow label="วันที่เบิกจ่าย">
                    <p> {{ ToDateOnly(data.advancePaymentDate) }} </p>
                  </InfoRow>
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4">
                  <div class="flex items-center gap-2 mt-2 mr-2">
                    <p class="text-sm">สถานะ :</p>
                    <BadgeComponent :color="BadgeStatus(data.status).color" :label="BadgeStatus(data.status).label" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text"
                    @click="() => router.push({ name: 'ac01Detail', params: { id: data.id } })" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="searchCriteria.pageNumber" :page-size="searchCriteria.pageSize"
          :total-record="dataResponse.data.totalRecords" @change="onChangePageSize" />
      </template>
    </Card>
  </Form>
</template>