<script setup lang="ts">
import { Button, Card, DataView } from 'primevue';
import { InputField } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '@/components/Pagination.vue';
import { onMounted, watch } from 'vue';
import { useSt004ListStore } from '@/stores/ST/st004';
import { useRouter } from 'vue-router';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';

const router = useRouter();
const listStore = useSt004ListStore();
const menuStore = useMenuStore();

onMounted(async (): Promise<void> => {
  await listStore.onGetPermissionListData();
});

watch((): number[] => [listStore.searchCriteria.pageNumber, listStore.searchCriteria.pageSize], async (): Promise<void> => {
  await listStore.onGetPermissionListData();
});
</script>

<template>
  <TitleHeader label="กำหนดสิทธิ์">
    <template #action>
      <Button label="เพิ่มสิทธิ์" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'st004Detail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetPermissionListData">
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="ค้นหา" v-model.trim="listStore.searchCriteria.keyword" hide-details />
          </div>
          <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end">
            <ButtonSearch class="lg:w-fit w-full" type="submit" />
            <ButtonClear class="lg:w-fit w-full" @click="() => listStore.onResetCriteria()" />
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
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="รหัสสิทธิ์">
                  <p>{{ data.code }}</p>
                </InfoRow>
                <InfoRow label="ชื่อสิทธิ์">
                  <p>{{ data.name }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text"
                  @click="() => router.push({ name: 'st004Detail', params: { code: data.code } })"
                  v-if="menuStore.hasManage || menuStore.hasView" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="() => listStore.onDeleteRoleByCode(data.code)" v-if="menuStore.hasManage" />
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