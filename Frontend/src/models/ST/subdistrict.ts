import type { TPaginated } from '../shared/paginated';

export type TSubdistrictCriteria = {
  keyword?: string;
} & TPaginated;

export type TSubdistrictList = {
  id: string;
  code: string;
  nameTh: string;
  nameEn: string;
};

export type TSubdistrictDetail = {
  id?: string;
  code: string;
  nameTh: string;
  nameEn: string;
};
