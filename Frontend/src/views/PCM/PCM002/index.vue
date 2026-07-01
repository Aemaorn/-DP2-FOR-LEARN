<script setup lang="ts">
import { CriteriaGroupButton, Datepicker, InputField, Select, StatusGroupButton } from '@/components/forms';
import { TitleHeader, InfoRow } from '@/components/cosmetic';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { BadgeStatus } from '@/components';
import { useRouter } from 'vue-router';
import { usePcm002DetailStore, usePcm002ListStore } from '@/stores/PCM/pcm002';
import { onMounted, watch } from 'vue';
import Pw119Constant from '@/constants/pcm002';
import { QuarterOptions, YearOptions } from '@/constants/date';
import { Pcm002Status } from '@/enums/pcm002';
import { useMenuStore } from '@/stores/menu';
import type { EWorkProcess } from '@/enums/shared';
import { SharedConstants } from '@/constants';
import { Form } from 'vee-validate';
import { formatCurrency } from '@/helpers/currency';

const router = useRouter();
const menuStore = useMenuStore();
const pcm002Store = usePcm002ListStore();
const detailStore = usePcm002DetailStore();
const { BadgeStatusColor } = Pw119Constant;
const { WorkProcessOptions } = SharedConstants;

const routeToDetail = (id?: string) => {
  let route = 'pcm002/detail';

  if (id) {
    route = `${route}/${id}`;
  }

  if (!id) {
    detailStore.onResetDetail();
  }

  router.push(route);
};

onMounted(async (): Promise<void> => {
  Promise.all([
    await pcm002Store.getDepartmentDDLAsync(),
    await pcm002Store.getDataList(),
  ]);
});

watch((): (number | EWorkProcess | undefined | Pcm002Status)[] =>
  [
    pcm002Store.criteria.pageNumber,
    pcm002Store.criteria.pageSize,
    pcm002Store.criteria.workProcess,
    pcm002Store.criteria.status,
  ], async (): Promise<void> => {
    await pcm002Store.getDataList();
  });
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="รายการจัดซื้อจัดจ้าง ว119">
      <template #action>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => routeToDetail()" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <Form @submit="pcm002Store.getDataList">
          <CriteriaGroupButton :options="WorkProcessOptions" v-model="pcm002Store.criteria.workProcess" />
          <div class="mt-10 space-y-8 lg:space-y-10">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <InputField label="คำค้นหา" v-model.trim="pcm002Store.criteria.keyword" hide-details class="lg:col-span-3" />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select label="ฝ่าย/ภาค เขต" v-model="pcm002Store.criteria.departmentCode"
                :options="pcm002Store.departmentDropdown" @enterClose="pcm002Store.getDataList" hide-details />
              <Select label="ปีงบประมาณ" v-model="pcm002Store.criteria.budgetYear" :options="YearOptions"
                @enterClose="pcm002Store.getDataList" hide-details />
              <Select label="ไตรมาส" v-model="pcm002Store.criteria.quarter" :options="QuarterOptions"
                @enterClose="pcm002Store.getDataList" hide-details />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Datepicker label="วันที่อนุมัติ" v-model="pcm002Store.criteria.actionAtFrom"
                :max="pcm002Store.criteria.actionAtTo" hide-details />
              <Datepicker label="ถึงวันที่" v-model="pcm002Store.criteria.actionAtTo"
                :min="pcm002Store.criteria.actionAtFrom" hide-details />
              <div class="lg:col-span-3 flex items-end justify-end gap-2">
                <ButtonSearch class="lg:w-fit w-full" type="submit" />
                <ButtonClear class="lg:w-fit w-full" @click="() => pcm002Store.onResetCriteria()" />
              </div>
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <div class="flex justify-end mb-4">
          <Button label="พิมพ์รายงานประกาศผลผู้ชนะ" icon="pi pi-file-export" variant="outlined"
            class="bg-white! hover:bg-green-50!" @click="() => pcm002Store.exportExcelAsync()" />
        </div>
        <StatusGroupButton :optionBadges="pcm002Store.statusOptionBadge" v-model="pcm002Store.criteria.status" />
        <DataView :value="pcm002Store.dataResponse.data?.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                    <p class="underline text-blue-400 cursor-pointer w-fit" @click="() => routeToDetail(data.id)">
                      {{ data.pw119Number }}
                    </p>
                  </InfoRow>
                  <InfoRow label="ชื่อโครงการ">
                    <p class="font-bold">
                      {{ data.subject }}
                    </p>
                  </InfoRow>
                  <InfoRow label="วงเงินงบประมาณ">
                    <p>
                      {{ formatCurrency(data.budget) }}
                    </p>
                  </InfoRow>
                  <InfoRow label="ฝ่าย/ภาคเขต">
                    <p>
                      {{ data.departmentName }}
                    </p>
                  </InfoRow>
                  <InfoRow label="วิธีจัดหา">
                    <p>
                      {{ data.supplyMethodName }} : {{ data.supplyMethodSpecialName }}
                    </p>
                  </InfoRow>
                  <InfoRow v-if="data.glAccounts?.length" label="รหัสบัญชี">
                    <p class="break-words text-base text-gray-600">
                      <template v-for="(gl, glIndex) in data.glAccounts" :key="glIndex">
                        <span v-if="Number(glIndex) > 0" class="mx-1.5 text-gray-300">|</span>
                        <span class="font-bold text-gray-900 tabular-nums">{{ gl.split(' : ')[0] }}</span>
                        <span class="text-gray-900">{{ gl.split(' : ').slice(1).length ? ' ' + gl.split(' : ').slice(1).join(' : ') : '' }}</span>
                      </template>
                    </p>
                  </InfoRow>
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                  <div class="flex items-center gap-2 mt-2 mr-2">
                    <p class="text-sm">สถานะ :</p>
                    <BadgeStatus :label="BadgeStatusColor(data.status).label"
                      :color="BadgeStatusColor(data.status).color" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text" @click="() => routeToDetail(data.id)" />
                  <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text"
                    v-if="menuStore.hasManage && [Pcm002Status.Draft, Pcm002Status.Edit, Pcm002Status.Rejected].includes(data.status)"
                    @click="() => pcm002Store.onDeleteAsync(data.id)" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="pcm002Store.criteria.pageNumber" :page-size="pcm002Store.criteria.pageSize"
          :total-record="pcm002Store.dataResponse.data?.totalRecords" />
      </template>
    </Card>
  </div>
</template>
