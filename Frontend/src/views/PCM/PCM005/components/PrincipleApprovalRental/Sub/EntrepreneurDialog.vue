<script setup lang="ts">
import type { Entrepreneurs, ShareHolder } from '@/models/PCM/PCM005/principleApprovalRental';
import type { Option } from '@/models/shared/option';
import { Dialog, Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { Radio, InputField } from '@/components/forms';
import { ButtonSave } from '@/components/Button';
import { Form } from 'vee-validate';
import { ref, watch } from 'vue';
import { PP006SearchType } from '@/views/PP/enums/pp006';
import { VendorConstants } from '@/constants';
import { showPartnerDialogAsync } from '@/helpers/dialog';
import { usePcm005PrinApproveRentStore } from '@/stores/PCM/PCM005/principleApprovalRental';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import ToastHelper from '@/helpers/toast';
import suVendorShareholdersService from '@/services/SU/suVendorShareholders';

const show = defineModel('show', { type: Boolean, required: true, default: false });

const data = defineModel<Entrepreneurs>("vendor", { default: () => ({} as Entrepreneurs) });

const isNew = ref<boolean>(false);

const menuStore = useMenuStore();
const store = usePcm005PrinApproveRentStore();
const searchTypeSelect = [
  { label: 'ค้นหา เลขประจำตัวผู้เสียภาษี', value: PP006SearchType.TaxID },
  { label: 'ค้นหา ชื่อบริษัท/ชื่อ - นามสกุล', value: PP006SearchType.Name },
] as Option[];

const personTypeOptions = [
  { value: false, label: 'บุคคลธรรมดา' },
  { value: true, label: 'นิติบุคคล' },
] as Option[];

const { deleteItemAndReSequence, reSequence } = ArrayHelper();
const { typeNameByCode, nationalityNameBycode } = VendorConstants;
const oldData = ref<Entrepreneurs>({} as Entrepreneurs);

const searchType = ref(PP006SearchType.TaxID);
const searchText = ref();

const onClose = (): void => {
  show.value = false;
  data.value = { ...oldData.value };
};

const addUser = (): void => {
  data.value.shareholders = data.value.shareholders ?? [];
  data.value.shareholders.push({
    sequence: data.value.shareholders.length + 1,
    taxId: '',
    firstName: '',
    lastName: '',
    isJuristic: undefined,
    isDirector: false,
    isShareholder: false,
  } as ShareHolder);
};

const checkTypeSearch = (): string => {
  if (searchType.value === PP006SearchType.TaxID) {
    return 'เลขประจำตัวผู้เสียภาษี';
  }
  return 'ชื่อบริษัท/ชื่อ - นามสกุล';
};

const onOpenPartnerAsync = async (): Promise<void> => {
  const res = await showPartnerDialogAsync(searchText.value);

  if (store.body.entrepreneurs?.some(i => i.vendorId === res.id)) {
    return ToastHelper.warning('เพิ่มผู้ประกอบการ', 'มีผู้ประกอบการนี้แล้ว');
  }

  if (res) {
    const { data: shareholders } = await suVendorShareholdersService.getByVendorIdAsync(res.id);

    data.value = {
      vendorId: res.id,
      sequence: store.body.entrepreneurs ? store.body.entrepreneurs.length + 1 : 1,
      coiResult: false,
      egpResult: false,
      watchlistResult: false,
      emailSend: false,
      entrepreneurTaxId: res.taxpayerIdentificationNo,
      entrepreneurName: res.establishmentName,
      entrepreneurEmail: res.email,
      shareholders: shareholders.map((item, i): ShareHolder => ({
        sequence: i + 1,
        taxId: '',
        firstName: [item.firstName, item.lastName].filter(Boolean).join(' '),
        lastName: '',
        isJuristic: item.isJuristic ?? undefined,
        isDirector: item.isDirector ?? false,
        isShareholder: item.isShareholder ?? false,
      } as ShareHolder)),
      ...res,
      id: undefined,
      details: [],
      attachments: [],
    } as Entrepreneurs;
  }

  if (isNew.value) {
    store.createEntrepreneurAsync(data.value);
    data.value = store.body.entrepreneurs[store.body.entrepreneurs.length - 1];
  }
};

const onEndResequence = (): void => {
  if (data.value.shareholders) {
    data.value.shareholders = reSequence(data.value.shareholders);
  }
};

const deleteEntrepreneur = (index: number): void => {
  if (data.value.shareholders) {
    data.value.shareholders = deleteItemAndReSequence(data.value.shareholders, index);
  }
};

const onSubmitAsync = async (): Promise<void> => {
  const hasMissing = data.value.shareholders?.some(
    (s: ShareHolder): boolean => !s.taxId && !s.firstName
  );
  if (hasMissing) {
    return ToastHelper.warning('ข้อมูลผู้ถือหุ้น', 'กรุณากรอกข้อมูลอย่างน้อย 1 ฟิลด์ต่อรายการ (เลขประจำตัว หรือ ชื่อ)');
  }

  if (data.value.shareholders) {
    data.value.shareholders = data.value.shareholders.map((s): ShareHolder => {
      if (s.isJuristic) {
        return { ...s, firstName: (s.firstName ?? '').trim(), lastName: '' };
      }
      const fullName = (s.firstName ?? '').trim().replace(/\s+/g, ' ');
      const parts = fullName.split(' ');
      return { ...s, firstName: parts[0] ?? '', lastName: parts.slice(1).join(' ') || '' };
    });
  }

  if (data.value.id) {
    const success = await store.updateEntrepreneurAsync(data.value);
    if (success) show.value = false;
  } else {
    const success = await store.createEntrepreneurApiAsync(data.value);
    if (success) show.value = false;
  }
};

watch(() => show.value, (newValue) => {
  if (newValue) {
    isNew.value = !data.value.vendorId;
    oldData.value = { ...data.value };

    data.value.shareholders = data.value.shareholders?.map((s): ShareHolder => ({
      ...s,
      firstName: [s.firstName, s.lastName].filter(Boolean).join(' '),
      lastName: '',
      isJuristic: s.isJuristic,
    }));
  }
});
</script>

<template>
  <Dialog v-model:visible="show" modal :style="{ width: '80vw' }" :draggable="false" :breakpoints="{ '575px': '90vw' }">
    <template #container>
      <Form @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" class="p-5 overflow-auto">
        <TitleHeader :label="`${isNew ? 'เพิ่ม' : 'แก้ไข'}ผู้ประกอบการ`">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="onClose"></i>
          </template>
        </TitleHeader>
        <Card>
          <template #content>
            <TitleHeader label="รายการตรวจสอบ" />
            <div v-if="isNew">
              <Radio :options="searchTypeSelect" v-model="searchType" :disabled="!menuStore.hasManage" />
              <div class="grid lg:grid-cols-3 gap-2 mt-4">
                <InputField :label="checkTypeSearch()" v-model="searchText" :disabled="!menuStore.hasManage">
                  <template #appendAction>
                    <InputGroupAddon>
                      <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! w-full h-full"
                        @click="onOpenPartnerAsync" v-if="menuStore.hasManage" />
                    </InputGroupAddon>
                  </template>
                </InputField>
              </div>
            </div>
            <div v-if="data.vendorId">
              <div class="grid lg:grid-cols-3">
                <InfoItem title="สัญชาติของผู้ประกอบการ" :content="nationalityNameBycode(data.nationality)" />
                <InfoItem title="ประเภท" :content="typeNameByCode(data.type)" />
                <InfoItem title="ประเภทผู้ประกอบการ" :content="data.entrepreneurTypeLabel" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="data.entrepreneurTaxId" />
                <InfoItem title="ชื่อสถานประกอบการ" :content="data.entrepreneurName" />
                <InfoItem title="รหัสสาขา" :content="data.sapBranchNumber || '-'" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="data.tel" />
                <InfoItem title="อีเมล" :content="data.entrepreneurEmail" />
              </div>
            </div>
          </template>
        </Card>

        <Card class="mt-5" v-if="data.vendorId">
          <template #content>
            <TitleHeader label="ข้อมูลสำหรับผู้ถือหุ้น">
              <template #action>
                <Button label="เพิ่มรายชื่อผู้ถือหุ้น" icon="pi pi-plus" severity="primary" variant="outlined"
                  class="bg-white! hover:bg-red-50!" @click="() => addUser()"
                  v-if="store.status.canEdit && menuStore.hasManage" />
              </template>
            </TitleHeader>
            <table v-if="data.shareholders && data.shareholders.length > 0"
              class="w-full border-collapse text-sm mt-4">
              <thead>
                <tr class="bg-gray-200 text-gray-900 font-bold">
                  <th class="border border-gray-300 px-3 py-2 text-center w-12">ลำดับที่</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">กรรมการ / ผู้ถือหุ้น</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">ประเภท</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน / เลขประจำตัวผู้เสียภาษี</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ-นามสกุล / บริษัท<br><span class="text-red-500 font-normal text-xs"><b>หมายเหตุ</b> ชื่อ-นามสกุล (ไม่ต้องกรอกคำนำหน้าชื่อ) เช่น สมชาย ใจดี</span></th>
                  <th v-if="store.status.canEdit && menuStore.hasManage" class="border border-gray-300 px-3 py-2 w-20"></th>
                </tr>
              </thead>
              <draggable v-model="data.shareholders" handle=".drag-handle" item-key="sequence"
                tag="tbody" @end="onEndResequence">
                <template #item="{ element: row, index }">
                  <tr class="odd:bg-white even:bg-gray-50">
                    <td class="border border-gray-300 px-3 py-2 text-center text-gray-500">{{ row.sequence }}</td>
                    <td class="border border-gray-300 px-3 py-2">
                      <div class="flex flex-wrap gap-4">
                        <div class="flex items-center gap-2">
                          <Checkbox v-model="row.isDirector" :binary="true" :inputId="`isDirector-pcm005e-${index}`"
                            :disabled="!store.status.canEdit || !menuStore.hasManage" />
                          <label :for="`isDirector-pcm005e-${index}`" class="cursor-pointer -mt-4">กรรมการ</label>
                        </div>
                        <div class="flex items-center gap-2">
                          <Checkbox v-model="row.isShareholder" :binary="true" :inputId="`isShareholder-pcm005e-${index}`"
                            :disabled="!store.status.canEdit || !menuStore.hasManage" />
                          <label :for="`isShareholder-pcm005e-${index}`" class="cursor-pointer -mt-4">ผู้ถือหุ้น</label>
                        </div>
                      </div>
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <Radio :options="personTypeOptions" v-model="row.isJuristic" rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage" />
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <InputField v-model.trim="row.taxId" rules="digits13" eager :disabled="!store.status.canEdit || !menuStore.hasManage" />
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <InputField v-model.trim="row.firstName"
                        :disabled="!store.status.canEdit || !menuStore.hasManage"
                        @blur="row.firstName = row.isJuristic ? (row.firstName ?? '').trim() : (row.firstName ?? '').trim().replace(/\s+/g, ' ')" />
                    </td>
                    <td v-if="store.status.canEdit && menuStore.hasManage"
                      class="border border-gray-300 px-3 py-2 text-center">
                      <div class="flex items-center justify-center gap-2">
                        <i class="pi pi-trash text-red-500 cursor-pointer" @click="() => deleteEntrepreneur(index)"></i>
                        <span class="material-symbols-outlined drag-handle cursor-pointer text-gray-400">
                          drag_indicator
                        </span>
                      </div>
                    </td>
                  </tr>
                </template>
              </draggable>
            </table>
          </template>
        </Card>

        <div class="mt-5 flex gap-2 justify-end items-center" v-if="store.status.canEdit && menuStore.hasManage">
          <Button severity="secondary" variant="outlined" label="ยกเลิก" @click="onClose" />
          <ButtonSave type="submit" text="ยืนยัน" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
