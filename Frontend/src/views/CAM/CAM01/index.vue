<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Pagination } from '@/components';
import { InputField, Select, Datepicker, CriteriaGroupButton, StatusGroupButton } from '@/components/forms';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { useCam01ListStore } from '@/stores/CAM/CAM01/cam01.list';
import { storeToRefs } from 'pinia';
import { onMounted, watch } from 'vue';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { Cam01List } from '@/models/CAM/CAM01/cam01';
import Cam01Constants from '@/constants/CAM/CAM01/cam01';
import { useRouter } from 'vue-router';
import { SharedConstants } from '@/constants';
import { Form } from 'vee-validate';

const router = useRouter();
const store = useCam01ListStore();
const { WorkProcessOptions } = SharedConstants;
const { criteria, table, options, statusOptionBadge } = storeToRefs(store);
const { onChangePageSize, onGetDropdown, onGetList, onResetCriteria, } = store;
const { Cam01BadgeStatus, Cam01BadgeType, Cam01ListStatusColor } = Cam01Constants;

onMounted(async () => {
  await Promise.all([onGetDropdown(), onGetList()]);

  onWatch();
});

const onWatch = () => {
  watch(() =>
    [
      criteria.value.pageNumber,
      criteria.value.pageSize,
      criteria.value.workProcess,
      criteria.value.status,
    ], async () => {
      await onGetList();
    });
};

const routeToProcurement = (selectedData: Cam01List) => {
  if (selectedData.procurementType === 'Rent') {
    router.push({ name: 'pcm005Detail', params: { id: selectedData.procurementId } });

    return;
  }

  router.push({ name: 'ppDetail', params: { id: selectedData.procurementId } });
}
</script>

<template>
  <TitleHeader label="รายการบันทึกต่อท้ายสัญญา">
    <template #action>
      <Button icon="pi pi-plus" label="สร้างรายการบันทึกต่อท้ายสัญญา" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'cam01-detail' })" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="onGetList">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="criteria.workProcess" />
        <div class="grid lg:grid-cols-5 gap-2 gap-y-10 mt-8">
          <InputField label="คำค้นหา" v-model.trim="criteria.keyword" class="lg:col-span-3" hide-details />
          <Datepicker label="วันที่ลงนามสัญญา" v-model="criteria.signedDate" class="lg:col-start-1" hide-details />
          <Select label="ประเภทสัญญา" :options="options.rentalType" v-model="criteria.contractTypeCode" hide-details />
          <div class="flex items-center lg:justify-end lg:col-span-5 gap-2">
            <ButtonSearch type="submit" class="lg:w-fit w-full" />
            <ButtonClear @click="onResetCriteria" class="lg:w-fit w-full" />
          </div>
        </div>
      </Form>
    </template>
  </Card>

  <Card>
    <template #content>
      <StatusGroupButton :optionBadges="statusOptionBadge" v-model="criteria.status" />
      <DataView :value="table.data">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as Array<Cam01List>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid lg:grid-cols-12">
              <div class="lg:col-span-8  order-2 lg:order-1">
                <InfoRow label="เลขที่สัญญา จพ.(สบส.)">
                  <p class="text-blue-500 underline hover:cursor-pointer w-fit" @click="() => routeToProcurement(data)">
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
                    {{ `${data.entrepreneurTax} : ${data.entrepreneurName}` }}
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

                <InfoRow label="ประเภทสัญญา">
                  <p>
                    {{ data.contractTypeLabel ?? '-' }}
                  </p>
                </InfoRow>
              </div>

              <div class="lg:col-span-4 order-1 lg:order-2">
                <div class="flex items-center justify-end gap-2">
                  <div class="flex items-center gap-2">
                    <BadgeStatus :label="Cam01BadgeType(data.type).label" :color="Cam01BadgeType(data.type).color" />
                    <BadgeStatus :label="Cam01BadgeStatus(data.status).label"
                      :text-color-class="Cam01ListStatusColor(data.status).textColorClass"
                      :bg-color-class="Cam01ListStatusColor(data.status).bgColorClass" />
                  </div>
                  <Button icon="pi pi-pen-to-square" size="small" variant="text"
                    class="text-blue-600 hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    @click="() => router.push({ name: 'cam01-detail', params: { id: data.id } })" />
                </div>
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center font-bold">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :pageNumber="criteria.pageNumber" :pageSize="criteria.pageSize" :totalRecord="table.totalRecords"
        @change="onChangePageSize" />
    </template>
  </Card>
</template>
