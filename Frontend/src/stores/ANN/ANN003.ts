import { defineStore } from 'pinia';
import { ref } from 'vue';
import { HttpStatusCode } from 'axios';
import * as XLSX from 'xlsx';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TAnnouncementSorKorRorCriteria,
  TAnnouncementSorKorRorList,
  TAnnouncementSorKorRorBody,
  TAnnouncementSorKorRorImportRow,
} from '@/models/ANN/ann003';
import AnnouncementSorKorRorService from '@/services/ANN/ann003';
import SharedService from '@/services/Shared/dropdown';
import type { Option } from '@/models/shared/option';
import { EGroupCode } from '@/enums/shared';
import ToastHelper from '@/helpers/toast';

export const useAnnouncementSorKorRorListStore = defineStore('announcement-sor-kor-ror-list-store', () => {
  const initCriteria: TAnnouncementSorKorRorCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  };

  const criteria = ref<TAnnouncementSorKorRorCriteria>({ ...initCriteria });

  const table = ref<TDataTableResult<TAnnouncementSorKorRorList>>({
    data: [],
    totalRecords: 0,
  });

  const departmentTypeOptions = ref<Option[]>([]);

  const onGetListAsync = async (): Promise<void> => {
    const { data, status } = await AnnouncementSorKorRorService.onGetListAsync(criteria.value);
    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onGetDepartmentTypeOptionsAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AssignDept);
    if (status === HttpStatusCode.Ok) {
      departmentTypeOptions.value = data;
    }
  };

  const onResetCriteria = (): void => {
    criteria.value = { ...initCriteria };
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    criteria.value = { ...criteria.value, pageNumber, pageSize };
  };

  const onDeleteAsync = async (id: string): Promise<boolean> => {
    const { status } = await AnnouncementSorKorRorService.deleteAsync(id);
    if (status === HttpStatusCode.NoContent) {
      ToastHelper.success('ลบสำเร็จ', 'ลบข้อมูลเรียบร้อย');
      await onGetListAsync();
      return true;
    }
    return false;
  };

  const isImporting = ref(false);

  const COLUMN_MAP: Partial<Record<keyof TAnnouncementSorKorRorImportRow, string>> = {
    oldId: 'OldId',
    year: 'ปี',
    month: 'เดือน',
    amount: 'จำนวน (ฉบับ)',
    departmentTypeCode: 'ประเภทหน่วยงาน',
    documentUrl: 'เอกสารแนบ',
  };

  const INT_FIELDS = new Set<keyof TAnnouncementSorKorRorImportRow>(['oldId', 'year', 'month', 'amount']);

  const onDownloadTemplate = (): void => {
    const headers = Object.values(COLUMN_MAP) as string[];
    const sampleRows = departmentTypeOptions.value.length > 0
      ? departmentTypeOptions.value.map((opt, idx): unknown[] => [
          idx + 1,
          2569,
          1,
          5,
          opt.label,
          'https://example.com/document.pdf',
        ])
      : [[1, 2569, 1, 5, 'ประเภทหน่วยงาน', 'เอกสารแนบ']];
    const ws = XLSX.utils.aoa_to_sheet([headers, ...sampleRows]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Template');
    XLSX.writeFile(wb, 'AnnouncementSorKorRor_Template.xlsx');
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

      type RowValidationError = { rowIndex: number; errors: string[] };

      const validateRow = (row: TAnnouncementSorKorRorImportRow): string[] => {
        const errs: string[] = [];
        if (!row.year) errs.push('ปี: ไม่พบข้อมูล');
        if (!row.month) errs.push('เดือน: ไม่พบข้อมูล');
        if (!row.departmentTypeCode?.trim()) errs.push('ประเภทหน่วยงาน: ไม่พบข้อมูล');
        return errs;
      };

      const validRows: TAnnouncementSorKorRorImportRow[] = [];
      const clientFailedRows: RowValidationError[] = [];

      raw.forEach((r, idx): void => {
        const row: TAnnouncementSorKorRorImportRow = {};
        for (const [field, thCol] of Object.entries(COLUMN_MAP)) {
          const typedField = field as keyof TAnnouncementSorKorRorImportRow;
          const val = r[thCol];
          if (val === undefined || val === null || val === '') continue;
          if (INT_FIELDS.has(typedField)) {
            const n = Number(val);
            if (!isNaN(n)) (row as Record<string, unknown>)[field] = Math.trunc(n);
          } else {
            (row as Record<string, unknown>)[field] = String(val);
          }
        }
        const errs = validateRow(row);
        if (errs.length > 0) {
          clientFailedRows.push({ rowIndex: idx + 2, errors: errs });
        } else {
          validRows.push(row);
        }
      });

      if (validRows.length === 0 && clientFailedRows.length > 0) {
        const summary = clientFailedRows
          .slice(0, 5)
          .map((f): string => `แถว ${f.rowIndex}: ${f.errors.join(', ')}`)
          .join('\n');
        ToastHelper.error(`ข้อมูลไม่ถูกต้อง (${clientFailedRows.length} แถวมีข้อผิดพลาด)`, summary);
        return;
      }

      const { data, status } = await AnnouncementSorKorRorService.importAsync(validRows);
      if (status === HttpStatusCode.Ok) {
        const totalFailed = clientFailedRows.length + data.failedCount;
        const parts: string[] = [`นำเข้าสำเร็จ ${data.successCount} รายการ`];
        if (totalFailed > 0) parts.push(`ล้มเหลว ${totalFailed} รายการ`);
        if (data.skippedCount > 0) parts.push(`ข้าม ${data.skippedCount} รายการ (ข้อมูลซ้ำ)`);
        const toastFn = totalFailed > 0 || data.skippedCount > 0 ? ToastHelper.warning : ToastHelper.success;
        toastFn('นำเข้าข้อมูล', parts.join(', '));

        if (data.skippedCount > 0) {
          const skippedDetail = data.skippedRows
            .slice(0, 5)
            .map((r): string => {
              const label = departmentTypeOptions.value.find((o): boolean => o.value === r.departmentTypeCode)?.label ?? r.departmentTypeCode ?? '-';
              return `แถว ${r.rowIndex}: ${label} ปี ${r.year ?? '-'} เดือน ${r.month ?? '-'}`;
            })
            .join('\n');
          const suffix = data.skippedCount > 5 ? `\n... และอีก ${data.skippedCount - 5} รายการ` : '';
          ToastHelper.warning('ข้อมูลซ้ำ', skippedDetail + suffix);
        }

        await onGetListAsync();
      }
    } catch {
      ToastHelper.error('เกิดข้อผิดพลาด', 'ไม่สามารถนำเข้าข้อมูลได้');
    } finally {
      isImporting.value = false;
    }
  };

  return {
    criteria,
    table,
    departmentTypeOptions,
    isImporting,
    onGetListAsync,
    onGetDepartmentTypeOptionsAsync,
    onResetCriteria,
    onChangePageSize,
    onDeleteAsync,
    onImportAsync,
    onDownloadTemplate,
  };
});

export const useAnnouncementSorKorRorDetailStore = defineStore('announcement-sor-kor-ror-detail-store', () => {
  const initBody: TAnnouncementSorKorRorBody = {
    year: undefined,
    month: undefined,
    amount: undefined,
    departmentTypeCode: undefined,
    documentInfo: undefined,
    documentName: undefined,
    documentId: undefined,
    documentUrl: undefined,
  };

  const body = ref<TAnnouncementSorKorRorBody>({ ...initBody });

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await AnnouncementSorKorRorService.getByIdAsync(id);
    if (status === HttpStatusCode.Ok) {
      body.value = {
        year: data.year,
        month: data.month,
        amount: data.amount,
        departmentTypeCode: data.departmentTypeCode,
        documentInfo: undefined,
        documentName: data.documentName,
        documentId: data.documentId,
        documentUrl: data.documentUrl,
      };
    }
  };

  const onCreateAsync = async (): Promise<string | null> => {
    const { data, status } = await AnnouncementSorKorRorService.createAsync(body.value);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('บันทึกสำเร็จ', 'เพิ่มข้อมูลสำเร็จ');
      return data;
    }
    return null;
  };

  const onUpdateAsync = async (id: string): Promise<boolean> => {
    const { status } = await AnnouncementSorKorRorService.updateAsync(id, body.value);
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
