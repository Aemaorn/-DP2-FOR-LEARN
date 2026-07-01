<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, Datepicker } from '@/components/forms';
import addressHelper from '@/helpers/address';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';
import { onMounted, watch } from 'vue';

const store = usePcmContractDraftStore();
const { getProvinceAsync, getDistrictAsync, getSubDistrictAsync, provinceOptions, districtOptions, subDistrictOptions } = addressHelper;

watch(
  () => store.body.detail.vendor.province,
  async (provinceCode) => {

    districtOptions.value = [];
    subDistrictOptions.value = [];

    if (provinceCode) {
      await getDistrictAsync(provinceCode);
    }
  }
);

watch(
  () => store.body.detail.vendor.district,
  async (districtCode) => {

    subDistrictOptions.value = [];

    if (districtCode) {
      await getSubDistrictAsync(districtCode);
    }
  }
);

onMounted(() => {
  getProvinceAsync();
});

</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลผู้ค้า" />
      <div class="grid lg:grid-cols-3 gap-2 mt-5">
        <InputField label="ชื่อผู้ค้า" v-model="store.body.detail.vendor.name" />
        <InputField label="เลขประจำตัวผู้เสียภาษี/เลขประจำตัวประชาชน"
          v-model="store.body.detail.vendor.taxpayerIdentificationNo" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="สถานที่ทะเบียนนิติบุคคล" v-model="store.body.detail.vendor.registrationPlace" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <InputField label="เลขที่" v-model="store.body.detail.vendor.address" />
        <InputField label="ถนน" v-model="store.body.detail.vendor.street" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <Select :options="provinceOptions" label="จังหวัด" v-model="store.body.detail.vendor.province" />
        <Select :options="districtOptions" label="อำเภอ/เขต" v-model="store.body.detail.vendor.district" />
        <Select :options="subDistrictOptions" label="ตำบล/แขวง" v-model="store.body.detail.vendor.subDistrict" />
      </div>
      <div class="grid lg:grid-cols-3 gap-2">
        <Datepicker label="วันที่เอกสารบันทึกข้อความแต่งตั้ง" v-model="store.body.detail.vendor.startDate" />
        <Datepicker label="วันที่เอกสารบันทึกข้อความแต่งตั้ง" v-model="store.body.detail.vendor.endDate" />
      </div>
    </template>
  </Card>
</template>