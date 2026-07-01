<script setup lang="ts">
import { computed, type PropType } from 'vue';

const props = defineProps({
  label: { type: String, required: true },
  icon: { type: String },
  color: { type: String as PropType<string | undefined>, default: 'All' },
  size: { type: String as PropType<'Small' | 'Medium' | 'Large' | 'ExtraLarge'> },
});

const statusColor = computed(() => {
  switch (props.color) {
    case 'all':
      return { bgColor: 'bg-gray-400', textColor: 'text-black' };
    case 'annualPlan':
      return { bgColor: 'bg-[#e3f2fd]', textColor: 'text-[#2972ff]' };
    case 'inYearPlan':
      return { bgColor: 'bg-[#f9a825]', textColor: 'text-[#fffde7]' };
    default:
      return { bgColor: 'bg-gray-200!', textColor: 'text-gray-600!' };
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
    default:
      return 'text-base leading-4';
  }
});

const paddingSize = computed(() => {
  switch (props.size) {
    case 'ExtraLarge':
      return 'px-3';
    case 'Large':
      return 'px-3';
    case 'Medium':
      return 'px-2';
    case 'Small':
      return 'px-1';
    default:
      return 'px-1.5';
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
    default:
      return 'text-[1rem]!';
  }
});
</script>

<template>
  <Chip class="rounded-full shadow-sm" :class="`${statusColor.bgColor}`">
    <div class="flex items-center gap-2" :class="`${paddingSize}`">
      <i :class="`${props.icon} ${iconSize} ${statusColor.textColor}`" v-if="props.icon" />
      <p :class="`${statusColor.textColor} ${fontSize}`" class="subpixel-antialiased!">
        {{ props.label }}
      </p>
    </div>
  </Chip>
</template>
