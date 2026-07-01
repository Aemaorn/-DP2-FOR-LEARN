import ToastHelper from '@/helpers/toast';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { CustomFileSt007, TSt007Criteria, TSt007Detail, TSt007List } from '@/models/ST/st007';
import router from '@/router';
import ST007Service from '@/services/ST/ST007';
import FileService from '@/services/file';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref, type Ref } from 'vue';
import type { Option } from '@/models/shared/option';
import { EGroupCode } from '@/enums/shared';
import SharedService from '@/services/Shared/dropdown';
import { errorMessageHandler } from '@/helpers/error';

const onGetDropdownAsync = async (target: Ref<Array<Option>>, group: EGroupCode, parentCode?: string) => {
  const { data, status } = await SharedService.onGetParameterByGroupCodeAsync(group, parentCode, true);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

const groupData = [
  { value: 'Ap', label: 'ขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง' },
  { value: 'CAInv', label: 'เชิญชวนลงนามในสัญญา' },
  { value: 'CA', label: 'ร่างสัญญา' },
  { value: 'CMCltr', label: 'คืนหลักประกัน' },
  { value: 'CMTermination', label: 'บอกเลิกสัญญา' },
  { value: 'CMComplete', label: 'รายงานสัญญาแล้วเสร็จ' },
  { value: 'CAExt', label: 'ร่างสัญญาขยายเวลา' },
  { value: 'CMR', label: 'ส่งมอบ และตรวจรับ' },
  { value: 'INV', label: 'จัดทำหนังสือเชิญชวนผู้ประกอบการ' },
  { value: 'Jp04', label: 'แจ้งข้อมูลเบื้องต้น (จพ. 004)' },
  { value: 'Jp05', label: 'ขอซื้อขอจ้าง (จพ. 005)' },
  { value: 'Jp06', label: 'ขออนุมัติสั่งซื้อ/สั่งจ้าง (จพ.006)' },
  { value: 'Mdp', label: 'ราคากลาง' },
  { value: 'Plan', label: 'รายการจัดซื้อจัดจ้าง' },
  { value: 'PlanAnnment', label: 'ประกาศเผยแพร่แผนจัดซื้อจัดจ้าง' },
  { value: 'Tor', label: 'ร่างขอบเขตของงาน (TOR)' },
  { value: 'Jp10', label: 'แจ้งข้อมูลเบื้องต้น (จพ. 010)' },
  { value: 'PW119', label: 'กรณีวงเงินเล็กน้อย (ว 119)' },
  { value: 'P79C2', label: 'กรณีเร่งด่วน' },
  { value: 'PPettyCash', label: 'เงินสดย่อย (Petty Cash)' },
  { value: 'CMDbm', label: 'ขออนุมัติเบิกจ่าย (เบิกจ่าย) ' },
  { value: 'PRentalPcp', label: 'ขออนุุมัติหลักการงานเช่า' },
  { value: 'PRental', label: 'ขออนุุมัติเช่า' },
  { value: 'CertificateRequisition', label: 'ใบรองรับผลงาน' },
  { value: 'CamContractAmendment', label: 'รายการบันทึกต่อท้ายสัญญา' },
  { value: 'AuditAndRevenueReport', label: 'ข้อมูลรายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร' },
  { value: 'QuarterlyCompletion', label: 'เอกสารสัญญาแล้วเสร็จตามไตรมาส' },
  { value: 'CommitteeChange', label: 'แก้ไขคณะกรรมการ' },
  { value: 'UserManual', label: 'คู่มือการใช้งาน' },
] as Option[];

const ContractAmendmentDocumentType = [
  { value: 'AppendNewPurchaseOrder', label: 'ใบสั่ง/สัญญา -เพิ่ม PO ใหม่ต่อท้ายขอแก้ไข' },
  { value: 'WaiveOrReducePenalty', label: 'ใบสั่ง/สัญญา -เพิ่ม PO ขออนุมัติงด/ลดค่าปรับ' },
  { value: 'AdjustContractDuration', label: 'ใบสั่ง/สัญญา -เพิ่ม PO การขยายหรือลดระยะเวลาสัญญา' },
] as Option[];

export const useSt007ListStore = defineStore('st-007-list-store', () => {
  const searchCriteria = ref({
    pageNumber: 1,
    pageSize: 10,
    sort: [],
  } as TSt007Criteria);

  const table = ref({
    data: [] as TSt007List[],
    totalRecords: 0,
  } as TDataTableResult<TSt007List>);

  const groupDropdown = ref<Array<Option>>(structuredClone(groupData));

  const contractAmendmentDocumentTypeDropdown = ref<Array<Option>>(structuredClone(ContractAmendmentDocumentType));

  const onGetListData = async (): Promise<void> => {
    const { data, status } = await ST007Service.onGetListAsync(searchCriteria.value);

    if (status === HttpStatusCode.Ok) {
      table.value = data;
    }
  };

  const onChangePageSize = (pageNumber: number, pageSize: number): void => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };
  };

  const onResetCriteria = (): void => {
    searchCriteria.value = {
      pageNumber: 1,
      pageSize: 10,
      sort: [],
    };
  };

  const onDeleteAsync = async (id: string): Promise<void> => {
    const { status } = await ST007Service.onDeleteAsync(id);

    if (status === HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
      await onGetListData();
    }
  };

  return { searchCriteria, table, onChangePageSize, onGetListData, onResetCriteria, onDeleteAsync, groupDropdown, contractAmendmentDocumentTypeDropdown };
});

export const useSt007DetailStore = defineStore('st-007-detail-store', () => {
  const groupDropdown = ref<Array<Option>>(structuredClone(groupData));
  const contractAmendmentDocumentTypeDropdown = ref<Array<Option>>(structuredClone(ContractAmendmentDocumentType));
  const supplyMethodDropdown = ref<Array<Option>>([]);
  const supplyMethodTypeDropdown = ref<Array<Option>>([]);
  const contractTypeOptions = ref<Option[]>([]);
  const templateTypeOptions = ref<Option[]>([]);
  const pRentalPcpDropdown = ref<Array<Option>>([]);

  const body = ref({
    id: '',
    group: '',
    code: '',
    name: '',
    previewPdfFile: {
      id: '',
      fileName: '',
      file: {} as File,
    } as CustomFileSt007,
    file: {
      id: '',
      fileName: '',
      file: {} as File,
    } as CustomFileSt007,
    isActive: true,
  } as TSt007Detail);

  const onResetBody = (): void => {
    body.value = {
      id: '',
      group: '',
      code: '',
      name: '',
      previewPdfFile: {
        id: '',
        fileName: '',
        file: {} as File,
      } as CustomFileSt007,
      file: {
        id: '',
        fileName: '',
        file: {} as File,
      } as CustomFileSt007,
      isActive: true,
    } as TSt007Detail;
  };

  const initialDropdownDataAsync = async () => {
    await Promise.all([onGetDropdownAsync(supplyMethodDropdown, EGroupCode.SMethod)]);
  }

  const onGetContractTypeAsync = async (): Promise<void> => {
    onGetDropdownAsync(contractTypeOptions, EGroupCode.CMType);
  }

  const onGetTemplateTypeAsync = async (parentCode?: string): Promise<void> => {
    onGetDropdownAsync(templateTypeOptions, EGroupCode.CMType, parentCode);
  };

  const onGetSupplyMethodTypeAsync = async (parentCode?: string): Promise<void> => {
    onGetDropdownAsync(supplyMethodTypeDropdown, EGroupCode.SMethodType, parentCode);
  };

  const onGetPRentalPcpAsync = async (): Promise<void> => {
    onGetDropdownAsync(pRentalPcpDropdown, EGroupCode.PRentalTpl);
  };

  const onGetByIdAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST007Service.onGetByIdAsync(id);

    if (status === HttpStatusCode.Ok) {
      body.value = {
        ...data,
      } as TSt007Detail;
    }
  };

  const onSubmitAsync = async (id?: string): Promise<void> => {
    if (id) {
      await onUpdateAsync(id);
      return;
    }

    await onCreateAsync();
  };

  const onCreateAsync = async (): Promise<void> => {
    const { data, status } = await ST007Service.onCreateAsync(body.value);

    if (status === HttpStatusCode.Ok) {
      router.replace({ name: 'st007Detail', params: { id: data.id } });
      ToastHelper.createdMessageToast();
    }

    if (status === HttpStatusCode.Conflict) {
      ToastHelper.errorDescription(errorMessageHandler(data));
    }
  };

  const onUpdateAsync = async (id: string): Promise<void> => {
    const { data, status } = await ST007Service.onUpdateAsync(id, body.value);

    if (status === HttpStatusCode.Ok) {
      ToastHelper.updatedMessageToast();
    }

    if (status === HttpStatusCode.Conflict) {
      ToastHelper.errorDescription(errorMessageHandler(data));
    }
  };

  const onUploadFileAsync = async (file: File): Promise<void> => {
    // ST007 uploads .odt document templates — these go through a dedicated endpoint
    // because the general /files whitelist no longer accepts .odt.
    const { data, status } = await FileService.uploadTemplateFile(file);

    if (status === HttpStatusCode.Ok) {
      body.value.file.id = data.id;
      ToastHelper.success('สำเร็จ', 'อัปโหลดไฟล์สำเร็จ');
    }
  };

  return {
    body,
    onResetBody,
    onGetByIdAsync,
    onGetContractTypeAsync,
    onGetTemplateTypeAsync,
    onGetSupplyMethodTypeAsync,
    onGetPRentalPcpAsync,
    onSubmitAsync,
    onCreateAsync,
    onUpdateAsync,
    onUploadFileAsync,
    groupDropdown,
    initialDropdownDataAsync,
    supplyMethodDropdown,
    supplyMethodTypeDropdown,
    contractTypeOptions,
    templateTypeOptions,
    pRentalPcpDropdown,
    contractAmendmentDocumentTypeDropdown,
  };
});
