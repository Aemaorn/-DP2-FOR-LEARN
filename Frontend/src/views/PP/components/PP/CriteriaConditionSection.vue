<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Card } from 'primevue';
import { Select } from '@/components/forms';
import type { Option } from '@/models/shared/option';
import { onMounted, ref } from 'vue';
import SharedService from '@/services/Shared/dropdown';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { useMenuStore } from '@/stores/menu';

type Props = {
  isDisabled?: boolean;
}

const props = defineProps<Props>();

const value = defineModel<string>({
  required: true,
});

const menuStore = useMenuStore();

const dropdownData = ref<Array<Option>>([]);

onMounted(async () => {
  await onGetCriteriaConditionDropdownAsync();
});

const onGetCriteriaConditionDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CriteriaCons);

  if (status === HttpStatusCode.Ok) {
    dropdownData.value = data;
  }
};
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="หลักเกณฑ์การคัดเลือก" />
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-2 mt-10">
        <Select label="หลักเกณฑ์การคัดเลือก" v-model="value" :options="dropdownData" rules="required"
          :disabled="props.isDisabled || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>