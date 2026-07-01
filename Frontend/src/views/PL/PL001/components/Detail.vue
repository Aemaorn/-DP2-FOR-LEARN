<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { InputField, Select, Radio, InputArea, PrimeVueDatePicker, InputNumber } from '@/components/forms';
import { ButtonClose, ButtonCancelClose } from '@/components/Button';
import { showReasonDialogAsync, showConfirmDialogAsync } from '@/helpers/dialog';
import { ReasonDialogType, ConfirmDialogType } from '@/enums/dialog';
import { PlanAction } from '@/enums/plan';
import { getYearOptionsWithValue } from '@/constants/date';
import { PlanDepartmentCode, PlanStatus } from '@/enums/plan';
import { ProcurementPlanType } from '@/enums/procurement';
import { useAuthenticationStore } from '@/stores/authentication';
import { usePL001DetailStore } from '@/stores/PL/pl001';
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { Field } from 'vee-validate';
import { AssigneeType } from '@/enums/participants';
import { useMenuStore } from '@/stores/menu';
import { checkIsEighty, checkIsSixty } from '@/helpers/supplyMethod';

const menuStore = useMenuStore();
const authStore = useAuthenticationStore();
const store = usePL001DetailStore();
const stockOption = [
  { label: 'ใช่', value: true },
  { label: 'ไม่ใช่', value: false },
];
const planType = [
  { value: ProcurementPlanType.AnnualPlan, label: 'แผนรวมปี' },
  { value: ProcurementPlanType.InYearPlan, label: 'แผนระหว่างปี' },
];

const commercialOption = ref<Option[]>([]);

const checkPlanDepartmentCode = computed(() => {
  return [PlanDepartmentCode.CCD, PlanDepartmentCode.MCD, PlanDepartmentCode.NMD, PlanDepartmentCode.OABAD, PlanDepartmentCode.RDMD1, PlanDepartmentCode.RDMD2].includes(store.body.departmentCode as PlanDepartmentCode) && checkIsEighty(store.body.supplyMethodCode);
});

const isJopPorDepartment = computed(() => [PlanDepartmentCode.JP].includes(store.body.departmentCode as PlanDepartmentCode));

const canDraftRecordDocument = computed(() => {
  const checkAllApprove =
    store.body.assignees && store.body.assignees.length > 0 &&
    store.body.assignees
      .filter(a => a.assigneeType === AssigneeType.Director);

  const isJpSecetion = [PlanStatus.DraftPlan, PlanStatus.EditPlan, PlanStatus.RejectPlan, PlanStatus.WaitingApprovePlan, PlanStatus.WaitingAssign].includes(store.body.status);

  return store.budgetCondition && checkAllApprove && !isJpSecetion;
});

const canEditEGP = computed(() => {
  const status = [
    PlanStatus.WaitingAssign,
    PlanStatus.RejectToAssignee,
    PlanStatus.Assigned,
    PlanStatus.DraftRecordDocument,
  ].includes(store.body.status);
  const isJorPor = authStore.profile.isJorPor;

  return status && isJorPor;
});

onMounted(() => {
  initAsync();
});

const initAsync = async () => {
  await Promise.all([
    store.getDepartmentDDLAsync(),
    store.getAssignDepartmentDDLAsync(),
    store.getSupplyMethodCodeDDLAsync(),
    store.getSupplyMethodTypeDDLAsync(),
  ]);

  if (!store.body.id) {
    store.body.departmentCode = authStore.profile.departmentCode;
  }
};

onUnmounted(() => {
  store.clearBody();
});

const handleRadioLabel = () => {
  const data = [
    { label: '', value: true },
    { label: 'ไม่มี', value: false },
  ];

  switch (store.body.departmentCode) {
    case PlanDepartmentCode.CCD:
    case PlanDepartmentCode.MCD:
      data[0].label = [
        '1. การซื้อหรือจ้างที่เกี่ยวข้องกับการผลิตสื่อ การโฆษณา การประชาสัมพันธ์ผลิตภัณฑ์ ภาพลักษณ์และบริการของธนาคาร',
        '2. การจัดซื้อหรือจ้างที่เกี่ยวข้องกับเครื่องแต่งกาย เครื่องประดับ หรือพัสดุอื่นๆ ที่เกี่ยวข้องกับพิธีอุปถัมภ์ สำหรับแบรนด์แอมบาสเดอร์ หรือผู้แทนธนาคารในงานพิธีการต่างๆ',
        '3. การจ้างบริการแต่งหน้าทำผม สำหรับแบรนด์แอมบาสเดอร์ หรือผู้แทนธนาคารในงานพิธีการต่างๆ'
      ].join('<br/>');
      break;
    case PlanDepartmentCode.OABAD:
      data[0].label = [
        '1. งานจ้างที่เกี่ยวซ่อมแซมยานพาหนะ เครื่องจักร เครื่องใช้สำนักงาน ครุภัณฑ์ และอาคารสถานที่ จัดซื้อจัดหาอะไหล่ เพื่อการซ่อมแซมหรือบำรุงรักษา หรือทำความสะอาดเครื่องจักร เครื่องใช้สำนักงาน ครุภัณฑ์ ยานพาหนะ อาคารสถานที่ รวมถึงอุปกรณ์ต่างๆ ที่จำเป็นต้องใช้',
        '2. การจัดซื้อจัดจ้าง หรือซ่อมอุปกรณ์ต่างๆ ที่เกี่ยวข้องกับความปลอดภัย',
        '3. การจัดซื้อต้นไม้ และอุปกรณ์ในการปรับแต่งภูมิทัศน์',
        '4. การจัดซื้อจัดจ้างเกี่ยวกับการจัดสถานที่ในงานของธนาคาร',
        '5. เช่าที่จอดรถ',
        '6. การจัดซื้อจัดจ้างให้รวมถึงการเช่าสำหรับทรัพย์ด้วย'
      ].join('<br/>');
      break;
    case PlanDepartmentCode.NMD:
      data[0].label = [
        '1. การจ้างทำงานหรือจ้างบริการบำรุงรักษา หรือซ่อมแซมทรัพย์สินที่อยู่ในความรับผิดชอบ หรือทรัพย์สินของผู้อื่น ที่ได้รับความเสียหายอันเกิดจากทรัพย์สินที่อยู่ในความรับผิดชอบให้อยู่ในสภาพสามารถใช้งานได้เป็นปกติ',
        '2. การจ้างทำสื่อประชาสัมพันธ์ กิจกรรมทางการตลาดและการขาย'
      ].join('<br/>');
      break;
    case PlanDepartmentCode.RDMD1:
    case PlanDepartmentCode.RDMD2:
      data[0].label = [
        '1. การจ้างทำงานหรือจ้างบริการบำรุงรักษาหรือซ่อมแซมทรัพย์สินที่อยู่ในความรับผิดชอบหรือทรัพย์สินของผู้อื่นที่ได้รับความเสียหายอันเกิดจากทรัพย์สินที่อยู่ในความรับผิดชอบให้อยู่ในสภาพใช้งานได้เป็นปกติ',
        '2. การจ้างทำสื่อประชาสัมพันธ์ กิจกรรมทางการตลาดและการขาย'
      ].join('<br/>');
      break;
    default:
  }

  commercialOption.value = [...data];
};

const onSelectBudgetYear = (val?: number) => {
  if (!val) {
    store.body.expectingProcurementAt = undefined;

    return;
  }

  if (!store.body.id) {
    const date = ref<Date>(new Date());
    date.value.setFullYear((val - 543));
    store.body.expectingProcurementAt = date.value;
  }
};

watch(() => store.body.supplyMethodCode, async (newValue) => {
  if (newValue) {
    return await store.getSupplyMethodSpecialTypeDDlAsync(newValue);
  }

  store.body.supplyMethodSpecialTypeCode = undefined;
  store.body.supplyMethodTypeCode = undefined;
});

watch(() => store.body.departmentCode, () => {
  handleRadioLabel();
});


const onChangetype = (e?: string) => {
  if (!e) return;

  store.updateBudgetYearOnTypeChange(e as ProcurementPlanType);
}

watch(() => checkPlanDepartmentCode.value, (val: boolean) => {
  if (!val) {
    store.body.isCommercialMaterial = false;
  }
});

const yearOptions = computed(() => {
  return getYearOptionsWithValue(store.body.budgetYear);
});

const onClosePlanAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.ClosePlan, true);

  if (res.isConfirm && store.body.id) {
    await store.actionAsync(store.body.id, { action: PlanAction.ClosePlan, remark: res.reason, attachments: res.attachments });
  }
};

const onCancelClosePlanAsync = async (): Promise<void> => {
  const isConfirm = await showConfirmDialogAsync(ConfirmDialogType.CancelClosePlan);

  if (isConfirm && store.body.id) {
    await store.actionAsync(store.body.id, { action: PlanAction.CancelClosePlan });
  }
};
</script>

<template>
  <Card class="relative">
    <template #content>
      <div v-if="store.canClosePlan || store.canCancelClosePlan" class="flex justify-end mb-4">
        <ButtonClose v-if="store.canClosePlan" @click="onClosePlanAsync" />
        <ButtonCancelClose v-if="store.canCancelClosePlan" @click="onCancelClosePlanAsync" />
      </div>
      <div class="flex flex-col gap-8">
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mt-6">
          <InputField label="เลขที่รายการจัดซื้อจัดจ้าง" v-model="store.body.planNumber" disabled />
          <Select label="ฝ่าย/ภาคเขต" :options="store.departmentDDL" v-model="store.body.departmentCode"
            rules="required" disabled />
          <Datepicker v-if="canDraftRecordDocument" label="วันที่เอกสาร" v-model="store.body.documentDate"
            :disabled="!store.canAssignAssigneeTypeAssignee || !menuStore.hasManage" />
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div class="flex flex-col">
            <Radio class="-mt-10" label="ประเภทแผนจัดซื้อจัดจ้าง" :options="planType" v-model="store.body.type" rules="required"
              :disabled="!store.canEdit || !menuStore.hasManage || store.body.isCancel || store.body.isChange"
              @update:modelValue="(e: string | number | boolean | undefined) => onChangetype(e as string | undefined)" />
            <div class="bg-gray-100 rounded-lg py-2 px-4 h-10">
              <small> หมายเหตุ: แผนรวมปีจะต้องจัดทำในช่วงเดือนตุลาคมของทุกปี</small>
            </div>
          </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Select label="ปีงบประมาณ" :options="yearOptions" v-model="store.body.budgetYear" rules="required"
            :disabled="!store.canEdit || store.body.isCancel || !menuStore.hasManage"
            @onSelect="(e: number | undefined) => onSelectBudgetYear(e)" />
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Select label="วิธีการจัดหา" :options="store.supplyMethodCodeDDL" v-model="store.body.supplyMethodCode"
            rules="required" :disabled="!store.canEdit || store.body.isCancel || !menuStore.hasManage" />
          <Select v-model="store.body.supplyMethodTypeCode" :options="store.supplyMethodTypeCodeDDL" rules="required"
            :disabled="!store.canEdit || store.body.isCancel || !menuStore.hasManage" />
          <Select v-model="store.body.supplyMethodSpecialTypeCode" :options="store.supplyMethodSpecialTypeCodeDDL"
            rules="required" :disabled="!store.canEdit || store.body.isCancel || !menuStore.hasManage" />
        </div>

        <InputArea label="ชื่อโครงการ" :row="5" v-model="store.body.name" rules="required"
          :disabled="!store.canEdit || !menuStore.hasManage" />
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <InputNumber label="วงเงินงบประมาณ" v-model="store.body.budget" rules="required" grouping :min-fraction-digits="2"
            :disabled="!store.canEdit || store.body.isCancel || !menuStore.hasManage" />
          <PrimeVueDatePicker label="ประมาณการช่วงเวลาการจัดซื้อจัดจ้าง" v-model="store.body.expectingProcurementAt"
            rules="required" :disabled="!store.canEdit || !menuStore.hasManage || !store.body.budgetYear"
            view="smallmonth" />
        </div>
        <InputArea label="เหตุผลที่ทำแผนระหว่างปี" :row="5" v-model="store.body.remark"
          :rules="(store.body.type == ProcurementPlanType.InYearPlan && store.body.budget > 500000) ? 'required' : ''"
          :disabled="!store.canEdit || !menuStore.hasManage"
          v-if="store.body.type === ProcurementPlanType.InYearPlan" />

        <InputArea label="เหตุผลการขอยกเลิก" :row="5" v-model="store.body.cancelReason"
          :disabled="!store.canEdit || !menuStore.hasManage" v-if="store.body.isCancel" />

        <InputArea label="เหตุผลการขอเปลี่ยนแปลง" :row="5" v-model="store.body.changeReason"
          :disabled="!store.canEdit || !menuStore.hasManage" v-if="store.body.isChange" />

        <InputArea label="หมายเหตุการปิดงาน" :row="5" v-model="store.body.remarkClosed"
          disabled v-if="store.body.remarkClosed" />

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <InputField label="เบอร์โทร" v-model="store.body.telephone" :disabled="!store.canEdit || !menuStore.hasManage"
            rules="required" />
        </div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-4" v-if="isJopPorDepartment">
        <Radio label="(Stock) เป็นรายการวัสดุเครื่องเขียนแบบพิมพ์ และวัสดุของใช้สิ้นเปลืองคลังพัสดุ"
          :options="stockOption" v-model="store.body.isStock" :disabled="!store.canEdit || !menuStore.hasManage" />
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4" v-if="canDraftRecordDocument">
        <InputField v-if="checkIsSixty(store.body.supplyMethodCode)" id="pl001-group-egp-number" label="เลขกลุ่ม e-GP"
          v-model="store.body.groupEgpNumber" :disabled="!canEditEGP || !menuStore.hasManage" rules="required" />
        <InputField v-if="checkIsSixty(store.body.supplyMethodCode)" id="pl001-egp-number" label="เลขที่ e-GP" v-model="store.body.egpNumber"
          :disabled="!canEditEGP || !menuStore.hasManage" rules="required" />
      </div>

    </template>
  </Card>
  <Card class="mt-4" v-if="checkPlanDepartmentCode">
    <template #content>
      <Field v-model="store.body.isCommercialMaterial" name="isCommercialMaterial"
        :rules="(val: unknown) => val !== null || 'กรุณาเลือกข้อมูล'" v-slot="{ errorMessage }" as="div">
        <p>การจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง<span class="text-red-500">*</span></p>
        <div v-for="option in commercialOption" :key="option.label" class="flex gap-2">
          <RadioButton v-model="store.body.isCommercialMaterial" :inputId="option.label" name="dynamic"
            :value="option.value" :disabled="!store.canEdit || !menuStore.hasManage" class="mt-1" />
          <label :for="option.label" v-html="option.label"></label>
        </div>
        <small class="pl-2 text-red-500!" v-if="errorMessage">{{ errorMessage }}</small>
      </Field>
    </template>
  </Card>
</template>
