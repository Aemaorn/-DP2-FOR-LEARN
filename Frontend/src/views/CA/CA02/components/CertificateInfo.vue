<script setup lang="ts">
import type { CA02Body } from '@/models/CA/ca02';
import { Card } from 'primevue';
import { InputField, Datepicker, InputArea } from '@/components/forms';
type Props = {
  disabled?: boolean;
}

const value = defineModel<CA02Body>({
  required: true,
});

const { disabled } = defineProps<Props>();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <div class="grid lg:grid-cols-3 gap-2 gap-y-8 mt-8">
        <InputField label="เลขที่ใบรับรองผลงาน" v-model="value.certificateNo" disabled />
        <Datepicker label="วันที่รับเรื่อง" v-model="value.receiveDate" :disabled rules="required" />
        <InputField class="lg:col-start-1" label="เลขที่หนังสือของบริษัทคู่สัญญา" v-model="value.sbsDocumentNo"
          :disabled />
        <Datepicker label="ฉบับลงวันที่" v-model="value.documentDate" :disabled />
        <InputArea class="col-span-3" label="เหตุผลที่ขอ" v-model="value.requestReason" rules="required" :disabled />
      </div>
    </template>
  </Card>
</template>