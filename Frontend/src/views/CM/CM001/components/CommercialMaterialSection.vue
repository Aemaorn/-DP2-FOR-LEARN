<script setup lang="ts">
import { PlanDepartmentCode } from '@/enums/plan';
import { Card } from 'primevue';
import { computed } from 'vue';
import { Field } from 'vee-validate';

const props = defineProps<{
  modelValue: boolean | undefined;
  departmentId?: string;
  supplyMethodCode?: string;
  disabled?: boolean;
  isRequired?: boolean;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: boolean | undefined];
}>();

const options = computed(() => {
  const data = [
    { label: '', value: true },
    { label: 'ไม่มี', value: false },
  ];

  switch (props.departmentId) {
    case PlanDepartmentCode.CCD:
    case PlanDepartmentCode.MCD:
      data[0].label = [
        '1. การซื้อหรือจ้างที่เกี่ยวข้องกับการผลิตสื่อ การโฆษณา การประชาสัมพันธ์ผลิตภัณฑ์ ภาพลักษณ์และบริการของธนาคาร',
        '2. การจัดซื้อหรือจ้างที่เกี่ยวข้องกับเครื่องแต่งกาย เครื่องประดับ หรือพัสดุอื่นๆ ที่เกี่ยวข้องกับพิธีอุปถัมภ์ สำหรับแบรนด์แอมบาสเดอร์ หรือผู้แทนธนาคารในงานพิธีการต่างๆ',
        '3. การจ้างบริการแต่งหน้าทำผม สำหรับแบรนด์แอมบาสเดอร์ หรือผู้แทนธนาคารในงานพิธีการต่างๆ',
      ].join('<br/>');
      break;
    case PlanDepartmentCode.OABAD:
      data[0].label = [
        '1. งานจ้างที่เกี่ยวซ่อมแซมยานพาหนะ เครื่องจักร เครื่องใช้สำนักงาน ครุภัณฑ์ และอาคารสถานที่ จัดซื้อจัดหาอะไหล่ เพื่อการซ่อมแซมหรือบำรุงรักษา หรือทำความสะอาดเครื่องจักร เครื่องใช้สำนักงาน ครุภัณฑ์ ยานพาหนะ อาคารสถานที่ รวมถึงอุปกรณ์ต่างๆ ที่จำเป็นต้องใช้',
        '2. การจัดซื้อจัดจ้าง หรือซ่อมอุปกรณ์ต่างๆ ที่เกี่ยวข้องกับความปลอดภัย',
        '3. การจัดซื้อต้นไม้ และอุปกรณ์ในการปรับแต่งภูมิทัศน์',
        '4. การจัดซื้อจัดจ้างเกี่ยวกับการจัดสถานที่ในงานของธนาคาร',
        '5. เช่าที่จอดรถ',
        '6. การจัดซื้อจัดจ้างให้รวมถึงการเช่าสำหรับทรัพย์ด้วย',
      ].join('<br/>');
      break;
    case PlanDepartmentCode.NMD:
      data[0].label = [
        '1. การจ้างทำงานหรือจ้างบริการบำรุงรักษา หรือซ่อมแซมทรัพย์สินที่อยู่ในความรับผิดชอบ หรือทรัพย์สินของผู้อื่น ที่ได้รับความเสียหายอันเกิดจากทรัพย์สินที่อยู่ในความรับผิดชอบให้อยู่ในสภาพสามารถใช้งานได้เป็นปกติ',
        '2. การจ้างทำสื่อประชาสัมพันธ์ กิจกรรมทางการตลาดและการขาย',
      ].join('<br/>');
      break;
    case PlanDepartmentCode.RDMD1:
    case PlanDepartmentCode.RDMD2:
      data[0].label = [
        '1. การจ้างทำงานหรือจ้างบริการบำรุงรักษาหรือซ่อมแซมทรัพย์สินที่อยู่ในความรับผิดชอบหรือทรัพย์สินของผู้อื่นที่ได้รับความเสียหายอันเกิดจากทรัพย์สินที่อยู่ในความรับผิดชอบให้อยู่ในสภาพใช้งานได้เป็นปกติ',
        '2. การจ้างทำสื่อประชาสัมพันธ์ กิจกรรมทางการตลาดและการขาย',
      ].join('<br/>');
      break;
  }

  return data;
});
</script>

<template>
  <Card class="mt-4">
    <template #content>
      <Field :model-value="props.modelValue" name="isCommercialMaterial"
        :rules="props.isRequired ? ((val: unknown) => val !== null && val !== undefined || 'กรุณาเลือกข้อมูล') : ''"
        v-slot="{ errorMessage }" as="div">
        <p>การจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง<span v-if="props.isRequired" class="text-red-500">*</span></p>
        <div v-for="option in options" :key="option.label" class="flex gap-2">
          <RadioButton :model-value="props.modelValue" :inputId="option.label" name="isCommercialMaterial"
            :value="option.value" :disabled="props.disabled" class="mt-1" required
            @update:model-value="emit('update:modelValue', $event as boolean)" />
          <label :for="option.label" v-html="option.label"></label>
        </div>
        <small class="pl-2 text-red-500!" v-if="errorMessage">{{ errorMessage }}</small>
      </Field>
    </template>
  </Card>
</template>
