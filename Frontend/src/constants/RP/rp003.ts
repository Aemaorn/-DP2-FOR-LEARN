import { rp003SupplyMethod } from "@/enums/RP/rp003";
import type { Rp003Counts } from "@/models/RP/rp003";
import type { ColorClass, ColorLabel } from "@/models/shared/color";
import type { OptionBadge } from "@/models/shared/option";

const StatusName = (status: rp003SupplyMethod): string => {
  switch (status) {
    case rp003SupplyMethod.ALL:
      return 'ทั้งหมด';
    case rp003SupplyMethod.SMethod002:
      return 'พ.ร.บ.จัดซื้อจัดจ้างฯ 2560';
    case rp003SupplyMethod.SMethod004:
      return 'ข้อบังคับธนาคาร 80';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const StatusColorLabel = (value: rp003SupplyMethod): ColorLabel => {
  const label = StatusName(value);

  switch (value) {
    case rp003SupplyMethod.ALL:
      return { label, color: 'gray' };
    case rp003SupplyMethod.SMethod002:
      return { label, color: 'yellow' };
    case rp003SupplyMethod.SMethod004:
      return { label, color: 'green' };
    default: return { label, color: 'red' };
  }
};

const MapCountStatus = (count: Rp003Counts): OptionBadge[] => {
  return Object.entries(rp003SupplyMethod)
    .map(([, value]): OptionBadge => {
      const color = MapStatusColor(value as rp003SupplyMethod);
      const statusKey = value.toLowerCase();
      const countLower = Object.fromEntries(
        Object.entries(count).map(([key, val]) => [key.toLowerCase(), val]));

      return {
        label: StatusName(value),
        value: value,
        count: countLower[statusKey] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const MapStatusColor = (status: rp003SupplyMethod): ColorClass => {
  switch (status) {
    case rp003SupplyMethod.ALL:
      return { bgColorClass: 'bg-[#FAFAFA]', textColorClass: 'text-black' };

    case rp003SupplyMethod.SMethod002:
      return { bgColorClass: 'bg-[#1D4ED8]', textColorClass: 'text-white' };

    case rp003SupplyMethod.SMethod004:
      return { bgColorClass: 'bg-[#047857]', textColorClass: 'text-white' };

    default:
      return { bgColorClass: 'bg-[#6B7280]', textColorClass: 'text-white' };
  }
};


const RP003Constants = {
  StatusColorLabel,
  MapCountStatus,
};

export default RP003Constants;