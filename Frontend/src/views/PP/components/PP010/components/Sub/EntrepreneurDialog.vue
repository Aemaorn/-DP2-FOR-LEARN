<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Dialog, Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { Radio, InputField } from '@/components/forms';
import { ref, watch } from 'vue';
import { VendorConstants } from '@/constants';
import { ButtonSave } from '@/components/Button';
import { Form } from 'vee-validate';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import ToastHelper from '@/helpers/toast';
import type { Shareholder } from '@/views/PP/models/PP006/pp006Model';
import type { TContractDraftBody } from '@/views/PP/models/PP0010/ContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';

const show = defineModel({
  type: Boolean,
  default: false,
  required: true,
});

const menuStore = useMenuStore();
const store = useContractDraftStore();

const userType = [
  { label: 'ผู้ถือหุ้น ตั้งแต่ 20% ขึ้นไปกรรมการ', value: false },
  { label: 'กรรมการ', value: true },
] as Option[];

const { deleteItemAndReSequence, reSequence } = ArrayHelper();
const { typeNameByCode, nationalityNameBycode } = VendorConstants;

const pp009Vendor = ref<TContractDraftBody>({
  shareholder: [] as Shareholder[],
} as TContractDraftBody);

const addUser = (): void => {
  pp009Vendor.value.shareholder?.push({
    sequence: pp009Vendor.value.shareholder.length + 1,
  } as Shareholder);
};

const getEntrepreneur = (): void => {
  const findData = store.body;

  if (findData) {
    const copyData: TContractDraftBody = JSON.parse(JSON.stringify(findData));

    pp009Vendor.value = {
      ...copyData,
    };
  }
};

const onSubmitAsync = async (): Promise<void> => {
  store.body = pp009Vendor.value

  show.value = false;
};

const onClose = (): void => {
  show.value = false;
};

const resetData = (): void => {
  pp009Vendor.value = {
    shareholder: [] as Shareholder[],
  } as TContractDraftBody;
};

const onEndResequence = (): void => {
  if (pp009Vendor.value.shareholder) {
    pp009Vendor.value.shareholder = reSequence(pp009Vendor.value.shareholder);
  }
};

const deleteEntrepreneur = (index: number): void => {
  if (pp009Vendor.value.shareholder) {
    pp009Vendor.value.shareholder = deleteItemAndReSequence(pp009Vendor.value.shareholder, index);
  }
};

watch(() => show.value, (newValue) => {
  if (newValue) {
    return getEntrepreneur();
  }

  if (!newValue) {
    resetData();
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '80vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" class="p-5 overflow-auto">
        <TitleHeader :label="`ข้อมูลผู้ประกอบการ`">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="onClose"></i>
          </template>
        </TitleHeader>
        <Card>
          <template #content>
            <TitleHeader label="รายการตรวจสอบ" />
            <div v-if="pp009Vendor.id">
              <div class="grid lg:grid-cols-3">
                <InfoItem title="สัญชาติของผู้ประกอบการ"
                  :content="nationalityNameBycode(pp009Vendor.detail.vendor!.nationality)" />
                <InfoItem title="ประเภท" :content="typeNameByCode(pp009Vendor.detail.vendor!.type)" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="ประเภทผู้ประกอบการ" :content="pp009Vendor.detail.vendor!.entrepreneurType" />
                <InfoItem title="เลขประจำตัวผู้เสียภาษี"
                  :content="pp009Vendor.detail.vendor!.taxpayerIdentificationNo" />
                <InfoItem title="ชื่อสถานประกอบการ" :content="pp009Vendor.detail.vendor!.name" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="pp009Vendor.detail.vendor!.tel" />
                <InfoItem title="อีเมล" :content="pp009Vendor.detail.vendor!.email" />
              </div>
            </div>
          </template>
        </Card>
        <Card class="mt-5" v-if="pp009Vendor.id">
          <template #content>
            <TitleHeader label="ข้อมูลสำหรับผู้ถือหุ้น">
              <template #action>
                <Button label="เพิ่มรายชื่อผู้ถือหุ้น" icon="pi pi-plus" severity="primary" variant="outlined"
                  class="bg-white! hover:bg-red-50!" @click="() => addUser()"
                  v-if="store.states.canEdit && menuStore.hasManage" />
              </template>
            </TitleHeader>
            <div v-if="pp009Vendor.shareholder && pp009Vendor.shareholder.length > 0">
              <draggable v-model="pp009Vendor.shareholder" handle=".drag-handle" item-key="id" @end="onEndResequence">
                <template #item="{ element: data, index }">
                  <div class="bg-gray-100 p-5 mt-5">
                    <div class="flex gap-5">
                      <p>{{ data.sequence }}.</p>
                      <div class="bg-white p-5 w-full">
                        <div class="grid lg:grid-cols-3 gap-2">
                          <InputField label="เลขประจำตัวผู้เสียภาษี" v-model="data.taxId" rules="required"
                            :disabled="!store.states.canEdit || !menuStore.hasManage" />
                        </div>
                        <div class="grid lg:grid-cols-3 gap-2">
                          <InputField label="ชื่อ" v-model="data.firstName" rules="required"
                            :disabled="!store.states.canEdit || !menuStore.hasManage" />
                          <InputField label="นามสกุล" v-model="data.lastName" rules="required"
                            :disabled="!store.states.canEdit || !menuStore.hasManage" />
                        </div>
                      </div>
                      <div class="flex items-center gap-2" v-if="store.states.canEdit && menuStore.hasManage">
                        <i class="pi pi-trash text-red-500 cursor-pointer" @click="() => deleteEntrepreneur(index)"></i>
                        <span class="material-symbols-outlined drag-handle cursor-pointer">
                          drag_indicator
                        </span>
                      </div>
                    </div>
                    <Radio :options="userType" class="mt-5 ml-3" v-model="data.isDirectorOr20PctShareholder"
                      :disabled="!store.states.canEdit || !menuStore.hasManage" />
                  </div>
                </template>
              </draggable>
            </div>
          </template>
        </Card>
        <div class="mt-5 flex gap-2 justify-end items-center" v-if="store.states.canEdit && menuStore.hasManage">
          <Button severity="secondary" variant="outlined" label="ยกเลิก" @click="onClose" />
          <ButtonSave type="submit" text="ยินยัน" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
