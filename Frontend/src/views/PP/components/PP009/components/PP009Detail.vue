<script setup lang="ts">
import { computed } from 'vue';
import Information from './sub/Information.vue';
import Entrepreneur from './sub/Entrepreneur.vue';
import type { VendorInfo } from '@/views/PP/models/PP009/pp009Model';

type Props = {
  isValidate?: boolean;
  isDisabled?: boolean;
  vendorIndex?: number;
  isReadonly?: boolean;
};

const props = defineProps<Props>();

const value = defineModel<VendorInfo>({
  required: true,
});

const isDisabledOrReadonly = computed(() => props.isDisabled || props.isReadonly);
</script>

<template>
  <Information v-model="value" :isValidate="props.isValidate" :isDisabled="isDisabledOrReadonly" />
  <Entrepreneur v-model="value" :isDisabled="isDisabledOrReadonly" :vendor-index="props.vendorIndex" />
</template>