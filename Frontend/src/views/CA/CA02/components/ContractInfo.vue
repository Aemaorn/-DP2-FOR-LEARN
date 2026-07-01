<script setup lang="ts">
import { Card, Dialog } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { InputField, InputNumber, InputArea, Datepicker, Select } from '@/components/forms';
import { computed, onMounted, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useCA02ContractDialogStore, useCA02DetailStore } from '@/stores/CA/ca02';
import type { CA02ContractVendorInfo, TCA02DialogTable } from '@/models/CA/ca02';
import { ToDateOnly } from '@/helpers/dateTime';
import { formatCurrency } from '@/helpers/currency';
import { showConfirmDialogAsync, showPartnerDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { isNullorEmpty } from '@/utils/validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useRouter } from 'vue-router';
import { useMenuStore } from '@/stores/menu';

type Props = {
  disabled?: boolean;
  autoOpen?: boolean;
};

const router = useRouter();
const store = useCA02DetailStore();
const menuStore = useMenuStore();
const value = defineModel<CA02ContractVendorInfo>({
  required: true,
});
const { disabled, autoOpen } = defineProps<Props>();

const isVisible = ref(false);
const { isManualContractInfoEditing: isEditing } = storeToRefs(store);
const dialogStore = useCA02ContractDialogStore();
const emit = defineEmits<(event: 'onSelected', value: TCA02DialogTable) => void>();

const onShowDialogAsync = async (): Promise<void> => {
  if (value.value.id && !await showConfirmDialogAsync(ConfirmDialogType.ConfirmChange)) return;

  isVisible.value = true;
  await dialogStore.fn.onGetListAsync();
};

onMounted((): void => {
  if (autoOpen && !store.body.isManual && !store.body.id) onShowDialogAsync();
});

const onSelected = (selectedValue: TCA02DialogTable) => {
  emit('onSelected', selectedValue);

  isVisible.value = false;
};

watch(
  (): number[] => [dialogStore.searchCriteria.pageNumber, dialogStore.searchCriteria.pageSize],
  (): void => { dialogStore.fn.onGetListAsync(); }
);

watch(
  (): boolean | undefined => store.body.isManual,
  (isManual: boolean | undefined): void => {
    if (isManual && !store.body.id) isEditing.value = true;
  },
  { immediate: true }
);

const entrepreneurDisplay = computed((): string => {
  const id = value.value?.entrepreneurCode;
  const name = value.value?.entrepreneurName;
  if (!name) return '';
  return id ? `${id} : ${name}` : name;
});

const contractSignedDateModel = computed({
  get: (): Date | undefined => value.value?.contractSignedDate ? new Date(value.value.contractSignedDate) : undefined,
  set: (v: Date | undefined): void => { if (value.value) value.value.contractSignedDate = v?.toISOString() ?? ''; }
});

const deliveryDateModel = computed({
  get: (): Date | undefined => value.value?.deliveryDate ? new Date(value.value.deliveryDate) : undefined,
  set: (v: Date | undefined): void => { if (value.value) value.value.deliveryDate = v?.toISOString() ?? ''; }
});

const onSelectPartnerAsync = async (): Promise<void> => {
  const res = await showPartnerDialogAsync();
  if (!res) return;
  value.value.entrepreneurId = res.id;
  value.value.entrepreneurName = res.establishmentName;
  value.value.entrepreneurEmail = res.email ?? '';
  value.value.entrepreneurCode = res.sapVendorNumber ?? '';
};

const enterEditMode = (): void => { isEditing.value = true; };

const onCancelEdit = async (): Promise<void> => {
  isEditing.value = false;
  if (store.body.id) {
    await store.fn.onGetByIdAsync(undefined, store.body.id);
  }
};

const loadSupplyMethodDDLAsync = async (): Promise<void> => {
  await Promise.all([
    store.fn.getSupplyMethodCodeDDLAsync(),
    store.fn.getSupplyMethodTypeCodeDDLAsync(),
  ]);
  if (value.value?.supplyMethodCode) {
    await store.fn.getSupplyMethodSpecialTypeCodeDDLAsync(value.value.supplyMethodCode);
  }
};

watch(
  (): boolean | undefined => store.body.isManual,
  async (isManual): Promise<void> => {
    if (isManual) await loadSupplyMethodDDLAsync();
  },
  { immediate: true }
);

watch(
  (): string | undefined => value.value?.supplyMethodCode,
  async (newCode, oldCode): Promise<void> => {
    if (!store.body.isManual || newCode === oldCode) return;
    value.value.supplyMethodTypeCode = undefined;
    value.value.supplyMethodSpecialTypeCode = undefined;
    await store.fn.getSupplyMethodSpecialTypeCodeDDLAsync(newCode);
  }
);
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา">
        <template #action>
          <template v-if="store.body.isManual">
            <div class="flex items-center gap-2">
              <span class="text-[#1D4ED8] leading-none">&#9679;</span>
              <p class="whitespace-nowrap text-sm text-[#1D4ED8] font-bold">สร้างใหม่ (ไม่อ้างอิงเอกสาร)</p>
            </div>
            <template v-if="menuStore.hasManage && store.body.id && store.states.isEdit">
              <i v-if="!isEditing" class="pi pi-pencil text-orange-500 cursor-pointer hover:text-orange-700"
                style="font-size: 0.75rem;" aria-label="แก้ไขข้อมูล" @click="enterEditMode" />
              <template v-else>
                <Button label="ยกเลิก" icon="pi pi-times" severity="secondary" variant="outlined" size="small"
                  @click="onCancelEdit" />
              </template>
            </template>
          </template>
        </template>
      </TitleHeader>

      <!-- Search field (ซ่อนเมื่อ manual + มี id + ไม่ได้ editing) -->
      <div class="grid lg:grid-cols-3 gap-2 mt-8" v-if="!store.body.isManual || !store.body.id || isEditing">
        <InputField :label="store.body.isManual ? 'สร้างใหม่ (ไม่อ้างอิงเอกสาร)' : 'เลขที่สัญญา'" v-model="value.contractNumber" :rules="store.body.isManual ? undefined : 'required'" disabled>
          <template #appendAction>
            <InputGroupAddon v-if="!disabled">
              <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! h-full w-full"
                @click="onShowDialogAsync" />
            </InputGroupAddon>
          </template>
        </InputField>
      </div>

      <!-- Reference mode: info display after selection -->
      <div class="grid grid-cols-3 gap-2" v-if="!store.body.isManual && value.id">
        <InfoItem title="คู่ค้า" :content="`${value.entrepreneur.code} : ${value.entrepreneur.name}`" />
        <InfoItem title="Email" :content="!isNullorEmpty(value.entrepreneur.email) ? value.entrepreneur.email : '-'" />
        <InfoItem class="lg:col-start-1" title="เลขที่สัญญา" :content="value.contractNumber" />
        <InfoItem title="เลขที่ PO (SAP)" :content="value.poNumber" />
        <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(value.budget)" />
        <InfoItem class="lg:col-start-1" title="วิธีการจัดหา" :content="value.supplyMethodLabel ?? '-'" />
        <InfoItem title="" :content="value.supplyMethodTypeLabel ?? ''" />
        <InfoItem title="" :content="value.supplyMethodSpecialTypeLabel ?? ''" />
        <InfoItem title="ชื่อสัญญา" :content="value.contractName" />
        <InfoItem title="ประเภทสัญญา" :content="value.contractTypeLabel" />
        <InfoItem title="รูปแบบสัญญา" :content="value.templateLabel" />
        <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(value.contractSignedDate)" />
        <InfoItem title="กำหนดส่งมอบภายใน" :content="`${value.deliveryLeadTime} วัน`" />
        <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(value.deliveryDate)" />
        <InfoItem title="ระยะเวลารับประกัน" :content="value.deliveryLeadTimeTypeLabel" />
      </div>

      <!-- Manual mode: info view (read-only) -->
      <div class="grid grid-cols-3 gap-2" v-if="store.body.isManual && store.body.id && !isEditing">
        <InfoItem title="คู่ค้า" :content="entrepreneurDisplay" />
        <InfoItem title="Email คู่ค้า" :content="!isNullorEmpty(value.entrepreneurEmail) ? value.entrepreneurEmail : '-'" />
        <InfoItem class="lg:col-start-1" title="เลขที่สัญญา" :content="value.contractNumber" />
        <InfoItem title="เลขที่ PO (SAP)" :content="value.poNumber" />
        <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(value.budget)" />
        <InfoItem class="lg:col-start-1" title="วิธีการจัดหา" :content="value.supplyMethodLabel ?? '-'" />
        <InfoItem title="" :content="value.supplyMethodTypeLabel ?? ''" />
        <InfoItem title="" :content="value.supplyMethodSpecialTypeLabel ?? ''" />
        <InfoItem class="lg:col-span-3" title="ชื่อสัญญา" :content="value.contractName" />
        <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(value.contractSignedDate)" />
        <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(value.deliveryDate)" />
      </div>

      <!-- Manual mode: edit -->
      <template v-if="store.body.isManual && (!store.body.id || isEditing)">
        <div class="grid lg:grid-cols-3 gap-2 gap-y-8 mt-8">
          <InputField label="คู่ค้า" :modelValue="entrepreneurDisplay" rules="required" :disabled="!isEditing">
            <template #appendAction>
              <InputGroupAddon v-if="isEditing">
                <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! w-full h-full"
                  @click="onSelectPartnerAsync" />
              </InputGroupAddon>
            </template>
          </InputField>
          <InputField label="Email คู่ค้า" :modelValue="value?.entrepreneurEmail ?? ''" :disabled="!isEditing" />
          <InputField class="lg:col-start-1" label="เลขที่สัญญา" v-model="value.contractNumber" rules="required"
            :disabled="!isEditing" />
          <InputField label="เลขที่ PO (SAP)" v-model="value.poNumber" rules="required" :disabled="!isEditing" />
          <InputNumber label="วงเงินตามสัญญา" v-model="value.budget" rules="required" grouping
            :min-fraction-digits="2" :disabled="!isEditing" />
          <Select class="lg:col-start-1" label="วิธีการจัดหา" :options="store.supplyMethodCodeDDL"
            v-model="value.supplyMethodCode" :disabled="!isEditing" rules="required" />
          <Select :options="store.supplyMethodTypeCodeDDL" v-model="value.supplyMethodTypeCode"
            :disabled="!isEditing" rules="required" />
          <Select :options="store.supplyMethodSpecialTypeCodeDDL" v-model="value.supplyMethodSpecialTypeCode"
            :disabled="!isEditing || !value.supplyMethodCode" rules="required" />
          <InputArea class="lg:col-span-3" label="ชื่อสัญญา" v-model="value.contractName" rules="required"
            :disabled="!isEditing" />
          <Datepicker label="วันที่ลงนามในสัญญา" v-model="contractSignedDateModel" rules="required"
            :disabled="!isEditing" />
          <Datepicker label="ครบกำหนดส่งมอบงาน วันที่" v-model="deliveryDateModel" rules="required"
            :disabled="!isEditing" />
        </div>
      </template>
    </template>
  </Card>

  <Dialog v-model:visible="isVisible" modal :style="{ width: '80vw' }" :draggable="false"
    :breakpoints="{ '575px': '90vw' }" maximizable>
    <template #container="{ closeCallback, maximizeCallback }">
      <div class="flex flex-col bg-white rounded-2xl max-h-[90vh] overflow-hidden">
        <!-- Header -->
        <div class="flex items-center justify-between p-4 shrink-0">
          <TitleHeader label="ค้นหาสัญญา"></TitleHeader>
          <div class="flex items-center gap-2">
            <span
              class="material-symbols-outlined text-gray-500 border-[0.5px] border-gray-500 rounded-md cursor-pointer"
              @click="maximizeCallback">
              expand_content
            </span>
            <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">close</span>
          </div>
        </div>

        <!-- Criteria (fixed) -->
        <div class="px-4 shrink-0">
          <Card>
            <template #content>
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2 gap-y-5">
                <InputField label="คำค้นหา" class="lg:col-span-2" v-model.trim="dialogStore.searchCriteria.keyword" />
              </div>
              <div class="flex gap-2 justify-start lg:justify-end items-center mt-5">
                <Button class="lg:w-fit w-full" label="ค้นหา" icon="pi pi-search" @click="dialogStore.fn.onGetListAsync" />
                <Button class="lg:w-fit w-full" label="ล้าง" icon="pi pi-eraser" variant="outlined"
                  @click="dialogStore.fn.onResetCriteria" />
              </div>
            </template>
          </Card>
        </div>

        <!-- Results (scrollable) -->
        <div class="flex-1 overflow-y-auto px-4 min-h-0">
          <p class="text-primary font-bold text-lg mt-4 mb-2">ผลการค้นหา</p>

          <Card v-for="(item, index) in (dialogStore.table.data as Array<TCA02DialogTable>)" :key="index"
            class="mt-2 border border-gray-300">
            <template #content>
              <div class="grid lg:grid-cols-12 gap-x-4 gap-y-2">
                <div class="lg:col-span-8">
                  <InfoRow label="เลขที่จัดซื้อจัดจ้าง">
                    <p class="underline text-blue-300 hover:cursor-pointer"
                      @click="router.push({ name: 'pl001Detail', params: { id: item.planId } })">
                      {{ item.dpNumber }}
                    </p>
                  </InfoRow>
                  <InfoRow label="เลขที่สัญญา">
                    <p class="underline text-blue-300">{{ item.contractNumber }}</p>
                  </InfoRow>
                  <InfoRow label="เลขที่ PO (SAP)">
                    {{ item.poNumber }}
                  </InfoRow>
                  <InfoRow label="วันที่ลงนามสัญญา">
                    {{ ToDateOnly(item.contractSignedDate) }}
                  </InfoRow>
                  <InfoRow label="คู่ค้า">
                    {{ `${item.entrepreneurCode} : ${item.entrepreneurName}` }}
                  </InfoRow>
                  <InfoRow label="ชื่อสัญญา">
                    <p class="font-bold">{{ item.contractName }}</p>
                  </InfoRow>
                  <InfoRow label="วงเงินตามสัญญา">
                    {{ formatCurrency(item.budget) }}
                  </InfoRow>
                  <InfoRow label="ประเภทสัญญา">
                    {{ item.contractTypeLabel }}
                  </InfoRow>
                </div>
                <div class="lg:col-span-4 flex flex-col items-end justify-center gap-2">
                  <Button class="text-[#f9a825] border-[#f9a825] bg-white hover:bg-[#f9a825] hover:text-white"
                    label="เลือก" @click="() => onSelected(item)" :disabled />
                </div>
              </div>
            </template>
          </Card>
          <p v-if="!dialogStore.table.data?.length" class="text-center mt-4">ไม่พบข้อมูล</p>
        </div>

        <!-- Pagination (fixed) -->
        <div class="px-4 py-2 shrink-0">
          <Pagination :page-number="dialogStore.searchCriteria.pageNumber"
            :page-size="dialogStore.searchCriteria.pageSize" :total-record="dialogStore.table.totalRecords"
            @change="dialogStore.fn.onChangePageSize" />
        </div>
      </div>
    </template>
  </Dialog>
</template>
