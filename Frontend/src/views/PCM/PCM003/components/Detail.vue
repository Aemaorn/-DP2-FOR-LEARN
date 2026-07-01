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
import { usePcm003DetailStore } from '@/stores/PCM/pcm003';
import { defineAsyncComponent, ref, watch } from 'vue';
import { YearOptions } from '@/constants/date';
import type { Pcm003Vendor, Pcm003VendorParcels } from '@/models/PCM/pcm003';
import { showPartnerDialogAsync } from '@/helpers/dialog';
import { DataTable, type DataTableRowReorderEvent } from 'primevue';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import { DatatableHelper } from '@/helpers/datable';
import { formatCurrency } from '@/helpers/currency';
import { SupplyMethodCode, SupplyMethodSpecialTypeCode } from '@/enums/supplyMethod';

const PartnerDialog = defineAsyncComponent(() => import('./PartnerDialog.vue'));

const menuStore = useMenuStore();
const pcm003DetailStore = usePcm003DetailStore();
const { detail } = storeToRefs(pcm003DetailStore);
const showPartnerDialog = ref(false);
const { onRowReorder } = DatatableHelper();

const onShowPartnerDialogAsync = async (data: Pcm003Vendor) => {
  const res = await showPartnerDialogAsync();

  if (res) {
    data.suVendorId = res.id;
    data.vendorName = res.establishmentName;
    data.taxNumber = res.taxpayerIdentificationNo;
    data.vendorBranchNumber = res.sapBranchNumber;
  }
};

const isVendorType = (vendor: Pcm003Vendor, type: string) => {
  return vendor.vendorType === type;
};

const getTaxNumberLabel = (vendor: Pcm003Vendor) => {
  return vendor.vendorType === '0' ? 'เลขประจำตัวผู้เสียภาษี' : 'เลขประจำตัว ปปช. (ถ้ามี)';
};

const getTaxNumberRules = (vendor: Pcm003Vendor) => {
  return vendor.vendorType === '0'
    ? 'required'
    : '';
};

const onRowReorderFunction = (event: DataTableRowReorderEvent, mainIndex: number) => {
  detail.value.vendors[mainIndex].vendorParcels = onRowReorder(event)
};

watch(
  () => detail.value.supplyMethodCode,
  async (newValue) => {
    await pcm003DetailStore.getSupplyMethodSpecialTypeDDLAsync(newValue);

    const options = pcm003DetailStore.supplyMethidSpecialTypeDropdown;
    if (options.length === 0) return;

    pcm003DetailStore.detail.supplyMethodSpecialTypeCode =
      newValue === SupplyMethodCode.eighty
        ? SupplyMethodSpecialTypeCode.supplyMethod003
        : SupplyMethodSpecialTypeCode.specificMethod;

    if (newValue === SupplyMethodCode.eighty) {
      detail.value.procurementReasonItem1 = '';
      detail.value.procurementReasonItem2 = '';
    } else {
      detail.value.procurementReasonItem1 = 'โดยวิธี...............เนื่องจากเป็นการจัดซื้อจ้างสินค้า ที่มีวงเงินในการจัดซื้อจ้างครั้งหนึ่งไม่เกินวงเงิน 500,000 บาท ตามกฎกระทรวง (กำหนดวงเงินการจัดชื่อจัดจ้างพัสดโดยวิธีเฉพาะเจาะจง วงเงินการจัดซื้อจัดจ้าง ที่ไม่ทำข้อตกลงเป็นหนังสือและวงเงินการจัดซื้อจัดจ้างในการแต่งตั้งผู้ตรวจรับพัสดุ พ.ศ.2560)';
      detail.value.procurementReasonItem2 = 'ในการดำเนินการครั้งนี้มีความจำเป็นเร่งด่วนที่เกิดขึ้นโดยไม่ได้คาดหมายไว้และไม่อาจดำเนินการตามปกติได้ทัน จึงต้องจัดชื้อจัดจ้างเป็นการเร่งด่วน เพื่อบรรเทาความเดือดร้อนให้ผู้ประสบอุทกภัยได้ทันเวลา และเพื่อไม้ให้เกิดความเสียหายแก่ธนาคาร (เอกสารแนบ)';
    }
  },
  { immediate: true }
);

function calculateParcelPrice(parcel: Pcm003VendorParcels) {
  const quantity = Number(parcel.quantity || 0);
  const unitPrice = Number(parcel.unitPrice || 0);
  const totalPrice = quantity * unitPrice;
  parcel.totalPriceVat = totalPrice;
  parcel.totalPrice = totalPrice;
}

function setTotalPrice(parcel: Pcm003VendorParcels) {
  parcel.totalPrice = parcel.totalPriceVat;
}
</script>

<template>
  <Card>
    <template #content>
      <div class="grid lg:grid-cols-4 gap-2 gap-y-8 mt-6">
        <InputField label="เลขที่" v-model="detail.p79Clause2Number" disabled />
        <Datepicker label="วันที่" rules="required" v-model="detail.p79Clause2Date"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
        <InputField label="เบอร์โทร" v-model="detail.telephone" rules="required"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

        <Select class="lg:col-start-1" label="ฝ่าย/ภาค เขต" v-model="detail.departmentCode"
          :options="pcm003DetailStore.departmentDropdown" rules="required" disabled />
        <Select label="ปีงบประมาณ" v-model="detail.budgetYear" :options="YearOptions" rules="required"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

        <Select class="lg:col-start-1" label="วิธีการจัดหา" :options="pcm003DetailStore.supplyMethodDropdown"
          v-model="detail.supplyMethodCode" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
        <Select :options="pcm003DetailStore.supplyMethodTypeDropdown" v-model="detail.supplyMethodTypeCode"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" />
        <Select :options="pcm003DetailStore.supplyMethidSpecialTypeDropdown"
          v-model="detail.supplyMethodSpecialTypeCode" disabled />

        <InputArea class="lg:col-span-4" label="เรื่อง" rules="required" v-model="detail.subject"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
      </div>

      <p>กำหนดระยะเวลาให้งานจัดซื้อ/จัดจ้างแล้วเสร็จ <span class="text-red-500">*</span></p>
      <div class="grid lg:grid-cols-2 gap-2 mt-8">
        <Datepicker label="ภายในวันที่" v-model="detail.deliveryDate" rules="required"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
      </div>

      <template v-if="detail.supplyMethodCode === SupplyMethodCode.sixty">
        <div class="mb-4">
          <p class="font-bold">วิธีการจัดซื้อ/จัดจ้าง และเหตุผลที่ต้องการซื้อหรือจ้างโดยวิธีนี้</p>
        </div>
        <div class="grid grid-cols-1 mt-8">
          <InputArea v-model="detail.procurementReasonItem1"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <InputArea v-model="detail.procurementReasonItem2"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
        </div>
      </template>
    </template>
  </Card>

  <Card class="mt-4" v-if="!pcm003DetailStore.isBranchOrZoneDepartment">
    <template #content>
      <TitleHeader label="ส่วนงาน จพ. รับผิดชอบ" />
      <div class="grid lg:grid-cols-4 gap-2 mt-10">
        <Select label="ส่วนงาน จพ." v-model="pcm003DetailStore.detail.assignSegmentCode" rules="required"
          :options="pcm003DetailStore.assignDepartmentDDL" v-if="pcm003DetailStore.assignDepartmentDDL"
          :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
      </div>
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="รายละเอียดการโอนเข้าบัญชีผู้สำรองจ่าย" />
      <div class="px-2">
        <Radio
          :options="[{ label: 'จ่ายเงินคืนผู้สำรองจ่าย', value: true }, { label: 'ชำระเงินให้ผู้ประกอบการ', value: false }]"
          v-model="pcm003DetailStore.detail.isAdvance" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

        <div class="grid grid-cols-4 gap-y-8 gap-4 mt-6">
          <InputField v-if="detail.isAdvance" class="lg:col-span-2" label="ผู้สำรองจ่าย"
            v-model="detail.advance.advanceName" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <Select class="lg:col-start-1" label="ช่องทางชำระเงิน" :options="pcm003DetailStore.paymentMethodDropDown"
            v-model="detail.advance.advancePaymentMethodCode" rules="required"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

          <Select v-if="detail.advance.advancePaymentMethodCode == 'PaymentMethod002'" class="lg:col-start-1"
            label="ธนาคาร" :options="pcm003DetailStore.bankDropDown" v-model="detail.advance.advanceBankCode"
            rules="required" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <InputField v-if="detail.advance.advancePaymentMethodCode == 'PaymentMethod002'" label="เลขที่บัญชี"
            v-model="detail.advance.advanceBankAccount" rules="required"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <InputField v-if="detail.advance.advancePaymentMethodCode == 'PaymentMethod002'" label="สาขา"
            v-model="detail.advance.advanceBankBranch" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

          <InputArea class="lg:col-span-4" label="รายละเอียดเพิ่มเติม"
            v-model="pcm003DetailStore.detail.advance.advanceDetail"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
        </div>
      </div>
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <TitleHeader label="ผู้ประกอบการ">
        <template #action>
          <Button label="เพิ่มคู่ค้า" icon="pi pi-plus" severity="primary" variant="outlined"
            class="bg-white! hover:bg-red-50!" @click="pcm003DetailStore.addVendors"
            v-if="pcm003DetailStore.isEdit && menuStore.hasManage" />
        </template>
      </TitleHeader>

      <div v-for="(data, vendorIndex) in pcm003DetailStore.detail.vendors" :key="data.id" class="mb-5">
        <div class="flex items-center justify-between gap-4 px-2">
          <Radio :options="[{ label: 'นิติบุคคล', value: '0' }, { label: 'บุคคลธรรมดา', value: '1' }]"
            v-model="data.vendorType" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

          <i class="pi pi-trash text-red-600 cursor-pointer pb-5"
            v-if="pcm003DetailStore.isEdit && menuStore.hasManage && data.sequence !== 1"
            @click="() => pcm003DetailStore.removeVendorList(vendorIndex)" />
        </div>

        <div class="grid lg:grid-cols-4 gap-y-8 mt-4 gap-2">
          <InputField v-if="isVendorType(data, '0')" label="ผู้ค้า" v-model="data.vendorName" rules="required"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage">
            <template #appendAction>
              <InputGroupAddon>
                <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! w-full h-full"
                  @click="onShowPartnerDialogAsync(data)" v-if="menuStore.hasManage" />
              </InputGroupAddon>
            </template>
          </InputField>

          <InputField v-if="isVendorType(data, '1')" label="ผู้ค้า" v-model="data.vendorName"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" />

          <InputField :label="getTaxNumberLabel(data)" v-model="data.taxNumber" :rules="getTaxNumberRules(data)"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <InputField v-if="isVendorType(data, '0')" label="เลขที่สาขา" v-model="data.vendorBranchNumber"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

          <Select class="col-start-1" label="ประเภทเอกสาร" :options="pcm003DetailStore.invoiceDocumentTypeDropDown"
            v-model="data.billTypeCode" rules="required"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <InputField v-if="data.billTypeCode === 'InvoiceDocType005'" label="ระบุ ประเภทเอกสารอื่นๆ"
            v-model="data.billTypeOther" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <InputField label="เลขที่เอกสาร" v-model="data.billBookNo"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
          <Datepicker label="วันที่เอกสาร" v-model="data.billDate" rules="required"
            :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />

          <InputArea class="lg:col-span-4" label="รายละเอียดเพิ่มเติม เช่น ที่อยู่ใบเสร็จ เบอร์โทร อีเมล อื่นๆ"
            v-model="data.billDetail" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
        </div>

        <TitleHeader label="รายการพัสดุ">
          <template #action>
            <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
              class="bg-white! hover:bg-red-50!" @click="pcm003DetailStore.addParcelToVendor(vendorIndex)"
              v-if="pcm003DetailStore.isEdit && menuStore.hasManage" />
          </template>
        </TitleHeader>

        <DataTable :value="data.vendorParcels" @row-reorder="(e) => onRowReorderFunction(e, vendorIndex)" class="mt-4">
          <Column body-class="bg-gray-100 align-top" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">ลำดับ</p>
            </template>
            <template #body="{ data: row }">
              <p class="text-center mt-6">{{ row.sequence }}</p>
            </template>
          </Column>

          <Column body-class="bg-gray-100 align-top min-w-[300px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">รายการ</p>
            </template>
            <template #body="{ data: row }">
              <InputField v-model="row.item" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage"
                rules="required" hide-details class="mb-9 mt-6" />
              <InputArea label="รายละเอียด" v-model="row.itemDetail"
                :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
            </template>
          </Column>

          <Column field="unitCode" body-class="bg-gray-100 align-top min-w-[400px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">จำนวน/หน่วย</p>
            </template>
            <template #body="{ data: row }">
              <div class="grid lg:grid-cols-2 gap-2 mt-6">
                <InputNumber v-model="row.quantity" :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage"
                  rules="required" hide-details inputClass="text-right" @onChange="() => calculateParcelPrice(row)" />
                <Select :options="pcm003DetailStore.unitOfMeasureDropDown" v-model="row.unitCode"
                  :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details />
              </div>
            </template>
          </Column>

          <Column field="unitPrice" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">ประเภทภาษี</p>
            </template>
            <template #body="{ data: row }">
              <Select class="mt-6" :options="pcm003DetailStore.vatTypeDropDown" v-model="row.vatIncludeTypeCode"
                :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" />
            </template>
          </Column>

          <Column field="unitPrice" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">{{ data.vatIncludeTypeCode === 'VATType001' ?
                'ราคา/หน่วย' : 'ราคา/หน่วย' }} </p>
            </template>
            <template #body="{ data: row }">
              <InputNumber class="mt-6" v-model="row.unitPrice" grouping :min-fraction-digits="2"
                :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" hide-details
                @onChange="() => calculateParcelPrice(row)" />
            </template>
          </Column>

          <Column field="totalPriceVat" body-class="bg-gray-100 align-top min-w-[200px]" header-class="bg-gray-100">
            <template #header>
              <p class="w-full font-bold text-center">
                {{ data.vatIncludeTypeCode === 'VATType001' ? 'ราคารวม' : 'ราคารวม' }}
              </p>
            </template>
            <template #body="{ data: row }">
              <InputNumber class="mt-6" v-model="row.totalPriceVat" grouping :min-fraction-digits="2" rules="required"
                hide-details @onChange="() => setTotalPrice(row)" />
            </template>
          </Column>

          <Column field="control" body-class="bg-gray-100 align-top" header-class="bg-gray-100"
            v-if="pcm003DetailStore.isEdit && menuStore.hasManage">
            <template #body="{ data, index }">
              <i class="pi pi-trash mt-10 text-red-600 cursor-pointer" v-if="data.sequence !== 1"
                @click="() => pcm003DetailStore.removeParcelFromVendor(vendorIndex, index)" />
            </template>
          </Column>

          <Column rowReorder body-class="bg-gray-100 align-top pt-6" header-class="bg-gray-100 w-[3rem]"
            v-if="pcm003DetailStore.isEdit && menuStore.hasManage">
            <template #rowreordericon>
              <span class="material-symbols-outlined cursor-pointer mt-6" :draggable="true">
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
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <div class="flex items-center gap-6">
        <TitleHeader label="ข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย">
          <template #action>
            <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
              class="bg-white! hover:bg-red-50!" @click="pcm003DetailStore.addGLAccount()"
              v-if="pcm003DetailStore.isEdit && menuStore.hasManage" />
          </template>
        </TitleHeader>
      </div>
      <DataTable :value="pcm003DetailStore.detail.glAccounts">
        <Column field="sequence" bodyStyle="vertical-align: center" class="min-w-[50px]">
          <template #header>
            <p class="w-full font-bold text-center">ลำดับ</p>
          </template>
          <template #body="{ data }">
            <p class="text-center">
              {{ data.sequence }}
            </p>
          </template>
        </Column>
        <Column field="solId" class="min-w-[200px]">
          <template #header>
            <p class="w-full font-bold text-center">ศูนย์ต้นทุน</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm003DetailStore.solIdDropDown" v-model="data.solId" hide-details
              :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="budgetTypeCode" class="min-w-[200px]">
          <template #header>
            <p class="w-full font-bold text-center">ประเภทงบประมาณ</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm003DetailStore.budgetTypeDropDown" v-model="data.budgetTypeCode" hide-details
              :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="projectNumber" class="min-w-[200px]">
          <template #header>
            <p class="w-full font-bold text-center">รหัสโครงการ</p>
          </template>
          <template #body="{ data }">
            <InputField v-model="data.projectNumber" hide-details
              :disabled="!pcm003DetailStore.isEdit || data.budgetTypeCode === 'BudgetType001' || !menuStore.hasManage" />
          </template>
        </Column>
        <Column field="glAccountCode">
          <template #header>
            <p class="w-full font-bold text-center">รหัสบัญชี</p>
          </template>
          <template #body="{ data }">
            <Select :options="pcm003DetailStore.glAccountDropDown" v-model="data.glAccountCode" hide-details
              :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="amount" class="min-w-[150px]">
          <template #header>
            <p class="w-full font-bold text-center">จำนวนเงิน</p>
          </template>
          <template #body="{ data }">
            <InputNumber v-model="data.amount" grouping :min-fraction-digits="2" hide-details
              :disabled="!pcm003DetailStore.isEdit || !menuStore.hasManage" rules="required" />
          </template>
        </Column>
        <Column field="control" bodtClass="align-top" v-if="pcm003DetailStore.isEdit && menuStore.hasManage">
          <template #body="{ index }">
            <i v-if="pcm003DetailStore.detail.glAccounts.length > 1"
              class="pi pi-trash mt-4 text-red-600 cursor-pointer"
              @click="() => pcm003DetailStore.removeGlAccount(index)" />
          </template>
        </Column>
      </DataTable>
    </template>
  </Card>
  <PartnerDialog v-model:show="showPartnerDialog" />
</template>

<style scoped lang="scss">
:deep(.p-datatable-column-title) {
  font-size: 20px;
}
</style>