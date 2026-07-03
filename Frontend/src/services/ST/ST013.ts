import http from '@/configs/axios';
import type { TDataTableResult } from '@/models/shared/paginated';
import type { TSt013Criteria, TSt013Detail, TSt013List } from '@/models/ST/st013';
import type { AxiosResponse } from 'axios';

// The backend Raws.RawSubDistrict API names the postal code field "zipCode" and doesn't
// return provinceCode (only districtCode) — translate at the boundary so the rest of the
// frontend can work with the TSt013List/TSt013Detail shape.
type TSt013ListWire = Omit<TSt013List, 'postalCode'> & { zipCode: string | null };
type TSt013DetailWire = Omit<TSt013Detail, 'postalCode' | 'provinceCode'> & { zipCode: string | null };

const toList = (wire: TSt013ListWire): TSt013List => ({ ...wire, postalCode: wire.zipCode ?? '' });
const toDetail = (wire: TSt013DetailWire): TSt013Detail => ({ ...wire, postalCode: wire.zipCode ?? '', provinceCode: '' });

const onGetListAsync = async (
  params: TSt013Criteria
): Promise<AxiosResponse<TDataTableResult<TSt013List>, any>> => {
  const res = await http.get<TDataTableResult<TSt013ListWire>>(`/api/st/st013`, { params });

  return {
    ...res,
    data: res.data && { data: res.data.data.map(toList), totalRecords: res.data.totalRecords },
  };
};

const onGetByIdAsync = async (id: string): Promise<AxiosResponse<TSt013Detail, any>> => {
  const res = await http.get<TSt013DetailWire>(`/api/st/st013/${id}`);

  return { ...res, data: res.data && toDetail(res.data) };
};

const onCreateAsync = async (body: TSt013Detail): Promise<AxiosResponse<{ id: string }, any>> => {
  const { postalCode, provinceCode: _provinceCode, ...rest } = body;

  return http.post<{ id: string }>(`/api/st/st013`, { ...rest, zipCode: postalCode });
};

const onUpdateAsync = async (
  id: string,
  body: TSt013Detail
): Promise<AxiosResponse<void, any>> => {
  const { postalCode, ...rest } = body;

  return http.put<void>(`/api/st/st013/${id}`, { ...rest, zipCode: postalCode });
};

const onDeleteAsync = async (id: string): Promise<AxiosResponse<void, any>> =>
  http.delete<void>(`/api/st/st013/${id}`);

const ST013Service = {
  onGetListAsync,
  onGetByIdAsync,
  onCreateAsync,
  onUpdateAsync,
  onDeleteAsync,
};

export default ST013Service;
