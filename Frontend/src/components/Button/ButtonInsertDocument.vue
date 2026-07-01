<script setup lang="ts">

const props = defineProps<{
  text?: string;
}>();

</script>

<template>
  <Button :label="props.text ?? 'แทรกข้อมูลลงในเอกสาร'" icon="pi pi-pencil" severity="warn" />
</template>