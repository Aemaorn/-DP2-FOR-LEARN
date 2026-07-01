<script setup lang="ts">
import { InputField } from '@/components/forms';
import { useSearchAllStore } from '@/stores/searchAll';
import type { TWorkflowStep } from '@/models/searchCriteria';
import { Button, Card } from 'primevue';
import Chart from 'primevue/chart';
import { Form } from 'vee-validate';
import { useRouter } from 'vue-router';
import { formatCurrency } from '@/helpers/currency';
import { computed, onMounted, nextTick, ref } from 'vue';
import logoText from '@/assets/images/logo-text.png';

const store = useSearchAllStore();
const router = useRouter();
const searchWrapperRef = ref<HTMLDivElement | null>(null);
const hasSearched = ref(false);
const isChartOpen = ref(false);

onMounted(async () => {
  await nextTick();
  searchWrapperRef.value?.querySelector('input')?.focus();
});

const handleSearch = async () => {
  if (!store.searchCriteria.searchText) return;
  await store.searchAllAsync();
  hasSearched.value = true;
  await nextTick();
  searchWrapperRef.value?.querySelector('input')?.focus();
};

const resetToDefault = () => {
  hasSearched.value = false;
  store.searchCriteria.searchText = undefined;
  store.dateTableItems.data = [];
  store.dateTableItems.totalRecords = 0;
  nextTick(() => {
    searchWrapperRef.value?.querySelector('input')?.focus();
  });
};

const openInNewTab = (url: string) => {
  const resolved = router.resolve(url);
  window.open(resolved.href, '_blank');
};

type GroupedStep = {
  name: string;
  entries: { refNumber: string; url: string }[];
};

function groupSteps(steps: TWorkflowStep[]): GroupedStep[] {
  const groups: GroupedStep[] = [];
  for (const step of steps) {
    const last = groups[groups.length - 1];
    if (last && last.name === step.name) {
      last.entries.push({ refNumber: step.refNumber, url: step.url });
    } else {
      groups.push({ name: step.name, entries: [{ refNumber: step.refNumber, url: step.url }] });
    }
  }
  return groups;
}

const PIE_COLORS = ['#60a5fa', '#34d399', '#fbbf24', '#f87171', '#a78bfa', '#fb923c', '#22d3ee', '#f472b6', '#94a3b8'];

type GroupByKey = 'supplyMethodTypeName' | 'supplyMethodSpecialTypeName';

const groupByOptions = [
  { label: 'วิธีจัดหา (Type)', value: 'supplyMethodTypeName' as GroupByKey },
  { label: 'ประเภทพิเศษ (Special Type)', value: 'supplyMethodSpecialTypeName' as GroupByKey },
];

const groupBy = ref<GroupByKey>('supplyMethodTypeName');

const groupByLabel = computed(() =>
  groupByOptions.find(o => o.value === groupBy.value)?.label ?? '');

const budgetBySupplyType = computed(() => {
  const map = new Map<string, number>();
  for (const item of store.dateTableItems.data) {
    const key = item[groupBy.value]?.trim();
    if (!key) continue;
    map.set(key, (map.get(key) ?? 0) + (item.budget ?? 0));
  }
  return Array.from(map.entries())
    .map(([name, total]) => ({ name, total }))
    .sort((a, b) => b.total - a.total);
});

const totalBudget = computed(() => budgetBySupplyType.value.reduce((s, x) => s + x.total, 0));

const pieChartData = computed(() => ({
  labels: budgetBySupplyType.value.map(x => x.name),
  datasets: [{
    data: budgetBySupplyType.value.map(x => x.total),
    backgroundColor: budgetBySupplyType.value.map((_, i) => PIE_COLORS[i % PIE_COLORS.length]),
    borderColor: '#ffffff',
    borderWidth: 2,
    hoverOffset: 6,
  }],
}));

const pieChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(28,25,23,0.88)',
      titleColor: '#e7e5e4',
      bodyColor: '#d6d3d1',
      padding: 12,
      cornerRadius: 8,
      callbacks: {
        label: (ctx: any) => ` ${formatCurrency(ctx.parsed)}`,
      },
    },
  },
}));
</script>

<template>
  <div :class="['h-full', hasSearched ? 'bg-white' : '']">
      <!-- Default state: centered layout -->
      <div v-if="!hasSearched" class="text-center flex flex-col items-center justify-center min-h-[calc(100vh-8rem)] pb-32">
        <!-- Logo: Clear Space ระบุใน padding ของ wrapper (PNG trim แล้ว ไม่มี transparent border ในตัว) -->
        <div class="px-3 py-2 mt-4">
          <img :src="logoText" width="248" alt="ธอส. - ธนาคารอาคารสงเคราะห์" />
        </div>
        <h2 class="text-4xl font-bold mb-2">ค้นหาข้อมูลการจัดซื้อจัดจ้าง</h2>
        <Form @submit="handleSearch" class="w-1/2">
          <div ref="searchWrapperRef">
            <InputField v-model.trim="store.searchCriteria.searchText" class="w-full mx-auto mt-8"
              placeholder="ค้นหาข้อมูล">
              <template #appendAction>
                <InputGroupAddon>
                  <Button icon="pi pi-search" type="submit" severity="primary"
                    class="w-full h-full border-none! rounded-none!" />
                </InputGroupAddon>
              </template>
            </InputField>
          </div>
        </Form>
      </div>

      <!-- Post-search state: compact header -->
      <template v-else>
        <div class="flex items-center gap-4 pt-4 pb-1 border-b border-gray-200 sticky top-[60px] bg-white z-50 px-6 shadow-sm -mx-4 -mt-6">
          <!-- Logo: Clear Space ระบุใน padding ของ wrapper (PNG trim แล้ว) -->
          <div class="p-3 cursor-pointer shrink-0" @click="resetToDefault" title="กลับสู่หน้าค้นหา">
            <img :src="logoText" width="124" alt="ธอส. - ธนาคารอาคารสงเคราะห์" />
          </div>
          <Form @submit="handleSearch" class="flex-1 max-w-3xl">
            <div ref="searchWrapperRef">
              <InputField v-model.trim="store.searchCriteria.searchText" placeholder="ค้นหาข้อมูล" hide-details>
                <template #appendAction>
                  <InputGroupAddon>
                    <Button icon="pi pi-search" type="submit" severity="primary"
                      class="w-full h-full border-none! rounded-none!" />
                  </InputGroupAddon>
                </template>
              </InputField>
            </div>
          </Form>
        </div>

        <div class="grid lg:grid-cols-12 gap-4 mt-3 px-6 -mx-4">
          <!-- Left: Results list -->
          <Card :pt="{ body: { class: 'p-0!' }, content: { class: 'p-0!' } }"
            :class="['transition-all duration-300', isChartOpen ? 'lg:col-span-9' : 'lg:col-span-12']">
            <template #content>
              <div>
          <p v-if="store.dateTableItems.totalRecords > 0" class="text-sm text-gray-400 px-2">
            พบ {{ store.dateTableItems.totalRecords.toLocaleString() }} รายการ
          </p>

          <div v-for="(item, index) in store.dateTableItems.data" :key="item.id"
            :class="['px-2 py-4', index < store.dateTableItems.data.length - 1 ? 'border-b border-gray-100' : '', index % 2 === 0 ? 'bg-white' : 'bg-gray-50']">
            <div class="grid lg:grid-cols-12">
              <!-- Left: Workflow Steps -->
              <div class="lg:col-span-2 border-r border-gray-100 pr-4">
                <div v-for="(group, groupIndex) in groupSteps(item.steps)" :key="groupIndex" class="flex gap-3 mb-1">
                  <div class="flex flex-col items-center">
                    <div :class="['w-5 h-5 rounded-full flex items-center justify-center text-sm font-semibold shrink-0 mt-1',
                      group.entries.some(e => e.refNumber) ? 'bg-blue-100 text-blue-500' : 'bg-gray-100 text-gray-400']">
                      {{ groupIndex + 1 }}
                    </div>
                    <div v-if="groupIndex < groupSteps(item.steps).length - 1"
                      :class="['w-px grow my-1', group.entries.some(e => e.refNumber) ? 'bg-blue-200' : 'bg-gray-200']"></div>
                  </div>
                  <div class="min-w-0">
                    <span class="text-sm text-gray-400">{{ group.name }}</span>
                    <div v-for="(entry, entryIndex) in group.entries" :key="entryIndex">
                      <span v-if="entry.url" class="underline text-blue-500 font-bold cursor-pointer text-sm"
                        @click="openInNewTab(entry.url)">
                        {{ entry.refNumber }}
                      </span>
                      <span v-else class="text-sm text-gray-400">{{ entry.refNumber || '-' }}</span>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Right: Google-style -->
              <div class="lg:col-span-10 pl-6">
                <h3 class="text-lg text-gray-900! font-medium leading-snug mb-2 break-words">
                  {{ item.programName || '-' }}
                </h3>
                <div class="flex flex-col gap-y-1 text-base">
                  <span>
                    <span class="text-gray-400">วงเงิน</span>
                    <span class="ml-1 font-medium text-gray-700">{{ formatCurrency(item.budget ?? 0) }}</span>
                  </span>
                  <span v-if="item.departmentName">
                    <span class="text-gray-400">ฝ่าย/ภาคเขต</span>
                    <span class="ml-1 font-medium text-gray-700">{{ item.departmentName }}</span>
                  </span>
                  <span v-if="item.budgetYear">
                    <span class="text-gray-400">ปีงบประมาณ</span>
                    <span class="ml-1 font-medium text-gray-700">{{ item.budgetYear }}</span>
                  </span>
                  <span v-if="item.supplyMethodName">
                    <span class="text-gray-400">วิธีจัดหา</span>
                    <span class="ml-1 font-medium text-gray-700">{{ [item.supplyMethodName, item.supplyMethodTypeName, item.supplyMethodSpecialTypeName].filter(Boolean).join(' : ') }}</span>
                  </span>
                  <span v-if="item.vendorName">
                    <span class="text-gray-400">คู่ค้า</span>
                    <span class="ml-1 font-medium text-gray-700">{{ item.vendorName }}</span>
                  </span>
                </div>
              </div>
            </div>
          </div>

          <div class="text-center py-12 text-gray-400" v-if="store.dateTableItems.data.length === 0 && store.dateTableItems.totalRecords === 0">
            <i class="pi pi-search text-3xl mb-3 block opacity-30" />
            <p>ไม่พบข้อมูลที่ค้นหา</p>
          </div>
          </div>
          </template>
        </Card>

          <!-- Right: Summary pie chart -->
          <Transition name="chart-slide">
            <aside v-if="isChartOpen" class="lg:col-span-3">
              <Card :pt="{ body: { class: 'p-4!' }, content: { class: 'p-0!' } }" class="sticky top-[125px] relative">
                <template #content>
                  <div class="sticky top-0 -mx-4 -mt-4 px-4 pt-4 pb-2 bg-white z-10 border-b border-gray-100">
                    <button type="button" @click="isChartOpen = false"
                      class="absolute top-2 right-2 w-7 h-7 flex items-center justify-center rounded-full text-gray-400 hover:bg-gray-100 hover:text-gray-600 cursor-pointer"
                      title="ซ่อนกราฟ">
                      <i class="pi pi-angle-right text-sm" />
                    </button>

                    <p class="text-sm font-semibold text-gray-700 mb-1 pr-8">สรุปงบประมาณตาม{{ groupByLabel }}</p>
                    <p class="text-xs text-gray-400 mb-2">รวม {{ formatCurrency(totalBudget) }}</p>

                    <div class="flex gap-1 p-1 bg-gray-100 rounded-lg">
                      <button v-for="opt in groupByOptions" :key="opt.value" type="button"
                        @click="groupBy = opt.value"
                        :class="['flex-1 text-xs px-2 py-1.5 rounded-md transition-colors cursor-pointer',
                          groupBy === opt.value
                            ? 'bg-white text-blue-600 font-semibold shadow-sm'
                            : 'text-gray-500 hover:text-gray-700']">
                        {{ opt.label }}
                      </button>
                    </div>
                  </div>

                  <div class="pt-3"></div>

                  <div v-if="budgetBySupplyType.length > 0" style="height: 220px;">
                    <Chart type="pie" :data="pieChartData" :options="pieChartOptions" class="w-full h-full" />
                  </div>
                  <div v-else class="text-center py-6 text-xs text-gray-400">
                    ไม่มีข้อมูล
                  </div>

                  <ul v-if="budgetBySupplyType.length > 0" class="mt-4 space-y-2">
                    <li v-for="(entry, i) in budgetBySupplyType" :key="entry.name" class="flex items-start gap-2 text-xs">
                      <span class="inline-block w-2.5 h-2.5 rounded-sm shrink-0 mt-1"
                        :style="{ backgroundColor: PIE_COLORS[i % PIE_COLORS.length] }"></span>
                      <div class="flex-1 min-w-0">
                        <div class="text-gray-700 truncate">{{ entry.name }}</div>
                        <div class="text-gray-500">{{ formatCurrency(entry.total) }}</div>
                      </div>
                    </li>
                  </ul>
                </template>
              </Card>
            </aside>
          </Transition>
        </div>

        <!-- Floating reopen button -->
        <button v-if="!isChartOpen" type="button" @click="isChartOpen = true"
          class="fixed right-0 top-[200px] z-40 bg-white border border-r-0 border-gray-200 rounded-l-lg shadow-md px-2 py-3 flex flex-col items-center gap-1 text-gray-500 hover:text-blue-500 hover:bg-blue-50 cursor-pointer"
          title="แสดงกราฟ">
          <i class="pi pi-chart-pie text-base" />
          <i class="pi pi-angle-left text-xs" />
        </button>
      </template>
  </div>
</template>

<style scoped>
.chart-slide-enter-active,
.chart-slide-leave-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.chart-slide-enter-from,
.chart-slide-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
