<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { Form } from 'vee-validate';
import { Button } from 'primevue';
import { PreProcurementType } from '@/enums/preProcurement';
import { InfoItem } from '@/components/cosmetic';
import { InputField } from '@/components/forms';
import { formatCurrency } from '@/helpers/currency';
import { usePPDetailStore } from '@/stores/PP/ppStore';
import { PreProcurementConstants } from '@/constants';
import { useMenuStore } from '@/stores/menu';
import ProcurementDialog from './ProcurementDialog.vue';
import TypeBadgeChip from './TypeBadgeChip.vue'
import type { TChangeCommitteeProcurement } from '@/models/CAM/CAM02/cam02';
import { useCam02DetailStore } from '@/stores/CAM/CAM02/cam02Store';
import router from '@/router';

type Props = {
  procurement: TChangeCommitteeProcurement;
}

const props = defineProps<Props>();
const store = useCam02DetailStore();

const { PreProcurementTypeName } = PreProcurementConstants;
const preProcurementDetailStore = usePPDetailStore();
const menuStore = useMenuStore();

const showDialogProcurement = ref(false);

const hasProcurement = computed(() =>
  !!(preProcurementDetailStore.procurementDetail.planId || preProcurementDetailStore.procurementDetail.planNumber)
);

const onShowDialogProcurement = () => {
  showDialogProcurement.value = true;
};

onMounted(() => {
  if (props.procurement) {
    preProcurementDetailStore.procurementDetail.planId = props.procurement.planId;
    preProcurementDetailStore.procurementDetail.planNumber = props.procurement.planNumber;
    preProcurementDetailStore.procurementDetail.procurementNumber = props.procurement.procurementNumber;
    preProcurementDetailStore.procurementDetail.planType = props.procurement.planType;
    preProcurementDetailStore.procurementDetail.departmentName = props.procurement.departmentName ?? '';
    preProcurementDetailStore.procurementDetail.planName = props.procurement.planName;
    preProcurementDetailStore.procurementDetail.budget = props.procurement.budget ?? 0;
    preProcurementDetailStore.procurementDetail.budgetYear = props.procurement.budgetYear;
    preProcurementDetailStore.procurementDetail.supplyMethod = props.procurement.supplyMethod;
    preProcurementDetailStore.procurementDetail.supplyMethodType = props.procurement.supplyMethodType;
    preProcurementDetailStore.procurementDetail.supplyMethodSpecialType = props.procurement.supplyMethodSpecialType;
  }

})

const onRouteProcurementToDetail = (id: string, type?: string) => {
  let path = "";

  switch (type) {
    case "Rent":
      path = `/pcm/pcm005/detail/${id}`;
      break;

    case "Procurement":
      path = `/pp/detail/${id}`;
      break;

    default:
      return;
  }

  const route = router.resolve(path);
  window.open(route.href, "_blank");
};

const onRoutePlanToDetail = (id?: string) => {

  const route = router.resolve(`/pl/pl001/detail/${id}`);
  window.open(route.href, "_blank");
};

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง"> </TitleHeader>
      <Form @submit="() => { }">
        <div class="flex justify-between">
          <div class="w-full">
            <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 my-4">
              <InputField label="เลขที่อ้างอิงในระบบ" v-model="preProcurementDetailStore.procurementDetail.procurementNumber"
                v-if="!store.procurementDetail.id && menuStore.hasManage"
                rules="required" disabled>
                <template #appendAction>
                  <InputGroupAddon>
                    <Button label="ค้นหา" class="rounded-none! text-white! bg-gray-500! border-none! h-full"
                      @click="onShowDialogProcurement" />
                  </InputGroupAddon>
                </template>
              </InputField>

              <InfoItem title="เลขที่รายการจัดซื้อจัดจ้าง" v-if="store.procurementDetail.id && preProcurementDetailStore.procurementDetail.planNumber">
                <template #content>
                  <span class="text-blue-500 underline cursor-pointer"
                    @click="onRoutePlanToDetail(preProcurementDetailStore.procurementDetail.planId)">
                    {{ preProcurementDetailStore.procurementDetail.planNumber}}
                  </span>
                </template>
              </InfoItem>

              <InfoItem title="เลขที่การจัดซื้อจัดจ้าง" v-if="store.procurementDetail.id">
                <template #content>
                  <span class="text-blue-500 underline cursor-pointer"
                    @click="onRouteProcurementToDetail(store.procurementDetail.procurementId, store.procurementDetail.procurementType)">
                    {{ preProcurementDetailStore.procurementDetail.procurementNumber}}
                  </span>
                </template>
              </InfoItem>

              <InfoItem v-if="hasProcurement" title="ฝ่าย/ภาคเขต"
                :content="preProcurementDetailStore.procurementDetail.departmentName" />
              <InfoItem v-if="hasProcurement && preProcurementDetailStore.procurementDetail.planType"
                title="ประเภทแผน" :content="preProcurementDetailStore.procurementDetail.planType as string">
                <template #content="{ item }">
                  <TypeBadgeChip :label="PreProcurementTypeName(item as PreProcurementType)" size="Small"
                    :color="item as string" class="w-fit" />
                </template>
              </InfoItem>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-3 items-center gap-2 mb-4"
              v-if="hasProcurement">
              <InfoItem title="โครงการ" :content="preProcurementDetailStore.procurementDetail.planName ?? ''" />
              <InfoItem title="ปีงบประมาณ" :content="preProcurementDetailStore.procurementDetail.budgetYear ?? ''" />
              <InfoItem title="วงเงินงบประมาณ" content="">
                <template #content>
                  <p v-if="preProcurementDetailStore.procurementDetail.budget > 0">
                    {{ formatCurrency(preProcurementDetailStore.procurementDetail.budget) }}
                  </p>
                </template>
              </InfoItem>
            </div>

            <div class="grid grid-cols-3 lg:grid-cols-3 gap-2 mb-4"
              v-if="hasProcurement">
              <InfoItem title="วิธีจัดหา" :content="preProcurementDetailStore.procurementDetail.supplyMethod ?? ''" />
              <InfoItem title="" :content="preProcurementDetailStore.procurementDetail.supplyMethodType ?? ''" />
              <InfoItem title="" :content="preProcurementDetailStore.procurementDetail.supplyMethodSpecialType ?? ''" />
            </div>
          </div>
        </div>
      </Form>
    </template>
  </Card>

  <ProcurementDialog v-model="showDialogProcurement">
  </ProcurementDialog>
</template>
