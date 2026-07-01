<script setup lang="ts">
import type { MenuItem } from 'primevue/menuitem';

type Props = {
  label: string;
  routeItems?: Array<MenuItem>;
  hiddenIcon?: boolean;
  hiddenLine?: boolean;
}

const props = defineProps<Props>();
</script>

<template>
  <div class="w-full my-4 z-50">
    <div class="flex gap-2 md:gap-4 items-center">
      <div class="h-4 md:h-7 w-3 md:w-6 bg-primary transform -skew-x-12" v-if="!props.hiddenIcon" />
      <p class="whitespace-nowrap text-xl! md:text-2xl! font-bold">{{ props.label }}</p>
      <div class="h-px bg-gray-300 flex-1" v-if="!props.hiddenLine" />
      <slot name="action" />
    </div>
    <div class="flex gap-2 items-center justify-between" v-if="props.routeItems && props.routeItems.length > 0">
      <Breadcrumb :model="props.routeItems" class="p-0! pt-4! m-0! bg-transparent!">
        <template #item="{ item }">
          <router-link :to="{ path: item.url }" class="text-gray-500!"
            :class="`${item.url ? '' : 'hover:cursor-default!'}`">
            <p> {{ item.label }}</p>
          </router-link>
        </template>
        <template #separator>
          <p>/</p>
        </template>
      </Breadcrumb>
      <slot name="breadcrumbAction" />
    </div>
  </div>
</template>
