<script setup lang="ts">
import { ref } from 'vue'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import InputText from 'primevue/inputtext'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import StatusBadge from './StatusBadge.vue'
import type { ProcurementItem } from '@/types/procurement'
import { FilterMatchMode } from '@primevue/core/api'
import { calculateStatus, calculateTotalDuration } from '@/composables/useStatusCalculation'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'

const toast = useToast()
const confirm = useConfirm()

const props = defineProps<{
  items: ProcurementItem[]
  title: string
  showBudget: boolean
}>()

const emit = defineEmits<{
  update: [item: ProcurementItem]
  remove: [id: number | string]
}>()

const filters = ref({
  global: { value: null, matchMode: FilterMatchMode.CONTAINS },
})

const first = ref(0)

// Edit dialog
const showEdit = ref(false)
const editItem = ref<ProcurementItem | null>(null)
const methodOptions = ['เฉพาะเจาะจง', 'คัดเลือก', 'e-bidding', 'e-market', 'พิเศษ']

const tempDates = ref({
  approvalDate: null as Date | null,
  poDate: null as Date | null,
  docPrepNoticeDate: null as Date | null,
  contractSignDate: null as Date | null,
})

function toISODate(d: Date | null): string | null {
  if (!d) return null
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function fromISODate(s: string | null): Date | null {
  if (!s) return null
  return new Date(s + 'T00:00:00')
}

function openEdit(item: ProcurementItem) {
  editItem.value = { ...item }
  tempDates.value = {
    approvalDate: fromISODate(item.approvalDate),
    poDate: fromISODate(item.poDate),
    docPrepNoticeDate: fromISODate(item.docPrepNoticeDate),
    contractSignDate: fromISODate(item.contractSignDate),
  }
  showEdit.value = true
}

function saveEdit() {
  if (!editItem.value) return
  const approvalDate = toISODate(tempDates.value.approvalDate)
  if (!approvalDate) return

  const contractSignDate = toISODate(tempDates.value.contractSignDate)

  const updated: ProcurementItem = {
    ...editItem.value,
    approvalDate,
    poDate: toISODate(tempDates.value.poDate),
    docPrepNoticeDate: toISODate(tempDates.value.docPrepNoticeDate),
    contractSignDate,
    status: calculateStatus(approvalDate, contractSignDate ?? undefined),
    totalDurationDays: calculateTotalDuration(approvalDate, contractSignDate),
  }

  try {
    emit('update', updated)
    showEdit.value = false
    toast.add({ severity: 'success', summary: 'สำเร็จ', detail: 'บันทึกการแก้ไขเรียบร้อยแล้ว', life: 3000 })
  } catch {
    toast.add({ severity: 'error', summary: 'ผิดพลาด', detail: 'ไม่สามารถบันทึกได้', life: 3000 })
  }
}

function confirmRemove(id: number | string) {
  confirm.require({
    message: 'ต้องการลบรายการนี้หรือไม่?',
    header: 'ยืนยันการลบ',
    icon: 'pi pi-exclamation-triangle',
    acceptLabel: 'ลบ',
    rejectLabel: 'ยกเลิก',
    acceptClass: 'p-button-danger',
    accept: () => {
      emit('remove', id)
      toast.add({ severity: 'success', summary: 'สำเร็จ', detail: 'ลบรายการเรียบร้อยแล้ว', life: 3000 })
    },
  })
}

function formatDate(date: string | null): string {
  if (!date) return '-'
  return new Date(date).toLocaleDateString('th-TH', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function formatCurrency(amount: number | undefined): string {
  if (amount == null) return '-'
  return new Intl.NumberFormat('th-TH', {
    style: 'currency',
    currency: 'THB',
    minimumFractionDigits: 0,
  }).format(amount)
}
</script>

<template>
  <div class="bg-white rounded-lg p-5 mb-6">
    <div class="flex items-center justify-between mb-4">
      <h2 v-if="props.title" class="text-lg font-semibold text-stone-700">{{ props.title }}</h2>
      <IconField>
        <InputIcon class="pi pi-search" />
        <InputText v-model="filters['global'].value" placeholder="ค้นหา..." />
      </IconField>
    </div>

    <DataTable
      :value="props.items"
      dataKey="id"
      :filters="filters"
      :paginator="props.items.length > 10"
      :rows="10"
      :rowsPerPageOptions="[5, 10, 20]"
      v-model:first="first"
      stripedRows
      removableSort
      scrollable
      scrollHeight="flex"
      tableStyle="min-width: 60rem"
    >
      <template #empty>
        <div class="text-center py-10">
          <i class="pi pi-inbox text-3xl text-stone-300 mb-3"></i>
          <p class="text-sm text-stone-400">ยังไม่มีข้อมูล</p>
        </div>
      </template>
      <Column header="ลำดับ" style="width: 5rem">
        <template #body="{ index }">
          {{ first + index + 1 }}
        </template>
      </Column>
      <Column field="projectName" header="ชื่อโครงการ/แผนงาน" sortable style="min-width: 14rem" />
      <Column field="method" header="วิธีการซื้อจ้าง" sortable style="width: 8rem" />
      <Column
        v-if="props.showBudget"
        field="budgetAmount"
        header="วงเงินซื้อจ้าง"
        sortable
        style="width: 9rem"
      >
        <template #body="{ data }">
          {{ formatCurrency((data as ProcurementItem).budgetAmount) }}
        </template>
      </Column>
      <Column field="approvalDate" header="วันที่อนุมัติ" sortable style="width: 8rem">
        <template #body="{ data }">
          {{ formatDate((data as ProcurementItem).approvalDate) }}
        </template>
      </Column>
      <Column field="poDate" header="วันที่ออกใบสั่งซื้อ" sortable style="width: 9rem">
        <template #body="{ data }">
          {{ formatDate((data as ProcurementItem).poDate) }}
        </template>
      </Column>
      <Column field="docPrepNoticeDate" header="วันที่แจ้งเตรียมเอกสาร" sortable style="width: 10rem">
        <template #body="{ data }">
          {{ formatDate((data as ProcurementItem).docPrepNoticeDate) }}
        </template>
      </Column>
      <Column field="contractSignDate" header="วันที่ลงนามสัญญา" sortable style="width: 9rem">
        <template #body="{ data }">
          {{ formatDate((data as ProcurementItem).contractSignDate) }}
        </template>
      </Column>
      <Column field="totalDurationDays" header="ระยะเวลา (วัน)" sortable style="width: 7rem">
        <template #body="{ data }">
          {{ (data as ProcurementItem).totalDurationDays ?? '-' }}
        </template>
      </Column>
      <Column field="status" header="สถานะ" sortable style="width: 9rem">
        <template #body="{ data }">
          <StatusBadge :status="(data as ProcurementItem).status" />
        </template>
      </Column>
      <Column header="จัดการ" style="width: 7rem">
        <template #body="{ data }">
          <div class="flex gap-1">
            <Button icon="pi pi-pencil" severity="warn" text rounded size="small" @click="openEdit(data as ProcurementItem)" />
            <Button icon="pi pi-trash" severity="danger" text rounded size="small" @click="confirmRemove((data as ProcurementItem).id)" />
          </div>
        </template>
      </Column>
    </DataTable>

    <!-- Edit Dialog -->
    <Dialog
      v-model:visible="showEdit"
      header="แก้ไขรายการ"
      :modal="true"
      :style="{ width: '95vw', maxWidth: '550px' }"
    >
      <div v-if="editItem" class="grid grid-cols-1 md:grid-cols-2 gap-4 py-2">
        <div class="md:col-span-2">
          <label class="text-xs text-stone-400 mb-1 block font-medium">ชื่อโครงการ/แผนงาน</label>
          <InputText v-model="editItem.projectName" class="w-full" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วิธีการซื้อจ้าง</label>
          <Select v-model="editItem.method" :options="methodOptions" class="w-full" />
        </div>
        <div v-if="props.showBudget">
          <label class="text-xs text-stone-400 mb-1 block font-medium">วงเงินซื้อจ้าง (บาท)</label>
          <InputNumber v-model="editItem.budgetAmount" mode="currency" currency="THB" locale="th-TH" class="w-full" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่อนุมัติ *</label>
          <DatePicker v-model="tempDates.approvalDate" dateFormat="dd/mm/yy" class="w-full" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่ออกใบสั่งซื้อสั่งจ้าง</label>
          <DatePicker v-model="tempDates.poDate" dateFormat="dd/mm/yy" class="w-full" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่ออกหนังสือแจ้งเตรียมเอกสาร</label>
          <DatePicker v-model="tempDates.docPrepNoticeDate" dateFormat="dd/mm/yy" class="w-full" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่ลงนามสัญญา</label>
          <DatePicker v-model="tempDates.contractSignDate" dateFormat="dd/mm/yy" class="w-full" />
        </div>
      </div>

      <template #footer>
        <Button label="ยกเลิก" severity="secondary" text @click="showEdit = false" />
        <Button label="บันทึก" icon="pi pi-check" @click="saveEdit" />
      </template>
    </Dialog>
  </div>
</template>
