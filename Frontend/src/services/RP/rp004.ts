import http from '@/configs/axios';
import type { TCheckHistoryRequest, TCheckHistoryItemResponse, TRp004Body } from '@/models/RP/rp004';

const createVendorCheckHistoryRequest = async (body: TRp004Body) =>
  await http.post(`api/st/st003/${body.vendorId}/check-history`, body);

const checkHistoryLookup = async (req: TCheckHistoryRequest) =>
  await http.post<TCheckHistoryItemResponse[]>(`api/st/st003/check-history/lookup`, req);

const rp004Service = {
  createVendorCheckHistoryRequest,
  checkHistoryLookup,
}

export default rp004Service;