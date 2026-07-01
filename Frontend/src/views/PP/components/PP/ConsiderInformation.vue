<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Datepicker, InputNumber, InputArea, Radio, InputField } from '@/components/forms';
import type { JorPor04Requisition } from '../../models/PP004/pp004Model';
import PreProcurementConstants from '@/constants/preProcurement';
import { useMenuStore } from '@/stores/menu';

type Props = {
  title: string;
  isOverPrice: boolean;
  isDisabled?: boolean;
  number?: string;
}

const props = defineProps<Props>();
const value = defineModel<JorPor04Requisition>({
  required: true,
});
const documentDate = defineModel<Date | undefined>('documentDate');
const { ConsiderOverPriceTypeOptions } = PreProcurementConstants;
const menuStore = useMenuStore();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.title" />
      <div class="mt-10 grid grid-cols-1 lg:grid-cols-2 gap-2 gap-y-8">
        <InputField label="เลขที่" :model-value="props.number"
            :disabled="true" />
        <Datepicker label="วันที่เอกสาร" v-model="documentDate"
          :disabled="props.isDisabled || !menuStore.hasManage" />
      </div>
      <div class="mt-2 px-4" v-if="props.isOverPrice">
        <Radio
          label="ราคากลางของพัสดุที่จะซื้อหรือจ้างเช่า หรือ ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา กรณีวงเงิน เกิน 100,000 บาท มีข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา ดังนี้"
          :options="ConsiderOverPriceTypeOptions" vertical v-model="value.priceReasonablenessInfo"
          :disabled="props.isDisabled || !menuStore.hasManage" rules="required" />
        <div class="mt-4 grid grid-cols-1 lg:grid-cols-2 gap-2 gap-y-8">
          <InputNumber label="ราคากลางอ้างอิง" v-model="value.medianPriceAmount" rules="required"
            :min-fraction-digits="2" grouping :disabled="props.isDisabled || !menuStore.hasManage" />
          <InputField label="เลขที่เอกสาร PR" v-model="value.prNumber"
            :disabled="props.isDisabled || !menuStore.hasManage" rules="required" />
          <InputField label="เบอร์โทรศัพท์ติดต่อ" v-model="value.telephone"
            :disabled="props.isDisabled || !menuStore.hasManage" rules="required" />
        </div>
      </div>
      <div class="mt-2 px-4" v-else>
        <small class="mb-4">ราคากลางของพัสดุที่จะซื้อหรือจ้างเช่า หรือ
          ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา
          กรณีวงเงิน ไม่เกิน 100,000 บาท มีข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา ดังนี้
        </small>
        <div class="mt-8 grid grid-cols-1 lg:grid-cols-2 gap-5 gap-y-8">
          <InputArea class="lg:col-span-2 col-span-auto" label="รายละเอียด" v-model="value.description" rules="required"
            :disabled="props.isDisabled || !menuStore.hasManage" />
          <InputNumber label="ราคากลางอ้างอิง" v-model="value.medianPriceAmount" rules="required" :min-fraction-digits="2"
            grouping :disabled="props.isDisabled || !menuStore.hasManage" />
          <InputField label="เลขที่เอกสาร PR" v-model="value.prNumber"
            :disabled="props.isDisabled || !menuStore.hasManage" rules="required" />
          <InputField label="เบอร์โทรศัพท์ติดต่อ" v-model="value.telephone"
            :disabled="props.isDisabled || !menuStore.hasManage" rules="required" />
        </div>
      </div>
    </template>
  </Card>
</template>