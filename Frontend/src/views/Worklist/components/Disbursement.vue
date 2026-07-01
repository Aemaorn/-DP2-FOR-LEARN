<script setup lang="ts">
import { formatCurrency } from '@/helpers/currency';
import { BadgeStatus, Pagination } from '@/components';
import { useWlStore } from '@/stores/WorkList/wl';
import { ToTHDateFullMonthOnly } from '@/helpers/dateTime';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import router from '@/router';
import type { sourceType } from '@/enums/AC/ac01';
import AC01Helper from '@/helpers/AC/ac01';

const store = useWlStore();
const { SourceTypeName } = AC01Helper;

const onNavigate = (id: string, type?: string): void => {
  if (type === 'Rent') {
    router.push(`/pcm/pcm005/detail/${id}`);

    return;
  }

  router.push(`/pp/detail/${id}`);
};
</script>

<template>
  <DataView :value="store.worklistRes.expenseDisbursements?.page?.data" data-key="id">
    <template #list="{ items }">
      <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
        <div class="grid grid-cols-12 px-2">
          <div class="lg:col-span-8">
            <InfoRow label="เลขที่">
              <button class="cursor-pointer" type="button" @click="() => onNavigate(data.id, data.type)">
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
            <InfoRow label="จำนวนเงิน">
              <p>
                {{ formatCurrency(data.budget) }}
              </p>
            </InfoRow>
            <InfoRow label="ประเภทงาน">
              <StatusChip color="gray" :label="SourceTypeName(data.sourceType as sourceType) ?? ''"
                class="w-fit text-center justify-center" />
            </InfoRow>
            <InfoRow label="วันที่จัดส่งเอกสาร">
              <p> {{ ToTHDateFullMonthOnly(data.date) }} </p>
            </InfoRow>
            <InfoRow label="วันที่เบิกจ่าย">
              <p> {{ ToTHDateFullMonthOnly(data.advancePaymentDate) }} </p>
            </InfoRow>
          </div>
          <div
            class="col-span-12 lg:col-span-4 flex items-center justify-end lg:items-start gap-1.5 order-1 lg:order-2 mb-2 lg:mb-0">
            <div class="mr-2 flex gap-2">
              <div class="flex flex-col gap-1 text-sm text-gray-500 text-right whitespace-nowrap">
                <span>โปรแกรม :</span>
              </div>
              <div class="flex flex-col gap-1 min-w-40">
                <BadgeStatus class="!w-full text-sm!" text-color-class="text-[#1D4ED8]" label="การเบิกจ่าย" />
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
    :total-record="store.worklistRes.expenseDisbursements?.page?.totalRecords" @change="store.onChangePageSizeAsync" />
</template>
