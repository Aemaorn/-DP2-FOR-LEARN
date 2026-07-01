<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Radio } from '@/components/forms';
import { Card } from 'primevue';
import PreProcurementConstants from '@/constants/preProcurement';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { ConsiderOverPriceType } from '@/enums/preProcurement';
import { storeToRefs } from 'pinia';
import { useMenuStore } from '@/stores/menu';

type Props = {
  title: string;
}

const { ConsiderOverPriceTypeOptions } = PreProcurementConstants;
const pp004Store = usePP004Store();
const menuStore = useMenuStore();
const { body } = storeToRefs(pp004Store);

const props = defineProps<Props>();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="props.title" />
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-2 px-4 mt-2">
        <small class="mb-4">ราคากลางของพัสดุที่จะซื้อหรือจ้างเช่า หรือ
          ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา
          กรณีวงเงิน ไม่เกิน 100,000 บาท มีข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา ดังนี้
        </small>
      </div>
      <div class="px-4 mt-2">
        <Radio
          label="ราคากลางของพัสดุที่จะซื้อหรือจ้างเช่า หรือ ข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา กรณีวงเงิน เกิน 100,000 บาท มีข้อมูลประกอบการพิจารณาด้านความเหมาะสมของราคา ดังนี้"
          :options="ConsiderOverPriceTypeOptions" vertical v-model="body.requisition.priceReasonablenessInfo"
          :disabled="!menuStore.hasManage" />
      </div>

      <div class="mt-2"
        v-if="pp004Store.body.requisition.priceReasonablenessInfo === ConsiderOverPriceType.STANDARD_PRICE_BY_AGENCY">
        <InputNumber v-model="pp004Store.body.requisition.medianPriceAmount" :label="'จำนวนอ้างอิง'" :min="0"
          mode="decimal" :disabled="!menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>