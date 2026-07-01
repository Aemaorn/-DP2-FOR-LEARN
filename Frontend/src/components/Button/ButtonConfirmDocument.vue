<script setup lang="ts">

const props = defineProps<{
  text?: string;
}>();

</script>

<template>
  <Button :label="props.text ?? 'ยันยันใช้งานเอกสาร'" icon="pi pi-check" severity="warn" />
</template>