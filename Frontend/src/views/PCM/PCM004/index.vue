<script setup lang="ts">
import { CriteriaGroupButton, InputField, Select, StatusGroupButton } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { useRouter } from 'vue-router';
import { usePcm004ListStore } from '@/stores/PCM/pcm004';
import { onMounted, watch } from 'vue';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import Pcm004Constant from '@/constants/pcm004';
import { Pcm004Status } from '@/enums/pcm004';
import { YearOptions } from '@/constants/date';
import { formatCurrency } from '@/helpers/currency';
import { useMenuStore } from '@/stores/menu';
import type { EWorkProcess } from '@/enums/shared';
import { SharedConstants } from '@/constants';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { Form } from 'vee-validate';
import { BadgeStatus } from '@/components';

const menuStore = useMenuStore();
const router = useRouter();
const pcm004Store = usePcm004ListStore();
const { BadgeStatusColor, Pcm004StatusColor } = Pcm004Constant;
const { WorkProcessOptions } = SharedConstants;
const canDelete = [Pcm004Status.Draft, Pcm004Status.Edit];

const routeToDetail = (id?: string) => {
  let route = 'pcm004/detail';

  if (id) {
    route = `${route}/${id}`;
  }

  router.push(route);
};

onMounted(async (): Promise<void> => {
  Promise.all([
    await pcm004Store.getDepartmentDDLAsync(),
    await pcm004Store.getDataList(),
  ]);
});

watch((): (number | EWorkProcess | undefined | Pcm004Status)[] =>
  [
    pcm004Store.criteria.pageNumber,
    pcm004Store.criteria.pageSize,
    pcm004Store.criteria.workProcess,
    pcm004Store.criteria.status,
  ], async (): Promise<void> => {
    await pcm004Store.getDataList();
  });

</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="รายการจัดซื้อจัดจ้าง Petty Cash">
      <template #action>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => routeToDetail()" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <Form @submit="pcm004Store.getDataList">
          <CriteriaGroupButton :options="WorkProcessOptions" v-model="pcm004Store.criteria.workProcess" />
          <div class="mt-10 space-y-8 lg:space-y-10">
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
              <InputField label="คำค้นหา" v-model.trim="pcm004Store.criteria.keyword" hide-details />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select label="ฝ่าย/ภาค เขต" v-model="pcm004Store.criteria.departmentCode"
                :options="pcm004Store.departmentDropdown" @enterClose="pcm004Store.getDataList" hide-details />
              <Select label="ปีงบประมาณ" v-model="pcm004Store.criteria.budgetYear" :options="YearOptions"
                @enterClose="pcm004Store.getDataList" hide-details />
              <div class="lg:col-span-3 flex items-end justify-end gap-2">
                <ButtonSearch class="lg:w-fit w-full" type="submit" />
                <ButtonClear class="lg:w-fit w-full" @click="() => pcm004Store.onResetCriteria()" />
              </div>
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <StatusGroupButton :optionBadges="pcm004Store.statusOptionBadge" v-model="pcm004Store.criteria.status" />
        <DataView :value="pcm004Store.dataResponse.data?.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                    <p class="underline text-blue-400 w-fit" @click="() => routeToDetail(data.id)">
                      {{ data.pPettyCashNumber }}
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
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                  <div class="flex items-center gap-2 mt-2 mr-2">
                    <p class="text-sm">สถานะ :</p>
                    <BadgeStatus :label="BadgeStatusColor(data.status).label"
                      :bg-color-class="Pcm004StatusColor(data.status).bgColorClass"
                      :text-color-class="Pcm004StatusColor(data.status).textColorClass" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text" @click="() => routeToDetail(data.id)" />
                  <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text" @click="pcm004Store.onDeleteAsync(data.id)"
                    v-if="menuStore.hasManage && canDelete.includes(data.status)" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="pcm004Store.criteria.pageNumber" :page-size="pcm004Store.criteria.pageSize"
          :total-record="pcm004Store.dataResponse.data?.totalRecords" />
      </template>
    </Card>
  </div>
</template>
