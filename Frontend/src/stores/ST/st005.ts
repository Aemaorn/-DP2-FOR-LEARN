import { ConfirmDialogType } from '@/enums/dialog';
import { OrganizationLevelEnum } from '@/enums/shared';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import ToastHelper from '@/helpers/toast';
import type { Option } from '@/models/shared/option';
import type { TDataTableResult } from '@/models/shared/paginated';
import type {
  TRawEmployeeDialog,
  TRawEmployeeDialogCriteria,
  TSt005Criteria,
  TSt005Detail,
  TSt005List,
  TSt005Role,
} from '@/models/ST/st005';
import SharedService from '@/services/Shared/dropdown';
import ST005Service from '@/services/ST/ST005';
import { HttpStatusCode } from 'axios';
import { defineStore } from 'pinia';
import { ref, type Ref } from 'vue';

const fetchDropdown = async (level: OrganizationLevelEnum, target: Ref<Option[]>, parentId?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetBusinessUnitAsync(level, parentId);

  if (status === HttpStatusCode.Ok) {
    target.value = data;
  }
};

export const useSt005ListStore = defineStore('st-005-list-store', () => {
  const searchCriteria = ref<TSt005Criteria>({
    isActive: true,
    pageNumber: 1,
    pageSize: 10,
  } as TSt005Criteria);

  const table = ref({
    data: [] as TSt005List[],
    totalRecords: 0,
  } as TDataTableResult<TSt005List>);

  const onGetListAsync = async (): Promise<void> => {
    const { data, status } = await ST005Service.onGetUserListAsync(searchCriteria.value);

    if (status == HttpStatusCode.Ok) {
      table.value.data = data.data;
      table.value.totalRecords = data.totalRecords;
    }
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    searchCriteria.value = {
      ...searchCriteria.value,
      pageNumber,
      pageSize,
    };

    onGetListAsync();
  };

  const onResetCriteria = () => {
    searchCriteria.value = {
      pageNumber: 1,
      pageSize: 10,
      sort: [],
      isActive: true,
    } as TSt005Criteria;
  };

  const dropdowns = {
    department: ref<Option[]>([] as Option[]),
  };

  const getDropDownDepartment = () => fetchDropdown(OrganizationLevelEnum.Department, dropdowns.department);

  const onDeleteUser = async (id: string): Promise<void> => {
    if (!(await showConfirmDialogAsync(ConfirmDialogType.Delete))) return;

    const { status } = await ST005Service.onDeleteUserAsync(id);

    if (status == HttpStatusCode.NoContent) {
      ToastHelper.deletedMessageToast();
    }

    await onGetListAsync();
  };

  const onUnlockUser = async (id: string): Promise<void> => {
    if (!(await showConfirmDialogAsync(undefined, 'ยืนยันการปลดล็อคบัญชีผู้ใช้งานนี้?'))) return;

    const { status } = await ST005Service.onUnlockUserAsync(id);

    if (status == HttpStatusCode.NoContent) {
      ToastHelper.success('จัดการผู้ใช้งาน', 'ปลดล็อคบัญชีสำเร็จ');
    }

    await onGetListAsync();
  };

  return {
    searchCriteria,
    table,
    onGetListAsync,
    onChangePageSize,
    onResetCriteria,
    dropdowns,
    getDropDownDepartment,
    onDeleteUser,
    onUnlockUser,
  };
});

export const useSt005DetailStore = defineStore('st-005-detail-store', () => {
  const body = ref<TSt005Detail>({
    isActive: true,
    role: [] as TSt005Role[],
  } as TSt005Detail);

  const userCriteria = ref<TRawEmployeeDialogCriteria>({
    pageNumber: 1,
    pageSize: 10,
  });

  const clearCriteria = async () => {
    userCriteria.value = {
      pageNumber: 1,
      pageSize: 10,
    };
  };

  const userSourceList = ref<TDataTableResult<TRawEmployeeDialog>>({
    data: [] as TRawEmployeeDialog[],
    totalRecords: 0,
  });

  const dropdowns = {
    group: ref<Option[]>([] as Option[]),
    line: ref<Option[]>([] as Option[]),
    department: ref<Option[]>([] as Option[]),
    role: ref<Option[]>([] as Option[]),
  };

  const onChangePageSize = (pageNumber: number, pageSize: number) => {
    userCriteria.value = { ...userCriteria.value, pageNumber, pageSize };
  };

  const getDropDownGroup = () => fetchDropdown(OrganizationLevelEnum.Group, dropdowns.group);
  const getDropDownLine = (parentId: string) => fetchDropdown(OrganizationLevelEnum.Line, dropdowns.line, parentId);
  const getDropDownDepartment = (parentId: string) => fetchDropdown(OrganizationLevelEnum.Department, dropdowns.department, parentId);

  const getDropDownRole = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetRoleAsync();

    if (status == HttpStatusCode.Ok) {
      dropdowns.role.value = data;
    }
  };

  const getUserDialog = async (): Promise<void> => {
    const { data, status } = await ST005Service.onGetRawEmployeeDialog(userCriteria.value);

    if (status == HttpStatusCode.Ok) {
      userSourceList.value = data;
    }
  }

  const onSelectUser = (index: number) => {
    const user = userSourceList.value.data[index];

    if (!user) return;

    body.value = {
      ...body.value,
      name: user.fullName,
      email: user.email,
      departmentName: user.departmentName,
      positionName: user.fullPositionName,
      employeeCode: user.employeeCode,
    };
  };

  const addRole = () => {
    body.value.role.push({
      roleCode: '',
    });
  };

  const removeRole = (index: number) => {
    if (index >= 0 && index < body.value.role.length) {
      body.value.role.splice(index, 1);
    }
  };

  const onGetUserById = async (id: string): Promise<void> => {
    const { data, status } = await ST005Service.onGetUserByIdAsync(id);

    if (status == HttpStatusCode.Ok) {
      body.value = data;
      body.value.role = data.role;
    }
  };

  const onCreateUser = async (): Promise<string | undefined> => {
    const { data, status } = await ST005Service.onCreateUserAsync(body.value);

    if (status == HttpStatusCode.Created) {
      ToastHelper.success("จัดการผู้ใช้งาน", "สร้างผู้ใช้งานสำเร็จ");

      await onGetUserById(data);

      return data;
    };

    if (status == HttpStatusCode.Conflict) {
      ToastHelper.warning("จัดการผู้ใช้งาน", "มีผู้ใช้งานอยู่ในระบบ");
    }
  };

  const onUpdateUser = async (id: string): Promise<void> => {
    const { status } = await ST005Service.onUpdateUserByIdAsync(id, body.value);

    if (status == HttpStatusCode.Ok) {
      ToastHelper.success("จัดการผู้ใช้งาน", "บันทึกข้อมูลสำเร็จ");
    }
  };

  const onUnlockUser = async (id: string): Promise<void> => {
    if (!(await showConfirmDialogAsync(undefined, 'ยืนยันการปลดล็อคบัญชีผู้ใช้งานนี้?'))) return;

    const { status } = await ST005Service.onUnlockUserAsync(id);

    if (status == HttpStatusCode.NoContent) {
      ToastHelper.success('จัดการผู้ใช้งาน', 'ปลดล็อคบัญชีสำเร็จ');
      await onGetUserById(id);
    }
  };

  const onClearBody = () => {
    body.value = {
      isActive: true,
      role: [] as TSt005Role[],
    } as TSt005Detail
  };

  return {
    body,
    userCriteria,
    clearCriteria,
    userSourceList,
    dropdowns,
    onChangePageSize,
    onSelectUser,
    addRole,
    removeRole,
    getDropDownGroup,
    getDropDownLine,
    getDropDownDepartment,
    getDropDownRole,
    getUserDialog,
    onGetUserById,
    onCreateUser,
    onUpdateUser,
    onUnlockUser,
    onClearBody,
  };
});
