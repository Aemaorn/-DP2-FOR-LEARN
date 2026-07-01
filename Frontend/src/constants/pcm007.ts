import { Pcm007Status } from '@/enums/pcm007';
import type { ColorClass, ColorLabel } from '@/models/shared/color';

const Pcm007StatusName = (value: Pcm007Status): string => {
  switch (value) {
    case Pcm007Status.All:
      return 'ทั้งหมด';
    case Pcm007Status.Draft:
      return 'แบบร่าง';
    case Pcm007Status.Edit:
      return 'เรียกคืนแก้ไข';
    case Pcm007Status.WaitingApproval:
      return 'รอเห็นชอบ/อนุมัติ';
    case Pcm007Status.WaitingCommitteeApprove:
      return 'รอคณะกรรมการตรวจรับ';
    case Pcm007Status.WaitingAccounting:
      return 'รอบัญชีเห็นชอบ';
    case Pcm007Status.WaitingDisbursementDate:
      return 'รอบันทึกวันเบิกจ่าย';
    case Pcm007Status.Paid:
      return 'เบิกจ่ายแล้ว';
    case Pcm007Status.Rejected:
      return 'ส่งกลับแก้ไข';
  }
};

const BadgeStatusColor = (value: Pcm007Status): ColorLabel => {
  const label = Pcm007StatusName(value);
  switch (value) {
    case Pcm007Status.All:
    case Pcm007Status.Draft:
      return { color: 'gray', label };
    case Pcm007Status.Edit:
      return { color: 'orange', label };
    case Pcm007Status.Rejected:
      return { color: 'red', label };
    case Pcm007Status.WaitingApproval:
    case Pcm007Status.WaitingCommitteeApprove:
    case Pcm007Status.WaitingAccounting:
    case Pcm007Status.WaitingDisbursementDate:
      return { color: 'yellow', label };
    case Pcm007Status.Paid:
      return { color: 'green', label };
  }
};

const Pcm007StatusColor = (value: Pcm007Status): ColorClass => {
  switch (value) {
    case Pcm007Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case Pcm007Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Pcm007Status.Edit:
      return { bgColorClass: 'bg-orange-200', textColorClass: 'text-orange-600' };
    case Pcm007Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case Pcm007Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm007Status.WaitingCommitteeApprove:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm007Status.WaitingAccounting:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm007Status.WaitingDisbursementDate:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm007Status.Paid:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
  }
};

const Pcm007Constant = {
  Pcm007StatusName,
  Pcm007StatusColor,
  BadgeStatusColor,
};

export default Pcm007Constant;
