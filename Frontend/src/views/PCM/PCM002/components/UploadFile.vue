<script setup lang="ts">
import { UploadFileGroup } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';
import { usePcm002DetailStore } from '@/stores/PCM/pcm002';
import { ref, watch } from 'vue';

const scrollBarY = ref(0);
const pcm002Store = usePcm002DetailStore();
const menuStore = useMenuStore();

watch(
  () => scrollBarY.value,
  (val: number) => {
    if (val != 0) {
      window.scrollTo({ top: document.documentElement.scrollHeight, behavior: 'smooth' });

      return;
    }

    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
);
</script>

<template>
  <div class="mt-4">
    <UploadFileGroup v-if="pcm002Store.detail.id" v-model="pcm002Store.detail.attachments"
      @upload="pcm002Store.onUpsertAttachments" @remove-file="pcm002Store.onUpsertAttachments"
      @remove-group="pcm002Store.onUpsertAttachments" @reorder="pcm002Store.onUpsertAttachments"
      :disabled="!menuStore.hasManage" />
  </div>
</template>
