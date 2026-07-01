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
import { usePcm002DetailStore } from '@/stores/PCM/pcm002';
import { watch } from 'vue';
import { YearOptions } from '@/constants/date';
import { SupplyMethodSpecialTypeCode } from '@/enums/supplyMethod';
import { showPartnerDialogAsync } from '@/helpers/dialog';
import type { Pcm002Vendor, Pcm002VendorParcels } from '@/models/PCM/pcm002';
import { useMenuStore } from '@/stores/menu';
import type { DataTableRowReorderEvent } from 'primevue';
import { DatatableHelper } from '@/helpers/datable';
import { formatCurrency } from '@/helpers/currency';

const { onRowReorder } = DatatableHelper();

const menuStore = useMenuStore();
const pcm002DetailStore = usePcm002DetailStore();

const onRowReorderFunction = (event: DataTableRowReorderEvent, mainIndex: number) => {
  pcm002DetailStore.detail.vendors[mainIndex].vendorParcels = onRowReorder(event)
};

const onRowReOrderGlAccount = (event: any) => {
  pcm002DetailStore.removeGlAccount(event.value);
};

watch(
  () => pcm002DetailStore.detail.supplyMethodCode,
  async (newValue) => {

    await pcm002DetailStore.getSupplyMethodSpecialTypeDDLAsync(newValue);

    const options = pcm002DetailStore.supplyMethidSpecialTypeDropDown;

    if (options.length > 0 && !pcm002DetailStore.detail.supplyMethodSpecialTypeCode) {
      pcm002DetailStore.detail.supplyMethodSpecialTypeCode = SupplyMethodSpecialTypeCode.specificMethod;
    }
  },
  { immediate: true });

const onShowPartnerDialogAsync = async (data: Pcm002Vendor) => {
  const res = await showPartnerDialogAsync();

  if (res) {
    data.suVendorId = res.id;
    data.vendorName = res.establishmentName;
    data.taxNumber = res.taxpayerIdentificationNo;
    data.vendorBranchNumber = res.sapBranchNumber;
  }
};

const isVendorType = (vendor: Pcm002Vendor, type: string) => {
  return vendor.vendorType === type;
};

const getTaxNumberLabel = (vendor: Pcm002Vendor) => {
  return vendor.vendorType === '0' ? 'เลขประจำตัวผู้เสียภาษี' : 'เลขประจำตัว ปปช. (ถ้ามี)';
};

const getTaxNumberRules = (vendor: Pcm002Vendor) => {
  return vendor.vendorType === '0'
    ? 'required'
    : '';
};

const onChangeBudgetType = (e: string | undefined, index: number) => {
  if (e && e === 'BudgetType001') {
    pcm002DetailStore.detail.glAccounts[index].projectNumber = undefined;
  }
}

function calculateParcelPrice(parcel: Pcm002VendorParcels) {
  const quantity = Number(parcel.quantity || 0);
  const unitPrice = Number(parcel.unitPrice || 0);
  const totalPrice = quantity * unitPrice;
  parcel.totalPriceVat = totalPrice;
  parcel.totalPrice = totalPrice;
}

function setTotalPrice(parcel: Pcm002VendorParcels) {
  parcel.totalPrice = parcel.totalPriceVat;
}

</script>

<template>
  <Card>
    <template #content>
      <div class="grid lg:grid-cols-4 gap-2 mt-6">
        <InputField label="เลขที่" v-model="pcm002DetailStore.detail.pw119Number" disabled />
        <Datepicker label="วันที่" rules="required" v-model="pcm002DetailStore.detail.pw119Date"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="เบอร์โทร" v-model="pcm002DetailStore.detail.telephone" rules="required"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <Select label="ฝ่าย/ภาค เขต" v-model="pcm002DetailStore.detail.departmentCode"
          :options="pcm002DetailStore.departmentDropdown" rules="required" disabled />

        <Select label="ปีงบประมาณ" v-model="pcm002DetailStore.detail.budgetYear" :options="YearOptions" rules="required"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />

        <Select label="วิธีการจัดหา" :options="pcm002DetailStore.supplyMethodDropdown"
          v-model="pcm002DetailStore.detail.supplyMethodCode" disabled />
        <Select :options="pcm002DetailStore.supplyMethidSpecialTypeDropDown"
          v-model="pcm002DetailStore.detail.supplyMethodSpecialTypeCode" disabled />
      </div>
      <InputArea label="เรื่อง" rules="required" v-model="pcm002DetailStore.detail.subject"
        :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" class="mb-2 mt-8" />
      <InputArea label="สรุปเรื่อง" rules="required" v-model="pcm002DetailStore.detail.source"
        :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" class="mb-2 mt-8" />
      <p class="font-bold">ตาราง 1
        รายการเกี่ยวกับค่าใช้จ่ายในการบริหารงานของหน่วยงานของรัฐที่ต้องดำเนินการภายใต้พระราชบัญญติการจัดซื้อจัดจ้างและการบริหารพัสดุภาครัฐ
        พ.ศ.2560</p>
      <div>
        <Select label="ลำดับค่าใช้จ่าย" class="my-2 mt-8" :options="pcm002DetailStore.expenseItemW119DropDown"
          v-model="pcm002DetailStore.detail.w119CategoriesCode" rules="required"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
        <InputArea label="รายละเอียดค่าใช้จ่าย" class="mt-8"
          :model-value="pcm002DetailStore.expenseItemW119DropDown.find(item => item.value === pcm002DetailStore.detail.w119CategoriesCode)?.label"
          disabled />
      </div>
    </template>
  </Card>

  <Card class="mt-4" v-if="!pcm002DetailStore.isBranchOrZoneDepartment">
    <template #content>
      <TitleHeader label="ส่วนงาน จพ. รับผิดชอบ" />
      <div class="grid lg:grid-cols-4 gap-2 mt-10">
        <Select label="ส่วนงาน จพ." v-model="pcm002DetailStore.detail.assignSegmentCode" rules="required"
          :options="pcm002DetailStore.assignDepartmentDDL" v-if="pcm002DetailStore.assignDepartmentDDL"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="รายละเอียดการโอนเข้าบัญชีผู้สำรองจ่าย" />
      <div class="flex items-center justify-between gap-4 px-2">
        <Radio
          :options="[{ label: 'จ่ายเงินคืนผู้สำรองจ่าย', value: true }, { label: 'ชำระเงินให้ผู้ประกอบการ', value: false }]"
          v-model="pcm002DetailStore.detail.advance.isAdvance"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
      </div>

      <div class="grid grid-cols-2 mt-8" v-if="pcm002DetailStore.detail.advance.isAdvance">
        <InputField label="ผู้สำรองจ่าย" v-model="pcm002DetailStore.detail.advance.advanceName"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
      </div>

      <div class="grid lg:grid-cols-4 gap-2 mt-8">
        <Select label="ช่องทางชำระเงิน" :options="pcm002DetailStore.paymentMethodDropDown"
          v-model="pcm002DetailStore.detail.advance.advancePaymentMethodCode" rules="required"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <div class="grid lg:grid-cols-4 gap-2 mt-8"
        v-if="pcm002DetailStore.detail.advance.advancePaymentMethodCode === 'PaymentMethod002'">
        <Select label="ธนาคาร" :options="pcm002DetailStore.bankDropDown"
          v-model="pcm002DetailStore.detail.advance.advanceBankCode" rules="required"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="เลขที่บัญชี" v-model="pcm002DetailStore.detail.advance.advanceBankAccount" rules="required"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="สาขา" v-model="pcm002DetailStore.detail.advance.advanceBankBranch"
          :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
      </div>
      <InputArea label="รายละเอียดเพิ่มเติม" v-model="pcm002DetailStore.detail.advance.advanceDetail"
        :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" class="mt-8" />
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="ผู้ประกอบการ">
        <template #action>
          <Button label="เพิ่มคู่ค้า" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="pcm002DetailStore.addVendors"
            v-if="pcm002DetailStore.isEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>

      <div v-for="(data, vendorIndex) in pcm002DetailStore.detail.vendors" :key="data.id">
        <div class="flex items-center justify-between gap-4 px-2">
          <Radio :options="[{ label: 'นิติบุคคล', value: '0' }, { label: 'บุคคลธรรมดา', value: '1' }]"
            v-model="data.vendorType" :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />

          <i class="pi pi-trash text-red-600 cursor-pointer pb-5 mt-10"
            v-if="pcm002DetailStore.isEdit && menuStore.hasManage && data.sequence !== 1"
            @click="() => pcm002DetailStore.removeVendorList(vendorIndex)" />
        </div>
        <div>

          <div class="grid lg:grid-cols-4 gap-2 gap-y-8 mt-6">
            <InputField v-if="isVendorType(data, '0')" label="ผู้ค้า" v-model="data.vendorName" rules="required"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage">
              <template #appendAction>
                <InputGroupAddon>
                  <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! h-full"
                    @click="onShowPartnerDialogAsync(data)" v-if="menuStore.hasManage" />
                </InputGroupAddon>
              </template>
            </InputField>

            <InputField v-if="isVendorType(data, '1')" label="ผู้ค้า" v-model="data.vendorName"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" />

            <InputField :label="getTaxNumberLabel(data)" v-model="data.taxNumber" :rules="getTaxNumberRules(data)"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
            <InputField v-if="isVendorType(data, '0')" label="เลขที่สาขา" v-model="data.vendorBranchNumber"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />

            <Select class="lg:col-start-1" label="ประเภทเอกสาร" :options="pcm002DetailStore.invoiceDocumentTypeDropDown"
              v-model="data.billTypeCode" rules="required"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
            <InputField v-if="data.billTypeCode === 'InvoiceDocType005'" label="ระบุ ประเภทเอกสารอื่นๆ"
              v-model="data.billTypeOther" :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
            <InputField label="เลขที่เอกสาร" v-model="data.billBookNo" rules="required"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
            <Datepicker label="วันที่เอกสาร" v-model="data.billDate" rules="required"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
            <div class="lg:col-start-1 lg:col-span-3 lg:flex items-center justify-between">
              <div class="bg-[#F5F5F5] p-2 flex-1">
                ให้เจ้าหน้าที่หรือผู้ที่ได้รับมอบหมายดำเนินการจัดซื้อจัดจ้างพัสดุไปก่อน แล้วรีบรายงาน ขอความเห็นชอบ
                พร้อมด้วยหลักฐานการจัดซื้อจัดจ้างนั้นเสนอต่อหัวหน้าหน่วยงานของรัฐภายใน 5 วันทำการถัดไป
              </div>
            </div>
            <InputArea class="lg:col-start-1 lg:col-span-3"
              label="รายละเอียดเพิ่มเติม เช่น ที่อยู่ใบเสร็จ เบอร์โทร อีเมล อื่นๆ" v-model="data.billDetail"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
          </div>

          <TitleHeader label="รายการพัสดุ">
            <template #action>
              <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                class="bg-white! hover:bg-red-50!" @click="pcm002DetailStore.addParcelToVendor(vendorIndex)"
                v-if="pcm002DetailStore.isEdit && menuStore.hasManage" />
            </template>
          </TitleHeader>

          <DataTable :value="data.vendorParcels" @row-reorder="(e) => onRowReorderFunction(e, vendorIndex)"
            class="mt-3">
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
                <p class=" w-full font-bold text-center">รายการ</p>
              </template>
              <template #body="{ data: row }">
                <InputField v-model="row.item" :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage"
                  rules="required" hide-details class="mb-5 mt-6" />
                <InputArea class="mt-14" label="รายละเอียด" v-model="row.itemDetail"
                  :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
              </template>
            </Column>

            <Column field="unitCode" body-class="bg-gray-100 align-top min-w-[400px]" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">จำนวน/หน่วย</p>
              </template>
              <template #body="{ data: row }">
                <div class="grid lg:grid-cols-2 gap-2">
                  <InputNumber class="mt-6" v-model="row.quantity"
                    :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details
                    inputClass="text-right" @onChange="() => calculateParcelPrice(row)" />
                  <Select class="mt-6" :options="pcm002DetailStore.unitOfMeasureDropDown" v-model="row.unitCode"
                    :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details />
                </div>
              </template>
            </Column>

            <Column field="unitPrice" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">ประเภทภาษี</p>
              </template>
              <template #body="{ data: row }">
                <Select class="mt-6" :options="pcm002DetailStore.vatTypeDropDown" v-model="row.vatIncludeTypeCode"
                  :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
              </template>
            </Column>

            <Column field="unitPrice" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">{{ data.vatIncludeTypeCode === 'VATType001' ?
                  'ราคา/หน่วย' : 'ราคา/หน่วย' }} </p>
              </template>
              <template #body="{ data: row }">
                <InputNumber class="mt-6" v-model="row.unitPrice" grouping :min-fraction-digits="2"
                  :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details
                  @onChange="() => calculateParcelPrice(row)" />
              </template>
            </Column>
            <Column field="totalPriceVat" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
              <template #header>
                <p class="w-full font-bold text-center">{{ data.vatIncludeTypeCode === 'VATType001' ? 'ราคารวม' :
                  'ราคารวม' }} </p>
              </template>
              <template #body="{ data: row }">
                <InputNumber class="mt-6" v-model="row.totalPriceVat" grouping :min-fraction-digits="2" rules="required"
                  hide-details @onChange="() => setTotalPrice(row)"
                  :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" />
              </template>
            </Column>

            <Column field="control" body-class="bg-gray-100 align-top" header-class="bg-gray-100"
              v-if="pcm002DetailStore.isEdit && menuStore.hasManage">
              <template #body="{ index }">
                <i class="pi pi-trash mt-4 text-red-600 cursor-pointer"
                  @click="() => pcm002DetailStore.removeParcelFromVendor(vendorIndex, index)" v-if="index !== 0" />
              </template>
            </Column>

            <Column rowReorder body-class="bg-gray-100 align-top pt-6" header-class="bg-gray-100 w-[3rem]"
              v-if="pcm002DetailStore.isEdit && menuStore.hasManage">
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
      </div>
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย">
        <template #action>
          <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="pcm002DetailStore.addGLAccount()"
            v-if="pcm002DetailStore.isEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>
      <DataTable :value="pcm002DetailStore.detail.glAccounts" :reorderable-columns="true"
        @row-reorder="onRowReOrderGlAccount">
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
            <Select :options="pcm002DetailStore.solIdDropDown" v-model="data.solId" hide-details
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="budgetTypeCode">
          <template #header>
            <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
          </template>
          <template #body="{ data, index }">
            <Select :options="pcm002DetailStore.budgetTypeDropDown" v-model="data.budgetTypeCode" hide-details
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required"
              @on-select="(e: string | undefined) => onChangeBudgetType(e, index)" />
          </template>
        </Column>
        <Column field="projectNumber">
          <template #header>
            <p class="w-full font-bold text-center min-w-[200px]">รหัสโครงการ</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.projectNumber" hide-details
              :disabled="!pcm002DetailStore.isEdit || data.budgetTypeCode === 'BudgetType001' || !menuStore.hasManage" />
          </template>
        </Column>
        <Column field="glAccountCode">
          <template #header>
            <p class="w-full font-bold text-center">รหัสบัญชี</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm002DetailStore.glAccountDropDown" v-model="data.glAccountCode" hide-details
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="amount">
          <template #header>
            <p class="w-full font-bold text-center min-w-[200px]">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <InputNumber v-model="data.amount" hide-details grouping :min-fraction-digits="2"
              :disabled="!pcm002DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="control" bodyStyle="vertical-align: top" v-if="pcm002DetailStore.isEdit && menuStore.hasManage">
          <template #body="{ index }">
            <i v-if="pcm002DetailStore.detail.glAccounts.length > 1"
              class="pi pi-trash mt-4 text-red-600 cursor-pointer"
              @click="() => pcm002DetailStore.removeGlAccount(index)" />
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
