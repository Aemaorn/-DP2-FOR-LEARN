<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { Form as VeeForm } from 'vee-validate';
import { storeToRefs } from 'pinia';
import { TitleHeader } from '@/components/cosmetic';
import { AutoCompleteField, InputField } from '@/components/forms';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import Pagination from '@/components/Pagination.vue';
import { useSt009ListStore } from '@/stores/ST/st009';
import { useMenuStore } from '@/stores/menu';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const router = useRouter();
const store = useSt009ListStore();
const menuStore = useMenuStore();
const { searchCriteria, table, supplyMethodOptions, supplyMethodSpecialTypeOptions } = storeToRefs(store);

const onDeleteAsync = async (id: string): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;
  await store.onDeleteAsync(id);
};

onMounted(async (): Promise<void> => {
  await store.onGetDropdownOptionsAsync();
  await store.onGetListAsync();
});

watch(
  (): number[] => [searchCriteria.value.pageNumber, searchCriteria.value.pageSize],
  async (): Promise<void> => {
    await store.onGetListAsync();
  }
);

watch(
  (): string | undefined => searchCriteria.value.supplyMethodCode,
  async (code: string | undefined, oldCode: string | undefined): Promise<void> => {
    if (oldCode !== undefined) {
      searchCriteria.value.supplyMethodSpecialTypeCode = undefined;
    }
    await store.onGetSupplyMethodSpecialTypeOptionsAsync(code);
  }
);
</script>

<template>
  <TitleHeader label="กำหนดอำนาจอนุมัติ">
    <template #action>
      <Button
        v-if="menuStore.hasPermission"
        label="เพิ่มรายการ"
        icon="pi pi-plus"
        size="small"
        severity="primary"
        variant="outlined"
        @click="router.push({ name: 'st009Detail' })"
      />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <VeeForm @submit="store.onGetListAsync">
        <div class="mt-10 grid grid-cols-1 lg:grid-cols-3 gap-8 lg:gap-x-2 lg:gap-y-8">
          <InputField label="คำค้นหา" v-model.trim="searchCriteria.keyword" hide-details class="lg:col-span-2" />
          <div class="hidden lg:block" />
          <AutoCompleteField label="วิธีการจัดหา" v-model="searchCriteria.supplyMethodCode" :options="supplyMethodOptions" />
          <AutoCompleteField v-model="searchCriteria.supplyMethodSpecialTypeCode" :options="supplyMethodSpecialTypeOptions" />
          <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end">
            <ButtonSearch type="submit" class="lg:w-fit w-full" />
            <ButtonClear @click="store.onResetCriteria" class="lg:w-fit w-full" />
          </div>
        </div>
      </VeeForm>
    </template>
  </Card>

  <Card>
    <template #content>
      <DataView :value="table.data" data-key="id">
        <template #list="{ items }">
          <div
            v-for="(data, index) in items"
            :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1"
          >
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12">
              <div class="lg:col-span-10">
                <InfoRow label="คำสั่งธนาคาร">
                  <p class="font-semibold">{{ data.refBankOrder }}</p>
                </InfoRow>
                <InfoRow label="วงเงินสูงสุด">
                  <p>{{ data.maximumBudget.toLocaleString('th-TH', { minimumFractionDigits: 2 }) }}</p>
                </InfoRow>
                <InfoRow label="วิธีจัดหา">
                  <p>{{ data.supplyMethod ?? '-' }}
                    <span v-if="data.supplyMethodSpecialType">
                      : {{ data.supplyMethodSpecialType }}
                    </span>
                  </p>
                </InfoRow>
                <InfoRow label="หมายเหตุ">
                  <p>{{ data.remark ?? '-' }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end lg:col-span-2 mb-2 lg:mb-0">
                <Button
                  icon="pi pi-pen-to-square"
                  class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small"
                  variant="text"
                  @click.stop="() => router.push({ name: 'st009Detail', params: { id: data.id } })"
                />
                <Button
                  v-if="menuStore.hasPermission"
                  icon="pi pi-trash"
                  class="text-red-500! hover:bg-red-300/20! focus:bg-red-300/20!"
                  size="small"
                  variant="text"
                  @click.stop="onDeleteAsync(data.id)"
                />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination
        :page-number="searchCriteria.pageNumber"
        :page-size="searchCriteria.pageSize"
        :total-record="table.totalRecords"
        @change="store.onChangePageSize"
      />
    </template>
  </Card>
</template>
