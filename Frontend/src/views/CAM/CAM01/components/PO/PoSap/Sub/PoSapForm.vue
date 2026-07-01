<script setup lang="ts">
import { InputField, Datepicker, InputArea } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { Cam01PoPayment } from '@/models/CAM/CAM01/cam01';
import type { Cam01PoSapBody } from '@/models/CAM/CAM01/cam01.poSap';
import draggable from 'vuedraggable';
import { useCam01PoSapStore } from '@/stores/CAM/CAM01/PO/cam01.poSap';
import { storeToRefs } from 'pinia';


const store = useCam01PoSapStore();
const { isCanEdit } = storeToRefs(store);

const value = defineModel<Cam01PoSapBody>({
  required: true,
});
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
          <DataTable :value="value.oldPaymentTerms">
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">งวด</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ data.sequence }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">รายการพัสดุ</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ data.description }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ระยะเวลา (วัน)</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ `${data.leadTime} วัน` }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">วันที่ต้องส่งมอบ</p>
              </template>
              <template #body="{ data }">
                <p class="text-center">{{ ToDateOnly(data.deliveryDate) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">ร้อยละ</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ data.installmentPercentage }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">จำนวนเงิน</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ formatCurrency(data.amount) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">หักเงินล่วงหน้า</p>
              </template>
              <template #body="{ data }">
                <p class="text-end">{{ formatCurrency(data.advanceDeductionAmount) }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
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
            <InputField label="คู่สัญญา" v-model="value.newContract.vendorName" disabled />
            <InputField label="รหัสสาขา (SAP) ตัวอย่าง 0000, 0001" v-model="value.newContract.sapNumber"
              rules="required" disabled />
            <InputField label="เลขที่สัญญา PO (SAP)" v-model="value.newContract.poNumber" rules="required"
              :disabled="!isCanEdit" />
          </div>

          <div class="p-datatable-table-container overflow-auto">
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

              </div>
              <Divider />

              <draggable v-model="value.paymentTerms" tag="div" handle=".drag-handle" itemKey="sequence">
                <template #item="{ element: data }: { element: Cam01PoPayment, index: number }">
                  <div>
                    <div class="grid grid-cols-8 gap-2 p-2 items-start">
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.paymentTermNo" rules="required" disabled class="text-center"
                          inputClass="text-center" />
                      </div>
                      <div class="pt-2 px-2">
                        <InputField v-model="data.title" rules="required" disabled />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.leadTime" rules="required" disabled input-class="text-center" />
                      </div>
                      <div class="pt-2 px-2">
                        <Datepicker v-model="data.deliveryDate" rules="required" disabled />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.installmentPercentage" :min-fraction-digits="2" :max-fraction-digits="3" grouping rules="required"
                          input-class="text-center" disabled />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.amount" :min-fraction-digits="2" grouping rules="required" disabled />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.advanceDeductionAmount" :min-fraction-digits="2" grouping
                          rules="required" disabled />
                      </div>
                      <div class="pt-2 px-2">
                        <InputNumber v-model="data.performanceDeductionAmount" :min-fraction-digits="2" grouping
                          rules="required" disabled />
                      </div>

                    </div>

                    <div class="grid grid-cols-8 gap-2 px-2">
                      <div></div>
                      <div class="col-span-7">
                        <InputArea v-model="data.description" rules="required" disabled />
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