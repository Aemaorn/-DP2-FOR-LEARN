import { ContractType, rp001Status } from "@/enums/RP/rp001";
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const rp001StatusName = (value: rp001Status): string => {
  switch (value) {
    case rp001Status.All:
      return "ทั้งหมด";
    case rp001Status.Draft:
      return "แบบร่าง";
    case rp001Status.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case rp001Status.Edit:
      return "เรียกคืนแก้ไข";
    case rp001Status.Approved:
      return "อนุมัติ";
    case rp001Status.Rejected:
      return "ส่งกลับแก้ไข"
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const rp001StatusColor = (value: rp001Status): ColorClass => {
  switch (value) {
    case rp001Status.All:
      return { bgColorClass: 'bg-[#FAFAFA]', textColorClass: 'text-black' };
    case rp001Status.Draft:
      return { bgColorClass: 'bg-[#6B7280]', textColorClass: 'text-white' };
    case rp001Status.Edit:
      return { bgColorClass: 'bg-[#C2410C]', textColorClass: 'text-white' };
    case rp001Status.WaitingApproval:
      return { bgColorClass: 'bg-[#7C3AED]', textColorClass: 'text-white' };
    case rp001Status.Approved:
      return { bgColorClass: 'bg-[#16A34A]', textColorClass: 'text-white' };
    case rp001Status.Rejected:
      return { bgColorClass: 'bg-[#DC2626]', textColorClass: 'text-white' };
  }
};

const rp001ContractName = (value: ContractType): string => {
  switch (value) {
    case ContractType.All:
      return "ทั้งหมด";
    case ContractType.CMType003:
      return "สัญญาเช่า";
    case ContractType.CMType001:
      return "สัญญาซื้อขาย";
    case ContractType.CMType002:
      return "สัญญาจ้าง";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const rp001ContractColor = (value: ContractType): ColorClass => {
  switch (value) {
    case ContractType.All:
      return { bgColorClass: 'bg-[#FAFAFA]', textColorClass: 'text-black' };
    case ContractType.CMType001:
      return { bgColorClass: 'bg-[#FCD34D]', textColorClass: 'text-black' };
    case ContractType.CMType002:
      return { bgColorClass: 'bg-[#FBBF24]', textColorClass: 'text-black' };
    case ContractType.CMType003:
      return { bgColorClass: 'bg-[#B45309]', textColorClass: 'text-white' };
  }
};

const BadgeStatus = (status: rp001Status): ColorLabel => {
  switch (status) {
    case rp001Status.Draft:
      return { label: rp001StatusName(status), color: 'gray' };
    case rp001Status.WaitingApproval:
    case rp001Status.Edit:
      return { label: rp001StatusName(status), color: 'yellow' };
    case rp001Status.Approved:
      return { label: rp001StatusName(status), color: 'green' };
    case rp001Status.Rejected:
      return { label: rp001StatusName(status), color: 'red' };
    default: return { label: rp001StatusName(status), color: 'red' };
  }
};

const rp001Constant = {
  rp001StatusName,
  rp001StatusColor,
  rp001ContractName,
  rp001ContractColor,
  BadgeStatus
};

export default rp001Constant;