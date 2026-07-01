<script setup lang="ts">
import { formatCurrency } from '@/helpers/currency';
import { Pagination } from '@/components';
import { useWlStore } from '@/stores/WorkList/wl';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import router from '@/router';
import ContractManagementConstants from '@/constants/contractManagement';
import { souceType } from '@/enums/CM/cm001';
import type { WorklistContractAmendmentItemDTO, WorklistContractManagementItemDTO } from '@/models/WorkList/worklist';
import { ProcurementType } from '@/enums/procurement';
import { computed } from 'vue';

const store = useWlStore();

const { WorklistContractManagementStatusName, ContractManagementStepShortName } = ContractManagementConstants;

type CombinedItem =
  | (WorklistContractManagementItemDTO & { _itemType: 'cm' })
  | (WorklistContractAmendmentItemDTO & { _itemType: 'amendment' });

const combinedItems = computed((): CombinedItem[] => {
  const cmItems = (store.worklistRes.contractManagement?.page?.data ?? [])
    .map(d => ({ ...d, _itemType: 'cm' as const }));
  const amendItems = (store.worklistRes.contractAmendments?.page?.data ?? [])
    .map(d => ({ ...d, _itemType: 'amendment' as const }));
  return [...cmItems, ...amendItems];
});

const combinedTotal = computed(() =>
  (store.worklistRes.contractManagement?.page?.totalRecords ?? 0) +
  (store.worklistRes.contractAmendments?.page?.totalRecords ?? 0)
);

const getStatusColor = (statusCode: string): string => {
  if (statusCode.startsWith('Waiting') || statusCode === 'Pending' || statusCode === 'InProgress') {
    return 'yellow';
  }
  switch (statusCode) {
    case 'Draft':
    case 'Edit':
    case 'Cancelled':
    case 'NotInvited':
      return 'gray';
    case 'Approved':
    case 'Completed':
    case 'Paid':
    case 'Announcement':
      return 'green';
    case 'Rejected':
    case 'RejectToAssignee':
    case 'RejectPlan':
    case 'CancelPlan':
      return 'red';
    default:
      return 'gray';
  }
};

const onNavigateCm = (id: string, detailId: string, type?: string): void => {
  switch (type) {
    case 'DeliveryAcceptance':
      router.push(`/cm/cm001/detail/${detailId}/period/${id}`);
      return

    case 'DisbursementApproval':
      router.push(`/cm/cm004/detail/${detailId}/disbursement/${id}`);
      return

    case 'ContractTermination':
      router.push(`/cm/cm005/contract/${detailId}/detail/${id}`);
      return

    case 'ContractGuaranteeReturn':
      router.push(`/cm/cm006/detail/${detailId}/${id}`);
      return

    default: return;
  }
};

const onNavigateAmendment = (id: string, type?: string): void => {
  if (type === 'ContractDraftEditVendor') {
    router.push({ name: 'cm007Detail', params: { id } });
    return;
  }
  if (type === 'Rent') {
    router.push(`/pcm/pcm005/detail/${id}`);
    return;
  }
  router.push(`/pp/detail/${id}`);
};

const routeToRefData = (data: WorklistContractManagementItemDTO) => {
  if (!data.refId) return;

  if (data.sourceType == souceType.Plan) {
    const routeData = router.resolve({ name: 'pl001Detail', params: { id: data.refId } })
    window.open(routeData.href, '_blank');
  }

  if (data.sourceType == souceType.ContractDraftVendor || data.sourceType == souceType.ContractDraftVendorEdit) {
    if (data.processType) {
      onRounte(data.refId, data.processType)
    }
  }

  if (data.sourceType == souceType.Procurement) {
    const routeData = router.resolve({ name: 'pp001Detail', params: { id: data.refId } })
    window.open(routeData.href, '_blank');
  }
}

const onRounte = (id: string, type: string) => {
  const routeData = router.resolve({
    name: type === ProcurementType.Procurement ? 'ppDetail' : 'pcm005Detail',
    params: { id }
  });

  window.open(routeData.href, '_blank');
}
</script>

<template>
  <DataView :value="combinedItems" data-key="id">
    <template #list="{ items }">
      <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">

        <!-- ContractManagement items -->
        <template v-if="data._itemType === 'cm'">
          <div class="grid grid-cols-12 px-2" v-if="data.processType == 'DeliveryAcceptance'">
            <div class="col-span-12 lg:col-span-8" v-if="data.sourceType == souceType.Plan">
              <InfoRow label="เลขที่">
                <p class="text-blue-500 underline cursor-pointer" @click="() => routeToRefData(data)">
                  {{ data.contractNumber }}
                </p>
              </InfoRow>
              <InfoRow label="เลขที่รายการตรวจรับ">
                <p class="text-blue-500 underline cursor-pointer"
                  @click="() => onNavigateCm(data.id, data.detailId, data.processType)">
                  {{ data.periodNumber }}
                </p>
              </InfoRow>
              <InfoRow label="ชื่องาน">
                <p class="font-bold">{{ data.contractName }}</p>
              </InfoRow>
              <InfoRow label="วงเงินงบประมาณ">
                <p>{{ formatCurrency(data.budget) }}</p>
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                <p>{{ data.departmentName }}</p>
              </InfoRow>
              <InfoRow label="วิธีจัดหา">
                {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
              </InfoRow>
            </div>
            <div class="col-span-12 lg:col-span-8" v-if="data.sourceType == souceType.ContractDraftVendor">
              <InfoRow label="เลขที่">
                <p class="text-blue-500 underline cursor-pointer" @click="() => routeToRefData(data)">
                  {{ data.contractNumber }}
                </p>
              </InfoRow>
              <InfoRow label="เลขที่ PO (SAP)">
                <p>
                  {{ data.poNumber }}
                </p>
              </InfoRow>
              <InfoRow label="เลขที่รายการตรวจรับ">
                <p class="text-blue-500 underline cursor-pointer"
                  @click="() => onNavigateCm(data.id, data.detailId, data.processType)">
                  {{ data.periodNumber }}
                </p>
              </InfoRow>
              <InfoRow label="ชื่องาน">
                <p class="font-bold">{{ data.contractName }}</p>
              </InfoRow>
              <InfoRow label="คู่ค้า">
                <p>{{ data.vendorName }}</p>
              </InfoRow>
              <InfoRow label="วงเงินตามสัญญา">
                <p>{{ formatCurrency(data.budget) }}</p>
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                <p>{{ data.departmentName }}</p>
              </InfoRow>
              <InfoRow label="วิธีจัดหา">
                {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
              </InfoRow>
            </div>
            <div class="col-span-12 lg:col-span-8" v-if="data.sourceType == souceType.ContractDraftVendorEdit">
              <InfoRow label="เลขที่">
                <p class="text-blue-500 underline cursor-pointer" @click="() => routeToRefData(data)">
                  {{ data.contractNumber }}
                </p>
              </InfoRow>
              <InfoRow label="เลขที่ PO (SAP)">
                <p>{{ data.poNumber }}</p>
              </InfoRow>
              <InfoRow label="เลขที่รายการตรวจรับ">
                <p class="text-blue-500 underline cursor-pointer"
                  @click="() => onNavigateCm(data.id, data.detailId, data.processType)">
                  {{ data.periodNumber }}
                </p>
              </InfoRow>
              <InfoRow label="ชื่องาน">
                <p class="font-bold">{{ data.contractName }}</p>
              </InfoRow>
              <InfoRow label="คู่ค้า">
                <p>{{ data.vendorName }}</p>
              </InfoRow>
              <InfoRow label="วงเงินตามสัญญา">
                <p>{{ formatCurrency(data.budget) }}</p>
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                <p>{{ data.departmentName }}</p>
              </InfoRow>
              <InfoRow label="วิธีจัดหา">
                {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
              </InfoRow>
            </div>
            <div class="col-span-12 lg:col-span-8" v-if="data.sourceType == souceType.Procurement">
              <InfoRow label="เลขที่">
                <p class="text-blue-500 underline cursor-pointer" @click="() => routeToRefData(data)">
                  {{ data.procurementNumber }}
                </p>
              </InfoRow>
              <InfoRow label="เลขที่รายการตรวจรับ">
                <p class="text-blue-500 underline cursor-pointer"
                  @click="() => onNavigateCm(data.id, data.detailId, data.processType)">
                  {{ data.periodNumber }}
                </p>
              </InfoRow>
              <InfoRow label="ชื่องาน">
                <p class="font-bold">{{ data.contractName }}</p>
              </InfoRow>
              <InfoRow label="วงเงินงบประมาณ">
                <p>{{ formatCurrency(data.budget) }}</p>
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                <p>{{ data.departmentName }}</p>
              </InfoRow>
              <InfoRow label="วิธีจัดหา">
                {{ data.supplyMethod }} {{ data.supplyMethodType }} {{ data.supplyMethodSpecialType }}
              </InfoRow>
            </div>
            <div
              class="col-span-12 lg:col-span-4 flex items-center justify-end lg:items-start gap-1.5 order-1 lg:order-2 mb-2 lg:mb-0">
              <div class="mr-2 flex gap-2">
                <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                  <span>โปรแกรม :</span>
                  <span>สถานะ :</span>
                </div>
                <div class="flex flex-col gap-1 min-w-40">
                  <BadgeStatus class="!w-full text-sm!" text-color-class="text-[#1D4ED8]" :label="ContractManagementStepShortName(data.processType)" />
                  <BadgeStatus class="!w-full text-lg! leading-5!"
                    :color="getStatusColor(data.status ?? '')"
                    :label="WorklistContractManagementStatusName(data.status)" />
                </div>
              </div>
            </div>
          </div>
          <div class="grid grid-cols-12 px-2" v-if="data.processType !== 'DeliveryAcceptance'">
            <div class="col-span-12 lg:col-span-8">
              <InfoRow label="เลขที่รายการจัดซื้อจัดจ้าง">
                <button class="cursor-pointer" type="button"
                  @click="() => onNavigateCm(data.id, data.detailId, data.processType)">
                  <p class="underline text-blue-400 items-center w-fit">
                    {{ data.contractNumber ?? '-' }}
                  </p>
                </button>
              </InfoRow>
              <InfoRow label="ชื่อโครงการ">
                <p class="font-bold grid">
                  {{ data.contractName ?? '-' }}
                </p>
              </InfoRow>
              <InfoRow label="วงเงินงบประมาณ">
                <p>
                  {{ formatCurrency(data.budget) }}
                </p>
              </InfoRow>
            </div>
            <div
              class="col-span-12 lg:col-span-4 flex items-center justify-end lg:items-start gap-1.5 order-1 lg:order-2 mb-2 lg:mb-0">
              <div class="mr-2 flex gap-2">
                <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                  <span>โปรแกรม :</span>
                  <span>สถานะ :</span>
                </div>
                <div class="flex flex-col gap-1 min-w-40">
                  <BadgeStatus class="!w-full text-sm!" text-color-class="text-[#1D4ED8]" :label="ContractManagementStepShortName(data.processType)" />
                  <BadgeStatus class="!w-full text-lg! leading-5!"
                    :color="getStatusColor(data.status ?? '')"
                    :label="WorklistContractManagementStatusName(data.status)" />
                </div>
              </div>
              <Button icon="pi pi-arrow-circle-right" label="ไปยังเอกสาร"
                class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20! text-nowrap mt-0.5" size="large"
                variant="text"
                @click="() => onNavigateCm(data.id, data.detailId, data.processType)" />
            </div>
          </div>
        </template>

        <!-- ContractAmendment items -->
        <template v-else>
          <div class="grid grid-cols-12 px-2">
            <div class="col-span-12 lg:col-span-8">
              <InfoRow label="เลขที่">
                <button class="cursor-pointer" type="button" @click="() => onNavigateAmendment(data.id, data.type)">
                  <p class="underline text-blue-400 items-center w-fit">
                    {{ data.camContractAmendmentNumber ?? '-' }}
                  </p>
                </button>
              </InfoRow>
              <InfoRow label="ชื่องาน">
                <p class="font-bold grid">
                  {{ data.contractName ?? '-' }}
                </p>
              </InfoRow>
              <InfoRow label="วงเงินงบประมาณ">
                <p>
                  {{ formatCurrency(data.budget) }}
                </p>
              </InfoRow>
              <InfoRow label="ฝ่าย/ภาคเขต">
                <p>
                  {{ data.department ?? '-' }}
                </p>
              </InfoRow>
            </div>
            <div
              class="col-span-12 lg:col-span-4 flex items-center justify-end lg:items-start gap-1.5 order-1 lg:order-2 mb-2 lg:mb-0">
              <div class="mr-2 flex gap-2">
                <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                  <span>โปรแกรม :</span>
                </div>
                <div class="flex flex-col gap-1 min-w-40">
                  <BadgeStatus class="!w-full text-sm!" text-color-class="text-[#1D4ED8]" :label="data.type === 'ContractDraftEditVendor' ? 'บันทึกต่อท้ายสัญญา' : 'การจัดการสัญญา'" />
                </div>
              </div>
              <Button icon="pi pi-arrow-circle-right" label="ไปยังเอกสาร"
                class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20! text-nowrap mt-0.5" size="large"
                variant="text"
                @click="() => onNavigateAmendment(data.id, data.type)" />
            </div>
          </div>
        </template>

      </div>
    </template>
    <template #empty>
      <p class="text-center">ไม่พบข้อมูล</p>
    </template>
  </DataView>
  <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
    :total-record="combinedTotal" @change="store.onChangePageSizeAsync" />
</template>
