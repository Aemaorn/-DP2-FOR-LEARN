<script setup lang="ts">
import { Dialog, Button } from 'primevue';
import { InputField, Select } from '@/components/forms';
import { ButtonSearch, ButtonClear } from '@/components/Button';
import { onMounted, ref, watch } from 'vue';
import { useEventListener } from '@vueuse/core';
import { usePartnerDialogStore } from '@/stores/Shared/partnerDialog';
import type { TSt003List } from '@/models/ST/st003';
import { VendorConstants } from '@/constants';
import type { TPartnerDialog } from '@/models/shared/dialog';
import Pagination from '@/components/Pagination.vue';
import InfoRow from '@/components/cosmetic/InfoRow.vue';

const partnerDialogStore = usePartnerDialogStore();

const isShow = ref<boolean>(false);

onMounted(async () => {
  useEventListener(document, 'onShowPartnerDialog', async (data: CustomEvent<TPartnerDialog>): Promise<void> => {
    isShow.value = true;
    partnerDialogStore.searchCriteria.keyword = data.detail.searchText;

    await Promise.all([partnerDialogStore.onGetEntrepreneurTypeOptionsAsync(), partnerDialogStore.onGetPartnerListDataAsync()]);
  });
});

const onSelected = (selectedItem: TSt003List): void => {
  document.dispatchEvent(new CustomEvent<TSt003List>('onClosePartnerDialog', {
    detail: selectedItem,
  }));

  isShow.value = false;
};

const afterCloseModal = (): void => {
  partnerDialogStore.onResetStore();
};

watch(isShow, (val: boolean) => {
  if (!val) {
    document.dispatchEvent(new CustomEvent<TSt003List>('onClosePartnerDialog', {}));
  }
});
</script>

<template>
  <Dialog v-model:visible="isShow" modal :style="{ width: '80vw' }" :draggable="false"
    :breakpoints="{ '575px': '90vw' }" @afterHide="afterCloseModal" maximizable>
    <template #container="{ closeCallback, maximizeCallback }">
      <div class="h-full overflow-y-auto hide-scrollbar">
        <div class="sticky top-0 bg-white z-10 rounded-lg">
          <div class="p-4">
            <div class="flex gap-2 justify-between items-center">
              <div class="flex gap-2 md:gap-4 items-center w-full">
                <div class="h-4 md:h-7 w-3 md:w-6 bg-primary transform -skew-x-12" />
                <h6 class="font-bold">ข้อมูลคู่ค้า</h6>
                <div class="h-px bg-gray-300 flex-1" />
              </div>
              <div class="flex items-center gap-2">
                <span
                  class="material-symbols-outlined text-gray-500 border-[0.5px] border-gray-500 rounded-md cursor-pointer"
                  @click="maximizeCallback">
                  expand_content
                </span>
                <span class="material-symbols-outlined cursor-pointer" @click="closeCallback">
                  close
                </span>
              </div>
            </div>
            <div class="my-4">
              <div class="grid grid-cols-1 lg:grid-cols-4 gap-2">
                <InputField class="col-span-auto lg:col-span-3" label="คำค้นหา"
                  v-model.trim="partnerDialogStore.searchCriteria.keyword" hideDetails />
                <Select class="cols-start-auto lg:col-start-1 mt-4" label="ประเภท"
                  v-model="partnerDialogStore.searchCriteria.type" :options="VendorConstants.vendorTypeOptions"
                  hideDetails />
                <Select class="mt-4" label="ประเภทผู้ประกอบการ"
                  v-model="partnerDialogStore.searchCriteria.entrepreneurType"
                  :options="partnerDialogStore.entrepreneurTypeOptions" hideDetails />
                <div class="col-start-auto lg:col-start-4 flex gap-2 justify-start lg:justify-end items-center mt-4">
                  <ButtonSearch class="lg:w-fit w-full" @click="partnerDialogStore.onGetPartnerListDataAsync" />
                  <ButtonClear class="lg:w-fit w-full" @click="partnerDialogStore.onResetCriteriaAsync" />
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="p-4">
          <DataView class="mt-5" :value="partnerDialogStore.table.data" data-key="id">
            <template #list="{ items }">
              <div v-for="(item, index) in items" :key="index"
                class="border-1 border-gray-300 rounded-sm mb-4 p-1 pl-2 pr-4">
                <div class="grid lg:grid-cols-12 gap-2">
                  <div class="lg:col-span-8">
                    <InfoRow label="ประเภท" :label-span="5" :content-span="7">
                      <p>{{ VendorConstants.typeNameByCode(item.type) }}</p>
                    </InfoRow>
                    <InfoRow label="ประเภทผู้ประกอบการ" :label-span="5" :content-span="7">
                      <p>{{ item.entrepreneurType }}</p>
                    </InfoRow>
                    <InfoRow label="เลขประจำตัวผู้เสียภาษีอากร เลขประจำตัวประชาชน (ถ้ามี)" :label-span="5"
                      :content-span="7">
                      <p>{{ item.taxpayerIdentificationNo }}</p>
                    </InfoRow>
                    <InfoRow label="ชื่อบริษัท/ชื่อ - นามสกุล" :label-span="5" :content-span="7">
                      <p class="font-bold">{{ item.establishmentName }}</p>
                    </InfoRow>
                  </div>
                  <div class="flex flex-col justify-center items-end lg:col-span-4">
                    <Button class="w-full mb-4 lg:mb-0 lg:max-w-[70px]" label="เลือก" severity="primary"
                      variant="outlined" @click="() => onSelected(item)" />
                  </div>
                </div>
              </div>
            </template>
            <template #empty>
              <p class="text-center font-bold">ไม่พบข้อมูล</p>
            </template>
          </DataView>
        </div>
      </div>
      <div class="mb-4">
        <Pagination :pageNumber="partnerDialogStore.searchCriteria.pageNumber"
          :pageSize="partnerDialogStore.searchCriteria.pageSize" :total-record="partnerDialogStore.table.totalRecords"
          @change="partnerDialogStore.onChangePageSize" />
      </div>
    </template>
  </Dialog>
</template>

<style lang="css" scoped>
.hide-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>