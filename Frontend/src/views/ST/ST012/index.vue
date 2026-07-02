<script setup lang="ts">
import { onBeforeMount, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import { Form } from 'vee-validate';
import { useSt012ListStore } from '@/stores/ST/st012';
import { InputField } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '@/components/Pagination.vue';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';

const router = useRouter();
const listStore = useSt012ListStore();

onBeforeMount((): void => {
  listStore.onResetCriteria();
});

onMounted(async (): Promise<void> => {
  await listStore.onGetListData();
});

watch(
  (): number[] => [listStore.searchCriteria.pageNumber, listStore.searchCriteria.pageSize],
  async (): Promise<void> => {
    await listStore.onGetListData();
  }
);

const onDeleteAsync = async (id: string): Promise<void> => {
  if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

  await listStore.onDeleteByIdAsync(id);
};
</script>

<template>
  <TitleHeader label="อำเภอ/เขต">
    <template #action>
      <Button label="เพิ่มอำเภอ/เขต" icon="pi pi-plus" severity="primary"
        @click="() => router.push({ name: 'st012Detail' })" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetListData">
        <div class="mt-6 space-y-4 lg:space-y-2">
          <div class="grid grid-cols-1 lg:grid-cols-6 gap-8 lg:gap-2">
            <InputField label="ค้นหา" v-model.trim="listStore.searchCriteria.keyword" class="lg:col-span-3" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-6 gap-8 lg:gap-2">
            <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end lg:col-end-7">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="() => listStore.onResetCriteria()" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card>
    <template #content>
      <DataTable :value="listStore.table.data" dataKey="id" showGridlines
        :pt="{ headerRow: { class: 'bg-gray-200 text-gray-900' } }">
        <Column style="width: 6rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center">ลำดับ</p>
          </template>
          <template #body="{ index }">
            <p class="text-center">
              {{ (listStore.searchCriteria.pageNumber - 1) * listStore.searchCriteria.pageSize + index + 1 }}
            </p>
          </template>
        </Column>
        <Column field="code" style="width: 8rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold text-center">รหัส</p>
          </template>
          <template #body="{ data }">
            <p>{{ data.code }}</p>
          </template>
        </Column>
        <Column field="nameTh" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold">อำเภอ/เขต</p>
          </template>
          <template #body="{ data }">
            <p class="font-bold">{{ data.nameTh }}</p>
          </template>
        </Column>
        <Column field="nameEn" headerClass="bg-gray-200 !text-black font-bold">
          <template #header>
            <p class="w-full font-bold">อำเภอ/เขต (EN)</p>
          </template>
          <template #body="{ data }">
            <p>{{ data.nameEn }}</p>
          </template>
        </Column>
        <Column style="width: 8rem" headerClass="bg-gray-200 !text-black font-bold">
          <template #body="{ data }">
            <div class="flex items-center justify-center gap-1.5">
              <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                size="small" variant="text"
                @click="() => router.push({ name: 'st012Detail', params: { id: data.id } })" />
              <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                size="small" variant="text" @click="() => onDeleteAsync(data.id)" />
            </div>
          </template>
        </Column>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataTable>
      <Pagination :page-number="listStore.searchCriteria.pageNumber" :page-size="listStore.searchCriteria.pageSize"
        :total-record="listStore.table.totalRecords" @change="listStore.onChangePageSize" />
    </template>
  </Card>
</template>
