<script setup lang="ts">
import { Select, StatusGroupButton } from '@/components/forms';
import { TitleHeader, InfoRow } from '@/components/cosmetic';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { BadgeStatus } from '@/components';
import { useRouter } from 'vue-router';
import { usePcm007DetailStore, usePcm007ListStore } from '@/stores/PCM/pcm007';
import { onMounted, watch } from 'vue';
import Pcm007Constant from '@/constants/pcm007';
import { YearOptions } from '@/constants/date';
import { Pcm007Status } from '@/enums/pcm007';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import { formatCurrency } from '@/helpers/currency';
import InputField from '@/components/forms/InputField.vue';

const router = useRouter();
const menuStore = useMenuStore();
const pcm007Store = usePcm007ListStore();
const detailStore = usePcm007DetailStore();
const { BadgeStatusColor, Pcm007StatusColor } = Pcm007Constant;

const routeToDetail = (id?: string) => {
  let route = 'pcm007/detail';

  if (id) {
    route = `${route}/${id}`;
  }

  if (!id) {
    detailStore.onResetDetail();
  }

  router.push(route);
};

onMounted(async (): Promise<void> => {
  await Promise.all([
    pcm007Store.getDepartmentDDLAsync(),
    pcm007Store.getDataList(),
  ]);
});

watch((): (number | Pcm007Status | undefined)[] =>
  [
    pcm007Store.criteria.pageNumber,
    pcm007Store.criteria.pageSize,
    pcm007Store.criteria.status,
  ], async (): Promise<void> => {
    await pcm007Store.getDataList();
  });
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="รายการจัดซื้อจัดจ้าง ว 804">
      <template #action>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => routeToDetail()" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <Form @submit="pcm007Store.getDataList">
          <div class="mt-4 space-y-2 lg:space-y-4">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <InputField label="คำค้นหา" v-model.trim="pcm007Store.criteria.keyword" hide-details class="lg:col-span-3" />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select label="ฝ่าย/ภาค เขต" v-model="pcm007Store.criteria.departmentCode"
                :options="pcm007Store.departmentDropdown" @enterClose="pcm007Store.getDataList" hide-details />
              <Select label="ปีงบประมาณ" v-model="pcm007Store.criteria.budgetYear" :options="YearOptions"
                @enterClose="pcm007Store.getDataList" hide-details />
              <div class="lg:col-span-3 flex items-end justify-end gap-2">
                <ButtonSearch class="lg:w-fit w-full" type="submit" />
                <ButtonClear class="lg:w-fit w-full" @click="() => pcm007Store.onResetCriteria()" />
              </div>
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <StatusGroupButton :optionBadges="pcm007Store.statusOptionBadge" v-model="pcm007Store.criteria.status" />
        <DataView :value="pcm007Store.dataResponse.data?.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่รายการ">
                    <p class="underline text-blue-400 cursor-pointer w-fit" @click="() => routeToDetail(data.id)">
                      {{ data.pw184Number }}
                    </p>
                  </InfoRow>
                  <InfoRow label="เรื่อง">
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
                      {{ data.supplyMethodName }} {{ data.supplyMethodSpecialName ? `: ${data.supplyMethodSpecialName}` : '' }}
                    </p>
                  </InfoRow>
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                  <div class="flex items-center gap-2 mt-2 mr-2">
                    <p class="text-sm">สถานะ :</p>
                    <BadgeStatus :label="BadgeStatusColor(data.status).label"
                      :bg-color-class="Pcm007StatusColor(data.status).bgColorClass"
                      :text-color-class="Pcm007StatusColor(data.status).textColorClass" />
                  </div>
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text" @click="() => routeToDetail(data.id)" />
                  <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text"
                    v-if="menuStore.hasManage && [Pcm007Status.Draft, Pcm007Status.Edit, Pcm007Status.Rejected].includes(data.status)"
                    @click="() => pcm007Store.onDeleteAsync(data.id)" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="pcm007Store.criteria.pageNumber" :page-size="pcm007Store.criteria.pageSize"
          :total-record="pcm007Store.dataResponse.data?.totalRecords" />
      </template>
    </Card>
  </div>
</template>
