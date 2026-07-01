<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { Checkbox } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import ST003Service from '@/services/file';
import { useAttachmentFileAllStore } from '@/stores/attachmentFileAll';
import { useAuthenticationStore } from '@/stores/authentication';
import { PlanDepartmentCode } from '@/enums/plan';
import type { TAttachmentFileItem } from '@/models/attachmentFileAll';

const route = useRoute();
const router = useRouter();
const store = useAttachmentFileAllStore();
const { searchCriteria, body } = storeToRefs(store);
const { onSearchAsync } = store;

const expandedSubs = ref<string[]>([]);

onMounted(async (): Promise<void> => {
  const procurementId = route.params.procurementId as string;
  if (procurementId) {
    searchCriteria.value.procurementId = procurementId;
    await onSearchAsync();
    body.value?.stages.forEach((stage): void => {
      stage.subSections.forEach((sub): void => {
        if (sub.groups.some((g): boolean => g.files.length > 0)) {
          expandedSubs.value.push(subKey(stage.stageName, sub.label));
        }
      });
    });
  }
});

const subKey = (stageName: string, label: string): string => `${stageName}::${label}`;

const toggleSub = (stageName: string, label: string): void => {
  const key = subKey(stageName, label);
  const idx = expandedSubs.value.indexOf(key);
  if (idx >= 0) {
    expandedSubs.value.splice(idx, 1);
  } else {
    expandedSubs.value.push(key);
  }
};

const isSubExpanded = (stageName: string, label: string): boolean =>
  expandedSubs.value.includes(subKey(stageName, label));

const getFileIcon = (fileName: string): string => {
  const ext = fileName.split('.').pop()?.toLowerCase() ?? '';
  if (ext === 'pdf') return 'pi pi-file-pdf text-red-500';
  if (['xlsx', 'xls'].includes(ext)) return 'pi pi-file-excel text-green-600';
  if (['docx', 'doc'].includes(ext)) return 'pi pi-file-word text-blue-600';
  if (['png', 'jpg', 'jpeg', 'gif'].includes(ext)) return 'pi pi-image text-purple-500';
  return 'pi pi-file text-gray-500';
};

const authStore = useAuthenticationStore();

const canAccessFile = (file: TAttachmentFileItem): boolean =>
  file.isPublic || file.createdBy === authStore.profile.id || authStore.profile.departmentCode === PlanDepartmentCode.JP;

const onDownload = async (file: TAttachmentFileItem): Promise<void> => {
  if (!canAccessFile(file)) return;
  await ST003Service.downloadFile(file.fileId, file.fileName);
};

const onNavigate = (type: 'Plan' | 'Procurement', itemId: string): void => {
  let path = '';
  switch (type) {
    case 'Plan':
      path = `/pl/pl001/detail/${itemId}`;
      break;
    case 'Procurement':
      path = `/pp/detail/${itemId}`;
      break;
    default:
      return;
  }
  window.open(router.resolve(path).href, '_blank');
};
</script>

<template>
  <div v-if="body" class="flex flex-col gap-4">
    <TitleHeader label="รวบรวมเอกสารแนบทั้งหมด" />
    <Card>
      <template #content>
        <TitleHeader label="ข้อมูลจัดซื้อจัดจ้าง" hidden-icon />
        <div class="px-4 mt-2 space-y-4">
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <InfoItem title="เลขที่รายการจัดซื้อจัดจ้าง">
              <template #content>
                <span
                  v-if="body.planId"
                  class="text-blue-500 underline cursor-pointer"
                  @click="onNavigate('Plan', body.planId)"
                >{{ body.planNumber }}</span>
                <span v-else>{{ body.planNumber || '-' }}</span>
              </template>
            </InfoItem>
            <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
              <template #content>
                <span
                  class="text-blue-500 underline cursor-pointer"
                  @click="onNavigate('Procurement', body.procurementId)"
                >{{ body.procurementNumber }}</span>
              </template>
            </InfoItem>
            <InfoItem title="ฝ่าย/ภาคเขต">
              <template #content>{{ body.departmentName ?? '-' }}</template>
            </InfoItem>
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <InfoItem title="โครงการ">
              <template #content>{{ body.projectName }}</template>
            </InfoItem>
              <InfoItem title="ปีงบประมาณ">
              <template #content>{{ body.budgetYear ?? '-' }}</template>
            </InfoItem>
            <InfoItem title="วงเงินประมาณการ (บาท)">
              <template #content>{{ formatCurrency(body.budget) }}</template>
            </InfoItem>
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
            <InfoItem title="วิธีจัดหา">
              <template #content>{{ body.supplyMethod ?? '-' }}</template>
            </InfoItem>
            <InfoItem title="">
              <template #content>{{ body.supplyMethodType ?? '' }}</template>
            </InfoItem>
            <InfoItem title="">
              <template #content>{{ body.supplyMethodSpecialType ?? '' }}</template>
            </InfoItem>
          </div>
        </div>
      </template>
    </Card>

    <Card v-for="stage in body.stages" :key="stage.stageName">
      <template #content>
        <div class="-mx-5 -mt-5 px-5 py-3 border-b border-gray-200">
          <TitleHeader :label="stage.stageName" :hidden-line="true" />
        </div>
        <div class="mt-4 space-y-5">
          <div v-for="sub in stage.subSections" :key="sub.label">
            <div
              class="flex items-center justify-between cursor-pointer select-none px-4 py-2 mb-2 bg-gray-100 rounded"
              @click="toggleSub(stage.stageName, sub.label)"
            >
              <p class="font-semibold text-gray-700">{{ sub.label }}</p>
              <i
                :class="isSubExpanded(stage.stageName, sub.label) ? 'pi pi-chevron-up' : 'pi pi-chevron-down'"
                class="text-gray-400 text-xs shrink-0"
              />
            </div>
            <div v-if="isSubExpanded(stage.stageName, sub.label)">
              <div v-if="sub.groups.length === 0" class="text-center text-gray-400 text-sm py-2">
                ไม่พบเอกสารแนบ
              </div>
              <div v-for="group in sub.groups" :key="group.refNumber" class="mb-3 pl-4">
                <p class="text-lg font-medium text-gray-700 mb-2">{{ group.refNumber }}</p>
                <div v-for="file in group.files" :key="file.fileId">
                  <div class="flex w-full items-center justify-between mt-4">
                    <div class="flex items-center gap-3 min-w-0">
                      <p class="shrink-0 w-5 text-right">{{ file.sequence }}.</p>
                      <Checkbox
                        v-model="file.isPublic"
                        :true-value="false"
                        :false-value="true"
                        label="เอกสารส่วนบุคคล"
                        class="shrink-0"
                        hide-details
                        disabled
                      />
                      <div class="w-px h-5 bg-gray-300 shrink-0" />
                      <div class="flex items-center gap-2 min-w-0">
                        <i :class="getFileIcon(file.fileName)" class="text-lg shrink-0" />
                        <p
                          class="truncate"
                          :class="canAccessFile(file) ? 'text-blue-500 cursor-pointer underline' : 'text-gray-400'"
                          @click="() => onDownload(file)"
                        >{{ file.fileName }}</p>
                      </div>
                    </div>
                  </div>
                  <div class="h-px bg-gray-300 flex-1 mt-4" />
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </Card>

  </div>
</template>
