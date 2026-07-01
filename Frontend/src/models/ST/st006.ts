import type { Option } from '../shared/option';
import type { TPaginated } from '../shared/paginated';

export type St006Criteria = {
  group?: string;
  subGroup?: string;
  parentId?: string;
  parameter?: string;
} & TPaginated;

export type St006Option = {
  id: string;
} & Option;

export type St006GroupDDL = {
  id: string;
  group: string;
  label: string;
};

export type St006SubGroupDDL = {
  id: string;
  code: string;
  label: string;
};

export type St006ListResponse = {
  id: string;
  group: string;
  subGroup?: string;
  parentId?: string | null;
  parameter: string;
  sequence: number;
};

export type St006Detail = {
  group: string;
  subGroup: string;
  parentId?: string | null;
  sequence: number;
  code: string;
  name: string;
  parameters: St006Parameter[];
  isActive: boolean;
};

export type St006DetailUpdate = {
  group: string;
  subGroup: string;
  parentId?: string | null;
  sequence: number;
  code: string;
  name: string;
  values: St006Parameter[];
  isActive: boolean;
};

export type St006ParentDDL = {
  id: string;
  code: string;
  label: string;
};

export type St006Parameter = {
  key: string;
  value: { sequence: number; value: string };
};

export type St006DefaultResponse = {
  nextSequence: number;
  nextCode: string | null;
};
