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
import { usePurchaseOrder } from '@/views/PP/stores/PP007/PP007Store';
import type { PP007Entrepreneurs } from '@/views/PP/models/PP007/pp007Model';
import type { Shareholder } from '@/views/PP/models/PP006/pp006Model';
import { PP006SearchType } from '@/views/PP/enums/pp006';
import suVendorShareholdersService from '@/services/SU/suVendorShareholders';
import { showPartnerDialogAsync } from '@/helpers/dialog';

const show = defineModel({
  type: Boolean,
  default: false,
  required: true,
});

const props = defineProps({
  selected: { type: Number, default: undefined },
  readonly: { type: Boolean, default: false },
});

const menuStore = useMenuStore();
const store = usePurchaseOrder();
const searchType = ref(PP006SearchType.TaxID);
const searchText = ref();
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

const checkTypeSearch = (): string => {
  if (searchType.value === PP006SearchType.TaxID) {
    return 'เลขประจำตัวผู้เสียภาษี';
  }

  return 'ชื่อบริษัท/ชื่อ - นามสกุล';
};

const pp007Entrepreneurs = ref<PP007Entrepreneurs>({
  shareholder: [] as Shareholder[],
} as PP007Entrepreneurs);

const addUser = (): void => {
  pp007Entrepreneurs.value.shareholder?.push({
    sequence: pp007Entrepreneurs.value.shareholder.length + 1,
  } as Shareholder);
};

const getEntrepreneur = (): void => {
  const findData = store.body.entrepreneurs[props.selected!];

  if (findData) {
    const copyData: PP007Entrepreneurs = JSON.parse(JSON.stringify(findData));

    pp007Entrepreneurs.value = {
      ...copyData,
      shareholder: copyData.shareholder?.map((s): Shareholder => ({
        ...s,
        firstName: [s.firstName, s.lastName].filter(Boolean).join(' '),
        lastName: '',
      })),
    };
  }
};

const onUpdateEntrepreneurAsync = async (): Promise<void> => {
  await store.updateEntreprenuerAsync(pp007Entrepreneurs.value);

  onClose();
};

const onSubmitAsync = async (): Promise<void> => {
  const hasMissing = pp007Entrepreneurs.value.shareholder?.some(
    (s: Shareholder): boolean => !s.taxId && !s.firstName
  );
  if (hasMissing) {
    return ToastHelper.warning('ข้อมูลผู้ถือหุ้น', 'กรุณากรอกข้อมูลอย่างน้อย 1 ฟิลด์ต่อรายการ (เลขประจำตัว หรือ ชื่อ)');
  }

  if (pp007Entrepreneurs.value.shareholder) {
    pp007Entrepreneurs.value.shareholder = pp007Entrepreneurs.value.shareholder.map((s): Shareholder => {
      if (s.isJuristic) {
        return { ...s, firstName: (s.firstName ?? '').trim(), lastName: '' };
      }
      const fullName = (s.firstName ?? '').trim().replace(/\s+/g, ' ');
      const parts = fullName.split(' ');
      return { ...s, firstName: parts[0] ?? '', lastName: parts.slice(1).join(' ') || '' };
    });
  }

  if (!pp007Entrepreneurs.value.vendorId) {
    return ToastHelper.warning('ข้อมูลผู้ประกอบการ', 'กรุณาค้นหาและเลือกผู้ประกอบการก่อนบันทึก');
  }

  if (store.body.jp006Id) {
    if (pp007Entrepreneurs.value.entrepreneurId) {
      await onUpdateEntrepreneurAsync();
      return;
    }

    if (props.selected !== undefined) {
      store.body.entrepreneurs[props.selected] = pp007Entrepreneurs.value;
    } else {
      store.body.entrepreneurs = [
        ...store.body.entrepreneurs,
        pp007Entrepreneurs.value,
      ];
    }

    await store.onUpdateJp006Async();
    show.value = false;
    return;
  }

  if (props.selected !== undefined) {
    store.body.entrepreneurs[props.selected] = pp007Entrepreneurs.value;
  } else {
    store.body.entrepreneurs = [
      ...store.body.entrepreneurs,
      pp007Entrepreneurs.value,
    ];
  }

  await store.onCreateJp006Async();
  show.value = false;
};

const onClose = (): void => {
  show.value = false;
};

const resetData = (): void => {
  pp007Entrepreneurs.value = {
    shareholder: [] as Shareholder[],
  } as PP007Entrepreneurs;
};

const onEndResequence = (): void => {
  if (pp007Entrepreneurs.value.shareholder) {
    pp007Entrepreneurs.value.shareholder = reSequence(pp007Entrepreneurs.value.shareholder);
  }
};

const deleteEntrepreneur = (index: number): void => {
  if (pp007Entrepreneurs.value.shareholder) {
    pp007Entrepreneurs.value.shareholder = deleteItemAndReSequence(pp007Entrepreneurs.value.shareholder, index);
  }
};

watch(() => show.value, (newValue) => {
  if (newValue && props.selected !== undefined) {
    return getEntrepreneur();
  }

  if (!newValue) {
    resetData();
  }
});

const onOpenPartnerAsync = async (): Promise<void> => {
  const res = await showPartnerDialogAsync(searchText.value);

  if (store.body.entrepreneurs?.some(i => i.vendorId === res.id)) {
    return ToastHelper.warning('เพิ่มผู้ประกอบการ', 'มีผู้ประกอบการนี้แล้ว');
  }

  if (res) {
    const { data } = await suVendorShareholdersService.getByVendorIdAsync(res.id);

    pp007Entrepreneurs.value = {
      vendorId: res.id,
      sequence: store.body.entrepreneurs ? store.body.entrepreneurs.length + 1 : 1,
      emailSended: false,
      entrepreneurTaxId: res.taxpayerIdentificationNo,
      entrepreneurName: res.establishmentName,
      entrepreneurEmail: res.email ?? "",
      entrepreneurPhoneNumber: res.tel,
      entrepreneurType: res.entrepreneurType ?? "",
      sapBranchNumber: res.sapBranchNumber,
      entrepreneurNationality: res.nationality,
      entrepreneurPlaceName: res.placeName ?? "",
      shareholder: data.map((item, i): Shareholder => ({
        sequence: i + 1,
        taxId: '',
        firstName: [item.firstName, item.lastName].filter(Boolean).join(' '),
        lastName: '',
        isDirector: item.isDirector ?? false,
        isShareholder: item.isShareholder ?? false,
        isJuristic: item.isJuristic ?? undefined,
      } as Shareholder)),
      coi: {
        remark: "",
        date: new Date(),
      },
      egp: {
        remark: "",
        date: new Date(),
      },
      watchlist: {
        remark: "",
        date: new Date(),
      },
      isWinner: false,
      isBidding: false,
      type: res.type,
      priceDetails: [],
      attachments: [],
    };
  }
};

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
            <div v-if="!pp007Entrepreneurs.vendorId">
              <Radio :options="searchTypeSelect" v-model="searchType" :disabled="!menuStore.hasManage || props.readonly" />
              <div class="grid lg:grid-cols-3 gap-2 mt-4">
                <InputField :label="checkTypeSearch()" v-model="searchText" :disabled="!menuStore.hasManage || props.readonly">
                  <template #appendAction>
                    <InputGroupAddon>
                      <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! w-full h-full"
                        @click="onOpenPartnerAsync" v-if="menuStore.hasManage && !props.readonly" />
                    </InputGroupAddon>
                  </template>
                </InputField>
              </div>
            </div>
            <div v-if="pp007Entrepreneurs.vendorId">
              <div class="grid lg:grid-cols-3">
                <InfoItem title="สัญชาติของผู้ประกอบการ"
                  :content="nationalityNameBycode(pp007Entrepreneurs.entrepreneurNationality)" />
                <InfoItem title="ประเภท" :content="typeNameByCode(pp007Entrepreneurs.type)" />
                <InfoItem title="ประเภทผู้ประกอบการ" :content="pp007Entrepreneurs.entrepreneurType" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="เลขประจำตัวผู้เสียภาษี" :content="pp007Entrepreneurs.entrepreneurTaxId" />
                <InfoItem title="ชื่อสถานประกอบการ" :content="pp007Entrepreneurs.entrepreneurName" />
                <InfoItem title="รหัสสาขา" :content="pp007Entrepreneurs.sapBranchNumber || '-'" />
              </div>
              <div class="grid lg:grid-cols-3 mt-3">
                <InfoItem title="หมายเลขโทรศัพท์สำหรับติดต่อ" :content="pp007Entrepreneurs.entrepreneurPhoneNumber" />
                <InfoItem title="อีเมล" :content="pp007Entrepreneurs.entrepreneurEmail" />
              </div>
            </div>
          </template>
        </Card>
        <Card class="mt-5" v-if="pp007Entrepreneurs.vendorId">
          <template #content>
            <TitleHeader label="ข้อมูลสำหรับผู้ถือหุ้น">
              <template #action>
                <Button label="เพิ่มรายชื่อผู้ถือหุ้น" icon="pi pi-plus" severity="primary" variant="outlined"
                  class="bg-white! hover:bg-red-50!" @click="() => addUser()"
                  v-if="store.canEdit && menuStore.hasManage && !props.readonly" />
              </template>
            </TitleHeader>
            <table v-if="pp007Entrepreneurs.shareholder && pp007Entrepreneurs.shareholder.length > 0"
              class="w-full border-collapse text-sm mt-4">
              <thead>
                <tr class="bg-gray-200 text-gray-900 font-bold">
                  <th class="border border-gray-300 px-3 py-2 text-center w-12">ลำดับที่</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">กรรมการ / ผู้ถือหุ้น</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">ประเภท</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">เลขที่บัตรประชาชน / เลขประจำตัวผู้เสียภาษี</th>
                  <th class="border border-gray-300 px-3 py-2 text-center">ชื่อ-นามสกุล / บริษัท<br><span class="text-red-500 font-normal text-xs"><b>หมายเหตุ</b> ชื่อ-นามสกุล (ไม่ต้องกรอกคำนำหน้าชื่อ) เช่น สมชาย ใจดี</span></th>
                  <th v-if="store.canEdit && menuStore.hasManage && !props.readonly" class="border border-gray-300 px-3 py-2 w-20"></th>
                </tr>
              </thead>
              <draggable v-model="pp007Entrepreneurs.shareholder" handle=".drag-handle" item-key="id"
                tag="tbody" @end="onEndResequence">
                <template #item="{ element: data, index }">
                  <tr class="odd:bg-white even:bg-gray-50">
                    <td class="border border-gray-300 px-3 py-2 text-center text-gray-500">{{ data.sequence }}</td>
                    <td class="border border-gray-300 px-3 py-2">
                      <div class="flex flex-wrap gap-4">
                        <div class="flex items-center gap-2">
                          <Checkbox v-model="data.isDirector" :binary="true" :inputId="`isDirector-pp007-${index}`" :disabled="!store.canEdit || !menuStore.hasManage || props.readonly" />
                          <label :for="`isDirector-pp007-${index}`" class="cursor-pointer -mt-4">กรรมการ</label>
                        </div>
                        <div class="flex items-center gap-2">
                          <Checkbox v-model="data.isShareholder" :binary="true" :inputId="`isShareholder-pp007-${index}`" :disabled="!store.canEdit || !menuStore.hasManage || props.readonly" />
                          <label :for="`isShareholder-pp007-${index}`" class="cursor-pointer -mt-4">ผู้ถือหุ้น</label>
                        </div>
                      </div>
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <Radio :options="personTypeOptions" v-model="data.isJuristic" rules="required" :disabled="!store.canEdit || !menuStore.hasManage || props.readonly" />
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <InputField v-model.trim="data.taxId" rules="digits13" eager
                        :disabled="!store.canEdit || !menuStore.hasManage || props.readonly" />
                    </td>
                    <td class="border border-gray-300 px-3 py-2">
                      <InputField v-model.trim="data.firstName"
                        :disabled="!store.canEdit || !menuStore.hasManage || props.readonly"
                        @blur="data.firstName = data.isJuristic ? (data.firstName ?? '').trim() : (data.firstName ?? '').trim().replace(/\s+/g, ' ')" />
                    </td>
                    <td v-if="store.canEdit && menuStore.hasManage && !props.readonly"
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
        <div class="mt-5 flex gap-2 justify-end items-center" v-if="store.canEdit && menuStore.hasManage && !props.readonly">
          <Button severity="secondary" variant="outlined" label="ยกเลิก" @click="onClose" />
          <ButtonSave type="submit" text="ยืนยัน" />
        </div>
      </Form>
    </template>
  </Dialog>
</template>
