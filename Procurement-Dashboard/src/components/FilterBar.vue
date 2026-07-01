<script setup lang="ts">
import { ref } from 'vue'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import InputText from 'primevue/inputtext'

export interface FilterCriteria {
  periodType: string
  dateRange: Date[] | null
  method: string | null
  status: string | null
  search: string
}

const periodType = ref('อนุมัติเมื่อ')
const dateRange = ref<Date[] | null>(null)
const method = ref<string | null>(null)
const status = ref<string | null>(null)
const search = ref('')

const periodTypes = ['อนุมัติเมื่อ', 'ออกใบสั่งซื้อเมื่อ', 'ลงนามสัญญาเมื่อ']
const methodOptions = ['ทั้งหมด', 'เฉพาะเจาะจง', 'คัดเลือก', 'e-bidding', 'e-market', 'พิเศษ']
const statusOptions = ['ทั้งหมด', 'ตามแผน', 'มีแนวโน้มล่าช้า', 'ช้ากว่าแผน']

const emit = defineEmits<{
  query: [filters: FilterCriteria]
  reset: []
}>()

function onQuery() {
  emit('query', {
    periodType: periodType.value,
    dateRange: dateRange.value,
    method: method.value === 'ทั้งหมด' ? null : method.value,
    status: status.value === 'ทั้งหมด' ? null : status.value,
    search: search.value,
  })
}

function onReset() {
  periodType.value = 'อนุมัติเมื่อ'
  dateRange.value = null
  method.value = null
  status.value = null
  search.value = ''
  emit('reset')
}
</script>

<template>
  <div class="bg-white border border-stone-200 rounded-xl p-4 mb-5">
    <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3 items-end">
      <div>
        <label class="text-[11px] text-stone-400 mb-1 block font-medium">ช่วงเวลาตาม</label>
        <Select v-model="periodType" :options="periodTypes" class="w-full" size="small" />
      </div>
      <div>
        <label class="text-[11px] text-stone-400 mb-1 block font-medium">ช่วงวันที่</label>
        <DatePicker v-model="dateRange" selectionMode="range" dateFormat="dd/mm/yy" class="w-full" size="small" placeholder="ทั้งหมด" />
      </div>
      <div>
        <label class="text-[11px] text-stone-400 mb-1 block font-medium">วิธีการซื้อจ้าง</label>
        <Select v-model="method" :options="methodOptions" placeholder="ทั้งหมด" class="w-full" size="small" />
      </div>
      <div>
        <label class="text-[11px] text-stone-400 mb-1 block font-medium">สถานะ</label>
        <Select v-model="status" :options="statusOptions" placeholder="ทั้งหมด" class="w-full" size="small" />
      </div>
      <div>
        <label class="text-[11px] text-stone-400 mb-1 block font-medium">ค้นหา</label>
        <InputText v-model="search" placeholder="ชื่อโครงการ..." class="w-full" size="small" />
      </div>
      <div class="flex gap-2">
        <button
          class="px-3 py-1.5 bg-amber-500 text-white text-xs font-medium rounded-md hover:bg-amber-600 transition-colors"
          @click="onQuery"
        >
          <i class="pi pi-search mr-1 text-[10px]"></i> ค้นหา
        </button>
        <button
          class="px-3 py-1.5 text-stone-500 text-xs font-medium rounded-md border border-stone-200 hover:bg-stone-50 transition-colors"
          @click="onReset"
        >
          ล้าง
        </button>
      </div>
    </div>
  </div>
</template>
