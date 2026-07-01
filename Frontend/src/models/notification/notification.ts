import type { TDataTableResult, TPaginated } from "../shared/paginated";

export type NotiCriteria = {
  keyword?: string;
  isMarkRead?: boolean;
} & TPaginated;

export type NotiListRes = {
  count: number;
  notifications: TDataTableResult<NotiRes>;
}

export type NotiRes = {
  id: string;
  user: string;
  title: string;
  message: string;
  createdAt: Date;
  program: string;
  linkUrl: string;
  isRead: boolean;
}