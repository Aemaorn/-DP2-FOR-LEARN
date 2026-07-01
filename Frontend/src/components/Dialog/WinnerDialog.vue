<script setup lang="ts">
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { InputField, Select } from '@/components/forms';
import { VendorConstants } from '@/constants';
import { formatCurrency } from '@/helpers/currency';
import { useWinnerDialogStore } from '@/stores/Shared/winnerDialog';
import { useEventListener } from '@vueuse/core';
import { Button, Dialog } from 'primevue';
import { Form } from 'vee-validate';
import { onMounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import InfoRow from '../cosmetic/InfoRow.vue';
import Pagination from '../Pagination.vue';

const store = useWinnerDialogStore();
const route = useRoute();


onMounted(async () => {
  useEventListener(document, 'onShowWinnerDialog', async (): Promise<void> => { });
});


const afterCloseModal = (): void => {
  store.winnerListReponse = {
    data: [],
    totalRecords: 0,
  };
};

watch(() => route.path, () => {
  store.isShow = false;
});
</script>

<template>
  <Dialog v-model:visible="store.isShow" modal :style="{ width: '80vw' }" :draggable="false"
    @afterHide="afterCloseModal" maximizable>
    <template #container="{ closeCallback, maximizeCallback }">
      <Form @submit="store.getListAsync" class="my-4">
        <div class="max-h-[calc(100vh-200px)] overflow-y-auto">
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
                <div class="grid grid-cols-1 lg:grid-cols-2 items-center gap-2 mt-8">
                  <InputField class="col-span-auto" label="คำค้นหา" v-model="store.criteria.keyword" hideDetails />
                  <Select class="cols-start-auto" label="ประเภท" v-model="store.criteria.type"
                    :options="VendorConstants.vendorTypeOptions" hideDetails />
                  <div class="col-start-auto lg:col-start-2 flex gap-2 justify-start lg:justify-end items-center mt-4">
                    <ButtonSearch class="lg:w-fit w-full" type="submit" />
                    <ButtonClear class="lg:w-fit w-full" @click="store.onClearCriteriaAsync" />
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div class="px-4">
            <DataView class="mt-5" :value="store.winnerListReponse.data" data-key="id">
              <template #list="{ items }">
                <div v-for="(item, index) in items" :key="index"
                  class="border-1 border-gray-300 rounded-sm mb-4 p-1 pl-2 pr-4">
                  <div class="grid lg:grid-cols-12 gap-2">
                    <div class="lg:col-span-8 flex flex-col gap-4">
                      <InfoRow label="ประเภท" :label-span="8" :content-span="4">
                        <p>{{ VendorConstants.typeNameByCode(item.type) }}</p>
                      </InfoRow>
                      <InfoRow label="เลขประจำตัวผู้เสียภาษีอากร เลขประจำตัวประชาชน (ถ้ามี)" :label-span="8"
                        :content-span="4">
                        <p>{{ item.taxId }}</p>
                      </InfoRow>
                      <InfoRow label="ชื่อบริษัท/ชื่อ - นามสกุล" :label-span="8" :content-span="4">
                        <p>{{ item.name }}</p>
                      </InfoRow>
                      <InfoRow label="ราคาที่ตกลง" :label-span="8" :content-span="4">
                        <p class="font-bold">{{ formatCurrency(item.agreedPrice) }}</p>
                      </InfoRow>
                    </div>
                    <div class="flex flex-col justify-center items-end lg:col-span-4">
                      <Button class="w-full mb-4 lg:mb-0 lg:max-w-[70px]" label="เลือก" severity="warn"
                        variant="outlined" @click="() => store.onSelected(item)" />
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
        <Pagination :pageNumber="store.criteria.pageNumber" :pageSize="store.criteria.pageSize"
          :total-record="store.winnerListReponse.totalRecords" @change="store.onChangePageSizeAsync" />
      </Form>
    </template>
  </Dialog>
</template>

<style lang="css" scoped>
.hide-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>