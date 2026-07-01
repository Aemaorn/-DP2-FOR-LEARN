import { EPcm006Status } from "@/enums/pcm006";
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const Pcm006StatusName = (value: EPcm006Status): string => {
  switch (value) {
    case EPcm006Status.All:
      return 'ทั้งหมด';
    case EPcm006Status.Draft:
      return 'แบบร่าง';
    case EPcm006Status.Edit:
      return 'เรียกคืนแก้ไข';
    case EPcm006Status.WaitingApproval:
      return 'รอเห็นชอบ/อนุมัติ';
    // case EPcm006Status.Approved:
    //   return 'อนุมัติ';
    case EPcm006Status.Cancelled:
      return 'ยกเลิก'
    case EPcm006Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case EPcm006Status.WaitingAccountingApproval:
      return 'รอบัญชีเห็นชอบ';
    case EPcm006Status.WaitingDisbursementDate:
      return 'รอบันทึกวันที่เบิกจ่าย';
    case EPcm006Status.Paid:
      return 'เบิกจ่ายเสร็จสิ้น';
  }
};

const BadgeStatusColor = (value: EPcm006Status): ColorLabel => {
  const label = Pcm006StatusName(value);
  switch (value) {
    case EPcm006Status.All:
      return { color: "gray", label };
    case EPcm006Status.Draft:
      return { color: "gray", label };
    case EPcm006Status.Edit:
    case EPcm006Status.WaitingApproval:
      return { color: "yellow", label };
    // case EPcm006Status.Approved:
    //   return { color: "green", label };
    case EPcm006Status.Cancelled:
    case EPcm006Status.Rejected:
      return { color: "red", label };
    case EPcm006Status.WaitingAccountingApproval:
      return { color: "yellow", label };
    case EPcm006Status.WaitingDisbursementDate:
      return { color: "yellow", label };
    case EPcm006Status.Paid:
      return { color: "green", label };
  }
};

const Pcm006StatusColor = (value: EPcm006Status): ColorClass => {
  switch (value) {
    case EPcm006Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case EPcm006Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case EPcm006Status.Edit:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case EPcm006Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    // case EPcm006Status.Approved:
    //   return { bgColorClass: 'bg-[#16A34A]', textColorClass: 'text-white' };
    case EPcm006Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case EPcm006Status.Cancelled:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case EPcm006Status.WaitingAccountingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case EPcm006Status.WaitingDisbursementDate:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case EPcm006Status.Paid:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
  }
};

const Pcm006Constant = {
  Pcm006StatusName,
  Pcm006StatusColor,
  BadgeStatusColor
};

export default Pcm006Constant
