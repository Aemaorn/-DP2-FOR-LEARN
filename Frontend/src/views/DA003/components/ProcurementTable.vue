<script setup lang="ts">
import { ref, computed } from 'vue'
import { InputText, IconField, InputIcon, Button, Dialog } from 'primevue'
import Datepicker from '@/components/forms/Datepicker.vue'
import { ProcurementStatus, type ProcurementItem } from '@/models/DA/da003'
import ToastHelper from '@/helpers/toast'
import Pagination from '@/components/Pagination.vue'

const props = defineProps<{
  items: ProcurementItem[]
  showBudget: boolean
  readonly?: boolean
}>()

const emit = defineEmits<{
  update: [item: ProcurementItem]
  remove: [planId: string]
}>()

const search = ref('')
const pageNumber = ref(1)
const pageSize = ref(10)

const filteredItems = computed(() => {
  if (!search.value) return props.items
  const q = search.value.toLowerCase()
  return props.items.filter(i =>
    i.projectName.toLowerCase().includes(q) ||
    i.planNumber.toLowerCase().includes(q) ||
    i.departmentName.toLowerCase().includes(q)
  )
})

const pagedItems = computed(() => {
  const start = (pageNumber.value - 1) * pageSize.value
  return filteredItems.value.slice(start, start + pageSize.value)
})

function onChangePage(newPage: number, newSize: number) {
  pageNumber.value = newPage
  pageSize.value = newSize
}

// Delete confirm dialog
const showDeleteConfirm = ref(false)
const deleteTargetId = ref<string | null>(null)

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function confirmRemove(planId: string) {
  deleteTargetId.value = planId
  showDeleteConfirm.value = true
}

function doRemove() {
  if (deleteTargetId.value !== null) {
    emit('remove', deleteTargetId.value)
    ToastHelper.deletedMessageToast()
  }
  showDeleteConfirm.value = false
  deleteTargetId.value = null
}

// Inline editing
const editingId = ref<string | null>(null)
const editingDates = ref({
  approvalDate: undefined as Date | undefined,
  poDate: undefined as Date | undefined,
  docPrepNoticeDate: undefined as Date | undefined,
  contractSignDate: undefined as Date | undefined,
})

function toISODate(d: Date | null | undefined): string | null {
  if (!d) return null
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function fromISODate(s: string | null): Date | undefined {
  if (!s) return undefined
  return new Date(s + 'T00:00:00')
}

function startEdit(item: ProcurementItem) {
  editingId.value = item.planId
  editingDates.value = {
    approvalDate: fromISODate(item.approvalDate),
    poDate: fromISODate(item.poDate),
    docPrepNoticeDate: fromISODate(item.docPrepNoticeDate),
    contractSignDate: fromISODate(item.contractSignDate),
  }
}

function cancelEdit() {
  editingId.value = null
}

function saveInlineEdit(item: ProcurementItem) {
  const approvalDate = toISODate(editingDates.value.approvalDate)
  if (!approvalDate) {
    ToastHelper.warning('กรุณาระบุวันที่อนุมัติ', 'โปรดระบุวันที่อนุมัติก่อนบันทึก')
    return
  }
  const contractSignDate = toISODate(editingDates.value.contractSignDate)
  const updated: ProcurementItem = {
    ...item,
    approvalDate,
    poDate: toISODate(editingDates.value.poDate),
    docPrepNoticeDate: toISODate(editingDates.value.docPrepNoticeDate),
    contractSignDate,
  }
  emit('update', updated)
  editingId.value = null
  ToastHelper.updatedMessageToast()
}

function formatDate(date: string | null): string {
  if (!date) return '-'
  return new Date(date).toLocaleDateString('th-TH', { year: 'numeric', month: 'short', day: 'numeric' })
}

function countWorkingDays(from: Date, to: Date, includeStart: boolean): number {
  const [a, b] = from <= to ? [from, to] : [to, from]
  let count = 0
  const cur = new Date(a)
  if (!includeStart) cur.setDate(cur.getDate() + 1)
  while (cur <= b) {
    const day = cur.getDay()
    if (day !== 0 && day !== 6) count++
    cur.setDate(cur.getDate() + 1)
  }
  return count
}

function calcDays(from: string | null, to: string | null): { text: string; negative: boolean } | null {
  if (!from || !to) return null
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  const sign = end >= start ? 1 : -1
  const count = countWorkingDays(start, end, false)
  if (count === 0) return null
  return { text: `(${sign * count} วันทำการ)`, negative: sign < 0 }
}

function calcDaysInclusive(from: string | null, to: string | null): string | null {
  if (!from || !to) return null
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  const count = countWorkingDays(start, end, true)
  if (count === 0) return null
  return `${count} วันทำการ`
}

function calcTotalDays(from: string | null, to: string | null): number | null {
  if (!from || !to) return null
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  const count = countWorkingDays(start, end, true)
  return count === 0 ? null : count
}

const localStatusStyle: Record<ProcurementStatus, { dot: string; label: string }> = {
  [ProcurementStatus.OnPlan]: { dot: 'bg-emerald-500', label: 'เป็นไปตามแผน' },
  [ProcurementStatus.Risk]:   { dot: 'bg-amber-500',   label: 'มีแนวโน้มล่าช้า'  },
  [ProcurementStatus.Delay]:  { dot: 'bg-rose-500',    label: 'ช้ากว่าแผน'  },
}

function getLocalStatus(approvalDate: string | null, contractSignDate: string | null): ProcurementStatus | null {
  const days = calcTotalDays(approvalDate, contractSignDate)
  if (days === null) return null
  if (days <= 10) return ProcurementStatus.OnPlan
  if (days <= 15) return ProcurementStatus.Risk
  return ProcurementStatus.Delay
}
</script>

<template>
  <div class="pb-4">
    <!-- Search -->
    <div v-if="!readonly" class="flex items-center justify-end mb-3">
      <IconField>
        <InputIcon class="pi pi-search" />
        <InputText v-model="search" placeholder="ค้นหา..." size="small" />
      </IconField>
    </div>

    <!-- Table Header + Body (shared scroll) -->
    <div class="shadow-sm overflow-x-auto">
      <!-- Header -->
      <div class="grid bg-gray-50 divide-x divide-gray-200 min-w-[105rem] border-b border-gray-200"
        :class="readonly ? 'grid-cols-24' : 'grid-cols-25'">
        <div class="header col-span-1">ลำดับ</div>
        <div class="header col-span-1">ปีงบประมาณ</div>
        <div class="header col-span-2">หน่วยงาน</div>
        <div class="header col-span-2">เลขที่แผน</div>
        <div class="header col-span-3">ชื่อโครงการ/แผนงาน</div>
        <div class="header col-span-1">วิธีการซื้อจ้าง</div>
        <div class="header col-span-3">วันที่อนุมัติ</div>
        <div class="header col-span-3">วันที่ออกใบสั่งซื้อ</div>
        <div class="header col-span-3">วันที่แจ้งเตรียมเอกสาร</div>
        <div class="header col-span-3">วันที่ลงนามสัญญา</div>
        <div class="header col-span-1">รวมระยะเวลาดำเนินการ</div>
        <div class="header col-span-1">สถานะ</div>
        <div v-if="!readonly" class="header col-span-1">จัดการ</div>
      </div>

      <!-- Body -->
      <div v-if="pagedItems.length > 0" class="divide-y divide-gray-200 min-w-[105rem]">
        <div
          v-for="(data, index) in pagedItems"
          :key="data.planId"
          class="grid items-center divide-x divide-gray-200 transition-colors"
          :class="[readonly ? 'grid-cols-24' : 'grid-cols-25', editingId === data.planId ? 'bg-blue-50/60' : 'hover:bg-blue-50/40']"
        >
          <p class="cell text-center col-span-1">{{ (pageNumber - 1) * pageSize + index + 1 }}</p>
          <p class="cell text-center col-span-1">{{ data.budgetYear }}</p>
          <p class="cell col-span-2">{{ data.departmentName }}</p>
          <p class="cell text-center col-span-2">{{ data.planNumber }}</p>
          <p class="cell col-span-3">{{ data.projectName }}</p>
          <p class="cell text-center col-span-1">{{ data.supplyMethodSpecialType ?? data.method }}</p>

          <!-- วันที่อนุมัติ -->
          <div class="cell text-center col-span-3">
            <Datepicker v-if="editingId === data.planId" v-model="editingDates.approvalDate" hide-details />
            <span v-else>{{ formatDate(data.approvalDate) }}</span>
          </div>

          <!-- วันที่ออกใบสั่งซื้อ -->
          <div class="cell text-center col-span-3">
            <Datepicker v-if="editingId === data.planId" v-model="editingDates.poDate" hide-details />
            <template v-else>
              <span>{{ formatDate(data.poDate) }}</span>
              <p v-if="calcDays(data.approvalDate, data.poDate)" class="text-xs mt-0.5"
                :class="calcDays(data.approvalDate, data.poDate)!.negative ? 'text-red-500' : 'text-blue-400'">
                {{ calcDays(data.approvalDate, data.poDate)!.text }}
              </p>
            </template>
          </div>

          <!-- วันที่แจ้งเตรียมเอกสาร -->
          <div class="cell text-center col-span-3">
            <Datepicker v-if="editingId === data.planId" v-model="editingDates.docPrepNoticeDate" hide-details />
            <template v-else>
              <span>{{ formatDate(data.docPrepNoticeDate) }}</span>
              <p v-if="calcDays(data.poDate, data.docPrepNoticeDate)" class="text-xs mt-0.5"
                :class="calcDays(data.poDate, data.docPrepNoticeDate)!.negative ? 'text-red-500' : 'text-blue-400'">
                {{ calcDays(data.poDate, data.docPrepNoticeDate)!.text }}
              </p>
            </template>
          </div>

          <!-- วันที่ลงนามสัญญา -->
          <div class="cell text-center col-span-3">
            <Datepicker v-if="editingId === data.planId" v-model="editingDates.contractSignDate" hide-details />
            <template v-else>
              <span>{{ formatDate(data.contractSignDate) }}</span>
              <p v-if="calcDaysInclusive(data.docPrepNoticeDate, data.contractSignDate)" class="text-xs mt-0.5 text-blue-400">
                ({{ calcDaysInclusive(data.docPrepNoticeDate, data.contractSignDate) }})
              </p>
            </template>
          </div>

          <!-- รวมระยะเวลาดำเนินการ -->
          <div class="cell text-center col-span-1">
            <span class="font-medium" v-if="calcDaysInclusive(data.approvalDate, data.contractSignDate)">
              {{ calcDaysInclusive(data.approvalDate, data.contractSignDate) }}
            </span>
            <span v-else class="text-stone-300">-</span>
          </div>

          <div class="cell flex items-center justify-center gap-1.5 col-span-1">
            <template v-if="getLocalStatus(data.approvalDate, data.contractSignDate)">
              <span :class="[localStatusStyle[getLocalStatus(data.approvalDate, data.contractSignDate)!].dot, 'w-2 h-2 rounded-full shrink-0']"></span>
              <span class="text-xs text-stone-400 font-medium">{{ localStatusStyle[getLocalStatus(data.approvalDate, data.contractSignDate)!].label }}</span>
            </template>
            <span v-else class="text-xs text-stone-300">-</span>
          </div>
          <div v-if="!readonly" class="cell flex justify-center gap-1 col-span-1">
            <template v-if="editingId === data.planId">
              <Button icon="pi pi-check" class="text-emerald-600! hover:bg-emerald-300/20! focus:bg-emerald-300/20!" size="small" variant="text" @click="saveInlineEdit(data)" />
              <Button icon="pi pi-times" class="text-gray-500! hover:bg-gray-300/20! focus:bg-gray-300/20!" size="small" variant="text" @click="cancelEdit" />
            </template>
            <template v-else>
              <Button icon="pi pi-pen-to-square" class="text-blue-600! hover:bg-blue-300/20! focus:bg-blue-300/20!" size="small" variant="text" @click="startEdit(data)" />
            </template>
          </div>
        </div>
      </div>
      <p v-else class="text-center py-6 text-gray-400">ไม่พบข้อมูล</p>
    </div>

    <!-- Paginator -->
    <Pagination
      :page-number="pageNumber"
      :page-size="pageSize"
      :total-record="filteredItems.length"
      @change="onChangePage"
    />

    <!-- Delete Confirm Dialog -->
    <Dialog v-model:visible="showDeleteConfirm" header="ยืนยันการลบ" :modal="true" :style="{ width: '380px' }">
      <div class="flex items-center gap-3 py-2">
        <i class="pi pi-exclamation-triangle text-2xl text-orange-400"></i>
        <span>ต้องการลบรายการนี้หรือไม่?</span>
      </div>
      <template #footer>
        <Button label="ยกเลิก" severity="secondary" text @click="showDeleteConfirm = false" />
        <Button label="ลบ" severity="danger" icon="pi pi-trash" @click="doRemove" />
      </template>
    </Dialog>

  </div>
</template>

<style scoped>
.header {
  text-align: center;
  font-weight: bold;
  font-size: 0.875rem;
  padding: 0.5rem 0.5rem;
}

.cell {
  font-size: 0.875rem;
  padding: 0.5rem 0.5rem;
}
</style>
