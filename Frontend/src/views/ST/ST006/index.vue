<script setup lang="ts">
import { useSt006ListStore } from '@/stores/ST/st006';
import { onMounted, watch } from 'vue';
import { Form as VeeForm } from 'vee-validate';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import Pagination from '@/components/Pagination.vue';
import router from '@/router';
import { useMenuStore } from '@/stores/menu';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { InputField } from '@/components/forms';

const store = useSt006ListStore();
const menuStore = useMenuStore();

onMounted(async (): Promise<void> => {
  await Promise.all([store.onGetGroupDropdownAsync(), store.onGetListAsync()]);
});

const routeToDetail = (id?: string): void => {
  const route = '/st/st006/detail';
  const finalRoute = id ? `${route}/${id}` : route;

  router.push(finalRoute);
};

watch(
  (): string | undefined => store.criteria.group,
  async (groupCode): Promise<void> => {
    const findData = store.groupDropdown.find((g): boolean => g.value === groupCode);

    if (findData) {
      return await store.onGetSubGroupDropdownAsync(findData.id);
    }

    store.subGroupDropdown = [];
  }
);
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="พารามิเตอร์">
      <template #action>
        <Button label="เพิ่มรายการ" icon="pi pi-plus" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="() => routeToDetail()" v-if="menuStore.hasPermission" />
      </template>
    </TitleHeader>
    <Card>
      <template #content>
        <VeeForm @submit="store.onGetListAsync">
          <div class="mt-10 space-y-8 lg:space-y-10">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
              <Select label="กลุ่ม" class="w-full" :options="store.groupDropdown" v-model="store.criteria.group"
                @enterClose="store.onGetListAsync" />
              <InputField label="ค่าพารามิเตอร์" class="w-full" v-model.trim="store.criteria.parameter" />
            </div>
            <div class="grid grid-cols-2 gap-2 lg:flex lg:items-center lg:justify-end">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="store.onClearCriteriaSearch" />
            </div>
          </div>
        </VeeForm>
      </template>
    </Card>
    <Card>
      <template #content>
        <DataView :value="store.dataList.data" data-key="id">
          <template #list="{ items }">
            <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1">
              <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
                <div class="lg:col-span-8">
                  <InfoRow label="กลุ่ม">
                    <p>{{ data.group }}</p>
                  </InfoRow>
                  <InfoRow label="กลุ่มย่อย">
                    <p>{{ data.subGroup ?? '-' }}</p>
                  </InfoRow>
                  <InfoRow label="ค่าพารามิเตอร์">
                    <p>{{ data.parameter }}</p>
                  </InfoRow>
                  <InfoRow label="ลำดับ">
                    <p>{{ data.sequence }}</p>
                  </InfoRow>
                </div>
                <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                  <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                    size="small" variant="text" @click="() => routeToDetail(data.id)" v-if="menuStore.hasPermission" />
                  <Button icon="pi pi-trash" class="text-red-600! hover:bg-red-300/20! focus:bg-red-300/20!"
                    size="small" variant="text" @click="() => store.onDeleteAsync(data.id)"
                    v-if="menuStore.hasPermission" />
                </div>
              </div>
            </div>
          </template>
          <template #empty>
            <p class="text-center">ไม่พบข้อมูล</p>
          </template>
        </DataView>
        <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
          :total-record="store.dataList.totalRecords" @change="store.onChangePageSize" />
      </template>
    </Card>
  </div>
</template>
