<script setup lang="ts">
import type { SequenceDescription } from '@/views/PP/models/PP002/pp002Model';
import { InputField } from '@/components/forms';
import { Card, DataTable } from 'primevue';
import { TitleHeader } from '@/components/cosmetic';

const value = defineModel<SequenceDescription[]>({
  default: () => [],
});

</script>

<template>
  <Card class="mb-4">
    <template #content>
      <TitleHeader label="วัตถุประสงค์" />
      <div v-if="value && value.length > 0">
        <div class="mt-4">
          <DataTable :value="value">
            <Column bodyStyle="vertical-align: top" class="max-w-[10px]">
              <template #header>
                <p class="w-full font-bold text-center">ลำดับ</p>
              </template>
              <template #body="{ data }">
                <div>
                  <p class="text-center">{{ data.sequence }}</p>
                </div>
              </template>
            </Column>
            <Column bodyStyle="vertical-align: top">
              <template #header>
                <p class="w-full font-bold text-center">รายละเอียด</p>
              </template>
              <template #body="{ data }">
                <InputField v-model="data.description" hide-details rules="required" disabled />
              </template>
            </Column>
          </DataTable>
        </div>
      </div>
    </template>
  </Card>
</template>
