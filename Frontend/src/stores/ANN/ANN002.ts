import { defineStore } from 'pinia';
import { ref } from 'vue';
import { HttpStatusCode } from 'axios';
import * as XLSX from 'xlsx';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TAnnouncementReportCriteria,
  TAnnouncementReportList,
  TAnnouncementReportBody,
  TAnnouncementReportImportRow,
} from '@/models/ANN/ann002';
import AnnouncementReportService from '@/services/ANN/ann002';
import SharedService from '@/services/Shared/dropdown';
import type { Option } from '@/models/shared/option';
import { EGroupCode } from '@/enums/shared';
import ToastHelper from '@/helpers/toast';

export const useAnnouncementReportListStore = defineStore('announcement-report-list-store', () => {
  const initCriteria: TAnnouncementReportCriteria = {
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  };

  const criteria = ref<TAnnouncementReportCriteria>({ ...initCriteria });

  const table = ref<TDataTableResult<TAnnouncementReportList>>({
    data: [],
    totalRecords: 0,
  });

  const reportTypeOptions = ref<Option[]>([]);

  const onGetListAsync = async (): Promise<void> => {
    const { data, status } = await AnnouncementReportService.onGetListAsync(criteria.value);
    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onGetReportTypeOptionsAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(EGroupCode.AnnReportType);
    if (status === HttpStatusCode.Ok) {
      reportTypeOptions.value = data;
    }
  };

  const onResetCriteria = (): void => {
    criteria.value = { ...initCriteria };
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    criteria.value = { ...criteria.value, pageNumber, pageSize };
  };

  const onDeleteAsync = async (id: string): Promise<boolean> => {
    const { status } = await AnnouncementReportService.deleteAsync(id);
    if (status === HttpStatusCode.NoContent) {
      ToastHelper.success('ลบสำเร็จ', 'ลบข้อมูลรายงานประกาศเรียบร้อยแล้ว');
      await onGetListAsync();
      return true;
    }
    return false;
  };

  const isImporting = ref(false);

  const COLUMN_MAP: Partial<Record<keyof TAnnouncementReportImportRow, string>> = {
    oldId: 'OldId',
    year: 'ปี',
    discretion: 'รายละเอียด',
    announcementReportTypeCode: 'ประเภทรายงาน',
    documentUrl: 'เอกสารแนบ',
  };

  const INT_FIELDS = new Set<keyof TAnnouncementReportImportRow>(['oldId', 'year']);

  const onDownloadTemplate = (): void => {
    const headers = Object.values(COLUMN_MAP) as string[];
    const sampleRows = reportTypeOptions.value.length > 0
      ? reportTypeOptions.value.map((opt, idx): unknown[] => [
          idx + 1,
          2569,
          'รายละเอียดรายงานประกาศ',
          opt.label,
          'https://info.ghbank.co.th/assets/uploads/2026/04/1775727075_a835e51134f6b3ee0b627f8d775ecf1e.pdf',
        ])
      : [[1, 2569, 'รายละเอียดรายงานประกาศ', 'ประเภทรายงาน', 'เอกสารแนบ']];
    const ws = XLSX.utils.aoa_to_sheet([headers, ...sampleRows]);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Template');
    XLSX.writeFile(wb, 'AnnouncementReport_Template.xlsx');
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

      const validateRow = (row: TAnnouncementReportImportRow): string[] => {
        const errs: string[] = [];
        if (!row.year) errs.push('ปี: ไม่พบข้อมูล');
        if (!row.announcementReportTypeCode?.trim()) errs.push('ประเภทรายงาน: ไม่พบข้อมูล');
        if (!row.documentUrl?.trim()) errs.push('เอกสารแนบ: ไม่พบข้อมูล');
        return errs;
      };

      const validRows: TAnnouncementReportImportRow[] = [];
      const clientFailedRows: RowValidationError[] = [];

      raw.forEach((r, idx): void => {
        const row: TAnnouncementReportImportRow = {};
        for (const [field, thCol] of Object.entries(COLUMN_MAP)) {
          const typedField = field as keyof TAnnouncementReportImportRow;
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

      const { data, status } = await AnnouncementReportService.importAsync(validRows);
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
              const label = reportTypeOptions.value.find((o): boolean => o.value === r.announcementReportTypeCode)?.label ?? r.announcementReportTypeCode ?? '-';
              return `แถว ${r.rowIndex}: ${label} ปี ${r.year ?? '-'}`;
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
    reportTypeOptions,
    isImporting,
    onGetListAsync,
    onGetReportTypeOptionsAsync,
    onResetCriteria,
    onChangePageSize,
    onDeleteAsync,
    onImportAsync,
    onDownloadTemplate,
  };
});

export const useAnnouncementReportDetailStore = defineStore('announcement-report-detail-store', () => {
  const initBody: TAnnouncementReportBody = {
    year: undefined,
    discretion: undefined,
    announcementReportTypeCode: undefined,
    documentInfo: undefined,
    documentName: undefined,
    documentId: undefined,
    documentUrl: undefined,
  };

  const body = ref<TAnnouncementReportBody>({ ...initBody });

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await AnnouncementReportService.getByIdAsync(id);
    if (status === HttpStatusCode.Ok) {
      body.value = {
        year: data.year,
        discretion: data.discretion,
        announcementReportTypeCode: data.announcementReportTypeCode,
        documentInfo: undefined,
        documentName: data.documentName,
        documentId: data.documentId,
        documentUrl: data.documentUrl,
      };
    }
  };

  const onCreateAsync = async (): Promise<string | null> => {
    const { data, status } = await AnnouncementReportService.createAsync(body.value);
    if (status === HttpStatusCode.Ok) {
      ToastHelper.success('บันทึกสำเร็จ', 'เพิ่มข้อมูลสำเร็จ');
      return data;
    }

    if (status === HttpStatusCode.Conflict) {
      ToastHelper.error('ข้อมูลซ้ำ', 'ข้อมูลประเภทรายงานและปีซ้ำกับในระบบ กรุณาเปลี่ยนข้อมูลประเภทรายงานหรือปี');
    }

    return null;
  };

  const onUpdateAsync = async (id: string): Promise<boolean> => {
    const { status } = await AnnouncementReportService.updateAsync(id, body.value);
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
