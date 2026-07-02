import type { TPaginated } from '../shared/paginated';

export type TDistrictCriteria = {
  keyword?: string;
} & TPaginated;

export type TDistrictList = {
  id: string;
  code: string;
  nameTh: string;
  nameEn: string;
};

export type TDistrictDetail = {
  id?: string;
  code: string;
  nameTh: string;
  nameEn: string;
};
