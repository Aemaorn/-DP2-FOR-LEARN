<script setup lang="ts">
import InvitationInfo from './sub/InvitationInfo.vue';
import Entrepreneur from './sub/Entrepreneur.vue';
import { usePP006DetailStore } from '@/views/PP/stores/PP006/PP006Store';

defineProps({
  readonly: { type: Boolean, default: false },
});

const store = usePP006DetailStore();
</script>

<template>
  <InvitationInfo :readonly="readonly" />
  <Entrepreneur v-if="store.detail.isInvite" :readonly="readonly" />
</template>
