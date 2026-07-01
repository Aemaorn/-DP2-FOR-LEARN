<script setup lang="ts">
import { Card, DataView } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, Datepicker, CriteriaGroupButton } from '@/components/forms';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { Pagination, BadgeStatus } from '@/components';
import { Form as VeeForm } from 'vee-validate';
import { useCm006ListStore } from '@/stores/CM/CM006/cm006.list';
import { storeToRefs } from 'pinia';
import type { Cm006List } from '@/models/CM/cm006';
import Cm006Constants from '@/constants/CM/cm006';
import { useRouter } from 'vue-router';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import { onMounted, watch } from 'vue';
import { SharedConstants } from '@/constants';
import { useMenuStore } from '@/stores/menu';

const router = useRouter();
const store = useCm006ListStore();
const { searchCriteria, table, options } = storeToRefs(store);
const { onChangePageSize, onResetCriteria, onGetList, onGetDropdown } = store;
const { WorkProcessOptions } = SharedConstants;
const { Cm006BadgeStatus } = Cm006Constants;

const menuStore = useMenuStore();

onMounted(async () => {
  await Promise.all([onGetDropdown(), onGetList()]);

  onWatch();
});

const onWatch = () => {
  watch(() =>
    [
      searchCriteria.value.pageNumber,
      searchCriteria.value.pageSize,
      searchCriteria.value.workProcess,
      searchCriteria.value.status,
    ], async () => {
      await onGetList();
    });
};
</script>

<template>
  <TitleHeader label="รายการคืนหลักประกันสัญญา">
    <template #action>
      <Button label="เพิ่มรายการคืนหลักประกันสัญญา" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'cm006Detail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>
  <!-- CM006 -->
  <Card class="my-4">
    <template #content>
      <CriteriaGroupButton :options="WorkProcessOptions" v-model="searchCriteria.workProcess" />
      <VeeForm @submit="onGetList">
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <InputField v-model.trim="searchCriteria.keyword" label="คำค้นหา" class="lg:col-span-3" hideDetails />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select :options="options.department" v-model="searchCriteria.departmentCode" label="ฝ่าย/ภาคเขต"
              hideDetails />
            <Datepicker v-model="searchCriteria.signedDate" label="วันที่ลงนามสัญญา" hideDetails />
            <Select :options="options.rentalType" v-model="searchCriteria.contractTypeCode" label="ประเภทสัญญา"
              hideDetails />
            <div class="lg:col-span-2 flex items-end justify-end gap-2">
              <ButtonSearch type="submit" class="lg:w-fit w-full" />
              <ButtonClear @click="onResetCriteria" class="lg:w-fit w-full" />
            </div>
          </div>
        </div>
      </VeeForm>
    </template>
  </Card>

  <Card>
    <template #content>
      <DataView :value="table.data">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as Array<Cm006List>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่สัญญา จพ.(สบส.)">
                  <p @click="() => router.push({ name: 'cm006Detail', params: { contractVendorId: data.contractDraftVendorId, id: data.contractGuaranteeReturnId, } })"
                    class="text-blue-500 underline hover:cursor-pointer">
                    {{ data.contractNumber }}
                  </p>
                </InfoRow>

                <InfoRow label="เลขที่ PO (SAP)">
                  <p>
                    {{ data.poNumber }}
                  </p>
                </InfoRow>

                <InfoRow label="วันที่ลงนามสัญญา">
                  <p>
                    {{ ToDateOnly(data.contractSignedDate) }}
                  </p>
                </InfoRow>

                <InfoRow label="คู่สัญญา">
                  <p>
                    {{ `${data.entrepreneurCode} : ${data.entrepreneurName}` }}
                  </p>
                </InfoRow>

                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">
                    {{ data.contractName }}
                  </p>
                </InfoRow>

                <InfoRow label="วงเงินตามสัญญา">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>

                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>
                    {{ data.department }}
                  </p>
                </InfoRow>

                <InfoRow label="ประเภทสัญญา">
                  <p>
                    {{ data.contractTypeLabel ?? '-' }}
                  </p>
                </InfoRow>
              </div>

              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus :label="Cm006BadgeStatus(data.contractGuaranteeReturnStatus).label"
                    :color="Cm006BadgeStatus(data.contractGuaranteeReturnStatus).color" />
                </div>
                <Button icon="pi pi-pen-to-square" size="small" variant="text"
                  @click="() => router.push({ name: 'cm006Detail', params: { contractVendorId: data.contractDraftVendorId, id: data.contractGuaranteeReturnId, } })"
                  class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center font-bold">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :pageNumber="searchCriteria.pageNumber" :pageSize="searchCriteria.pageSize"
        :totalRecord="table.totalRecords" @change="onChangePageSize" />
    </template>
  </Card>
</template>