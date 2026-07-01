import { CA02Accordion, CA02Status, CA02TabHeader } from "@/enums/CA/ca02";
import type { TCA02StatusCount } from "@/models/CA/ca02";
import type { ColorClass, ColorLabel } from "@/models/shared/color";
import type { Option, OptionBadge } from "@/models/shared/option";

const MapCA02StatusColor = (status: CA02Status): ColorClass => {
  switch (status) {
    case CA02Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };

    case CA02Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case CA02Status.Cancelled:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };

    case CA02Status.WaitingForCommitteeApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case CA02Status.Edit:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };

    case CA02Status.Approved:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };

    case CA02Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };

    default:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
  }
};

const StatusName = (status: CA02Status) => {
  switch (status) {
    case CA02Status.All:
      return 'ทั้งหมด';
    case CA02Status.Draft:
      return 'แบบร่าง';
    case CA02Status.WaitingForCommitteeApproval:
      return 'รอคณะกรรมการตรวจรับอนุมัติ';
    case CA02Status.Approved:
      return 'อนุมัติแล้ว';
    case CA02Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case CA02Status.Edit:
      return 'เรียกคืนแก้ไข';
    case CA02Status.Cancelled:
      return 'ยกเลิก';
    default: return 'ทั้งหมด';
  }
};

const MapOptionBadgeStatus = (count: TCA02StatusCount): Array<OptionBadge> => {
  return Object.entries(CA02Status)
    .filter(f => f[1] != CA02Status.Cancelled)
    .map(([, value]): OptionBadge => {
      const color = MapCA02StatusColor(value);
      const statusKey = value.toLowerCase();
      const keyStatus = statusKey as keyof TCA02StatusCount;
      const countLower = Object.fromEntries(
        Object.entries(count).map(([key, val]) => [key.toLowerCase(), val]));

      return {
        label: StatusName(value),
        value: value,
        count: countLower[keyStatus] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const BadgeStatus = (value: CA02Status): ColorLabel => {
  const label = StatusName(value);

  switch (value) {
    case CA02Status.Draft:
      return { color: 'gray', label };
    case CA02Status.WaitingForCommitteeApproval:
      return { color: 'yellow', label };
    case CA02Status.Approved:
      return { color: 'green', label };
    case CA02Status.Rejected:
      return { color: 'red', label };
    case CA02Status.Edit:
      return { color: 'yellow', label };
    case CA02Status.Cancelled:
      return { color: 'gray', label };
    default: return { color: 'red', label: 'เกิดข้อผิดพลาด' };
  };
};

const TabHeaderName = (value: CA02TabHeader) => {
  const mapType = {
    [CA02TabHeader.Detail]: 'รายละเอียด',
    [CA02TabHeader.Document]: 'เอกสารใบรับรองผลงาน'
  };

  return mapType[value];
};

const TabHeaderItem = [
  {
    label: TabHeaderName(CA02TabHeader.Detail),
    value: CA02TabHeader.Detail,
  },
  {
    label: TabHeaderName(CA02TabHeader.Document),
    value: CA02TabHeader.Document,
  },
] as Array<Option>;

const AccordionName = (value: CA02Accordion) => {
  if (value === CA02Accordion.Committee) {
    return 'คณะกรรมการตรวจรับ';
  }

  return 'เกิดข้อผิดพลาด';
};

export const CA02Helper = {
  MapOptionBadgeStatus,
  MapCA02StatusColor,
  BadgeStatus,
  TabHeaderItem,
  AccordionName,
};