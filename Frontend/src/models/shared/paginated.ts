export type TPaginated = {
  pageNumber: number;
  pageSize: number;
  sort?: string[];
};

export type TDataTableResult<T> = {
  data: T[];
  totalRecords: number;
};
