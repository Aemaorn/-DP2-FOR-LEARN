import { RP002Status } from "@/enums/RP/rp002";
import type { RP002StatusCount } from "@/models/RP/rp002";
import type { ColorClass, ColorLabel } from "@/models/shared/color";
import type { OptionBadge } from "@/models/shared/option";

const StatusName = (status: RP002Status): string => {
  switch (status) {
    case RP002Status.All:
      return 'ทั้งหมด';
    case RP002Status.Draft:
      return 'แบบร่าง';
    case RP002Status.Edit:
      return 'เรียกคืนแก้ไข';
    case RP002Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case RP002Status.Approved:
      return 'อนุมัติ';
    case RP002Status.Rejected:
      return 'ส่งกลับแก้ไข';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const StatusColorLabel = (value: RP002Status): ColorLabel => {
  const label = StatusName(value);

  switch (value) {
    case RP002Status.Draft:
      return { label, color: 'gray' };
    case RP002Status.Edit:
    case RP002Status.WaitingApproval:
      return { label, color: 'yellow' };
    case RP002Status.Approved:
      return { label, color: 'green' };
    case RP002Status.Rejected:
      return { label, color: 'red' };
    default: return { label, color: 'red' };
  }
};

const MapCountStatus = (count: RP002StatusCount): OptionBadge[] => {
  return Object.entries(RP002Status)
    .map(([, value]): OptionBadge => {
      const color = MapStatusColor(value as RP002Status);
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

const MapStatusColor = (status: RP002Status): ColorClass => {
  switch (status) {
    case RP002Status.All:
      return { bgColorClass: 'bg-[#FAFAFA]', textColorClass: 'text-black' };

    case RP002Status.Draft:
      return { bgColorClass: 'bg-[#6B7280]', textColorClass: 'text-white' };

    case RP002Status.WaitingApproval:
      return { bgColorClass: 'bg-[#7C3AED]', textColorClass: 'text-white' };

    case RP002Status.Approved:
      return { bgColorClass: 'bg-[#16A34A]', textColorClass: 'text-white' };

    case RP002Status.Rejected:
      return { bgColorClass: 'bg-[#DC2626]', textColorClass: 'text-white' };

    default:
      return { bgColorClass: 'bg-[#6B7280]', textColorClass: 'text-white' };
  }
};


const RP002Constants = {
  StatusColorLabel,
  MapCountStatus,
};

export default RP002Constants;