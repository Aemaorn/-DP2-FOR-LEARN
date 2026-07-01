<script setup lang="ts">
import PreProcurementConstants from '@/constants/preProcurement';
import { PreProcurementStep } from '@/enums/preProcurement';
import { usePPDetailStore } from '../../../../stores/PP/ppStore';
import { computed } from 'vue';
import { ProcurementStatus } from '@/enums/procurement';

const value = defineModel<PreProcurementStep | null>({
  required: true,
});

const steps = defineModel<Array<PreProcurementStep>>('steps', {
  required: true,
});

const emit = defineEmits(['onSelectPreProcurementStep']);

const { PreProcurementStepFullName } = PreProcurementConstants;
const procurementStore = usePPDetailStore();

const isCurrentStep = (step: string) => {
  return value.value === step;
};

const isButtonDisabled = (step: string) => computed(() => {

  if ((step === PreProcurementStep.ContractDraft || step === PreProcurementStep.ContractInvitation)
    && procurementStore.procurementDetail.status === ProcurementStatus.Completed
    && procurementStore.procurementDetail.contractType === 'CType002') {
    return true;
  }

  return !steps.value.includes(step as PreProcurementStep);
});

const getButtonClass = (step: PreProcurementStep) => {
  if (
    (step === PreProcurementStep.ContractDraft &&
      steps.value[steps.value.length - 1] === step &&
      procurementStore.procurementDetail.contractDraft?.status === 'Approved')
  ) {
    if (isCurrentStep(step)) {
      return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
    }
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (
    (step === PreProcurementStep.PurchaseOrderApproval &&
      steps.value[steps.value.length - 1] === step &&
      procurementStore.procurementDetail.purchaseOrderApproval?.status === 'Assigned')
  ) {
    if (isCurrentStep(step)) {
      return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
    }
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (
    (step === PreProcurementStep.PurchaseOrderApproval &&
      !isCurrentStep(step) &&
      procurementStore.procurementDetail.purchaseOrderApproval?.status === 'Assigned' &&
      procurementStore.procurementDetail.status === ProcurementStatus.Completed)
  ) {
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (isButtonDisabled(step).value) {
    return 'w-full mb-5 bg-black border-none rounded-none text-white';
  }

  if (isSelectType(step) && isCurrentStep(step)) {
    return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
  }

  if (isSelectType(step) && steps.value.includes(step as PreProcurementStep)) {
    return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
  }

  if (isCurrentStep(step) && steps.value[steps.value.length - 1] == value.value) {
    return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
  }

  if (isCurrentStep(step)) {
    return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
  }

  if (steps.value.filter(f => f !== procurementStore.procurementDetail.currentStep).includes(step)) {
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (!steps.value.filter(f => f !== procurementStore.procurementDetail.currentStep).includes(step)) {
    return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
  }

  return 'w-full mb-5 bg-gray-400 border-none rounded-none text-white';
};

const onSelectPreProcurementStep = (step: string) => {
  if (isButtonDisabled(step).value) {
    return;
  }

  emit('onSelectPreProcurementStep', step);
};

const isSelectType = (currentStep: PreProcurementStep) => {
  return !procurementStore.procurementDetail.appoint && !procurementStore.procurementDetail.purchaseRequisition && !procurementStore.procurementDetail.purchaseOrderApproval && steps.value.includes(currentStep);
}
</script>

<template>
  <div class="grid grid-cols-3 gap-4">
    <div class="center">
      <h5 class="text-primary">Pre-Procurement</h5>
    </div>
    <div class="center">
      <h5 class="text-primary">Procurement</h5>
    </div>
    <div class="center">
      <h5 class="text-primary">Contract Agreement</h5>
    </div>
  </div>
  <hr />
  <div class="grid grid-cols-3 gap-4">
    <div class="center">
      <Button :class="getButtonClass(PreProcurementStep.Appoint)"
        :disabled="isButtonDisabled(PreProcurementStep.Appoint).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.Appoint)">
        <span class="text-wrap">
          {{ PreProcurementStepFullName(PreProcurementStep.Appoint) }}
        </span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.TorDraft)"
        :disabled="isButtonDisabled(PreProcurementStep.TorDraft).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.TorDraft)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.TorDraft) }}</span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.MedianPrice)"
        :disabled="isButtonDisabled(PreProcurementStep.MedianPrice).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.MedianPrice)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.MedianPrice) }}</span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.PurchaseRequisition)"
        :disabled="isButtonDisabled(PreProcurementStep.PurchaseRequisition).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.PurchaseRequisition)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.PurchaseRequisition) }}</span>
      </Button>
    </div>
    <div class="center">
      <Button :class="getButtonClass(PreProcurementStep.Jp005)"
        :disabled="isButtonDisabled(PreProcurementStep.Jp005).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.Jp005)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.Jp005) }}</span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.Invite)"
        :disabled="isButtonDisabled(PreProcurementStep.Invite).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.Invite)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.Invite) }}</span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.PurchaseOrder)"
        :disabled="isButtonDisabled(PreProcurementStep.PurchaseOrder).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.PurchaseOrder)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.PurchaseOrder) }}</span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.PurchaseOrderApproval)"
        :disabled="isButtonDisabled(PreProcurementStep.PurchaseOrderApproval).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.PurchaseOrderApproval)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.PurchaseOrderApproval) }}</span>
      </Button>
    </div>
    <div class="center">
      <Button :class="getButtonClass(PreProcurementStep.ContractInvitation)"
        :disabled="isButtonDisabled(PreProcurementStep.ContractInvitation).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.ContractInvitation)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.ContractInvitation) }}</span>
      </Button>
      <Button :class="getButtonClass(PreProcurementStep.ContractDraft)"
        :disabled="isButtonDisabled(PreProcurementStep.ContractDraft).value"
        @click="onSelectPreProcurementStep(PreProcurementStep.ContractDraft)">
        <span>{{ PreProcurementStepFullName(PreProcurementStep.ContractDraft) }}</span>
      </Button>
    </div>
  </div>
</template>

<style scoped lang="scss">
.center {
  text-align: center;
}

hr {
  display: block;
  height: 1px;
  border: 0;
  border-top: 2px solid var(--color-primary);
  margin: 1em 0;
  padding: 0;
}

.disabled-btn {
  cursor: not-allowed;
}

.text-wrap {
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}
</style>
