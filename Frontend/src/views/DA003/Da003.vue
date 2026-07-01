<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { TitleHeader, TabHeader } from '@/components/cosmetic'
import { Button, Tabs, TabPanels, TabPanel } from 'primevue'
import CriteriaGroupButton from '@/components/forms/CriteriaGroupButton.vue'
import FilterBar from './components/FilterBar.vue'
import OverviewCards from './components/OverviewCards.vue'
import PerformanceTrend from './components/PerformanceTrend.vue'
import StatusPieChart from './components/StatusPieChart.vue'
import SummaryCards from './components/SummaryCards.vue'
import ProcurementTable from './components/ProcurementTable.vue'
import { useDa003Store } from '@/stores/DA/da003'
import { type ProcurementItem, type FilterCriteria, type DateFilterType, ProcurementStatus } from '@/models/DA/da003'
import da003Service from '@/services/DA/da003'
import ToastHelper from '@/helpers/toast'

function calcWorkingDaysInclusive(from: string | null, to: string | null): number | null {
  if (!from || !to) return null
  const start = new Date(from + 'T00:00:00')
  const end = new Date(to + 'T00:00:00')
  let count = 0
  const cur = new Date(start)
  while (cur <= end) {
    const day = cur.getDay()
    if (day !== 0 && day !== 6) count++
    cur.setDate(cur.getDate() + 1)
  }
  return count || null
}

function getLocalStatus(item: ProcurementItem): ProcurementStatus | null {
  const days = calcWorkingDaysInclusive(item.approvalDate, item.contractSignDate)
  if (days === null) return null
  if (days <= 10) return ProcurementStatus.OnPlan
  if (days <= 15) return ProcurementStatus.Risk
  return ProcurementStatus.Delay
}

const store = useDa003Store()

// ---- Tab ----
const activeTab = ref<string | number>('overview')
const tabItems = ref([
  { label: 'ภาพรวม', value: 'overview' },
  { label: 'ติดตามสถานะ', value: 'tracking' },
])

// ---- Filters ----
const filters = ref<FilterCriteria | null>(null)

const statusLabelMap: Record<string, ProcurementStatus> = {
  'เป็นไปตามแผน': ProcurementStatus.OnPlan,
  'มีแนวโน้มล่าช้า': ProcurementStatus.Risk,
  'ช้ากว่าแผน': ProcurementStatus.Delay,
}


const periodTypeDateTypeMap: Record<string, DateFilterType> = {
  'อนุมัติเมื่อ': 'PlanDate',
  'ออกใบสั่งซื้อเมื่อ': 'PurchaseOrderDate',
  'แจ้งเตรียมเอกสารเมื่อ': 'DocPrepareNotifyDate',
  'ลงนามสัญญาเมื่อ': 'ContractDate',
}

function toISODate(d: Date | null): string | undefined {
  if (!d) return undefined
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

// ---- Overview — client-side filter on loaded data ----
const allItems = computed(() => [...store.items1, ...store.items2])

const activeDateRange = computed<[Date, Date] | null>(() => {
  if (!filters.value?.dateFrom || !filters.value?.dateTo) return null
  return [filters.value.dateFrom, filters.value.dateTo]
})

// ---- Tracking ----
const trackingTab = ref<string | number>('0')
const trackingTabItems = ref([
  { label: 'กลุ่มที่ 1: สัญญาของธนาคาร', value: '0' },
  { label: 'กลุ่มที่ 2: สัญญาของหน่วยงานอื่น', value: '1' },
])
const statusFilter = ref<ProcurementStatus | null>(null)

const currentItems = computed(() => {
  const items = trackingTab.value === '0' ? store.items1 : store.items2
  if (!statusFilter.value) return items
  return items.filter(i => getLocalStatus(i) === statusFilter.value)
})

// ---- Export Excel ----
async function exportExcel() {
  const f = filters.value
  const params = {
    keyword: f?.search || undefined,
    supplyMethodSpecialTypeLabel: f?.method || undefined,
    status: f?.status ? statusLabelMap[f.status] : undefined,
    dateType: f?.periodType ? periodTypeDateTypeMap[f.periodType] : undefined,
    dateFrom: f?.dateFrom ? toISODate(f.dateFrom) : undefined,
    dateTo: f?.dateTo ? toISODate(f.dateTo) : undefined,
  }
  const { data, status } = await da003Service.exportExcelAsync(params)
  if (status !== 200) return
  const now = new Date()
  const dateStr = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}${String(now.getSeconds()).padStart(2, '0')}`
  const fileName = `ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา_${dateStr}.xlsx`
  const url = window.URL.createObjectURL(data)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  window.URL.revokeObjectURL(url)
  a.remove()
}

// ---- Import Excel ----
const importFileInput = ref<HTMLInputElement | null>(null)
const importing = ref(false)

function triggerImport() {
  importFileInput.value?.click()
}

async function onImportFileChange(event: Event) {
  const file = (event.target as HTMLInputElement).files?.[0]
  if (!file) return
  importing.value = true
  try {
    const { data } = await da003Service.importExcelAsync(file)
    const msg = `นำเข้าสำเร็จ ${data.imported} รายการ` + (data.skipped > 0 ? `, ไม่พบแผน ${data.skipped} รายการ (${data.skippedPlanNumbers.join(', ')})` : '')
    ToastHelper.success('นำเข้าข้อมูลสำเร็จ', msg)
    await fetchData()
  } catch {
    ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถนำเข้าข้อมูลได้')
  } finally {
    importing.value = false
    if (importFileInput.value) importFileInput.value.value = ''
  }
}

// ---- API fetch ----
async function fetchData() {
  const f = filters.value
  const params = {
    keyword: f?.search || undefined,
    supplyMethodSpecialTypeLabel: f?.method || undefined,
    status: f?.status ? statusLabelMap[f.status] : undefined,
    dateType: f?.periodType ? periodTypeDateTypeMap[f.periodType] : undefined,
    dateFrom: f?.dateFrom ? toISODate(f.dateFrom) : undefined,
    dateTo: f?.dateTo ? toISODate(f.dateTo) : undefined,
  }
  await store.fetchItems1(params)
}

onMounted(() => fetchData())

watch(trackingTab, () => { statusFilter.value = null })

const activeCriteriaTags = computed(() => {
  const f = filters.value
  if (!f) return []
  const tags: { label: string; icon: string }[] = []

  if (f.periodType && f.periodType !== 'อนุมัติเมื่อ')
    tags.push({ label: f.periodType, icon: 'pi-calendar' })

  if (f.dateFrom)
    tags.push({ label: `จาก ${f.dateFrom.toLocaleDateString('th-TH', { calendar: 'buddhist', year: 'numeric', month: 'short', day: 'numeric' })}`, icon: 'pi-calendar' })

  if (f.dateTo)
    tags.push({ label: `ถึง ${f.dateTo.toLocaleDateString('th-TH', { calendar: 'buddhist', year: 'numeric', month: 'short', day: 'numeric' })}`, icon: 'pi-calendar' })

  if (f.method)
    tags.push({ label: f.method, icon: 'pi-tag' })

  if (f.status)
    tags.push({ label: f.status, icon: 'pi-info-circle' })

  if (f.search)
    tags.push({ label: `"${f.search}"`, icon: 'pi-search' })

  return tags
})
</script>

<template>
  <TitleHeader label="ติดตามสถานะการออกใบสั่งจ้างและการลงนามสัญญา">
    <template #action>
      <div class="flex items-center gap-2 mr-4 px-4 py-1.5 bg-blue-50 border border-blue-200 rounded-full text-base text-blue-800">
        <i class="pi pi-calendar text-blue-500" />
        <span class="font-bold">ข้อมูล ณ วันที่</span>
        <span>{{ new Date().toLocaleDateString('th-TH', { calendar: 'buddhist', year: 'numeric', month: 'long', day: 'numeric' }) }}</span>
      </div>
    </template>
  </TitleHeader>

  <div class="mt-4">
    <FilterBar
      @query="(f) => { filters = f; fetchData() }"
      @reset="() => { filters = null; fetchData() }"
    />

    <div v-if="activeCriteriaTags.length > 0"
      class="flex flex-wrap items-center gap-x-4 gap-y-2 my-3 px-4 py-2 bg-gray-50 border border-gray-200 rounded-lg">
      <span class="flex items-center gap-1.5 text-xs text-blue-600 whitespace-nowrap shrink-0">
        <i class="pi pi-filter" />
        กำลังแสดงข้อมูลตาม :
      </span>
      <div class="flex flex-wrap gap-1.5">
        <span
          v-for="tag in activeCriteriaTags"
          :key="tag.label"
          class="inline-flex items-center gap-1 px-2.5 py-0.5 bg-blue-50 border border-blue-200 text-blue-700 rounded-full text-xs font-medium"
        >
          <i :class="`pi ${tag.icon} text-[10px]`" />
          {{ tag.label }}
        </span>
      </div>
    </div>

    <CriteriaGroupButton v-model="activeTab" :options="tabItems" />

    <!-- ===== Tab: ภาพรวม ===== -->
    <div v-if="activeTab === 'overview'">
      <OverviewCards :items="allItems" />
      <div class="grid grid-cols-1 lg:grid-cols-[4fr_2fr] gap-5 mb-5">
        <PerformanceTrend :items="allItems" :dateRange="activeDateRange" />
        <StatusPieChart :items="allItems" />
      </div>
    </div>

    <!-- ===== Tab: ติดตามสถานะ ===== -->
    <div v-if="activeTab === 'tracking'">
      <SummaryCards
        :items="trackingTab === '0' ? store.items1 : store.items2"
        :activeStatus="statusFilter"
        @filter="(s) => statusFilter = s"
      />

      <div class="bg-white shadow-sm px-4">
        <Tabs :value="trackingTab" unstyled @update:value="(v) => { trackingTab = v.toString(); statusFilter = null }">
          <div class="flex items-center justify-between">
            <TabHeader :items="trackingTabItems" />
            <div class="flex items-center gap-2">
              <input ref="importFileInput" type="file" accept=".xlsx,.xls" class="hidden" @change="onImportFileChange" />
              <Button label="นำเข้า Excel" icon="pi pi-file-import" severity="secondary" variant="outlined"
                class="bg-white! hover:bg-gray-50!" :loading="importing" @click="triggerImport" />
              <Button label="พิมพ์รายงาน" icon="pi pi-file-export" severity="primary" variant="outlined"
                class="bg-white! hover:bg-blue-50!" @click="exportExcel" />
            </div>
          </div>
          <TabPanels>
            <TabPanel value="0">
              <ProcurementTable
                :items="currentItems"
                :showBudget="false"
                @update="store.updateItem1"
                @remove="store.removeItem1"
              />
            </TabPanel>
            <TabPanel value="1">
              <ProcurementTable
                :items="currentItems"
                :showBudget="false"
                @update="store.updateItem2"
                @remove="store.removeItem2"
              />
            </TabPanel>
          </TabPanels>
        </Tabs>
      </div>
    </div>
  </div>
</template>
