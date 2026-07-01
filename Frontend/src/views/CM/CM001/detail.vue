<script setup lang="ts">
import { BadgeStatus as BadgeComponent } from '@/components';
import { ButtonSave } from '@/components/Button';
import { InfoItem, TitleHeader } from '@/components/cosmetic';
import { InputArea, InputField, InputNumber, Select } from '@/components/forms';
import { CM001PeriodStatus, CM001Status, CmDeliveryAcceptancePeriodAccountStatus, souceType } from '@/enums/CM/cm001';
import { PlanDepartmentCode } from '@/enums/plan';
import { EGroupCode } from '@/enums/shared';
import { CM001Helper, CM001PeriodHelper } from '@/helpers/CM/cm001';
import { formatCurrency } from '@/helpers/currency';
import { ToDateOnly } from '@/helpers/dateTime';
import { checkIsEighty } from '@/helpers/supplyMethod';
import type { Option } from '@/models/shared/option';
import { showActivityDialog } from '@/helpers/dialog';
import SharedService from '@/services/Shared/dropdown';
import { useAuthenticationStore } from '@/stores/authentication';
import { useCm001DetailStore as CM001Store } from '@/stores/CM/cm001';
import { useMenuStore } from '@/stores/menu';
import { HttpStatusCode } from 'axios';
import { Button, Card, InputGroupAddon } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { computed, defineAsyncComponent, onBeforeMount, onMounted, ref, watch } from 'vue';
import { Form as VeeForm } from 'vee-validate';
import ToastHelper from '@/helpers/toast';
import { useRoute, useRouter } from 'vue-router';
import CommercialMaterialSection from './components/CommercialMaterialSection.vue';
import ModeSelectionDialog from './components/ModeSelectionDialog.vue';
import SelectDialog from './components/SelectDialog.vue';
import SendEmailWarrantyPeriodDialog from './components/Sub/SendEmailWarrantyPeriodDialog.vue';

const route = useRoute();
const router = useRouter();
const id = computed<string>(() => route.params.id as string);
const dropdown = ref<Array<Option>>([]);
const menuStore = useMenuStore();
const showSelectDialog = ref<boolean>(false);
const showEmailWarrantyDialog = ref<boolean>(false);
const showModeDialog = ref<boolean>(false);

const canSendWarrantyEmail = computed<boolean>(() => {
  const approvedAccountStatuses = [
    CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval,
    CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected,
    CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate,
    CmDeliveryAcceptancePeriodAccountStatus.Paid,
  ];
  return !!store.body.cm001Info?.warranty?.hasWarranty
    && store.body.periods.length > 0
    && store.body.periods.every(p => p.status === CM001PeriodStatus.Approved && approvedAccountStatuses.includes(p.accountStatus));
});

const { BadgeStatus } = CM001Helper;
const { PeriodBadgeStatus, PeriodAccountStatus } = CM001PeriodHelper;

const store = CM001Store();
const authStore = useAuthenticationStore();
const ContractSection = defineAsyncComponent(() => import('@/views/CM/CM001/components/ContractInfo.vue'));

const routeItems = ref<Array<MenuItem>>([
  { label: 'บันทึกรายงานผลการตรวจรับ (จพ.008)', url: '/cm/cm001' },
  { label: 'ข้อมูลส่งมอบ ตรวจรับ และขออนุมัติเบิกจ่าย', },
]);

onMounted(async () => {
  await onGetContractDropdownAsync();
});

const onGetContractDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CType);

  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};

const toPeriodDetail = (periodId: string) => {
  router.push({
    name: 'cm001PeriodDetail',
    params: {
      id: id.value,
      periodId: periodId,
    },
  });
};

onBeforeMount(async () => {
  store.onResetBody();

  if (id.value) {
    await store.fn.onGetByIdAsync(id.value);
    return;
  }

  showModeDialog.value = true;
});

const onChooseReference = () => {
  showModeDialog.value = false;
  showSelectDialog.value = true;
};

const onChooseManual = async () => {
  showModeDialog.value = false;
  store.body.sourceType = souceType.Manual;
  store.body.refId = undefined;
  store.body.refCode = 'สร้างใหม่ (ไม่อ้างอิงเอกสาร)';
  store.body.departmentId = authStore.profile.departmentCode;

  await Promise.all([
    store.fn.getDepartmentDDLAsync(),
    store.fn.getSupplyMethodCodeDDLAsync(),
    store.fn.getSupplyMethodTypeCodeDDLAsync(),
  ]);
};

const checkPlanDepartmentCode = computed(() => {
  return [PlanDepartmentCode.CCD, PlanDepartmentCode.MCD, PlanDepartmentCode.NMD, PlanDepartmentCode.OABAD, PlanDepartmentCode.RDMD1, PlanDepartmentCode.RDMD2].includes(store.body.departmentId as PlanDepartmentCode) && checkIsEighty(store.body.supplyMethodCode);
});

watch(() => checkPlanDepartmentCode.value, (val: boolean) => {
  if (!val) {
    store.body.isCommercialMaterial = undefined;
  }
});

watch(() => store.body.supplyMethodCode, async (newValue) => {
  if (store.body.sourceType !== souceType.Manual) return;

  if (newValue) {
    await store.fn.getSupplyMethodSpecialTypeCodeDDLAsync(newValue);
    return;
  }

  store.body.supplyMethodSpecialTypeCode = undefined;
});

const onCancelMode = () => {
  showModeDialog.value = false;
};

const getLabelFromDropdown = (value?: string) => {
  if (value === undefined) {
    return '-';
  }

  return dropdown.value.find(d => d.value === value)?.label;
}

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
  <div v-if="!store.body.id">
    <VeeForm @submit="store.fn.onCreateAsync" @invalidSubmit="ToastHelper.invalidMessageToast()" v-slot="{ handleSubmit }">
    <Card>
      <template #content>
        <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง/ข้อมูลสัญญา" :routeItems="routeItems">
          <template #action>
            <div v-if="store.body.sourceType" class="flex items-center gap-2 mr-2">
              <span class="text-[#1D4ED8] leading-none">&#9679;</span>
              <p class="whitespace-nowrap text-sm text-[#1D4ED8] font-bold">
                {{ getSourceTypeLabel(store.body.sourceType) }}
              </p>
            </div>
            <ButtonSave @click="handleSubmit(store.fn.onCreateAsync)" />
          </template>
        </TitleHeader>
        <div class="px-4 mt-2 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 items-center">
          <InfoItem class="col-start-1" title="เลขที่อ้างอิงในระบบ">
            <template #content>
              <InputField v-if="menuStore.hasManage" v-model="store.body.refCode" rules="required" class="w-full"
                disabled>
                <template #appendAction>
                  <InputGroupAddon v-if="menuStore.hasManage">
                    <Button label="ค้นหา"
                      class="rounded-l-none rounded-r-none text-white! bg-gray-500! border-none! h-full"
                      @click="showModeDialog = true" />
                  </InputGroupAddon>
                </template>
              </InputField>
            </template>
          </InfoItem>

          <template v-if="store.body.sourceType === souceType.Plan">
            <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.planId, 'Plan')">
                  {{ store.body.cm001Info.planCode }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="เลขที่การจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.procurementId, 'Procurement')">
                  {{ store.body.cm001Info.procurementNumber }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem title="ฝ่าย/ภาคเขต">
              <template #content>
                {{ store.body.cm001Info.departmentName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="โครงการ">
              <template #content>
                {{ store.body.cm001Info.name ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="ปีงบประมาณ">
              <template #content>
                {{ store.body.cm001Info.budgetYear ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="งบประมาณ">
              <template #content>
                {{ formatCurrency(store.body.cm001Info.budget ?? 0) }}
              </template>
            </InfoItem>

            <InfoItem title="วิธีจัดหา">
              <template #content>
                {{ store.body.cm001Info.supplyMethod ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodType ?? "" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodSpecialType ?? "" }}
              </template>
            </InfoItem>
          </template>

          <template v-if="store.body.sourceType === souceType.ContractDraftVendor">
            <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.planId, 'Plan')">
                  {{ store.body.cm001Info.planCode }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="เลขที่การจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.procurementId, 'Procurement')">
                  {{ store.body.cm001Info.procurementNumber }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem title="ฝ่าย/ภาคเขต">
              <template #content>
                {{ store.body.cm001Info.departmentName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="คู่ค้า">
              <template #content>
                {{ store.body.cm001Info.vendorName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="Email">
              <template #content>
                {{ store.body.cm001Info.vendorEmail ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="เลขที่สัญญา">
              <template #content>
                {{ store.body.cm001Info.contractNumber ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="วงเงินตามสัญญา">
              <template #content>
                {{ formatCurrency(store.body.cm001Info.contractBudget ?? 0) }}
              </template>
            </InfoItem>

            <InfoItem title="ชื่อสัญญา">
              <template #content>
                {{ store.body.cm001Info.name ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="ประเภทสัญญา">
              <template #content>
                {{ store.body.cm001Info.contractTypeName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="รูปแบบสัญญา">
              <template #content>
                {{ store.body.cm001Info.templateName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="วันที่ลงนามตามสัญญา">
              <template #content>
                {{ ToDateOnly(store.body.cm001Info.contractDate) }}
              </template>
            </InfoItem>

            <InfoItem title="กำหนดส่งมอบภายใน">
              <template #content>
                {{ store.body.cm001Info.period ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="ครบกำหนดส่งมอบงานวันที่">
              <template #content>
                {{ ToDateOnly(store.body.cm001Info.deliveryDate) }}
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="วิธีจัดหา">
              <template #content>
                {{ store.body.cm001Info.supplyMethod ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodType ?? "" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodSpecialType ?? "" }}
              </template>
            </InfoItem>
          </template>

          <template v-if="store.body.sourceType === souceType.ContractDraftVendorEdit">
            <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.planId, 'Plan')">
                  {{ store.body.cm001Info.planCode }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="เลขที่การจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.procurementId, 'Procurement')">
                  {{ store.body.cm001Info.procurementNumber }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem title="ฝ่าย/ภาคเขต">
              <template #content>
                {{ store.body.cm001Info.departmentName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="คู่ค้า">
              <template #content>
                {{ store.body.cm001Info.vendorName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="Email">
              <template #content>
                {{ store.body.cm001Info.vendorEmail ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="เลขที่สัญญา">
              <template #content>
                {{ store.body.cm001Info.contractNumber ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="วงเงินตามสัญญา">
              <template #content>
                {{ formatCurrency(store.body.cm001Info.contractBudget ?? 0) }}
              </template>
            </InfoItem>

            <InfoItem title="ชื่อสัญญา">
              <template #content>
                {{ store.body.cm001Info.name ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="ประเภทสัญญา">
              <template #content>
                {{ store.body.cm001Info.contractTypeName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="รูปแบบสัญญา">
              <template #content>
                {{ store.body.cm001Info.templateName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="วันที่ลงนามตามสัญญา">
              <template #content>
                {{ ToDateOnly(store.body.cm001Info.contractDate) }}
              </template>
            </InfoItem>

            <InfoItem title="กำหนดส่งมอบภายใน">
              <template #content>
                {{ store.body.cm001Info.period ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="ครบกำหนดส่งมอบงานวันที่">
              <template #content>
                {{ ToDateOnly(store.body.cm001Info.deliveryDate) }}
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="วิธีจัดหา">
              <template #content>
                {{ store.body.cm001Info.supplyMethod ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodType ?? "" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodSpecialType ?? "" }}
              </template>
            </InfoItem>
          </template>

          <template v-if="store.body.sourceType === souceType.Procurement">
            <InfoItem class="col-start-1" title="เลขที่รายการจัดซื้อจัดจ้าง">
              {{ store.body.cm001Info.planId }}
              <template #content>
                <span v-if="store.body.cm001Info.planId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.planId, 'Plan')">
                  {{ store.body.cm001Info.planCode }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem class="col-start-1" title="เลขที่การจัดซื้อจัดจ้าง">
              <template #content>
                <span v-if="store.body.cm001Info.procurementId" class="text-blue-500 underline cursor-pointer"
                  @click="onRouteProcurementToDetail(store.body.cm001Info.procurementId, 'Procurement')">
                  {{ store.body.cm001Info.procurementNumber }}
                </span>
                <template v-else>-</template>
              </template>
            </InfoItem>

            <InfoItem title="ฝ่าย/ภาคเขต">
              <template #content>
                {{ store.body.cm001Info.departmentName ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="โครงการ">
              <template #content>
                {{ store.body.cm001Info.name ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="ปีงบประมาณ">
              <template #content>
                {{ store.body.cm001Info.budgetYear ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="งบประมาณ">
              <template #content>
                {{ formatCurrency(store.body.cm001Info.budget ?? 0) }}
              </template>
            </InfoItem>

            <InfoItem title="วิธีจัดหา">
              <template #content>
                {{ store.body.cm001Info.supplyMethod ?? "-" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodType ?? "" }}
              </template>
            </InfoItem>

            <InfoItem title="">
              <template #content>
                {{ store.body.cm001Info.supplyMethodSpecialType ?? "" }}
              </template>
            </InfoItem>
          </template>

        </div>

        <div v-if="store.body.sourceType === souceType.Manual" class="px-4 mt-4 flex flex-col gap-4">
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
      :is-required="!store.body.id" />
    </VeeForm>
  </div>
  <div v-if="store.body.id">
    <ContractSection :data="store.body.cm001Info" :contractType="getLabelFromDropdown(store.body.contractType)" />
    <div class="p-2">
      <TitleHeader label="การรายงานผลการตรวจรับ" :routeItems="routeItems">
        <template #action>
          <div class="flex items-center gap-2">
            <p class="text-sm">สถานะ :</p>
            <BadgeComponent :label="BadgeStatus(store.body.status).label" :color="BadgeStatus(store.body.status).color" />
          </div>
          <Button label="สร้างรายการส่งมอบตรวจรับ" icon="pi pi-plus" severity="primary" variant="outlined"
            class="hover:bg-red-200 hover:text-red-900 bg-red-50"
            @click="() => router.push({ name: 'cm001PeriodDetail', params: { id: id } })"
            v-if="menuStore.hasManage && store.body.status !== CM001Status.Completed" />
          <Button label="ยืนยันปิดงาน" icon="pi pi-check" severity="success"
            @click="store.fn.onApproveDeliveryAcceptanceAsync"
            v-if="store.body.canApproveDeliveryAcceptance && store.body.status !== CM001Status.Completed" />
          <Button label="ส่งอีเมลแจ้งระยะเวลารับประกัน" icon="pi pi-envelope" severity="warn" variant="outlined"
            v-if="canSendWarrantyEmail"
            @click="showEmailWarrantyDialog = true" />
          <Button label="ประวัติการใช้งาน" icon="pi pi-refresh" variant="outlined" severity="primary" v-if="store.body.id"
            class="bg-white! hover:bg-red-50!" @click="() => showActivityDialog(store.body.id!)" />
        </template>
      </TitleHeader>
      <Card class="mb-4">
        <template #content>
          <table class="w-full border-collapse border border-gray-300 text-center">
            <thead>
              <tr class="bg-gray-200 text-gray-900 font-bold">
                <th class="border border-gray-300 px-4 py-2 w-[8%]">งวด</th>
                <th class="border border-gray-300 px-4 py-2 w-[10%]">เลขที่ตรวจรับ</th>
                <th class="border border-gray-300 px-4 py-2 w-[34%]">รายละเอียดการตรวจรับ</th>
                <th class="border border-gray-300 px-4 py-2 w-[12%]">สถานะตรวจรับ</th>
                <th class="border border-gray-300 px-4 py-2 w-[12%]">สถานะเบิกจ่าย</th>
                <th class="border border-gray-300 px-4 py-2 w-[24%]"></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="({ paymentTermNo, acceptanceNumber, description, id, status, accountStatus }) in store.body.periods"
                :key="id">
                <td class="border border-gray-300 px-4 py-2 font-bold">{{ paymentTermNo }}</td>
                <td class="border border-gray-300 px-4 py-2 font-bold">{{ acceptanceNumber }}</td>
                <td class="border border-gray-300 px-4 py-2 text-start whitespace-pre-line">{{ description }}</td>
                <td class="border border-gray-300 px-4 py-2">
                  <div class="flex justify-center items-center">
                    <BadgeComponent :label="PeriodBadgeStatus(status).label" :color="PeriodBadgeStatus(status).color" />
                  </div>
                </td>
                <td class="border border-gray-300 px-4 py-2">
                  <div class="flex justify-center items-center">
                    <BadgeComponent :label="PeriodAccountStatus(accountStatus, status).label"
                      :color="PeriodAccountStatus(accountStatus, status).color" />
                  </div>
                </td>
                <td class="border border-gray-300 px-4 py-2">
                  <div class="flex items-center justify-center gap-2">
                    <Button label="ไปยังบันทึกส่งมอบและตรวจรับ" variant="outlined"
                      :severity="status !== CM001PeriodStatus.Approved ? 'danger' : 'success'" icon="pi pi-sign-out"
                      @click="() => toPeriodDetail(id)" />
                    <Button
                      v-if="[CM001PeriodStatus.Draft, CM001PeriodStatus.Edit, CM001PeriodStatus.Rejected].includes(status)"
                      variant="outlined" :severity="'danger'" icon="pi pi-trash"
                      @click="() => store.fn.onDeletePeriodAsync(id)" />
                    <div v-else style="width: 2.5rem" />
                  </div>
                </td>
              </tr>
              <tr v-if="store.body.periods.length == 0">
                <td colspan="6" class="border border-gray-300 px-4 py-4 text-gray-500">-- ไม่มีข้อมูลการรายงานผลการตรวจรับ --</td>
              </tr>
            </tbody>
          </table>
        </template>
      </Card>
    </div>
  </div>
  <SelectDialog v-model="showSelectDialog" />
  <SendEmailWarrantyPeriodDialog v-model="showEmailWarrantyDialog" />
  <ModeSelectionDialog v-model:visible="showModeDialog" @select-reference="onChooseReference"
    @select-manual="onChooseManual" @cancel="onCancelMode" />
</template>