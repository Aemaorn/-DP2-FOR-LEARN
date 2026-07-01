<script setup lang="ts">
import type { TitleHeader } from '@/components/cosmetic';
import { InputArea, InputField, InputNumber, Select, type Checkbox } from '@/components/forms';
import { ArrayHelper } from '@/helpers/array';
import { usePurchaseOrder } from '@/views/PP/stores/PP007/PP007Store';
import { Dialog, type DataTableRowReorderEvent } from 'primevue';
import type { Option } from '@/models/shared/option';
import { onMounted, ref, computed, watch } from 'vue';
import { EGroupCode } from '@/enums/shared';
import SharedService from '@/services/Shared/dropdown';
import { HttpStatusCode } from 'axios';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import type { PP007PriceDetail } from '@/views/PP/models/PP007/pp007Model'
import ButtonSave from '@/components/Button/ButtonSave.vue';
import { formatCurrency } from '@/helpers/currency';
import { Form } from 'vee-validate';
import ToastHelper from '@/helpers/toast';
import { useMenuStore } from '@/stores/menu';

const value = defineModel<boolean>({
  default: false
});

const index = defineModel<number>("index", { default: 0 });

const IsPriceDetail = defineModel<boolean>("isPriceDetail", { default: true })

const oldPriceDetail = defineModel<PP007PriceDetail[]>("oldPriceDetail", { default: () => [] });

const props = defineProps({
  readonly: { type: Boolean, default: false },
});

const menuStore = useMenuStore();

const store = usePurchaseOrder();

const procurementStore = usePPDetailStore();

const { addSequence, deleteItemAndReSequence, reSequence } = ArrayHelper();

const addPriceDetail = () => {
  if (!store.body.entrepreneurs[index.value].priceDetails) {
    store.body.entrepreneurs[index.value].priceDetails = [];
  }

  store.body.entrepreneurs[index.value].priceDetails = addSequence(store.body.entrepreneurs[index.value].priceDetails, {
  } as PP007PriceDetail)
};

const unitDropdownData = ref<Array<Option>>([]);

const onGetunitDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.UnitOfMea);

  if (status === HttpStatusCode.Ok) {
    unitDropdownData.value = data;
  }
}

const vatDropdownData = ref<Array<Option>>([]);

const onVatDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.VATType);

  if (status === HttpStatusCode.Ok) {
    vatDropdownData.value = data;
  }
};

const winReasonDropdownData = ref<Array<Option>>([]);

const onWinReasonDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.WinReason);

  if (status === HttpStatusCode.Ok) {
    winReasonDropdownData.value = data;
  }
};

const SumAllOfferedPriceSum = computed(() => {
  return store.body.entrepreneurs[index.value].priceDetails.reduce((sum, s) => sum + (s.offeredPriceSum || 0), 0);
});

const SumAllAgreedPriceSum = computed(() => {
  return store.body.entrepreneurs[index.value].priceDetails.reduce((sum, s) => sum + (s.agreedPriceSum || 0), 0);
});
const budget = computed(() => procurementStore.procurementDetail.budget);
const medianPrice = computed(() => store.body.medianPrice);

const isUnderBudget = computed(() => SumAllAgreedPriceSum.value <= budget.value);
const isOverBudget = computed(() => SumAllAgreedPriceSum.value > budget.value)
const isUnderMedianPrice = computed(() => SumAllAgreedPriceSum.value <= (medianPrice.value ?? 0));
const isOverMedianPrice = computed(() => SumAllAgreedPriceSum.value > (medianPrice.value ?? 0))

const budgetDiffPercent = computed(() => {
  const diff = Math.abs(SumAllAgreedPriceSum.value - budget.value);
  return ((diff * 100) / (isUnderBudget.value ? budget.value : SumAllAgreedPriceSum.value)).toFixed(2);
});

const totalAgreedDiffPercent = computed(() => {
  const diff = Math.abs(SumAllAgreedPriceSum.value - SumAllOfferedPriceSum.value);
  const base = SumAllOfferedPriceSum.value || 1;
  return ((diff * 100) / base).toFixed(2);
});

const medianPriceDif = computed(() => {
  const diff = Math.abs(SumAllAgreedPriceSum.value - (medianPrice.value ?? 0));
  return ((diff * 100) / (isUnderMedianPrice.value ? (medianPrice.value ?? 0) : SumAllAgreedPriceSum.value)).toFixed(2);
});

const deleteItem = (indexValue: number): void => {
  store.body.entrepreneurs[index.value].priceDetails = deleteItemAndReSequence(store.body.entrepreneurs[index.value].priceDetails, indexValue);
};

const onRowReorder = (event: DataTableRowReorderEvent): void => {
  store.body.entrepreneurs[index.value].priceDetails = reSequence(event.value);
};

const onSubmit = async () => {
  if (IsPriceDetail.value) {
    if (store.body.entrepreneurs[index.value].priceDetails.length == 0) {
      ToastHelper.warning("ข้อมูลประกอบการ", "กรุณากรอกข้อมูลราคาพัสดุ");

      return;
    }

    if (store.body.entrepreneurs[index.value].priceDetails.every(x =>
      x.vatTypeCode &&
      x.offeredPrice != null &&
      (x.agreedPrice != null && x.agreedPrice > 0)
    )) {
      store.body.entrepreneurs[index.value].isBidding = true;
    } else {
      store.body.entrepreneurs[index.value].isBidding = false;
    }
  }

  if (store.body.jp006Id) {
    await store.onUpdateJp006Async();
  } else {
    await store.onCreateJp006Async();
  }

  value.value = false;
}

const sumOfferdPriceByDetail = (priceDetailIndex: number, quantity: number, price: number) => {
  if (!store.body.entrepreneurs[index.value].priceDetails[priceDetailIndex]) return;

  store.body.entrepreneurs[index.value].priceDetails[priceDetailIndex].offeredPriceSum = quantity * price;
}

const sumAgreedPriceByDetail = (priceDetailIndex: number, quantity: number, price: number) => {
  if (!store.body.entrepreneurs[index.value].priceDetails[priceDetailIndex]) return;

  store.body.entrepreneurs[index.value].priceDetails[priceDetailIndex].agreedPriceSum = quantity * price;
}

const onCancel = () => {
  if (IsPriceDetail.value) {
    store.body.entrepreneurs[index.value].priceDetails = oldPriceDetail.value;
  }

  value.value = false;
}

watch(() => value.value, (newValue) => {
  if (newValue) {
    oldPriceDetail.value = store.body.entrepreneurs[index.value].priceDetails.map(x => ({ ...x }));

    store.body.entrepreneurs[index.value].priceDetails.forEach((detail, idx) => {
      if (detail.parcelQuantity && detail.offeredPrice) {
        sumOfferdPriceByDetail(idx, detail.parcelQuantity, detail.offeredPrice);
      }
      if (detail.parcelQuantity && detail.agreedPrice) {
        sumAgreedPriceByDetail(idx, detail.parcelQuantity, detail.agreedPrice);
      }
    });
  }
});

onMounted(async (): Promise<void> => {
  await onGetunitDropdownAsync();
  await onVatDropdownAsync();
  await onWinReasonDropdownAsync();
});
</script>

<template>
  <Dialog :closable="!store.canEdit || props.readonly" v-model:visible="value" modal :draggable="false"
    :style="{ width: '90vw', hight: '100%' }" :breakpoints="{ '1199px': '75vw', '575px': '90vw' }"
    @hide="() => (value = false)">
    <template #header>
      <TitleHeader label="ข้อมูลผู้ค้า" />
    </template>
    <template #default>
      <Form @submit="onSubmit()">
        <div>
          <div class="grid grid-cols-3 gap-4 px-4">
            <div>
              <div class="text-gray-400">
                สัญชาติของผู้ประกอบการ
              </div>
              <div>
                {{ store.body.entrepreneurs[index].entrepreneurNationality == "TH" ? 'ไทย' : 'ต่างชาติ' }}
              </div>
            </div>
            <div class="col-span-2">
              <div class="text-gray-400">
                ประเภท
              </div>
              <div>
                <div>
                  {{ store.body.entrepreneurs[index].type == "Individual" ? 'บุคคลธรรมดา' :
                    store.body.entrepreneurs[index].type == "Consortium" ? "กิจการค้าร่วม (Consortium)" :
                      store.body.entrepreneurs[index].type == "JointVenture" ? "กิจการร่วมค้า (Joint Venture)" : "นิติบุคคล"
                  }}
                </div>
              </div>
            </div>
            <div>
              <div class="text-gray-400">
                เลขประจำตัวผู้เสียภาษี
              </div>
              <div>
                {{ store.body.entrepreneurs[index].entrepreneurTaxId }}
              </div>
            </div>
            <div>
              <div class="text-gray-400">
                ประเภทผู้ประกอบการ
              </div>
              <div>
                {{ store.body.entrepreneurs[index].entrepreneurType }}
              </div>
            </div>
            <div>
              <div class="text-gray-400">
                ชื่อสถานประกอบการ
              </div>
              <div>
                {{ store.body.entrepreneurs[index].entrepreneurName }}
              </div>
            </div>
            <div>
              <div class="text-gray-400">
                หมายเลขโทรศัพท์สำหรับติดต่อ
              </div>
              <div>
                {{ store.body.entrepreneurs[index].entrepreneurPhoneNumber }}
              </div>
            </div>
            <div>
              <div class="text-gray-400">
                อีเมล
              </div>
              <div>
                {{ store.body.entrepreneurs[index].entrepreneurEmail }}
              </div>
            </div>
          </div>

          <TitleHeader label="รายการของพัสดุ">
            <template #action>
              <Button v-if="IsPriceDetail && store.canEdit && menuStore.hasManage && !props.readonly" class="bg-white hover:bg-yellow-50"
                icon="pi pi-plus" label="เพิ่มรายการ" severity="primary" variant="outlined" @click="addPriceDetail" />
            </template>
          </TitleHeader>
          <DataTable :value="store.body.entrepreneurs[index].priceDetails" @row-reorder="onRowReorder">
            <Column bodyStyle="vertical-align: top" class="min-w-[40px]">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ data }">
                <p class="text-center mt-8">{{ data.sequence }}</p>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="min-w-[150px]">
              <template #header>
                <p class="w-full font-bold text-center">รายการ</p>
              </template>
              <template #body="{ data }">
                <InputField class="mt-8" label="รายการ" v-model="data.parcelName" rules="required"
                  :disabled="!store.canEdit || !isPriceDetail || !menuStore.hasManage || props.readonly" />
                <InputArea class="mt-8" label="รายละเอียด" v-model="data.description" rules="required"
                  :disabled="!store.canEdit || !isPriceDetail || !menuStore.hasManage || props.readonly" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="min-w-[150px]">
              <template #header>
                <p class="w-full font-bold text-center">จำนวน</p>
              </template>
              <template #body="{ data, index: priceIndex }">
                <InputNumber class="mt-8" label="จำนวน" v-model="data.parcelQuantity" rules="required"
                  :disabled="!menuStore.hasManage || !store.canEdit || !isPriceDetail || props.readonly" @update:model-value="(value) => {
                    sumAgreedPriceByDetail(priceIndex, value ?? 0, data.agreedPrice);
                    sumOfferdPriceByDetail(priceIndex, value ?? 0, data.offeredPrice);
                  }" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="min-w-[150px]">
              <template #header>
                <p class="w-full font-bold text-center">หน่วยนับ</p>
              </template>
              <template #body="{ data }">
                <Select class="mt-8" label="หน่วยนับ" v-model="data.parcelUnitCode" :options="unitDropdownData"
                  rules="required" :disabled="!store.canEdit || !isPriceDetail || !menuStore.hasManage || props.readonly" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="min-w-[150px]">
              <template #header>
                <p class="w-full font-bold text-center">ประเภท VAT</p>
              </template>
              <template #body="{ data }">
                <Select class="mt-8" label="ประเภท VAT" v-model="data.vatTypeCode" :options="vatDropdownData"
                  rules="required" :disabled="!store.canEdit || !isPriceDetail || !menuStore.hasManage || props.readonly" />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="min-w-[150px]">
              <template #header>
                <p class="w-full font-bold text-center">ราคาที่เสนอ</p>
              </template>
              <template #body="{ data, index: priceIndex }">
                <InputNumber class="mt-8" label="ราคา/หน่วย" v-model="data.offeredPrice" rules="required" grouping
                  :min-fraction-digits="2" :max-fraction-digits="6" :disabled="!store.canEdit || !isPriceDetail || !menuStore.hasManage || props.readonly"
                  @update:model-value="(value) => sumOfferdPriceByDetail(priceIndex, data.parcelQuantity, value ?? 0)" />
                <InputNumber class="mt-8" label="ราคาเสนอรวม"
                  :model-value="(data.offeredPrice ?? 0) * (data.parcelQuantity ?? 0)" rules="required" grouping
                  :min-fraction-digits="2" :max-fraction-digits="6" disabled />
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top" class="min-w-[150px]">
              <template #header>
                <p class="w-full font-bold text-center">ราคาที่ตกลง</p>
              </template>
              <template #body="{ data, index: priceIndex }">
                <InputNumber class="mt-8" label="ราคา/หน่วย" v-model="data.agreedPrice" rules="required" grouping
                  :min-fraction-digits="2" :max-fraction-digits="4" :disabled="!store.canEdit || !isPriceDetail || !menuStore.hasManage || props.readonly"
                  @update:model-value="(value) => sumAgreedPriceByDetail(priceIndex, data.parcelQuantity, value ?? 0)" />
                <InputNumber class="mt-8" label="ราคาตกลงรวม"
                  :model-value="(data.agreedPrice ?? 0) * (data.parcelQuantity ?? 0)" :min-fraction-digits="2"
                  :max-fraction-digits="4" rules="required" grouping disabled />
              </template>
            </Column>
            <Column v-if="IsPriceDetail && store.canEdit && menuStore.hasManage && !props.readonly" bodyStyle="vertical-align: top"
              class="min-w-[25px]">
              <template #body="{ index: priceIndex }">
                <Button icon="pi pi-trash" severity="danger" variant="text" @click="() => deleteItem(priceIndex)" />
              </template>
            </Column>
            <Column v-if="IsPriceDetail && store.canEdit && menuStore.hasManage && !props.readonly" rowReorder headerStyle="width: 3rem"
              :reorderableColumn="false" bodyStyle="vertical-align: top;padding-top: 25px" class="min-w-[25px]">
              <template>
                <span class="material-symbols-outlined cursor-pointer" :draggable="true">
                  drag_indicator
                </span>
              </template>
            </Column>
            <template #empty>
              <p class="text-center text-gray-500">ไม่มีข้อมูล</p>
            </template>
          </DataTable>
          <Card class="bg-gray-100" v-if="SumAllAgreedPriceSum > 0 && SumAllOfferedPriceSum > 0">
            <template #content>
              <div class="flex justify-end gap-4">
                <div class="flex justify-end flex-col gap-4 text-end">
                  <p>งบประมาณ {{ formatCurrency(budget) }}</p>
                  <p v-if="isUnderBudget || isOverBudget">
                    {{ isUnderBudget ? 'ต่ำกว่า' : 'สูงกว่า' }}งบประมาณ
                    {{ formatCurrency(Math.abs(SumAllAgreedPriceSum - budget)) }}
                    คิดเป็นร้อยละ {{ budgetDiffPercent }}%
                  </p>
                  <p>ราคากลาง {{ formatCurrency(medianPrice ?? 0) }}</p>
                  <p v-if="(isUnderMedianPrice || isOverMedianPrice)">
                    {{ isUnderMedianPrice ? 'ต่ำกว่า' : 'สูงกว่า' }}ราคากลาง
                    {{ formatCurrency(Math.abs((medianPrice ?? 0) - SumAllAgreedPriceSum)) }}
                    คิดเป็นร้อยละ {{ medianPriceDif }}%
                  </p>
                </div>
                <div class="flex justify-end flex-col gap-4 text-end">
                  <p>ราคารวมที่เสนอ {{ formatCurrency(SumAllOfferedPriceSum) }}</p>
                  <p class="text-red-500 text-4xl font-bold">
                    ราคารวมที่ตกลง {{ formatCurrency(SumAllAgreedPriceSum) }}
                  </p>
                  <p>
                    {{ SumAllOfferedPriceSum > SumAllAgreedPriceSum ? 'ต่ำกว่า' : 'สูงกว่า' }}ราคาที่เสนอ
                    {{ formatCurrency(Math.abs(SumAllAgreedPriceSum - SumAllOfferedPriceSum)) }}
                    คิดเป็นร้อยละ {{ totalAgreedDiffPercent }}%
                  </p>
                </div>
              </div>
            </template>
          </Card>
          <Card v-if="!IsPriceDetail" class="mt-4">
            <template #content>
              <TitleHeader label="ผลการพิจารณา" />
              <div class="flex flex-col gap-4">
                <div class="flex gap-4">
                  <Checkbox :disabled="!store.canEdit || !menuStore.hasManage || props.readonly"
                    v-model="store.body.entrepreneurs[index].isWinner" />
                  <spane>
                    ผู้ชนะ
                  </spane>
                </div>
                <Select :disabled="!store.canEdit || !menuStore.hasManage || props.readonly" class="md:w-2/4"
                  :rules="store.body.entrepreneurs[index].isWinner ? 'required' : ''"
                  v-model="store.body.entrepreneurs[index].selectionReasonCode" :options="winReasonDropdownData" />
                <InputArea :disabled="!store.canEdit || !menuStore.hasManage || props.readonly" label="รายละเอียด"
                  v-model="store.body.entrepreneurs[index].remark" />
              </div>
            </template>
          </Card>
          <div v-if="store.canEdit && menuStore.hasManage && !props.readonly" class="flex justify-end gap-4 mt-4">
            <Button @click="onCancel" severity="secondary" variant="outlined" label="ยกเลิก" />
            <ButtonSave type="submit" />
          </div>
        </div>
      </Form>
    </template>
  </Dialog>
</template>