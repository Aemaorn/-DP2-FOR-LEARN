<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { Select, InputNumber, Radio, Datepicker, InputArea } from '@/components/forms';
import { useCam01AdjustContractStore } from '@/stores/CAM/CAM01/AdjustContract/cam01.adjustContract';
import type { Cam01AdjustContractBody, Cam01AdjustContractPayment } from '@/models/CAM/CAM01/cam.adjustContract';
import { storeToRefs } from 'pinia';
import draggable from 'vuedraggable';
import { ref } from 'vue';

const store = useCam01AdjustContractStore();
const { options, isCanEdit } = storeToRefs(store);

const value = defineModel<Cam01AdjustContractBody>({
  required: true,
});

const radioType = ref([{ label: 'ขยายระยะเวลาสัญญา', value: "Extend" }, { label: 'ลดระยะเวลาสัญญา', value: "Reduce" }]);
</script>

<template>
  <Card>
    <template #content>
      <TitleHeader label="รายละเอียดแก้ไขสัญญา" />
      <div class="bg-gray-50 py-2 px-4">
        <TitleHeader label="(จากเดิม)" :hidden-icon="true" />
        <div class="grid lg:grid-cols-3 gap-2 mt-5">
          <Datepicker label="วันที่เริ่มปฎิบัติงาน" v-model="value.adjustContractDurationOld.workStartDate" disabled />
          <Datepicker label="ถึงวันที่" v-model="value.adjustContractDurationOld.newEndDate" disabled />
          <Select class="lg:col-start-1" label="ประเภทการจ่ายเงิน" :options="options.paymentType"
            v-model="value.adjustContractDurationOld.paymentTypeCode" disabled />
        </div>

        <div class="p-datatable-table-container overflow-x-auto">
          <table class="p-datatable-table p-datatable-striped min-w-[50rem]">
            <thead class="p-datatable-thead sticky">
              <tr>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">งวด</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">ระยะเวลา (วัน)</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">วันที่ต้องส่งมอบ</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">ร้อยละ</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">จำนวนเงิน</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">หักเงินล่วงหน้า</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">หักเงินประกันผลงาน</p>
                </th>
              </tr>
            </thead>
            <draggable v-model="value.adjustContractDurationOld.paymentTerms" tag="tbody" handle=".drag-term"
              item-key="sequence">
              <template #item="{ element: data }: { element: Cam01AdjustContractPayment }">
                <template style="display: contents;">
                  <tr>
                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.paymentTermNo" disabled class="text-center" inputClass="text-center" />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.leadTime" disabled input-class="text-center" />
                    </td>

                    <td class="pt-2 px-2">
                      <Datepicker v-model="data.deliveryDate" disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.installmentPercent" :min-fraction-digits="2" :max-fraction-digits="3" grouping
                        input-class="text-center" disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.amount" :min-fraction-digits="2" grouping disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.advanceDeductionAmount" :min-fraction-digits="2" grouping disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.performanceDeductionAmount" :min-fraction-digits="2" grouping disabled />
                    </td>
                  </tr>
                  <tr>
                    <td colspan="7">
                      <InputArea label="รายละเอียดการส่งมอบงาน" v-model="data.description" disabled />
                    </td>
                  </tr>
                </template>
              </template>
            </draggable>
          </table>
        </div>
      </div>

      <div class="mt-2 bg-gray-50 py-2 px-4">
        <TitleHeader label="(แก้ไขเป็น)" :hidden-icon="true" />
        <div class="grid lg:grid-cols-3 gap-2">
          <Radio :options="radioType" v-model="value.adjustContractDurationNew.changeType" rules="required"
            :disabled="!isCanEdit" />
          <Datepicker class="lg:col-start-1 lg:mt-5" label="วันที่เริ่มปฎิบัติงาน"
            v-model="value.adjustContractDurationNew.workStartDate" :disabled="!isCanEdit" rules="required" />
          <Datepicker class="lg:mt-5" label="ถึงวันที่" v-model="value.adjustContractDurationNew.newEndDate"
            :disabled="!isCanEdit" rules="required" />
          <Select class="lg:col-start-1" label="ประเภทการจ่ายเงิน" :options="options.paymentType"
            v-model="value.adjustContractDurationNew.paymentTypeCode" disabled />
        </div>

        <div class="p-datatable-table-container overflow-x-auto">
          <table class="p-datatable-table p-datatable-striped min-w-[50rem]">
            <thead class="p-datatable-thead sticky">
              <tr>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">งวด</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">ระยะเวลา (วัน)</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">วันที่ต้องส่งมอบ</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">ร้อยละ</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">จำนวนเงิน</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">หักเงินล่วงหน้า</p>
                </th>
                <th class="p-datatable-header-cell bg-gray-50 text-primary" scope="col">
                  <p class="w-full font-bold text-center">หักเงินประกันผลงาน</p>
                </th>
              </tr>
            </thead>
            <draggable v-model="value.adjustContractDurationNew.paymentTerms" tag="tbody" handle=".drag-term"
              item-key="sequence">
              <template #item="{ element: data }: { element: Cam01AdjustContractPayment }">
                <template style="display: contents;">
                  <tr>
                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.paymentTermNo" disabled class="text-center" inputClass="text-center" />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.leadTime" rules="required" :disabled="!isCanEdit"
                        input-class="text-center" />
                    </td>

                    <td class="pt-2 px-2">
                      <Datepicker v-model="data.deliveryDate" rules="required" :disabled="!isCanEdit" />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.installmentPercent" :min-fraction-digits="2" :max-fraction-digits="3" grouping rules="required"
                        input-class="text-center" disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.amount" :min-fraction-digits="2" grouping disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.advanceDeductionAmount" :min-fraction-digits="2" grouping disabled />
                    </td>

                    <td class="pt-2 px-2">
                      <InputNumber v-model="data.performanceDeductionAmount" :min-fraction-digits="2" grouping disabled />
                    </td>
                  </tr>
                  <tr>
                    <td colspan="7">
                      <InputArea label="รายละเอียดการส่งมอบงาน" v-model="data.description" disabled />
                    </td>
                  </tr>
                </template>
              </template>
            </draggable>
          </table>
        </div>
      </div>
    </template>
  </Card>
</template>