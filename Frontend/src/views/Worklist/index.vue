<script setup lang="ts">
import { ButtonClear, ButtonSearch } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { BudgetYearSelect, InputField, Select, StatusGroupButton } from '@/components/forms';
import { WorklistType } from '@/enums/worklist';
import type { OptionBadge } from '@/models/shared/option';
import { useWlStore } from '@/stores/WorkList/wl';
import { Card, Checkbox } from 'primevue';
import { Form } from 'vee-validate';
import { onMounted, ref, watch } from 'vue';
import All from './components/All.vue';
import ContractAgreement from './components/ContractAgreement.vue';
import ContractManagement from './components/ContractManagement.vue';
import Plan from './components/Plan.vue';
import PlanAnnouncement from './components/PlanAnnouncement.vue';
import PreProcurement from './components/PreProcurement.vue';
import Procurement from './components/Procurement.vue';

const store = useWlStore();

const optionSelected = ref(WorklistType.Combined);
const isExpanded = ref<boolean>(false);
const options = ref<Array<OptionBadge>>([
  {
    label: 'ทั้งหมด',
    count: 0,
    value: WorklistType.Combined,
    bgColorClass: 'bg-[#FAFAFA]',
    textColorClass: 'text-black',
  },
  {
    label: 'Plan',
    count: 0,
    value: WorklistType.Plan,
    bgColorClass: 'bg-[#E6F0FF]',
    textColorClass: 'text-black',
  },
  {
    label: 'Plan Announcement',
    count: 0,
    value: WorklistType.PlanAnnouncement,
    bgColorClass: 'bg-[#E0FFF4]',
    textColorClass: 'text-black',
  },
  {
    label: 'Pre-Procurement',
    count: 0,
    value: WorklistType.PreProcurement,
    bgColorClass: 'bg-[#F3FFE0]',
    textColorClass: 'text-black',
  },
  {
    label: 'Procurement',
    count: 0,
    value: WorklistType.Procurement,
    bgColorClass: 'bg-[#FFF1CC]',
    textColorClass: 'text-black',
  },
  {
    label: 'Contract Agreement',
    count: 0,
    value: WorklistType.ContractAgreement,
    bgColorClass: 'bg-[#FFD6CC]',
    textColorClass: 'text-black',
  },
  {
    label: 'Contract Management',
    count: 0,
    value: WorklistType.ContractManagement,
    bgColorClass: 'bg-[#F0E0FF]',
    textColorClass: 'text-black',
  },
]);

onMounted(async (): Promise<void> => {
  await initAsync();
});

const initAsync = async (): Promise<void> => {
  await Promise.all([
    store.getDepartmentDDLAsync(),
    store.getSupplyMethodDDLAsync(),
    store.getSupplyMethodTypeDDLAsync(),
  ]);

  await onGetWorklistWithTypeAsync();
};

const onGetWorklistWithTypeAsync = async (): Promise<void> => {
  store.onResetWorklistType();

  switch (optionSelected.value) {
    case WorklistType.Plan:
      store.criteria.includePlans = true;

      break;
    case WorklistType.PlanAnnouncement:
      store.criteria.includeAnnouncements = true;

      break;
    case WorklistType.PreProcurement:
      store.criteria.includePreProcurement = true;

      break;
    case WorklistType.Procurement:
      store.criteria.includeProcurement = true;

      break;
    case WorklistType.ContractAgreement:
      store.criteria.includeContractAgreement = true;

      break;
    case WorklistType.ContractManagement:
      store.criteria.includeContractManagement = true;
      store.criteria.includeContractAmendment = true;

      break;
    default:
      store.criteria.includeAll = true;
  }

  await store.getListAsync();
};

watch((): string | undefined => store.criteria.supplyMethodCode, async (newValue): Promise<void> => {
  if (newValue) {
    await store.getSupplyMethodSpecialTypeDDlAsync(newValue);
  }
});

watch((): WorklistType => optionSelected.value, async (): Promise<void> => {
  await onGetWorklistWithTypeAsync();
});

watch(
  (): {
    plans: number;
    planAnnouncements: number;
    preProcurement: number;
    procurement: number;
    contractAgreement: number;
    combined: number;
    contractManagement: number;
    contractAmendments: number;
    expenseDisbursement: number;
  } => store.worklistRes.counts,
  (newValue): void => {
    if (!newValue) return;

    const mapping: Record<number, keyof typeof newValue> = {
      1: "plans",
      2: "planAnnouncements",
      3: "preProcurement",
      4: "procurement",
    };

    Object.entries(mapping).forEach(([index, key]): void => {
      options.value[+index].count = newValue[key];
    });
  },
  { immediate: true }
);

watch(
  () => store.worklistRes.contractAgreement?.page?.totalRecords,
  (newValue): void => {
    if (newValue !== undefined) {
      options.value[5].count = newValue;
    }
  },
  { immediate: true }
);

watch(
  () => store.worklistRes.combined?.totalRecords,
  (newValue): void => {
    if (newValue !== undefined) {
      options.value[0].count = newValue;
    }
  },
  { immediate: true }
);

watch(
  () => [
    store.worklistRes.contractManagement?.page?.totalRecords,
    store.worklistRes.contractAmendments?.page?.totalRecords,
  ] as const,
  ([cm, ca]): void => {
    if (cm !== undefined || ca !== undefined) {
      options.value[6].count = (cm ?? 0) + (ca ?? 0);
    }
  },
  { immediate: true }
);

const onEnter = (el: Element): void => {
  const element = el as HTMLElement;
  element.style.height = '0';
  element.style.opacity = '0';
  void element.offsetHeight;
  element.style.transition = 'height 0.4s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.4s cubic-bezier(0.4, 0, 0.2, 1)';
  element.style.height = element.scrollHeight + 'px';
  element.style.opacity = '1';
};

const onAfterEnter = (el: Element): void => {
  const element = el as HTMLElement;
  element.style.height = 'auto';
};

const onLeave = (el: Element): void => {
  const element = el as HTMLElement;
  element.style.height = element.scrollHeight + 'px';
  element.style.opacity = '1';
  void element.offsetHeight;
  element.style.transition = 'height 0.4s cubic-bezier(0.4, 0, 0.2, 1), opacity 0.4s cubic-bezier(0.4, 0, 0.2, 1)';
  element.style.height = '0';
  element.style.opacity = '0';
};
</script>

<template>
  <TitleHeader label="Worklist" class="mb-4" />
  <Card class="mb-4">
    <template #content>
      <Form @submit="store.getListAsync">
        <div class="relative">
          <Transition name="button-slide" mode="out-in">
            <div v-show="true" key="top" class="flex justify-end p-2">
              <button type="button" @click="isExpanded = !isExpanded"
                class="text-primary hover:text-primary-600 transition-all duration-300 flex items-center gap-2 text-sm font-medium">
                <i :class="`text-primary pi ${isExpanded ? 'pi-chevron-up' : 'pi-chevron-down'}`"
                  style="font-size: 1.3rem" />
              </button>
            </div>
          </Transition>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-5 gap-2 mt-2 items-start">
          <InputField class="col-span-1 lg:col-span-3" label="คำค้นหา" v-model.trim="store.criteria.keyword" />
          <Transition name="button-slide" mode="out-in">
            <div v-if="!isExpanded" key="buttons-top" class="flex gap-2 justify-end items-start lg:col-start-5">
              <ButtonSearch type="submit" class="lg:w-fit w-full" />
              <ButtonClear @click="store.onClearCriteriaAsync" class="lg:w-fit w-full" />
            </div>
          </Transition>
        </div>

        <Transition name="expand" @enter="onEnter" @after-enter="onAfterEnter" @leave="onLeave">
          <div v-if="isExpanded" class="overflow-hidden">
            <div class="pt-4 space-y-2">
              <div class="grid grid-cols-1 lg:grid-cols-5 gap-2">
                <Select :options="store.departmentDDL" label="ฝ่าย/ภาคเขต" v-model="store.criteria.departmentCode" />
                <BudgetYearSelect v-model="store.criteria.budgetYear" not-set-default />
              </div>
              <div class="grid grid-cols-1 lg:grid-cols-5 gap-2">
                <Select :options="store.supplyMethodCodeDDL" label="วิธีการจัดหา"
                  v-model="store.criteria.supplyMethodCode" />
                <Select :options="store.supplyMethodTypeCodeDDL" label="ประเภทวิธีการจัดหา" v-model="store.criteria.supplyMethodTypeCode" />
                <Select :options="store.supplyMethodSpecialTypeCodeDDL"
                  v-model="store.criteria.supplyMethodSpecialTypeCode" />
              </div>
              <div class="flex items-center gap-2">
                <Checkbox v-model="store.criteria.isPendingDepartment" :binary="true" inputId="isPendingDepartment" />
                <label for="isPendingDepartment" class="cursor-pointer select-none">รอรับดำเนินการในฝ่าย</label>
              </div>
              <Transition name="button-slide" mode="out-in">
                <div v-if="isExpanded" key="buttons-bottom" class="flex justify-end gap-2">
                  <div class="flex gap-2 lg:justify-end items-start">
                    <ButtonSearch type="submit" class="lg:w-fit w-full" />
                    <ButtonClear @click="store.onClearCriteriaAsync" class="lg:w-fit w-full" />
                  </div>
                </div>
              </Transition>
            </div>
          </div>
        </Transition>
      </Form>
    </template>
  </Card>
  <Card>
    <template #content>
      <StatusGroupButton :optionBadges="options" v-model="optionSelected" />
      <All v-if="optionSelected === WorklistType.Combined" />
      <Plan v-if="optionSelected === WorklistType.Plan" />
      <PlanAnnouncement v-if="optionSelected === WorklistType.PlanAnnouncement" />
      <PreProcurement v-if="optionSelected === WorklistType.PreProcurement" />
      <Procurement v-if="optionSelected === WorklistType.Procurement" />
      <ContractAgreement v-if="optionSelected === WorklistType.ContractAgreement" />
      <ContractManagement v-if="optionSelected === WorklistType.ContractManagement" />
    </template>
  </Card>
</template>

<style scoped>
.expand-enter-active,
.expand-leave-active {
  overflow: hidden;
}

.expand-enter-from,
.expand-leave-to {
  opacity: 0;
  height: 0;
}

.expand-enter-to,
.expand-leave-from {
  opacity: 1;
}

/* Button slide animation */
.button-slide-enter-active,
.button-slide-leave-active {
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.button-slide-enter-from {
  opacity: 0;
  transform: translateY(-30px);
}

.button-slide-leave-to {
  opacity: 0;
  transform: translateY(30px);
}

.button-slide-enter-to,
.button-slide-leave-from {
  opacity: 1;
  transform: translateY(0);
}
</style>