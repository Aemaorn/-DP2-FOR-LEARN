import { defineStore } from 'pinia';
import { ref } from 'vue';
import { HttpStatusCode } from 'axios';
import * as XLSX from 'xlsx';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TAnnouncementInfoCriteria,
  TAnnouncementInfoSupplyMethodCount,
  TAnnouncementInfoList,
  TAnnouncementInfoImportRow,
  TAnnouncementInfoBody,
} from '@/models/ANN/ann001';
import AnnouncementInfoService from '@/services/ANN/ann001';
import SharedService from '@/services/Shared/dropdown';
import type { Option, OptionBadge } from '@/models/shared/option';
import { EGroupCode } from '@/enums/shared';
import ToastHelper from '@/helpers/toast';

const ALL_CODE = 'All';

const buildSupplyMethodBadges = (
  options: Option[],
  allCount: number,
  counts: TAnnouncementInfoSupplyMethodCount[]
): OptionBadge[] => {
  const badges: OptionBadge[] = [
    {
      label: 'ทั้งหมด',
      value: ALL_CODE,
      count: allCount,
      bgColorClass: 'bg-[#FAFAFA]',
      textColorClass: 'text-black',
    },
  ];

  const colors = [
    { bgColorClass: 'bg-[#60A5FA]', textColorClass: 'text-white' },
    { bgColorClass: 'bg-[#34D399]', textColorClass: 'text-white' },
    { bgColorClass: 'bg-[#F59E0B]', textColorClass: 'text-white' },
    { bgColorClass: 'bg-[#A78BFA]', textColorClass: 'text-white' },
    { bgColorClass: 'bg-[#F87171]', textColorClass: 'text-white' },
  ];

  options.forEach((opt, index) => {
    const code = String(opt.value);
    const color = colors[index % colors.length];
    const found = counts.find((c) => c.code === code);
    badges.push({
      label: opt.label,
      value: code,
      count: found?.count ?? 0,
      bgColorClass: color.bgColorClass,
      textColorClass: color.textColorClass,
    });
  });

  return badges;
};

export const useAnnouncementInfoListStore = defineStore('announcement-info-list-store', () => {
  const initCriteria: TAnnouncementInfoCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
    supplyMethodCode: ALL_CODE,
  };

  const criteria = ref<TAnnouncementInfoCriteria>({ ...initCriteria });

  const table = ref<TDataTableResult<TAnnouncementInfoList>>({
    data: [],
    totalRecords: 0,
  });

  const supplyMethodOptions = ref<Option[]>([]);
  const announcementCategoryOptions = ref<Option[]>([]);
  const supplyMethodBadges = ref<OptionBadge[]>([]);

  const onGetListAsync = async (): Promise<void> => {
    const params = {
      ...criteria.value,
      supplyMethodCode:
        criteria.value.supplyMethodCode === ALL_CODE
          ? undefined
          : criteria.value.supplyMethodCode,
    };

    const { data, status } = await AnnouncementInfoService.onGetListAsync(params);
    if (status === HttpStatusCode.Ok) {
      table.value = data.data;
      supplyMethodBadges.value = buildSupplyMethodBadges(supplyMethodOptions.value, data.allCount ?? 0, data.counts ?? []);
    }
  };

  const onGetSupplyMethodOptionsAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.SMethod, undefined, true);
    if (status === HttpStatusCode.Ok) {
      supplyMethodOptions.value = data;
    }
  };

  const onGetAnnouncementCategoryOptionsAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AnnCategory);
    if (status === HttpStatusCode.Ok) {
      announcementCategoryOptions.value = data;
    }
  };

  const getSupplyMethodName = (code?: string): string => {
    if (!code) return '';
    const option = supplyMethodOptions.value.find((o) => o.value === code);
    return option ? option.label : code;
  };

  const getCategoryName = (code?: string): string => {
    if (!code) return '';
    const option = announcementCategoryOptions.value.find((o) => o.value === code);
    return option ? option.label : code;
  };

  const onResetCriteria = (): void => {
    criteria.value = { ...initCriteria };
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    criteria.value = { ...criteria.value, pageNumber, pageSize };
  };

  const onDeleteAsync = async (id: string): Promise<boolean> => {
    const { status } = await AnnouncementInfoService.deleteAsync(id);
    if (status === HttpStatusCode.NoContent) {
      ToastHelper.success('ลบสำเร็จ', 'ลบข้อมูลประกาศเรียบร้อยแล้ว');
      await onGetListAsync();
      return true;
    }
    return false;
  };

  const isImporting = ref(false);

  const COLUMN_MAP: Partial<Record<keyof TAnnouncementInfoImportRow, string>> = {
    oldId: 'OldId',
    announcementName: 'ประกาศ',
    status: 'สถานะ',
    announcementDate: 'วันที่เผยแพร่',
    lastModifiedAt: 'วันที่แก้ไข',
    createdBy: 'ชื่อคนสร้าง',
    announcementCategoryCode: 'ประเภทประกาศ',
    description: 'รายละเอียด',
    supplyMethodCode: 'อ้างอิง',
    budgetAmount: 'วงเงินงบประมาณ',
    announcementTitle: 'หัวข้อประกาศ',
    email: 'อีเมล',
    documentUrl: 'ไฟล์แนบ',
    referencePrice: 'ราคากลางอ้างอิง',
    expectedDate: 'คาดว่าจะประกาศ(เดือน/ปี)',
    budgetYear: 'ปีงบประมาณ',
    startDate: 'วันที่เริ่มต้นประชาพิจารณ์',
    endDate: 'วันที่สิ้นสุดประชาพิจารณ์',
  };

  const INT_FIELDS = new Set<keyof TAnnouncementInfoImportRow>([
    'oldId', 'expectedDate', 'budgetYear',
  ]);
  const DECIMAL_FIELDS = new Set<keyof TAnnouncementInfoImportRow>([
    'budgetAmount', 'referencePrice',
  ]);
  const DATE_FIELDS = new Set<keyof TAnnouncementInfoImportRow>([
    'announcementDate', 'lastModifiedAt', 'startDate', 'endDate',
  ]);

  const toDateString = (val: unknown): string | undefined => {
    if (!val) return undefined;
    if (val instanceof Date) return val.toISOString();
    const str = String(val).trim();
    return str || undefined;
  };

  const parseDecimal = (val: unknown): number => {
    if (typeof val === 'number') return val;
    const cleaned = String(val).replace(/,/g, '');
    const match = cleaned.match(/-?\d+(\.\d+)?/);
    return match ? parseFloat(match[0]) : NaN;
  };

  const onImportAsync = async (file: File): Promise<void> => {
    isImporting.value = true;
    try {
      const isCsv = file.name.toLowerCase().endsWith('.csv');
      const wb = isCsv
        ? XLSX.read(await file.text(), { type: 'string' })
        : XLSX.read(await file.arrayBuffer(), { type: 'array', cellDates: true });
      const ws = wb.Sheets[wb.SheetNames[0]];
      const raw = XLSX.utils.sheet_to_json<Record<string, unknown>>(ws, { defval: undefined });

      type RowValidationError = { rowIndex: number; announcementName?: string; errors: string[] };

      const validateRow = (row: TAnnouncementInfoImportRow): string[] => {
        const errs: string[] = [];
        if (!row.announcementName?.trim()) errs.push('ประกาศ: ไม่พบข้อมูล');
        if (!row.announcementDate) errs.push('วันที่เผยแพร่: ไม่พบข้อมูล');
        if (!row.announcementCategoryCode?.trim()) errs.push('ประเภทประกาศ: ไม่พบข้อมูล');
        if (!row.supplyMethodCode?.trim()) errs.push('อ้างอิง: ไม่พบข้อมูล');
        return errs;
      };

      const validRows: TAnnouncementInfoImportRow[] = [];
      const clientFailedRows: RowValidationError[] = [];

      await Promise.all(
        raw.map(async (r, idx) => {
          const row: TAnnouncementInfoImportRow = {};
          for (const [field, thCol] of Object.entries(COLUMN_MAP)) {
            const typedField = field as keyof TAnnouncementInfoImportRow;
            const val = r[thCol];
            if (val === undefined || val === null || val === '') continue;
            if (INT_FIELDS.has(typedField) || DECIMAL_FIELDS.has(typedField)) {
              if (val instanceof Date) continue;
              const n = DECIMAL_FIELDS.has(typedField) ? parseDecimal(val) : Number(val);
              if (!isNaN(n)) (row as Record<string, unknown>)[field] = INT_FIELDS.has(typedField) ? Math.trunc(n) : n;
            } else if (DATE_FIELDS.has(typedField)) {
              const ds = toDateString(val);
              if (ds) (row as Record<string, unknown>)[field] = ds;
            } else {
              (row as Record<string, unknown>)[field] = String(val);
            }
          }

          const errs = validateRow(row);
          if (errs.length > 0) {
            clientFailedRows.push({ rowIndex: idx + 2, announcementName: row.announcementName, errors: errs });
          } else {
            validRows.push(row);
          }
        })
      );

      if (validRows.length === 0 && clientFailedRows.length > 0) {
        const summary = clientFailedRows
          .slice(0, 5)
          .map((f) => `แถว ${f.rowIndex}: ${f.errors.join(', ')}`)
          .join('\n');
        ToastHelper.error(
          `ข้อมูลไม่ถูกต้อง (${clientFailedRows.length} แถวมีข้อผิดพลาด)`,
          summary
        );
        return;
      }

      const { data, status } = await AnnouncementInfoService.importAsync(validRows);
      if (status === HttpStatusCode.Ok) {
        const hardFailed = clientFailedRows.length + data.failedCount;

        const parts: string[] = [`นำเข้าสำเร็จ ${data.successCount} รายการ`];
        if (hardFailed > 0) parts.push(`ล้มเหลว ${hardFailed} รายการ`);

        const toastFn = hardFailed > 0 ? ToastHelper.error : ToastHelper.success;
        toastFn('นำเข้าข้อมูล', parts.join(', '));
        await onGetListAsync();
      }
    } catch {
      ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถนำเข้าข้อมูลได้');
    } finally {
      isImporting.value = false;
    }
  };

  const onDownloadTemplate = (): void => {
    const headers = Object.values(COLUMN_MAP) as string[];
    const categories = announcementCategoryOptions.value.length > 0
      ? announcementCategoryOptions.value.map((o) => o.label)
      : ['ประกาศแผนการจัดซื้อจัดจ้าง'];
    const sampleRows = categories.map((category, idx): (string | number)[] => [
      idx + 1,
      'ประกาศจัดซื้อคอมพิวเตอร์',
      'publish',
      '4/29/2026',
      '4/29/2026',
      'สมชาย ใจดี',
      category,
      'รายละเอียดการจัดซื้อ',
      '80',
      500000.80,
      'หัวข้อประกาศจัดซื้อ',
      'somchai@example.com',
      'http://info.ghbank.co.th/assets/uploads/2026/04/1777448751_59f951d3afa4fbf6439d1642bbe12336.pdf',
      450000,
      2,
      2569,
      '4/29/2026',
      '5/29/2026',
    ]);
    const ws = XLSX.utils.aoa_to_sheet([headers, ...sampleRows]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Template');
    XLSX.writeFile(wb, 'Announcement_Template.xlsx');
  };

  return {
    criteria,
    table,
    supplyMethodOptions,
    announcementCategoryOptions,
    supplyMethodBadges,
    isImporting,
    onGetListAsync,
    onGetSupplyMethodOptionsAsync,
    onGetAnnouncementCategoryOptionsAsync,
    getSupplyMethodName,
    getCategoryName,
    onResetCriteria,
    onChangePageSize,
    onImportAsync,
    onDeleteAsync,
    onDownloadTemplate,
  };
});

export const useAnnouncementInfoDetailStore = defineStore('announcement-info-detail-store', () => {
  const initBody: TAnnouncementInfoBody = {
    announcementTitle: '',
    announcementName: '',
    announcementDate: undefined,
    budgetAmount: undefined,
    announcementCategoryCode: '',
    supplyMethodCode: '',
    budgetYear: undefined,
    annotation: '',
    description: '',
    expectedDate: undefined,
    referencePrice: undefined,
    startDate: undefined,
    endDate: undefined,
    documentInfo: undefined,
  };

  const body = ref<TAnnouncementInfoBody>({ ...initBody });

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await AnnouncementInfoService.getByIdAsync(id);
    if (status === HttpStatusCode.Ok) {
      body.value = {
        announcementTitle: data.announcementTitle ?? '',
        announcementName: data.announcementName,
        announcementDate: data.announcementDate ? new Date(data.announcementDate) : undefined,
        budgetAmount: data.budgetAmount,
        announcementCategoryCode: data.announcementCategoryCode ?? '',
        supplyMethodCode: data.supplyMethodCode ?? '',
        budgetYear: data.budgetYear,
        annotation: data.annotation,
        remark: data.remark,
        description: data.description,
        expectedDate: data.expectedDate ? new Date(data.expectedDate) : undefined,
        referencePrice: data.referencePrice,
        startDate: data.startDate ? new Date(data.startDate) : undefined,
        endDate: data.endDate ? new Date(data.endDate) : undefined,
        documentInfo: undefined,
        documentId: data.documentId,
        documentName: data.documentName,
        documentUrl: data.documentUrl,
      };
    }
  };

  const onCreateAsync = async (): Promise<string | null> => {
    const { data, status } = await AnnouncementInfoService.createAsync(body.value);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('บันทึกสำเร็จ', 'เพิ่มข้อมูลสำเร็จ');
      return data;
    }
    return null;
  };

  const onUpdateAsync = async (id: string): Promise<boolean> => {
    const { status } = await AnnouncementInfoService.updateAsync(id, body.value);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('บันทึกสำเร็จ', 'แก้ไขข้อมูลสำเร็จ');
      return true;
    }
    return false;
  };

  const onResetBody = (): void => {
    body.value = { ...initBody };
  };

  return { body, onGetByIdAsync, onCreateAsync, onUpdateAsync, onResetBody };
});
