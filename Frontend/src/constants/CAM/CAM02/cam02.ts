import { Cam02Status } from '@/enums/CAM/CAM02/cam02';
import type { ColorClass, ColorLabel } from '@/models/shared/color';

const Cam02StatusName = (status?: Cam02Status): string => {
  switch (status) {
    case Cam02Status.All:
      return 'ทั้งหมด';
    case Cam02Status.Draft:
      return 'แบบร่าง';
    case Cam02Status.WaitingApproval:
      return 'รออนุมัติ';
    case Cam02Status.Approved:
      return 'เสร็จสิ้น';
    case Cam02Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case Cam02Status.Edit:
      return 'เรียกคืนแก้ไข';
    case Cam02Status.Cancelled:
      return 'ยกเลิก';
    case Cam02Status.WaitingAssign:
      return 'รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมาย';
    case Cam02Status.WaitingComment:
      return 'ยืนยันมอบหมายงาน';
    case Cam02Status.RejectToAssignee:
      return 'ส่งกลับแก้ไขผู้รับผิดชอบ';
    case Cam02Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case Cam02Status.WaitingCommitteeApproval:
      return 'รอบุคคล/คณะกรรมการเห็นชอบ';
    default:
      return 'แบบร่าง';
  }
};

const Cam02BadgeStatus = (value?: Cam02Status): ColorLabel => {
  const label = Cam02StatusName(value);

  switch (value) {
    case Cam02Status.All:
    case Cam02Status.Draft:
      return { color: 'gray', label };
    case Cam02Status.WaitingApproval:
      return { color: 'yellow', label };
    case Cam02Status.Approved:
      return { color: 'green', label };
    case Cam02Status.Rejected:
      return { color: 'red', label };
    case Cam02Status.Edit:
      return { color: 'orange', label };
    case Cam02Status.Cancelled:
      return { color: 'gray', label };
    case Cam02Status.WaitingCommitteeApproval:
      return { color: 'yellow', label };
    case Cam02Status.WaitingAssign:
      return { color: 'yellow', label };
    case Cam02Status.WaitingComment:
      return { color: 'yellow', label };
    case Cam02Status.RejectToAssignee:
      return { color: 'red', label };
    default:
      return { color: 'gray', label };
  }
};

const Cam02ListStatusColor = (value: Cam02Status): ColorClass => {
  switch (value) {
    case Cam02Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case Cam02Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Cam02Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Cam02Status.Approved:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
    case Cam02Status.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case Cam02Status.Edit:
      return { bgColorClass: 'bg-orange-200', textColorClass: 'text-orange-600' };
    case Cam02Status.Cancelled:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Cam02Status.RejectToAssignee:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case Cam02Status.WaitingCommitteeApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Cam02Status.WaitingAssign:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Cam02Status.WaitingComment:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    default:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
  }
};

const Cam02Constants = {
  Cam02StatusName,
  Cam02BadgeStatus,
  Cam02ListStatusColor,
};

export default Cam02Constants;
