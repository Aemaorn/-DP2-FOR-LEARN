import { Pcm004Status } from "@/enums/pcm004";
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const Pcm004StatusName = (value: Pcm004Status): string => {
  switch (value) {
    case Pcm004Status.All:
      return 'ทั้งหมด';
    case Pcm004Status.Draft:
      return 'แบบร่าง';
    case Pcm004Status.Edit:
      return 'เรียกคืนแก้ไข';
    case Pcm004Status.WaitingApproval:
      return 'รอผู้ให้ความเห็น';
    case Pcm004Status.WaitingForInspector:
      return 'รอผู้ตรวจรับเห็นชอบ';
    case Pcm004Status.WaitingForAssignment:
      return 'รอมอบหมาย';
    case Pcm004Status.WaitingForCompletion:
      return 'รอยืนยันเบิกจ่าย';
    case Pcm004Status.Completed:
      return 'อนุมัติ';
    case Pcm004Status.Rejected:
      return 'ส่งกลับแก้ไข';
  }
};

const BadgeStatusColor = (value: Pcm004Status): ColorLabel => {
  const label = Pcm004StatusName(value);
  switch (value) {
    case Pcm004Status.All:
      return { color: "gray", label };
    case Pcm004Status.Draft:
      return { color: "gray", label };
    case Pcm004Status.Edit:
    case Pcm004Status.WaitingApproval:
    case Pcm004Status.WaitingForInspector:
    case Pcm004Status.WaitingForAssignment:
    case Pcm004Status.WaitingForCompletion:
      return { color: "yellow", label };
    case Pcm004Status.Completed:
      return { color: "green", label };
    case Pcm004Status.Rejected:
      return { color: "red", label };
  }
};

const Pcm004StatusColor = (value: Pcm004Status): ColorClass => {
  switch (value) {
    case Pcm004Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case Pcm004Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Pcm004Status.Edit:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm004Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm004Status.WaitingForInspector:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm004Status.WaitingForAssignment:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm004Status.WaitingForCompletion:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Pcm004Status.Completed:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
    case Pcm004Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
  }
};

const Pcm004Constant = {
  Pcm004StatusName,
  Pcm004StatusColor,
  BadgeStatusColor,
};

export default Pcm004Constant
