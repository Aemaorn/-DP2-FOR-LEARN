<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import type { InvitedEntrepreneurs, Shareholder } from '@/views/PP/models/PP006/pp006Model';
import { Dialog, Card } from 'primevue';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { Radio, InputField } from '@/components/forms';
import { PP006SearchType, PP006Status } from '@/views/PP/enums/pp006';
import { ref, watch } from 'vue';
import { showPartnerDialogAsync } from '@/helpers/dialog';
import { usePP006DetailStore } from '@/views/PP/stores/PP006/PP006Store';
import { VendorConstants } from '@/constants';
import { ButtonSave } from '@/components/Button';
import { Form } from 'vee-validate';
import { ArrayHelper } from '@/helpers/array';
import { useMenuStore } from '@/stores/menu';
import draggable from 'vuedraggable';
import ToastHelper from '@/helpers/toast';
import suVendorShareholdersService from '@/services/SU/suVendorShareholders';

const show = defineModel({
  type: Boolean,
  default: false,
  required: true,
});

const props = defineProps({
  selected: { type: String },
});

const menuStore = useMenuStore();
const store = usePP006DetailStore();
const searchTypeSelect = [
  { label: 'ค้นหา เลขประจำตัวผู้เสียภาษี', value: PP006SearchType.TaxID },
  { label: 'ค้นหา ชื่อบริษัท/ชื่อ - นามสกุล', value: PP006SearchType.Name },
] as Option[];
const { deleteItemAndReSequence, reSequence } = ArrayHelper();
const { typeNameByCode, nationalityNameBycode } = VendorConstants;

const personTypeOptions = [
  { value: false, label: 'บุคคลธรรมดา' },
  { value: true, label: 'นิติบุคคล' },
] as Option[];

const searchType = ref(PP006SearchType.TaxID);
const searchText = ref();
const invitedEntrepreneurs = ref<InvitedEntrepreneurs>({
  shareholders: [] as Shareholder[],
} as InvitedEntrepreneurs);

const checkTypeSearch = (): string => {
  if (searchType.value === PP006SearchType.TaxID) {
    return 'เลขประจำตัวผู้เสียภาษี';
  }

  return 'ชื่อบริษัท/ชื่อ - นามสกุล';
};

const addUser = (): void => {
  invitedEntrepreneurs.value.shareholders?.push({
    sequence: invitedEntrepreneurs.value.shareholders.length + 1,
  } as Shareholder);
};

const getEntrepreneur = async (): Promise<void> => {
  const findData = store.detail.invitedEntrepreneurs.find(i => i.id === props.selected);

  if (findData) {
    const copyData: InvitedEntrepreneurs = JSON.parse(JSON.stringify(findData));

    invitedEntrepreneurs.value = {
      ...copyData,
      shareholders: copyData.shareholders?.map((s): Shareholder => ({
        ...s,
        firstName: [s.firstName, s.lastName].filter(Boolean).join(' '),
        lastName: '',
      })),
    };
  }
};

const onOpenPartnerAsync = async (): Promise<void> => {
  const res = await showPartnerDialogAsync(searchText.value);

  if (store.detail.invitedEntrepreneurs?.some(i => i.vendorId === res.id)) {
    return ToastHelper.warning('เพิ่มผู้ประกอบการ', 'มีผู้ประกอบการนี้แล้ว');
  }

  if (res) {
    const { data } = await suVendorShareholdersService.getByVendorIdAsync(res.id);

    invitedEntrepreneurs.value = {
      vendorId: res.id,
      sequence: store.detail.invitedEntrepreneurs ? store.detail.invitedEntrepreneurs.length + 1 : 1,
      coiResult: false,
      egpResult: false,
      watchlistResult: false,
      emailSend: false,
      entrepreneurTaxId: res.taxpayerIdentificationNo,
      entrepreneurName: res.establishmentName,
      entrepreneurEmail: res.email,
      shareholders: data.map((item, i): Shareholder => ({
        sequence: i + 1,
        taxId: '',
        firstName: [item.firstName, item.lastName].filter(Boolean).join(' '),
        lastName: '',
        isDirector: item.isDirector ?? false,
        isShareholder: item.isShareholder ?? false,
        isJuristic: item.isJuristic ?? undefined,
      } as Shareholder)),
      ...res,
      id: undefined,
    } as InvitedEntrepreneurs;
  }
};

const onInviteEntrepreneurAsync = async (): Promise<void> => {
  if (!invitedEntrepreneurs.value.vendorId) {
    return ToastHelper.warning('เพิ่มผู้ประกอบการ', 'กรุณาเลือกผู้ประกอบการ');
  }

  if (store.detail.invitedEntrepreneurs?.some(i => i.vendorId === invitedEntrepreneurs.value.vendorId)) {
    return ToastHelper.warning('เพิ่มผู้ประกอบการ', 'มีผู้ประกอบการนี้แล้ว');
  }

  await store.invitedEntreprenuerAsync(invitedEntrepreneurs.value);
  onClose();
};

const onUpdateEntrepreneurAsync = async (): Promise<void> => {
  if (store.detail.id) {
    await store.updateAsync(store.detail.id, PP006Status.Draft, true)
  }

  await store.updateEntrepreneurAsync(invitedEntrepreneurs.value, 'แก้ไขผู้ประกอบการ', 'แก้ไขผู้ประกอบการสำเร็จ');

  onClose();
};

const onSubmitAsync = async (): Promise<void> => {
  const hasMissing = invitedEntrepreneurs.value.shareholders?.some(
    (s: Shareholder): boolean => !s.taxId && !s.firstName
  );
  if (hasMissing) {
    return ToastHelper.warning('ข้อมูลผู้ถือหุ้น', 'กรุณากรอกข้อมูลอย่างน้อย 1 ฟิลด์ต่อรายการ (เลขประจำตัว หรือ ชื่อ)');
  }

  if (invitedEntrepreneurs.value.shareholders) {
    invitedEntrepreneurs.value.shareholders = invitedEntrepreneurs.value.shareholders.map((s): Shareholder => {
      if (s.isJuristic) {
        return { ...s, firstName: (s.firstName ?? '').trim(), lastName: '' };
      }
      const fullName = (s.firstName ?? '').trim().replace(/\s+/g, ' ');
      const parts = fullName.split(' ');
      return { ...s, firstName: parts[0] ?? '', lastName: parts.slice(1).join(' ') || '' };
    });
  }

  if (invitedEntrepreneurs.value.id) {
    await onUpdateEntrepreneurAsync();
  } else {
    await onInviteEntrepreneurAsync();
  }
};

const onClose = (): void => {
  show.value = false;
};

const resetData = (): void => {
  invitedEntrepreneurs.value = {
    shareholders: [] as Shareholder[],
  } as InvitedEntrepreneurs;
};

const onEndResequence = (): void => {
  if (invitedEntrepreneurs.value.shareholders) {
    invitedEntrepreneurs.value.shareholders = reSequence(invitedEntrepreneurs.value.shareholders);
  }
};

const deleteEntrepreneur = (index: number): void => {
  if (invitedEntrepreneurs.value.shareholders) {
    invitedEntrepreneurs.value.shareholders = deleteItemAndReSequence(invitedEntrepreneurs.value.shareholders, index);
  }
};

watch(() => show.value, (newValue) => {
  if (newValue && props.selected) {
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
        <TitleHeader
          :label="`${store.status.canEdit ? invitedEntrepreneurs.id ? 'แก้ไข' : 'เพิ่ม' : 'ข้อมูล'}ผู้ประกอบการ`">
          <template #action>
            <i class="pi pi-times cursor-pointer" @click="onClose"></i>
          </template>
        </TitleHeader>
        <Card>
          <template #content>
            <TitleHeader label="รายการตรวจสอบ" />
            <div v-if="!invitedEntrepreneurs.id">
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
            <div v-if="invitedEntrepreneurs.vendorId">
              <div class="grid lg:grid-cols-3">
                <InfoItem title="สัญชาติของผู้ประกอบการ"
                  :content="nationalityNameBycode(invitedEntrepreneurs.nationality)" />
                <InfoItem title="ประเภท" :content="typeNameByCode(invitedEntrepreneurs.type)" />
                <InfoItem title="ประเภทผู้ประกอบการ" :content="invitedEntrepreneurs.entrepreneurType" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="invitedEntrepreneurs.entrepreneurTaxId" />
                <InfoItem title="ชื่อสถานประกอบการ" :content="invitedEntrepreneurs.entrepreneurName" />
                <InfoItem title="รหัสสาขา" :content="invitedEntrepreneurs.sapBranchNumber || '-'" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="invitedEntrepreneurs.tel" />
                <InfoItem title="อีเมล" :content="invitedEntrepreneurs.entrepreneurEmail" />
              </div>
            </div>
          </template>
        </Card>
        <Card class="mt-5" v-if="invitedEntrepreneurs.vendorId">
          <template #content>
            <TitleHeader label="ข้อมูลสำหรับผู้ถือหุ้น">
              <template #action>
                <Button label="เพิ่มรายชื่อผู้ถือหุ้น" icon="pi pi-plus" severity="primary" variant="outlined"
                  class="bg-white! hover:bg-red-50!" @click="() => addUser()"
                  v-if="store.status.canEdit && menuStore.hasManage" />
              </template>
            </TitleHeader>
            <table v-if="invitedEntrepreneurs.shareholders && invitedEntrepreneurs.shareholders.length > 0"
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
              <draggable v-model="invitedEntrepreneurs.shareholders" handle=".drag-handle" item-key="id"
                tag="tbody" @end="onEndResequence">
                <template #item="{ element: data, index }">
                  <tr class="odd:bg-white even:bg-gray-50">
                    <td class="border border-gray-300 px-3 py-2 text-center text-gray-500">{{ data.sequence }}</td>
                    <td class="border border-gray-300 px-3 py-2">
                      <div class="flex flex-wrap gap-4">
                        <div class="flex items-center gap-2">
                          <Checkbox v-model="data.isDirector" :binary="true" :inputId="`isDirector-pp006-${index}`" :disabled="!store.status.canEdit || !menuStore.hasManage" />
                          <label :for="`isDirector-pp006-${index}`" class="cursor-pointer -mt-4">กรรมการ</label>
                        </div>
                        <div class="flex items-center gap-2">
                          <Checkbox v-model="data.isShareholder" :binary="true" :inputId="`isShareholder-pp006-${index}`" :disabled="!store.status.canEdit || !menuStore.hasManage" />
                          <label :for="`isShareholder-pp006-${index}`" class="cursor-pointer -mt-4">ผู้ถือหุ้น</label>
                        </div>
                      </div>
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <Radio :options="personTypeOptions" v-model="data.isJuristic" rules="required" :disabled="!store.status.canEdit || !menuStore.hasManage" />
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <InputField v-model.trim="data.taxId" rules="digits13" eager
                        :disabled="!store.status.canEdit || !menuStore.hasManage" />
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <InputField v-model.trim="data.firstName"
                        :disabled="!store.status.canEdit || !menuStore.hasManage"
                        @blur="data.firstName = data.isJuristic ? (data.firstName ?? '').trim() : (data.firstName ?? '').trim().replace(/\s+/g, ' ')" />
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
          <ButtonSave type="submit" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
