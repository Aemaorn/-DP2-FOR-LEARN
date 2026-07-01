<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { watchDebounced } from '@vueuse/core';
import { da004Service } from '@/services/DA/da004';
import type { Da004BarChartItem, Da004Criteria, Da004DepartmentSummary, Da004PriceSummary, Da004SpecialTypeChartItem } from '@/models/DA/da004';
import { formatCurrency } from '@/helpers/currency';
import { TitleHeader } from '@/components/cosmetic';
import { InputField, Select, CriteriaGroupButton } from '@/components/forms';
import { ButtonClear } from '@/components/Button';
import { Button, Dialog } from 'primevue';
import Chart from 'primevue/chart';
import { YearOptions, QuarterOptions } from '@/constants/date';
import { getMonthOptions } from '@/helpers/dateTime';
import { EGroupCode, OrganizationLevelEnum } from '@/enums/shared';
import SharedService from '@/services/Shared/dropdown';
import type { Option } from '@/models/shared/option';
import Pagination from '@/components/Pagination.vue';

const departmentItems = ref<Da004DepartmentSummary[]>([]);

type SummaryTab = 'winner' | 'department';
const activeTab = ref<SummaryTab>('winner');
const summaryTabOptions = [
  { label: 'สรุปผลข้อมูลประกาศผู้ชนะ', value: 'winner' },
  { label: 'สรุปผลตามฝ่าย', value: 'department' },
];

const items = ref<Da004PriceSummary[]>([]);
const totalRecords = ref(0);
const loading = ref(false);
const specialTypeChartItems = ref<Da004SpecialTypeChartItem[]>([]);
const barChartItems = ref<Da004BarChartItem[]>([]);
const supplyMethodDDL = ref<Option[]>([]);
const supplyMethodSpecialTypeDDL = ref<Option[]>([]);
const departmentDDL = ref<Option[]>([]);
const monthDDL = getMonthOptions();

const criteria = ref<Da004Criteria>({
  pageNumber: 1,
  pageSize: 10,
  keyword: '',
  budgetYear: new Date().getFullYear() + 543,
});

const summaryTotals = computed(() => {
  const s = barChartItems.value.find(i => i.period === 'ภาพรวม');
  return {
    totalBudget: s?.totalBudget ?? 0,
    totalMedianPrice: s?.totalMedianPrice ?? 0,
    totalOffered: s?.totalOfferedPrice ?? 0,
    totalAgreed: s?.totalAgreedPrice ?? 0,
  };
});

const totalProjects = computed(() => totalRecords.value);

const activeCriteriaTags = computed(() => {
  const tags: { label: string; icon: string }[] = [];

  if (criteria.value.budgetYear)
    tags.push({ label: `ปีงบประมาณ ${criteria.value.budgetYear}`, icon: 'pi-calendar' });

  if (criteria.value.departmentId) {
    const found = departmentDDL.value.find(o => o.value === criteria.value.departmentId);
    if (found) tags.push({ label: found.label, icon: 'pi-building' });
  }

  if (criteria.value.supplyMethodCode) {
    const found = supplyMethodDDL.value.find(o => o.value === criteria.value.supplyMethodCode);
    if (found) tags.push({ label: found.label, icon: 'pi-tag' });
  }

  if (criteria.value.supplyMethodSpecialTypeCode) {
    const found = supplyMethodSpecialTypeDDL.value.find(o => o.value === criteria.value.supplyMethodSpecialTypeCode);
    if (found) tags.push({ label: found.label, icon: 'pi-tag' });
  }

  if (criteria.value.quarter) {
    const q = QuarterOptions.find(o => o.value === criteria.value.quarter);
    if (q) tags.push({ label: q.label, icon: 'pi-chart-bar' });
  }

  if (criteria.value.month) {
    const m = monthDDL.find(o => o.value === criteria.value.month);
    if (m) tags.push({ label: m.label, icon: 'pi-calendar' });
  }

  if (criteria.value.keyword)
    tags.push({ label: `"${criteria.value.keyword}"`, icon: 'pi-search' });

  return tags;
});

const chartKey = ref(0);
type ChartMode = 'summary' | 'quarter' | 'month';
const chartMode = ref<ChartMode>('summary');

const selectedBarPeriod = ref<{ quarter?: number; month?: number } | null>(null);

const selectedBarLabel = computed(() => {
  if (!selectedBarPeriod.value) return null;
  if (selectedBarPeriod.value.quarter) return `ไตรมาส ${selectedBarPeriod.value.quarter}`;
  if (selectedBarPeriod.value.month) return monthLabels[(selectedBarPeriod.value.month) - 1];
  return null;
});

const loadDoughnutChartAsync = async (quarter?: number, month?: number) => {
  const mergedCriteria = { ...criteria.value, quarter, month };
  const { data, status } = await da004Service.getSpecialTypeChartAsync(mergedCriteria);
  if (status === 200 && data) {
    specialTypeChartItems.value = data;
    chartKey.value++;
  }
};

const onBarClick = (_event: any, elements: any[]) => {
  if (!elements.length || chartMode.value === 'summary') return;
  const index = elements[0].index;
  if (chartMode.value === 'quarter') {
    const q = index + 1;
    selectedBarPeriod.value = { quarter: q };
    loadDoughnutChartAsync(q, undefined);
  } else {
    const m = index + 1;
    selectedBarPeriod.value = { month: m };
    loadDoughnutChartAsync(undefined, m);
  }
};

const resetBarFilter = () => {
  selectedBarPeriod.value = null;
  loadDoughnutChartAsync(criteria.value.quarter, criteria.value.month);
};

const doughnutCenterPlugin = {
  id: 'doughnutCenter',
  afterDraw(chart: any) {
    const { ctx, chartArea: { width, height, left, top } } = chart;
    const dataset = chart.data.datasets[0];
    const amounts = dataset.amounts as number[];
    const counts = dataset.data as number[];
    const totalAmount = amounts.reduce((s: number, v: number) => s + v, 0);
    const totalCount = counts.reduce((s: number, v: number) => s + v, 0);
    if (!totalCount) return;

    const cx = left + width / 2;
    const cy = top + height / 2;

    // Total count
    ctx.save();
    ctx.font = 'bold 18px ThaiSansNeue, system-ui';
    ctx.fillStyle = '#111827';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(`${totalCount.toLocaleString('th-TH')} โครงการ`, cx, cy - 18);
    ctx.restore();

    // Label
    ctx.save();
    ctx.font = 'bold 16px ThaiSansNeue, system-ui';
    ctx.fillStyle = 'rgb(34,197,94)';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText('ราคาที่ตกลง', cx, cy + 4);
    ctx.restore();

    // Total amount
    ctx.save();
    ctx.font = '12px ThaiSansNeue, system-ui';
    ctx.fillStyle = '#6b7280';
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(totalAmount.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' บาท', cx, cy + 22);
    ctx.restore();
  },
};

const DOUGHNUT_COLORS = [
  'rgba(239,68,68,0.85)',   'rgba(6,182,212,0.85)',   'rgba(236,72,153,0.85)',
  'rgba(245,158,11,0.85)',  'rgba(99,102,241,0.85)',  'rgba(20,184,166,0.85)',
  'rgba(244,63,94,0.85)',   'rgba(14,165,233,0.85)',  'rgba(132,204,22,0.85)',
  'rgba(217,70,239,0.85)',
];
const DOUGHNUT_BORDER_COLORS = [
  'rgb(239,68,68)',   'rgb(6,182,212)',   'rgb(236,72,153)',
  'rgb(245,158,11)',  'rgb(99,102,241)',  'rgb(20,184,166)',
  'rgb(244,63,94)',   'rgb(14,165,233)',  'rgb(132,204,22)',
  'rgb(217,70,239)',
];

const doughnutChartData = computed(() => {
  const labels = specialTypeChartItems.value.map(i => i.specialTypeName);
  return {
    labels,
    datasets: [{
      data: specialTypeChartItems.value.map(i => i.projectCount),
      amounts: specialTypeChartItems.value.map(i => i.totalAgreedPrice),
      backgroundColor: labels.map((_, i) => DOUGHNUT_COLORS[i % DOUGHNUT_COLORS.length]),
      borderColor: '#ffffff',
      borderWidth: 2,
      hoverOffset: 8,
    }],
  };
});

const doughnutOuterLabelPlugin = {
  id: 'doughnutOuterLabel',
  afterDraw(chart: any) {
    const { ctx, chartArea: { width, height, left, top } } = chart;
    const cx = left + width / 2;
    const cy = top + height / 2;
    const dataset = chart.data.datasets[0];
    const meta = chart.getDatasetMeta(0);
    const total = (dataset.data as number[]).reduce((s: number, v: number) => s + v, 0);
    if (!total) return;

    meta.data.forEach((arc: any, i: number) => {
      const count = dataset.data[i] as number;
      const amount = (dataset.amounts as number[])[i];
      if (!count) return;

      const angle = (arc.startAngle + arc.endAngle) / 2;
      const outerR = arc.outerRadius;
      const lineStart = outerR + 6;
      const lineEnd = outerR + 20;
      const labelR = outerR + 28;

      const cos = Math.cos(angle);
      const sin = Math.sin(angle);
      const x1 = cx + cos * lineStart;
      const y1 = cy + sin * lineStart;
      const x2 = cx + cos * lineEnd;
      const y2 = cy + sin * lineEnd;
      const lx = cx + cos * labelR;
      const ly = cy + sin * labelR;

      const color = DOUGHNUT_BORDER_COLORS[i % DOUGHNUT_BORDER_COLORS.length];

      // Leader line
      ctx.save();
      ctx.beginPath();
      ctx.moveTo(x1, y1);
      ctx.lineTo(x2, y2);
      ctx.strokeStyle = color;
      ctx.lineWidth = 1.5;
      ctx.stroke();
      ctx.restore();

      const isRight = cos >= 0;
      const textAlign = isRight ? 'left' : 'right';
      const textX = lx + (isRight ? 4 : -4);

      const label = chart.data.labels[i] as string;
      const lineH = 18;
      const totalH = lineH * 3;
      const startY = ly - totalH / 2;

      // Label name
      ctx.save();
      ctx.font = 'bold 15px ThaiSansNeue, system-ui';
      ctx.fillStyle = color;
      ctx.textAlign = textAlign as CanvasTextAlign;
      ctx.textBaseline = 'top';
      ctx.fillText(label, textX, startY);
      ctx.restore();

      // Count
      ctx.save();
      ctx.font = 'bold 13px ThaiSansNeue, system-ui';
      ctx.fillStyle = '#1f2937';
      ctx.textAlign = textAlign as CanvasTextAlign;
      ctx.textBaseline = 'top';
      ctx.fillText(`${count} โครงการ`, textX, startY + lineH);
      ctx.restore();

      // Amount
      ctx.save();
      ctx.font = '13px ThaiSansNeue, system-ui';
      ctx.fillStyle = '#6b7280';
      ctx.textAlign = textAlign as CanvasTextAlign;
      ctx.textBaseline = 'top';
      ctx.fillText(amount.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' บาท', textX, startY + lineH * 2);
      ctx.restore();
    });
  },
};

const doughnutChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  cutout: '55%',
  layout: { padding: 70 },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(28,25,23,0.88)',
      titleColor: '#e7e5e4',
      bodyColor: '#d6d3d1',
      padding: 12,
      cornerRadius: 8,
      callbacks: {
        label: (ctx: any) => {
          const amount = (ctx.dataset.amounts as number[])[ctx.dataIndex];
          return [
            ` ${ctx.parsed} โครงการ`,
            ` ${amount.toLocaleString('th-TH', { minimumFractionDigits: 2 })} บาท`,
          ];
        },
      },
    },
  },
}));

const chartImageUrl = ref<string | null>(null);
const chartImageFileName = ref('');
const showChartImageDialog = ref(false);
const chartRef = ref();
const doughnutChartRef = ref();

const LEGEND_ITEMS = [
  { label: 'งบประมาณ',   color: 'rgb(59,130,246)' },
  { label: 'ราคากลาง',   color: 'rgb(168,85,247)' },
  { label: 'ราคาที่เสนอ', color: 'rgb(249,115,22)' },
  { label: 'ราคาที่ตกลง', color: 'rgb(34,197,94)' },
];

const exportChartAsync = async () => {
  const chartInstance = chartRef.value?.getChart();
  if (!chartInstance) return;

  const srcCanvas = chartInstance.canvas as HTMLCanvasElement;
  const modeLabel = chartMode.value === 'summary' ? 'ภาพรวม' : chartMode.value === 'quarter' ? 'รายไตรมาส' : 'รายเดือน';
  const dateStr = new Date().toISOString().slice(0, 10).replace(/-/g, '');

  // Layout constants
  const W = srcCanvas.width;
  const PAD = 24;
  const TITLE_H = 40;
  const TAG_H = activeCriteriaTags.value.length > 0 ? 32 : 0;
  const LEGEND_H = 28;
  const CHART_H = srcCanvas.height;
  const TOTAL_H = TITLE_H + TAG_H + LEGEND_H + CHART_H + PAD * 2;

  const canvas = document.createElement('canvas');
  canvas.width = W + PAD * 2;
  canvas.height = TOTAL_H;
  const ctx = canvas.getContext('2d')!;

  // Background
  ctx.fillStyle = '#ffffff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  let y = PAD;

  // Title
  ctx.fillStyle = '#374151';
  ctx.font = 'bold 15px ThaiSansNeue, system-ui, sans-serif';
  ctx.textBaseline = 'middle';
  ctx.fillText(`เปรียบเทียบผลรวมราคา — ${modeLabel}`, PAD, y + TITLE_H / 2);
  y += TITLE_H;

  // Criteria tags
  if (activeCriteriaTags.value.length > 0) {
    ctx.font = '11px ThaiSansNeue, system-ui, sans-serif';
    let x = PAD;
    for (const tag of activeCriteriaTags.value) {
      const tw = ctx.measureText(tag.label).width + 20;
      ctx.fillStyle = '#eff6ff';
      ctx.strokeStyle = '#bfdbfe';
      ctx.lineWidth = 1;
      const rx = 10;
      ctx.beginPath();
      ctx.roundRect(x, y + 4, tw, 22, rx);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = '#1d4ed8';
      ctx.fillText(tag.label, x + 10, y + 4 + 11);
      x += tw + 8;
    }
    y += TAG_H;
  }

  // Color legend
  ctx.font = '12px ThaiSansNeue, system-ui, sans-serif';
  let lx = PAD;
  for (const item of LEGEND_ITEMS) {
    ctx.fillStyle = item.color;
    ctx.fillRect(lx, y + 8, 12, 12);
    ctx.fillStyle = '#4b5563';
    ctx.fillText(item.label, lx + 16, y + 14);
    lx += ctx.measureText(item.label).width + 38;
  }
  y += LEGEND_H;

  // Chart
  ctx.drawImage(srcCanvas, PAD, y, W, CHART_H);

  // Border
  ctx.strokeStyle = '#e5e7eb';
  ctx.lineWidth = 1;
  ctx.strokeRect(0.5, 0.5, canvas.width - 1, canvas.height - 1);

  chartImageUrl.value = canvas.toDataURL('image/png');
  chartImageFileName.value = `กราฟเปรียบเทียบราคา_${modeLabel}_${dateStr}.png`;
  showChartImageDialog.value = true;
};

const downloadChartImage = () => {
  if (!chartImageUrl.value) return;
  const a = document.createElement('a');
  a.href = chartImageUrl.value;
  a.download = chartImageFileName.value;
  a.click();
};

const exportDoughnutChartAsync = async () => {
  const chartInstance = doughnutChartRef.value?.getChart();
  if (!chartInstance) return;

  const srcCanvas = chartInstance.canvas as HTMLCanvasElement;
  const dateStr = new Date().toISOString().slice(0, 10).replace(/-/g, '');

  const W = srcCanvas.width;
  const PAD = 24;
  const TITLE_H = 40;
  const TAG_H = activeCriteriaTags.value.length > 0 ? 32 : 0;
  const CHART_H = srcCanvas.height;
  const NOTE_H = 24;
  const TOTAL_H = TITLE_H + TAG_H + CHART_H + NOTE_H + PAD * 2;

  const canvas = document.createElement('canvas');
  canvas.width = W + PAD * 2;
  canvas.height = TOTAL_H;
  const ctx = canvas.getContext('2d')!;

  ctx.fillStyle = '#ffffff';
  ctx.fillRect(0, 0, canvas.width, canvas.height);

  let y = PAD;

  // Title
  ctx.fillStyle = '#374151';
  ctx.font = 'bold 15px ThaiSansNeue, system-ui, sans-serif';
  ctx.textBaseline = 'middle';
  ctx.fillText('เปรียบเทียบวิธีตามราคาที่ตกลง', PAD, y + TITLE_H / 2);
  y += TITLE_H;

  // Criteria tags
  if (activeCriteriaTags.value.length > 0) {
    ctx.font = '11px ThaiSansNeue, system-ui, sans-serif';
    let x = PAD;
    for (const tag of activeCriteriaTags.value) {
      const tw = ctx.measureText(tag.label).width + 20;
      ctx.fillStyle = '#eff6ff';
      ctx.strokeStyle = '#bfdbfe';
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.roundRect(x, y + 4, tw, 22, 10);
      ctx.fill();
      ctx.stroke();
      ctx.fillStyle = '#1d4ed8';
      ctx.fillText(tag.label, x + 10, y + 4 + 11);
      x += tw + 8;
    }
    y += TAG_H;
  }

  // Chart
  ctx.drawImage(srcCanvas, PAD, y, W, CHART_H);
  y += CHART_H;

  // Note
  ctx.font = '11px ThaiSansNeue, system-ui, sans-serif';
  ctx.fillStyle = '#9ca3af';
  ctx.textAlign = 'right';
  ctx.fillText('* จำนวนเงินแสดงเป็นราคาที่ตกลง', canvas.width - PAD, y + NOTE_H / 2);

  // Border
  ctx.strokeStyle = '#e5e7eb';
  ctx.lineWidth = 1;
  ctx.strokeRect(0.5, 0.5, canvas.width - 1, canvas.height - 1);

  chartImageUrl.value = canvas.toDataURL('image/png');
  chartImageFileName.value = `กราฟวิธีตามราคาที่ตกลง_${dateStr}.png`;
  showChartImageDialog.value = true;
};


const barColumnUnderlinePlugin = {
  id: 'barColumnUnderline',
  afterDatasetsDraw(chart: any) {
    const sel = selectedBarPeriod.value;
    if (!sel) return;

    const selectedIndex = sel.quarter != null ? sel.quarter - 1
      : sel.month != null ? sel.month - 1
      : null;
    if (selectedIndex === null) return;

    const { ctx, chartArea: { bottom } } = chart;

    // หา x ซ้ายสุดและขวาสุดจากทุก dataset ที่ index นี้
    let xMin = Infinity, xMax = -Infinity;
    for (let d = 0; d < chart.data.datasets.length; d++) {
      const meta = chart.getDatasetMeta(d);
      const bar = meta?.data?.[selectedIndex];
      if (!bar) continue;
      const hw = (bar.width ?? 20) / 2;
      xMin = Math.min(xMin, bar.x - hw);
      xMax = Math.max(xMax, bar.x + hw);
    }
    if (xMin === Infinity) return;

    const x = xMin - 6;
    const w = (xMax - xMin) + 12;
    const y = bottom + 6;

    ctx.save();
    ctx.strokeStyle = 'rgb(239,68,68)';
    ctx.lineWidth = 3;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.moveTo(x, y);
    ctx.lineTo(x + w, y);
    ctx.stroke();
    ctx.restore();
  },
};

const barValueLabelPlugin = {
  id: 'barValueLabel',
  afterDatasetsDraw(chart: any) {
    if (chart.config._config.data.__mode === 'month') return;
    const ctx = chart.ctx;
    chart.data.datasets.forEach((dataset: any, i: number) => {
      const meta = chart.getDatasetMeta(i);
      if (meta.hidden) return;
      meta.data.forEach((bar: any, j: number) => {
        const value = dataset.data[j] as number;
        if (!value) return;
        const isSummary = chart.config._config.data.__mode === 'summary';
        const label = isSummary
          ? `${value.toLocaleString('th-TH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} บาท`
          : value.toLocaleString('th-TH', { notation: 'compact', maximumFractionDigits: 1 });
        ctx.save();
        ctx.font = 'bold 13px ThaiSansNeue, system-ui';
        ctx.fillStyle = '#374151';
        ctx.textAlign = 'center';
        ctx.textBaseline = 'bottom';
        ctx.fillText(label, bar.x, bar.y - 5);
        ctx.restore();
      });
    });
  },
};

const monthLabels = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];
const quarterLabels = ['ไตรมาส 1', 'ไตรมาส 2', 'ไตรมาส 3', 'ไตรมาส 4'];

const DATASET_COLORS = {
  budget:  { bg: 'rgba(59,130,246,0.75)',  border: 'rgb(59,130,246)' },
  median:  { bg: 'rgba(168,85,247,0.75)',  border: 'rgb(168,85,247)' },
  offered: { bg: 'rgba(249,115,22,0.75)', border: 'rgb(249,115,22)' },
  agreed:  { bg: 'rgba(34,197,94,0.75)',  border: 'rgb(34,197,94)' },
};

// eslint-disable-next-line @typescript-eslint/no-unused-vars
function makeDataset(label: string, data: number[], color: { bg: string; border: string }) {
  return {
    label,
    data,
    backgroundColor: color.bg,
    borderColor: color.border,
    borderWidth: 1.5,
    borderRadius: 4,
  };
}

function makeHighlightedDataset(label: string, data: number[], color: { bg: string; border: string }, selectedIndex: number | null) {
  const bgColors = data.map((_, i) => {
    if (selectedIndex === null) return color.bg;
    return i === selectedIndex ? color.bg : color.bg.replace(/[\d.]+\)$/, '0.2)');
  });
  const borderColors = data.map((_, i) => {
    if (selectedIndex === null) return color.border;
    return i === selectedIndex ? color.border : color.border.replace('rgb', 'rgba').replace(')', ', 0.2)');
  });
  return {
    label,
    data,
    backgroundColor: bgColors,
    borderColor: borderColors,
    borderWidth: 1.5,
    borderRadius: 4,
  };
}

const barChartData = computed(() => {
  if (chartMode.value === 'summary') {
    const s = barChartItems.value.find(i => i.period === 'ภาพรวม');
    return {
      __mode: 'summary',
      labels: ['งบประมาณ', 'ราคากลาง', 'ราคาที่เสนอ', 'ราคาที่ตกลง'],
      datasets: [
        {
          label: 'ผลรวม (บาท)',
          data: [
            s?.totalBudget ?? 0,
            s?.totalMedianPrice ?? 0,
            s?.totalOfferedPrice ?? 0,
            s?.totalAgreedPrice ?? 0,
          ],
          backgroundColor: [
            DATASET_COLORS.budget.bg,
            DATASET_COLORS.median.bg,
            DATASET_COLORS.offered.bg,
            DATASET_COLORS.agreed.bg,
          ],
          borderColor: [
            DATASET_COLORS.budget.border,
            DATASET_COLORS.median.border,
            DATASET_COLORS.offered.border,
            DATASET_COLORS.agreed.border,
          ],
          borderWidth: 1.5,
          borderRadius: 6,
        },
      ],
    };
  }

  if (chartMode.value === 'quarter') {
    const labels = quarterLabels;
    const getItem = (label: string) => barChartItems.value.find(i => i.period === label);
    const sel = selectedBarPeriod.value?.quarter != null ? selectedBarPeriod.value.quarter - 1 : null;
    return {
      __mode: 'quarter',
      labels,
      datasets: [
        makeHighlightedDataset('งบประมาณ',   labels.map(l => getItem(l)?.totalBudget ?? 0),       DATASET_COLORS.budget,  sel),
        makeHighlightedDataset('ราคากลาง',   labels.map(l => getItem(l)?.totalMedianPrice ?? 0),  DATASET_COLORS.median,  sel),
        makeHighlightedDataset('ราคาที่เสนอ', labels.map(l => getItem(l)?.totalOfferedPrice ?? 0), DATASET_COLORS.offered, sel),
        makeHighlightedDataset('ราคาที่ตกลง', labels.map(l => getItem(l)?.totalAgreedPrice ?? 0),  DATASET_COLORS.agreed,  sel),
      ],
    };
  }

  // monthly
  const getItem = (label: string) => barChartItems.value.find(i => i.period === label);
  const sel = selectedBarPeriod.value?.month != null ? selectedBarPeriod.value.month - 1 : null;
  return {
    __mode: 'month',
    labels: monthLabels,
    datasets: [
      makeHighlightedDataset('งบประมาณ',   monthLabels.map(l => getItem(l)?.totalBudget ?? 0),       DATASET_COLORS.budget,  sel),
      makeHighlightedDataset('ราคากลาง',   monthLabels.map(l => getItem(l)?.totalMedianPrice ?? 0),  DATASET_COLORS.median,  sel),
      makeHighlightedDataset('ราคาที่เสนอ', monthLabels.map(l => getItem(l)?.totalOfferedPrice ?? 0), DATASET_COLORS.offered, sel),
      makeHighlightedDataset('ราคาที่ตกลง', monthLabels.map(l => getItem(l)?.totalAgreedPrice ?? 0),  DATASET_COLORS.agreed,  sel),
    ],
  };
});

const barChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  layout: { padding: { top: 28 } },
  onClick: onBarClick,
  onHover: (_event: any, elements: any[], chart: any) => {
    chart.canvas.style.cursor = (elements.length && chartMode.value !== 'summary') ? 'pointer' : 'default';
  },
  plugins: {
    legend: { display: false },
    tooltip: {
      backgroundColor: 'rgba(28,25,23,0.88)',
      titleColor: '#e7e5e4',
      bodyColor: '#d6d3d1',
      padding: 12,
      cornerRadius: 8,
      callbacks: {
        label: (ctx: any) => ` ${ctx.dataset.label}: ${ctx.parsed.y.toLocaleString('th-TH', { minimumFractionDigits: 2 })} บาท`,
      },
    },
  },
  scales: {
    x: {
      grid: { display: false },
      border: { display: false },
      ticks: { color: '#57534e', font: { size: chartMode.value === 'summary' ? 13 : 11, weight: '600' as const } },
    },
    y: {
      beginAtZero: true,
      border: { display: false },
      grid: { color: '#e7e5e4', lineWidth: 1 },
      ticks: {
        color: '#78716c',
        font: { size: 11 },
        callback: (v: number) => v.toLocaleString('th-TH', { maximumFractionDigits: 0 }),
      },
    },
  },
}));

watch(() => chartMode.value, () => {
  selectedBarPeriod.value = null;
  chartKey.value++;
});

watch(() => items.value, () => { chartKey.value++; }, { deep: true });

const loadDepartmentDDL = async () => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(OrganizationLevelEnum.Department, undefined, true);
  if (status === 200) departmentDDL.value = data;
};

const loadSupplyMethodDDL = async () => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, undefined, true);
  if (status === 200) supplyMethodDDL.value = data;
};

const loadSupplyMethodSpecialTypeDDL = async (parentCode?: string) => {
  supplyMethodSpecialTypeDDL.value = [];
  criteria.value.supplyMethodSpecialTypeCode = undefined;
  if (!parentCode) return;
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, parentCode, true);
  if (status === 200) supplyMethodSpecialTypeDDL.value = data;
};

const onGetListAsync = async () => {
  loading.value = true;
  try {
    const [listRes, barRes, doughnutRes, deptRes] = await Promise.all([
      da004Service.getListAsync(criteria.value),
      da004Service.getBarChartAsync(criteria.value),
      da004Service.getSpecialTypeChartAsync(criteria.value),
      da004Service.getDepartmentSummaryAsync(criteria.value).catch(() => ({ status: 200, data: [] })),
    ]);
    if (listRes.status === 200 && listRes.data) {
      items.value = listRes.data.data;
      totalRecords.value = listRes.data.totalRecords;
    }
    if (barRes.status === 200 && barRes.data) {
      barChartItems.value = barRes.data;
    }
    if (doughnutRes.status === 200 && doughnutRes.data) {
      specialTypeChartItems.value = doughnutRes.data;
    }
    if (deptRes.status === 200 && deptRes.data) {
      departmentItems.value = deptRes.data;
    }
  } finally {
    loading.value = false;
  }
};

const onChangePageSize = (pageNumber: number, pageSize: number) => {
  criteria.value.pageNumber = pageNumber;
  criteria.value.pageSize = pageSize;
  onGetListAsync();
};

const onSearch = () => {
  selectedBarPeriod.value = null;
  criteria.value.pageNumber = 1;
  onGetListAsync();
};

const onResetCriteria = () => {
  selectedBarPeriod.value = null;
  criteria.value = {
    pageNumber: 1,
    pageSize: 10,
    keyword: '',
    budgetYear: new Date().getFullYear() + 543,
  };
  supplyMethodSpecialTypeDDL.value = [];
  onGetListAsync();
};

const getDiffLabel = (isUnder: boolean) => isUnder ? 'ต่ำกว่า' : 'สูงกว่า';
const getDiffClass = (isUnder: boolean) => isUnder ? 'text-green-600' : 'text-red-600';

const formatDate = (dateStr?: string) => {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('th-TH', { calendar: 'buddhist', year: 'numeric', month: 'short', day: 'numeric' });
};

const exportExcelAsync = async () => {
  const isDepartmentTab = activeTab.value === 'department';
  const { data, status } = isDepartmentTab
    ? await da004Service.exportDepartmentExcelAsync(criteria.value)
    : await da004Service.exportExcelAsync(criteria.value);
  if (status !== 200) return;
  const now = new Date();
  const dateStr = `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, '0')}${String(now.getDate()).padStart(2, '0')}${String(now.getHours()).padStart(2, '0')}${String(now.getMinutes()).padStart(2, '0')}${String(now.getSeconds()).padStart(2, '0')}`;
  const fileName = isDepartmentTab
    ? `สรุปผลตามฝ่าย_${dateStr}.xlsx`
    : `สรุปผลข้อมูลประกาศผู้ชนะ_${dateStr}.xlsx`;
  const url = window.URL.createObjectURL(data);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  window.URL.revokeObjectURL(url);
  a.remove();
};

watch(() => criteria.value.supplyMethodCode, (newVal) => {
  loadSupplyMethodSpecialTypeDDL(newVal);
});

watchDebounced(
  () => [
    criteria.value.keyword,
    criteria.value.departmentId,
    criteria.value.supplyMethodCode,
    criteria.value.supplyMethodSpecialTypeCode,
    criteria.value.budgetYear,
    criteria.value.quarter,
    criteria.value.month,
  ],
  () => onSearch(),
  { debounce: 500, deep: true },
);

watch((): (number | undefined)[] => [
  criteria.value.pageNumber,
  criteria.value.pageSize,
], async (): Promise<void> => {
  await onGetListAsync();
});

onMounted(() => {
  loadDepartmentDDL();
  loadSupplyMethodDDL();
  onGetListAsync();
});
</script>

<template>
  <TitleHeader label="รายงานผลข้อมูลประกาศผู้ชนะ">
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
      <div class="mt-8 space-y-6 lg:space-y-8">
          <!-- Row 1: keyword -->
          <div class="grid grid-cols-1 lg:grid-cols-4 gap-4">
            <InputField label="คำค้นหา (ชื่อโครงการ / เลขแผน / ผู้ประกอบการ)" v-model.trim="criteria.keyword" hide-details class="lg:col-span-2" />
          </div>
          <!-- Row 2: department + supplyMethod + specialType -->
          <div class="grid grid-cols-1 lg:grid-cols-4 gap-4">
            <Select label="ฝ่าย" v-model="criteria.departmentId" :options="departmentDDL" hide-details />
            <Select label="วิธีจัดซื้อจัดจ้าง" v-model="criteria.supplyMethodCode" :options="supplyMethodDDL" hide-details />
            <Select v-model="criteria.supplyMethodSpecialTypeCode"
              :options="supplyMethodSpecialTypeDDL" hide-details
              :disabled="!criteria.supplyMethodCode || supplyMethodSpecialTypeDDL.length === 0" />
          </div>
          <!-- Row 3: budgetYear + quarter + month + clear -->
          <div class="grid grid-cols-1 lg:grid-cols-4 gap-4 items-end">
            <Select label="ปีงบประมาณ" v-model="criteria.budgetYear" :options="YearOptions" hide-details />
            <Select label="ไตรมาส (วันที่ประกาศผู้ชนะ)" v-model="criteria.quarter" :options="QuarterOptions" hide-details />
            <Select label="เดือน (วันที่ประกาศผู้ชนะ)" v-model="criteria.month" :options="monthDDL" hide-details />
            <div class="flex gap-2 justify-end">
              <ButtonClear class="w-fit" @click="onResetCriteria" />
            </div>
          </div>
        </div>
    </template>
  </Card>

  <!-- Active Criteria Tags -->
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

  <!-- Summary Cards + Chart -->
  <div v-if="items.length > 0" class="space-y-4 mb-4">
    <!-- Cards -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <Card class="bg-gray-50">
        <template #content>
          <p class="text-sm text-gray-500">จำนวนโครงการทั้งหมด</p>
          <p class="text-lg font-bold text-gray-700">{{ totalProjects.toLocaleString('th-TH') }} โครงการ</p>
        </template>
      </Card>
      <Card class="bg-blue-50">
        <template #content>
          <p class="text-sm text-gray-500">งบประมาณรวม</p>
          <p class="text-lg font-bold text-blue-700">{{ formatCurrency(summaryTotals.totalBudget) }} บาท</p>
        </template>
      </Card>
      <Card class="bg-purple-50">
        <template #content>
          <p class="text-sm text-gray-500">ราคากลางรวม</p>
          <p class="text-lg font-bold text-purple-700">{{ formatCurrency(summaryTotals.totalMedianPrice) }} บาท</p>
        </template>
      </Card>
      <Card class="bg-green-50">
        <template #content>
          <p class="text-sm text-gray-500">ราคาที่ตกลงรวม</p>
          <p class="text-lg font-bold text-green-700">{{ formatCurrency(summaryTotals.totalAgreed) }} บาท</p>
        </template>
      </Card>
    </div>
    <!-- Charts Row -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
    <!-- Bar Chart -->
    <Card class="lg:col-span-2">
      <template #content>
        <div class="flex items-center justify-between mb-2">
          <p class="text-base text-gray-600 font-semibold">เปรียบเทียบผลรวมราคา</p>
          <div class="flex items-center gap-2">
            <div class="flex gap-1 bg-gray-100 rounded-lg p-1">
            <button
              v-for="opt in ([{ value: 'summary', label: 'ภาพรวม' }, { value: 'quarter', label: 'รายไตรมาส' }, { value: 'month', label: 'รายเดือน' }] as const)"
              :key="opt.value"
              @click="chartMode = opt.value"
              :class="[
                'px-3 py-1 text-xs rounded-md font-medium transition-all',
                chartMode === opt.value
                  ? 'bg-white text-blue-600 shadow-sm'
                  : 'text-gray-500 hover:text-gray-700'
              ]"
            >{{ opt.label }}</button>
            </div>
                        <button @click="exportChartAsync"
              class="flex items-center gap-1 px-2 py-1 text-xs text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-md transition-all"
              title="บันทึกกราฟเป็นรูปภาพ">
              <i class="pi pi-download text-xs" />
              บันทึกรูป
            </button>
          </div>
        </div>
        <div style="height: 300px;">
          <Chart ref="chartRef" :key="chartKey" type="bar" :data="barChartData" :options="barChartOptions" :plugins="[barColumnUnderlinePlugin, barValueLabelPlugin]" class="w-full h-full" />
        </div>
        <!-- Color Legend -->
        <div class="flex flex-wrap justify-center gap-x-5 gap-y-1 mt-8">
          <span class="flex items-center gap-1.5 text-xs text-gray-600">
            <span class="w-3 h-3 rounded-sm flex-shrink-0" style="background:rgb(59,130,246)" />
            งบประมาณ
          </span>
          <span class="flex items-center gap-1.5 text-xs text-gray-600">
            <span class="w-3 h-3 rounded-sm flex-shrink-0" style="background:rgb(168,85,247)" />
            ราคากลาง
          </span>
          <span class="flex items-center gap-1.5 text-xs text-gray-600">
            <span class="w-3 h-3 rounded-sm flex-shrink-0" style="background:rgb(249,115,22)" />
            ราคาที่เสนอ
          </span>
          <span class="flex items-center gap-1.5 text-xs text-gray-600">
            <span class="w-3 h-3 rounded-sm flex-shrink-0" style="background:rgb(34,197,94)" />
            ราคาที่ตกลง
          </span>
        </div>
      </template>
    </Card>
    <!-- Doughnut Chart -->
    <Card class="lg:col-span-1">
      <template #content>
        <div class="flex items-center justify-between mb-3">
          <div>
            <p class="text-base text-gray-600 font-semibold">เปรียบเทียบวิธีตาม<span style="color:rgb(34,197,94)">ราคาที่ตกลง</span></p>
            <div v-if="selectedBarLabel" class="flex items-center gap-1 mt-1">
              <span class="inline-flex items-center gap-1 px-2 py-0.5 bg-blue-50 border border-blue-200 text-blue-700 rounded-full text-xs font-medium">
                <i class="pi pi-filter text-[10px]" />
                {{ selectedBarLabel }}
                <button @click="resetBarFilter" class="ml-0.5 text-blue-400 hover:text-blue-700 leading-none">
                  <i class="pi pi-times text-[10px]" />
                </button>
              </span>
            </div>
          </div>
          <button @click="exportDoughnutChartAsync"
            class="flex items-center gap-1 px-2 py-1 text-xs text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-md transition-all"
            title="บันทึกกราฟเป็นรูปภาพ">
            <i class="pi pi-download text-xs" />
            บันทึกรูป
          </button>
        </div>
        <div style="height: 400px;">
          <Chart ref="doughnutChartRef" :key="chartKey" type="doughnut" :data="doughnutChartData" :options="doughnutChartOptions" :plugins="[doughnutOuterLabelPlugin, doughnutCenterPlugin]" class="w-full h-full" />
        </div>
        <p class="text-xs text-gray-400 mt-2 text-right">* จำนวนเงินแสดงเป็นราคาที่ตกลง</p>
      </template>
    </Card>
    </div>
  </div>

  <Card>
    <template #content>
      <div class="flex items-center justify-between mb-4">
        <CriteriaGroupButton v-model="activeTab" :options="summaryTabOptions" class="mb-0!" />
        <Button label="พิมพ์รายงาน" icon="pi pi-file-export" severity="primary" variant="outlined"
          class="bg-white! hover:bg-red-50!" @click="exportExcelAsync" />
      </div>
      <div v-if="activeTab === 'winner'">
      <!-- Table -->
      <div class="border border-gray-200 rounded-lg overflow-x-auto">
        <!-- Table Header -->
        <div class="grid bg-gray-50 divide-x divide-gray-200 border-b border-gray-200 sticky top-0 z-10 min-w-[2200px]"
          style="grid-template-columns: 80px 1fr 120px 120px 1fr 120px 140px 130px 140px 130px 140px 130px 160px 250px;">
          <div class="header flex items-center justify-center py-2 px-2 sticky left-0 z-20 bg-gray-50 border-r border-gray-200">เลขที่</div>
          <div class="header flex items-center justify-center py-2 px-2 sticky left-[80px] z-20 bg-gray-50" style="box-shadow: 4px 0 6px -2px rgba(0,0,0,0.08);">ชื่อโครงการ</div>
          <div class="header flex items-center justify-center py-2 px-2">วิธีจัดซื้อ/จ้าง/เช่า</div>
          <div class="header flex items-center justify-center py-2 px-2">วิธี</div>
          <div class="header flex items-center justify-center py-2 px-2">ผู้ประกอบการ</div>
          <div class="header flex items-center justify-center py-2 px-2">วันที่อนุมัติ</div>
          <div class="header flex items-center justify-center py-2 px-2">งบประมาณ</div>
          <div class="header flex items-center justify-center py-2 px-2">ราคากลาง</div>
          <div class="header flex items-center justify-center py-2 px-2">ราคารวมที่เสนอ</div>
          <div class="header flex items-center justify-center py-2 px-2">ราคารวมที่ตกลง</div>
          <div class="header flex items-center justify-center py-2 px-2">เทียบงบประมาณ</div>
          <div class="header flex items-center justify-center py-2 px-2">เทียบราคากลาง</div>
          <div class="header flex items-center justify-center py-2 px-2">เทียบราคาที่เสนอ</div>
          <div class="header flex items-center justify-center py-5 px-5">เหตุผลการคัดเลือก / หมายเหตุ</div>
        </div>
        <!-- Table Body -->
        <div v-if="items.length > 0" class="divide-y divide-gray-200">
          <div v-for="(data, index) in items" :key="data.jp006Id ?? index"
            class="grid items-center divide-x divide-gray-200 py-4 min-w-[2200px] bg-white"
            style="grid-template-columns: 80px 1fr 120px 120px 1fr 120px 140px 130px 140px 130px 140px 130px 160px 250px;">
            <span class="px-2 text-sm font-bold sticky left-0 z-10 bg-white self-stretch flex items-center border-r border-gray-200">{{ data.procurementNumber }}</span>
            <span class="px-2 text-sm sticky left-[80px] z-10 bg-white self-stretch flex items-center" style="box-shadow: 4px 0 6px -2px rgba(0,0,0,0.08);">{{ data.projectName }}</span>
            <span class="px-2 text-sm">{{ data.supplyMethodName }}</span>
            <span class="px-2 text-sm">{{ data.supplyMethodSpecialTypeName ?? '-' }}</span>
            <span class="px-2 text-sm">{{ data.vendorName }}</span>
            <span class="px-2 text-sm text-center">{{ formatDate(data.approvedDate) }}</span>
            <span class="px-2 text-sm text-end font-bold">{{ formatCurrency(data.budget) }}</span>
            <span class="px-2 text-sm text-end font-bold">{{ formatCurrency(data.medianPrice) }}</span>
            <span class="px-2 text-sm text-end font-bold">{{ formatCurrency(data.totalOfferedPrice) }}</span>
            <span class="px-2 text-sm text-end font-bold">{{ formatCurrency(data.totalAgreedPrice) }}</span>
            <div class="px-2 text-sm">
              <div v-if="data.budget > 0">
                <span :class="getDiffClass(data.isUnderBudget)">
                  {{ getDiffLabel(data.isUnderBudget) }} {{ formatCurrency(data.budgetDiff) }}
                </span>
                <br />
                <small class="text-gray-500">ร้อยละ {{ data.budgetDiffPercent }}%</small>
              </div>
              <span v-else class="text-gray-400">-</span>
            </div>            
            <div class="px-2 text-sm">
              <div v-if="data.medianPrice > 0">
                <span :class="getDiffClass(data.isUnderMedianPrice)">
                  {{ getDiffLabel(data.isUnderMedianPrice) }} {{ formatCurrency(data.medianPriceDiff) }}
                </span>
                <br />
                <small class="text-gray-500">ร้อยละ {{ data.medianPriceDiffPercent }}%</small>
              </div>
              <span v-else class="text-gray-400">-</span>
            </div>            
            <div class="px-2 text-sm">
              <div v-if="data.totalOfferedPrice > 0">
                <span :class="getDiffClass(data.isUnderOfferedPrice)">
                  {{ getDiffLabel(data.isUnderOfferedPrice) }} {{ formatCurrency(data.offeredPriceDiff) }}
                </span>
                <br />
                <small class="text-gray-500">ร้อยละ {{ data.offeredPriceDiffPercent }}%</small>
              </div>
              <span v-else class="text-gray-400">-</span>
            </div>
            <div class="px-2 text-sm">
              <span v-if="data.selectionReasonName" class="text-gray-700">{{ data.selectionReasonName }}</span>
              <span v-if="data.remark" class="text-gray-500 mt-0.5">{{ data.remark }}</span>
              <span v-if="!data.selectionReasonName && !data.remark" class="text-gray-400">-</span>
            </div>
          </div>
        </div>
        <p class="text-center py-4" v-else>ไม่พบข้อมูล</p>
      </div>
      <Pagination :page-number="criteria.pageNumber" :page-size="criteria.pageSize"
        :totalRecord="totalRecords" @change="onChangePageSize" />
      </div>
      <div v-else-if="activeTab === 'department'">
        <div class="border border-gray-200 rounded-lg overflow-x-auto">
          <div class="grid bg-gray-50 divide-x divide-gray-200 border-b border-gray-200"
            style="grid-template-columns: minmax(200px, 2fr) repeat(4, minmax(140px, 1fr));">
            <div class="header flex items-center justify-center py-2 px-2">ฝ่าย</div>
            <div class="header flex items-center justify-center py-2 px-2">จำนวนโครงการ</div>
            <div class="header flex items-center justify-center py-2 px-2">งบประมาณรวม</div>
            <div class="header flex items-center justify-center py-2 px-2">ราคากลางรวม</div>
            <div class="header flex items-center justify-center py-2 px-2">ราคาที่ตกลงรวม</div>
          </div>
          <div v-if="departmentItems.length > 0" class="divide-y divide-gray-200">
            <div v-for="row in departmentItems" :key="row.departmentName"
              class="grid items-center divide-x divide-gray-200 py-3"
              style="grid-template-columns: minmax(200px, 2fr) repeat(4, minmax(140px, 1fr));">
              <span class="px-3 text-base">{{ row.departmentName }}</span>
              <span class="px-3 text-base text-center font-bold">{{ row.projectCount }}</span>
              <span class="px-3 text-base text-end font-bold">{{ formatCurrency(row.totalBudget) }}</span>
              <span class="px-3 text-base text-end font-bold">{{ formatCurrency(row.totalMedianPrice) }}</span>
              <span class="px-3 text-base text-end font-bold">{{ formatCurrency(row.totalAgreedPrice) }}</span>
            </div>
          </div>
          <p class="text-center py-4" v-else>ไม่พบข้อมูล</p>
        </div>
      </div>
    </template>
  </Card>

  <!-- Chart Image Dialog -->
  <Dialog v-model:visible="showChartImageDialog" modal header="กราฟเปรียบเทียบผลรวมราคา" :style="{ width: '80vw' }">
    <img v-if="chartImageUrl" :src="chartImageUrl" alt="chart" class="w-full rounded-lg border border-gray-200" />
    <template #footer>
      <Button label="ดาวน์โหลด" icon="pi pi-download" @click="downloadChartImage" />
      <Button label="ปิด" icon="pi pi-times" severity="secondary" variant="outlined" @click="showChartImageDialog = false" />
    </template>
  </Dialog>
</template>

<style scoped>
.header {
  text-align: center;
  font-weight: bold;
  font-size: large;
}
</style>
