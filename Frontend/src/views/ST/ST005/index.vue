<script setup lang="ts">
import { InputField, Select, Checkbox } from '@/components/forms';
import { TitleHeader } from '@/components/cosmetic';
import Pagination from '@/components/Pagination.vue';
import StatusChip from '@/components/StatusChip.vue';
import Card from 'primevue/card';
import DataView from 'primevue/dataview';
import { useSt005ListStore } from '@/stores/ST/st005';
import { ToDateTime } from '@/helpers/dateTime';
import { useRouter } from 'vue-router';
import { onMounted, watch } from 'vue';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import InfoRow from '@/components/cosmetic/InfoRow.vue';

const listStore = useSt005ListStore();
const menuStore = useMenuStore();

const router = useRouter();

onMounted(async (): Promise<void> => {
  await listStore.getDropDownDepartment();
  await listStore.onGetListAsync();
});

watch(
  (): number[] => [listStore.searchCriteria.pageNumber, listStore.searchCriteria.pageSize],
  async (): Promise<void> => {
    await listStore.onGetListAsync();
  }
);
</script>

<template>
  <TitleHeader label="จัดการผู้ใช้งาน">
    <template #action>
      <Button label="เพิ่มผู้ใช้งาน" icon="pi pi-plus" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="() => router.push({ name: 'st005Detail' })"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>

  <Card class="my-4">
    <template #content>
      <Form @submit="listStore.onGetListAsync">
        <div class="mt-10 space-y-2 lg:space-y-4">
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-2">
            <InputField label="คำค้นหา"
              v-model.trim="listStore.searchCriteria.searchText" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Select label="ฝ่าย/ภาคเขต" :options="listStore.dropdowns.department"
              v-model="listStore.searchCriteria.departmentCode" @enterClose="listStore.onGetListAsync" />
            <div class="flex items-end">
              <Checkbox label="ใช้งาน" binary v-model="listStore.searchCriteria.isActive" />
            </div>
            <div class="lg:col-span-3 flex items-end justify-end gap-2">
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
      <DataView :value="listStore.table.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm p-3 mb-4">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-4">
                <InfoRow label="ชื่อ-นามสกุล ">
                  <p class="font-semibold">{{ data.name }}</p>
                </InfoRow>
                <InfoRow label="ตำแหน่ง">
                  <p class="">{{ data.positionName }}</p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p class="">{{ data.departmentName }}</p>
                </InfoRow>
              </div>

              <div class="lg:col-span-4">
                <InfoRow label="อีเมล">
                  <p class="">{{ data.email }}</p>
                </InfoRow>
                <InfoRow label="ชื่อผู้แก้ไข">
                  <p class="">{{ data.lastModifiedByName }}</p>
                </InfoRow>
                <InfoRow label="วันที่แก้ไข">
                  <p class="">{{ ToDateTime(data.lastModifiedAt) }}</p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <StatusChip :label="data.isActive ? 'ใช้งาน' : 'ไม่ใช้งาน'"
                    :color="data.isActive ? 'Success' : 'Error'" class="h-fit text-nowrap" />
                  <StatusChip v-if="data.isLockedOut" label="ถูกล็อค" color="Error" class="h-fit text-nowrap" />
                </div>
                <Button v-if="data.isLockedOut && menuStore.hasManage" icon="pi pi-unlock" title="ปลดล็อคบัญชี"
                  class="text-amber-600! h-fit hover:bg-amber-300/20! focus:bg-amber-300/20!" size="small" variant="text"
                  @click="listStore.onUnlockUser(data.id)" />
                <Button icon="pi pi-pen-to-square"
                  class="text-blue-600! h-fit hover:bg-blue-300/20! focus:bg-blue-300/20!" size="small" variant="text"
                  @click="router.push({ name: `st005Detail`, params: { id: data.id } })"
                  v-if="menuStore.hasPermission" />
                <Button icon="pi pi-trash" class="text-red-600! h-fit hover:bg-red-300/20! focus:bg-red-300/20!"
                  size="small" variant="text" @click="listStore.onDeleteUser(data.id)" v-if="menuStore.hasManage" />
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
