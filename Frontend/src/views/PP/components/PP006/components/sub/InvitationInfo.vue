<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Radio, Datepicker, InputField, TimePicker } from '@/components/forms';
import { usePP006DetailStore } from '@/views/PP/stores/PP006/PP006Store';
import { useMenuStore } from '@/stores/menu';

const menuStore = useMenuStore();
const store = usePP006DetailStore();
const inviteOptions = [
  { label: 'เชิญชวน', value: true },
  { label: 'ไม่เชิญชวน', value: false },
] as Option[];
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="จัดทำหนังสือเชิญชวนผู้ประกอบการ" />
      <div class="px-4">
        <Radio v-model="store.detail.isInvite" :options="inviteOptions"
          :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required" />
      </div>
      <div class="px-4 mt-8 grid lg:grid-cols-2 gap-2 gap-y-8" v-if="store.detail.isInvite">
        <Datepicker label="วันที่เอกสาร" v-model="store.detail.documentDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
  <Card class="mb-4" v-if="store.detail.isInvite">
    <template #content>
      <TitleHeader label="รายละเอียดสำหรับผู้ยื่นข้อเสนอ" />
      <p>ผู้ยื่นข้อเสนอต้องยื่นข้อเสนอและเสนอราคา</p>
      <div class="px-4 mt-8 grid lg:grid-cols-2 gap-2 gap-y-8">
        <Datepicker label="ตั้งแต่วันที่" v-model="store.detail.submitProposalStartDate" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" :max-date="store.detail.submitProposalEndDate"/>
        <TimePicker label="เวลา" v-model="store.detail.startTime" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Datepicker label="ถึงวันที่" v-model="store.detail.submitProposalEndDate" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" :min-date="store.detail.submitProposalStartDate"/>
        <TimePicker label="เวลา" v-model="store.detail.endTime" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </div>
      <p>ผู้สนใจต้องการทราบรายละเอียดเพิ่มเติมเกี่ยวกับรายละเอียดของขอบเขตงานทั้งโครงการภายในวันที่</p>
      <div class="px-4 mt-8 grid lg:grid-cols-2 gap-2 gap-y-8">
        <Datepicker label="วันที่" v-model="store.detail.needToKnowWithinDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage" hide-details />
      </div>
      <p>โดยธนาคารจะชี้แจงรายละเอียดดังกล่าว</p>
      <div class="px-4 mt-8 grid lg:grid-cols-2 gap-2 gap-y-10">
        <Datepicker label="วันที่" v-model="store.detail.clarifyDetailViaDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage" hide-details />
        <InputField label="เบอร์โทรศัพท์" helperText="ตัวอย่าง 02-202-xxxx" class="lg:col-start-1" v-model="store.detail.phoneNumber"
          :disabled="!store.status.canEdit || !menuStore.hasManage" rules="required" />
      </div>
    </template>
  </Card>
</template>
