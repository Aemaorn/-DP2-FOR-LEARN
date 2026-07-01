<script setup lang="ts">
import { ref } from 'vue'
import { watchDebounced } from '@vueuse/core'
import { Card } from 'primevue'
import { InputField, Select, Datepicker } from '@/components/forms'
import { ButtonClear } from '@/components/Button'
import type { FilterCriteria } from '@/models/DA/da003'
import type { Option } from '@/models/shared/option'

const periodType = ref<string | undefined>('อนุมัติเมื่อ')
const dateFrom = ref<Date | undefined>(undefined)
const dateTo = ref<Date | undefined>(undefined)
const method = ref<string | undefined>(undefined)
const status = ref<string | undefined>(undefined)
const search = ref<string | undefined>(undefined)

const expanded = ref(false)

const periodTypeOptions: Option[] = [
  { label: 'อนุมัติเมื่อ', value: 'อนุมัติเมื่อ' },
  { label: 'ออกใบสั่งซื้อเมื่อ', value: 'ออกใบสั่งซื้อเมื่อ' },
  { label: 'แจ้งเตรียมเอกสารเมื่อ', value: 'แจ้งเตรียมเอกสารเมื่อ' },
  { label: 'ลงนามสัญญาเมื่อ', value: 'ลงนามสัญญาเมื่อ' },
]

const methodOptions: Option[] = [
  { label: 'ทั้งหมด', value: '' },
  { label: 'เฉพาะเจาะจง', value: 'เฉพาะเจาะจง' },
  { label: 'คัดเลือก', value: 'คัดเลือก' },
  { label: 'e-bidding', value: 'e-bidding' },
  { label: 'e-market', value: 'e-market' },
  { label: 'พิเศษ', value: 'พิเศษ' },
]

const statusOptions: Option[] = [
  { label: 'ทั้งหมด', value: '' },
  { label: 'เป็นไปตามแผน', value: 'เป็นไปตามแผน' },
  { label: 'มีแนวโน้มล่าช้า', value: 'มีแนวโน้มล่าช้า' },
  { label: 'ช้ากว่าแผน', value: 'ช้ากว่าแผน' },
]

const emit = defineEmits<{
  query: [filters: FilterCriteria]
  reset: []
}>()

function onQuery() {
  emit('query', {
    periodType: periodType.value ?? 'อนุมัติเมื่อ',
    dateFrom: dateFrom.value ?? null,
    dateTo: dateTo.value ?? null,
    method: method.value || null,
    status: status.value || null,
    search: search.value ?? '',
  })
}

watchDebounced(
  () => [periodType.value, dateFrom.value, dateTo.value, method.value, status.value, search.value],
  () => onQuery(),
  { debounce: 500, deep: true },
)

function onReset() {
  periodType.value = 'อนุมัติเมื่อ'
  dateFrom.value = undefined
  dateTo.value = undefined
  method.value = undefined
  status.value = undefined
  search.value = undefined
  emit('reset')
}
</script>

<template>
  <Card class="mb-4 relative" :pt="{ root: { style: 'box-shadow: none; border: none;' } }">
    <template #content>
      <!-- ปุ่ม expand/collapse มุมขวาบน -->
      <button type="button" @click="expanded = !expanded"
        class="absolute top-3 right-4 text-primary hover:text-primary-600 transition-all duration-300 flex items-center gap-2 text-sm font-medium">
        <i :class="`text-primary pi ${expanded ? 'pi-chevron-up' : 'pi-chevron-down'}`" style="font-size: 1.3rem" />
      </button>

      <div class="mt-2 space-y-7">
          <!-- แถวแรก: คำค้นหา + ปุ่ม (แสดงเสมอ) -->
          <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-2 items-end">
            <InputField label="คำค้นหา" class="lg:col-span-3" v-model="search" hide-details />
            <div v-if="!expanded" class="lg:col-span-2 flex items-end justify-end gap-2">
              <ButtonClear class="lg:w-fit w-full" @click="onReset" />
            </div>
          </div>

          <!-- แถวที่เหลือ: แสดงเมื่อ expanded -->
          <template v-if="expanded">
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-4">
              <Select label="ช่วงเวลาตาม" v-model="periodType" :options="periodTypeOptions" hide-details />
              <Datepicker label="จากวันที่" v-model="dateFrom" hide-details />
              <Datepicker label="ถึงวันที่" v-model="dateTo" :minDate="dateFrom" hide-details />
            </div>
            <div class="grid grid-cols-1 lg:grid-cols-5 gap-8 lg:gap-4">
              <Select label="วิธีการซื้อจ้าง" v-model="method" :options="methodOptions" hide-details />
              <Select label="สถานะ" v-model="status" :options="statusOptions" hide-details />
              <div class="lg:col-span-3 flex items-end justify-end gap-2">
                <ButtonClear class="lg:w-fit w-full" @click="onReset" />
              </div>
            </div>
          </template>
        </div>
    </template>
  </Card>
</template>
