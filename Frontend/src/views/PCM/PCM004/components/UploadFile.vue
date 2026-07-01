<script setup lang="ts">
import { UploadFileGroup } from '@/components/forms';
import { usePcm004DetailStore } from '@/stores/PCM/pcm004';
import { useMenuStore } from '@/stores/menu';
import { ref, watch } from 'vue';

const pcm004Store = usePcm004DetailStore();
const menuStore = useMenuStore();
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
      <UploadFileGroup v-if="pcm004Store.detail.id" v-model="pcm004Store.detail.attachments"
        @upload="pcm004Store.onUpsertAttachments" @remove-file="pcm004Store.onUpsertAttachments"
        @remove-group="pcm004Store.onUpsertAttachments" @reorder="pcm004Store.onUpsertAttachments"
        :disabled="!menuStore.hasManage" />
    </template>
  </Card>
</template>
