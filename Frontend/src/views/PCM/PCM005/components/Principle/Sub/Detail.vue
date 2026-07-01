<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  Select,
  Datepicker,
  InputNumber,
  InputArea,
} from '@/components/forms';
import { usePcm005PrincipleStore } from '@/stores/PCM/PCM005/principle';
import { onMounted, watch } from 'vue';
import { useMenuStore } from '@/stores/menu';
import addressHelper from '@/helpers/address';
import { FindDiffDate, FindDiffMonth } from '@/helpers/dateTime';

const menuStore = useMenuStore();
const store = usePcm005PrincipleStore();

const { getProvinceAsync, getDistrictAsync, getSubDistrictAsync, provinceOptions, districtOptions, subDistrictOptions } = addressHelper;

onMounted(() => {
  getProvinceAsync();
});

watch(
  () => store.body.provinceCode,
  async (provinceCode) => {

    districtOptions.value = [];
    subDistrictOptions.value = [];

    if (provinceCode) {
      await getDistrictAsync(provinceCode);
    }
  }
);

watch(
  () => store.body.districtCode,
  async (districtCode) => {

    subDistrictOptions.value = [];

    if (districtCode) {
      await getSubDistrictAsync(districtCode);
    }
  }
);

const calculateRentalDuration = (): void => {
  store.body.rentalDurationYear = undefined;
  store.body.rentalDurationMonth = undefined;
  store.body.rentalDurationDay = undefined;

  // Calculate only if both dates are available
  if (store.body.rentalStartDate && store.body.rentalEndDate) {
    const { years, months, days } = FindDiffDate(
      store.body.rentalStartDate,
      store.body.rentalEndDate
    );

    store.body.rentalDurationYear = years;
    store.body.rentalDurationMonth = months;
    store.body.rentalDurationDay = days;
  }
};

const onChangeStartDate = (date?: Date): void => {
  if (!date) {
    store.body.rentalEndDate = undefined;
    store.body.rentalDurationYear = undefined;
    store.body.rentalDurationMonth = undefined;
    store.body.rentalDurationDay = undefined;

    return;
  }

  calculateRentalDuration();
};

const updateProvince = (proviceCode: string) => {
  store.body.provinceName = provinceOptions.value.find(
    (p) => p.value === proviceCode
  )?.label ?? "";

  store.body.districtCode = undefined;
  store.body.subDistrictCode = undefined;
}

const updateDistrict = (districtCode: string) => {
  store.body.districtName = districtOptions.value.find(
    (d) => d.value === districtCode
  )?.label ?? "";

  store.body.subDistrictCode = undefined;
}

const updateSubDistrict = (subDistrictCode: string) => {
  store.body.subDistrictName = subDistrictOptions.value.find(
    (s) => s.value === subDistrictCode
  )?.label ?? "";
}

const onChangeEndDate = (date?: Date): void => {
  store.body.expectedContractDate = undefined;

  if (date) {
    store.body.expectedContractDate = new Date(date);
    calculateRentalDuration();
    onChangeMaxMonthlyRent(store.body.maxMonthlyRent);

    return;
  }

  store.body.rentalDurationYear = undefined;
  store.body.rentalDurationMonth = undefined;
  store.body.rentalDurationDay = undefined;
};

const getDaysInMonth = (date: Date) => {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0).getDate();
};

const addMonths = (date: Date, months: number) => {
  const d = new Date(date);
  d.setMonth(d.getMonth() + months);
  return d;
};

const diffDaysInclusive = (start: Date, end: Date) => {
  const msPerDay = 1000 * 60 * 60 * 24;
  return Math.floor((end.getTime() - start.getTime()) / msPerDay) + 1;
};

const onChangeMaxMonthlyRent = (val?: number) => {
  store.body.totalRentalAmount = undefined;

  if (!val || !store.body.rentalStartDate || !store.body.rentalEndDate) {
    return;
  }

  const startDate = new Date(store.body.rentalStartDate);
  const endDate = new Date(store.body.rentalEndDate);

  const fullMonths = FindDiffMonth(startDate, endDate);
  let total = 0;

  if (fullMonths > 0) {
    total += val * fullMonths;
  }

  const afterFullMonthDate = addMonths(startDate, fullMonths);

  if (afterFullMonthDate <= endDate) {
    const remainDays = diffDaysInclusive(afterFullMonthDate, endDate);
    const daysInEndMonth = getDaysInMonth(endDate);
    const rentPerDay = val / daysInEndMonth;

    total += rentPerDay * remainDays;
  }

  store.body.totalRentalAmount = total;
};

</script>
<template>
  <Card>
    <template #content>
      <TitleHeader label="ข้อมูลเช่าพื้นที่/อาคาร/ที่จอดรถ/ป้าย" />
      <div class="grid lg:grid-cols-3 gap-2 gap-y-8 mt-10">
        <InputField label="ที่ทำการสาขา" v-model="store.body.branchLocation" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <InputField label="เบอร์โทร" v-model="store.body.phoneNumber" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Datepicker label="วันที่เอกสาร" v-model="store.body.documentDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <Select class="lg:col-start-1" label="แบบขออนุมัติเช่า" :options="store.docuementTemplateDDL"
          v-model="store.body.rentTypeCode" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <Datepicker class="lg:col-start-1" label="ตั้งแต่วันที่" v-model="store.body.rentalStartDate" rules="required"
          @on-selected="onChangeStartDate" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Datepicker label="ถึง" v-model="store.body.rentalEndDate" :minDate="store.body.rentalStartDate"
          @on-selected="onChangeEndDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage || !store.body.rentalStartDate" rules="required" />

        <InputNumber class="lg:col-start-1" label="ระยะเวลาเช่า (ปี)" v-model="store.body.rentalDurationYear" grouping
          rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <InputNumber label="เดือน" v-model="store.body.rentalDurationMonth" grouping rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <InputNumber label="วัน" v-model="store.body.rentalDurationDay" grouping rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <InputNumber class="lg:col-start-1" label="อัตราค่าเช่าเดือนละไม่เกิน" v-model="store.body.maxMonthlyRent"
          grouping :min-fraction-digits="2" :max-fraction-digits="3" rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage"
          @on-change="(e) => onChangeMaxMonthlyRent(e)" />
        <InputNumber label="รวมเป็นจำนวนเงิน" v-model="store.body.totalRentalAmount" grouping :min-fraction-digits="2"
          rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Datepicker label="สัญญาครบกำหนดในวันที่" v-model="store.body.expectedContractDate"
          :min-date="store.body.rentalEndDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage || !store.body.rentalEndDate" rules="required" />

        <InputArea class="lg:col-span-3" label="รายละเอียดสถานที่เช่า" v-model="store.body.rentalLocationDetails"
          rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <Select class="lg:col-start-1" label="จังหวัด" :options="provinceOptions" v-model="store.body.provinceCode"
          rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage"
          @update:model-value="(e) => updateProvince(e as string)" />
        <Select label="อำเภอ" :options="districtOptions" v-model="store.body.districtCode" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage"
          @update:model-value="(e) => updateDistrict(e as string)" />
        <Select label="ตำบล" :options="subDistrictOptions" v-model="store.body.subDistrictCode" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage"
          @update:model-value="(e) => updateSubDistrict(e as string)" />
      </div>
    </template>
  </Card>
</template>
