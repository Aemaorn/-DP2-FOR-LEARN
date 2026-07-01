<script setup lang="ts">
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { CriteriaGroupButton, InputField, Select, StatusGroupButton } from '@/components/forms';
import { SharedConstants } from '@/constants';
import { formatCurrency } from '@/helpers/currency';
import { useMenuStore } from '@/stores/menu';
import { usePcm006ListStore } from '@/stores/PCM/pcm006';
import { Form } from 'vee-validate';
import { onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import Pcm006Constant from '../../../constants/pcm006';
import { EPcm006Status } from '@/enums/pcm006';
import { BadgeStatus } from '@/components';
import type { TPcm006ListItems } from '@/models/PCM/pcm006';
import { ToDateOnly } from '@/helpers/dateTime';

const menuStore = useMenuStore();
const rounter = useRouter();

const { WorkProcessOptions } = SharedConstants;

const { Pcm006StatusColor, Pcm006StatusName } = Pcm006Constant;

const store = usePcm006ListStore();

const navagateToDetail = (id?: string) => {
  rounter.push({ name: 'pcm006Detail', params: { id: id } })
};

onMounted(async () => {
  await Promise.all([
    store.api.getDropdownAsync(),
    store.api.onGetList(),
  ])
});

watch(() => store.criteria.workProcess, () => {
  store.criteria.status = EPcm006Status.All;
  store.criteria.pageNumber = 1;
});

watch(() => [store.criteria.workProcess, store.criteria.status, store.criteria.pageNumber, store.criteria.pageSize], async () => {
  await store.api.onGetList();
})
</script>

<template>
  <div class="flex flex-col gap-4">
    <TitleHeader label="รายการเบิกชดเชยเงินสดย่อย">
      <template #action>
        <Button label="เพิ่มรายการเบิกชดเชยเงินสดย่อย" icon="pi pi-plus" severity="primary" variant="outlined"
          class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="navagateToDetail()" v-if="menuStore.hasManage" />
      </template>
    </TitleHeader>
  </div>

  <Card class="mt-4">
    <template #content>
      <Form @submit="store.api.onGetList()">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="store.criteria.workProcess" />
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-x-2 lg:gap-y-6">
            <InputField class="lg:col-span-3" hide-details label="คำค้นหา" v-model.trim="store.criteria.keyword" />
            <Select class="lg:col-start-1" hide-details label="ฝ่าย/ภาค เขต" :options="store.departmentDropDown"
              v-model="store.criteria.departmentCode" />
            <div class="lg:col-span-4 flex items-end justify-end gap-2">
              <ButtonSearch class="lg:w-fit w-full" type="submit" />
              <ButtonClear class="lg:w-fit w-full" @click="store.fn.onClearCirteria" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>

  <Card class="mt-4">
    <template #content>
      <StatusGroupButton :optionBadges="store.statusOptionBadge" v-model="store.criteria.status" />
      <DataView :value="store.dataTabel.data?.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in (items as Array<TPcm006ListItems>)" :key="index"
            class="border-1 border-gray-300 rounded-sm mb-2 p-1">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 px-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่">
                  <p class="underline text-blue-400 cursor-pointer w-fit" @click="navagateToDetail(data.id)">
                    {{ data.number }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ขอเบิก">
                  <p class="font-bold">
                    {{ ToDateOnly(data.reimbursementDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="เรื่อง">
                  <p class="font-bold">
                    {{ data.subject }}
                  </p>
                </InfoRow>
                <InfoRow label="ฝ่าย/ภาคเขต">
                  <p>{{ data.departmentName }}</p>
                </InfoRow>
                <InfoRow label="จำนวนเงินรวม">
                  <p>
                    {{ formatCurrency(data.totalAmount) }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus :label="Pcm006StatusName(data.status)"
                    :bg-color-class="Pcm006StatusColor(data.status).bgColorClass"
                    :text-color-class="Pcm006StatusColor(data.status).textColorClass" />
                </div>
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  size="small" variant="text" @click="() => navagateToDetail(data.id)" />
                <Button icon="pi pi-trash" class="text-red-600!  hover:bg-red-300/20! focus:bg-red-300/20!" size="small"
                  variant="text"
                  v-if="[EPcm006Status.Draft, EPcm006Status.Edit, EPcm006Status.Rejected].includes(data.status) && menuStore.hasManage"
                  @click="store.api.onDeleteAsync(data.id)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
        :total-record="store.dataTabel.data?.totalRecords" />
    </template>
  </Card>
</template>
