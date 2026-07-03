import type { TPaginated } from '../shared/paginated';

export type TSt012Criteria = {
  keyword?: string;
} & TPaginated;

export type TSt012List = {
  id: string;
  code: string;
  nameTh: string;
  nameEn: string;
  provinceCode: string;
  provinceName?: string;
};

export type TSt012Detail = {
  id?: string;
  code: string;
  nameTh: string;
  nameEn: string;
  provinceCode: string;
};
