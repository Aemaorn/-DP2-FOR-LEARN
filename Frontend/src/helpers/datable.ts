import type { DataTableRowReorderEvent, DataTableSortMeta } from 'primevue';

export enum EOrderType {
  ASC = 'asc',
  DESC = 'desc',
}

export const DatatableHelper = () => {
  const ordering = (data: DataTableSortMeta[] | undefined): string[] => {
    if (data) {
      return data.map((s: DataTableSortMeta): string => `${s.field} ${OrderType(s.order)}`);
    }
    return [];
  };

  const OrderType = (value: 1 | 0 | -1 | undefined | null): EOrderType => {
    return value === -1 ? EOrderType.DESC : EOrderType.ASC;
  };

  const onRowReorder = <T extends { sequence: number }>(event: DataTableRowReorderEvent): T[] => {
    return (event.value as T[]).map((item, index): T => {
      item.sequence = index + 1;

      return item;
    });
  };

  return {
    ordering,
    onRowReorder,
  };
};