<script setup lang="ts">

const props = defineProps<{
  text?: string;
}>();

</script>

<template>
  <Button :label="props.text ?? 'บันทึก'" icon="pi pi-save" severity="success" />
</template>