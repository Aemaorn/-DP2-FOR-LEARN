import type { TPaginated } from '../shared/paginated';

export type TNationalityType = 'TH' | 'Foreign';

export type TVendorType = 'Individual' | 'JuristicPerson' | 'Consortium' | 'JointVenture';

export type TSt003Criteria = {
  keyword?: string;
  name?: string;
  type?: TVendorType;
  entrepreneurType?: string;
} & TPaginated;

export type TSt003List = {
  id: string;
  taxpayerIdentificationNo: string;
  establishmentName: string;
  type: TVendorType;
  entrepreneurType?: string;
  sapVendorNumber: string;
  sapBranchNumber: string;
  nationality: TNationalityType;
  email?: string;
  tel?: string;
  placeName?: string;
};

export type TSt003Detail = {
  id?: string; // UUID
  nationality: TNationalityType;
  type: TVendorType;
  entrepreneurType: string;
  taxpayerIdentificationNo: string;
  establishmentName: string;
  placeName: string;
  address: TSt003AddressDetail;
  tel: string;
  fax: string;
  sapVendorNumber: string;
  sapBranchNumber: string;
  email: string;
  attachments: CustomFileSt003[];
};

export interface TSt003AddressDetail {
  houseNumber: string;
  roomNumber: string;
  floor: string;
  villageName: string;
  moo: string;
  allay: string;
  road: string;
  rawProvinceCode: string;
  rawDistrictCode: string;
  rawSubDistrictCode: string;
  postalCode: string;
}

export type CustomFileSt003 = {
  id: string;
  file: File;
  fileId: string;
  fileName: string;
  sequence: number;
  isPrivate: boolean;
  createById: string;
};
