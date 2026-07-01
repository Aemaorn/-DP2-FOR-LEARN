<script setup lang="ts">
import { BadgeStatus, Pagination } from '@/components';
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import InfoRow from '@/components/cosmetic/InfoRow.vue';
import { CriteriaGroupButton, Datepicker, InputField, Select, StatusGroupButton } from '@/components/forms';
import { SharedConstants } from '@/constants';
import cm005Constant from '@/constants/CM/cm005';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { useCM005ListStore } from '@/stores/CM/cm005';
import { useMenuStore } from '@/stores/menu';
import { Form } from 'vee-validate';
import { onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const menuStore = useMenuStore();
const store = useCM005ListStore();
const { WorkProcessOptions } = SharedConstants;
const { cm005StatusName, cm005ColorClass } = cm005Constant;

onMounted(() => {
  initAsync();
});

const initAsync = async (): Promise<void> => {
  await store.getContractTypeDDLAsync();
  await store.getListAsync();
};

const navigateToDetail = (contractId: string, id: string): void => {
  router.push(`/cm/cm005/contract/${contractId}/detail/${id}`);
};

const openPlanDetail = (id: string | number, procurementType: string) => {
  if (procurementType === 'Rent') {
    router.push({ name: 'pcm005Detail', params: { id: id } })

    return;
  }

  router.push({ name: 'ppDetail', params: { id: id } })
};

watch(() => [
  store.criteria.pageNumber,
  store.criteria.pageSize,
  store.criteria.workProcess,
  store.criteria.status], async () => {
    await store.getListAsync();
  });
</script>

<template>
  <TitleHeader label="บอกเลิกสัญญา">
    <template #action>
      <Button icon="pi pi-plus" label="เพิ่มรายการบอกเลิกสัญญา" severity="primary" variant="outlined"
        class="hover:bg-red-200 hover:text-red-900 bg-red-50" @click="router.push('/cm/cm005/contract-selected')"
        v-if="menuStore.hasManage" />
    </template>
  </TitleHeader>

  <Card class="mb-4">
    <template #content>
      <Form @submit="store.getListAsync">
        <CriteriaGroupButton :options="WorkProcessOptions" v-model="store.criteria.workProcess" />
        <div class="mt-10 space-y-8 lg:space-y-10">
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <InputField label="คำค้นหา" class="lg:col-span-3" v-model.trim="store.criteria.keyword" hide-details />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2">
            <Datepicker label="วันที่ลงนามในสัญญา" v-model="store.criteria.contractSignedDate"
              hide-details />
            <Select :options="store.contractTypeDDL" label="ประเภทสัญญา" v-model="store.criteria.contractType"
              hide-details @enterClose="store.getListAsync" />
            <div class="lg:col-span-3 flex items-end justify-end gap-2">
              <ButtonSearch type="submit" class="w-full lg:w-fit" />
              <ButtonClear @click="store.onClearCriteriaAsync" class="w-full lg:w-fit" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>
  <Card class="mb-4">
    <template #content>
      <StatusGroupButton :optionBadges="store.badgeOptions" v-model="store.criteria.status" />
      <DataView :value="store.dataList?.data?.data" data-key="id">
        <template #list="{ items }">
          <div v-for="(data, index) in items" :key="index" class="border-1 border-gray-300 rounded-sm mb-2 p-1 px-3">
            <div class="flex flex-col-reverse lg:grid lg:grid-cols-12 gap-2">
              <div class="lg:col-span-8">
                <InfoRow label="เลขที่สัญญา">
                  <p class="underline text-blue-400 cursor-pointer"
                    @click="() => openPlanDetail(data.procurementId, data.procurementType)">
                    {{ data.contractNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="เลขที่ PO (SAP)">
                  <p>
                    {{ data.poNumber }}
                  </p>
                </InfoRow>
                <InfoRow label="ชื่อสัญญา">
                  <p class="font-bold">
                    {{ data.contractName }}
                  </p>
                </InfoRow>
                <InfoRow label="คู่สัญญา">
                  <p>
                    {{ data.entrepreneurName }}
                  </p>
                </InfoRow>
                <InfoRow label="วันที่ลงนามในสัญญา">
                  <p>
                    {{ ToDateOnly(data.contractSignedDate) }}
                  </p>
                </InfoRow>
                <InfoRow label="วงเงินตามสัญญา">
                  <p>
                    {{ formatCurrency(data.budget) }}
                  </p>
                </InfoRow>
                <InfoRow label="ประเภทสัญญา">
                  <p>
                    {{ data.contractTypeName }}
                  </p>
                </InfoRow>
              </div>
              <div class="flex items-start justify-end gap-1.5 lg:col-span-4 mb-2 lg:mb-0">
                <div class="flex items-center gap-2 mt-2 mr-2">
                  <p class="text-sm">สถานะ :</p>
                  <BadgeStatus :bg-color-class="cm005ColorClass(data.status).bgColorClass"
                    :text-color-class="cm005ColorClass(data.status).textColorClass"
                    :label="cm005StatusName(data.status)" />
                </div>
                <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!"
                  variant="text" @click="() => navigateToDetail(data.contractId, data.id)" />
              </div>
            </div>
          </div>
        </template>
        <template #empty>
          <p class="text-center">ไม่พบข้อมูล</p>
        </template>
      </DataView>
      <Pagination :page-number="store.criteria.pageNumber" :page-size="store.criteria.pageSize"
        :total-record="store.dataList?.data?.totalRecords" @change="store.onChangePageSizeAsync" />
    </template>
  </Card>
</template>
