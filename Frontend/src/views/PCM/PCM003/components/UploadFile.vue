<script setup lang="ts">
import { UploadFileGroup } from '@/components/forms';
import { useMenuStore } from '@/stores/menu';
import { usePcm003DetailStore } from '@/stores/PCM/pcm003';
import { ref, watch } from 'vue';

const menuStore = useMenuStore();
const pcm003Store = usePcm003DetailStore();
const scrollBarY = ref(0);

watch(() => scrollBarY.value, (val: number) => {
  if (val != 0) {
    window.scrollTo({ top: document.documentElement.scrollHeight, behavior: 'smooth' });

    return;
  }

  window.scrollTo({ top: 0, behavior: 'smooth' })
})
</script>

<template>
  <div class="my-4">
    <UploadFileGroup v-if="pcm003Store.detail.id" v-model="pcm003Store.detail.attachments"
      @upload="pcm003Store.onUpsertAttachments" @remove-file="pcm003Store.onUpsertAttachments"
      @remove-group="pcm003Store.onUpsertAttachments" @reorder="pcm003Store.onUpsertAttachments"
      :disabled="!menuStore.hasManage" />
  </div>
</template>
