<script setup lang="ts">
import { BadgeStatus } from '@/components';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, StatusGroupButton, CriteriaGroupButton, Select } from '@/components/forms';
import { SharedConstants } from '@/constants';
import PreProcurementConstants from '@/constants/preProcurement';
import { usePcm005ListStore } from '@/stores/PCM/PCM005/pcm005';
import { onBeforeMount, onMounted, watch } from 'vue';
import type { TPreProcurement } from '@/models/PP/ppModel';
import procurementHelper from '@/helpers/procurement';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { PreProcurementStep } from '@/enums/preProcurement';
import { useMenuStore } from '@/stores/menu';
import { useRouter } from 'vue-router';
import { YearOptions } from '@/constants/date';
import type { ColorLabel } from '@/models/shared/color';
import AppointmentHelper from '@/helpers/appointment';
import TorDraftConstant from '@/constants/torDraft';
import MedianPriceHelper from '@/helpers/medianPrice';
import PurchaseRequisitionHelper from '@/helpers/purchaseRequisition';
import Jp005Helper from '@/helpers/jp005';
import InviteConstant from '@/constants/invite';
import PoaConstant from '@/constants/poa';
import ContractInvitationHelper from '@/helpers/contractInvitation';
import principleConstant from '@/constants/PCM005/principle';
import principleApprovalRentalConstant from '@/constants/PCM005/principleApprovalRental';
import { ProcurementStatus } from '@/enums/procurement';
import { useAuthenticationStore } from '@/stores/authentication';

const router = useRouter();
const store = usePcm005ListStore();
const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const { WorkProcessOptions } = SharedConstants;
const { ChildStatusBadge } = procurementHelper;

const resolveStatusBadge = (processType: PreProcurementStep, status: string, procurementStatus: ProcurementStatus): ColorLabel => {
  if (procurementStatus === ProcurementStatus.Cancelled) {
    return procurementHelper.ProcurementBadgeStatus(procurementStatus);
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
    case PreProcurementStep.PrincipleApproval:
      return principleConstant.principleStatusColor(status as any);
    case PreProcurementStep.PrincipleApprovalRental:
      return principleApprovalRentalConstant.principleApprovalRentalStatusColor(status as any);
    default:
      return ChildStatusBadge(status);
  }
};


const routeToDetail = (id?: string): void => {
  const route = '/pcm/pcm005/detail';
  const finalRoute = id ? `${route}/${id}` : route;

  router.push(finalRoute);
};

onBeforeMount(async () => {
  await store.fn.getDropdownAsync();
});

onMounted(async () => {
  await store.fn.onGetListAsync();
});

watch(
  () => [
    store.searchCriteria.pageNumber,
    store.searchCriteria.pageSize,
    store.searchCriteria.workProcess,
    store.searchCriteria.step,
  ],
  async (): Promise<void> => {
    await store.fn.onGetListAsync();
  }
);

const onChangeSmCodeAsync = async (code?: string) => {
  store.dropdown.smSpecialTypeCodeDropdown = [];
  store.searchCriteria.supplyMethodSpecialTypeCode = undefined;
  store.searchCriteria.supplyMethodTypeCode = undefined;

  if (code) {
    await store.fn.getSmSpecialTypeCodeDropdownAsync(code);
  }
};
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="เช่าพื้นที่ทำการสาขา">
      <template #action>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => routeToDetail()" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>

    <Card class="my-4">
      <template #content>
        <Form @submit="store.fn.onGetListAsync">
          <CriteriaGroupButton :options="WorkProcessOptions" v-model="store.searchCriteria.workProcess" />
          <div class="mt-10 space-y-2 lg:space-y-4">
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
              <InputField label="คำค้นหา" v-model.trim="store.searchCriteria.keyword" />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select class="lg:col-span-2" label="ฝ่าย/ภาค เขต" v-model="store.searchCriteria.departmentCode"
                :options="store.dropdown.departmentDropdown" @enterClose="store.fn.onGetListAsync" />
              <Select label="ปีงบประมาณ" v-model="store.searchCriteria.budgetYear" :options="YearOptions"
                @enterClose="store.fn.onGetListAsync" hide-details />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select label="วิธีการจัดหา" v-model="store.searchCriteria.supplyMethodCode"
                :options="store.dropdown.smCodeDropdown" @onSelect="(e: string) => onChangeSmCodeAsync(e)"
                @enterClose="store.fn.onGetListAsync" />

              <Select v-model="store.searchCriteria.supplyMethodTypeCode" :options="store.dropdown.smTypeCodeDropdown"
                @enterClose="store.fn.onGetListAsync" />

              <Select v-model="store.searchCriteria.supplyMethodSpecialTypeCode"
                :options="store.dropdown.smSpecialTypeCodeDropdown" @enterClose="store.fn.onGetListAsync" />
              <div class="lg:col-span-2 flex items-end justify-end gap-2">
                <ButtonSearch class="lg:w-fit w-full" type="submit" />
                <ButtonClear class="lg:w-fit w-full" @click="() => store.fn.onResetCriteria()" />
              </div>
            </div>
          </div>
        </Form>
      </template>
    </Card>

    <Card>
      <template #content>
        <StatusGroupButton :optionBadges="store.statusOptionBadge" v-model="store.searchCriteria.step" />
        <DataView :value="store.table.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data) in (items as Array<TPreProcurement>)" :key="data.id"
              class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่">
                    <p class="underline text-blue-400 cursor-pointer w-fit" @click="() => routeToDetail(data.id)">
                      {{ data.procurementNumber }}
                    </p>
                  </InfoRow>
                  <InfoRow label="เรื่อง">
                    <p class="font-bold">{{ data.name }}</p>
                  </InfoRow>
                  <InfoRow label="ระยะเวลา">
                    <p>{{ data.period || '-' }}</p>
                  </InfoRow>
                  <InfoRow label="รูปแบบสัญญาเช่า">
                    <p>{{ data.rentTypeName || '-' }}</p>
                  </InfoRow>
                  <InfoRow label="ฝ่าย/ภาคเขต">
                    <p>{{ data.departmentName }}</p>
                  </InfoRow>
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
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
                      </div>
                      <BadgeStatus class="!w-full"
                        :color="resolveStatusBadge(data.processType, data.status, data.procurementStatus).color"
                        :label="resolveStatusBadge(data.processType, data.status, data.procurementStatus).label" />
                    </div>
                  </div>
                  <div class="flex items-center gap-1">
                    <Button icon="pi pi-pen-to-square"
                      class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!" size="small" variant="text"
                      @click="() => routeToDetail(data.id)" />
                    <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                      size="small" variant="text" v-if="data.canDelete && data.departmentCode === authStore.profile.departmentCode"
                      @click="() => store.fn.onDeleteAsync(data.id)" />
                  </div>
                </div>
              </div>
            </div>
          </template>

          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>

        <Pagination :page-number="store.searchCriteria.pageNumber" :page-size="store.searchCriteria.pageSize"
          :total-record="store.table.totalRecords" @change="store.fn.onChangePageSize" />
      </template>
    </Card>
  </div>
</template>