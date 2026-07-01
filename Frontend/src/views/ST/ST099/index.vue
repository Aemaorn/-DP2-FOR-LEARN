<script setup lang="ts">
import { onBeforeMount, onMounted, watch } from 'vue';
import { Button, Card, DataView } from 'primevue';
import { useRouter } from 'vue-router';
import { InputField } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '@/components/Pagination.vue';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { useST099ListStore } from '@/stores/ST/st099';

const router = useRouter();
const listStore = useST099ListStore();
const menuStore = useMenuStore();

onBeforeMount((): void => {
  listStore.onResetCriteria();
});

onMounted(async (): Promise<void> => {
  await listStore.onGetListAsync();
});

watch((): number[] => [listStore.searchCriteria.pageNumber, listStore.searchCriteria.pageSize], async (): Promise<void> => {
  await listStore.onGetListAsync();
});
</script>

<template>
  <TitleHeader label="รายการคำสั่ง">
    <template #action>
      <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'st099Detail' })"
        v-if="menuStore.hasPermission" />
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetListAsync">
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="ค้นหา" v-model.trim="listStore.searchCriteria.keyword" />
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
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-8">
              <div class="lg:col-span-4">
                <InfoRow label="เลขที่">
                  <p>{{ data.id ?? '-' }}</p>
                </InfoRow>
                <InfoRow label="refBankOrder">
                  <p>{{ data.refBankOrder ?? '-' }}</p>
                </InfoRow>
                <InfoRow label="maximumBudget">
                  <p>{{ data.maximumBudget ?? '-' }}</p>
                </InfoRow>
                <InfoRow label="remark">
                  <p>{{ data.entrepreneurType ?? '-' }}</p>
                </InfoRow>
                <InfoRow label="supplyMethodCode">
                  <p>{{ data.supplyMethodCode ?? '-' }}</p>
                </InfoRow>
                <InfoRow label="supplyMethodSpecialTypeCode">
                  <p>{{ data.supplyMethodSpecialTypeCode ?? '-' }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text"
                  @click="() => router.push({ name: 'st099Detail', params: { id: data.id } })"
                  v-if="menuStore.hasPermission" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text" @click="() => listStore.onDeleteByIdAsync(data.id)" v-if="menuStore.hasPermission" />
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
