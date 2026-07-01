<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import { storeToRefs } from 'pinia';
import { computed } from 'vue';

const store = useCm006DetailStore();
const { body } = storeToRefs(store);

const guaranteeTypeMap: Record<string, string> = {
  PBondType001: 'เงินสด (เงินโอน)',
  PBondType002: 'หนังสือค้ำประกันของธนาคาร',
  PBondType003: 'หนังสือค้ำประกันอิเล็กทรอนิกส์ของธนาคาร',
  PBondType004: 'เช็คหรือดราฟท์ที่ธนาคารเซ็นสั่งจ่าย',
  PBondType005: 'หนังสือค้ำประกันของบริษัทเงินทุน',
  PBondType006: 'พันธบัตรรัฐบาลไทย',
};

const guaranteeTypeName = computed(() => {
  return guaranteeTypeMap[body.value.guaranteeReturn.guaranteeTypeCode ?? 'PBondType001'] || 'เงินสด (เงินโอน)';
});
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader :label="`บันทึกผลการพิจารณา กรณีวางหลักประกันเป็น${guaranteeTypeName}`" />
      <div v-for="(item, index) in body.guaranteeReturn.requiredDocuments" :key="index" class="py-1">
        <Checkbox v-model="item.isSubmitted" :label="item.documentName"
          :disabled="!store.isCanAssign && !store.isConfirmAssigned" />
      </div>
    </template>
  </Card>
</template>