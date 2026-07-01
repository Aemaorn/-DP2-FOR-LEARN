<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, InputNumber, Datepicker, InputArea } from '@/components/forms';
import type { Cam01PoAddendumBody } from '@/models/CAM/CAM01/cam01.poAddendum';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import { ArrayHelper } from '@/helpers/array';
import { showPartnerDialogAsync } from '@/helpers/dialog';
import { useCam01PoAddendumStore } from '@/stores/CAM/CAM01/PO/cam01.poAddendum';
import { storeToRefs } from 'pinia';
import type { Cam01PoPayment } from '@/models/CAM/CAM01/cam01';
import draggable from 'vuedraggable';

const { addSequence, reSequence, deleteItemAndReSequence } = ArrayHelper();

const store = useCam01PoAddendumStore();
const { isCanEdit } = storeToRefs(store);

const value = defineModel<Cam01PoAddendumBody>({
  required: true,
});

const addPaymentItems = () => {
  value.value.newPaymentTerms = addSequence(value.value.newPaymentTerms, {
    paymentTermNo: value.value.newPaymentTerms.length + 1,
  } as Cam01PoPayment);
};

const deletePaymentItem = (index: number) => {
  value.value.newPaymentTerms = deleteItemAndReSequence(value.value.newPaymentTerms, index);
};

const onReSequence = (): void => {
  value.value.newPaymentTerms = reSequence(value.value.newPaymentTerms);

  value.value.newPaymentTerms.forEach((e, i) => {
    e.paymentTermNo = i + 1;
  });
};

const onSelectedVendor = async () => {
  const resp = await showPartnerDialogAsync();

  if (resp) {
    value.value.newContract.vendorId = resp.id;
    value.value.newContract.vendorName = resp.establishmentName;
  }
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="รายละเอียดแก้ไขสัญญา" />
      <div class="my-2">
        <div class="bg-gray-50 py-2 px-4">
          <TitleHeader label="(จากเดิม)" :hidden-icon="true" />
          <div class="grid lg:grid-cols-3 gap-2">
            <InputField label="เลขที่สัญญา จพ.(สบส.)" v-model="value.oldContract.contractNo" disabled />
            <InputField label="คู่สัญญา" v-model="value.oldContract.vendorName" disabled />
            <InputField label="รหัสสาขา (SAP) ตัวอย่าง 0000, 0001" v-model="value.oldContract.sapNumber" disabled />
            <InputField label="เลขที่สัญญา PO (SAP)" v-model="value.oldContract.poNumber" disabled />
          </div>
          <DataTable :value="value.oldPaymentTerms" stripedRows tableStyle="min-width: 50rem">
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">งวด</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ data.sequence }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">รายการพัสดุ</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ data.description }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">ระยะเวลา (วัน)</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ `${data.leadTime} วัน` }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">วันที่ต้องส่งมอบ</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ ToDateOnly(data.deliveryDate) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">ร้อยละ</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ data.installmentPercentage }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนเงิน</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ formatCurrency(data.amount) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">หักเงินล่วงหน้า</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ formatCurrency(data.advanceDeductionAmount) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" header-class="bg-gray-50" body-class="bg-gray-50">
              <template #header>
                <p class="w-full font-bold text-center">หักเงินประกันผลงาน</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ formatCurrency(data.performanceDeductionAmount) }}</p>
              </template>
            </Column>
          </DataTable>
        </div>

        <div class="mt-2 bg-gray-50 py-2 px-4">
          <TitleHeader label="(แก้ไขเป็น)" :hidden-icon="true" />
          <div class="grid lg:grid-cols-3 gap-2">
            <InputField label="เลขที่สัญญา จพ.(สบส.)" v-model="value.newContract.contractNo" disabled />
            <InputField label="คู่สัญญา" v-model="value.newContract.vendorName" :disabled="!isCanEdit">
              <template #appendAction v-if="isCanEdit">
                <Button class="rounded-l-none bg-gray-300 border-gray-200 text-black" label="ค้นหา"
                  @click="onSelectedVendor" />
              </template>
            </InputField>
            <InputField label="รหัสสาขา (SAP) ตัวอย่าง 0000, 0001" v-model="value.newContract.sapNumber"
              rules="required" :disabled="!isCanEdit" />
          </div>
          <TitleHeader label="โดยมีรายละเอียดการมอบ ดังนี้" :hidden-icon="true">
            <template #action>
              <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                class="bg-white! hover:bg-red-50!" @click="addPaymentItems" v-if="isCanEdit" />
            </template>
          </TitleHeader>

          <div class="p-datatable-table-container overflow-x-auto">
            <div class="p-datatable-table p-datatable-striped min-w-[50rem]">
              <div
                class="grid grid-cols-9 gap-2 sticky top-0 p-datatable-thead text-primary items-center text-center font-bold">
                <p>งวด</p>
                <p>รายการพัสดุ</p>
                <p>ระยะเวลา (วัน)</p>
                <p>วันที่ต้องส่งมอบ</p>
                <p>ร้อยละ</p>
                <p>จำนวนเงิน</p>
                <p>หักเงินล่วงหน้า</p>
                <p>หักเงินประกันผลงาน</p>
                <p></p>
              </div>
              <Divider />
              <draggable v-model="value.newPaymentTerms" tag="div" handle=".drag-handle" itemKey="sequence"
                @end="onReSequence">
                <template #item="{ element: data, index }: { element: Cam01PoPayment, index: number }">
                  <div>
                    <div class="grid grid-cols-9 gap-2 p-2 items-start">
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.paymentTermNo" rules="required" :disabled="!isCanEdit"
                          class="text-center" inputClass="text-center" />
                      </div>
                      <div class="pt-2 px-2">
                        <InputField v-model="data.title" rules="required" :disabled="!isCanEdit" />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.leadTime" rules="required" :disabled="!isCanEdit"
                          input-class="text-center" />
                      </div>
                      <div class="pt-2 px-2">
                        <Datepicker v-model="data.deliveryDate" rules="required" :disabled="!isCanEdit" />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.installmentPercentage" rules="required" input-class="text-center"
                          :disabled="!isCanEdit" :min-fraction-digits="2" :max-fraction-digits="3" grouping />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.amount" :min-fraction-digits="2" grouping rules="required"
                          :disabled="!isCanEdit" />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.advanceDeductionAmount" :min-fraction-digits="2" grouping
                          rules="required" :disabled="!isCanEdit" />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.performanceDeductionAmount" :min-fraction-digits="2" grouping
                          rules="required" :disabled="!isCanEdit" />
                      </div>
                      <div class="pt-2 px-2">
                        <div class="flex justify-end items-center gap-6" v-if="isCanEdit">
                          <Button icon="pi pi-trash" severity="danger" variant="text"
                            class="h-fit w-fit m-0 mt-0 mb-0 pt-0 pb-0" @click="() => deletePaymentItem(index)" />
                          <span class="material-symbols-outlined mt-0 cursor-move drag-handle">
                            drag_indicator
                          </span>
                        </div>
                      </div>
                    </div>

                    <div class="grid grid-cols-9 gap-2 px-2">
                      <div></div>
                      <div class="col-span-7">
                        <InputArea v-model="data.description" rules="required" :disabled="!isCanEdit" />
                      </div>
                    </div>
                    <Divider />
                  </div>
                </template>
              </draggable>
            </div>
          </div>

        </div>
      </div>
    </template>
  </Card>
</template>