import { PlanStatus } from "@/enums/plan";
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const PlanStatusName = (value: PlanStatus): string => {
  switch (value) {
    case PlanStatus.All:
      return 'ทั้งหมด';
    case PlanStatus.DraftPlan:
      return 'แบบร่าง';
    case PlanStatus.EditPlan:
      return 'เรียกคืนแก้ไข';
    case PlanStatus.WaitingApprovePlan:
      return 'รอฝ่ายเห็นชอบ';
    case PlanStatus.WaitingAssign:
      return 'รอมอบหมายงาน';
    case PlanStatus.Assigned:
      return 'จพ.มอบหมายงาน';
    case PlanStatus.DraftRecordDocument:
      return 'รอผู้รับผิดชอบดำเนินการ';
    case PlanStatus.WaitingAcceptor:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PlanStatus.ApprovePlan:
      return 'เห็นชอบ';
    case PlanStatus.WaitingAnnouncement:
      return 'รอประกาศเผยแพร่แผน';
    case PlanStatus.Announcement:
      return 'ประกาศเผยแพร่แผน';
    case PlanStatus.RejectPlan:
      return 'ส่งกลับแก้ไข';
    case PlanStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไขผู้รับผิดชอบ';
    case PlanStatus.CancelPlan:
      return 'ยกเลิกรายการ';
    case PlanStatus.Closed:
      return 'ปิดงาน';
  }
};

const WorkListPlanStatusName = (value: PlanStatus): string => {
  switch (value) {
    case PlanStatus.All:
      return 'ทั้งหมด';
    case PlanStatus.DraftPlan:
      return 'อยู่ระหว่างจัดทำ';
    case PlanStatus.EditPlan:
      return 'อยู่ระหว่างจัดทำ';
    case PlanStatus.WaitingApprovePlan:
      return 'รอเห็นชอบ';
    case PlanStatus.WaitingAssign:
      return 'รอมอบหมาย';
    case PlanStatus.Assigned:
      return 'จพ.มอบหมายงาน';
    case PlanStatus.DraftRecordDocument:
      return 'อยู่ระหว่างจัดทำ';
    case PlanStatus.WaitingAcceptor:
      return 'รอเห็นชอบ/อนุมัติ';
    case PlanStatus.ApprovePlan:
      return 'เห็นชอบ';
    case PlanStatus.WaitingAnnouncement:
      return 'รอประกาศเผยแพร่แผนฯ';
    case PlanStatus.Announcement:
      return 'อนุมัติ';
    case PlanStatus.RejectPlan:
      return 'ส่งกลับแก้ไข';
    case PlanStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
    case PlanStatus.CancelPlan:
      return 'ยกเลิก';
    case PlanStatus.Closed:
      return 'ปิดงาน';
  }
};

const BadgeStatusColorLabel = (value: PlanStatus): ColorLabel => {
  const label = PlanStatusName(value);

  switch (value) {
    case PlanStatus.All:
      return { label, color: 'neutral' };     // #FAFAFA

    case PlanStatus.DraftPlan:
      return { label, color: 'gray' };        // #6B7280

    case PlanStatus.EditPlan:
      return { label, color: 'orange' };      // #C2410C

    case PlanStatus.WaitingApprovePlan:
      return { label, color: 'indigo' };      // #4338CA

    case PlanStatus.RejectPlan:
      return { label, color: 'red' };         // #DC2626

    case PlanStatus.WaitingAssign:
      return { label, color: 'blue' };        // #1D4ED8

    case PlanStatus.Assigned:
      return { label, color: 'emerald' };     // #0F766E

    case PlanStatus.DraftRecordDocument:
      return { label, color: 'teal' };        // #0D9488

    case PlanStatus.RejectToAssignee:
      return { label, color: 'rose' };        // #991B1B

    case PlanStatus.WaitingAcceptor:
      return { label, color: 'violet' };      // #7C3AED

    case PlanStatus.ApprovePlan:
      return { label, color: 'green' };       // #16A34A

    case PlanStatus.WaitingAnnouncement:
      return { label, color: 'amber' };       // #D97706

    case PlanStatus.Announcement:
      return { label, color: 'cyan' };        // #0891B2

    case PlanStatus.CancelPlan:
      return { label, color: 'gray' };        // #9CA3AF

    case PlanStatus.Closed:
      return { label, color: 'slate' };

    default: return { label, color: 'red' };
  }
}

const PlanStatusColor = (value: PlanStatus): ColorClass => {
  switch (value) {
    case PlanStatus.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case PlanStatus.DraftPlan:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case PlanStatus.EditPlan:
      return { bgColorClass: 'bg-orange-200', textColorClass: 'text-orange-600' };
    case PlanStatus.WaitingApprovePlan:
      return { bgColorClass: 'bg-indigo-200', textColorClass: 'text-indigo-600' };
    case PlanStatus.WaitingAssign:
      return { bgColorClass: 'bg-blue-200', textColorClass: 'text-blue-600' };
    case PlanStatus.Assigned:
      return { bgColorClass: 'bg-emerald-200', textColorClass: 'text-emerald-600' };
    case PlanStatus.DraftRecordDocument:
      return { bgColorClass: 'bg-teal-200', textColorClass: 'text-teal-600' };
    case PlanStatus.WaitingAcceptor:
      return { bgColorClass: 'bg-violet-200', textColorClass: 'text-violet-600' };
    case PlanStatus.ApprovePlan:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
    case PlanStatus.WaitingAnnouncement:
      return { bgColorClass: 'bg-amber-200', textColorClass: 'text-amber-600' };
    case PlanStatus.Announcement:
      return { bgColorClass: 'bg-cyan-200', textColorClass: 'text-cyan-600' };
    case PlanStatus.RejectPlan:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
    case PlanStatus.RejectToAssignee:
      return { bgColorClass: 'bg-rose-200', textColorClass: 'text-rose-600' };
    case PlanStatus.CancelPlan:
      return { bgColorClass: 'bg-[#4B5563]', textColorClass: 'text-white' };
    case PlanStatus.Closed:
      return { bgColorClass: 'bg-[#334155]', textColorClass: 'text-white' };
  }
};

const PlanConstant = {
  PlanStatusName,
  PlanStatusColor,
  BadgeStatusColorLabel,
  WorkListPlanStatusName
};

export default PlanConstant
