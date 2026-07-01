<script setup lang="ts">
import { ref } from 'vue';
import { useMenuStore } from '@/stores/menu';
import ChEditor from '@/components/Document/ChEditor.vue';
import { useCam02DetailStore } from '@/stores/CAM/CAM02/cam02Store';
import type { DocumentVersion } from '@/models/shared/document';

type Props = {
  readonly?: boolean;
  versions?: DocumentVersion[];
}

const props = withDefaults(defineProps<Props>(), {
  readonly: false,
});

const menuStore = useMenuStore();
const store = useCam02DetailStore();

const docRef = ref<InstanceType<typeof ChEditor> | null>(null);

const docId = defineModel<string | undefined>({
  required: true,
});

const saveDocumentFirst = (): Promise<void> => {
  return new Promise((resolve) => {
    if (docRef.value?.saveAndWait && !props.readonly) {
      docRef.value.saveAndWait(() => {
        resolve();
      });
    } else {
      resolve();
    }
  });
};

defineExpose({
  saveDocumentFirst,
});
</script>

<template>
  <ChEditor v-if="docId" :docId="docId"
    :docName="`committee-change-doc-${new Date().toDateString()}`"
    :readonly="!menuStore.hasManage || props.readonly"
    ref="docRef"
    :versions="props.versions"
    :key="`${docId}-${store.procurementDetail.status}`" />
</template>
