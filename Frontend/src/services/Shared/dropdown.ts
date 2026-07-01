import http from '@/configs/axios';
import type { EGroupCode, OrganizationLevelEnum, TemplateGroup } from '@/enums/shared';
import type { SupplyMethodCode } from '@/enums/supplyMethod';
import type { Option } from '@/models/shared/option';
import type { AxiosResponse } from 'axios';

const onGetParameterByGroupCodeAsync = async (
  groupCode: EGroupCode,
  parentCode?: string,
  isDisabledLoad?: boolean,
  optionGroupCode?: string): Promise<AxiosResponse<Option[]>> => {

  const params = {
    groupCode,
    parentCode,
    optionGroupCode,
  };

  return http.get<Array<Option>>('/api/dropdown/parameter', { params, headers: { isDisabledLoad } });
};

export type ParameterOptionWithChildren = Option & {
  children?: ParameterOptionWithChildren[] | null;
  valueKeys?: string[] | null;
};

const onGetParameterByGroupCodeIncludeChildrenAsync = async (
  groupCode: EGroupCode,
  isDisabledLoad?: boolean,
  optionGroupCode?: string): Promise<AxiosResponse<ParameterOptionWithChildren[]>> => {

  const params = {
    groupCode,
    optionGroupCode,
    includeChildren: true,
  };

  return http.get<Array<ParameterOptionWithChildren>>('/api/dropdown/parameter', { params, headers: { isDisabledLoad } });
};

const onGetParamByGroupCodeWithParentIdAsync = async (groupCode: EGroupCode, parentId?: string, isDisabledLoad?: boolean, parentCode?: string): Promise<AxiosResponse<Option[]>> => {
  const params = {
    groupCode,
    parentId,
    parentCode,
  };

  return http.get<Array<Option>>('/api/dropdown/parameter', { params, headers: { isDisabledLoad } });
};

const onGetBusinessUnitAsync = async (organizationLevel: OrganizationLevelEnum, parentId?: string, isDisabledLoad?: boolean): Promise<AxiosResponse<Option[]>> => {
  const params = {
    organizationLevel,
    parentId,
  };

  return http.get('/api/dropdown/businessunit', { params, headers: { isDisabledLoad } });
};

const onGetRoleAsync = async (): Promise<AxiosResponse<Option[]>> =>
  http.get<Array<Option>>('/api/dropdown/rolecode');

const onGetTemplateDropdownByGroupCodeAsync = async (
  groupCode: Array<TemplateGroup>,
  data?: {
    supplyMethodCode?: SupplyMethodCode,
    budget?: number,
    isJorPorComment?: boolean,
    isCancel?: boolean,
    isChange?: boolean
  }, isDisabledLoad?: boolean) => {
  const params = {
    groupCode,
    ...data,
  };

  return http.get<Array<Option>>(`/api/dropdown/document-templates`, {
    params, headers: { isDisabledLoad }, paramsSerializer: {
      indexes: null,
    },
  });
}

const onGetProvincesAsync = async (): Promise<AxiosResponse<Option[]>> =>
  http.get<Array<Option>>('/api/dropdown/provinces');

const onGetDistrictsAsync = async (provinceCode?: string): Promise<AxiosResponse<Option[]>> => {
  const params = {
    provinceCode
  };

  return http.get('/api/dropdown/districts', { params });
};

const onGetSubDistrictsAsync = async (districtCode?: string): Promise<AxiosResponse<Option[]>> => {
  const params = {
    districtCode
  };

  return http.get('/api/dropdown/subDistricts', { params });
};

const SharedService = {
  onGetParameterByGroupCodeAsync,
  onGetParameterByGroupCodeIncludeChildrenAsync,
  onGetBusinessUnitAsync,
  onGetRoleAsync,
  onGetTemplateDropdownByGroupCodeAsync,
  onGetParamByGroupCodeWithParentIdAsync,
  onGetProvincesAsync,
  onGetDistrictsAsync,
  onGetSubDistrictsAsync,
};

export default SharedService;