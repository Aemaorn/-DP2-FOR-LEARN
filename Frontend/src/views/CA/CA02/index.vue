<script setup lang="ts">
import { DataView } from 'primevue';
import { BadgeStatus as BadgeStatusComponent } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, StatusGroupButton, CriteriaGroupButton } from '@/components/forms';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { useCA02ListStore } from '@/stores/CA/ca02';
import { SharedConstants } from '@/constants';
import { onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import type { TCA02List } from '@/models/CA/ca02';
import { CA02Helper } from '@/helpers/CA/ca02';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { CA02Status } from '@/enums/CA/ca02';

const router = useRouter();
const listStore = useCA02ListStore();
const { searchCriteria, table, statusOptions } = storeToRefs(listStore);
const { fn: { onChangePageSize, onGetListAsync, onResetCriteria, onDeleteAsync, } } = listStore;
const { WorkProcessOptions } = SharedConstants;
const { BadgeStatus, MapCA02StatusColor } = CA02Helper;

onMounted(async (): Promise<void> => {
  await onGetListAsync();
  onWatchCriteria();
});

const onWatchCriteria = () => {
  watch(() => [
    searchCriteria.value.pageNumber,
    searchCriteria.value.pageSize,
    searchCriteria.value.workProcess,
    searchCriteria.value.status], async () => {
      await onGetListAsync();
    });
};
</script>

<template>
  <TitleHeader label="ขอใบรับรองผลงาน">
    <template #action>
      <Button label="สร้างใบรับรองผลงาน" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'ca02Detail' })" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="onGetListAsync">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="listStore.searchCriteria.workProcess" />
        <div class="mt-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <InputField label="คำค้นหา" v-model.trim="listStore.searchCriteria.keyword" class="lg:col-span-3 w-full" />
            <div class="lg:col-span-2 flex items-end justify-end gap-2">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="onResetCriteria" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card>
    <template #content>
      <StatusGroupButton :option-badges="statusOptions" v-model="searchCriteria.status" />
      <DataView :value="table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as Array<TCA02List>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่ใบรับรองผลงาน">
                  <p class="underline text-blue-400 cursor-pointer"
                    @click="router.push({ name: 'ca02Detail', params: { contractDraftVendorId: data.contractDraftVendorId, id: data.id } })">
                    {{ data.certificateNo }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p class="font-bold">
                    {{ data.poNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p>
                    {{ data.contractName }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ลงนามในสัญญา">
                  <p>
                    {{ ToDateOnly(data.contractSignedDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="คู่สัญญา">
                  <p>
                    {{ `${data.vendorCode} : ${data.vendorName}` }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatusComponent :label="BadgeStatus(data.status).label"
                    :bg-color-class="MapCA02StatusColor(data.status).bgColorClass"
                    :text-color-class="MapCA02StatusColor(data.status).textColorClass" />
                </div>
                <Button icon="pi pi-pen-to-square"
                  class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20! mr-1" size="small" variant="text"
                  @click="() => router.push({ name: 'ca02Detail', params: { id: data.id } })" />
                <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="() => onDeleteAsync(data.contractDraftVendorId, data.id)"
                  v-if="[CA02Status.Draft, CA02Status.Edit, CA02Status.Rejected].includes(data.status)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="searchCriteria.pageNumber" :page-size="searchCriteria.pageSize"
        :totalRecord="table.totalRecords" @change="onChangePageSize" />
    </template>
  </Card>
</template>
