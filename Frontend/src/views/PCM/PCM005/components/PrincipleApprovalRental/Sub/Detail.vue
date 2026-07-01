<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  Select,
  Datepicker,
  InputNumber,
  InputArea,
} from '@/components/forms';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { onMounted, watch } from 'vue';
import { useMenuStore } from '@/stores/menu';
import addressHelper from '@/helpers/address';
import { FindDiffDate, FindDiffMonth } from '@/helpers/dateTime';

const menuStore = useMenuStore();

const store = usePcm005PrinApproveRentStore();

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
  store.body.rentalDurationYear = 0;
  store.body.rentalDurationMonth = 0;
  store.body.rentalDurationDay = 0;

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
    store.body.rentalEndDate = undefined as unknown as Date;
    store.body.rentalDurationYear = 0;
    store.body.rentalDurationMonth = 0;
    store.body.rentalDurationDay = 0;

    return;
  }

  calculateRentalDuration();
};

const onChangeEndDate = (date?: Date): void => {
  store.body.expectedContractDate = undefined as unknown as Date;

  if (date) {
    store.body.expectedContractDate = new Date(date);
    calculateRentalDuration();
    onChangeMaxMonthlyRent(store.body.maxMonthlyRent);

    return;
  }

  store.body.rentalDurationYear = 0;
  store.body.rentalDurationMonth = 0;
  store.body.rentalDurationDay = 0;
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
  store.body.totalRentalAmount = 0;

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
        <InputField label="ที่ทำการสาขา" v-model="store.body.branchLocation" disabled />
        <InputField label="เบอร์โทร" v-model="store.body.phoneNumber" rules="required" disabled />
        <Datepicker label="วันที่เอกสาร" v-model="store.body.documentDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <Select class="lg:col-start-1" label="แบบขออนุมัติเช่า"
          :options="[{ label: store.body.rentTypeName ?? '', value: store.body.rentTypeCode }]"
          v-model="store.body.rentTypeCode" disabled />

        <Datepicker class="lg:col-start-1" label="ตั้งแต่วันที่" v-model="store.body.rentalStartDate"
          @on-selected="onChangeStartDate" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Datepicker label="ถึง" v-model="store.body.rentalEndDate" :minDate="store.body.rentalStartDate"
          @on-selected="onChangeEndDate"
          :disabled="!store.status.canEdit || !menuStore.hasManage || !store.body.rentalStartDate" />

        <InputNumber class="lg:col-start-1" label="ระยะเวลาเช่า (ปี)" v-model="store.body.rentalDurationYear" grouping
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <InputNumber label="เดือน" v-model="store.body.rentalDurationMonth" grouping :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <InputNumber label="วัน" v-model="store.body.rentalDurationDay" grouping :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <InputNumber class="lg:col-start-1" label="อัตราค่าเช่าเดือนละไม่เกิน" v-model="store.body.maxMonthlyRent"
          grouping :min-fraction-digits="2" :max-fraction-digits="3" rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage"
          @on-change="(e) => onChangeMaxMonthlyRent(e)" />
        <InputNumber label="รวมเป็นจำนวนเงิน" v-model="store.body.totalRentalAmount" grouping :min-fraction-digits="2"
          rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Datepicker label="สัญญาครบกำหนดในวันที่" v-model="store.body.expectedContractDate" rules="required"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <InputArea class="lg:col-span-3" label="รายละเอียดสถานที่เช่า" v-model="store.body.rentalLocationDetails"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />

        <Select class="lg:col-start-1" label="จังหวัด" :options="provinceOptions" v-model="store.body.provinceCode"
          :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Select label="อำเภอ" :options="districtOptions" v-model="store.body.districtCode" :disabled="!store.status.canEdit || !menuStore.hasManage" />
        <Select label="ตำบล" :options="subDistrictOptions" v-model="store.body.subDistrictCode" :disabled="!store.status.canEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>
