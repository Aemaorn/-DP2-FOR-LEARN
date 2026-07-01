<script setup lang="ts">
import { computed, onMounted, watch } from 'vue';
import { Button, Dialog } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, StatusGroupButton } from '@/components/forms';
import { YearOptions } from '@/constants/date';
import PreProcurementConstants from '@/constants/preProcurement';
import TorDraftConstant from '@/constants/torDraft';
import InviteConstant from '@/constants/invite';
import PoaConstant from '@/constants/poa';
import { PreProcurementStep } from '@/enums/preProcurement';
import { ProcurementStatus, ProcurementType } from '@/enums/procurement';
import procurementHelper from '@/helpers/procurement';
import AppointmentHelper from '@/helpers/appointment';
import MedianPriceHelper from '@/helpers/medianPrice';
import PurchaseRequisitionHelper from '@/helpers/purchaseRequisition';
import Jp005Helper from '@/helpers/jp005';
import ContractInvitationHelper from '@/helpers/contractInvitation';
import type { ColorLabel } from '@/models/shared/color';
import { BadgeStatus } from '@/components';
import { usePPListDialogStore } from '../../../../stores/PP/ppStore';
import TypeBadgeChip from './TypeBadgeChip.vue';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { formatCurrency } from '@/helpers/currency';
import { Form } from 'vee-validate';
import { useAuthenticationStore } from '@/stores/authentication';
import { DepartmentId } from '@/enums/businessUnit';

const value = defineModel<boolean>({
  default: false,
});
const listDialogStore = usePPListDialogStore();
const authStore = useAuthenticationStore();
const { PreProcurementTypeName } =
  PreProcurementConstants;
const isJorPorDepartmentCode = computed(() => {
  return authStore.profile.departmentCode === DepartmentId.JorPor;
});

const onSelectPlan = (index: number, isJorPorDepartmentCode: boolean): void => {
  listDialogStore.onSelectData(index, isJorPorDepartmentCode);

  value.value = false;
};

const { ChildStatusBadge } = procurementHelper;

const resolveStatusBadge = (processType: PreProcurementStep, status: string): ColorLabel => {
  if (status === ProcurementStatus.Cancelled) {
    return ChildStatusBadge(status);
  }
  switch (processType) {
    case PreProcurementStep.Appoint:
      return AppointmentHelper.BadgeStatus(status as any);
    case PreProcurementStep.TorDraft:
      return TorDraftConstant.TorDraftStatusColor(status as any);
    case PreProcurementStep.MedianPrice:
      return MedianPriceHelper.MapStatusColor(status as any);
    case PreProcurementStep.PurchaseRequisition:
      return PurchaseRequisitionHelper.MapStatusColor(status as any);
    case PreProcurementStep.Jp005:
      return Jp005Helper.BadgeStatus(status as any, false);
    case PreProcurementStep.Invite:
      return InviteConstant.inviteStatusColor(status as any, false);
    case PreProcurementStep.PurchaseOrderApproval:
      return PoaConstant.poaStatusColor(status as any);
    case PreProcurementStep.ContractInvitation:
      return ContractInvitationHelper.BadgeStatus(status as any);
    default:
      return ChildStatusBadge(status);
  }
};

watch(
  () => [listDialogStore.searchCriteria.pageNumber, listDialogStore.searchCriteria.pageSize],
  () => {
    listDialogStore.onGetPreProcurementDialogListDataAsync();
  }
);

onMounted(async () => {
  await listDialogStore.getDepartmentDDLAsync();
  await listDialogStore.getSupplyMethodDDLAsync();
  await listDialogStore.getSupplyMethodTypeDDLAsync();
});

watch(value, async (val: boolean) => {
  if (val) {
    listDialogStore.searchCriteria.departmentCode = authStore.profile.departmentCode;

    await listDialogStore.onGetPreProcurementDialogListDataAsync();
  }
});

watch(() => listDialogStore.searchCriteria.supplyMethodCode, async (value) => {
  if (value) {
    await listDialogStore.getSupplyMethodSpecialTypeDDLAsync(value);
  }
});

const supplyMethodText = (supplyMethod: string, supplyMethodType?: string, supplyMethodSpecialType?: string) => {
  if (supplyMethodType && supplyMethodSpecialType) {
    return `${supplyMethod} ${supplyMethodType} ${supplyMethodSpecialType}`;
  }

  if (supplyMethodType) {
    return `${supplyMethod} ${supplyMethodType}`;
  }

  if (supplyMethodSpecialType) {
    return `${supplyMethod} ${supplyMethodSpecialType}`;
  }

  return `${supplyMethod}`;
};

const openProcurementDetail = (id: string | number, procurementType: string) => {

  if(procurementType === ProcurementType.Rent)
  {
    return window.open(`/pcm/pcm005/detail/${id}`, '_blank');
  }

  window.open(`/pp/detail/${id}`, '_blank');
};
</script>

<template>
  <Dialog v-model:visible="value" modal :draggable="false" :style="{ width: '90vw' }"
    :breakpoints="{ '1199px': '75vw', '575px': '90vw' }" @hide="() => (value = false)">
    <template #header>
      <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง"></TitleHeader>
    </template>
    <template #default>
      <Form @submit="listDialogStore.onGetPreProcurementDialogListDataAsync()">
        <div class="md:grid grid-cols-1 lg:grid-cols-2 gap-2">
          <InputField label="คำค้นหา" v-model.trim="listDialogStore.searchCriteria.keyword" />
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 mt-5">
          <div class="col-span-auto lg:col-span-3 grid gap-4 grid-cols-1 lg:grid-cols-2">
            <Select @enter-close="listDialogStore.onGetPreProcurementDialogListDataAsync()" label="ฝ่าย/ภาคเขต"
              v-model="listDialogStore.searchCriteria.departmentCode" :options="listDialogStore.departmentDropdown"
              disabled />
            <Select @enter-close="listDialogStore.onGetPreProcurementDialogListDataAsync()" label="ปีงบประมาณ"
              v-model="listDialogStore.searchCriteria.budgetYear" :options="YearOptions" />
          </div>
          <div class="col-span-auto lg:col-span-3 grid gap-4 grid-cols-1 lg:grid-cols-3 mt-5">
            <Select @enter-close="listDialogStore.onGetPreProcurementDialogListDataAsync()" hide-details
              label="วิธีการจัดหา" v-model="listDialogStore.searchCriteria.supplyMethodCode"
              :options="listDialogStore.supplyMethodDropdown" />
            <Select @enter-close="listDialogStore.onGetPreProcurementDialogListDataAsync()" label="ซื้อ" hide-details
              v-model="listDialogStore.searchCriteria.supplyMethodTypeCode"
              :options="listDialogStore.supplyMethodTypeDropdown" />
            <Select @enter-close="listDialogStore.onGetPreProcurementDialogListDataAsync()" label="วิธี"
              hide-details v-model="listDialogStore.searchCriteria.supplyMethodSpecialTypeCode"
              :options="listDialogStore.supplyMethidSpecialTypeDropDown" />
          </div>
          <div class="flex gap-2 justify-start lg:justify-end items-center">
            <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" type="submit" />
            <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
              @click="() => listDialogStore.onResetCriteriaDialog()" />
          </div>
        </div>
      </Form>

      <Card class="mt-4">
        <template #content>
          <StatusGroupButton :optionBadges="listDialogStore.statusOptionBadge"
            v-model="listDialogStore.searchCriteria.groupStep" />
          <DataView :value="listDialogStore.table.data" data-key="id">
            <template #list="{ items }">
              <div v-for="(data, index) in items" :key="data.id" class="border border-gray-300 rounded-sm mb-2 p-3">
                <div class="grid lg:grid-cols-12 gap-x-4 gap-y-2">
                  <div class="lg:col-span-8">
                    <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                      <p class="underline text-blue-400 hover:cursor-pointer" @click="openProcurementDetail(data.procurementId, data.procurementType)">
                        {{ data.planNumber || data.procurementNumber || '-' }}
                      </p>
                    </InfoRow>
                    <InfoRow label="ชื่อโครงการ">
                      <p class="font-bold">{{ data.planName }}</p>
                    </InfoRow>
                    <InfoRow label="วงเงินงบประมาณ" v-if="data.budget > 0">
                      <p>{{ formatCurrency(data.budget) }}</p>
                    </InfoRow>
                    <InfoRow label="ประเภทแผน">
                      <TypeBadgeChip v-if="data.type" :label="PreProcurementTypeName(data.type)" size="Small" :color="data.type" />
                      <span v-else>-</span>
                    </InfoRow>
                    <InfoRow label="ฝ่าย/ภาคเขต">
                      <p>{{ data.departmentName }}</p>
                    </InfoRow>
                    <InfoRow label="วิธีจัดหา">
                      <p>{{
                        supplyMethodText(
                          data.supplyMethod,
                          data.supplyMethodType,
                          data.supplyMethodSpecialType
                        )
                      }}</p>
                    </InfoRow>
                  </div>
                  <div class="lg:col-span-4 flex flex-col items-end justify-between gap-2">
                    <div class="flex items-end gap-2 mt-2">
                      <div class="flex gap-2">
                        <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                          <span>ขั้นตอน :</span>
                          <span>สถานะ :</span>
                        </div>
                        <div class="flex flex-col gap-1 min-w-40">
                          <div class="flex items-center gap-1 px-1">
                            <span class="text-[#1D4ED8] leading-none">&#9679;</span>
                            <p class="whitespace-nowrap text-sm text-[#1D4ED8] font-bold">
                              {{ PreProcurementConstants.PreProcurementStepFullName(data.processType ?? '') }}
                            </p>
                          </div>
                          <BadgeStatus class="!w-full"
                            :color="resolveStatusBadge(data.processType as PreProcurementStep, data.status as string).color"
                            :label="resolveStatusBadge(data.processType as PreProcurementStep, data.status as string).label" />
                        </div>
                      </div>
                      <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                        label="เลือก" @click="() => onSelectPlan(index as number, isJorPorDepartmentCode)" />
                    </div>
                  </div>
                </div>
              </div>
            </template>
            <template #empty>
              <p class="text-center">ไม่พบข้อมูล</p>
            </template>
          </DataView>
          <Pagination :page-number="listDialogStore.searchCriteria.pageNumber"
            :page-size="listDialogStore.searchCriteria.pageSize" :total-record="listDialogStore.table.totalRecords"
            @change="listDialogStore.onChangePageSizePPListDialog" />
        </template>
      </Card>
    </template>
  </Dialog>
</template>
