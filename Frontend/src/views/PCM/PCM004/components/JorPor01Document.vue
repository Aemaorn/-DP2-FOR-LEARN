<script setup lang="ts">
import { ref } from 'vue';
import ChEditor from '@/components/Document/ChEditor.vue';

const docRef = ref<InstanceType<typeof ChEditor> | null>(null);
const docId = ref('66540ca7-f03e-43bf-a2be-a2184eee0383');

const setPlaceholderInDocument = (text: string, hint?: string) => {
  if (docRef.value) {
    // Assuming the method setPlaceholderInDocument is defined in ChEditor
    // and it takes a document ID as an argument.
    docRef.value.setPlaceholderInDocument(text, hint);
  }
};

// Exposing methods for parent components
defineExpose({
  setPlaceholderInDocument,
});
</script>

<template>
  <ChEditor :docId="docId" docName="x" :readonly="false" ref="docRef" :key="docId" />
</template>