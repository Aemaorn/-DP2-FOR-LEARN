import type { TPaginated } from '../shared/paginated';

export type TSt013Criteria = {
  keyword?: string;
} & TPaginated;

export type TSt013List = {
  id: string;
  code: string;
  nameTh: string;
  nameEn: string;
  postalCode: string;
  provinceId: string;
  districtId: string;
  provinceName?: string;
  districtName?: string;
};

export type TSt013Detail = {
  id?: string;
  code: string;
  nameTh: string;
  nameEn: string;
  postalCode: string;
  provinceId: string;
  districtId: string;
};
