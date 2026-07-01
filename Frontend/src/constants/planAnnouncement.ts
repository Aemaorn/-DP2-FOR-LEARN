import { PlanAnnouncementStatus } from "@/enums/planAnnouncement";
import type { ColorClass, ColorLabel } from "@/models/shared/color";
import type { Option } from "@/models/shared/option";

const AnnouncementStatusName = (value: PlanAnnouncementStatus): string => {
  switch (value) {
    case PlanAnnouncementStatus.All:
      return 'ทั้งหมด';
    case PlanAnnouncementStatus.Draft:
      return 'แบบร่าง';
    case PlanAnnouncementStatus.WaitingAssign:
      return 'ยืนยันมอบหมาย';
    case PlanAnnouncementStatus.WaitingAcceptor:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PlanAnnouncementStatus.WaitingAnnouncement:
      return 'รอ ผอ. จพ. เผยแพร่แผน';
    case PlanAnnouncementStatus.Announcement:
      return 'ประกาศเผยแพร่แผน';
    case PlanAnnouncementStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case PlanAnnouncementStatus.Cancelled:
      return 'ยกเลิกรายการ';
    default:
      return 'เกิดข้อผิดพลาด';
  }
};

const WorklistAnnouncementStatusName = (value: PlanAnnouncementStatus): string => {
  switch (value) {
    case PlanAnnouncementStatus.All:
      return 'ทั้งหมด';
    case PlanAnnouncementStatus.Draft:
      return 'อยู่ระหว่างจัดทำ';
    case PlanAnnouncementStatus.WaitingAssign:
      return 'รอมอบหมาย';
    case PlanAnnouncementStatus.WaitingAcceptor:
      return 'รอเห็นชอบ/อนุมัติ';
    case PlanAnnouncementStatus.WaitingAnnouncement:
      return 'รอประกาศเผยแพร่แผนฯ';
    case PlanAnnouncementStatus.Announcement:
      return 'อนุมัติ';
    case PlanAnnouncementStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case PlanAnnouncementStatus.Cancelled:
      return 'ยกเลิก';
    default:
      return 'เกิดข้อผิดพลาด';
  }
};


const BadgeStatus = (value: PlanAnnouncementStatus): ColorLabel => {
  const label = AnnouncementStatusName(value);

  switch (value) {
    case PlanAnnouncementStatus.All:
      return { color: 'gray', label };
    case PlanAnnouncementStatus.Draft:
      return { color: 'gray', label };
    case PlanAnnouncementStatus.WaitingAssign:
    case PlanAnnouncementStatus.WaitingAcceptor:
    case PlanAnnouncementStatus.WaitingAnnouncement:
      return { color: 'yellow', label };
    case PlanAnnouncementStatus.Announcement:
      return { color: 'green', label };
    case PlanAnnouncementStatus.Rejected:
      return { color: 'red', label };
    case PlanAnnouncementStatus.Cancelled:
      return { color: 'gray', label };
    default:
      return { color: 'red', label };
  }
}

const AnnouncementStatusColor = (value: PlanAnnouncementStatus): ColorClass => {
  switch (value) {
    case PlanAnnouncementStatus.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case PlanAnnouncementStatus.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case PlanAnnouncementStatus.WaitingAssign:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case PlanAnnouncementStatus.WaitingAcceptor:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case PlanAnnouncementStatus.WaitingAnnouncement:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case PlanAnnouncementStatus.Announcement:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
    case PlanAnnouncementStatus.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case PlanAnnouncementStatus.Cancelled:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    default:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
  }
};

const AnnouncementStatusOptions = Object.entries(PlanAnnouncementStatus)
  .map(
    ([, value]): Option => ({
      value: value,
      label: AnnouncementStatusName(value),
    }));

const PlanAnnouncementConstants = {
  AnnouncementStatusName,
  AnnouncementStatusOptions,
  AnnouncementStatusColor,
  BadgeStatus,
  WorklistAnnouncementStatusName,
};

export default PlanAnnouncementConstants;