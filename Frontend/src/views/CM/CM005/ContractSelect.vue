<script setup lang="ts">
import type { ContractVendorData } from '@/models/CM/cm005';
import { ButtonSave } from '@/components/Button';
import { InfoItem, TitleHeader } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import { useCm005DetailStore } from '@/stores/CM/cm005';
import { Button, Card, InputGroupAddon } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { Form } from 'vee-validate';
import { onBeforeMount, ref } from 'vue';
import { formatCurrency } from '@/helpers/currency';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import { useMenuStore } from '@/stores/menu';
import ContractDialog from './components/ContractDialog.vue';

const menuStore = useMenuStore();
const store = useCm005DetailStore();

const showContractModal = ref(false);
const keyword = ref<string>();

const routeItems = ref<Array<MenuItem>>([
  { label: 'บอกเลิกสัญญา', url: '/cm/cm005' },
  { label: 'ข้อมูลสัญญา' },
]);

onBeforeMount(() => {
  store.onResetData();
});

const onSelectContract = (data: ContractVendorData): void => {
  store.body = {
    ...store.body,
    id: data.id,
    entrepreneurName: data.vendorName ?? '',
    entrepreneurEmail: data.vendorEmail ?? '',
    contractNumber: data.contractNumber ?? '',
    poNumber: data.poNumber ?? '',
    budget: data.contractBudget ?? 0,
    contractName: data.name ?? '',
    contractType: data.contractTypeName ?? '',
    contractTemplate: data.templateName ?? '',
    contractSignedDate: data.contractDate ?? '',
    deliveryDate: data.deliveryDate ? new Date(data.deliveryDate) : new Date(),
  };
};

const onCreateTerminationContractAsync = async (): Promise<void> => {
  if (!store.body.id) return;

  if (!await showConfirmDialogAsync(undefined, 'ต้องการยกเลิกสัญญานี้ใช่หรือไม่ ?')) return;

  await store.createAsync(store.body.id);
};
</script>

<template>
  <Form @submit="onCreateTerminationContractAsync">
    <Card>
      <template #content>
        <TitleHeader label="ข้อมูลสัญญา" :routeItems="routeItems">
          <template #action>
            <ButtonSave type="submit" v-if="store.body.id" />
          </template>
        </TitleHeader>
        <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
          <InfoItem class="col-start-1" title="ค้นหาข้อมูลสัญญา">
            <template #content>
              <InputField :model-value="store.body.contractNumber" class="w-4/5" disabled>
                <template #appendAction>
                  <InputGroupAddon v-if="menuStore.hasManage">
                    <Button label="ค้นหา"
                      class="rounded-l-none rounded-r-none text-white! bg-gray-500! border-none! h-full"
                      @click="() => showContractModal = true" />
                  </InputGroupAddon>
                </template>
              </InputField>
            </template>
          </InfoItem>

          <template v-if="store.body.id">
            <InfoItem title="คู่ค้า">
              <template #content>{{ store.body.entrepreneurName ?? "-" }}</template>
            </InfoItem>
            <InfoItem title="Email">
              <template #content>{{ store.body.entrepreneurEmail ?? "-" }}</template>
            </InfoItem>
            <InfoItem class="col-start-1" title="เลขที่สัญญา">
              <template #content>{{ store.body.contractNumber ?? "-" }}</template>
            </InfoItem>
            <InfoItem title="เลขที่ PO (SAP)">
              <template #content>{{ store.body.poNumber ?? "-" }}</template>
            </InfoItem>
            <InfoItem title="วงเงินตามสัญญา">
              <template #content>{{ formatCurrency(store.body.budget) }}</template>
            </InfoItem>
            <InfoItem title="ชื่อสัญญา">
              <template #content>{{ store.body.contractName ?? "-" }}</template>
            </InfoItem>
            <InfoItem title="ประเภทสัญญา">
              <template #content>{{ store.body.contractType ?? "-" }}</template>
            </InfoItem>
            <InfoItem title="รูปแบบสัญญา">
              <template #content>{{ store.body.contractTemplate ?? "-" }}</template>
            </InfoItem>
          </template>
        </div>
      </template>
    </Card>
  </Form>
  <ContractDialog v-model:show="showContractModal" :keyword="keyword" @on-select="onSelectContract" />
</template>
