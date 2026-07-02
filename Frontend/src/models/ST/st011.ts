import type { TPaginated } from '../shared/paginated';

export type TSt011Criteria = {
  keyword?: string;
} & TPaginated;

export type TSt011List = {
  id: string;
  code: string;
  nameTh: string;
  nameEn: string;
};

export type TSt011Detail = {
  id?: string;
  code: string;
  nameTh: string;
  nameEn: string;
};
