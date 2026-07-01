<script setup lang="ts">
import { TitleHeader } from '@/components/cosmetic';
import { Card } from 'primevue';
import { Radio } from '@/components/forms';
import SharedConstants from '@/constants/shared';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
import { useMenuStore } from '@/stores/menu';

const pp004Store = usePP004Store();
const menuStore = useMenuStore();

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="หลักประกันสัญญา" />
      <Radio class="px-4" v-model="pp004Store.body.requisition.hasContractGuarantee"
        :options="SharedConstants.HasOptions" :disabled="!pp004Store.IsEdit || !menuStore.hasManage" />
    </template>
  </Card>
</template>