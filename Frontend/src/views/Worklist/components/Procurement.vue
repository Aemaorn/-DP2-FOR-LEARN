<script setup lang="ts">
import { formatCurrency } from '@/helpers/currency';
import { BadgeStatus, Pagination } from '@/components';
import { useWlStore } from '@/stores/WorkList/wl';
import { PreProcurementConstants, ProcurementConstants } from '@/constants';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import router from '@/router';

const { WorklistChildStatusName } = ProcurementConstants;
const { PreProcurementStepShortName } = PreProcurementConstants;
const store = useWlStore();

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

const onNavigate = (id: string, type?: string): void => {
  switch (type) {
    case 'Rent':
      router.push(`/pcm/pcm005/detail/${id}`);
      return

    case 'Procurement':
      router.push(`/pp/detail/${id}`);
      return

    case 'Pw119':
      router.push(`/pcm/pcm002/detail/${id}`);
      return

    case 'P79Clause2':
      router.push(`/pcm/pcm003/detail/${id}`);
      return

    case 'PettyCashReimbursement':
      router.push(`/pcm/pcm006/detail/${id}`);
      return

    default: return;
  }
};
</script>

<template>
  <DataView :value="store.worklistRes.procurement?.page?.data" data-key="id">
    <template #list="{ items }">
      <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
        <div class="grid grid-cols-12 px-2">
          <div class="lg:col-span-8">
            <InfoRow label="เลขที่">
              <button class="cursor-pointer" type="button" @click="() => onNavigate(data.id, data.type)">
                <p class="underline text-blue-400 items-center w-fit">
                  {{ data.procurementNumber ?? '-' }}
                </p>
              </button>
            </InfoRow>
            <InfoRow label="ชื่องาน">
              <p class="font-bold grid">
                {{ data.name ?? '-' }}
              </p>
            </InfoRow>
            <InfoRow label="วงเงินงบประมาณ">
              <p>
                {{ formatCurrency(data.budget) }}
              </p>
            </InfoRow>
            <InfoRow label="ฝ่าย/ภาคเขต">
              <p>
                {{ data.departmentName ?? '-' }}
              </p>
            </InfoRow>
            <InfoRow label="วิธีจัดหา">
              <p>
                {{ data.supplyMethod ?? '-' }}
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
                <BadgeStatus class="!w-full text-sm!" text-color-class="text-[#1D4ED8]" :label="PreProcurementStepShortName(data.processType)" />
                <BadgeStatus class="!w-full text-lg! leading-5!"
                  :color="getStatusColor(data.status)"
                  :label="WorklistChildStatusName(data.status)" />
              </div>
            </div>
            <Button icon="pi pi-arrow-circle-right" label="ไปยังเอกสาร"
              class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20! text-nowrap mt-0.5" size="large"
              variant="text"
              @click="() => onNavigate(data.id, data.type)" />
          </div>
        </div>
      </div>
    </template>
    <template #empty>
      <p class="text-center">ไม่พบข้อมูล</p>
    </template>
  </DataView>
  <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
    :total-record="store.worklistRes.procurement?.page?.totalRecords" @change="store.onChangePageSizeAsync" />
</template>
