<script setup lang="ts">
import { ButtonClear } from '@/components/Button';
import { TitleHeader } from '@/components/cosmetic';
import { Select } from '@/components/forms';
import { Card } from 'primevue';
import type { DashboardValue } from '@/models/DA/dashboardMd';
import { onMounted, onUnmounted, ref, watch, nextTick } from 'vue';
import DoughnutChart from './components/DoughnutChart.vue';
import { formatCurrency } from '@/helpers/currency';
import { useDashboardMDStore } from '@/stores/DA/dashboardMd';
import { YearOptions } from '@/constants/date';

const store = useDashboardMDStore();

const showHideSub = (index: number) => {
  store.historyTable.rows[index].showExpand = !store.historyTable.rows[index].showExpand;
};

const toggleChild = (item: { showExpand: boolean }) => {
  item.showExpand = !item.showExpand;
};

const SUPPLY_METHOD_COLORS: Record<string, string> = {
  'ข้อบังคับ': 'rgb(249,115,22)',   // orange — SMethod004
  'พ.ร.บ.': 'rgb(59,130,246)',       // blue   — SMethod002
};

const resolveColors = (data: { label: string }[]) =>
  data.map(d => {
    const match = Object.entries(SUPPLY_METHOD_COLORS).find(([key]) => d.label.includes(key));
    return match ? match[1] : 'rgb(99,102,241)';
  });

const levelRowClass = (orgLevel: number) => {
  if (orgLevel <= 100) return 'bg-gray-100 font-bold text-gray-800';
  if (orgLevel === 200) return 'bg-gray-50 font-semibold text-gray-700';
  if (orgLevel === 300) return 'text-gray-600';
  return 'text-gray-500 text-sm';
};

const levelIndent = (orgLevel: number) => {
  if (orgLevel <= 100) return 'pl-2';
  if (orgLevel === 200) return 'pl-5';
  if (orgLevel === 300) return 'pl-9';
  return 'pl-14';
};

const levelBadge = (orgLevel: number) => {
  if (orgLevel <= 100) return { label: 'Head', cls: 'bg-slate-200 text-slate-700' };
  if (orgLevel === 200) return { label: 'กลุ่มงาน', cls: 'bg-blue-100 text-blue-700' };
  if (orgLevel === 300) return { label: 'สายงาน', cls: 'bg-purple-100 text-purple-700' };
  return { label: 'ฝ่าย', cls: 'bg-green-100 text-green-700' };
};

const HeaderItem = ref([
  {
    label: 'ข้อบังคับธนาคาร 80',
    value: 'SMethod004',
  },
  {
    label: 'พ.ร.บ.จัดซื้อจัดจ้างฯ 2560',
    value: 'SMethod002',
  }
]);

onMounted(async () => {
  await Promise.all([
    store.api.onGetDropdownAsync(),
    store.api.getDashBoardAsync(),
    store.api.getDashBoardTableAsync(),
  ]);
})

let isClearingCascade = false;
let isSyncingFromChart = false;

watch(() => store.tableCriteria.groupCode, (newVal) => {
  if (isClearingCascade) return;
  if (newVal) {
    isClearingCascade = true;
    store.tableCriteria.lineCode = undefined;
    store.tableCriteria.departmentCode = undefined;
    isClearingCascade = false;
  }
});

watch(() => store.tableCriteria.lineCode, (newVal) => {
  if (isClearingCascade) return;
  if (newVal) {
    isClearingCascade = true;
    store.tableCriteria.groupCode = undefined;
    store.tableCriteria.departmentCode = undefined;
    isClearingCascade = false;
  }
});

watch(() => store.tableCriteria.departmentCode, (newVal) => {
  if (isClearingCascade) return;
  if (newVal) {
    isClearingCascade = true;
    store.tableCriteria.groupCode = undefined;
    store.tableCriteria.lineCode = undefined;
    isClearingCascade = false;
  }
});

watch(
  () => [
    store.tableCriteria.supplyMethodCode,
    store.tableCriteria.groupCode,
    store.tableCriteria.lineCode,
    store.tableCriteria.departmentCode,
  ],
  async () => {
    if (isClearingCascade) return;
    await Promise.all([
      store.api.getDashBoardTableAsync(),
      store.api.getDashBoardAsync(),
    ]);
  },
);

watch(() => store.criteria.budgetYear, async () => {
  await store.api.getDashBoardAsync();
})

watch(() => store.criteria.supplyMethodCode, (newVal) => {
  if (isSyncingFromChart) return;
  const matched = store.supplyMethodDropdown.find(o => o.value === newVal);
  selectedSupplyMethodTypeLabel.value = matched?.label;
})

const selectedSupplyMethodTypeLabel = ref<string | undefined>(undefined);
const cachedPlanBudgetBySupplyMethod = ref<DashboardValue[]>([]);

const onSelectSupplyMethodType = async (label: string | undefined) => {
  selectedSupplyMethodTypeLabel.value = label;

  if (label !== undefined && cachedPlanBudgetBySupplyMethod.value.length === 0) {
    cachedPlanBudgetBySupplyMethod.value = [...store.body.charts.planBudgetBySupplyMethod];
  }

  const matched = store.supplyMethodDropdown.find(o => o.label === label);
  isSyncingFromChart = true;
  store.criteria.supplyMethodCode = matched?.value as string | undefined;
  await nextTick();
  isSyncingFromChart = false;
  await store.api.getDashBoardAsync();

  if (label === undefined) {
    cachedPlanBudgetBySupplyMethod.value = [];
  }
};

const planBudgetChartData = () =>
  cachedPlanBudgetBySupplyMethod.value.length > 0
    ? cachedPlanBudgetBySupplyMethod.value
    : store.body.charts.planBudgetBySupplyMethod;

onUnmounted(async () => {
  await store.clearCriteria();
  await store.clearTableCriteria();
})
</script>

<template>
  <TitleHeader label="ภาพรวมการจัดซื้อจัดจ้าง ธนาคารอาคารสงเคราะห์">
    <template #action>
      <div class="flex items-center gap-2 mr-4 px-4 py-1.5 bg-blue-50 border border-blue-200 rounded-full text-base text-blue-800">
        <i class="pi pi-calendar text-blue-500" />
        <span class="font-bold">ข้อมูล ณ วันที่</span>
        <span>{{ new Date().toLocaleDateString('th-TH', { calendar: 'buddhist', year: 'numeric', month: 'long', day: 'numeric' }) }}</span>
      </div>
    </template>
  </TitleHeader>
  <Card class="my-4">
    <template #content>
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-4">
        <Select :options="YearOptions" v-model="store.criteria.budgetYear" label="ปีงบประมาณ" hide-details />
        <Select :options="store.supplyMethodDropdown" v-model="store.criteria.supplyMethodCode" label="วิธีการจัดหา" hide-details />
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-4 gap-4 mt-6">
        <Select label="กลุ่มงาน" v-model="store.tableCriteria.groupCode" :options="store.groupDropdown" hide-details />
        <Select label="สายงาน" v-model="store.tableCriteria.lineCode" :options="store.lineDropdown" hide-details />
        <Select label="ฝ่าย" v-model="store.tableCriteria.departmentCode" :options="store.departmentDropdown" hide-details />
        <div class="flex items-end">
          <ButtonClear @click="async () => { await store.clearCriteria(); await store.clearTableCriteria(); }" />
        </div>
      </div>
    </template>
  </Card>
  <div v-for="(item, index) in store.body.items" :key="index" class="flex gap-3 mb-4">
    <span :class="`w-1 rounded-full flex-shrink-0 ${item.supplyMethodCode == 'SMethod004' ? 'bg-orange-400' : 'bg-blue-400'}`" />
    <div class="flex-1">
      <div class="flex items-center gap-2 mb-3">
        <span class="text-lg font-bold text-gray-800">{{ item.supplyMethodName }}</span>
        <span :class="`px-2.5 py-0.5 rounded-full text-sm font-semibold ${item.supplyMethodCode == 'SMethod004' ? 'bg-orange-100 text-orange-700' : 'bg-blue-100 text-blue-700'}`">
          รวม {{ (item.planCount + item.procurementCount + item.contractDraftVendorCount + item.principleApprovalCount).toLocaleString('th-TH') }} โครงการ
        </span>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <Card :class="item.supplyMethodCode == 'SMethod004' ? 'bg-orange-50' : 'bg-blue-50'">
          <template #content>
            <p class="text-sm text-gray-500">แผนการจัดซื้อจัดจ้าง</p>
            <p :class="`text-lg font-bold ${item.supplyMethodCode == 'SMethod004' ? 'text-orange-700' : 'text-blue-700'}`">
              {{ item.planCount.toLocaleString('th-TH') }} โครงการ
            </p>
          </template>
        </Card>
        <Card :class="item.supplyMethodCode == 'SMethod004' ? 'bg-orange-50' : 'bg-blue-50'">
          <template #content>
            <p class="text-sm text-gray-500">การจัดซื้อจัดจ้าง</p>
            <p :class="`text-lg font-bold ${item.supplyMethodCode == 'SMethod004' ? 'text-orange-700' : 'text-blue-700'}`">
              {{ item.procurementCount.toLocaleString('th-TH') }} โครงการ
            </p>
          </template>
        </Card>
        <Card :class="item.supplyMethodCode == 'SMethod004' ? 'bg-orange-50' : 'bg-blue-50'">
          <template #content>
            <p class="text-sm text-gray-500">จัดทำใบสั่ง / สัญญา</p>
            <p :class="`text-lg font-bold ${item.supplyMethodCode == 'SMethod004' ? 'text-orange-700' : 'text-blue-700'}`">
              {{ item.contractDraftVendorCount.toLocaleString('th-TH') }} โครงการ
            </p>
          </template>
        </Card>
      </div>
    </div>
  </div>
  <div class="grid grid-cols-1 lg:grid-cols-7 items-start gap-4 mt-4">
    <DoughnutChart :width="550" :height="450" :data="planBudgetChartData()" class="lg:col-span-4"
      :label="`ปีงบประมาณ ${store.criteria.budgetYear}`"
      :colors="resolveColors(planBudgetChartData())"
      :selected-label="selectedSupplyMethodTypeLabel"
      @select="onSelectSupplyMethodType" />
    <div class="lg:col-span-3 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-1 gap-4">
      <DoughnutChart :width="400" :height="200" :data="store.body.charts.planBudgetBySupplyMethodType"
        :label="`ภาพรวมการจัดซื้อจัดจ้าง ตามวิธี ปีงบประมาณ ${store.criteria.budgetYear}`"
        :active-filter-label="selectedSupplyMethodTypeLabel"
        @clear-filter="onSelectSupplyMethodType(undefined)" />
      <DoughnutChart :width="400" :height="200" :data="store.body.charts.combinedBudgetPlanPw119P79PettyCash"
        :label="`ภาพรวมการจัดซื้อจัดจ้าง ปีงบประมาณ ${store.criteria.budgetYear}`"
        :active-filter-label="selectedSupplyMethodTypeLabel"
        @clear-filter="onSelectSupplyMethodType(undefined)" />
    </div>
  </div>
  <Card class="mt-4">
    <template #content>
      <div class="sticky top-[58px] z-20 bg-[#F7F7F7]">
        <Tabs :value="store.tableCriteria.supplyMethodCode as string" unstyled
          @update:value="(tab) => store.tableCriteria.supplyMethodCode = tab.toString()">
          <TabHeader :items="HeaderItem" class="no-custom" />
        </Tabs>
        <!-- Table Header -->
        <div class="border-x border-t border-gray-200 rounded-t-lg mt-2">
          <div class="grid grid-cols-5 bg-gray-50 divide-x divide-gray-200">
            <div class="header col-span-4 flex items-center justify-center py-2">
              กลุ่มงาน/ฝ่าย
            </div>
            <div class="header col-span-1 flex items-center justify-center py-2">
              งบประมาณจัดซื้อจัดจ้าง
            </div>
          </div>
        </div>
      </div>
      <!-- Body -->
      <div class="border-x border-b border-gray-200 rounded-b-lg">
        <div v-if="store.historyTable.rows.length > 0" class="divide-y divide-gray-200">
          <div v-for="(data, index) in store.historyTable.rows" :key="index">
            <div :class="`grid grid-cols-5 items-center divide-x divide-gray-200 py-2 ${levelRowClass(data.orgLevel)}`">
              <div :class="`col-span-4 flex items-center gap-1 px-2 ${levelIndent(data.orgLevel)}`">
                <i class="cursor-pointer shrink-0 text-gray-400"
                  :class="data.showExpand ? 'pi pi-chevron-down' : 'pi pi-chevron-right'"
                  @click="() => showHideSub(index)" v-if="data.childenDetail.length > 0" />
                <span v-else class="w-4 shrink-0" />
                <span :class="`inline-flex items-center px-1.5 py-0.5 rounded text-xs mr-1 shrink-0 ${levelBadge(data.orgLevel).cls}`">
                  {{ levelBadge(data.orgLevel).label }}
                </span>
                <span>{{ data.departmentName }}</span>
              </div>
              <p class="text-end col-span-1 px-2">{{ formatCurrency(data.procurementAmount) }}</p>
            </div>
            <div v-if="data.showExpand" class="divide-y divide-gray-200">
              <template v-for="(sub, subIndex) in data.childenDetail" :key="subIndex">
                <!-- level 300 row -->
                <div :class="`grid grid-cols-5 items-center divide-x divide-gray-200 py-2 ${levelRowClass(sub.orgLevel)}`">
                  <div :class="`col-span-4 flex items-center gap-1 px-2 ${levelIndent(sub.orgLevel)}`">
                    <i v-if="sub.childenDetail.length > 0" class="cursor-pointer shrink-0 text-gray-400"
                      :class="sub.showExpand ? 'pi pi-chevron-down' : 'pi pi-chevron-right'"
                      @click="toggleChild(sub)" />
                    <span v-else class="w-4 shrink-0" />
                    <span :class="`inline-flex items-center px-1.5 py-0.5 rounded text-xs mr-1 shrink-0 ${levelBadge(sub.orgLevel).cls}`">
                      {{ levelBadge(sub.orgLevel).label }}
                    </span>
                    <span>{{ sub.departmentName }}</span>
                  </div>
                  <p class="text-end col-span-1 px-2">{{ formatCurrency(sub.procurementAmount) }}</p>
                </div>
                <!-- level 400 rows -->
                <template v-if="sub.showExpand">
                  <div v-for="(dept, deptIndex) in sub.childenDetail" :key="deptIndex"
                    :class="`grid grid-cols-5 items-center divide-x divide-gray-200 py-2 ${levelRowClass(dept.orgLevel)}`">
                    <div :class="`col-span-4 flex items-center gap-1 px-2 ${levelIndent(dept.orgLevel)}`">
                      <span class="w-4 shrink-0" />
                      <span :class="`inline-flex items-center px-1.5 py-0.5 rounded text-xs mr-1 shrink-0 ${levelBadge(dept.orgLevel).cls}`">
                        {{ levelBadge(dept.orgLevel).label }}
                      </span>
                      <span>{{ dept.departmentName }}</span>
                    </div>
                    <p class="text-end col-span-1 px-2">{{ formatCurrency(dept.procurementAmount) }}</p>
                  </div>
                </template>
              </template>
            </div>
          </div>
        </div>
        <p class="text-center py-4" v-else>ไม่พบข้อมูล</p>
      </div>
    </template>
  </Card>
</template>

<style scoped lang="scss">
.header {
  text-align: center;
  font-weight: bold;
  font-size: large;
}
</style>