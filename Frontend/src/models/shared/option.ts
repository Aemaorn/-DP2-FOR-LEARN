export type Option = {
  label: string;
  value: string | number | boolean;
  id?: string | number;
  status?: string;
  disabled?: boolean;
};

export type OptionBadge = {
  label: string;
  value: string | number;
  bgColorClass: string;
  textColorClass: string;
  count: number;
};

export type CardSelectItems = {
  title: string;
  description?: string;
  status?: string;
  isCompleted?: boolean;
  value: string | number | boolean;
}
