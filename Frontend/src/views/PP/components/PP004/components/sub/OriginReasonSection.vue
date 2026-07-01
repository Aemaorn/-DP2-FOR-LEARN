<script setup lang="ts">
import { Card } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';
import { usePP004Store } from '@/views/PP/stores/PP004/pp004Store';
// TODO: Waiting Spec and imprement this section. for render data

const pp004Store = usePP004Store();
</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="ที่มาและเหตุผล" />
      <div class="my-4 py-2 px-4 bg-gray-200 rounded-md">
        <p>{{ pp004Store.body.reason }}</p>
      </div>
    </template>
  </Card>
</template>