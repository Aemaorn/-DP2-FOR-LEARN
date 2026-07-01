<script setup lang="ts">
import { useToast, Toast } from 'primevue';
import { ToastConstants } from '@/constants';
import { onMounted, ref } from 'vue';
import type { IToastOptions } from '@/models/shared/toast';
import { useEventListener } from '@vueuse/core'

const toastIns = useToast();
const progress = ref(100);

onMounted(() => {
  useEventListener(document, 'toast', (event: any) => {
    progress.value = 100;

    toastIns.add({
      summary: event.detail.title,
      detail: {
        message: event.detail.message,
        ...event.detail.toastOption as IToastOptions,
      },
      life: ToastConstants.ToastLifeTime,
      styleClass: `shadow-lg! ${event.detail.toastOption.bgColor} border-none!`,
    });

    setTimeout(() => {
      progress.value = 0;
    }, 0);
  });
});
</script>

<template>
  <Toast position="top-right">
    <template #container="{ message }">
      <div class="rounded-xl!">
        <div class="flex flex-col gap-4 w-full p-4">
          <div class="flex items-center gap-3">
            <div class=" flex items-center rounded-2xl!" :class="`${message.detail.iconColor}`">
              <span class="material-symbols-outlined text-white p-2!" style="font-size: 1.15rem;">
                {{ message.detail.icon }}
              </span>
            </div>
            <div class="flex flex-col w-full!">
              <p class="text-black!">{{ message.summary }}</p>
              <span class="text-gray-500!">{{ message.detail.message }}</span>
            </div>
          </div>
        </div>
        <div class="h-[4px]! rounded-bl-xl!"
          :class="`duration-[${ToastConstants.ToastLifeTime}ms]! ${message.detail.progressbarColor}`"
          :style="{ width: `${progress}%` }">
        </div>
      </div>
    </template>
  </Toast>
</template>
