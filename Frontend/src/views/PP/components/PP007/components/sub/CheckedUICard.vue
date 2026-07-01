<script setup lang="ts">
import { Button } from 'primevue';
import { ToDateTime } from '@/helpers/dateTime';
import type { PP007EntrepreneurCheckConditions } from '@/views/PP/models/PP007/pp007Model';
import { PP006EntrepreneurType } from '@/views/PP/enums/pp006';

defineProps<{
  type: 'Coi' | 'Watchlist' | 'Egp';
  idx: number;
  item?: PP007EntrepreneurCheckConditions;
}>();

const emit = defineEmits<{
  check: [entype: string, idx: number];
}>();

const labelMap = {
  Coi:       { label: 'Coi',               entype: PP006EntrepreneurType.COI },
  Watchlist: { label: 'Watchlist',         entype: PP006EntrepreneurType.Watchlist },
  Egp:       { label: 'ผู้ทิ้งงาน (e-GP)', entype: PP006EntrepreneurType.EGP },
};
</script>

<template>
  <div class="flex flex-col h-full">
    <p class="font-bold text-nowrap text-center">{{ labelMap[type].label }}</p>

    <template v-if="item">
      <div class="flex flex-col items-center gap-y-1 mt-2 px-4">
        <span v-if="item.result === true" class="material-symbols-outlined text-green-400">check_circle</span>
        <span v-else-if="item.result === false && item.date" class="material-symbols-outlined text-red-400">cancel</span>
        <span v-else-if="item.result == null" class="material-symbols-outlined text-[#F9A825]">error</span>

        <p v-if="item.date && item.result != null" class="text-gray-400 text-[16px]">
          ตรวจวันที่ : {{ ToDateTime(item.date) }}
        </p>
      </div>

      <Button
        label="ตรวจสอบ"
        severity="success"
        rounded
        class="w-full mt-auto"
        @click="emit('check', labelMap[type].entype, idx)"
      />
    </template>
    <template v-else>
      <Button
        label="ตรวจสอบ"
        severity="warn"
        rounded
        class="w-full mt-auto"
        @click="emit('check', labelMap[type].entype, idx)"
      />
    </template>
  </div>
</template>
