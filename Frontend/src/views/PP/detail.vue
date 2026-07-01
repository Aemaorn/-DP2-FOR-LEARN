<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Form } from 'vee-validate';
import { Button } from 'primevue';
import Dialog from 'primevue/dialog';
import type { MenuItem } from 'primevue/menuitem';
import { PreProcurementStep, PreProcurementType } from '@/enums/preProcurement';
import { TitleHeader, InfoItem } from '@/components/cosmetic';
import { InputField, InputNumber, UploadFileGroup } from '@/components/forms';
import ToastHelper from '@/helpers/toast';
import { formatCurrency } from '@/helpers/currency';
import { usePPDetailStore } from '../../stores/PP/ppStore';
import { TypeBadgeChip, PreProcurementStep as PreProcurementStepComponents, ProcurementDialog } from './components/PP';
import { PP001, PP002, PP003, PP004, PP005, PP006, PP007, PP008, PP009, PP010 } from './components';
import { PreProcurementConstants } from '@/constants';
import { ProcurementStatus } from '@/enums/procurement';
import { useMenuStore } from '@/stores/menu';
import { DepartmentId } from '@/enums/businessUnit';
import procurementHelper from '@/helpers/procurement';
import { ButtonClose, ButtonCancelClose } from '@/components/Button';
import { showReasonDialogAsync, showConfirmDialogAsync } from '@/helpers/dialog';
import { ReasonDialogType, ConfirmDialogType } from '@/enums/dialog';

const { ProcurementBadgeStatus } = procurementHelper;
const { PreProcurementTypeName } = PreProcurementConstants;
const preProcurementDetailStore = usePPDetailStore();
const menuStore = useMenuStore();

const route = useRoute();
const router = useRouter();

const routeItems = ref([
  { label: 'รายการโครงการจัดซื้อจัดจ้าง', url: '/pp' },
  { label: 'จัดการข้อมูลรายการจัดซื้อจัดจ้าง' },
] as MenuItem[]);
const current = ref(preProcurementDetailStore.procurementDetail.currentStep);

const isJorPorDepartmentCode = computed(() => {
  return preProcurementDetailStore.procurementDetail.departmentCode === DepartmentId.JorPor;
});

onMounted(async (): Promise<void> => {
  if (route.params.id) {
    await preProcurementDetailStore.onGetProcurementById(route.params.id.toString());
    current.value = preProcurementDetailStore.procurementDetail.currentStep;
  }
});
const showConfirmModal = ref(false);
const editBudget = ref<number | undefined>(undefined);

const onSubmit = (): void => {
  if (preProcurementDetailStore.procurementDetail.budget > preProcurementDetailStore.procurementDetail.planbudget) {
    ToastHelper.error("ข้อมูลรายการจัดซื้อจัดจ้าง", `วงเงินงบประมาณ ไม่สามารถเกิด ${preProcurementDetailStore.procurementDetail.planbudget}`);
    return;
  }

  editBudget.value = preProcurementDetailStore.procurementDetail.budget;
  showConfirmModal.value = true;
}

const onConfirmCreate = async (): Promise<void> => {
  const planbudget = preProcurementDetailStore.procurementDetail.planbudget;

  if (editBudget.value === undefined || editBudget.value <= 0) {
    ToastHelper.error("ข้อมูลรายการจัดซื้อจัดจ้าง", `กรุณาระบุวงเงินงบประมาณ`);
    return;
  }
  if (editBudget.value > planbudget) {
    ToastHelper.error("ข้อมูลรายการจัดซื้อจัดจ้าง", `วงเงินงบประมาณ ไม่สามารถเกิด ${planbudget}`);
    return;
  }

  preProcurementDetailStore.procurementDetail.budget = editBudget.value;
  showConfirmModal.value = false;

  const newId = await preProcurementDetailStore.onCreateProcurement();

  if (newId) {
    router.replace(`detail/${newId}`);
    preProcurementDetailStore.onGetProcurementById(newId);
  }
}

onUnmounted(() => {
  preProcurementDetailStore.resetPreProcurementDetail();
});

const showDialogProcurement = ref(false);

const onShowDialogProcurement = () => {
  showDialogProcurement.value = true;
};

const onOpenPlanDetail = (planId: string): void => {
  const route = router.resolve({ name: 'pl001Detail', params: { id: planId } });
  window.open(route.href, '_blank');
};

const onSelectPreProcurementStep = (step: PreProcurementStep): void => {
  current.value = step;
};

watch(() => preProcurementDetailStore.procurementDetail.currentStep, (val: PreProcurementStep, oldVal: PreProcurementStep) => {
  if (route.params.id && oldVal === PreProcurementStep.Jp005 && val === PreProcurementStep.PurchaseRequisition) {
    current.value = val;
  }
});

const onCloseProcurementAsync = async (): Promise<void> => {
  const res = await showReasonDialogAsync(ReasonDialogType.ClosePlan, true);
  if (res.isConfirm) {
    await preProcurementDetailStore.onCloseProcurementAsync(res.reason ?? '', res.attachments);
  }
};

const onCancelCloseProcurementAsync = async (): Promise<void> => {
  const isConfirm = await showConfirmDialogAsync(ConfirmDialogType.CancelClosePlan);
  if (isConfirm) {
    await preProcurementDetailStore.onCancelCloseProcurementAsync();
  }
};
</script>

<template>
  <TitleHeader :route-items="routeItems" label="จัดการข้อมูลรายการจัดซื้อจัดจ้าง"> </TitleHeader>
  <Card class="my-4">
    <template #content>
      <TitleHeader label="ข้อมูลรายการจัดซื้อจัดจ้าง">
        <template #action>
          <BadgeStatus prefix="สถานะ: "
            :color="ProcurementBadgeStatus(preProcurementDetailStore.procurementDetail.procurementStatus).color"
            :label="ProcurementBadgeStatus(preProcurementDetailStore.procurementDetail.procurementStatus).label"
            v-if="preProcurementDetailStore.procurementDetail.procurementStatus" />
        </template>
      </TitleHeader>
      <Form @submit="onSubmit">
        <div class="flex justify-between">
          <div class="w-full mt-4">
            <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mb-2">
              <InfoItem v-if="preProcurementDetailStore.procurementDetail.appointNumber" class="col-span-3"
                title="เลขที่อ้างอิงการจัดซื้อจัดจ้าง">
                <template #content>
                  <p>{{
                    preProcurementDetailStore.procurementDetail.appointNumber }}</p>
                </template>
              </InfoItem>

              <InfoItem title="เลขที่อ้างอิงรายการจัดซื้อจัดจ้าง">
                <template #content>
                  <InputField v-if="!route.params.id && menuStore.hasManage"
                    v-model="preProcurementDetailStore.procurementDetail.planNumber" rules="required" class="w-4/5"
                    disabled>
                    <template #appendAction>
                      <InputGroupAddon v-if="!preProcurementDetailStore.procurementDetail.id && menuStore.hasManage">
                        <Button label="ค้นหา"
                          class="rounded-l-none rounded-r-none text-white! bg-gray-500! border-none! h-full"
                          @click="onShowDialogProcurement" />
                      </InputGroupAddon>
                    </template>
                  </InputField>
                  <p v-else-if="preProcurementDetailStore.procurementDetail.planId">
                    <span class="underline text-blue-500 cursor-pointer w-fit"
                      @click="onOpenPlanDetail(preProcurementDetailStore.procurementDetail.planId)">
                      {{ preProcurementDetailStore.procurementDetail.planNumber }}
                    </span>
                  </p>
                  <p v-else>{{ preProcurementDetailStore.procurementDetail.planNumber }}</p>
                </template>
              </InfoItem>

              <InfoItem v-if="preProcurementDetailStore.procurementDetail.planId" title="ฝ่าย/ภาคเขต"
                :content="preProcurementDetailStore.procurementDetail.departmentName" />
              <InfoItem
                v-if="preProcurementDetailStore.procurementDetail.planId && preProcurementDetailStore.procurementDetail.planType"
                title="ประเภทแผน" :content="(preProcurementDetailStore.procurementDetail.planType as string)">
                <template #content="{ item }">
                  <TypeBadgeChip :label="PreProcurementTypeName(item as PreProcurementType)" size="Small"
                    :color="(item as string)" class="w-fit" />
                </template>
              </InfoItem>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-3 items-center gap-2 mb-2"
              v-if="preProcurementDetailStore.procurementDetail.planId">
              <InfoItem title="โครงการ" :content="preProcurementDetailStore.procurementDetail.planName ?? ''" />
              <InfoItem title="ปีงบประมาณ" :content="preProcurementDetailStore.procurementDetail.budgetYear ?? ''" />
              <InfoItem title="วงเงินงบประมาณ" content="">
                <template #content>
                  <InputNumber v-if="!route.params.id" v-model="preProcurementDetailStore.procurementDetail.budget"
                    class="w-1/2 mt-2"
                    :rules="`required|max_value:${preProcurementDetailStore.procurementDetail.planbudget}`" grouping
                    :min-fraction-digits="2" />
                  <p v-else>{{ formatCurrency(preProcurementDetailStore.procurementDetail.budget) }}</p>
                </template>
              </InfoItem>
            </div>

            <div class="grid grid-cols-3 lg:grid-cols-3 gap-2 mb-2"
              v-if="preProcurementDetailStore.procurementDetail.planId">
              <InfoItem title="วิธีจัดหา" :content="preProcurementDetailStore.procurementDetail.supplyMethod ?? ''" />
              <InfoItem title="" :content="preProcurementDetailStore.procurementDetail.supplyMethodType ?? ''" />
              <InfoItem title="" :content="preProcurementDetailStore.procurementDetail.supplyMethodSpecialType ?? ''" />
            </div>

            <div class="grid grid-cols-3 lg:grid-cols-3 gap-2 mb-2"
              v-if="preProcurementDetailStore.procurementDetail.planId && isJorPorDepartmentCode">
              <InfoItem title="(Stock) เป็นรายการวัสดุเครื่องเขียนแบบพิมพ์ และวัสดุของใช้สิ้นเปลืองคลังพัสดุ"
                :content="preProcurementDetailStore.procurementDetail.isStock ? 'ใช่' : 'ไม่ใช่'" />
            </div>

            <div v-if="preProcurementDetailStore.procurementDetail.isCommercialMaterial">
              <InfoItem title="การจัดซื้อจัดจ้างและการบริหารพัสดุที่เกี่ยวกับการพาณิชย์โดยตรง"
                :content="preProcurementDetailStore.procurementDetail.isCommercialMaterial ? 'ใช่' : 'ไม่ใช่'" />
            </div>

            <div v-if="preProcurementDetailStore.procurementDetail.remarkClosed">
              <InfoItem title="หมายเหตุการปิดงาน"
                :content="preProcurementDetailStore.procurementDetail.remarkClosed" />
            </div>
          </div>
          <div v-if="!preProcurementDetailStore.procurementDetail.id && menuStore.hasManage">
            <Button label="บันทึก" icon="pi pi-save" severity="success" type="submit" />
          </div>
        </div>
        <div v-if="preProcurementDetailStore.canCloseProcurement" class="flex justify-end">
          <ButtonClose @click="onCloseProcurementAsync" />
        </div>
        <div v-if="preProcurementDetailStore.canCancelCloseProcurement" class="flex justify-end">
          <ButtonCancelClose @click="onCancelCloseProcurementAsync" />
        </div>
      </Form>
    </template>
  </Card>

  <PreProcurementStepComponents v-if="preProcurementDetailStore.procurementDetail.id" v-model="current"
    v-model:steps="preProcurementDetailStore.procurementDetail.steps"
    @on-select-pre-procurement-step="onSelectPreProcurementStep" />


  <div v-if="preProcurementDetailStore.procurementDetail.id">
    <PP001 v-if="
      current === PreProcurementStep.Appoint.toString()"
      :pre-procurement-id="preProcurementDetailStore.procurementDetail.id"
      :budget="preProcurementDetailStore.procurementDetail.budget"
      :id="preProcurementDetailStore.procurementDetail.appoint?.id"
      :budget-year="Number(preProcurementDetailStore.procurementDetail.budgetYear)"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled">
    </PP001>
    <PP002 v-if="current === PreProcurementStep.TorDraft.toString()"
      :procurementId="preProcurementDetailStore.procurementDetail.id"
      :tor-id="preProcurementDetailStore.procurementDetail.torDraft?.id"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP003 v-if="current === PreProcurementStep.MedianPrice.toString()"
      :procurementId="preProcurementDetailStore.procurementDetail.id"
      :id="preProcurementDetailStore.procurementDetail.medianPrice?.id"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP004 v-if="
      current === PreProcurementStep.PurchaseRequisition"
      :pre-procurement-id="preProcurementDetailStore.procurementDetail.id"
      :budget="preProcurementDetailStore.procurementDetail.budget"
      :id="preProcurementDetailStore.procurementDetail.purchaseRequisition?.id"
      :tor-id="preProcurementDetailStore.procurementDetail.torDraft?.id"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled">
    </PP004>

    <PP005 :procurementId="preProcurementDetailStore.procurementDetail.id"
      :id="preProcurementDetailStore.procurementDetail.jp005?.id" v-if="current === PreProcurementStep.Jp005"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP006 v-if="current === PreProcurementStep.Invite" :procurementId="preProcurementDetailStore.procurementDetail.id"
      :invite-id="preProcurementDetailStore.procurementDetail.invite?.id"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP007 v-if="current === PreProcurementStep.PurchaseOrder"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP008 v-if="current === PreProcurementStep.PurchaseOrderApproval"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP009 v-if="current === PreProcurementStep.ContractInvitation"
      :procurementId="preProcurementDetailStore.procurementDetail.id"
      :id="preProcurementDetailStore.procurementDetail.contractInvitation?.id"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />
    <PP010 v-if="current === PreProcurementStep.ContractDraft"
      :readonly="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled" />

    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2 z-50" v-if="current != PreProcurementStep.PurchaseOrder">
      <div class="lg:col-span-5 mb-5 my-1">
        <UploadFileGroup v-model="preProcurementDetailStore.procurementDetail.attachments"
          @upload="preProcurementDetailStore.onUpsertAttachments"
          @remove-file="preProcurementDetailStore.onUpsertAttachments"
          @remove-group="preProcurementDetailStore.onUpsertAttachments"
          @reorder="preProcurementDetailStore.onUpsertAttachments"
          :disabled="preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Completed || preProcurementDetailStore.procurementDetail.procurementStatus === ProcurementStatus.Cancelled || !menuStore.hasManage"
          :isShowActivityDialog="true"
          :isShowLinkFileAll="true"
          :id="preProcurementDetailStore.procurementDetail.id" />
      </div>
    </div>
  </div>

  <ProcurementDialog v-model="showDialogProcurement">
  </ProcurementDialog>

  <Dialog v-model:visible="showConfirmModal" header="ยืนยันสร้างข้อมูล" modal :style="{ width: '450px' }"
    :closable="false">
    <p class="mb-4">ต้องการยืนยันสร้างข้อมูลหรือไม่?</p>
    <div class="mb-2">
      <label class="text-sm font-medium block mb-1">วงเงินงบประมาณ <span class="text-red-500">*</span></label>
      <InputNumber v-model="editBudget" class="w-full" grouping :min-fraction-digits="2"
        :max="preProcurementDetailStore.procurementDetail.planbudget" />
    </div>
    <template #footer>
      <Button label="ยกเลิก" severity="secondary" outlined @click="showConfirmModal = false" />
      <Button label="ยืนยัน" severity="success" icon="pi pi-check" @click="onConfirmCreate" />
    </template>
  </Dialog>
</template>

<style scoped lang="scss">
.labeled-text {
  display: flex;
  flex-direction: column;
  top: 0;
  gap: 0;
}

.label {
  font-size: 16px;
  font-weight: 500;
  color: #bbbbbb;
}

.text {
  font-size: 20px;
  font-weight: 400;
  color: #374151;
}

.text-gray-500 {
  color: #6b7280;
}
</style>
