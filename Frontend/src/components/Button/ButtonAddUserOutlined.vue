<script setup lang="ts">
type Props = {
  type?: 'submit' | 'reset' | 'button';
}

const props = withDefaults(defineProps<Props>(), {
  type: 'button',
});
</script>

<template>
  <Button label="เพิ่มรายชื่อ" icon="pi pi-plus" severity="primary" variant="outlined"
    class="bg-white! hover:bg-red-50!" :type="props.type" />
</template>