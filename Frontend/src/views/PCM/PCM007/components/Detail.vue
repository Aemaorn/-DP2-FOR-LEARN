<script setup lang="ts">
import {
  InputField,
  Datepicker,
  Select,
  InputArea,
  InputNumber,
  Radio,
} from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { usePcm007DetailStore } from '@/stores/PCM/pcm007';
import { computed, watch } from 'vue';
import { YearOptions } from '@/constants/date';
import { SupplyMethodSpecialTypeCode } from '@/enums/supplyMethod';
import { showPartnerDialogAsync } from '@/helpers/dialog';
import type { Pcm007Vendor, Pcm007VendorParcels } from '@/models/PCM/pcm007';
import { Pcm007CommitteeType } from '@/enums/pcm007';
import { useMenuStore } from '@/stores/menu';
import { DataTable, Divider, type DataTableRowReorderEvent } from 'primevue';
import { DatatableHelper } from '@/helpers/datable';
import { formatCurrency } from '@/helpers/currency';
import Committee from './Committee.vue';

const { onRowReorder } = DatatableHelper();

const menuStore = useMenuStore();
const pcm007DetailStore = usePcm007DetailStore();

watch(
  () => pcm007DetailStore.detail.supplyMethodCode,
  async (newValue) => {
    await pcm007DetailStore.getSupplyMethodSpecialTypeDDLAsync(newValue);

    const options = pcm007DetailStore.supplyMethodSpecialTypeDropdown;

    if (options.length > 0 && !pcm007DetailStore.detail.supplyMethodSpecialTypeCode) {
      pcm007DetailStore.detail.supplyMethodSpecialTypeCode = SupplyMethodSpecialTypeCode.specificMethod;
    }
  },
  { immediate: true });

const onRowReorderFunction = (event: DataTableRowReorderEvent, mainIndex: number) => {
  pcm007DetailStore.detail.vendors[mainIndex].vendorParcels = onRowReorder(event);
};

const onRowReOrderGlAccount = (event: DataTableRowReorderEvent) => {
  pcm007DetailStore.detail.glAccounts = onRowReorder(event);
};

const onShowPartnerDialogAsync = async (data: Pcm007Vendor) => {
  const res = await showPartnerDialogAsync();

  if (res) {
    data.suVendorId = res.id;
    data.vendorName = res.establishmentName;
    data.taxNumber = res.taxpayerIdentificationNo;
    data.vendorBranchNumber = res.sapBranchNumber;
  }
};

const isVendorType = (vendor: Pcm007Vendor, type: string) => {
  return vendor.vendorType === type;
};

const getTaxNumberLabel = (vendor: Pcm007Vendor) => {
  return vendor.vendorType === '0' ? 'เลขประจำตัวผู้เสียภาษี' : 'เลขประจำตัว ปปช. (ถ้ามี)';
};

const getTaxNumberRules = (vendor: Pcm007Vendor) => {
  return vendor.vendorType === '0' ? 'required' : '';
};

const onChangeBudgetType = (e: string | undefined, index: number) => {
  if (e && e === 'BudgetType001') {
    pcm007DetailStore.detail.glAccounts[index].projectNumber = undefined;
  }
};

function calculateParcelPrice(parcel: Pcm007VendorParcels) {
  const quantity = Number(parcel.quantity || 0);
  const unitPrice = Number(parcel.unitPrice || 0);
  const totalPrice = quantity * unitPrice;
  parcel.totalPriceVat = totalPrice;
  parcel.totalPrice = totalPrice;
}

function setTotalPrice(parcel: Pcm007VendorParcels) {
  parcel.totalPrice = parcel.totalPriceVat;
}

const procCommittee = computed({
  get: () => {
    const committees = pcm007DetailStore.detail?.committees ?? [];
    return committees.filter(item => item.groupType === Pcm007CommitteeType.ProcurementCommittee);
  },
  set: (newValue) => {
    const committees = pcm007DetailStore.detail?.committees ?? [];
    pcm007DetailStore.detail.committees = [
      ...committees.filter(item => item.groupType !== Pcm007CommitteeType.ProcurementCommittee),
      ...newValue.map(item => ({ ...item, groupType: Pcm007CommitteeType.ProcurementCommittee }))
    ];
  }
});

const inspCommittee = computed({
  get: () => {
    const committees = pcm007DetailStore.detail?.committees ?? [];
    return committees.filter(item => item.groupType === Pcm007CommitteeType.InspectionCommittee);
  },
  set: (newValue) => {
    const committees = pcm007DetailStore.detail?.committees ?? [];
    pcm007DetailStore.detail.committees = [
      ...committees.filter(item => item.groupType !== Pcm007CommitteeType.InspectionCommittee),
      ...newValue.map(item => ({ ...item, groupType: Pcm007CommitteeType.InspectionCommittee }))
    ];
  }
});
</script>

<template>
  <Card>
    <template #content>
      <div class="grid lg:grid-cols-4 gap-2 mt-6">
        <InputField label="เลขที่" v-model="pcm007DetailStore.detail.pw184Number" disabled />
        <Datepicker label="วันที่" rules="required" v-model="pcm007DetailStore.detail.pw184Date"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <Select label="ฝ่าย/ภาค เขต" v-model="pcm007DetailStore.detail.departmentCode"
          :options="pcm007DetailStore.departmentDropdown" rules="required" disabled />
        <Select label="ปีงบประมาณ" v-model="pcm007DetailStore.detail.budgetYear" :options="YearOptions" rules="required"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <Select label="วิธีการจัดหา" :options="pcm007DetailStore.supplyMethodDropdown"
          v-model="pcm007DetailStore.detail.supplyMethodCode"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" />
        <Select :options="pcm007DetailStore.supplyMethodSpecialTypeDropdown"
          v-model="pcm007DetailStore.detail.supplyMethodSpecialTypeCode"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <InputArea class="lg:col-span-4" label="เรื่อง" rules="required" v-model="pcm007DetailStore.detail.subject"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <InputArea class="lg:col-span-4" label="สรุปเรื่อง/เหตุผลความจำเป็น" rules="required"
          v-model="pcm007DetailStore.detail.source" :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <InputArea class="lg:col-span-4" label="หมายเหตุ" v-model="pcm007DetailStore.detail.reason"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <InputNumber label="วงเงินงบประมาณ" rules="required" v-model="pcm007DetailStore.detail.budget" grouping
          :min-fraction-digits="2" :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>

  <Committee class="mt-4" :disable="!pcm007DetailStore.isEdit || !menuStore.hasManage" :showOption="false"
    label="ผู้ขอซื้อขอจ้าง" v-model:committee="procCommittee"
    v-model:spacialOption="pcm007DetailStore.positionProcOptions"
    :groupType="Pcm007CommitteeType.ProcurementCommittee" />

  <Committee :disable="!pcm007DetailStore.isEdit || !menuStore.hasManage" :showOption="false" label="ผู้ตรวจรับ"
    v-model:committee="inspCommittee" v-model:spacialOption="pcm007DetailStore.positionInspOptions"
    :groupType="Pcm007CommitteeType.InspectionCommittee" />

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="รายละเอียดการโอนเข้าบัญชีผู้สำรองจ่าย" />
      <div class="flex items-center gap-4 mt-4">
        <Radio
          :options="[{ label: 'จ่ายเงินคืนผู้สำรองจ่าย', value: true }, { label: 'ชำระเงินให้ผู้ประกอบการ', value: false }]"
          v-model="pcm007DetailStore.detail.isAdvance" :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 mt-8" v-if="pcm007DetailStore.detail.isAdvance">
        <InputField class="col-span-2" label="ผู้สำรองจ่าย" v-model="pcm007DetailStore.detail.advanceName"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>

      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <Select label="ช่องทางชำระเงิน" :options="pcm007DetailStore.paymentMethodDropdown"
          v-model="pcm007DetailStore.detail.advancePaymentMethodCode" rules="required"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <Datepicker label="วันที่จ่ายเงิน" v-model="pcm007DetailStore.detail.advancePaymentDate" rules="required"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8"
        v-if="pcm007DetailStore.detail.advancePaymentMethodCode === 'PaymentMethod002'">
        <Select label="ธนาคาร" :options="pcm007DetailStore.bankDropdown"
          v-model="pcm007DetailStore.detail.advanceBankCode" rules="required"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="เลขที่บัญชี" v-model="pcm007DetailStore.detail.advanceBankAccount" rules="required"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="สาขา" v-model="pcm007DetailStore.detail.advanceBankBranch"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="ชื่อบัญชี" v-model="pcm007DetailStore.detail.advanceBankAccountName"
          :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <InputArea label="รายละเอียดเพิ่มเติม" v-model="pcm007DetailStore.detail.advanceDetail"
        :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" class="mt-8" />
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="ผู้ประกอบการ">
        <template #action>
          <Button label="เพิ่มคู่ค้า" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="pcm007DetailStore.addVendor"
            v-if="pcm007DetailStore.isEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>

      <div v-for="(data, vendorIndex) in pcm007DetailStore.detail.vendors" :key="data.id ?? vendorIndex">
        <div class="flex items-center justify-between gap-4 px-2">
          <Radio :options="[{ label: 'นิติบุคคล', value: '0' }, { label: 'บุคคลธรรมดา', value: '1' }]"
            v-model="data.vendorType" :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />

          <i class="pi pi-trash text-red-600 cursor-pointer pb-5 mt-10"
            v-if="pcm007DetailStore.isEdit && menuStore.hasManage && data.sequence !== 1"
            @click="() => pcm007DetailStore.removeVendor(vendorIndex)" />
        </div>

        <div class="grid lg:grid-cols-4 gap-2 gap-y-8 mt-6">
          <InputField v-if="isVendorType(data, '0')" label="ผู้ค้า" v-model="data.vendorName" rules="required"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage">
            <template #appendAction>
              <InputGroupAddon>
                <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! h-full"
                  @click="onShowPartnerDialogAsync(data)" v-if="menuStore.hasManage" />
              </InputGroupAddon>
            </template>
          </InputField>

          <InputField v-if="isVendorType(data, '1')" label="ผู้ค้า" v-model="data.vendorName"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" />

          <InputField :label="getTaxNumberLabel(data)" v-model="data.taxNumber" :rules="getTaxNumberRules(data)"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
          <InputField v-if="isVendorType(data, '0')" label="เลขที่สาขา" v-model="data.vendorBranchNumber"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />

          <Select class="lg:col-start-1" label="ประเภทเอกสาร" :options="pcm007DetailStore.billTypeDropdown"
            v-model="data.billTypeCode" rules="required"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
          <InputField v-if="data.billTypeCode === 'InvoiceDocType005'" label="ระบุ ประเภทเอกสารอื่นๆ"
            v-model="data.billTypeOther" :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
          <InputField label="เลขที่เอกสาร" v-model="data.billBookNo" rules="required"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
          <Datepicker label="วันที่เอกสาร" v-model="data.billDate" rules="required"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
          <InputArea class="lg:col-start-1 lg:col-span-3" label="รายละเอียดเพิ่มเติม" v-model="data.billDetail"
            :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
        </div>

        <TitleHeader label="รายการพัสดุ" class="mt-4">
          <template #action>
            <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
              class="bg-white! hover:bg-red-50!" @click="pcm007DetailStore.addParcelToVendor(vendorIndex)"
              v-if="pcm007DetailStore.isEdit && menuStore.hasManage" />
          </template>
        </TitleHeader>

        <DataTable :value="data.vendorParcels" @row-reorder="(e) => onRowReorderFunction(e, vendorIndex)" class="mt-3">
          <Column field="sequence" body-class="bg-gray-100 align-top" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ data: row }">
              <p class="text-center mt-6">{{ row.sequence }}</p>
            </template>
          </Column>

          <Column field="item" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">รายการ</p>
            </template>
            <template #body="{ data: row }">
              <InputField v-model="row.item" :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage"
                rules="required" hide-details class="mb-5 mt-6" />
              <InputArea class="mt-14" label="รายละเอียด" v-model="row.itemDetail"
                :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
            </template>
          </Column>

          <Column field="unitCode" body-class="bg-gray-100 align-top min-w-[400px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">จำนวน/หน่วย</p>
            </template>
            <template #body="{ data: row }">
              <div class="grid lg:grid-cols-2 gap-2">
                <InputNumber class="mt-6" v-model="row.quantity"
                  :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details
                  inputClass="text-right" @onChange="() => calculateParcelPrice(row)" grouping />
                <Select class="mt-6" :options="pcm007DetailStore.unitOfMeasureDropdown" v-model="row.unitCode"
                  :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details />
              </div>
            </template>
          </Column>

          <Column field="vatIncludeTypeCode" body-class="bg-gray-100 align-top min-w-[200px]"
            header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">ประเภทภาษี</p>
            </template>
            <template #body="{ data: row }">
              <Select class="mt-6" :options="pcm007DetailStore.vatTypeDropdown" v-model="row.vatIncludeTypeCode"
                :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
            </template>
          </Column>

          <Column field="unitPrice" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">ราคา/หน่วย</p>
            </template>
            <template #body="{ data: row }">
              <InputNumber class="mt-6" v-model="row.unitPrice" grouping :min-fraction-digits="2"
                :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details
                @onChange="() => calculateParcelPrice(row)" />
            </template>
          </Column>

          <Column field="totalPriceVat" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">ราคารวม</p>
            </template>
            <template #body="{ data: row }">
              <InputNumber class="mt-6" v-model="row.totalPriceVat" grouping :min-fraction-digits="2" rules="required"
                hide-details @onChange="() => setTotalPrice(row)"
                :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" />
            </template>
          </Column>

          <Column field="control" body-class="bg-gray-100 align-top" header-class="bg-gray-100"
            v-if="pcm007DetailStore.isEdit && menuStore.hasManage">
            <template #body="{ index }">
              <i class="pi pi-trash mt-4 text-red-600 cursor-pointer"
                @click="() => pcm007DetailStore.removeParcelFromVendor(vendorIndex, index)" v-if="index !== 0" />
            </template>
          </Column>

          <Column rowReorder body-class="bg-gray-100 align-top pt-6" header-class="bg-gray-100 w-[3rem]"
            v-if="pcm007DetailStore.isEdit && menuStore.hasManage">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                drag_indicator
              </span>
            </template>
          </Column>
        </DataTable>
        <Divider class="py-4" />
        <div class="flex flex-col w-full items-end mt-8"
          v-if="!data.vendorParcels.some(p => p.vatIncludeTypeCode === 'VATType002')">
          <div class="flex items-center gap-4 text-xl font-bold pt-4">
            <span class="text-right">รวมจำนวนเงินทั้งสิ้น</span>
            <span class="min-w-[150px] text-right">{{formatCurrency(data.vendorParcels.reduce((prev, curr) => prev +
              curr.totalPrice, 0))}}</span>
          </div>
        </div>
        <div class="flex flex-col w-full items-end mt-8"
          v-if="data.vendorParcels.some(p => p.vatIncludeTypeCode === 'VATType002')">
          <div class="flex items-center gap-4 text-2xl font-bold mb-2">
            <span class="text-right">รวมจำนวนเงิน</span>
            <span class="min-w-[150px] text-right">{{
              formatCurrency(data.vendorParcels.reduce((sum, parcel) => sum + (parcel.totalPriceVat / 1.07), 0))
              }}</span>
          </div>
          <div class="flex items-center gap-4 text-xl font-bold text-primary-500 mb-4">
            <span class="text-right">ภาษีมูลค่าเพิ่ม</span>
            <span class="min-w-[150px] text-right">{{
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

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย">
        <template #action>
          <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="pcm007DetailStore.addGLAccount()"
            v-if="pcm007DetailStore.isEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <DataTable :value="pcm007DetailStore.detail.glAccounts" @row-reorder="onRowReOrderGlAccount">
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
            <Select :options="pcm007DetailStore.solIdDropdown" v-model="data.solId" hide-details
              :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="budgetTypeCode">
          <template #header>
            <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
          </template>
          <template #body="{ data, index }">
            <Select :options="pcm007DetailStore.budgetTypeDropdown" v-model="data.budgetTypeCode" hide-details
              :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required"
              @on-select="(e: string) => onChangeBudgetType(e, index)" />
          </template>
        </Column>
        <Column field="projectNumber">
          <template #header>
            <p class="w-full font-bold text-center min-w-[200px]">รหัสโครงการ</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.projectNumber" hide-details
              :disabled="!pcm007DetailStore.isEdit || data.budgetTypeCode === 'BudgetType001' || !menuStore.hasManage" />
          </template>
        </Column>
        <Column field="glAccountCode">
          <template #header>
            <p class="w-full font-bold text-center">รหัสบัญชี</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm007DetailStore.glAccountDropdown" v-model="data.glAccountCode" hide-details
              :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="amount">
          <template #header>
            <p class="w-full font-bold text-center min-w-[200px]">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <InputNumber v-model="data.amount" hide-details grouping :min-fraction-digits="2"
              :disabled="!pcm007DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="control" bodyStyle="vertical-align: top" v-if="pcm007DetailStore.isEdit && menuStore.hasManage">
          <template #body="{ index }">
            <i v-if="pcm007DetailStore.detail.glAccounts.length > 1"
              class="pi pi-trash mt-4 text-red-600 cursor-pointer"
              @click="() => pcm007DetailStore.removeGlAccount(index)" />
          </template>
        </Column>
      </DataTable>
    </template>
  </Card>
</template>

<style scoped lang="scss">
:deep(.p-datatable-column-title) {
  font-size: 20px;
}

:deep(.p-select-overlay .p-component) {
  max-width: 100% !important;
}
</style>
