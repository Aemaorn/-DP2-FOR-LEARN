<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { UploadFileGroup } from '@/components/forms';
import { PreProcurementStep } from '@/enums/preProcurement';
import { ProcurementStatus, ProcurementStep } from '@/enums/procurement';
import type { ProgramMenuType } from '@/models/PCM/PCM005/pcm005';
import { useMenuStore } from '@/stores/menu';
import { usePcm005DetailStore } from '@/stores/PCM/PCM005/pcm005';
import { Button } from 'primevue';
import type { MenuItem } from 'primevue/menuitem';
import { computed, defineAsyncComponent, onBeforeMount, ref, watch } from 'vue';
import { useRoute } from 'vue-router';

const Detail = defineAsyncComponent(() => import('./components/Detail.vue'));
const Principle = defineAsyncComponent(() => import('./components/Principle/Principle.vue'));
const PrincipleApprovalRental = defineAsyncComponent(() => import('./components/PrincipleApprovalRental/PrincipleApprovalRental.vue'));
const Approve = defineAsyncComponent(() => import('./components/Approve/Approve.vue'));
const ContractInvitation = defineAsyncComponent(() => import('./components/ContractInvitation/ContractInvitation.vue'));
const ContractAndAgreements = defineAsyncComponent(() => import('./components/ContractsAndAgreements/ContractsAndAgreements.vue'));

const store = usePcm005DetailStore();
const menuStore = useMenuStore();
const route = useRoute();

const current = ref(store.body.currentStep);

const routeItems = ref(
  [
    { label: 'รายการเช่าพื้นที่/อาคาร/ที่จอดรถ/ป้าย', url: '/pcm/pcm005' },
    { label: 'จัดการรายการเช่าพื้นที่/อาคาร/ที่จอดรถ/ป้าย', },
  ] as MenuItem[]);
const programMenu = ref<ProgramMenuType>({
  procurement: [
    { menu: 'ขออนุมัติหลักการ', status: 'Waiting', name: PreProcurementStep.PrincipleApproval },
    { menu: 'ขออนุมัติเช่า', status: 'Waiting', name: PreProcurementStep.PrincipleApprovalRental },
    { menu: 'อนุมัติใบสั่งเช่า และแจ้งทำสัญญา', status: 'Waiting', name: PreProcurementStep.PurchaseOrderApproval },
  ],
  contractAgreement: [
    { menu: 'หนังสือเชิญชวนทำสัญญา', status: 'Waiting', name: PreProcurementStep.ContractInvitation },
    { menu: 'ร่างสัญญาและสัญญา', status: 'Waiting', name: PreProcurementStep.ContractDraft },
  ],
});
const menuSelected = ref<PreProcurementStep>();

const id = computed(() => route.params.id as string);

onBeforeMount(async () => {
  store.onResetBody();

  await initAsync();
});

const initAsync = async () => {
  await store.getDDLAsync();
  if (store.body.supplyMethodCode) {
    await store.getsmSpTypeCodeDDLAsync(store.body.supplyMethodCode);
  }

  if (id.value) {
    await store.getDetailAsync(id.value);
    onChangeProgram(store.body.currentStep, store.body.procurementStep);
    menuSelected.value = store.body.currentStep;
  }
};

const onChangeProgram = (name: PreProcurementStep, type: ProcurementStep): void => {
  menuSelected.value = name;

  if (type === ProcurementStep.Procurement) {
    current.value = name;
  }

  if (type === ProcurementStep.ContractAgreement) {
    current.value = name;
  }
};

const isCurrentStep = (step: string) => {
  return current.value === step;
};

const getStatusButton = (step: PreProcurementStep): string => {
  if (
    step === PreProcurementStep.ContractDraft &&
    store.body.steps[store.body.steps.length - 1] === step &&
    store.body.contractDraft?.status === 'Approved'
  ) {
    if (isCurrentStep(step)) {
      return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
    }
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (
    step === PreProcurementStep.PurchaseOrderApproval &&
    store.body.steps[store.body.steps.length - 1] === step &&
    store.body.purchaseOrderApproval?.status === 'Approved'
  ) {
    if (isCurrentStep(step)) {
      return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
    }
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (
    (step === PreProcurementStep.PurchaseOrderApproval &&
      !isCurrentStep(step) &&
      store.body.purchaseOrderApproval?.status === 'Assigned' &&
      store.body.status === ProcurementStatus.Completed)
  ) {
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (isButtonDisabled(step).value) {
    return 'w-full mb-5 bg-black border-none rounded-none text-white';
  }

  if (isCurrentStep(step) && store.body.steps[store.body.steps.length - 1] == current.value) {
    return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
  }

  if (isCurrentStep(step)) {
    return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
  }

  if (store.body.steps.filter(f => f !== store.body.currentStep).includes(step)) {
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (!store.body.steps.filter(f => f !== store.body.currentStep).includes(step)) {
    return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
  }

  return 'w-full mb-5 bg-gray-400 border-none rounded-none text-white';
};

const isButtonDisabled = (step: string) => computed(() => {
  return ![...store.body.steps, menuSelected].includes(step as PreProcurementStep);
});

watch(() => store.body.currentStep, (val: PreProcurementStep) => {
  if (val) {
    onChangeProgram(val, store.body.procurementStep);
  };
});

const isCancelled = computed(() => store.body.status === ProcurementStatus.Cancelled);

</script>

<template>
  <TitleHeader label="จัดการรายการเช่าพื้นที่/อาคาร/ที่จอดรถ/ป้าย" :routeItems="routeItems" />
  <Detail />
  <div v-if="!store.states.isDraft">
    <div class="mt-5">
      <div class="grid grid-cols-2 text-center ">
        <p class="text-primary text-[30px]">Procurement</p>
        <p class="text-primary text-[30px]">Contract Agreement</p>
      </div>
      <hr class="mt-3 text-primary border-1" />
      <div class="grid grid-cols-2 gap-4 content">
        <div>
          <Button :class="`${getStatusButton(data.name)} w-full mt-2`" v-for="data in programMenu.procurement"
            :key="data.menu" :disabled="isButtonDisabled(data.name).value"
            @click="() => onChangeProgram(data.name, ProcurementStep.Procurement)">
            {{ data.menu }}
          </Button>
        </div>
        <div>
          <Button :class="`${getStatusButton(data.name)} w-full mt-2`" v-for="data in programMenu.contractAgreement"
            :key="data.menu" :disabled="isButtonDisabled(data.name).value"
            @click="() => onChangeProgram(data.name, ProcurementStep.ContractAgreement)">
            {{ data.menu }}
          </Button>
        </div>
      </div>
    </div>
    <div>
      <Principle v-if="menuSelected === PreProcurementStep.PrincipleApproval" :readonly="isCancelled" />
      <PrincipleApprovalRental v-else-if="menuSelected === PreProcurementStep.PrincipleApprovalRental" :readonly="isCancelled" />
      <Approve v-else-if="menuSelected === PreProcurementStep.PurchaseOrderApproval" :readonly="isCancelled" />
      <ContractInvitation v-else-if="menuSelected === PreProcurementStep.ContractInvitation" :readonly="isCancelled" />
      <ContractAndAgreements v-else-if="menuSelected === PreProcurementStep.ContractDraft" />
    </div>
    <div class="grid grid-cols-1 lg:grid-cols-7 gap-2 z-50">
      <div class="lg:col-span-5 mb-5 my-1">
        <UploadFileGroup v-model="store.body.attachments" @upload="store.onUpsertAttachments"
          @remove-file="store.onUpsertAttachments" @remove-group="store.onUpsertAttachments"
          @reorder="store.onUpsertAttachments"
          :disabled="store.body.status === ProcurementStatus.Completed || store.body.status === ProcurementStatus.Cancelled || !menuStore.hasManage"
          :isShowActivityDialog="true"
          :isShowLinkFileAll="true"
          :id="store.body.id" />
      </div>
    </div>
  </div>
</template>

<style scoped lang="scss">
$waiting-color: #BDBDBD;
$progress-color: #FFBF00;
$success-color: #00A160;

.content {
  color: white;
  margin: 10px;
  text-align: center;

  .waiting-content {
    background-color: $waiting-color;
  }

  .progress-content {
    background-color: $progress-color;
  }

  .success-content {
    background-color: $success-color;
  }
}
</style>
