<script setup lang="ts">
import { TitleHeader, CardSelect } from '@/components/cosmetic';
import { defineAsyncComponent, onMounted, ref, watch } from 'vue';
import { usePcmContractDraftStore } from '@/stores/PCM/PCM005/pcmContractDraft';
import { useContractDraftStore } from '@/views/PP/stores/PP0010/ContractDraft';
import { showActivityDialog, showConfirmDialogAsync } from '@/helpers/dialog';
import { ConfirmDialogType } from '@/enums/dialog';
import { BadgeStatus as BadgeComponent } from '@/components';
import ContractDraftHelper from '@/helpers/contractDraft';
import { useRoute } from 'vue-router';

const route = useRoute();

const TabsSelect = defineAsyncComponent(() => import('./Sub/TabSelect.vue'));

const store = usePcmContractDraftStore();

const ppContractDraftStore = useContractDraftStore();

const selectKey = ref(0);

const { BadgeStatus } = ContractDraftHelper;

const getContractDraftByVendorId = async (vendorId: string) => {
  const isDeepEqual = (a: any, b: any) => JSON.stringify(a) === JSON.stringify(b);

  const isChanged = !isDeepEqual(store.body, store.cloneBody);

  if (isChanged) {
    const confirmed = await showConfirmDialogAsync(ConfirmDialogType.ConfirmData);

    if (store.vendorId && !confirmed) {
      selectKey.value++;
      return;
    }
  }

  store.vendorId = vendorId;
  selectKey.value = 0;
  store.onClearBody();

  await store.api.getContractDraftByVendorIdAsync();
};

onMounted(async () => {
  const vendorIdParam = route.query.vendorId as string | undefined;

  await Promise.all([
    ppContractDraftStore.api.getPeriodTypeAsync(),
    ppContractDraftStore.api.getPeriodConditionTypeAsync(),
    ppContractDraftStore.api.getRCCRTypeAsync(),
    ppContractDraftStore.api.getFineTypeAsync(),
    ppContractDraftStore.api.getPayTypeAsync(),
    ppContractDraftStore.api.getUnitTypeAsync(),
    ppContractDraftStore.api.getVatTypeAsync(),
    ppContractDraftStore.api.getUnitMeaTypeAsync(),
    ppContractDraftStore.api.getWarrantyTypeAsync(),
    ppContractDraftStore.api.getConditionTypeOptions(),
  ]);

  await store.api.getVendorListAsync(vendorIdParam);
});

watch(() => store.body.template, async (newVal) => {
  await store.api.getCmRentalTypeAttacement(newVal);
});
</script>

<template>
  <div class="mt-4">
    <div class="my-2">
      <TitleHeader label="ข้อมูลสัญญา">
        <template #action>
          <BadgeComponent :label="BadgeStatus(store.body.contractStatus).label"
            :color="BadgeStatus(store.body.contractStatus).color" />
        </template>
      </TitleHeader>
    </div>

    <div class="flex items-start justify-between bg-[#F7F7F7]">
      <div class="max-w-[65dvw]">
        <CardSelect v-if="store.vendorList.length > 1" :key="selectKey" :items="store.vendorList"
          :value="store.vendorId" @select="(e) => getContractDraftByVendorId(e.toString())">
          <template #badge="{ item }">
            <BadgeComponent v-if="item.status" :label="BadgeStatus(item.status as any).label"
              :color="BadgeStatus(item.status as any).color" size="sm" />
          </template>
        </CardSelect>
      </div>
      <div v-if="store.vendorId" class="flex items-center justify-end gap-2 pt-3">
        <BadgeComponent :label="BadgeStatus(store.body.status).label" :color="BadgeStatus(store.body.status).color" />
        <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
          class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.vendorId!)" />
      </div>
    </div>

    <div v-if="store.vendorId">
      <TabsSelect />
    </div>
  </div>
</template>