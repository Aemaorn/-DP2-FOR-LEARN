<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, Datepicker, Radio } from '@/components/forms';
import { VendorConstants } from '@/constants';
import { useAddress } from '@/helpers/address';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { onMounted, watch } from 'vue';

type Props = {
  disable?: boolean;
}

const props = defineProps<Props>();

const body = defineModel<TContractDraftBody>("body", { required: true });

const { getProvinceAsync, getDistrictAsync, getSubDistrictAsync, provinceOptions, districtOptions, subDistrictOptions } = useAddress();

watch(
  () => body.value.detail.vendor.province,
  async (provinceCode) => {

    districtOptions.value = [];
    subDistrictOptions.value = [];

    if (provinceCode) {
      await getDistrictAsync(provinceCode);
    }
  },
  { immediate: true }
);

watch(
  () => body.value.detail.vendor.district,
  async (districtCode) => {

    subDistrictOptions.value = [];

    if (districtCode) {
      await getSubDistrictAsync(districtCode);
    }
  },
  { immediate: true }
);

onMounted(() => {
  getProvinceAsync();
});
</script>

<template>
  <Card :pt="{ root: { 'data-section-id': 'seller-info', 'data-section-label': 'ข้อมูลผู้ค้า' } }">
    <template #content>
      <TitleHeader label="ข้อมูลผู้ค้า" />
      <Radio disabled class="col-span-2" v-model="body.detail.vendor.type"
        :options="VendorConstants.vendorTypeOptions" />
      <div class="grid lg:grid-cols-3 gap-2 mt-6">
        <InputField disabled label="ชื่อผู้ค้า" v-model="body.detail.vendor.name" />
        <InputField disabled label="เลขประจำตัวผู้เสียภาษี/เลขประจำตัวประชาชน"
          v-model="body.detail.vendor.taxpayerIdentificationNo" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2 mt-8">
        <InputField :disabled="props.disable" label="สถานที่ทะเบียนนิติบุคคล"
          v-model="body.detail.vendor.vendorRegistrationPlace" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2 mt-8">
        <InputField :disabled="props.disable" label="เลขที่" v-model="body.detail.vendor.address" />
        <InputField :disabled="props.disable" label="ถนน" v-model="body.detail.vendor.street" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2 mt-8">
        <Select :disabled="props.disable" :options="provinceOptions" label="จังหวัด"
          v-model="body.detail.vendor.province" />
        <Select :disabled="props.disable" :options="districtOptions" label="อำเภอ/เขต"
          v-model="body.detail.vendor.district" />
        <Select :disabled="props.disable" :options="subDistrictOptions" label="ตำบล/แขวง"
          v-model="body.detail.vendor.subDistrict" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2 mt-8">
        <Datepicker :disabled="props.disable" label="วันที่ หนังสือรับรองสำนักทะเบียนหุ้นส่วนบริษัท"
          v-model="body.detail.vendor.startDate" />
        <Datepicker :disabled="props.disable" label="วันที่หนังสือมอบอำนาจ" v-model="body.detail.vendor.endDate" />
      </div>
      <div class="text-sm">
        <span class="text-red-500">*</span><span>หมายเหตุ จะแสดงหลังจากยืนยันวันที่ลงนามแล้ว โดยระบบจะคำนวนจาก
          เพื่อหาวันที่สิ้นสุดของสัญญา</span>
      </div>
    </template>
  </Card>
</template>