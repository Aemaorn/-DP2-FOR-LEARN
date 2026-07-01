import { Pcm002Status } from '@/enums/pcm002';
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const Pcm002StatusName = (value: Pcm002Status): string => {
  switch (value) {
    case Pcm002Status.All:
      return 'ทั้งหมด';
    case Pcm002Status.Draft:
      return 'แบบร่าง';
    case Pcm002Status.Edit:
      return 'เรียกคืนแก้ไข';
    case Pcm002Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    // case Pcm002Status.Approved:
    //   return 'อนุมัติ';
    case Pcm002Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case Pcm002Status.WaitingAccountingApproval:
      return 'รอบัญชีเห็นชอบ';
    case Pcm002Status.WaitingDisbursementDate:
      return 'รอบันทึกวันที่เบิกจ่าย';
    case Pcm002Status.Paid:
      return 'เบิกจ่ายเสร็จสิ้น';
  }
};

const BadgeStatusColor = (value: Pcm002Status): ColorLabel => {
  const label = Pcm002StatusName(value);

  switch (value) {
    case Pcm002Status.All:
      return { color: "gray", label };
    case Pcm002Status.Draft:
      return { color: "gray", label };
    case Pcm002Status.Edit:
    case Pcm002Status.WaitingApproval:
      return { color: "yellow", label };
    // case Pcm002Status.Approved:
    //   return { color: "green", label };
    case Pcm002Status.Rejected:
      return { color: "red", label };
    case Pcm002Status.WaitingAccountingApproval:
      return { color: "yellow", label };
    case Pcm002Status.WaitingDisbursementDate:
      return { color: "yellow", label };
    case Pcm002Status.Paid:
      return { color: "green", label };
  }
};

const Pcm002StatusColor = (value: Pcm002Status): ColorClass => {
  switch (value) {
    case Pcm002Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case Pcm002Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Pcm002Status.Edit:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm002Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    // case Pcm002Status.Approved:
    //   return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
    case Pcm002Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case Pcm002Status.WaitingAccountingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm002Status.WaitingDisbursementDate:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm002Status.Paid:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
  }
};

const Pcm002Constant = {
  Pcm002StatusName,
  Pcm002StatusColor,
  BadgeStatusColor,
};

export default Pcm002Constant
