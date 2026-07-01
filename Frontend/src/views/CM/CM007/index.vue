<script setup lang="ts">
import { Card, DataView } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { InputField, Select, Datepicker, CriteriaGroupButton } from '@/components/forms';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { Pagination, BadgeStatus } from '@/components';
import { Form as VeeForm } from 'vee-validate';
import { useCm007ListStore } from '@/stores/CM/CM007/cm007.list';
import { storeToRefs } from 'pinia';
import type { Cm007List } from '@/models/CM/cm007';
import Cm007Constants from '@/constants/CM/cm007';
import { useRouter } from 'vue-router';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import { onMounted, watch } from 'vue';
import { SharedConstants } from '@/constants';
import { useMenuStore } from '@/stores/menu';
import { Cm007Status } from '@/enums/CM/cm007';

const router = useRouter();
const store = useCm007ListStore();
const { searchCriteria, table, options } = storeToRefs(store);
const { onChangePageSize, onResetCriteria, onGetList, onGetDropdown, onDelete } = store;
const { WorkProcessOptions } = SharedConstants;
const { Cm007BadgeStatus } = Cm007Constants;

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
  <TitleHeader label="รายการบันทึกต่อท้ายสัญญา">
    <template #action>
      <Button label="เพิ่มรายการบันทึกต่อท้ายสัญญา" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'cm007Detail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>

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
            <Select :options="options.contractType" v-model="searchCriteria.contractTypeCode" label="ประเภทสัญญา"
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
          <div v-for="(data, index) in (items as Array<Cm007List>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่สัญญา">
                  <p @click="() => router.push({ name: 'cm007Detail', params: { id: data.id } })"
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
                    {{ data.departmentName }}
                  </p>
                </InfoRow>

                <InfoRow label="ประเภทสัญญา">
                  <p>
                    {{ data.contractTypeLabel ?? '-' }}
                  </p>
                </InfoRow>

                <InfoRow label="วิธีการจัดหา">
                  <p>
                    {{ data.supplyMethodName ?? '-' }}
                  </p>
                </InfoRow>
              </div>

              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus :label="Cm007BadgeStatus(data.status).label"
                    :color="Cm007BadgeStatus(data.status).color" />
                </div>
                <Button icon="pi pi-pen-to-square" size="small" variant="text"
                  @click="() => router.push({ name: 'cm007Detail', params: { id: data.id } })"
                  class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!" />
                <Button v-if="[Cm007Status.Draft, Cm007Status.Editing, Cm007Status.Rejected].includes(data.status)"
                  icon="pi pi-trash" size="small" class="mt-1" text rounded severity="danger"
                  @click="() => onDelete(data.id)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center font-bold">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="searchCriteria.pageNumber" :page-size="searchCriteria.pageSize"
        :total-record="table.totalRecords" @change="onChangePageSize" />
    </template>
  </Card>
</template>
