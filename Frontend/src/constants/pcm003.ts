import { Pcm003Status } from '@/enums/pcm003';
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const Pcm003StatusName = (value: Pcm003Status): string => {
  switch (value) {
    case Pcm003Status.All:
      return 'ทั้งหมด';
    case Pcm003Status.Draft:
      return 'แบบร่าง';
    case Pcm003Status.Edit:
      return 'เรียกคืนแก้ไข';
    case Pcm003Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    // case Pcm003Status.Approved:
    //   return 'อนุมัติ';
    case Pcm003Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case Pcm003Status.WaitingAccountingApproval:
      return 'รอบัญชีเห็นชอบ';
    case Pcm003Status.WaitingDisbursementDate:
      return 'รอบันทึกวันที่เบิกจ่าย';
    case Pcm003Status.Paid:
      return 'เบิกจ่ายเสร็จสิ้น';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const BadgeStatusColor = (value: Pcm003Status): ColorLabel => {
  const label = Pcm003StatusName(value);
  switch (value) {
    case Pcm003Status.All:
      return { color: "gray", label };
    case Pcm003Status.Draft:
      return { color: "gray", label };
    case Pcm003Status.Edit:
    case Pcm003Status.WaitingApproval:
      return { color: "yellow", label };
    // case Pcm003Status.Approved:
    //   return { color: "green", label };
    case Pcm003Status.Rejected:
      return { color: "red", label };
    case Pcm003Status.WaitingAccountingApproval:
      return { color: "yellow", label };
    case Pcm003Status.WaitingDisbursementDate:
      return { color: "yellow", label };
    case Pcm003Status.Paid:
      return { color: "green", label };
  }
};

const Pcm003StatusColor = (value: Pcm003Status): ColorClass => {
  switch (value) {
    case Pcm003Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case Pcm003Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Pcm003Status.Edit:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm003Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    // case Pcm003Status.Approved:
    //   return { bgColorClass: 'bg-[#16A34A]', textColorClass: 'text-white' };
    case Pcm003Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case Pcm003Status.WaitingAccountingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm003Status.WaitingDisbursementDate:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm003Status.Paid:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
  }
};

const Pcm003Constant = {
  Pcm003StatusName,
  Pcm003StatusColor,
  BadgeStatusColor,
};

export default Pcm003Constant
