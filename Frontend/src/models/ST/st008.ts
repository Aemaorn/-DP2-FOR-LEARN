import type { TPaginated } from "../shared/paginated";

export type ST008Criteria = {
  keyword?: string;
  startDate?: Date;
  endDate?: Date;
} & TPaginated;

export type ST008List = {
  timeStamp: Date;
  userName?: string;
  programName: string;
  message: string;
  ipAddress?: string;
  userId?: string;
};