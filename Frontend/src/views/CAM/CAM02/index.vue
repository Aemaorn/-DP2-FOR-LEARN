<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Pagination } from '@/components';
import { InputField, Select, CriteriaGroupButton, StatusGroupButton } from '@/components/forms';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { useCam02ListStore } from '@/stores/CAM/CAM02/cam02Store';
import { storeToRefs } from 'pinia';
import { onMounted, watch } from 'vue';
import type { TCam02ListData } from '@/models/CAM/CAM02/cam02';
import Cam02Constants from '@/constants/CAM/CAM02/cam02';
import { useRouter } from 'vue-router';
import { SharedConstants } from '@/constants';
import { Form } from 'vee-validate';
import { YearOptions } from '@/constants/date';
import { committeeGroupTypeMappingName } from './components/CommitteeTypeMappingName';

const router = useRouter();
const store = useCam02ListStore();
const { WorkProcessOptions } = SharedConstants;
const { criteria, table, statusOptionBadge } = storeToRefs(store);
const { Cam02BadgeStatus, Cam02ListStatusColor } = Cam02Constants;

onMounted(async () => {
  await Promise.all([store.onGetDropdown(), store.onGetList()]);

  onWatch();
});

onMounted(async (): Promise<void> => {
  Promise.all([
    await store.getDepartmentDDLAsync(),
    await store.getSupplyMethodDDLAsync(),
    await store.getSupplyMethodTypeDDLAsync(),
    await store.onDropDownCommitteeAsync(),
  ]);
});

const onWatch = () => {
  watch(() =>
    [
      criteria.value.pageNumber,
      criteria.value.pageSize,
      criteria.value.workProcess,
      criteria.value.status,
    ], async () => {
      await store.onGetList();
    });
};

const routeToProcurement = (selectedData: TCam02ListData) => {
  router.push({ name: 'cam02-detail', params: { id: selectedData.id } });
}
</script>

<template>
  <TitleHeader label="รายการแก้ไขคณะกรรมการ">
    <template #action>
      <Button icon="pi pi-plus" label="เพิ่มรายการแก้ไขกรรมการ" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'cam02-detail' })" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="store.onGetList">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="criteria.workProcess" />
        <div class="md:grid grid-cols-1 lg:grid-cols-2 gap-2 mt-10">
          <InputField label="คำค้นหา" v-model.trim="criteria.keyword" />
        </div>
        <div class="md:grid grid-cols-3 lg:grid-cols-5 gap-2 mt-6">
          <Select label="ฝ่าย/ภาค เขต" v-model="criteria.departmentCode" :options="store.departmentDDL"
            @enterClose="store.onGetList" />
          <Select label="ปีงบประมาณ" v-model="criteria.budgetYear" :options="YearOptions"
            @enterClose="store.onGetList" />
          <Select label="ข้อมูลขอแก้ไขคณะกรรมการ" v-model="criteria.committeeGroupType"
            :options="store.dropDownCommittee" @enterClose="store.onGetList" />
        </div>
        <div class="md:grid grid-cols-3 lg:grid-cols-5 gap-2 mt-6">
          <Select label="วิธีการจัดหา" v-model="criteria.supplyMethodCode" :options="store.supplyMethodCodeDDL"
            @enterClose="store.onGetList" />
          <Select v-model="criteria.supplyMethodTypeCode" :options="store.supplyMethodTypeCodeDDL"
            @enterClose="store.onGetList" />
          <Select v-model="criteria.SupplyMethodSpecialTypeCode" :options="store.supplyMethodSpecialTypeCodeDDL"
            @enterClose="store.onGetList" />
          <div class="flex gap-2 items-center justify-start md:justify-end col-end-6">
            <ButtonSearch type="submit" />
            <ButtonClear @click="() => store.onResetCriteria()" />
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
          <div v-for="(data, index) in (items as Array<TCam02ListData>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="grid lg:grid-cols-12">
              <div class="lg:col-span-8  order-2 lg:order-1">
                <InfoRow label="เลขที่การจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline hover:cursor-pointer w-fit" @click="() => routeToProcurement(data)">
                    {{ data.procurementNumber }}
                  </p>
                </InfoRow>

                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="text-blue-500 underline hover:cursor-pointer w-fit" @click="() => routeToProcurement(data)">
                    {{ data.planNumber ?? "-" }}
                  </p>
                </InfoRow>

                <InfoRow label="โครงการ">
                  <p class="font-bold">
                    {{ data.procurementName }}
                  </p>
                </InfoRow>

                <InfoRow label="ชุดกรรมการ">
                  <p>
                    {{ committeeGroupTypeMappingName(data.committeeType) }}
                  </p>
                </InfoRow>

                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>
                    {{ data.departmentName }}
                  </p>
                </InfoRow>

                <InfoRow label="วิธีจัดหา">
                  <p>
                    {{ data.supplyMethod }}
                  </p>
                </InfoRow>
              </div>

              <div class="lg:col-span-4 order-1 lg:order-2">
                <div class="flex items-center justify-end gap-2">
                  <div class="flex items-center gap-2">
                    <BadgeStatus :label="Cam02BadgeStatus(data.status).label"
                      :text-color-class="Cam02ListStatusColor(data.status).textColorClass"
                      :bg-color-class="Cam02ListStatusColor(data.status).bgColorClass" />
                  </div>
                  <Button icon="pi pi-pen-to-square" size="small" variant="text"
                    class="text-blue-600 hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    @click="() => router.push({ name: 'cam02-detail', params: { id: data.id } })" />
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
        @change="store.onChangePageSize" />
    </template>
  </Card>
</template>
