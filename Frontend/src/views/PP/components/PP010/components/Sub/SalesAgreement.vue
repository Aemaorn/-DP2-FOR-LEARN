<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import {
  InputField,
  InputArea,
  Select,
  InputNumber,
  Datepicker,
  Radio,
} from '@/components/forms';
import { ContractDraftTemplate } from '@/enums/contractDraftt';
import { useAddress } from '@/helpers/address';
import { ArrayHelper } from '@/helpers/array';
import type { TContractDraftBody, TLocationInfo, TPaymentBase, TPaymentTermDetail } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { computed, onMounted, watch } from 'vue';
import draggable from 'vuedraggable';

type Props = {
  label: string;
  disable?: boolean;
}

const props = defineProps<Props>();
const store = useContractDraftStore();
const body = defineModel<TContractDraftBody>("body", { required: true });
const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

const isExchangeOption = [
  { value: true, label: 'ผู้ให้แลกเปลี่ยน' },
  { value: false, label: 'ผู้รับแลกเปลี่ยน' },
];

const templateGroups = {
  salesGroup: [
    ContractDraftTemplate.CFormat002,
    ContractDraftTemplate.CMRentalTpl001,
    ContractDraftTemplate.CMRentalTpl002,
    ContractDraftTemplate.CMRentalTpl003,
    ContractDraftTemplate.CMRentalTpl004
  ],
  durationGroup: [ContractDraftTemplate.CFormat003],
  computerSalesGroup: [
    ContractDraftTemplate.CFormat004,
    ContractDraftTemplate.CFormat005
  ],
  exchangeGroup: [ContractDraftTemplate.CFormat012],
  constructionGroup: [
    ContractDraftTemplate.CFormat001,
    ContractDraftTemplate.CFormat014,
    ContractDraftTemplate.CFormat007
  ],
  serviceGroup: [
    ContractDraftTemplate.CFormat013,
    ContractDraftTemplate.CFormat009
  ],
  securityGroup: [ContractDraftTemplate.CFormat010],
  photocopierGroup: [ContractDraftTemplate.CFormat011],
  computerRentalGroup: [ContractDraftTemplate.CFormat006],
  carLeaseGroup: [ContractDraftTemplate.CFormat008],
  RentalGroup: [
    ContractDraftTemplate.CMRentalTpl001,
    ContractDraftTemplate.CMRentalTpl002,
    ContractDraftTemplate.CMRentalTpl003,
    ContractDraftTemplate.CMRentalTpl004
  ],
}

// Determine which field sets should be shown
const showFields = computed(() => ({
  itemDetail: true, // Used by almost all templates
  quantity: ![ContractDraftTemplate.CFormat006].includes(body.value.template),
  workplace: [
    ...templateGroups.constructionGroup,
    ...templateGroups.serviceGroup,
    ...templateGroups.securityGroup,
    ...templateGroups.photocopierGroup,
    ...templateGroups.carLeaseGroup,
    ...templateGroups.RentalGroup
  ].includes(body.value.template),
  province: [
    ...templateGroups.RentalGroup
  ].includes(body.value.template),
  duration: [
    ContractDraftTemplate.CFormat003,
    ContractDraftTemplate.CFormat010
  ].includes(body.value.template),
  dates: [
    ContractDraftTemplate.CFormat003,
    ContractDraftTemplate.CFormat010,
    ContractDraftTemplate.CFormat011,
    ContractDraftTemplate.CFormat008
  ].includes(body.value.template),
  serialNumber: [
    ...templateGroups.constructionGroup,
    ContractDraftTemplate.CFormat011
  ].includes(body.value.template),
  vehicleDetails: [
    ContractDraftTemplate.CFormat011,
    ContractDraftTemplate.CFormat008
  ].includes(body.value.template),
  engineCapacity: [ContractDraftTemplate.CFormat008].includes(body.value.template),
  exchange: [ContractDraftTemplate.CFormat012].includes(body.value.template),
  payment: [ContractDraftTemplate.CFormat011].includes(body.value.template),
  penalty: [ContractDraftTemplate.CFormat010].includes(body.value.template) && body.value.detail.penalty
}));

const addPaymentDetail = () => {
  if (!body.value.detail.payment) {
    body.value.detail.payment = {} as TPaymentBase;
  }

  if (!body.value.detail.payment.details) {
    body.value.detail.payment.details = [] as TPaymentTermDetail[];
  }

  body.value.detail.payment.details = addSequence(body.value.detail.payment.details, {} as TPaymentTermDetail)
};

const reSequenceData = () => {
  if (!body.value.detail.payment || !body.value.detail.payment.details) return;
  body.value.detail.payment.details = reSequence(body.value.detail.payment.details);
}

const deleteItem = (index: number): void => {
  if (!body.value.detail.payment || !body.value.detail.payment.details) return;
  body.value.detail.payment.details = deleteItemAndReSequence(
    body.value.detail.payment.details as TPaymentTermDetail[],
    index
  ) as TPaymentTermDetail[];
};

const onAmountChange = (item: TPaymentTermDetail) => {
  const budget = Number(body.value.budget);
  if (!budget) return;
  const amount = Number(item.amount || 0);
  item.installmentPercentage = Math.round(((amount / budget) * 100) * 100) / 100;
};

const onPercentageChange = (item: TPaymentTermDetail) => {
  const budget = Number(body.value.budget);
  if (!budget) return;
  const percent = Number(item.installmentPercentage || 0);
  item.amount = Math.round(((percent / 100) * budget) * 100) / 100;
};

const getTotalPercent = computed(() => {
  const details = body.value.detail?.payment?.details;
  if (!details || details.length === 0) return 0;

  const total = details.reduce((sum, d) => {
    return sum + Math.round((d.installmentPercentage || 0) * 100);
  }, 0);

  return total / 100;
});

watch(
  () => [
    body.value.detail?.agreement?.totalAmount,
    body.value.detail?.agreement?.vatRateTypeCode,
    body.value?.budget,
    body.value?.vatRateTypeCode
  ],
  () => {
    const agreement = body.value?.detail?.agreement;
    if (!agreement) return;

    if (!agreement.vatRateTypeCode) {
      agreement.vatRateTypeCode = body.value.vatRateTypeCode ?? '';
    }

    const amount =
      (agreement.totalAmount || undefined) ??
      body.value.budget ??
      0;

    agreement.totalAmount = amount;

    agreement.vatAmount =
      agreement.vatRateTypeCode === "VATType002"
        ? amount * 7 / 107
        : 0;
  },
  {
    immediate: true
  }
);

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
  <Card v-if="body.detail.agreement"
    :pt="{ root: { 'data-section-id': 'sales-agreement', 'data-section-label': props.label } }">
    <template #content>
      <TitleHeader :label="props.label" />

      <div class="flex flex-col gap-4 mt-6">
        <Radio v-if="showFields.exchange" :disabled="props.disable" :options="isExchangeOption"
          v-model="body.detail.agreement.isExchangeGiver" />

        <InputArea v-if="showFields.itemDetail" :disabled="props.disable" :label="body.template === ContractDraftTemplate.CFormat015 ? 'ลักษณะงานที่จ้างที่ปรึกษา' :
          body.template === ContractDraftTemplate.CFormat012 ? 'ให้แก่' :
            body.template === ContractDraftTemplate.CFormat011 ? 'ผู้ให้เช่าตกลงให้เช่าเครื่องถ่ายเอกสาร' :
              body.template === ContractDraftTemplate.CFormat006 ? 'ผู้ให้เช่าตกลงให้เช่าเครื่องคอมพิวเตอร์' :
                body.template === ContractDraftTemplate.CFormat008 ? 'รถยนต์ที่เช่า' :
                  body.template === ContractDraftTemplate.CFormat010 ? 'สถานที่รักษาความปลอดภัย' :
                    templateGroups.RentalGroup.includes(body.template) ? 'ผู้ให้เช่าตกลงให้เช่า' :
                      templateGroups.constructionGroup.includes(body.template) ? 'ผู้รับจ้างตกลงรับจ้างทำงาน' :
                        templateGroups.serviceGroup.includes(body.template) ? 'ผู้รับจ้างตกลงรับจ้างทำงาน' :
                          templateGroups.computerSalesGroup.includes(body.template) ? 'ผู้ขายตกลงขายและติดตั้งเครื่องคอมพิวเตอร์ฯ ซึ่งเป็นผลิตภัณฑ์ของ' :
                            'รายการสินค้าพัสดุที่ตกลงซื้อขาย'" v-model="body.detail.agreement.itemDetail"
          rules="required" class="mt-6" />

        <div v-if="showFields.vehicleDetails" class="grid lg:grid-cols-3 gap-2 mt-6">
          <InputField :disabled="props.disable" v-model="body.detail.agreement.brand" label="ยี่ห้อ" rules="required" />
          <InputField :disabled="props.disable" v-model="body.detail.agreement.model" label="รุ่น" rules="required" />
        </div>

        <div v-if="showFields.engineCapacity" class="grid lg:grid-cols-3 gap-2 mt-6">
          <InputNumber :disabled="props.disable" v-model="body.detail.agreement.engineCapacityCc"
            label="ขนาดเครื่องยนต์(ซีซี)" rules="required" />
        </div>

        <template v-if="showFields.workplace">
          <InputArea :disabled="props.disable"
            :label="templateGroups.RentalGroup.includes(body.template) ? 'รายละเอียดสถานที่เช่า' : 'สถานที่รับจ้างทำงาน'"
            v-model="body.detail.agreement.workplaceAddress" rules="required" class="mt-6" />
        </template>

        <div class="grid lg:grid-cols-3 gap-2 mt-6"
          v-if="body.detail.agreement.workplaceProvince && body.detail.agreement.workplaceDistrict && body.detail.agreement.workplaceSubDistrict">
          <Select :disabled="props.disable" :options="provinceOptions" label="จังหวัด"
            v-model="body.detail.agreement.workplaceProvince.code"
            @on-select="(code: any) => { const opt = provinceOptions.find((o: any) => o.value === code); if (opt) body.detail.agreement.workplaceProvince = { code, name: opt.label }; }" />
          <Select :disabled="props.disable" :options="districtOptions" label="อำเภอ/เขต"
            v-model="body.detail.agreement.workplaceDistrict.code"
            @on-select="(code: any) => { const opt = districtOptions.find((o: any) => o.value === code); if (opt) body.detail.agreement.workplaceDistrict = { code, name: opt.label }; }" />
          <Select :disabled="props.disable" :options="subDistrictOptions" label="ตำบล/แขวง"
            v-model="body.detail.agreement.workplaceSubDistrict.code"
            @on-select="(code: any) => { const opt = subDistrictOptions.find((o: any) => o.value === code); if (opt) body.detail.agreement.workplaceSubDistrict = { code, name: opt.label }; }" />
        </div>

        <div v-if="showFields.duration && body.detail.agreement.duration"
          class="grid lg:grid-cols-3 gap-2 mt-6 gap-y-6">
          <InputNumber :disabled="props.disable" label="กำหนดเวลาจะซื้อจะขาย(ปี)"
            v-model="body.detail.agreement.duration.year" rules="required" />
          <InputNumber :disabled="props.disable" label="(เดือน)" v-model="body.detail.agreement.duration.month"
            rules="required" />
          <InputNumber :disabled="props.disable" label="(วัน)" v-model="body.detail.agreement.duration.day"
            rules="required" />
        </div>

        <div v-if="showFields.dates" class="grid lg:grid-cols-3 gap-2 mt-6 gap-y-6">
          <Datepicker :disabled="props.disable" label="นับตั้งแต่วันที่" :max-date="body.detail.agreement.endDate"
            v-model="body.detail.agreement.startDate" rules="required" />
          <Datepicker :disabled="props.disable" label="จนถึง" :min-date="body.detail.agreement.startDate"
            v-model="body.detail.agreement.endDate" rules="required" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2 mt-6">
          <Select :disabled="props.disable" :options="store.dropdown.vatTypeOptions" label="อัตราภาษีมูลค่าเพิ่ม"
            v-model="body.detail.agreement.vatRateTypeCode" rules="required" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2 mt-6">
          <InputNumber disabled label="ภาษีมูลค่าเพิ่ม" grouping v-model="body.detail.agreement.vatAmount"
            :min-fraction-digits="2" />
        </div>
        <div class="grid lg:grid-cols-3 gap-2 mt-6">
          <InputNumber :disabled="props.disable" label="รวมราคาทั้งสิ้น (บาท)" grouping
            v-model="body.detail.agreement.totalAmount" :min-fraction-digits="2" />
        </div>

        <div v-if="showFields.penalty && body.detail.penalty">
          <div class="ml-2 text-xl">ค่าปรับ</div>
          <div class="grid lg:grid-cols-3 gap-2 mt-6">
            <Select :disabled="props.disable" label="ประเภทค่าปรับ" :options="store.dropdown.fineTypeOptions"
              v-model="body.detail.penalty.typeCode" rules="required" />
          </div>
          <div class="grid lg:grid-cols-3 gap-2 gap-y-6 mt-8">
            <InputNumber :disabled="props.disable" label="ค่าปรับอัตราร้อยละ" v-model="body.detail.penalty.rate"
              rules="required" :min-fraction-digits="2" :max-fraction-digits="3" grouping />
            <InputNumber :disabled="props.disable" label="จำนวนเงินค่าปรับ" v-model="body.detail.penalty.amount"
              rules="required" :min-fraction-digits="2" grouping />
            <Select :disabled="props.disable" label="ต่อ" :options="store.dropdown.unitMeaTypeOptions"
              v-model="body.detail.penalty.rateTypeCode" rules="required" />
            <Datepicker label="เริ่มลงมือทำงานในวันที่" v-model="body.startDate" rules="required"
              :disabled="props.disable" />
            <Datepicker label="เริ่มลงมือทำงานในวันที่" v-model="body.endDate" rules="required"
              :disabled="props.disable" />
          </div>
        </div>

        <div v-if="showFields.payment && body.detail.payment" class="mt-6">
          <TitleHeader label="โดยมีรายละเอียดการมอบดังนี้" :hidden-icon="true">
            <template #action>
              <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                @click="addPaymentDetail" v-if="!props.disable" />
            </template>
          </TitleHeader>

          <p v-if="body.detail.payment.details?.length == 0" class="text-center">ไม่พบข้อมูล</p>

          <!-- Summary bar -->
          <div v-if="body.detail.payment.details && body.detail.payment.details.length > 0"
            class="flex items-center gap-3 rounded-lg bg-gray-50 border border-gray-200 px-4 py-3 mt-4">
            <div class="flex-1">
              <div class="h-2.5 w-full rounded-full bg-gray-200 overflow-hidden">
                <div class="h-full rounded-full transition-all duration-300"
                  :class="getTotalPercent === 100 ? 'bg-green-500' : getTotalPercent > 100 ? 'bg-red-500' : 'bg-amber-500'"
                  :style="{ width: Math.min(getTotalPercent, 100) + '%' }" />
              </div>
            </div>
            <span class="text-sm font-semibold whitespace-nowrap"
              :class="getTotalPercent === 100 ? 'text-green-600' : getTotalPercent > 100 ? 'text-red-500' : 'text-amber-600'">
              {{ getTotalPercent.toFixed(2) }}%
            </span>
            <span v-if="getTotalPercent === 100"
              class="material-symbols-outlined text-green-600 text-lg">check_circle</span>
            <span v-else class="material-symbols-outlined text-amber-500 text-lg">warning</span>
            <span class="text-xs text-gray-500">{{ body.detail.payment.details.length }} รายการ</span>
          </div>

          <draggable v-model="body.detail.payment.details" group="files" class="mt-4" handle=".drag-handle"
            item-key="sequence" @end="reSequenceData">
            <template #item="{ element: data, index: index }">
              <div class="relative flex p-4 pt-10 bg-gray-100 rounded-2xl flex-col gap-2 mt-4">
                <div class="absolute top-3 right-3 flex items-center gap-4" v-if="!props.disable">
                  <i class="pi pi-trash cursor-pointer text-red-500 text-lg" @click="() => deleteItem(index)"
                    v-if="index != 0"></i>
                  <span class="material-symbols-outlined drag-handle cursor-move">
                    drag_indicator
                  </span>
                </div>
                <div class="grid lg:grid-cols-8 gap-2 gap-y-6">
                  <InputNumber :disabled="props.disable" label="งวดที่" v-model="data.no" rules="required" />
                  <InputNumber :disabled="props.disable" label="ระยะเวลา" v-model="data.leadTime" rules="required" />
                  <Datepicker :disabled="props.disable" label="วันที่ต้องส่งมอบ" v-model="data.deliveryDate" />
                  <InputNumber :disabled="props.disable" :max-number="100" :min-fraction-digits="2" label="ร้อยละ"
                    v-model="data.installmentPercentage" @update:model-value="() => onPercentageChange(data)"
                    rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="จำนวนเงิน"
                    v-model="data.amount" @update:model-value="() => onAmountChange(data)" grouping rules="required" />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="หักเงินล่วงหน้า"
                    v-model="data.advanceDeductionAmount" grouping />
                  <InputNumber :disabled="props.disable" :min-fraction-digits="2" label="หักเงินประกันผลงาน"
                    v-model="data.performanceDeductionAmount" grouping />
                </div>
                <InputArea :disabled="props.disable" label="รายละเอียดการส่งมอบ" v-model="data.description"
                  rules="required" class="mt-6" />
              </div>
            </template>
          </draggable>
        </div>
      </div>
    </template>
  </Card>
</template>