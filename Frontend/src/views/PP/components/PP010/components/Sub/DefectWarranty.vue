<script setup lang="ts">
import { EGroupCode } from '@/enums/shared';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { TRedeliveryBaseType } from '@/views/PP/enums/pp010';
import type { TContractDraftBody, TRedelivery, TRentalDurationInfo, TWarrantyInfo } from '@/views/PP/models/PP0010/ContractDraft';
import { HttpStatusCode } from 'axios';
import { onBeforeMount, ref, watch } from 'vue';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const onFetchDefectWarrantyOptionsAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.ContractDefectWarranty);

  if (status === HttpStatusCode.Ok) {
    defectWarrantyTypeOptions.value = data;
  }
};

const defectWarrantyTypeOptions = ref<Array<Option>>([]);

const hasWarrantyOptions = [
  { value: true, label: 'มี' },
  { value: false, label: 'ไม่มี' },
];

const body = defineModel<TContractDraftBody>("body", { required: true });

const initialWarranty = (init: boolean = true) => {
  if (!body.value.detail.warranty) return;

  if (init) {
    body.value.detail.warranty.warrantyPeriod = body.value.detail.warranty.warrantyPeriod || {} as TRentalDurationInfo;
    body.value.detail.warranty.fixingDeadlinePeriod = body.value.detail.warranty.fixingDeadlinePeriod || {} as TRentalDurationInfo;

    return;
  }

  body.value.detail.warranty = {
    hasWarranty: body.value.detail.warranty.hasWarranty,
  } as TWarrantyInfo;
};

const initialRedelivery = (init: boolean = true) => {
  if (!body.value.detail.redelivery) return;

  if (!init) {
    body.value.detail.redelivery = {
      type: TRedeliveryBaseType.Redelivery,
      redeliveryDeadline: undefined,
      correctionDueTypeCode: undefined,
    } as TRedelivery;

    return;
  }

  body.value.detail.redelivery = {
    type: TRedeliveryBaseType.Redelivery,
  } as TRedelivery;
};

watch(() => body.value.detail.warranty?.hasWarranty, (newVal, oldVal) => {
  if (!body.value.detail.warranty) {
    return;
  }

  if (newVal === oldVal) return;

  if (newVal) {
    initialWarranty();

    return;
  }

  initialWarranty(false);
}, { immediate: true });

onBeforeMount(async () => {
  await onFetchDefectWarrantyOptionsAsync();
});

const onChangeDefectWarrantyTypeCode = (e?: string) => {
  if (!e) return;

  if (e == 'CDW001') {
    initialWarranty();
    initialRedelivery(false);
    return;
  }

  initialWarranty(false);
  initialRedelivery();
}
</script>

<template>
  <Card :pt="{ root: { 'data-section-id': 'defect-warranty', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />
      <Radio :disabled="props.disable" :options="hasWarrantyOptions" v-model="body.detail.warranty.hasWarranty"
        v-if="body.detail.warranty" />

      <div v-if="body.detail.warranty && body.detail.warranty.hasWarranty" class="flex flex-col gap-4 mt-5">
        <Select class="w-72" label="ประเภทสัญญา" v-model="body.detail.defectWarrantyTypeCode" :options="defectWarrantyTypeOptions"
          rules="required" @update:model-value="(e) => onChangeDefectWarrantyTypeCode(e)" :disabled="props.disable" />

        <section id="section-a" v-if="body.detail.defectWarrantyTypeCode == 'CDW001'">
          <div class="flex flex-col gap-2">
            <div>
              <span class="text-xl">ระยะเวลาการรับประกันความชำรุดบกพร่องหรือขัดข้อง</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" label="ปี" input-class="text-end"
                  v-model="body.detail.warranty.warrantyPeriod.year" />
                <InputNumber class="w-48" :disabled="props.disable" label="เดือน" input-class="text-end"
                  v-model="body.detail.warranty.warrantyPeriod.month" />
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.warrantyPeriod.day" />
              </div>
            </div>

            <div>
              <span class="text-xl">ระยะเวลาให้แก้ไข ภายในกำหนด</span>
              <div class="flex gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" label="ปี" input-class="text-end"
                  v-model="body.detail.warranty.fixingDeadlinePeriod.year" />
                <InputNumber class="w-48" :disabled="props.disable" label="เดือน" input-class="text-end"
                  v-model="body.detail.warranty.fixingDeadlinePeriod.month" />
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.warranty.fixingDeadlinePeriod.day" />
              </div>
            </div>
          </div>
        </section>

        <section id="section-b" v-if="body.detail.redelivery && body.detail.defectWarrantyTypeCode == 'CDW002'">
          <div class="flex flex-col gap-6">
            <div>
              <span class="text-xl">หากชำรุดบกพร่อง ต้องซ่อมแซมหรือติดตั้งใหม่ ภายในกำหนด</span>
              <div class="flex items-end gap-4 mt-4">
                <InputNumber class="w-48" :disabled="props.disable" label="วัน" input-class="text-end"
                  v-model="body.detail.redelivery.redeliveryDeadline" rules="required" hide-details />
                <span class="text-xl whitespace-nowrap mb-2">นับถัดจากวันที่ได้รับแจ้ง</span>
              </div>
            </div>
          </div>
        </section>
      </div>

    </template>
  </Card>
</template>