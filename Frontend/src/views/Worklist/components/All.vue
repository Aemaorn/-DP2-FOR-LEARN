<script setup lang="ts">
import type { PlanStatus } from '@/enums/plan';
import type { PlanAnnouncementStatus } from '@/enums/planAnnouncement';
import { formatCurrency } from '@/helpers/currency';
import { BadgeStatus, Pagination } from '@/components';
import { useWlStore } from '@/stores/WorkList/wl';
import { PlanAnnouncementConstants, ProcurementConstants } from '@/constants';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import PlanConstant from '@/constants/plan';
import router from '@/router';
import ContractManagementConstants from '@/constants/contractManagement';
import type { ContractManagementStatus } from '@/enums/contractManagement';
import { PreProcurementStep } from '@/enums/preProcurement';

const { WorkListPlanStatusName } = PlanConstant;
const { WorklistAnnouncementStatusName } = PlanAnnouncementConstants;
const { WorklistChildStatusName } = ProcurementConstants;
const { WorklistContractManagementStatusName, WorklistGuaranteeReturnStatusName } = ContractManagementConstants;
const store = useWlStore();

const getStatusColor = (statusCode: string, processType?: string): string => {
  if (processType === PreProcurementStep.ContractDraft && statusCode === 'Approved') {
    return 'yellow';
  }
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
    case 'RejectedToAssignee':
    case 'RejectPlan':
    case 'CancelPlan':
    case 'AccountingRejected':
      return 'red';
    default:
      return 'gray';
  }
};

const getStatusName = (source: string, statusCode: string, processType?: string): string => {
  if (processType === PreProcurementStep.ContractDraft && statusCode === 'Approved') {
    return 'รอบันทึกวันที่ลงนาม';
  }
  switch (source) {
    case 'Plan':
      return WorkListPlanStatusName(statusCode as PlanStatus);

    case 'PlanAnnouncement':
      return WorklistAnnouncementStatusName(statusCode as PlanAnnouncementStatus);

    case 'PreProcurement':
    case 'Procurement':
    case 'ContractAgreement':
    case 'ContractAmendment':
    case 'ContractTermination':
    case 'ExpenseDisbursement':
      return WorklistChildStatusName(statusCode);

    case 'DeliveryPeriods': {
      const cmName = WorklistContractManagementStatusName(statusCode as ContractManagementStatus);
      return cmName || WorklistChildStatusName(statusCode);
    }

    case 'ContractManagement':
      return WorklistContractManagementStatusName(statusCode as ContractManagementStatus);

    case 'GuaranteeReturn':
      return WorklistGuaranteeReturnStatusName(statusCode);

    default:
      return WorklistChildStatusName(statusCode);
  }
};

const getSourceTypeLabel = (source: string): string => {
  switch (source) {
    case 'Plan':                return 'แผนจัดซื้อจัดจ้าง';
    case 'PlanAnnouncement':    return 'ขออนุมัติเผยแพร่แผนฯ';
    case 'PreProcurement':
    case 'Procurement':
    case 'ContractAgreement':   return 'การจัดซื้อจัดจ้าง';
    case 'ContractManagement':  return 'การบริหารสัญญา';
    case 'DeliveryPeriods':     return 'บันทึกตรวจรับ (จพ.008)';
    case 'ContractTermination': return 'บอกเลิกสัญญา';
    case 'GuaranteeReturn':     return 'คืนหลักประกันสัญญา';
    case 'ExpenseDisbursement': return 'เบิกจ่าย';
    case 'ContractAmendment':   return 'บันทึกต่อท้ายสัญญา';
    default:                    return '';
  }
};

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const getSourceTypeColor = (): { bgColorClass: string; textColorClass: string } => {
  return { bgColorClass: 'bg-[#1D4ED8]', textColorClass: 'text-white' };
};

const onRouteToDetail = (source: string, id: string, detailId?: string, type?: string, processType?: string, contractDraftVendorId?: string): void => {
  switch (source) {
    case 'Plan':
      router.push(`/pl/pl001/detail/${id}`);
      return;

    case 'PlanAnnouncement':
      router.push(`/pl/pl002/detail/${id}`);
      return;

    case 'PreProcurement':
    case 'Procurement':
    case 'ContractAgreement':
      onRouteProcurementToDetail(id, type, processType, contractDraftVendorId);
      return;

    case 'ContractManagement':
    case 'DeliveryPeriods':
    case 'ContractTermination':
    case 'GuaranteeReturn':
      onContractManagementNavigate(id, detailId, type);
      return;

    case 'ContractAmendment':
      router.push({ name: 'cm007Detail', params: { id } });
      return;

    case '':
      return;

    default:
      return;
  }
};

const onRouteProcurementToDetail = (id: string, type?: string, processType?: string, contractDraftVendorId?: string) => {
  const vendorQuery = processType === PreProcurementStep.ContractDraft && contractDraftVendorId
    ? `?vendorId=${contractDraftVendorId}`
    : '';

  switch (type) {
    case 'Rent':
      router.push(`/pcm/pcm005/detail/${id}${vendorQuery}`);
      return;

    case 'Procurement':
      router.push(`/pp/detail/${id}${vendorQuery}`);
      return;

    case 'Pw119':
      router.push(`/pcm/pcm002/detail/${id}`);
      return;

    case 'P79Clause2':
      router.push(`/pcm/pcm003/detail/${id}`);
      return;

    case 'PettyCash':
      router.push(`/pcm/pcm004/detail/${id}`);
      return;

    case 'PettyCashReimbursement':
      router.push(`/pcm/pcm006/detail/${id}`);
      return;

    default:
      return;
  }
};

const onContractManagementNavigate = (id: string, detailId?: string, type?: string): void => {
  switch (type) {
    case 'DeliveryAcceptance':
      router.push(`/cm/cm001/detail/${detailId}/period/${id}`);
      return;

    case 'DisbursementApproval':
      router.push(`/cm/cm004/detail/${detailId}/disbursement/${id}`);
      return;

    case 'ContractTermination':
      router.push(`/cm/cm005/contract/${detailId}/detail/${id}`);
      return;

    case 'ContractGuaranteeReturn':
      router.push(`/cm/cm006/detail/${detailId}/${id}`);
      return;

    default:
      return;
  }
};
</script>

<template>
  <DataView :value="store.worklistRes.combined?.data" data-key="id">
    <template #list="{ items }">
      <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
        <div class="grid grid-cols-12 px-2">
          <div class="col-span-12 lg:col-span-8 order-2 lg:order-1">
            <InfoRow v-if="data.planNumber" label="เลขที่รายการจัดซื้อจัดจ้าง">
              <p class="font-bold">
                {{ data.planNumber }}
              </p>
            </InfoRow>
            <InfoRow label="เลขที่">
              <button class="cursor-pointer" type="button"
                @click="() => onRouteToDetail(data.source, data.id, data.detailId, data.type, data.processType, data.contractDraftVendorId)">
                <p class="underline text-blue-400 items-center w-fit">
                  {{ data.number ?? '-' }}
                </p>
              </button>
            </InfoRow>
            <InfoRow label="ชื่องาน">
              <p class="font-bold grid">
                {{ data.name ?? '-' }}
              </p>
            </InfoRow>
            <InfoRow v-if="data.vendorName" label="ผู้ขาย/ผู้รับจ้าง">
              <p>
                {{ data.vendorName }}
              </p>
            </InfoRow>
            <InfoRow label="วงเงินงบประมาณ">
              <p>
                {{ formatCurrency(data.summaryBudget) }}
              </p>
            </InfoRow>
            <InfoRow label="ฝ่าย/ภาคเขต">
              <p>
                {{ data.departmentName ?? '-' }}
              </p>
            </InfoRow>
            <InfoRow label="วิธีการจัดหา">
              <p>
                {{ data.supplyMethodName ?? '-' }}
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

          <div
            class="col-span-12 lg:col-span-4 flex items-center justify-end lg:items-start gap-1.5 order-1 lg:order-2 mb-2 lg:mb-0">
            <div class="mr-2 flex gap-2">
              <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                <span>โปรแกรม :</span>
                <span>สถานะ :</span>
              </div>
              <div class="flex flex-col gap-1 min-w-40">
                <BadgeStatus class="!w-full text-sm!" text-color-class="text-[#1D4ED8]" :label="getSourceTypeLabel(data.source)" />
                <BadgeStatus class="!w-full text-lg! leading-5!"
                  :color="getStatusColor(data.statusCode, data.processType)"
                  :label="getStatusName(data.source, data.statusCode, data.processType)" />
              </div>
            </div>
            <Button icon="pi pi-arrow-circle-right" label="ไปยังเอกสาร"
              class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20! text-nowrap mt-0.5" size="large"
              variant="text"
              @click="() => onRouteToDetail(data.source, data.id, data.detailId, data.type, data.processType, data.contractDraftVendorId)" />
          </div>
        </div>
      </div>
    </template>
    <template #empty>
      <p class="text-center">ไม่พบข้อมูล</p>
    </template>
  </DataView>
  <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
    :total-record="store.worklistRes.combined?.totalRecords" @change="store.onChangePageSizeAsync" />
</template>
