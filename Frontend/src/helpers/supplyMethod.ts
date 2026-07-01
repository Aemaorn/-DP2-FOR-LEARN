import { SupplyMethodCode } from "@/enums/supplyMethod";

export const checkIsSixty = (code?: string): boolean => {
  if (!code) return false;

  return code === SupplyMethodCode.sixty;
};

export const checkIsEighty = (code?: string): boolean => {
  if (!code) return false;

  return code === SupplyMethodCode.eighty;
};

export const checkSupplyMethodCodeType = (code: string): string => {
  if (code === SupplyMethodCode.sixty) return 'isSixty';

  return 'isEighty';
};