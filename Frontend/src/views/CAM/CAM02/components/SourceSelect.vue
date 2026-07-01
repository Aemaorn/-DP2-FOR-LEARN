<script setup lang="ts">
import { computed, watch } from 'vue';
import { InputArea, Radio } from '@/components/forms';
import { useCam02DetailStore } from '@/stores/CAM/CAM02/cam02Store';
import { committeeGroupTypeMappingName } from './CommitteeTypeMappingName';

const emit = defineEmits<{ (e: 'committeeTypeChanged'): void }>();

const store = useCam02DetailStore();

const committeeTypeOptions = computed(() =>
  store.committeeGroupTypeList.map(item => ({
    label: committeeGroupTypeMappingName(item.committeeGroupType),
    value: item.committeeGroupType,
  }))
);

watch(
  () => store.procurementDetail.committeeType,
  async (val) => {
    if (val) {
      const getObj = store.committeeGroupTypeList.find(c => c.committeeGroupType === val);
      await store.onGetCommitteeBySourceTypeAsync(getObj!.sourceId, getObj!.sourceType, getObj!.committeeGroupType);
      emit('committeeTypeChanged');
    }
  }
);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลขอแก้ไขคณะกรรมการ"></TitleHeader>
      <div class="mt-10 px-4">
        <Radio v-model="store.procurementDetail.committeeType" name="sourceType" rules="required"
          :options="committeeTypeOptions" :disabled="!store.isCanEdit" vertical />
        <div class="mt-4 grid grid-cols-1 lg:grid-cols-2 gap-2 mt-8">
          <Datepicker label="วันที่เอกสาร" v-model="store.procurementDetail.documentDate"
            :disabled="!store.isCanEdit" />
        </div>
          <div class="mt-4 grid grid-cols-1 lg:grid-cols-1 gap-2 mt-8">
          <InputArea label="ความเห็นเพิ่มเติม" v-model="store.procurementDetail.remark" :disabled="!store.isCanEdit"/>
        </div>
        <Radio v-model="store.procurementDetail.isJorPorComment" name="isJorPorComment" rules="required"
          label="เจ้าหน้าที่พัสดุให้ความเห็น" :options="[{ label: 'มี', value: true }, { label: 'ไม่มี', value: false }]"
          :disabled="!store.isCanEdit" />
      </div>
    </template>
  </Card>
</template>
