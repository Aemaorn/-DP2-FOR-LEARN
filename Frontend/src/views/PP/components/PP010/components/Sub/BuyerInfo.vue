<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, InputArea, Select } from '@/components/forms';
import { useAddress } from '@/helpers/address';
import type { TContractDraftBody, TLocationInfo } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { onMounted, watch } from 'vue';

type Props = {
  label?: string;
  disable?: boolean;
}

const props = defineProps<Props>();

const store = useContractDraftStore();

const body = defineModel<TContractDraftBody>("body", { required: true });

watch(() => body.value.detail.buyer, () => {
  if (!body.value.detail.buyer.province) {
    body.value.detail.buyer.province = {} as TLocationInfo;
  }
  if (!body.value.detail.buyer.district) {
    body.value.detail.buyer.district = {} as TLocationInfo;
  }
  if (!body.value.detail.buyer.subDistrict) {
    body.value.detail.buyer.subDistrict = {} as TLocationInfo;
  }
});

const { getProvinceAsync, getDistrictAsync, getSubDistrictAsync, provinceOptions, districtOptions, subDistrictOptions } = useAddress();

onMounted(() => {
  getProvinceAsync();
});

watch(
  () => body.value.detail.buyer.province.code,
  async (provinceCode) => {
    if (provinceCode) {
      await getDistrictAsync(provinceCode);
    } else {
      districtOptions.value = [];
      subDistrictOptions.value = [];
    }
  },
  { immediate: true }
);

watch(
  () => body.value.detail.buyer.district.code,
  async (districtCode) => {
    if (districtCode) {
      await getSubDistrictAsync(districtCode);
    } else {
      subDistrictOptions.value = [];
    }
  },
  { immediate: true }
);
</script>

<template>
  <Card v-if="body.detail.buyer">
    <template #content>
      <TitleHeader :label="props.label ?? 'ข้อมูลผู้ซื้อ'" />
      <div class="grid lg:grid-cols-4 mt-10">
        <InputField label="ชื่อ-ที่อยู่" rules="required" v-model="body.detail.buyer.name" disabled />
      </div>
      <InputArea :disabled="props.disable" label="สัญญาฉบับนี้ทำขึ้น ณ" rules="required"
        v-model="body.detail.buyer.address" class="mt-8" />
      <div class="grid lg:grid-cols-3 gap-2 mt-8"
        v-if="body.detail.buyer.province && body.detail.buyer.district && body.detail.buyer.subDistrict">
        <Select :disabled="props.disable" :options="provinceOptions" label="จังหวัด"
          v-model="body.detail.buyer.province.code"
          @on-select="(e: any) => store.onSelectLocationInfo(e, provinceOptions, name => body.detail.buyer.province.name = name)"
          rules="required" />
        <Select :disabled="props.disable" :options="districtOptions" label="อำเภอ/เขต"
          v-model="body.detail.buyer.district.code"
          @on-select="(e: any) => store.onSelectLocationInfo(e, districtOptions, name => body.detail.buyer.district.name = name)"
          rules="required" />
        <Select :disabled="props.disable" :options="subDistrictOptions" label="ตำบล/แขวง"
          v-model="body.detail.buyer.subDistrict.code"
          @on-select="(e: any) => store.onSelectLocationInfo(e, subDistrictOptions, name => body.detail.buyer.subDistrict.name = name)"
          rules="required" />
      </div>
    </template>
  </Card>
</template>