<script setup lang="ts">
import Tag from 'primevue/tag'
import { ProcurementStatus } from '@/types/procurement'
import { getStatusLabel, getStatusSeverity } from '@/composables/useStatusCalculation'

const props = defineProps<{
  status: ProcurementStatus
}>()
</script>

<template>
  <Tag :value="getStatusLabel(props.status)" :severity="getStatusSeverity(props.status)" />
</template>
