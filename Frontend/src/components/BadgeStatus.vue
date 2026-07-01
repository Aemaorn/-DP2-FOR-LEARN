<script setup lang="ts">
import { computed } from 'vue';
import type { Color } from '@/models/shared/color';

type Props = {
  label: string;
  prefix?: string;
  bgColorClass?: string;
  textColorClass?: string;
  color?: Color | string;
  size?: 'xs' | 'sm' | 'md' | 'lg';
};

const textColorMap: Record<Color, string> = {
  red: 'text-red-600',
  orange: 'text-orange-600',
  amber: 'text-amber-600',
  yellow: 'text-yellow-600',
  lime: 'text-lime-600',
  green: 'text-green-600',
  emerald: 'text-emerald-600',
  teal: 'text-teal-600',
  cyan: 'text-cyan-600',
  sky: 'text-sky-600',
  blue: 'text-blue-600',
  indigo: 'text-indigo-600',
  violet: 'text-violet-600',
  purple: 'text-purple-600',
  fuchsia: 'text-fuchsia-600',
  pink: 'text-pink-600',
  rose: 'text-rose-600',
  slate: 'text-slate-600',
  gray: 'text-gray-600',
  zinc: 'text-zinc-600',
  neutral: 'text-neutral-600',
  stone: 'text-stone-600',
};

const props = withDefaults(defineProps<Props>(), { size: 'md' });

// คำนวณสีตัวอักษร (ink) สำหรับสไตล์ "จุด + ข้อความ ไม่มีพื้นหลัง"
// คืนเป็น class (กรณีสีชื่อ — Tailwind มี literal ใน textColorMap อยู่แล้ว)
// หรือ inline style (กรณี hex — Tailwind ไม่ generate text-[#hex] ให้ ต้องใช้ style)
const ink = computed<{ class?: string; color?: string }>(() => {
  // 1. ใช้ textColorClass ที่ส่งมา เว้นแต่เป็นสีขาว (เดิมคู่กับพื้นทึบ จะมองไม่เห็นบนพื้นขาว)
  if (props.textColorClass && props.textColorClass !== 'text-white' && props.textColorClass !== 'text-white!') {
    return { class: props.textColorClass };
  }
  // 2. derive สีจาก bgColorClass (เคส text-white)
  if (props.bgColorClass) {
    const named = props.bgColorClass.match(/^bg-([a-z]+)-\d{2,3}!?$/);
    if (named) {
      return { class: `text-${named[1]}-600` };
    }
    const hex = props.bgColorClass.match(/^bg-\[(#[0-9a-fA-F]+)\]!?$/);
    if (hex) {
      return { color: hex[1] };
    }
  }
  // 3. ใช้ชื่อสีจาก color prop
  if (props.color) {
    if (props.color in textColorMap) {
      return { class: textColorMap[props.color as Color] };
    }
    return { class: props.color };
  }
  // 4. fallback
  return { class: 'text-gray-600' };
});

const inkClass = computed(() => ink.value.class ?? '');
const inkStyle = computed(() => (ink.value.color ? { color: ink.value.color } : {}));

const fontClass = computed(() => {
  switch (props.size) {
    case 'xs':
      return 'text-xs';
    case 'sm':
      return 'text-base';
    case 'md':
      return 'text-lg';
    case 'lg':
    default:
      return 'text-xl';
  }
});
</script>

<template>
  <p :class="['flex items-center justify-start gap-1.5 whitespace-nowrap w-fit', inkClass, fontClass]" :style="inkStyle">
    <span v-if="props.prefix" class="text-sm text-gray-900">{{ props.prefix }}</span>
    <span class="w-2 h-2 rounded-full bg-current inline-block shrink-0" />
    {{ props.label }}
  </p>
</template>