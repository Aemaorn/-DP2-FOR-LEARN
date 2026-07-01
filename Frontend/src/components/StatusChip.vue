<script setup lang="ts">
import { computed, type PropType } from 'vue';


const props = defineProps({
  label: { type: String, required: true, },
  icon: { type: String, },
  color: { type: String as PropType<"Draft" | "Success" | "Warning" | "Info" | "Error">, default: "Draft", },
  size: { type: String as PropType<"Small" | "Medium" | "Large" | "ExtraLarge"> },
});

const statusColor = computed(() => {
  switch (props.color) {
    case 'Draft':
      return { textColor: 'text-gray-600!' };
    case 'Success':
      return { textColor: 'text-green-600!' };
    case 'Warning':
      return { textColor: 'text-yellow-600!' };
    case 'Info':
      return { textColor: 'text-blue-600!' };
    case 'Error':
      return { textColor: 'text-red-600!' };
    default:
      return { textColor: 'text-gray-600!' };
  }
});

const fontSize = computed(() => {
  switch (props.size) {
    case 'ExtraLarge':
      return 'text-2xl leading-6';
    case 'Large':
      return 'text-xl leading-5.5';
    case 'Medium':
      return 'text-lg leading-5';
    case 'Small':
      return 'text-sm leading-3';
    default: return 'text-base leading-4';
  }
});

const iconSize = computed(() => {
  switch (props.size) {
    case 'ExtraLarge':
      return 'text-xl';
    case 'Large':
      return 'text-md!';
    case 'Medium':
      return 'text-lg';
    case 'Small':
      return 'text-xs!';
    default: return 'text-[1rem]!';
  }
});
</script>

<template>
  <div class="flex items-center gap-1.5 whitespace-nowrap w-fit" :class="`${statusColor.textColor}`">
    <i :class="`${props.icon} ${iconSize}`" v-if="props.icon" />
    <span v-else class="w-2 h-2 rounded-full bg-current inline-block shrink-0" />
    <p :class="`${fontSize}`" class="subpixel-antialiased!">{{ props.label }}</p>
  </div>
</template>
