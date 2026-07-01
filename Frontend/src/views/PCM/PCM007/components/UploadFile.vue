<script setup lang="ts">
import { UploadFileGroup } from '@/components/forms';
import { usePcm007DetailStore } from '@/stores/PCM/pcm007';
import { ref, watch } from 'vue';

const pcm007Store = usePcm007DetailStore();
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
  <Card class="mt-4">
    <template #content>
      <UploadFileGroup v-model="pcm007Store.detail.attachments" @upload="pcm007Store.onUpsertAttachments"
        @remove-file="pcm007Store.onUpsertAttachments" @remove-group="pcm007Store.onUpsertAttachments"
        @reorder="pcm007Store.onUpsertAttachments" :disabled="!pcm007Store.isEdit" />
    </template>
  </Card>
</template>
