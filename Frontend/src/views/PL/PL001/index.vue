<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { BadgeStatus } from '@/components';
import { InputField, Radio, Select, CriteriaGroupButton, StatusGroupButton } from '@/components/forms';
import { Button } from 'primevue';
import { useRouter } from 'vue-router';
import { YearOptions } from '@/constants/date';
import { usePL001ListStore } from '@/stores/PL/pl001';
import { onMounted, watch } from 'vue';
import { Form } from 'vee-validate';
import { PlanStatus } from '@/enums/plan';
import { formatCurrency } from '@/helpers/currency';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { SharedConstants } from '@/constants';
import StatusChip from '@/components/StatusChip.vue';
import ProcurementConstants from '@/constants/procurement';
import PlanConstant from '@/constants/plan';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import type { EWorkProcess } from '@/enums/shared';
import { useMenuStore } from '@/stores/menu';
import cookie from '@/configs/cookie';
import { useAuthenticationStore } from '@/stores/authentication';
import { HttpStatusCode } from 'axios';

const router = useRouter();
const listStore = usePL001ListStore();
const menuStore = useMenuStore();
const { ProcurementTypeName } = ProcurementConstants;
const { WorkProcessOptions } = SharedConstants;
const { PlanStatusColor, PlanStatusName } = PlanConstant;

const authenticationStore = useAuthenticationStore();
const dp1Url = import.meta.env.VITE_APP_WEB_DP1_URL;

onMounted(async (): Promise<void> => {
  Promise.all([
    await listStore.getDepartmentDDLAsync(),
    await listStore.getSupplyMethodDDLAsync(),
    await listStore.getSupplyMethodTypeDDLAsync(),
    await listStore.getListAsync(),
  ]);
});

const navagateToDetail = async (isFromOldData: boolean, type: string, id?: string) => {
  if (isFromOldData) {
    const userName = cookie.get('userNameLogin');
    const accesstoken = cookie.get('accessToken-dp1');
    const refreshToken = cookie.get('refreshToken-dp1');
    const { status } = await authenticationStore.setTokenCachAsync(userName, accesstoken, refreshToken);

    if (status === HttpStatusCode.Ok) {
      const url = `${dp1Url}/exchange?userName=${userName}&id=${id}&programName=${type}`;
      window.open(url, "_blank");
    }

    return;
  }

  const route = '/pl/pl001/detail';
  const finalRoute = id ? `${route}/${id}` : route;

  router.push(finalRoute);
};

watch(() => listStore.searchCriteria.supplyMethodCode, async (newValue) => {
  if (newValue) {
    await listStore.getSupplyMethodSpecialTypeDDlAsync(newValue);
  }
});

watch((): (number | EWorkProcess | undefined | PlanStatus)[] =>
  [
    listStore.searchCriteria.pageNumber,
    listStore.searchCriteria.pageSize,
    listStore.searchCriteria.workProcess,
    listStore.searchCriteria.status,
  ], async (): Promise<void> => {
    await listStore.getListAsync();
  });
</script>

<template>
  <TitleHeader label="รายการจัดซื้อจัดจ้าง">
    <template #action>
      <Button label="เพิ่มรายการจัดซื้อจัดจ้าง" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ 'name': 'pl001Detail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.getListAsync">
        <CriteriaGroupButton class="space-y-0" :options="WorkProcessOptions"
          v-model="listStore.searchCriteria.workProcess" />
        <Radio v-model="listStore.searchCriteria.type" :options="ProcurementConstants.ProcurementTypeOptions"
          hide-details />
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="คำค้นหา" v-model.trim="listStore.searchCriteria.keyword" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="ฝ่าย/ภาค เขต" v-model="listStore.searchCriteria.departmentCode"
              :options="listStore.departmentDDL" @enterClose="listStore.getListAsync" hide-details />
            <Select label="ปีงบประมาณ" v-model="listStore.searchCriteria.budgetYear" :options="YearOptions"
              @enterClose="listStore.getListAsync" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="วิธีการจัดหา" v-model="listStore.searchCriteria.supplyMethodCode"
              :options="listStore.supplyMethodCodeDDL" @enterClose="listStore.getListAsync" hide-details />
            <Select v-model="listStore.searchCriteria.supplyMethodTypeCode" :options="listStore.supplyMethodTypeCodeDDL"
              @enterClose="listStore.getListAsync" hide-details />
            <Select v-model="listStore.searchCriteria.SupplyMethodSpecialTypeCode"
              :options="listStore.supplyMethodSpecialTypeCodeDDL" @enterClose="listStore.getListAsync" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <Radio class="mt-2" v-model="listStore.searchCriteria.process"
              :options="ProcurementConstants.ProcurementProcessOptions" />
            <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="() => listStore.onResetCriteria()" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card>
    <template #content>
      <div class="flex justify-end gap-4 mb-4">
        <Button label="พิมพ์รายงาน e-GP" icon="pi pi-file-export" variant="outlined"
          @click="() => listStore.exportExcelEGPAsync()" />
        <Button label="พิมพ์รายงานการจัดซื้อจัดจ้าง" icon="pi pi-file-export" variant="outlined"
          @click="() => listStore.exportExcelPlanAsync()" />
      </div>
      <StatusGroupButton :optionBadges="listStore.statusOptionBadge" v-model="listStore.searchCriteria.status" />
      <DataView :value="listStore.planResponse.data?.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                  <p class="underline text-blue-400 cursor-pointer w-fit"
                    @click="() => navagateToDetail(data.oldData, data.type, data.id)">
                    {{ data.planNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อโครงการ">
                  <p class="font-bold">
                    {{ data.name }}
                  </p>
                </InfoRow>
                <InfoRow label="ปีงบประมาณ">
                  <p>{{ data.budgetYear }}</p>
                </InfoRow>
                <InfoRow label="วงเงินงบประมาณ">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>
                <InfoRow label="ประเภทแผน">
                  <StatusChip :label="ProcurementTypeName(data.type) ?? ''" size="Medium" color="Info" />
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
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus :label="PlanStatusName(data.status)"
                    :bg-color-class="PlanStatusColor(data.status).bgColorClass"
                    :text-color-class="PlanStatusColor(data.status).textColorClass" />

                  <BadgeStatus v-if="data.isChange" label="ขอเปลี่ยนแปลง" color="amber" />
                  <BadgeStatus v-if="data.isCancel" label="ขอยกเลิก" color="red" />
                </div>
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text" @click="() => navagateToDetail(data.oldData, data.type, data.id)" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text"
                  v-if="[PlanStatus.DraftPlan, PlanStatus.EditPlan, PlanStatus.RejectPlan, PlanStatus.Closed].includes(data.status) && menuStore.hasManage && data.departmentCode === authenticationStore.profile.departmentCode"
                  @click="() => listStore.deleteAsync(data.id)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="listStore.searchCriteria.pageNumber" :page-size="listStore.searchCriteria.pageSize"
        :total-record="listStore.planResponse.data?.totalRecords" @change="listStore.onChangePageSize" />
    </template>
  </Card>
</template>
