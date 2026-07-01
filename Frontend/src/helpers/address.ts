import provices from '@/masterData/province.json';
import districts from '@/masterData/districts.json';
import subDistricts from '@/masterData/subdistricts.json';
import type { Option } from '@/models/shared/option';
import SharedService from '@/services/Shared/dropdown';
import { HttpStatusCode } from 'axios';
import { ref } from 'vue';

const provinceDropdown = () => provices.map(p =>
  ({ value: p.provinceCode, label: p.provinceNameTh } as Option));

const districtDropdown = (province: number | string) => {
  const filteredDistricts = districts.filter(d => d.provinceCode == province)
    .map(d => ({
      value: d.districtCode,
      label: d.districtNameTh
    } as Option));
  return filteredDistricts;
};

const subDistrictDropdown = (district: number | string) => {
  const filteredSubDistricts = subDistricts.filter(s => s.districtCode == district)
    .map(s => ({
      value: s.subdistrictCode,
      label: s.subdistrictNameTh
    } as Option));
  return filteredSubDistricts;
};

const getPostCode = (subDistrictCode: number | string) => {
  const postCode = subDistricts.find(s => s.subdistrictCode == subDistrictCode);

  if (!postCode?.postalCode) {
    return '';
  }

  return postCode?.postalCode;
}

const getProvince = (provinceCode: number | string) => {
  if (!provinceCode) {
    return '';
  };

  return provices.find(p => p.provinceCode == provinceCode);
}

const getDistrict = (districtCode: number | string) => {
  if (!districtCode) {
    return '';
  };

  return districts.find(d => d.districtCode == districtCode);
}

const getSubDistrict = (subDistrictCode: number | string) => {
  if (!subDistrictCode) {
    return '';
  };

  return subDistricts.find(s => s.subdistrictCode == subDistrictCode);
};

const provinceOptions = ref<Option[]>([]);
const districtOptions  = ref<Option[]>([]);
const subDistrictOptions  = ref<Option[]>([]);

const getProvinceAsync = async (): Promise<void> => {
  const { data, status } = await SharedService.onGetProvincesAsync();

  if (status === HttpStatusCode.Ok) {
    provinceOptions.value = data.map(p => ({
      value: p.value,
      label: p.label
    }));
  }
};

const getDistrictAsync = async (provinceCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetDistrictsAsync(provinceCode);

  if (status === HttpStatusCode.Ok) {
      districtOptions.value = data.map(p => ({
      value: p.value,
      label: p.label
    }));
  }
};

const getSubDistrictAsync = async (districtCode?: string): Promise<void> => {
  const { data, status } = await SharedService.onGetSubDistrictsAsync(districtCode);

  if (status === HttpStatusCode.Ok) {
      subDistrictOptions.value = data.map(p => ({
      value: p.value,
      label: p.label
    }));
  }
};

export const useAddress = () => {
  const provinceOptions = ref<Option[]>([]);
  const districtOptions = ref<Option[]>([]);
  const subDistrictOptions = ref<Option[]>([]);

  const getProvinceAsync = async (): Promise<void> => {
    const { data, status } = await SharedService.onGetProvincesAsync();

    if (status === HttpStatusCode.Ok) {
      provinceOptions.value = data.map(p => ({
        value: p.value,
        label: p.label
      }));
    }
  };

  const getDistrictAsync = async (provinceCode?: string): Promise<void> => {
    const { data, status } = await SharedService.onGetDistrictsAsync(provinceCode);

    if (status === HttpStatusCode.Ok) {
      districtOptions.value = data.map(p => ({
        value: p.value,
        label: p.label
      }));
    }
  };

  const getSubDistrictAsync = async (districtCode?: string): Promise<void> => {
    const { data, status } = await SharedService.onGetSubDistrictsAsync(districtCode);

    if (status === HttpStatusCode.Ok) {
      subDistrictOptions.value = data.map(p => ({
        value: p.value,
        label: p.label
      }));
    }
  };

  return {
    provinceOptions,
    districtOptions,
    subDistrictOptions,
    getProvinceAsync,
    getDistrictAsync,
    getSubDistrictAsync,
  };
};

const addressHelper = {
  provinceDropdown,
  districtDropdown,
  subDistrictDropdown,
  getPostCode,
  getProvince,
  getDistrict,
  getSubDistrict,
  getProvinceAsync,
  getDistrictAsync,
  getSubDistrictAsync,
  provinceOptions,
  districtOptions,
  subDistrictOptions,
};

export default addressHelper;