export type Menu = {
  id: string;
  code: string;
  label: string;
  path: string;
  sequence: number;
  permission: string;
  children?: Menu[];
}