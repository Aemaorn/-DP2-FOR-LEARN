<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { Select } from '@/components/forms';
import { onMounted, ref } from 'vue';
import { EGroupCode } from '@/enums/shared';
import { HttpStatusCode } from 'axios';
import { usePP002DetailStore } from '@/views/PP/stores/PP002/pp002Store';
import { useMenuStore } from '@/stores/menu';
import defaultProps from '@/helpers/defaultProps';
import TitleHeader from '@/components/cosmetic/TitleHeader.vue';
import SharedService from '@/services/Shared/dropdown';

const props = defineProps({
  titleName: defaultProps(''),
});

const value = defineModel<string | undefined>({
  default: '',
});

const menuStore = useMenuStore();
const store = usePP002DetailStore();

const dropdown = ref<Option[]>([]);

onMounted(() => {
  getDropdownAsync();
});

const getDropdownAsync = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.CriteriaCons, undefined, true);

  if (status === HttpStatusCode.Ok) {
    dropdown.value = data;
  }
};
</script>

<template>
  <Card class="mb-4" data-section-id="selection-criteria" :data-section-label="props.titleName">
    <template #content>
      <TitleHeader :label="props.titleName" />
      <div class="md:grid grid-cols-6 gap-2 mt-9">
        <Select class="col-span-2" label="หลักเกณฑ์การคัดเลือก" v-model="value" :options="dropdown" rules="required"
          :disabled="!store.status.canEditTor || !menuStore.hasManage" />
      </div>
    </template>
  </Card>
</template>
