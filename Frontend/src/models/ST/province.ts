import type { TPaginated } from '../shared/paginated';

export type TProvinceCriteria = {
  keyword?: string;
} & TPaginated;

export type TProvinceList = {
  id: string;
  code: string;
  nameTh: string;
  nameEn: string;
};

export type TProvinceDetail = {
  id?: string;
  code: string;
  nameTh: string;
  nameEn: string;
};
