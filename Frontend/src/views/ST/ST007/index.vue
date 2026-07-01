<script setup lang="ts">
import { onMounted, watch } from 'vue';
import { Button, Card, DataView } from 'primevue';
import { useRouter } from 'vue-router';
import { useSt007ListStore } from '@/stores/ST/st007';
import { InputField, Select } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '@/components/Pagination.vue';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const router = useRouter();
const listStore = useSt007ListStore();
const { groupDropdown } = storeToRefs(listStore);
const menuStore = useMenuStore();

onMounted(async (): Promise<void> => {
  await listStore.onGetListData();
});

watch((): number[] => [listStore.searchCriteria.pageNumber, listStore.searchCriteria.pageSize], async (): Promise<void> => {
  await listStore.onGetListData();
});

const onDeleteAsync = async (id: string): Promise<void> => {
  if (!await showConfirmDialogAsync(ConfirmDialogType.Delete)) return;

  await listStore.onDeleteAsync(id);
};

const getLabelGroupDropdownByValue = (value: string): string => {
  const option = groupDropdown.value.find((item): boolean => item.value === value);
  return option ? option.label : '';
};

</script>

<template>
  <TitleHeader label="จัดการรูปแบบเอกสาร">
    <template #action>
      <Button label="เพิ่มรูปแบบเอกสาร" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'st007Detail' })"
        v-if="menuStore.hasPermission" />
    </template>
  </TitleHeader>

  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetListData">
        <div class="mt-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="กลุ่ม" v-model="listStore.searchCriteria.group" :options="groupDropdown"
              @enterClose="listStore.onGetListData" />
            <InputField label="รูปแบบเอกสาร" v-model.trim="listStore.searchCriteria.name" />
            <div class="lg:col-span-3 flex items-end justify-end gap-2">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="listStore.onResetCriteria" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>

  <Card>
    <template #content>
      <DataView :value="listStore.table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12">
              <div class="lg:col-span-8">
                <InfoRow label="กลุ่ม">
                  <p>{{ getLabelGroupDropdownByValue(data.group) }}</p>
                </InfoRow>
                <InfoRow label="รหัส">
                  <p class="font-extrabold">{{ data.code }}</p>
                </InfoRow>
                <InfoRow label="รูปแบบเอกสาร">
                  <p>{{ data.name }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text"
                  @click="() => router.push({ name: 'st007Detail', params: { id: data.id } })"
                  v-if="menuStore.hasPermission" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="onDeleteAsync(data.id)" v-if="menuStore.hasPermission" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="listStore.searchCriteria.pageNumber" :page-size="listStore.searchCriteria.pageSize"
        :total-record="listStore.table.totalRecords" @change="listStore.onChangePageSize" />
    </template>
  </Card>
</template>
