<script setup lang="ts">
import { InfoItem } from '@/components/cosmetic';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { useCm006DetailStore } from '@/stores/CM/CM006/cm006.detail';
import { useMenuStore } from '@/stores/menu';
import { storeToRefs } from 'pinia';
import { InputGroupAddon } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { computed, defineAsyncComponent, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import SelectDiaglog from './components/Sub/SelectDiaglog.vue';

const route = useRoute();
const menuStore = useMenuStore();
const contractVendorId = computed<string>(() => route.params.contractVendorId as string);
const id = computed<string | undefined>(() => route.params.id as string | undefined);
const showSelectDialog = ref<boolean>(false);

const ContractInfo = defineAsyncComponent(() => import('@/views/CM/CM006/components/ContractInfo.vue'));
const GuaranteeReturnForm = defineAsyncComponent(() => import('@/views/CM/CM006/components/GuaranteeReturnForm.vue'));

const store = useCm006DetailStore();
const { body } = storeToRefs(store);
const { onGetById } = store;

const routeItems = ref<Array<MenuItem>>([
  { label: 'รายการคืนหลักประกันสัญญา', url: '/cm/cm006' },
  { label: 'ข้อมูลคืนหลักประกันสัญญา' },
]);

onMounted(async () => {
  store.onResetBody();

  if (contractVendorId.value && id.value) {
    await onGetById(contractVendorId.value, id.value);
  }
});
</script>

<template>
  <Card v-if="!contractVendorId">
    <template #content>
      <TitleHeader label="ข้อมูลสัญญา" hidden-icon :routeItems="routeItems" />
      <div class="px-4 mt-2">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
          <InfoItem class="col-start-1" title="เลขที่อ้างอิงในระบบ">
            <template #content>
              <InputField v-if="menuStore.hasManage" v-model="store.body.contractNumber" rules="required" class="w-4/5"
                disabled>
                <template #appendAction>
                  <InputGroupAddon v-if="menuStore.hasManage">
                    <Button label="ค้นหา"
                      class="rounded-l-none rounded-r-none text-white! bg-gray-500! border-none! h-full"
                      @click="showSelectDialog = true" />
                  </InputGroupAddon>
                </template>
              </InputField>
            </template>
          </InfoItem>
          <template v-if="store.body.contractNumber">
            <InfoItem class="col-start-1" title="คู่ค้า"
              :content="`${store.body.taxId ?? '-'} : ${store.body.entrepreneurName ?? '-'}`" />
            <InfoItem title="Email" :content="store.body.entrepreneurEmail" />

            <InfoItem title="เลขที่สัญญา" :content="store.body.entrepreneurEmail" class="lg:col-start-1" />
            <InfoItem title="เลขที่ PO (SAP)" :content="store.body.poNumber" />
            <InfoItem title="วงเงินตามสัญญา" :content="formatCurrency(store.body.budget)" />

            <InfoItem title="ชื่อสัญญา" :content="store.body.contractName" class="lg:col-start-1" />
            <InfoItem title="ประเภทสัญญา" :content="store.body.contractType" />
            <InfoItem title="รูปแบบสัญญา" :content="store.body.contractTemplate" />

            <InfoItem title="วันที่ลงนามในสัญญา" :content="ToDateOnly(store.body.contractSignedDate)"
              class="lg:col-start-1" />
            <InfoItem title="กำหนดส่งมอบภายใน" :content="`${store.body.deliveryLeadTime ?? '-'} วัน`" />
            <InfoItem title="ครบกำหนดส่งมอบงาน วันที่" :content="ToDateOnly(store.body.deliveryDate)" />

            <InfoItem title="ระยะเวลารับประกัน" :content="store.body.deliveryLeadTimeTypeLabel" class="lg:col-start-1" />
          </template>
        </div>
      </div>
    </template>
  </Card>
  <ContractInfo :data="body" v-if="contractVendorId" />
  <GuaranteeReturnForm v-if="store.body.id" />
  <SelectDiaglog v-model="showSelectDialog" :defaultDepartment="false" />
</template>