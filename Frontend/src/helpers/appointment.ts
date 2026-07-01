import type { ColorClass, ColorLabel } from "@/models/shared/color";
import { AppointStatus } from "@/views/PP/enums/pp001";


const AppointStatusName = (value: AppointStatus) => {
  switch (value) {
    case AppointStatus.Draft:
      return "แบบร่าง";
    case AppointStatus.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case AppointStatus.Approved:
      return "อนุมัติ";
    case AppointStatus.Rejected:
      return "ส่งกลับแก้ไข";
    case AppointStatus.Edit:
      return "เรียกคืนแก้ไข";
    case AppointStatus.Cancelled:
      return "ขอยกเลิก";
    default: return "เกิดข้อผิดพลาด";
  }
}

const AppointStatusColor = (value: AppointStatus): ColorClass => {
  switch (value) {
    case AppointStatus.Draft:
      return { bgColorClass: 'bg-gray-300', textColorClass: 'text-black' };
    case AppointStatus.Edit:
      return { bgColorClass: 'bg-gray-300', textColorClass: 'text-black' };
    case AppointStatus.WaitingApproval:
      return { bgColorClass: 'bg-lime-500', textColorClass: 'text-white' };
    case AppointStatus.Approved:
      return { bgColorClass: 'bg-green-300', textColorClass: 'text-white' };
    case AppointStatus.Rejected:
      return { bgColorClass: 'bg-red-500', textColorClass: 'text-white' };
    case AppointStatus.Cancelled:
      return { bgColorClass: 'bg-red-500', textColorClass: 'text-white' };
  }
};

const BadgeStatus = (value: AppointStatus): ColorLabel => {
  const label = AppointStatusName(value);
  switch (value) {
    case AppointStatus.Draft:
      return { color: 'gray', label };
    case AppointStatus.Edit:
      return { color: 'yellow', label };
    case AppointStatus.WaitingApproval:
      return { color: 'yellow', label };
    case AppointStatus.Approved:
      return { color: 'green', label };
    case AppointStatus.Rejected:
      return { color: 'red', label };
    case AppointStatus.Cancelled:
      return { color: 'red', label };
    default: return { color: 'red', label };
  }
}

const AppointmenttHelper = {
  AppointStatusName,
  AppointStatusColor,
  BadgeStatus,
};

export default AppointmenttHelper;