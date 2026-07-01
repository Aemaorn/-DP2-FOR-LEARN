<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, CriteriaGroupButton, StatusGroupButton } from '@/components/forms';
import { Button } from 'primevue';
import { useRouter } from 'vue-router';
import { YearOptions } from '@/constants/date';
import PreProcurementConstants from '@/constants/preProcurement';
import { usePPListStore } from '../../stores/PP/ppStore';
import { onMounted, watch } from 'vue';
import { TypeBadgeChip } from './components/PP';
import { formatCurrency } from '@/helpers/currency';
import { SharedConstants } from '@/constants';
import procurementHelper from '@/helpers/procurement';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import type { TPreProcurement } from '@/models/PP/ppModel';
import { ProcurementStatus } from '@/enums/procurement';
import { PreProcurementStep } from '@/enums/preProcurement';
import { useMenuStore } from '@/stores/menu';
import { useAuthenticationStore } from '@/stores/authentication';
import { BadgeStatus } from '@/components';
import type { ColorLabel } from '@/models/shared/color';
import AppointmentHelper from '@/helpers/appointment';
import TorDraftConstant from '@/constants/torDraft';
import MedianPriceHelper from '@/helpers/medianPrice';
import PurchaseRequisitionHelper from '@/helpers/purchaseRequisition';
import Jp005Helper from '@/helpers/jp005';
import InviteConstant from '@/constants/invite';
import PoaConstant from '@/constants/poa';
import ContractInvitationHelper from '@/helpers/contractInvitation';

const router = useRouter();
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const listStore = usePPListStore();
const { PreProcurementTypeName } = PreProcurementConstants;
const { WorkProcessOptions } = SharedConstants;
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

onMounted(async (): Promise<void> => {
  await listStore.onGetPreProcurementListDataAsync();
  await listStore.getSupplyMethodTypeDDLAsync();
});

watch(
  () => [
    listStore.searchCriteria.pageNumber,
    listStore.searchCriteria.pageSize,
    listStore.searchCriteria.workProcess,
    listStore.searchCriteria.step,
  ],
  async (): Promise<void> => {
    await listStore.onGetPreProcurementListDataAsync();
  }
);

onMounted(async () => {
  await listStore.getDepartmentDDLAsync();
  await listStore.getSupplyMethodDDLAsync();
});

watch(() => listStore.searchCriteria.supplyMethodCode, async (value) => {
  if (value) {
    await listStore.getSupplyMethodSpecialTypeDDLAsync(value);
  }
});
</script>

<template>
  <TitleHeader label="การจัดซื้อจัดจ้าง">
    <template #action>
      <Button label="เพิ่มรายการจัดซื้อจัดจ้าง" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'ppDetail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetPreProcurementListDataAsync">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="listStore.searchCriteria.workProcess" />
        <div class="space-y-8 lg:space-y-10 mt-8">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="คำค้นหา" v-model.trim="listStore.searchCriteria.keyword" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="ฝ่าย/ภาค เขต" v-model="listStore.searchCriteria.departmentCode" hide-details
              :options="listStore.departmentDropdown" @enterClose="listStore.onGetPreProcurementListDataAsync" />
            <Select label="ปีงบประมาณ" v-model="listStore.searchCriteria.budgetYear" :options="YearOptions"
              @enterClose="listStore.onGetPreProcurementListDataAsync" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="วิธีการจัดหา" v-model="listStore.searchCriteria.supplyMethodCode" hide-details
              :options="listStore.supplyMethodDropdown" @enterClose="listStore.onGetPreProcurementListDataAsync" />
            <Select v-model="listStore.searchCriteria.supplyMethodTypeCode" hide-details
              :options="listStore.supplyMethodTypeDropdown" @enterClose="listStore.onGetPreProcurementListDataAsync" />
            <Select v-model="listStore.searchCriteria.supplyMethodSpecialTypeCode" hide-details
              :options="listStore.supplyMethidSpecialTypeDropDown"
              @enterClose="listStore.onGetPreProcurementListDataAsync" />
            <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end lg:col-end-6">
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
        <Button label="พิมพ์รายงานการจัดซื้อจัดจ้าง" icon="pi pi-file-export" variant="outlined"
          @click="() => listStore.exportExcelProcurementAsync()" />
      </div>
      <StatusGroupButton :optionBadges="listStore.statusOptionBadge" v-model="listStore.searchCriteria.step" />
      <DataView :value="listStore.table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as TPreProcurement[])" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-9">
                <InfoRow label="เลขที่จัดซื้อจัดจ้าง">
                  <p :class="['text-blue-400 hover:cursor-pointer w-fit', data.planNumber && 'underline']"
                    @click="() => router.push({ name: 'ppDetail', params: { id: data.id } })">
                    {{ data.procurementNumber || '-' }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อโครงการ">
                  <p class="font-bold line-clamp-2" :title="(data.planNumber || '-') + ' : ' + data.name">
                    {{ (data.planNumber || '-') + ' : ' + data.name }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินงบประมาณ">
                  <p>{{ formatCurrency(data.budget) }}</p>
                </InfoRow>
                <InfoRow label="ประเภทแผน">
                  <template v-if="data.type">
                    <TypeBadgeChip :label="PreProcurementTypeName(data.type)" :color="data.type" size="Small" />
                  </template>
                  <template v-else>-</template>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>{{ data.supplyMethod }}
                    <span v-if="data.supplyMethodSpecialType">
                      : {{ data.supplyMethodSpecialType }}
                    </span>
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-3 mb-2 lg:mb-0">
                <div class="mr-2 flex gap-2">
                  <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                    <span>ขั้นตอน :</span>
                    <span>สถานะ :</span>
                  </div>

                  <div class="flex flex-col gap-1 min-w-40">
                    <div class="flex items-center gap-1 px-1">
                      <span class="text-[#1D4ED8] leading-none">&#9679;</span>
                      <p class="whitespace-nowrap text-sm text-[#1D4ED8] font-bold">
                        {{ PreProcurementConstants.PreProcurementStepFullName(data.processType) }}
                      </p>
                      <BadgeStatus v-if="data.isCancel" class="ml-1" label="ขอยกเลิก" color="red" size="xs" />
                      <BadgeStatus v-else-if="data.isChange" class="ml-1" label="ขอแก้ไข" color="amber" size="xs" />
                    </div>

                    <BadgeStatus class="!w-full"
                      :color="resolveStatusBadge(data.processType, data.status).color"
                      :label="resolveStatusBadge(data.processType, data.status).label" />
                  </div>
                </div>

                <div class="flex items-center gap-1">
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text"
                    @click="() => router.push({ name: 'ppDetail', params: { id: data.id } })" />
                  <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                    variant="text" v-if="data.canDelete && menuStore.hasManage && data.departmentCode === authStore.profile.departmentCode"
                    @click="() => listStore.onDeleteAsync(data.id)" />
                </div>
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="listStore.searchCriteria.pageNumber" :page-size="listStore.searchCriteria.pageSize"
        :total-record="listStore.table.totalRecords" @change="listStore.onChangePageSizePPList" />
    </template>
  </Card>
</template>
