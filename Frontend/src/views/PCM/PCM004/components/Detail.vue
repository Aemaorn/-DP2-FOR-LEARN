<script setup lang="ts">
import {
  InputField,
  Select,
  InputArea,
  InputNumber,
  Checkbox,
  Radio,
  Datepicker,
} from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { usePcm004DetailStore } from '@/stores/PCM/pcm004';
import { computed, onUnmounted, ref, watch } from 'vue';
import { YearOptions } from '@/constants/date';
import type { Pcm004Vendor, Pcm004VendorParcels, Pcm004GlAccount } from '@/models/PCM/pcm004';
import { DataTable, Divider, type DataTableRowReorderEvent } from 'primevue';
import { CashType, Pcm004CommitteeType, Pcm004Status } from '@/enums/pcm004';
import Committee from './Committee.vue';
import { useMenuStore } from '@/stores/menu';
import { ArrayHelper } from '@/helpers/array';
import { formatCurrency } from '@/helpers/currency';
import { SupplyMethodSpecialTypeCode } from '@/enums/supplyMethod';

const pcm004DetailStore = usePcm004DetailStore();
const menuStore = useMenuStore();
const advanceCardRef = ref()

const { reSequence } = ArrayHelper();

watch(
  () => pcm004DetailStore.detail.supplyMethodCode,
  async (newValue) => {

    await pcm004DetailStore.getSupplyMethodSpecialTypeDDLAsync(newValue);

    const options = pcm004DetailStore.supplyMethidSpecialTypeDropdown;

    if (options.length > 0 && !pcm004DetailStore.detail.supplyMethodSpecialTypeCode) {
      pcm004DetailStore.detail.supplyMethodSpecialTypeCode = SupplyMethodSpecialTypeCode.specificMethod;
    }
  },
  { immediate: true });


const isVendorType = (vendor: Pcm004Vendor, type: string) => {
  return vendor.vendorType === type;
};

const getTaxNumberLabel = (vendor: Pcm004Vendor) => {
  return vendor.vendorType === '0' ? 'เลขประจำตัวผู้เสียภาษี' : 'เลขประจำตัว ปปช. (ถ้ามี)';
};

const getTaxNumberRules = (vendor: Pcm004Vendor) => {
  return vendor.vendorType === '0'
    ? 'required'
    : '';
};

const onRowReorder = (index: number, event: DataTableRowReorderEvent) => {
  pcm004DetailStore.detail.vendors[index].vendorParcels = reSequence(event.value);
};

const onRowReorderGlAccount = (event: DataTableRowReorderEvent) => {
  pcm004DetailStore.detail.glAccounts = reSequence(event.value);
};

const procCommittee = computed({
  get: () => {
    const committees = pcm004DetailStore.detail?.committees ?? [];
    return committees.filter(item => item.groupType === Pcm004CommitteeType.ProcurementCommittee);
  },
  set: (newValue) => {
    const committees = pcm004DetailStore.detail?.committees ?? [];
    pcm004DetailStore.detail.committees = [
      ...committees.filter(item => item.groupType !== Pcm004CommitteeType.ProcurementCommittee),
      ...newValue.map(item => ({ ...item, groupType: Pcm004CommitteeType.ProcurementCommittee }))
    ];
  }
});

const inspCommittee = computed({
  get: () => {
    const committees = pcm004DetailStore.detail?.committees ?? [];
    return committees.filter(item => item.groupType === Pcm004CommitteeType.InspectionCommittee);
  },
  set: (newValue) => {
    const committees = pcm004DetailStore.detail?.committees ?? [];
    pcm004DetailStore.detail.committees = [
      ...committees.filter(item => item.groupType !== Pcm004CommitteeType.InspectionCommittee),
      ...newValue.map(item => ({ ...item, groupType: Pcm004CommitteeType.InspectionCommittee }))
    ];
  }
});

type ParamOption = {
  value: unknown;
  children?: ParamOption[] | null;
  valueKeys?: string[] | null;
};

const flattenOptionValues = (opts: ParamOption[]): string[] =>
  opts.flatMap(o => [String(o.value), ...flattenOptionValues(o.children ?? [])]);

const standardValues = computed(() => flattenOptionValues(
  pcm004DetailStore.isNotFromJorPor001
    ? pcm004DetailStore.pettyCashWithoutForm001TypeOptions
    : pcm004DetailStore.pettyCashStandardTypeOptions));
const convenienceValues = computed(() => flattenOptionValues(pcm004DetailStore.pettyCashConvenienceTypeOptions));

function findOptionByValue(opts: ParamOption[], value: string): ParamOption | undefined {
  for (const o of opts) {
    if (String(o.value) === value) return o;
    const found = findOptionByValue(o.children ?? [], value);
    if (found) return found;
  }
  return undefined;
}

function getSelectedSet(): Set<string> {
  const cats = pcm004DetailStore.detail?.categories ?? [];
  return new Set(cats.map(c => String(c.categoryTypeCode)));
}

function onCategoryChange(value: string, checked: boolean, group: 'standard' | 'convenience') {
  const current = getSelectedSet();

  const groupOptions = group === 'standard'
    ? (pcm004DetailStore.isNotFromJorPor001
        ? pcm004DetailStore.pettyCashWithoutForm001TypeOptions
        : pcm004DetailStore.pettyCashStandardTypeOptions)
    : pcm004DetailStore.pettyCashConvenienceTypeOptions;
  const option = findOptionByValue(groupOptions, value);
  const valueKeys = (option?.valueKeys ?? [])
    .filter(k => !!k && String(k).trim() !== '' && String(k).trim() !== '-');

  if (!Array.isArray(pcm004DetailStore.detail.glAccounts)) {
    pcm004DetailStore.detail.glAccounts = [];
  }

  if (checked) {
    const otherGroup = group === 'standard' ? convenienceValues.value : standardValues.value;
    otherGroup.forEach(v => current.delete(v));
    current.add(value);

    for (const key of valueKeys) {
      if (pcm004DetailStore.detail.glAccounts.some(x => x.glAccountCode === key)) continue;

      let firstEmpty = pcm004DetailStore.detail.glAccounts.find(x => !x.glAccountCode);
      if (!firstEmpty) {
        pcm004DetailStore.addGLAccount();
        firstEmpty = pcm004DetailStore.detail.glAccounts[pcm004DetailStore.detail.glAccounts.length - 1];
      }
      firstEmpty.glAccountCode = key;
    }
  } else {
    current.delete(value);
    option?.children?.forEach(c => current.delete(String(c.value)));

    for (const key of valueKeys) {
      const indexToRemove = pcm004DetailStore.detail.glAccounts.findIndex(x => x.glAccountCode === key);
      if (indexToRemove >= 0) pcm004DetailStore.removeGlAccount(indexToRemove);
    }

  }

  pcm004DetailStore.detail.categories = Array.from(current).map(code => ({ categoryTypeCode: code }));
}

function onCashTypeChange() {
  pcm004DetailStore.detail.categories = [];
  pcm004DetailStore.detail.glAccounts = [];
}

const selectedCategoryCodesSet = computed(() => getSelectedSet());

const onChangeSupplyMethod = (e?: string) => {
  if (!e) return;

  pcm004DetailStore.detail.supplyMethodSpecialTypeCode = undefined;
};

onUnmounted(() => {
  pcm004DetailStore.clearBody();
});


function calculateParcelPrice(parcel: Pcm004VendorParcels) {
  const quantity = Number(parcel.quantity || 0);
  const unitPrice = Number(parcel.unitPrice || 0);
  const totalPrice = quantity * unitPrice;
  parcel.totalPriceVat = totalPrice;
  parcel.totalPrice = totalPrice;
}

function setTotalPrice(parcel: Pcm004VendorParcels) {
  parcel.totalPrice = parcel.totalPriceVat;
}

const isSavePaymentAndVendors = computed(() => {

  if (pcm004DetailStore.isNotFromJorPor001) {
    return true;
  }

  return pcm004DetailStore.detail.status == Pcm004Status.WaitingForInspector
    || pcm004DetailStore.detail.status == Pcm004Status.WaitingForAssignment
    || pcm004DetailStore.detail.status == Pcm004Status.WaitingForCompletion
    || pcm004DetailStore.detail.status == Pcm004Status.Completed
});

const canEditPaymentDetail = computed(() =>
  pcm004DetailStore.isJPApproval || pcm004DetailStore.isEdit
);

const canEditVendor = computed(() =>
  pcm004DetailStore.isJPApproval
  || (pcm004DetailStore.isNotFromJorPor001 && pcm004DetailStore.isEdit)
);

const jorPor001Options = [
  { label: 'ไม่ออกแบบฟอร์ม จพ. 001', value: false },
  { label: 'ออกแบบฟอร์ม จพ. 001', value: true },
];

const jorPor001Model = computed<boolean | undefined>({
  get: () => pcm004DetailStore.detail.isFromJorPor001 ?? undefined,
  set: (v) => { pcm004DetailStore.detail.isFromJorPor001 = v ?? null; },
});

const isJorPor001Disabled = computed(() =>
  !pcm004DetailStore.isEdit || !menuStore.hasManage
);

const isCashTypeDisabled = computed(() =>
  (!pcm004DetailStore.isJPApproval && !pcm004DetailStore.isEdit) || !menuStore.hasManage
);

type CashGroup = {
  type: CashType;
  label: string;
  inputId: string;
  options: typeof pcm004DetailStore.pettyCashStandardTypeOptions;
  group: 'standard' | 'convenience';
};

const cashTypeGroups = computed((): CashGroup[] => {
  const groups: CashGroup[] = [
    {
      type: CashType.Standard,
      label: 'เงินสดย่อย',
      inputId: 'standard',
      options: pcm004DetailStore.isNotFromJorPor001
        ? pcm004DetailStore.pettyCashWithoutForm001TypeOptions
        : pcm004DetailStore.pettyCashStandardTypeOptions,
      group: 'standard',
    },
  ];

  if (!pcm004DetailStore.isNotFromJorPor001) {
    groups.push({
      type: CashType.Convenient,
      label: 'เงินสดย่อย-สะดวกใช้',
      inputId: 'convenient',
      options: pcm004DetailStore.pettyCashConvenienceTypeOptions,
      group: 'convenience',
    });
  }

  return groups;
});
</script>

<template>
  <Card>
    <template #content>
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-8 mt-8">
        <InputField class="col-start-1" label="เลขที่" v-model="pcm004DetailStore.detail.pPettyCashNumber" disabled />
        <Datepicker label="วันที่" rules="required" v-model="pcm004DetailStore.detail.pPettyCashDate"
          :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
        <Select class="col-start-1" label="ฝ่าย/ภาค" v-model="pcm004DetailStore.detail.departmentCode"
          :options="pcm004DetailStore.departmentDropdown" rules="required" disabled />
        <Select label="ปีงบประมาณ" v-model="pcm004DetailStore.detail.budgetYear" :options="YearOptions" rules="required"
          :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
        <Select class="col-start-1" label="วิธีการจัดหา" :options="pcm004DetailStore.supplyMethodDropdown"
          v-model="pcm004DetailStore.detail.supplyMethodCode" disabled rules="required"
          @update:model-value="onChangeSupplyMethod" />
        <Select :options="pcm004DetailStore.supplyMethodTypeDropdown"
          v-model="pcm004DetailStore.detail.supplyMethodTypeCode"
          :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" rules="required" />
        <Select :options="pcm004DetailStore.supplyMethidSpecialTypeDropdown"
          v-model="pcm004DetailStore.detail.supplyMethodSpecialTypeCode" disabled rules="required" />
        <InputField class="col-start-1 lg:col-span-4" label="เรื่อง" rules="required"
          v-model="pcm004DetailStore.detail.subject" :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
        <InputArea class="col-start-1 lg:col-span-4" label="เหตุผลความจำเป็นที่ต้องซื่อหรือจ้าง" rules="required"
          v-model="pcm004DetailStore.detail.reasons" :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
        <InputArea class="col-start-1 lg:col-span-4"
          label="ขอบเขตของงานหรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จะซื้อหรือจ้าง แล้วแต่กรณี" rules="required"
          v-model="pcm004DetailStore.detail.source" :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <InputNumber
          label="วงเงินที่จะซื้อหรือจ้าง โดยให้ระบุวงเงินงบประมาณ ถ้าไม่มีวงเงินดังกล่าว ให้ระบุวงเงินที่ประมาณว่าจะซื้อหรือจ้างในครั้งนั้น"
          :rules="`required|max_value:${pcm004DetailStore.detail.cashType == CashType.Standard ? 5000 : 2000}`"
          v-model="pcm004DetailStore.detail.budget" grouping :min-fraction-digits="2" :max-fraction-digits="3"
          :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <p>กำหนดเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ <span class="text-red-500">*</span></p>
      <div class="grid lg:grid-cols-4 gap-2 mt-6">
        <Datepicker label="ภายในวันที่" v-model="pcm004DetailStore.detail.deliveryDate" rules="required"
          :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage" />
      </div>
    </template>

  </Card>
  <Card class="mt-4">
    <template #content>
      <TitleHeader label="เบิกเงินสดย่อยกับฝ่าย" />
      <div class="grid lg:grid-cols-3 gap-2 mt-10">
        <Select class="col-start-1" label="เงินสดย่อยของฝ่าย" v-model="pcm004DetailStore.detail.pettyCaseDepartmentCode"
          :options="pcm004DetailStore.departmentDropdown" rules="required"
          :disabled="!pcm004DetailStore.isEdit || !menuStore.hasManage"
          @onSelect="(value: string) => pcm004DetailStore.getDefaultDepartmentDirectorAsync(value)" />
      </div>
    </template>
  </Card>

  <Card id="petty-cash-disbursement-section" class="mt-4" v-if="pcm004DetailStore.isWaitingForCompletion || pcm004DetailStore.isCompleted">
    <template #content>
      <TitleHeader label="รายละเอียดการเบิกจ่าย สำหรับผู้ถือเงินสดย่อย" />
      <div class="grid lg:grid-cols-4 gap-2 mt-10">
        <Datepicker label="วันที่เบิกเงินสดย่อย" v-model="pcm004DetailStore.detail.disbursementDate" rules="required"
          :disabled="!pcm004DetailStore.isWaitingForCompletion || !menuStore.hasManage" />
      </div>
    </template>
  </Card>

  
  <div id="petty-cash-detail-section">
    <Card class="mt-4">
      <template #content>
        <TitleHeader label="รายละเอียดเงินสดย่อย" />
        <Radio :options="jorPor001Options" v-model="jorPor001Model" :disabled="isJorPor001Disabled"
          rules="required" />

        <table v-if="jorPor001Model !== undefined" class="w-full border-collapse mt-4">
          <tbody>
            <tr v-for="g in cashTypeGroups" :key="g.inputId">
              <td class="border border-gray-300 p-3 align-top w-1/5">
                <div class="flex items-center gap-2">
                  <RadioButton v-model="pcm004DetailStore.detail.cashType" :inputId="g.inputId" :value="g.type"
                    :disabled="isCashTypeDisabled" @update:model-value="onCashTypeChange" />
                  <label :for="g.inputId">{{ g.label }}</label>
                </div>
              </td>
              <td class="border border-gray-300 p-3">
                <div class="flex flex-col gap-2">
                  <template v-for="data in g.options" :key="String(data.value)">
                    <Checkbox
                      :id="String(data.value)"
                      :label="String(data.label)"
                      :modelValue="selectedCategoryCodesSet.has(String(data.value))"
                      @update:model-value="(checked: any) => onCategoryChange(String(data.value), checked, g.group)"
                      :disabled="isCashTypeDisabled || pcm004DetailStore.detail.cashType !== g.type"
                      hide-details />
                    <div
                      v-if="data.children && data.children.length && selectedCategoryCodesSet.has(String(data.value))"
                      class="ml-6 mt-1 border-y border-gray-200 bg-white">
                      <div class="grid grid-cols-2">
                        <div v-for="(child, idx) in data.children" :key="String(child.value)"
                          class="px-3 py-2 border-gray-200 hover:bg-gray-50 transition-colors"
                          :class="[
                            idx % 2 === 0 ? 'border-r' : '',
                            idx < data.children.length - (data.children.length % 2 === 0 ? 2 : 1) ? 'border-b' : '',
                          ]">
                          <Checkbox
                            :id="String(child.value)"
                            :label="String(child.label)"
                            :modelValue="selectedCategoryCodesSet.has(String(child.value))"
                            @update:model-value="(checked: any) => onCategoryChange(String(child.value), checked, g.group)"
                            :disabled="isCashTypeDisabled || pcm004DetailStore.detail.cashType !== g.type"
                            hide-details />
                        </div>
                      </div>
                      <div
                        v-if="pcm004DetailStore.invalidParentCategoryCodes.includes(String(data.value))"
                        class="px-3 py-2 border-t border-red-200 bg-red-50 text-red-600 text-sm flex items-center gap-1">
                        <i class="pi pi-exclamation-circle" />
                        กรุณาเลือกรายการย่อยอย่างน้อย 1 รายการ
                      </div>
                    </div>
                  </template>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </template>
    </Card>
  </div>

  <Card class="mt-4" v-if="jorPor001Model !== undefined">
    <template #content>
      <div class="flex items-center gap-6">
        <TitleHeader label="ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย">
          <template #action>
            <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
              class="bg-white! hover:bg-red-50!" @click="pcm004DetailStore.addGLAccount()"
              v-if="pcm004DetailStore.isEdit && menuStore.hasManage" />
          </template>
        </TitleHeader>
      </div>
      <DataTable :value="pcm004DetailStore.detail.glAccounts" @row-reorder="onRowReorderGlAccount">
        <Column field="sequence" bodyStyle="vertical-align: center">
          <template #header>
            <p class="w-full font-bold text-center">ลำดับ</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">{{ data.sequence }}</p>
          </template>
        </Column>
        <Column field="solId">
          <template #header>
            <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm004DetailStore.solIdDropDown" v-model="data.solId" hide-details
              :disabled="!canEditPaymentDetail || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="budgetTypeCode">
          <template #header>
            <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm004DetailStore.budgetTypeDropDown" v-model="data.budgetTypeCode" hide-details
              :disabled="!canEditPaymentDetail || !menuStore.hasManage" rules="required"
              @update:model-value="data.projectNumber = undefined" />
          </template>
        </Column>
        <Column field="projectNumber" bodyStyle="min-width: 180px; width: 100px;">
          <template #header>
            <p class="w-full font-bold text-center">รหัสโครงการ</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.projectNumber" hide-details
              :disabled="!canEditPaymentDetail || data.budgetTypeCode !== 'BudgetType002' || !menuStore.hasManage" />
          </template>
        </Column>
        <Column field="glAccountCode">
          <template #header>
            <p class="w-full font-bold text-center">รหัสบัญชี</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm004DetailStore.glAccountDropDown" v-model="data.glAccountCode" hide-details :disabled="!canEditPaymentDetail || !menuStore.hasManage"
              rules="required" />
          </template>
        </Column>
        <Column field="amount" bodyStyle="min-width: 180px; width: 200px;">
          <template #header>
            <p class="w-full font-bold text-center">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <InputNumber v-model="data.amount" grouping :min-fraction-digits="2" hide-details
              :disabled="!canEditPaymentDetail || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column rowReorder headerStyle="width: 3rem" bodyStyle="vertical-align: top;padding-top: 22px">
          <template #rowreordericon>
            <div>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                v-if="pcm004DetailStore.isEdit && menuStore.hasManage && pcm004DetailStore.detail.glAccounts.length > 1">
                drag_indicator
              </span>
            </div>
          </template>
        </Column>
        <Column field="control" bodyStyle="vertical-align: top" v-if="pcm004DetailStore.isEdit && menuStore.hasManage">
          <template #body="{ index }">
            <i v-if="pcm004DetailStore.detail.glAccounts.length > 1"
              class="pi pi-trash mt-4 text-red-600 cursor-pointer"
              @click="() => pcm004DetailStore.removeGlAccount(index)" />
          </template>
        </Column>
      </DataTable>
    </template>
  </Card>

  <Committee v-if="pcm004DetailStore.detail.isFromJorPor001 === true" class="mt-4"
    :disable="!pcm004DetailStore.isEdit || !menuStore.hasManage" :showOption="false"
    label="ผู้ขอซื้อขอจ้าง" v-model:committee="procCommittee"
    v-model:spacialOption="pcm004DetailStore.positionProcOptions"
    :groupType="Pcm004CommitteeType.ProcurementCommittee" />

  <Committee v-if="pcm004DetailStore.detail.isFromJorPor001 === true"
    :disable="!pcm004DetailStore.isEdit || !menuStore.hasManage" :showOption="false" label="ผู้ตรวจรับ"
    v-model:committee="inspCommittee" v-model:spacialOption="pcm004DetailStore.positionInspOptions"
    :groupType="Pcm004CommitteeType.InspectionCommittee" />

  <template v-if="isSavePaymentAndVendors">
    <Card class="mt-4" ref="advanceCardRef">
      <template #content>
        <TitleHeader label="รายละเอียดการโอนเข้าบัญชีผู้สำรองจ่าย" />
        <Radio
          :options="[{ label: 'จ่ายเงินคืนผู้สำรองจ่าย', value: true }, { label: 'ชำระเงินให้ผู้ประกอบการ', value: false }]"
          v-model="pcm004DetailStore.detail.isAdvance"
          :disabled="!canEditVendor || !menuStore.hasManage" />
        <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 mt-10 gap-y-8">
          <InputField v-if="pcm004DetailStore.detail.isAdvance" label="ผู้สำรองจ่าย"
            v-model="pcm004DetailStore.detail.advance.advanceName"
            :disabled="!canEditVendor || !menuStore.hasManage" />
          <Select class="col-start-1" label="ช่องทางชำระเงิน" :options="pcm004DetailStore.paymentMethodDropDown"
            v-model="pcm004DetailStore.detail.advance.advancePaymentMethodCode" rules="required"
            :disabled="!canEditVendor || !menuStore.hasManage" />
          <Datepicker label="วันที่จ่ายเงิน" v-model="pcm004DetailStore.detail.advance.advancePaymentDate"
            rules="required" :disabled="!canEditVendor || !menuStore.hasManage" />
          <Select class="col-start-1"
            v-if="pcm004DetailStore.detail.advance.advancePaymentMethodCode == 'PaymentMethod002'" label="ธนาคาร"
            :options="pcm004DetailStore.bankDropDown" v-model="pcm004DetailStore.detail.advance.advanceBankCode"
            rules="required" :disabled="!canEditVendor || !menuStore.hasManage" />
          <InputField v-if="pcm004DetailStore.detail.advance.advancePaymentMethodCode == 'PaymentMethod002'"
            label="เลขที่บัญชี" v-model="pcm004DetailStore.detail.advance.advanceBankAccount" rules="required"
            :disabled="!canEditVendor || !menuStore.hasManage" />
          <InputField v-if="pcm004DetailStore.detail.advance.advancePaymentMethodCode == 'PaymentMethod002'"
            label="สาขา" v-model="pcm004DetailStore.detail.advance.advanceBankBranch"
            :disabled="!canEditVendor || !menuStore.hasManage" />
          <InputArea class="col-start-1 lg:col-span-4" label="รายละเอียดเพิ่มเติม"
            v-model="pcm004DetailStore.detail.advance.advanceDetail"
            :disabled="!canEditVendor || !menuStore.hasManage" />
        </div>
      </template>
    </Card>

    <Card class="mt-4">
      <template #content>
        <TitleHeader label="ผู้ประกอบการ">
          <template #action>
            <Button label="เพิ่มคู่ค้า" icon="pi pi-plus" severity="primary" variant="outlined"
              class="bg-white! hover:bg-red-50! " @click="pcm004DetailStore.addVendors"
              v-if="canEditVendor && menuStore.hasManage" />
          </template>
        </TitleHeader>
        <div v-for="(data, vendorIndex) in pcm004DetailStore.detail.vendors" :key="data.id">
          <div class="flex items-center mt-4 justify-end" v-if="pcm004DetailStore.detail.vendors.length > 0">
            <Button icon="pi pi-trash" severity="danger" variant="text"
              v-if="canEditVendor && menuStore.hasManage && pcm004DetailStore.detail.vendors.length > 1"
              @click="() => pcm004DetailStore.removeVendorList(vendorIndex)" />
          </div>

          <Radio :options="[{ label: 'นิติบุคคล', value: '0' }, { label: 'บุคคลธรรมดา', value: '1' }]"
            v-model="data.vendorType" rules="required"
            :disabled="!canEditVendor || !menuStore.hasManage" />

          <div class="grid lg:grid-cols-4 gap-2 mt-6 gap-y-8">
            <InputField v-if="isVendorType(data, '0')" label="ผู้ค้า" v-model="data.vendorName" rules="required"
              :disabled="!canEditVendor || !menuStore.hasManage">
            </InputField>

            <InputField v-if="isVendorType(data, '1')" label="ผู้ค้า" v-model="data.vendorName" rules="required"
              :disabled="!canEditVendor || !menuStore.hasManage" />

            <InputField :label="getTaxNumberLabel(data)" v-model="data.taxNumber" :rules="getTaxNumberRules(data)"
              :disabled="!canEditVendor || !menuStore.hasManage" />
            <InputField v-if="isVendorType(data, '0')" label="เลขที่สาขา" v-model="data.vendorBranchNumber"
              :disabled="!canEditVendor || !menuStore.hasManage" />
            <Select label="ประเภทภาษี" :options="pcm004DetailStore.vatTypeDropDown" v-model="data.vatIncludeTypeCode"
              :disabled="!canEditVendor || !menuStore.hasManage" rules="required" />

            <Select class="col-start-1" label="ประเภทเอกสาร" :options="pcm004DetailStore.invoiceDocumentTypeDropDown"
              v-model="data.billTypeCode" rules="required"
              :disabled="!canEditVendor || !menuStore.hasManage" />
            <InputField v-if="data.billTypeCode === 'InvoiceDocType005'" label="ระบุ ประเภทเอกสารอื่นๆ"
              v-model="data.billTypeOther" :disabled="!canEditVendor || !menuStore.hasManage" />
            <InputField label="เลขที่เอกสาร" v-model="data.billBookNo" rules="required"
              :disabled="!canEditVendor || !menuStore.hasManage" />
            <Datepicker label="วันที่เอกสาร (ถ้ามี)" v-model="data.billDate"
              :disabled="!canEditVendor || !menuStore.hasManage" />

            <InputArea class="col-start-1 lg:col-span-4"
              label="รายละเอียดเพิ่มเติม เช่น ที่อยู่ใบเสร็จ เบอร์โทร อีเมล อื่นๆ" v-model="data.billDetail"
              :disabled="!canEditVendor || !menuStore.hasManage" />
          </div>

          <TitleHeader label="รายการพัสดุ">
            <template #action>
              <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                class="bg-white! hover:bg-red-50!" @click="pcm004DetailStore.addParcelToVendor(vendorIndex)"
                v-if="canEditVendor && menuStore.hasManage" />
            </template>
          </TitleHeader>

          <DataTable class="mt-4" :value="data.vendorParcels" :reorderable-columns="true"
            @row-reorder="(e) => onRowReorder(vendorIndex, e)">
            <Column field="sequence" bodyStyle="vertical-align: top" class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ data: row }">
                <p class="text-center mt-6">{{ row.sequence }}</p>
              </template>
            </Column>

            <Column field="item" bodyStyle="vertical-align: top" class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">รายการ</p>
              </template>
              <template #body="{ data: row }">
                <InputField rules="required" class="mt-6" v-model="row.item"
                  :disabled="!canEditVendor || !menuStore.hasManage" />
                <InputArea rules="required" label="รายละเอียด" class="mt-5" v-model="row.itemDetail"
                  :disabled="!canEditVendor || !menuStore.hasManage" />
              </template>
            </Column>

            <Column field="unitCode" body-class="bg-gray-100 align-top" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">จำนวน/หน่วย</p>
              </template>
              <template #body="{ data: row }">
                <div class="grid lg:grid-cols-2 gap-2 mt-6">
                  <InputNumber v-model="row.quantity"
                    :disabled="!canEditVendor || !menuStore.hasManage" rules="required" hide-details
                    inputClass="text-right" @onChange="() => calculateParcelPrice(row)" />
                  <Select :options="pcm004DetailStore.unitOfMeasureDropDown" v-model="row.unitCode"
                    :disabled="!canEditVendor || !menuStore.hasManage" rules="required" hide-details />
                </div>
              </template>
            </Column>

            <Column field="unitPrice" body-class="bg-gray-100 align-top" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">{{ data.vatIncludeTypeCode === 'VATType001' ?
                  'ราคา/หน่วย' : 'ราคา/หน่วย' }} </p>
              </template>
              <template #body="{ data: row }">
                <InputNumber class="mt-6" v-model="row.unitPrice" grouping :min-fraction-digits="2"
                  :disabled="!canEditVendor || !menuStore.hasManage" rules="required" hide-details
                  @onChange="() => calculateParcelPrice(row)" />
              </template>
            </Column>
            <Column field="totalPriceVat" body-class="bg-gray-100 align-top" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">{{ data.vatIncludeTypeCode === 'VATType001' ? 'ราคารวม' :
                  'ราคารวม' }} </p>
              </template>
              <template #body="{ data: row }">
                <InputNumber class="mt-6" v-model="row.totalPriceVat" grouping :min-fraction-digits="2" rules="required"
                  disabled hide-details @onChange="() => setTotalPrice(row)" />
              </template>
            </Column>

            <Column field="control" bodyStyle="vertical-align: top" class="bg-gray-100">
              <template #body="{ index }">
                <Button icon="pi pi-trash" severity="danger" variant="text" v-if="data.vendorParcels.length > 1"
                  @click="() => pcm004DetailStore.removeParcelFromVendor(vendorIndex, index)" />
              </template>
            </Column>

            <Column rowReorder headerStyle="width: 3rem" class="max-w-[50px] bg-gray-100"
              bodyStyle="vertical-align: top;padding-top: 22px">
              <template #rowreordericon>
                <div>
                  <span class="material-symbols-outlined cursor-pointer" :draggable="true"
                    v-if="pcm004DetailStore.isEdit && menuStore.hasManage && data.vendorParcels.length > 1">
                    drag_indicator
                  </span>
                </div>
              </template>
            </Column>

            <template #empty>
              <p class="text-center font-bold">ไม่พบข้อมูล</p>
            </template>
          </DataTable>
          <Divider
            v-if="pcm004DetailStore.detail.vendors.length > 1 && vendorIndex != pcm004DetailStore.detail.vendors.length - 1"
            class="mt-8" />
          <div class="flex flex-col w-full items-end mt-8" v-if="data.vatIncludeTypeCode === 'VATType001'">
            <div class="flex items-center gap-4 text-xl font-bold pt-4">
              <span class="text-right">รวมจำนวนเงินทั้งสิ้น</span>
              <span class="min-w-[150px] text-right">{{formatCurrency(data.vendorParcels.reduce((prev, curr) => prev +
                curr.totalPrice, 0))}}</span>
            </div>
          </div>
          <div class="flex flex-col w-full items-end mt-8" v-if="data.vatIncludeTypeCode === 'VATType002'">
            <div class="flex items-center gap-4 text-2xl font-bold mb-2">
              <span class="text-right">รวมจำนวนเงิน</span>
              <span class="min-w-[150px] text-right">{{
                formatCurrency(
                  data.vendorParcels.reduce((sum, parcel) => sum + (parcel.totalPriceVat / 1.07), 0)
                )
              }}</span>
            </div>

            <div class="flex items-center gap-4 text-xl font-bold text-primary-500 mb-4">
              <span class="text-right">ภาษีมูลค่าเพิ่ม</span>
              <span class="min-w-[150px] text-right"> {{
                formatCurrency(
                  data.vendorParcels.reduce((sum, parcel) => sum + parcel.totalPriceVat, 0) -
                  data.vendorParcels.reduce((sum, parcel) => sum + (parcel.totalPriceVat / 1.07), 0)
                )
              }}</span>
            </div>

            <div class="flex items-center gap-4 text-xl font-bold border-t border-gray-300 pt-4">
              <span class="text-right">รวมจำนวนเงินทั้งสิ้น</span>
              <span class="min-w-[150px] text-right">{{
                formatCurrency(data.vendorParcels.reduce((sum, parcel) => sum + parcel.totalPriceVat, 0))
              }}</span>
            </div>
          </div>
        </div>
      </template>
    </Card>
  </template>
</template>

<style scoped lang="scss">
:deep(.p-datatable-column-title) {
  font-size: 20px;
}
</style>
