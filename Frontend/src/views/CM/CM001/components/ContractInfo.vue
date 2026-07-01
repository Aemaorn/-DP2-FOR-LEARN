<script setup lang="ts">
import { InfoItem, TitleHeader } from '@/components/cosmetic';
import { InputArea, InputNumber, Select } from '@/components/forms';
import { souceType } from '@/enums/CM/cm001';
import { PlanDepartmentCode } from '@/enums/plan';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { checkIsEighty } from '@/helpers/supplyMethod';
import type { CM001Info } from '@/models/CM/cm001';
import router from '@/router';
import { useCm001DetailStore } from '@/stores/CM/cm001';
import { useMenuStore } from '@/stores/menu';
import { Button } from 'primevue';
import { computed, ref, watch } from 'vue';
import CommercialMaterialSection from './CommercialMaterialSection.vue';

type Props = {
  data: CM001Info;
  contractType?: string;
}
const { data, contractType } = defineProps<Props>();

const store = useCm001DetailStore();
const menuStore = useMenuStore();

const isEditing = ref(false);

const enterEditMode = async () => {
  await Promise.all([
    store.fn.getDepartmentDDLAsync(),
    store.fn.getSupplyMethodCodeDDLAsync(),
    store.fn.getSupplyMethodTypeCodeDDLAsync(),
    store.body.supplyMethodCode
      ? store.fn.getSupplyMethodSpecialTypeCodeDDLAsync(store.body.supplyMethodCode)
      : Promise.resolve(),
  ]);

  isEditing.value = true;
};

const onCancelEdit = async () => {
  isEditing.value = false;
  if (store.body.id) {
    await store.fn.onGetByIdAsync(store.body.id);
  }
};

const onSaveEdit = async () => {
  const success = await store.fn.onUpdateManualAsync();

  if (success) {
    isEditing.value = false;
  }
};

const checkPlanDepartmentCode = computed(() => {
  return [PlanDepartmentCode.CCD, PlanDepartmentCode.MCD, PlanDepartmentCode.NMD, PlanDepartmentCode.OABAD, PlanDepartmentCode.RDMD1, PlanDepartmentCode.RDMD2].includes(store.body.departmentId as PlanDepartmentCode) && checkIsEighty(store.body.supplyMethodCode);
});

watch(() => store.body.supplyMethodCode, async (newValue) => {
  if (!isEditing.value || store.body.sourceType !== souceType.Manual) return;

  if (newValue) {
    await store.fn.getSupplyMethodSpecialTypeCodeDDLAsync(newValue);
    return;
  }

  store.body.supplyMethodSpecialTypeCode = undefined;
});

const getSourceTypeLabel = (sourceType?: string): string => {
  switch (sourceType) {
    case souceType.Plan: return 'แผนฯ จัดซื้อจัดจ้าง';
    case souceType.Procurement: return 'ไม่ทำสัญญา (40) / อื่นๆ';
    case souceType.ContractDraftVendor: return 'ทำสัญญา (41 / 30)';
    case souceType.ContractDraftVendorEdit: return 'สัญญาต่อท้าย';
    case souceType.Manual: return 'สร้างใหม่ (ไม่อ้างอิงเอกสาร)';
    default: return '';
  }
};

const onRouteProcurementToDetail = (itemId: string, type?: string) => {
  let path = "";

  switch (type) {
    case "Plan":
      path = `/pl/pl001/detail/${itemId}`;
      break;

    case "Procurement":
      path = `/pp/detail/${itemId}`;
      break;

    default:
      return;
  }

  const route = router.resolve(path);
  window.open(route.href, "_blank");
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader :label="`ข้อมูลรายการจัดซื้อจัดจ้าง/ข้อมูลสัญญา ${contractType ?? ''}`" hidden-icon>
        <template #action>
          <div v-if="store.body.sourceType" class="flex items-center gap-2">
            <span class="text-[#1D4ED8] leading-none">&#9679;</span>
            <p class="whitespace-nowrap text-sm text-[#1D4ED8] font-bold">
              {{ getSourceTypeLabel(store.body.sourceType) }}
            </p>
          </div>
          <template v-if="store.body.sourceType === souceType.Manual && menuStore.hasManage">
            <i v-if="!isEditing" class="pi pi-pencil text-orange-500 cursor-pointer hover:text-orange-700"
              style="font-size: 0.75rem;" aria-label="แก้ไขข้อมูล" @click="enterEditMode" />
            <template v-else>
              <Button label="บันทึก" icon="pi pi-save" severity="success" @click="onSaveEdit" />
              <Button label="ยกเลิก" icon="pi pi-times" severity="secondary" variant="outlined" @click="onCancelEdit" />
            </template>
          </template>
        </template>
      </TitleHeader>
      <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
        <!-- Plan type fields -->
        <template v-if="store.body.sourceType === souceType.Plan">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.planId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.planId, 'Plan')">
                {{ data.planCode }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.procurementId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.procurementId, 'Procurement')">
                {{ data.procurementNumber }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="โครงการ">
            <template #content>
              {{ data.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ปีงบประมาณ">
            <template #content>
              {{ data.budgetYear ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="งบประมาณ">
            <template #content>
              {{ formatCurrency(data.budget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem title="วิธีจัดหา">
            <template #content>
              {{ data.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>

        <!-- ContractDraftVendor type fields -->
        <template v-if="store.body.sourceType === souceType.ContractDraftVendor">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.planId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.planId, 'Plan')">
                {{ data.planCode }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.procurementId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.procurementId, 'Procurement')">
                {{ data.procurementNumber }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="คู่ค้า">
            <template #content>
              {{ data.vendorName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="Email">
            <template #content>
              {{ data.vendorEmail ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="เลขที่สัญญา">
            <template #content>
              {{ data.contractNumber ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="วงเงินตามสัญญา">
            <template #content>
              {{ formatCurrency(data.contractBudget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem title="ชื่อสัญญา">
            <template #content>
              {{ data.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ประเภทสัญญา">
            <template #content>
              {{ data.contractTypeName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="รูปแบบสัญญา">
            <template #content>
              {{ data.templateName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="วันที่ลงนามตามสัญญา">
            <template #content>
              {{ ToDateOnly(data.contractDate) }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="วิธีจัดหา">
            <template #content>
              {{ data.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>

        <!-- ContractDraftVendorEdit type fields -->
        <template v-if="store.body.sourceType === souceType.ContractDraftVendorEdit">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.planId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.planId, 'Plan')">
                {{ data.planCode }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.procurementId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.procurementId, 'Procurement')">
                {{ data.procurementNumber }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="คู่ค้า">
            <template #content>
              {{ data.vendorName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="Email">
            <template #content>
              {{ data.vendorEmail ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="เลขที่สัญญา">
            <template #content>
              {{ data.contractNumber ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="วงเงินตามสัญญา">
            <template #content>
              {{ formatCurrency(data.contractBudget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem title="ชื่อสัญญา">
            <template #content>
              {{ data.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ประเภทสัญญา">
            <template #content>
              {{ data.contractTypeName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="รูปแบบสัญญา">
            <template #content>
              {{ data.templateName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="วันที่ลงนามตามสัญญา">
            <template #content>
              {{ ToDateOnly(data.contractDate) }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="วิธีจัดหา">
            <template #content>
              {{ data.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>

        <!-- Procurement type fields -->
        <template v-if="store.body.sourceType === souceType.Procurement">
          <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.planId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.planId, 'Plan')">
                {{ data.planCode }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="เลขที่การจัดซื้อจัดจ้าง">
            <template #content>
              <span v-if="data.procurementId" class="text-blue-500 underline cursor-pointer inline-block w-fit"
                @click="onRouteProcurementToDetail(data.procurementId, 'Procurement')">
                {{ data.procurementNumber }}
              </span>
              <template v-else>-</template>
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="โครงการ">
            <template #content>
              {{ data.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ปีงบประมาณ">
            <template #content>
              {{ data.budgetYear ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="งบประมาณ">
            <template #content>
              {{ formatCurrency(data.budget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem title="วิธีจัดหา">
            <template #content>
              {{ data.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>

        <!-- Manual type fields (no reference, read-only) -->
        <template v-if="store.body.sourceType === souceType.Manual && !isEditing">
          <InfoItem class="col-start-1" title="เลขที่เอกสาร">
            <template #content>
              {{ store.body.number ?? store.body.refCode ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="ฝ่าย/ภาคเขต">
            <template #content>
              {{ data.departmentName ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="โครงการ">
            <template #content>
              {{ data.name ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="งบประมาณ">
            <template #content>
              {{ formatCurrency(data.budget ?? 0) }}
            </template>
          </InfoItem>

          <InfoItem class="col-start-1" title="วิธีจัดหา">
            <template #content>
              {{ data.supplyMethod ?? "-" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodType ?? "" }}
            </template>
          </InfoItem>

          <InfoItem title="">
            <template #content>
              {{ data.supplyMethodSpecialType ?? "" }}
            </template>
          </InfoItem>
        </template>
      </div>

      <div v-if="store.body.sourceType === souceType.Manual && isEditing" class="px-4 mt-4 flex flex-col gap-4">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Select label="ฝ่าย/ภาคเขต" :options="store.departmentDDL" v-model="store.body.departmentId"
            rules="required" />
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Select label="วิธีการจัดหา" :options="store.supplyMethodCodeDDL" v-model="store.body.supplyMethodCode"
            rules="required" />
          <Select v-model="store.body.supplyMethodTypeCode" :options="store.supplyMethodTypeCodeDDL" />
          <Select v-model="store.body.supplyMethodSpecialTypeCode" :options="store.supplyMethodSpecialTypeCodeDDL"
            :disabled="!store.body.supplyMethodCode" />
        </div>

        <InputArea label="ชื่อโครงการ" :row="5" v-model="store.body.name" rules="required" />

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 -mt-2">
          <InputNumber label="วงเงินงบประมาณ" v-model="store.body.budget" rules="required" grouping
            :min-fraction-digits="2" />
        </div>
      </div>
    </template>
  </Card>
  <CommercialMaterialSection
    v-if="checkPlanDepartmentCode && store.body.sourceType === souceType.Manual"
    v-model="store.body.isCommercialMaterial"
    :department-id="store.body.departmentId"
    :supply-method-code="store.body.supplyMethodCode"
    :disabled="!isEditing"
    :is-required="!store.body.id" />
</template>