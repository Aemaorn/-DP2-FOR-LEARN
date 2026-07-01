<script setup lang="ts">
import { CriteriaGroupButton, Datepicker, InputField, Select, StatusGroupButton } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { BadgeStatus, Pagination } from '@/components';
import { useRouter } from 'vue-router';
import { usePcm003DetailStore, usePcm003ListStore } from '@/stores/PCM/pcm003';
import Pcm003Constant from '@/constants/pcm003';
import { QuarterOptions, YearOptions } from '@/constants/date';
import { Pcm003Status } from '@/enums/pcm003';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useMenuStore } from '@/stores/menu';
import { watch, onMounted } from 'vue';
import type { EWorkProcess } from '@/enums/shared';
import { SharedConstants } from '@/constants';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { Form } from 'vee-validate';
import { formatCurrency } from '@/helpers/currency';

const router = useRouter();
const menuStore = useMenuStore();
const pcm003Store = usePcm003ListStore();
const detailStore = usePcm003DetailStore();
const { BadgeStatusColor, Pcm003StatusColor } = Pcm003Constant;
const { WorkProcessOptions } = SharedConstants;

const routeToDetail = (id?: string) => {
  let route = 'pcm003/detail';

  if (id) {
    route = `${route}/${id}`;
  }

  if (!id) {
    detailStore.onResetBody()
  }

  router.push(route);
};

onMounted(async (): Promise<void> => {
  Promise.all([
    await pcm003Store.getDepartmentDDLAsync(),
    await pcm003Store.getDataList(),
  ]);
});

watch((): (number | EWorkProcess | undefined | Pcm003Status)[] =>
  [
    pcm003Store.criteria.pageNumber,
    pcm003Store.criteria.pageSize,
    pcm003Store.criteria.workProcess,
    pcm003Store.criteria.status,
  ], async (): Promise<void> => {
    await pcm003Store.getDataList();
  });


</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="รายการจัดซื้อจัดจ้าง กรณีเร่งด่วน">
      <template #action>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => routeToDetail()" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <Form @submit="pcm003Store.getDataList">
          <CriteriaGroupButton :options="WorkProcessOptions" v-model.trim="pcm003Store.criteria.workProcess" />
          <div class="mt-10 space-y-8 lg:space-y-10">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <InputField label="คำค้นหา" v-model.trim="pcm003Store.criteria.keyword" hide-details class="lg:col-span-3"/>
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select label="ฝ่าย/ภาค เขต" v-model.trim="pcm003Store.criteria.departmentCode"
                :options="pcm003Store.departmentDropdown" @enterClose="pcm003Store.getDataList" hide-details />
              <Select label="ปีงบประมาณ" v-model.trim="pcm003Store.criteria.budgetYear" :options="YearOptions"
                @enterClose="pcm003Store.getDataList" hide-details />
              <Select label="ไตรมาส" v-model="pcm003Store.criteria.quarter" :options="QuarterOptions"
                @enterClose="pcm003Store.getDataList" hide-details />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Datepicker label="วันที่อนุมัติ" v-model="pcm003Store.criteria.actionAtFrom"
                :max="pcm003Store.criteria.actionAtTo" hide-details />
              <Datepicker label="ถึงวันที่" v-model="pcm003Store.criteria.actionAtTo"
                :min="pcm003Store.criteria.actionAtFrom" hide-details />
              <div class="lg:col-span-3 flex items-end justify-end gap-2">
                <ButtonSearch class="lg:w-fit w-full" type="submit" />
                <ButtonClear class="lg:w-fit w-full" @click="() => pcm003Store.onResetCriteria()" />
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
            class="bg-white! hover:bg-green-50!" @click="() => pcm003Store.exportExcelAsync()" />
        </div>
        <StatusGroupButton :optionBadges="pcm003Store.statusOptionBadge" v-model="pcm003Store.criteria.status" />
        <DataView :value="pcm003Store.dataResponse.data?.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                    <p class="underline text-blue-400 w-fit" @click="() => routeToDetail(data.id)">
                      {{ data.p79Clause2Number }}
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
                      :bg-color-class="Pcm003StatusColor(data.status).bgColorClass"
                      :text-color-class="Pcm003StatusColor(data.status).textColorClass" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text" @click="() => routeToDetail(data.id)" />
                  <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text"
                    v-if="menuStore.hasManage && [Pcm003Status.Draft, Pcm003Status.Edit].includes(data.status)"
                    @click="() => pcm003Store.onDeleteByIdAsync(data.id)" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center font-bold">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="pcm003Store.criteria.pageNumber" :page-size="pcm003Store.criteria.pageSize"
          :total-record="pcm003Store.dataResponse.data?.totalRecords" @change="pcm003Store.onChangePageSize" />
      </template>
    </Card>
  </div>
</template>
