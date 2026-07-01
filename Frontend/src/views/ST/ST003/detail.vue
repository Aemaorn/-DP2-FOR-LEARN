<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import { Form } from 'vee-validate';
import type { MenuItem } from 'primevue/menuitem';

import { Checkbox, DragdropFilesSection, InputField, Radio, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import { VendorConstants } from '@/constants';
import type { CustomFileSt003 } from '@/models/ST/st003';
import { useSt003DetailStore, useSt003ListStore } from '@/stores/ST/st003';
import addressHelper from '@/helpers/address';
import ST003Service from '@/services/file';
import { useAuthenticationStore } from '@/stores/authentication';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import ToastHelper from '@/helpers/toast';

const routeItems = ref([
  { label: 'รายการคู่ค้า', url: '/st/st003' },
  { label: 'ข้อมูลคู่ค้า' },
] as MenuItem[]);

const route = useRoute();

const authStore = useAuthenticationStore();
const listStore = useSt003ListStore();
const detailStore = useSt003DetailStore();

const { getProvinceAsync, getDistrictAsync, getSubDistrictAsync, provinceOptions, districtOptions, subDistrictOptions } = addressHelper;

onMounted(() => {
  getProvinceAsync();
});

watch(
  () => detailStore.body.address.rawProvinceCode,
  async (provinceCode) => {

    districtOptions.value = [];
    subDistrictOptions.value = [];

    if (provinceCode) {
      await getDistrictAsync(provinceCode);
    }
  }
);

watch(
  () => detailStore.body.address.rawDistrictCode,
  async (districtCode) => {

    subDistrictOptions.value = [];

    if (districtCode) {
      await getSubDistrictAsync(districtCode);
    }
  }
);

onMounted(async (): Promise<void> => {
  await listStore.onGetEntrepreneurTypeOptionsAsync();

  if (route.params.id) {
    await detailStore.onGetByIdAsync(route.params.id as string);
  }
});

onUnmounted((): void => {
  detailStore.onResetBody();
});

const onSubmitAsync = async (): Promise<void> => {
  if (route.params.id) {
    await detailStore.onUpdateAsync(route.params.id.toString());

    return;
  }

  await detailStore.onCreateAsync();
};

const onUploadFileAsync = async (id: string, files: CustomFileSt003[]): Promise<void> => {
  if (files.length > 0) {
    await detailStore.onUploadFileAsync(id, files);
  }
};

const onOpenFile = async (item: CustomFileSt003): Promise<void> => {
  if (item.isPrivate && item.createById != authStore.profile.id) return;

  if (item.fileId) {
    await ST003Service.downloadFile(item.fileId, item.fileName)

    return;
  }

  openFileNewTab(item.file);
};

const openFileNewTab = (file: File): void => {
  const url = URL.createObjectURL(file);
  window.open(url, '_blank');
};

let dragIndex = -1;

const onDragStart = (index: number): void => {
  dragIndex = index;
};

const onDragEnter = (targetIndex: number): void => {
  if (dragIndex === -1 || dragIndex === targetIndex) return;

  const moved = detailStore.body.attachments.splice(dragIndex, 1)[0];
  detailStore.body.attachments.splice(targetIndex, 0, moved);
  dragIndex = targetIndex;
};

const onDragEndAsync = async (): Promise<void> => {
  dragIndex = -1;

  detailStore.body.attachments = detailStore.body.attachments.map((item, index): CustomFileSt003 => {
    item.sequence = index + 1;
    return item;
  });

  await detailStore.onUpdateFileSequenceAsync(
    route.params.id.toString(),
    detailStore.body.attachments
  );
};
//#endregion

const removeFileAsync = async (item: CustomFileSt003): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  await detailStore.onDeleteFileAsync(route.params.id.toString(), item.id, item.fileId);
};

const onChangeFilePrivateAsync = async (): Promise<void> => {
  await detailStore.onUpdatIsPrivateAsync(
    route.params.id.toString(),
    detailStore.body.attachments
  );
};
</script>

<template>
  <Form class="pb-10" @submit="onSubmitAsync" @invalidSubmit="ToastHelper.invalidMessageToast()">
    <TitleHeader :label="route.params.id ? 'แก้ไขข้อมูลคู่ค้า' : 'เพิ่มข้อมูลคู่ค้า'" :route-items="routeItems">
      <template #breadcrumbAction>
        <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
      </template>
    </TitleHeader>
    <Card class="my-4">
      <template #content>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-2 gap-y-8">
          <Radio label="สัญชาติผู้ประกอบการ" v-model="detailStore.body.nationality"
            :options="VendorConstants.nationalityOptions" rules="required" />
          <Radio class="col-span-2" label="ประเภท" v-model="detailStore.body.type"
            :options="VendorConstants.vendorTypeOptions" rules="required" />
          <Select class="col-start-1" label="ประเภทผู้ประกอบการ" v-model="detailStore.body.entrepreneurType"
            :options="listStore.entrepreneurTypeOptions" rules="required" />
          <InputField label="เลขประจำตัวผู้เสียภาษี" v-model="detailStore.body.taxpayerIdentificationNo"
            rules="required" />
          <InputField label="รหัสสาขา" helperText="ตัวอย่าง 00001" v-model="detailStore.body.sapBranchNumber" rules="required|digits5" />
          <InputField label="ชื่อบริษัท/ชื่อ-นามสกุล" v-model="detailStore.body.establishmentName" rules="required" />
          <InputField class="col-start-1" label="ชื่ออาคาร/ชื่อสถานที่" v-model="detailStore.body.placeName" />
          <InputField label="เลขที่บ้าน" v-model="detailStore.body.address.houseNumber" />
          <InputField label="ห้องเลขที่" v-model="detailStore.body.address.roomNumber" />
          <InputField label="ชั้นที่" v-model="detailStore.body.address.floor" />
          <InputField class="col-start-1" label="หมู่บ้าน" v-model="detailStore.body.address.villageName" />
          <InputField label="หมู่ที่" v-model="detailStore.body.address.moo" />
          <InputField label="ซอย" v-model="detailStore.body.address.allay" />
          <InputField label="ถนน" v-model="detailStore.body.address.road" />
          <InputField class="col-start-1" label="รหัสไปรษณีย์" v-model="detailStore.body.address.postalCode"
            rules="required" />
          <Select label="จังหวัด" v-model="detailStore.body.address.rawProvinceCode" :options="provinceOptions"
            rules="required" />
          <Select label="อำเภอ/เขต" v-model="detailStore.body.address.rawDistrictCode"
            :options="districtOptions" rules="required" />
          <Select label="ตำบล/แขวง" v-model="detailStore.body.address.rawSubDistrictCode"
            :options="subDistrictOptions" rules="required" />
          <InputField class="col-start-1" label="หมายเลขโทรศัพท์สำหรับติดต่อ" v-model="detailStore.body.tel"
            rules="required" />
          <InputField label="หมายเลขโทรสาร" v-model="detailStore.body.fax" />
          <InputField class="col-start-1" label="เลขที่คู่ค้า (SAP)" v-model="detailStore.body.sapVendorNumber"/>
          <InputField label="อีเมล" v-model="detailStore.body.email" />
        </div>
      </template>
    </Card>
    <DragdropFilesSection v-if="route.params.id" label="เอกสารแนบ" v-model="detailStore.body.attachments"
      @on-change="onUploadFileAsync(route.params.id.toString(), detailStore.body.attachments)" multiple>
      <template #fileList="{ item, index }: { item: CustomFileSt003, index: number }">
        <Divider />
        <div class="w-full flex items-center gap-[12px] p-1 mb-5 relative text-[26px] grab" draggable="true"
          @dragstart="onDragStart(index)" @dragenter.prevent="onDragEnter(index)" @dragover.prevent
          @dragend="onDragEndAsync">
          <span class="w-[30px] font-bold">{{ item.sequence }}.</span>

          <Checkbox label="เอกสารส่วนบุคคล" v-model="item.isPrivate" @on-change="onChangeFilePrivateAsync" />

          <div class="ps-10">
            <i class="pi pi-file"></i>
            <span :style="{ color: item.isPrivate ? '#999' : 'inherit' }"
              class="ps-3 underline text-blue-500 cursor-pointer" @click="onOpenFile(item)">
              {{ item.file?.name ?? item.fileName }}
            </span>
          </div>

          <div class="center absolute right-0 top-1/2 -translate-y-1/2 gap-4">
            <span @click="removeFileAsync(item)" class="pi pi-trash text-red-500 pointer w-fit"
              v-if="(item.isPrivate && item.createById == authStore.profile.id) || !item.isPrivate"></span>
            <span class="material-symbols-outlined text-gray-400 grab">drag_indicator</span>
          </div>
        </div>
        <Divider />
      </template>
    </DragdropFilesSection>
  </Form>
</template>

<style scoped>
.center {
  display: flex;
  align-items: center;
  justify-content: center;
}

.underline {
  text-decoration: underline;
}

.pointer {
  cursor: pointer;
}

.grab {
  cursor: grab;
}
</style>
