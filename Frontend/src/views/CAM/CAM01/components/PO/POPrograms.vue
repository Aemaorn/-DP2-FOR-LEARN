<script setup lang="ts">
import Cam01Constants from '@/constants/CAM/CAM01/cam01';
import { Cam01PoStep, Cam01Status } from '@/enums/CAM/CAM01/cam01';
import { useCam01DetailStore } from '@/stores/CAM/CAM01/cam01.detail';
import { computed, defineAsyncComponent, ref } from 'vue';

const store = useCam01DetailStore();

type Props = {
  steps: Array<Cam01PoStep>,
}

const { Cam01PoProgramName } = Cam01Constants;

const { steps } = defineProps<Props>();
const currentStep = defineModel<Cam01PoStep>(
  {
    required: true,
  }
);

const programSelected = ref<Cam01PoStep>(currentStep.value);

const onSetProgram = (value: Cam01PoStep) => {
  programSelected.value = value;
};

const onGetStatusBtn = (step: Cam01PoStep) => {
  if (isButtonDisabled(step).value) {
    return 'w-full mb-5 bg-black border-none rounded-none text-white';
  }

  if (store.body.status === Cam01Status.Completed && steps[steps.length - 1] === step) {
    if (programSelected.value === step) {
      return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
    }

    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (programSelected.value === step && steps[steps.length - 1] === programSelected.value) {
    return 'w-full mb-5 bg-[#F9A825] border-none rounded-none text-white';
  }

  if (programSelected.value === step) {
    return 'w-full mb-5 bg-[#00A160] border-none rounded-none text-white';
  }

  if (steps.filter(f => f !== currentStep.value).includes(step)) {
    return 'w-full mb-5 bg-white border-[#00A160] rounded-none text-[#00A160]';
  }

  if (!steps.filter(f => f !== currentStep.value).includes(step)) {
    return 'w-full mb-5 border-[#F9A825] bg-white text-[#F9A825] rounded-none';
  }

  return 'w-full mb-5 bg-gray-400 border-none rounded-none text-white';
};

const isButtonDisabled = (step: Cam01PoStep) => computed(() => {
  return ![...steps, programSelected.value].includes(step);
});

const PoAddendum = defineAsyncComponent(() => import('@/views/CAM/CAM01/components/PO/PoAddendum/PoAddendum.vue'));
const PoSap = defineAsyncComponent(() => import("@/views/CAM/CAM01/components/PO/PoSap/PoSap.vue"));
</script>

<template>
  <div class="my-5">
    <div class="grid grid-cols-2 text-center gap-4 content">
      <Button :class="`${onGetStatusBtn(data)} w-full mt-2`"
        v-for="data in Object.entries(Cam01PoStep).map(([, value]) => value)" :key="data"
        @click="() => onSetProgram(data)" :disabled="isButtonDisabled(data).value">
        {{ Cam01PoProgramName(data) }}
      </Button>
    </div>
  </div>
  <div>
    <PoAddendum v-if="programSelected === Cam01PoStep.PoAddendum" />
    <PoSap v-if="programSelected === Cam01PoStep.PoSap" />
  </div>
</template>