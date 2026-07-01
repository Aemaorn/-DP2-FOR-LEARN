<script setup lang="ts">
import type { Entrepreneurs, EntrepreneursPriceDetail, EntrepreneursPriceDetailBody } from '@/models/PCM/PCM005/principleApprovalRental';
import { Dialog, Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { InputArea, InputField, Select, InputNumber } from '@/components/forms';
import { ButtonSave } from '@/components/Button';
import { Form } from 'vee-validate';
import { computed, ref, watch } from 'vue';
import { VendorConstants } from '@/constants';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { ArrayHelper } from '@/helpers/array';
import { formatCurrency } from '@/helpers/currency';
import { useMenuStore } from '@/stores/menu';
import ToastHelper from '@/helpers/toast';

const show = defineModel('show', { type: Boolean, required: true, default: false });

const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();
const { deleteItemAndReSequence, reSequence } = ArrayHelper();
const { typeNameByCode, nationalityNameBycode } = VendorConstants;

const body = defineModel<Entrepreneurs>('vendor', { required: true });
const oldData = ref<Entrepreneurs>({} as Entrepreneurs);

const sumOfferedPrice = computed(() => {
  if ((body.value.details?.length ?? 0) === 0) return 0;

  const result = body.value.details.reduce((sum, detail) => {
    return sum + ((detail.parcelQuantity ?? 0) * (detail.offeredPrice ?? 0));
  }, 0);

  return result;
});

const sumAgreedPrice = computed(() => {
  if ((body.value.details?.length ?? 0) === 0) return 0;

  const result = body.value.details.reduce((sum, detail) => {
    return sum + ((detail.parcelQuantity ?? 0) * (detail.agreedPrice ?? 0));
  }, 0);

  return result;
});

const calculatePercentage = computed(() => {
  if (sumAgreedPrice.value > sumOfferedPrice.value) {
    const result = sumAgreedPrice.value - sumOfferedPrice.value;
    const percentage = (result * 100) / sumAgreedPrice.value;

    return `สูงกว่าราคาที่เสนอ ${formatCurrency(result)} คิดเป็นร้อยละ ${formatCurrency(percentage)}%`;
  }

  const result = sumOfferedPrice.value - sumAgreedPrice.value;
  const percentage = (result * 100) / sumOfferedPrice.value;

  return `ต่ำกว่าราคาที่เสนอ ${formatCurrency(result)} คิดเป็นร้อยละ ${formatCurrency(percentage)}%`;
});

const onSubmitAsync = async (): Promise<void> => {
  if (body.value.id) {
    const priceDetailBody: EntrepreneursPriceDetailBody = {
      ...body.value,
      id: body.value.id!,
      entrepreneursPriceDetails: body.value.details,
    };
    const success = await store.createPriceDetailAsync(body.value.id, priceDetailBody);
    if (success) show.value = false;
  } else {
    const success = await store.createEntrepreneurApiAsync(body.value);
    if (success) show.value = false;
  }
};

const onClose = (): void => {
  show.value = false;
};

const onAddPriceDetail = (): void => {
  if (!body.value || body.value.details.length === 0) {
    body.value.details = [{ sequence: 1 }] as EntrepreneursPriceDetail[];

    return;
  }

  body.value.details.push({
    sequence: body.value.details.length + 1,
  } as EntrepreneursPriceDetail);
};

const removePriceDetail = (index: number): void => {
  body.value.details = deleteItemAndReSequence(body.value.details, index);
};

const onReorderPriceDetail = (data: EntrepreneursPriceDetail[]) => {
  body.value.details = reSequence(data);
};

watch(() => show.value, (newValue) => {
  if (newValue) {
    oldData.value = { ...body.value };
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '90vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" class="p-5 overflow-auto">
        <TitleHeader label="ข้อมูลคู่ค้า">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="onClose"></i>
          </template>
        </TitleHeader>
        <div class="m-10">
          <div class="grid lg:grid-cols-3">
            <InfoItem title="สัญชาติของผู้ประกอบการ" :content="nationalityNameBycode(body.nationality)" />
            <InfoItem title="ประเภท" :content="typeNameByCode(body.type)" />
          </div>
          <div class="grid lg:grid-cols-3 mt-3">
            <InfoItem title="ประเภทผู้ประกอบการ" :content="body.entrepreneurTypeLabel" />
            <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="body.entrepreneurTaxId" />
            <InfoItem title="ชื่อสถานประกอบการ" :content="body.entrepreneurName" />
          </div>
          <div class="grid lg:grid-cols-3 mt-3">
            <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="body.tel" />
            <InfoItem title="อีเมล" :content="body.entrepreneurEmail" />
          </div>
        </div>
        <Card class="mt-5">
          <template #content>
            <TitleHeader label="รายการของพัสดุ">
              <template #action>
                <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
                  class="bg-white! hover:bg-red-50!" @click="onAddPriceDetail"
                  v-if="store.status.canEdit && menuStore.hasManage" />
              </template>
            </TitleHeader>
            <div>
              <DataTable :value="body.details" @row-reorder="(e) => onReorderPriceDetail(e.value)">
                <Column bodyStyle="vertical-align: top" class="w-10">
                  <template #header>
                    <p class="w-full font-bold text-center">ลำดับ</p>
                  </template>
                  <template #body="{ data }">
                    <div>
                      <p class="text-center mt-6">{{ data.sequence }}.</p>
                    </div>
                  </template>
                </Column>
                <Column bodyStyle="vertical-align: top" class="max-w-[90px]">
                  <template #header>
                    <p class="w-full font-bold text-center">รายการ *</p>
                  </template>
                  <template #body="{ data }">
                    <InputField v-model="data.parcelName" rules="required" class="mt-6"
                      :disabled="!store.status.canEdit || !menuStore.hasManage" />
                    <InputArea v-model="data.description" rules="required" class="mt-8"
                      :disabled="!store.status.canEdit || !menuStore.hasManage" label="รายละเอียด" />
                  </template>
                </Column>
                <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
                  <template #header>
                    <p class="w-full font-bold text-center">จำนวน *</p>
                  </template>
                  <template #body="{ data }">
                    <InputNumber v-model="data.parcelQuantity" rules="required" class="mt-6"
                      :disabled="!store.status.canEdit || !menuStore.hasManage" grouping />
                  </template>
                </Column>
                <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
                  <template #header>
                    <p class="w-full font-bold text-center">หน่วยนับ *</p>
                  </template>
                  <template #body="{ data }">
                    <Select v-model="data.parcelUnitCode" :options="store.parcelUnitDDL" rules="required"
                      :disabled="!store.status.canEdit || !menuStore.hasManage" class="mt-6" />
                  </template>
                </Column>
                <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
                  <template #header>
                    <p class="w-full font-bold text-center">ประเภท VAT *</p>
                  </template>
                  <template #body="{ data }">
                    <Select v-model="data.vatTypeCode" :options="store.vatTypeDDL" rules="required"
                      :disabled="!store.status.canEdit || !menuStore.hasManage" class="mt-6" />
                  </template>
                </Column>
                <Column bodyStyle="vertical-align: top" class="max-w-[50px]">
                  <template #header>
                    <p class="w-full font-bold text-center">ราคาที่เสนอ *</p>
                  </template>
                  <template #body="{ data }">
                    <InputNumber v-model="data.offeredPrice" label="ราคาที่เสนอ / หน่วย" rules="required" grouping
                      :min-fraction-digits="2" :disabled="!store.status.canEdit || !menuStore.hasManage" class="mt-6" />
                    <InputNumber
                      :model-value="isNaN(data.parcelQuantity * data.offeredPrice) ? 0 : data.parcelQuantity * data.offeredPrice"
                      label="ราคาที่เสนอรวม" rules="required" disabled grouping :min-fraction-digits="2" class="mt-8" />
                  </template>
                </Column>
                <Column bodyStyle="vertical-align: top" class="max-w-[50px]">
                  <template #header>
                    <p class="w-full font-bold text-center">ราคาที่ตกลง *</p>
                  </template>
                  <template #body="{ data }">
                    <InputNumber v-model="data.agreedPrice" label="ราคาที่ตกลง / หน่วย" rules="required" grouping
                      :min-fraction-digits="2" :disabled="!store.status.canEdit || !menuStore.hasManage" class="mt-6" />
                    <InputNumber
                      :model-value="isNaN(data.parcelQuantity * data.agreedPrice) ? 0 : data.parcelQuantity * data.agreedPrice"
                      label="ราคาที่ตกลงรวม" rules="required" disabled grouping :min-fraction-digits="2" class="mt-8" />
                  </template>
                </Column>
                <Column headerStyle="width: 3rem" bodyStyle="vertical-align: top" v-if="store.status.canEdit">
                  <template #body="{ index }">
                    <Button icon="pi pi-trash" class="mt-1" severity="danger" variant="text"
                      @click="() => removePriceDetail(index)" />
                  </template>
                </Column>
                <Column rowReorder headerStyle="width: 3rem" :reorderableColumn="false"
                  bodyStyle="vertical-align: top;padding-top: 25px" v-if="store.status.canEdit && menuStore.hasManage">
                  <template #rowreordericon>
                    <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                      drag_indicator
                    </span>
                  </template>
                </Column>
                <template #empty>
                  <p class="text-center">ไม่พบข้อมูล</p>
                </template>
              </DataTable>
              <div class="flex justify-end items-center mt-5">
                <div class="text-end">
                  <p>ราคารวมที่เสนอ <span>{{ formatCurrency(sumOfferedPrice) }}</span></p>
                  <p class="text-red-500 text-2xl">ราคารวมที่ตกลง <span>{{ formatCurrency(sumAgreedPrice) }}</span>
                  </p>
                  <p>{{ calculatePercentage }}</p>
                </div>
              </div>
            </div>
          </template>
        </Card>
        <div class="mt-5 flex gap-2 justify-end items-center" v-if="store.status.canEdit && menuStore.hasManage">
          <Button severity="warn" label="ยกเลิก" @click="onClose" />
          <ButtonSave type="submit" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
