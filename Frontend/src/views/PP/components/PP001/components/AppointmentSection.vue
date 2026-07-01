<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, InputArea, Datepicker } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';
import { usePP001DetailStore } from '@/views/PP/stores/PP001/pp001Store'

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const appointmentStore = usePP001DetailStore();
const menuStore = useMenuStore();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader class="my-8" label="ข้อมูลเอกสารบันทึกข้อความแต่งตั้ง"></TitleHeader>
      <div class="grid lg:grid-cols-3 gap-2 gap-y-8">
        <InputField label="เลขที่แต่งตั้ง" v-model="appointmentStore.pp001Detail.appoint.appointNumber" :disabled="true" />
        <Datepicker label="วันที่เอกสารบันทึกข้อความแต่งตั้ง"
          v-model="appointmentStore.pp001Detail.appoint.memorandumDate" rules="required"
          :disabled="!appointmentStore.isEdit || !menuStore.hasManage || props.readonly" />
        <InputField label="เลขที่ระบบสารบรรณ (ถ้ามี)" v-model="appointmentStore.pp001Detail.appoint.memorandumNumber"
          :disabled="!appointmentStore.isEdit || !menuStore.hasManage || props.readonly" />

        <InputField label="เบอร์โทร" v-model="appointmentStore.pp001Detail.appoint.telephone" rules="required"
          :disabled="!appointmentStore.isEdit || !menuStore.hasManage || props.readonly" />

        <InputArea label="เหตุผลและความจำเป็นที่จะซื้อหรือจ้างเช่า" class="lg:col-span-3"
          v-model="appointmentStore.pp001Detail.appoint.reason" rules="required"
          :disabled="!appointmentStore.isEdit || !menuStore.hasManage || props.readonly" />
      </div>
      <div v-if="appointmentStore.pp001Detail.appoint.isChange" class="md:grid grid-cols-1 gap-4">
        <InputArea label="เหตุผลการขอเปลี่ยนแปลง" v-model="appointmentStore.pp001Detail.appoint.changeReason"
          :disabled="!appointmentStore.isEdit || !menuStore.hasManage || props.readonly" rules="required" />
      </div>
      <div v-if="appointmentStore.pp001Detail.appoint.isCancel" class="md:grid grid-cols-1 gap-4">
        <InputArea label="เหตุผลการขอยกเลิก" v-model="appointmentStore.pp001Detail.appoint.cancelReason"
          :disabled="!appointmentStore.isEdit || !menuStore.hasManage || props.readonly" rules="required" />
      </div>

    </template>
  </Card>
</template>
