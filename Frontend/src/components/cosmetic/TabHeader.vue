<script setup lang="ts">
import type { Option } from '@/models/shared/option';
import { TabList, Tab } from 'primevue';
import { ref } from 'vue';

type Props = {
  items: Option[];
  isUnderline?: boolean;
}

const props = defineProps<Props>();
const currentActive = ref(0);

const textCase = (currentActive: number, index: number) => {
  if (props.isUnderline) {
    return 'text-black';
  }

  return currentActive === index ? 'text-primary-600' : 'text-primary-500';
};

const bgCase = (currentActive: number, index: number) => {
  if (props.isUnderline && currentActive === index) {
    return 'border-b-4 border-primary-600 duration-100';
  }

  return currentActive === index ? 'bg-secondary duration-500' : 'bg-transparent';
}
</script>

<template>
  <TabList class="my-4 z-[1004]" :class="`${props.isUnderline ? 'bg-transparent' : 'border-b-4 border-primary'}`">
    <Tab v-for="(data, index) in props.items" :key="index" :value="(data.value as string | number)" :pt="{
      root(options) {
        if (options.context.active) {
          currentActive = index;
        }
      },
    }">
      <div class="mr-4 p-2 px-6 cursor-pointer rounded-t-sm" :class="`${bgCase(currentActive, index)}`">
        <p :class="`${textCase(currentActive, index)}`">
          {{ data.label }}</p>
      </div>
    </Tab>
  </TabList>
</template>
