<script setup lang="ts">
import { ref, computed } from 'vue'
import Tabs from 'primevue/tabs'
import TabList from 'primevue/tablist'
import Tab from 'primevue/tab'
import TabPanels from 'primevue/tabpanels'
import TabPanel from 'primevue/tabpanel'
import Button from 'primevue/button'
import Menu from 'primevue/menu'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import { useToast } from 'primevue/usetoast'
import SummaryCards from '@/components/SummaryCards.vue'
import ProcurementTable from '@/components/ProcurementTable.vue'
import { useProcurementStore } from '@/stores/procurement'
import type { ProcurementStatus, ProcurementItem } from '@/types/procurement'
import { calculateStatus, calculateTotalDuration } from '@/composables/useStatusCalculation'
import { exportToExcel, exportToPDF } from '@/composables/useExport'
import { parseExcelFile } from '@/composables/useImport'

const store = useProcurementStore()
const toast = useToast()
const activeTab = ref('0')
const statusFilter = ref<ProcurementStatus | null>(null)
const currentItems = computed(() => {
  const items = activeTab.value === '0' ? store.bankItems : store.otherItems
  if (!statusFilter.value) return items
  return items.filter(i => i.status === statusFilter.value)
})
const showBudget = computed(() => activeTab.value === '1')
const groupLabel = computed(() => activeTab.value === '0' ? 'กลุ่ม 1 สัญญาของธนาคาร' : 'กลุ่ม 2 สัญญาของหน่วยงานอื่น')

// Export menu
const exportMenu = ref()
const exportItems = [
  {
    label: 'Export Excel',
    icon: 'pi pi-file-excel',
    command: () => exportToExcel(currentItems.value, showBudget.value, groupLabel.value),
  },
  {
    label: 'Export PDF',
    icon: 'pi pi-file-pdf',
    command: () => exportToPDF(currentItems.value, showBudget.value, groupLabel.value),
  },
]

// Import
const importInput = ref<HTMLInputElement | null>(null)

async function onImportFile(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  try {
    const items = await parseExcelFile(file)
    if (items.length === 0) {
      toast.add({ severity: 'warn', summary: 'ไม่พบข้อมูล', detail: 'ตรวจสอบ header ใน Excel: ชื่อโครงการ/แผนงาน, วิธีการซื้อจ้าง, วันที่อนุมัติ', life: 5000 })
      return
    }
    const existing = currentItems.value.map(i => i.projectName.trim().toLowerCase())
    const newItems = items.filter(i => !existing.includes(i.projectName.trim().toLowerCase()))
    const skipped = items.length - newItems.length

    if (newItems.length === 0) {
      toast.add({ severity: 'warn', summary: 'ข้อมูลซ้ำทั้งหมด', detail: `${skipped} รายการมีอยู่แล้ว`, life: 3000 })
      return
    }

    let success = 0
    for (const item of newItems) {
      try {
        if (activeTab.value === '0') store.addBankItem(item)
        else store.addOtherItem(item)
        success++
      } catch {
        // skip failed rows
      }
    }
    const detail = skipped > 0 ? `เพิ่ม ${success} รายการ, ข้าม ${skipped} รายการซ้ำ` : `เพิ่ม ${success} รายการ`
    toast.add({ severity: 'success', summary: 'นำเข้าสำเร็จ', detail, life: 3000 })
  } catch {
    toast.add({ severity: 'error', summary: 'ผิดพลาด', detail: 'ไม่สามารถอ่านไฟล์ได้', life: 3000 })
  }
  if (importInput.value) importInput.value.value = ''
}

// Add dialog
const showAdd = ref(false)
const methodOptions = ['เฉพาะเจาะจง', 'คัดเลือก', 'e-bidding', 'e-market', 'พิเศษ']
const groupOptions = [
  { label: 'กลุ่ม 1: สัญญาของธนาคาร', value: 'bank' },
  { label: 'กลุ่ม 2: สัญญาของหน่วยงานอื่น', value: 'other' },
]
const addGroup = ref<'bank' | 'other'>('bank')
const addForm = ref({
  projectName: '',
  method: '',
  budgetAmount: undefined as number | undefined,
})
const addDates = ref({
  approvalDate: null as Date | null,
  poDate: null as Date | null,
  docPrepNoticeDate: null as Date | null,
  contractSignDate: null as Date | null,
})

function openAdd() {
  addGroup.value = activeTab.value === '0' ? 'bank' : 'other'
  showAdd.value = true
}

function resetAddForm() {
  addForm.value = { projectName: '', method: '', budgetAmount: undefined }
  addDates.value = { approvalDate: null, poDate: null, docPrepNoticeDate: null, contractSignDate: null }
}

function toISODate(d: Date | null): string | null {
  if (!d) return null
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function saveAdd() {
  const approvalDate = toISODate(addDates.value.approvalDate)
  if (!approvalDate || !addForm.value.projectName || !addForm.value.method) {
    toast.add({ severity: 'warn', summary: 'กรุณากรอกข้อมูล', detail: 'ชื่อโครงการ, วิธีการ และวันที่อนุมัติ จำเป็นต้องระบุ', life: 3000 })
    return
  }

  const contractSignDate = toISODate(addDates.value.contractSignDate)

  const payload: Omit<ProcurementItem, 'id'> = {
    projectName: addForm.value.projectName,
    method: addForm.value.method,
    approvalDate,
    poDate: toISODate(addDates.value.poDate),
    docPrepNoticeDate: toISODate(addDates.value.docPrepNoticeDate),
    contractSignDate,
    status: calculateStatus(approvalDate, contractSignDate ?? undefined),
    totalDurationDays: calculateTotalDuration(approvalDate, contractSignDate),
    ...(addGroup.value === 'other' ? { budgetAmount: addForm.value.budgetAmount } : {}),
  }

  if (addGroup.value === 'bank') store.addBankItem(payload)
  else store.addOtherItem(payload)

  toast.add({ severity: 'success', summary: 'สำเร็จ', detail: 'เพิ่มรายการเรียบร้อยแล้ว', life: 3000 })
  resetAddForm()
  showAdd.value = false
}

function onUpdateBank(item: ProcurementItem) {
  store.updateBankItem(item)
}

function onUpdateOther(item: ProcurementItem) {
  store.updateOtherItem(item)
}

function onRemoveBank(id: number | string) {
  store.removeBankItem(id as number)
}

function onRemoveOther(id: number | string) {
  store.removeOtherItem(id as number)
}
</script>

<template>
  <div class="p-4 sm:p-6 max-w-[1400px] mx-auto">
    <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-2 mb-1">
      <h1 class="text-lg font-semibold text-stone-800">ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา</h1>
      <div class="flex gap-2 flex-wrap">
        <input ref="importInput" type="file" accept=".xlsx,.xls" class="hidden" @change="onImportFile" />
        <Button label="นำเข้า Excel" icon="pi pi-upload" size="small" severity="secondary" outlined @click="importInput?.click()" />
        <Button label="Export" icon="pi pi-download" size="small" severity="secondary" outlined @click="exportMenu.toggle($event)" />
        <Menu ref="exportMenu" :model="exportItems" popup />
        <Button label="กรอกข้อมูล" icon="pi pi-plus" size="small" @click="openAdd" />
      </div>
    </div>
    <p class="text-xs text-stone-400 mb-5">ฝ่ายจัดหาและการพัสดุ | ข้อมูล ณ วันที่ {{ new Date().toLocaleDateString('th-TH', { year: 'numeric', month: 'long', day: 'numeric' }) }}</p>

    <SummaryCards
      :items="activeTab === '0' ? store.bankItems : store.otherItems"
      :activeStatus="statusFilter"
      @filter="(s) => statusFilter = s"
    />

    <div class="bg-white border border-stone-200 rounded-xl">
      <Tabs v-model:value="activeTab">
        <TabList>
          <Tab value="0">กลุ่มที่ 1: สัญญาของธนาคาร</Tab>
          <Tab value="1">กลุ่มที่ 2: สัญญาของหน่วยงานอื่น</Tab>
        </TabList>
        <TabPanels>
          <TabPanel value="0">
            <ProcurementTable
              title=""
              :items="currentItems"
              :showBudget="false"
              @update="onUpdateBank"
              @remove="onRemoveBank"
            />
          </TabPanel>
          <TabPanel value="1">
            <ProcurementTable
              title=""
              :items="currentItems"
              :showBudget="true"
              @update="onUpdateOther"
              @remove="onRemoveOther"
            />
          </TabPanel>
        </TabPanels>
      </Tabs>
    </div>

    <!-- Add Dialog -->
    <Dialog
      v-model:visible="showAdd"
      header="เพิ่มรายการใหม่"
      :modal="true"
      :style="{ width: '95vw', maxWidth: '600px' }"
      @hide="resetAddForm"
    >
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4 py-2">
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">กลุ่มสัญญา *</label>
          <Select v-model="addGroup" :options="groupOptions" optionLabel="label" optionValue="value" class="w-full" />
        </div>
        <div class="md:col-span-2">
          <label class="text-xs text-stone-400 mb-1 block font-medium">ชื่อโครงการ/แผนงาน *</label>
          <InputText v-model="addForm.projectName" class="w-full" placeholder="ระบุชื่อโครงการ" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วิธีการซื้อจ้าง *</label>
          <Select v-model="addForm.method" :options="methodOptions" class="w-full" placeholder="เลือกวิธีการ" />
        </div>
        <div v-if="addGroup === 'other'">
          <label class="text-xs text-stone-400 mb-1 block font-medium">วงเงินซื้อจ้าง (บาท)</label>
          <InputNumber v-model="addForm.budgetAmount" mode="currency" currency="THB" locale="th-TH" class="w-full" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่อนุมัติ *</label>
          <DatePicker v-model="addDates.approvalDate" dateFormat="dd/mm/yy" class="w-full" placeholder="เลือกวันที่" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่ออกใบสั่งซื้อสั่งจ้าง</label>
          <DatePicker v-model="addDates.poDate" dateFormat="dd/mm/yy" class="w-full" placeholder="เลือกวันที่" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่แจ้งเตรียมเอกสาร</label>
          <DatePicker v-model="addDates.docPrepNoticeDate" dateFormat="dd/mm/yy" class="w-full" placeholder="เลือกวันที่" />
        </div>
        <div>
          <label class="text-xs text-stone-400 mb-1 block font-medium">วันที่ลงนามสัญญา</label>
          <DatePicker v-model="addDates.contractSignDate" dateFormat="dd/mm/yy" class="w-full" placeholder="เลือกวันที่" />
        </div>
      </div>

      <template #footer>
        <Button label="ยกเลิก" severity="secondary" text @click="showAdd = false" />
        <Button label="เพิ่มรายการ" icon="pi pi-check" @click="saveAdd" />
      </template>
    </Dialog>
  </div>
</template>
