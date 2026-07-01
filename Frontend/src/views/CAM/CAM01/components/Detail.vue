<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { ButtonSave } from '@/components/Button';
import { InputField, InputArea, Radio } from '@/components/forms';
import { Form } from 'vee-validate';
import type { Option } from '@/models/shared/option';
import { computed, defineAsyncComponent, ref } from 'vue';
import { Cam01Type } from '@/enums/CAM/CAM01/cam01';
import { CAM01Helper } from '@/helpers/CAM/cam01';
import { useRoute } from 'vue-router';
import ContractDialog from './ContractDialog.vue';
import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { storeToRefs } from 'pinia';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import type { Cam001ContractDialog } from '@/models/CAM/CAM01/cam01';
import ToastHelper from '@/helpers/toast';

const route = useRoute();
const { ContractTypeName } = CAM01Helper;
const store = useCam01DetailStore();
const { body } = storeToRefs(store);
const { onSubmitAsync } = store;

const id = computed<string | undefined>(() => route.params.id as string | undefined);

const isShowDialog = ref(false);

const onSelectedContract = (item: Cam001ContractDialog) => {
  body.value.contractInfo = item;
  body.value.contractDraftVendorId = item.contractDraftVendorId;
};

const contractOption = ref<Array<Option>>(Object.entries(Cam01Type)
  .filter(([, value]) => value != Cam01Type.ChangeContractDetails)
  .map(([, value]) => ({ label: ContractTypeName(value), value })));

const POPrograms = defineAsyncComponent(() => import('@/views/CAM/CAM01/components/PO/POPrograms.vue'));
const FineProgram = defineAsyncComponent(() => import('@/views/CAM/CAM01/components/Fine/FineProgram.vue'));
const AdjustContractProgram = defineAsyncComponent(() => import('@/views/CAM/CAM01/components/AdjustContract/AdjustContractProgram.vue'));
</script>

<template>
  <Form @submit="onSubmitAsync" @invalid-submit="() => ToastHelper.invalidMessageToast()">
    <Card class="mb-4">
      <template #content>
        <TitleHeader label="ข้อมูลสัญญา">
          <template #action>
            <ButtonSave type="submit" v-if="!id" />
          </template>
        </TitleHeader>
        <div class="px-2">
          <div class="grid grid-cols-3 gap-2">
            <InputField
              :model-value="`${body.contractInfo != null ? `${body.contractInfo?.contractName} : ${body.contractInfo?.contractName}` : ''}`"
              readonly>
              <template #appendAction v-if="!id">
                <Button class="rounded-l-none bg-gray-300 border-gray-200 text-black" label="ค้นหา"
                  @click="() => isShowDialog = true" />
              </template>
            </InputField>
          </div>

          <div class="grid lg:grid-cols-3 gap-2" v-if="body.contractInfo">
            <InfoItem class="lg:col-start-1" title="คู่ค้า"
              :content="`${body.contractInfo.entrepreneurCode} : ${body.contractInfo.entrepreneurName}`" />
            <InfoItem title="Email" :content="body.contractInfo.entrepreneurEmail ?? '-'" />

            <InfoItem class="lg:col-start-1" title="เลขที่สัญญา" :content="body.contractInfo.contractNumber" />
            <InfoItem title="เลขที่ PO (SAP)" :content="body.contractInfo.poNumber" />
            <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(body.contractInfo.budget)" />

            <InfoItem title="ชื่อสัญญา" :content="body.contractInfo.contractName" />
            <InfoItem title="ประเภทสัญญา" :content="body.contractInfo.contractTypeLabel ?? '-'" />
            <InfoItem title="รูปแบบสัญญา" :content="body.contractInfo.contractTemplate ?? '-'" />

            <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(body.contractInfo.contractSignedDate)" />
            <InfoItem title="กำหนดส่งมอบภายใน"
              :content="`${body.contractInfo.deliveryLeadTime ? `${body.contractInfo.deliveryLeadTime} วัน` : '-'}`" />
            <InfoItem title="ครบกำหนดส่งมอบงาน วันที่"
              :content="`${body.contractInfo.deliveryDate ? ToDateOnly(body.contractInfo.deliveryDate) : '-'}`" />

            <InfoItem title="ระยะเวลารับประกัน" :content="body.contractInfo.deliveryLeadTimeTypeLabel ?? '-'" />
          </div>
        </div>
      </template>
    </Card>

    <Card class="mb-4" v-if="body.contractInfo">
      <template #content>
        <TitleHeader label="ข้อมูลแก้ไขสัญญา" />
        <div class="grid grid-cols-3 gap-2">
          <Radio :options="contractOption" v-model="body.type" vertical rules="required" :disabled="!!id" />
          <InputArea class="lg:col-span-3" label="เหตุผล" v-model="body.remark" rules="required" :disabled="!!id" />
        </div>
      </template>
    </Card>

    <div class="my-2" v-if="id">
      <POPrograms v-if="body.currentStep && body.type === Cam01Type.AppendNewPurchaseOrder" v-model="body.currentStep"
        :steps="body.steps" />
      <FineProgram v-if="body.type === Cam01Type.WaiveOrReducePenalty" />
      <AdjustContractProgram v-if="body.type === Cam01Type.AdjustContractDuration" />
    </div>
    <ContractDialog v-model="isShowDialog" @on-selected="onSelectedContract" />
  </Form>
</template>