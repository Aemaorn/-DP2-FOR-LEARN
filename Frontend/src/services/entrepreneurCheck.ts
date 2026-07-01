import http from '@/configs/axios';
import type { checkCoiBody, checkWatchlistBody } from '@/models/enterpreneurCheck';

export type CheckHistorySuVendorItem = {
  taxpayerIdentificationNo?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  isJuristic: boolean;
};

export type CheckHistorySuVendorRequest = {
  vendorId?: string | null;
  checkType: 'COI' | 'Watchlist';
  items: CheckHistorySuVendorItem[];
};

export type CheckHistorySuVendorItemResponse = {
  name: string | null;
  taxpayerIdentificationNo: string | null;
  result: boolean;
  remark: string;
  checkTime: string | null;
  checkerEmployeeCode: string | null;
  employeeName: string | null;
  position: string | null;
};

const qualificationCoiAsync = async (params: checkCoiBody) =>
  http.get(`/api/coi/search`, { params });

const qualificationWatchlistAsync = async (params: checkWatchlistBody) =>
  http.get(`/api/watchlist/search`, { params });

const checkHistoryLookupAsync = async (body: CheckHistorySuVendorRequest) =>
  http.post<CheckHistorySuVendorItemResponse[]>(`/api/st/st003/check-history/lookup`, body);

const entrepreneurCheckService = {
  qualificationCoiAsync,
  qualificationWatchlistAsync,
  checkHistoryLookupAsync,
};

export default entrepreneurCheckService;
