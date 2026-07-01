import {
  ProcurementProcess,
  ProcurementWorkProcess,
  ProcurementStatus,
  ProcurementTab,
  ProcurementPlanType,
} from '@/enums/procurement';
import type { ColorClass } from '@/models/shared/color';
import type { Option } from '@/models/shared/option';

const ProcurementWorkProcessName = (value: ProcurementWorkProcess) => {
  switch (value) {
    case ProcurementWorkProcess.InProcess:
      return 'อยู่ระหว่างดำเนินการ (In Process)';
    case ProcurementWorkProcess.Related:
      return 'ดำเนินการแล้ว (Related)';
    case ProcurementWorkProcess.Completed:
      return 'ดำเนินการแล้วเสร็จ (Completed)';
    case ProcurementWorkProcess.All:
      return 'งานทั้งหมด (All)';
  }
};

const ProcurementTypeName = (value: ProcurementPlanType) => {
  switch (value) {
    case ProcurementPlanType.All:
      return 'ทั้งหมด';
    case ProcurementPlanType.AnnualPlan:
      return 'แผนรวมปี';
    case ProcurementPlanType.InYearPlan:
      return 'แผนระหว่างปี';
  }
};

const ProcurementProcessName = (value: ProcurementProcess) => {
  switch (value) {
    case ProcurementProcess.All:
      return 'ทั้งหมด';
    case ProcurementProcess.DuringChange:
      return 'อยู่ระหว่างเปลี่ยนแปลง';
    case ProcurementProcess.DuringCancel:
      return 'อยู่ระหว่างยกเลิก';
  }
};

const ProcurementStatusName = (value: ProcurementStatus): string => {
  switch (value) {
    case ProcurementStatus.All:
      return 'ทั้งหมด';
    case ProcurementStatus.Draft:
      return 'แบบร่าง';
    case ProcurementStatus.InProgress:
      return 'กำลังดำเนินการ';
    case ProcurementStatus.Completed:
      return 'ดำเนินการแล้วเสร็จ';
    case ProcurementStatus.WaitingApproval:
      return 'รอฝ่ายเห็นชอบ';
    case ProcurementStatus.Cancelled:
      return 'ยกเลิกรายการ';
    default:
      return '';
  }
};

const WorklistChildStatusName = (value: string): string => {
  switch (value) {
    case 'Draft':
    case 'Edit':
    case 'InProgress':
      return 'อยู่ระหว่างจัดทำ';
    case 'WaitingUnitApproval':
    case 'WaitingCommitteeApproval':
    case 'WaitingForInspector':
      return 'รอเห็นชอบ';
    case 'Pending':
      return 'รอตรวจสอบ';
    case 'WaitingApproval':
    case 'WaitingAcceptance':
      return 'รอเห็นชอบ/อนุมัติ';
    case 'WaitingAssignment':
    case 'WaitingAssign':
      return 'รอมอบหมาย';
    case 'WaitingComment':
      return 'รอบันทึกให้ความเห็น';
    case 'Approved':
      return 'อนุมัติ';
    case 'Rejected':
    case 'RejectToAssignee':
    case 'RejectedToAssignee':
      return 'ส่งกลับแก้ไข';
    case 'Cancelled':
      return 'ยกเลิก';
    case 'NotInvited':
      return 'ไม่ได้รับเชิญ';
    case 'Completed':
      return 'ดำเนินการแล้วเสร็จ';
    case 'WaitingAccountingApproval':
      return 'รอบัญชีอนุมัติ';
    case 'WaitingDisbursementDate':
    case 'WaitingForCompletion':
      return 'รอบันทึกวันที่เบิกจ่าย';
    case 'Paid':
      return 'จ่ายแล้ว';
    default:
      return value;
  }
};

const WorklistProcurementStatusName = (value: ProcurementStatus): string => {
  switch (value) {
    case ProcurementStatus.All:
      return 'ทั้งหมด';
    case ProcurementStatus.Draft:
      return 'รอดำเนินการ';
    case ProcurementStatus.InProgress:
      return 'กำลังดำเนินการ';
    case ProcurementStatus.Completed:
      return 'ดำเนินการแล้วเสร็จ';
    case ProcurementStatus.WaitingApproval:
      return 'รอฝ่ายเห็นชอบ';
    case ProcurementStatus.Cancelled:
      return 'ยกเลิกรายการ';
    default:
      return '';
  }
};

const ProcurementTabName = (value: ProcurementTab) => {
  switch (value) {
    case ProcurementTab.Detail:
      return 'รายละเอียด';
    case ProcurementTab.RequestApprove:
      return 'เอกสารขออนุมัติประกาศเผยแพร่แผนการจัดซื้อจัดจ้าง';
    case ProcurementTab.Announcement:
      return 'เอกสารประกาศเผยแพร่แผน';
  }
};

const ProcurementWorkProcessOptions = Object.entries(ProcurementWorkProcess).map(
  ([, value]) =>
    ({
      label: ProcurementWorkProcessName(value),
      value: value,
    }) as Option
);

const ProcurementTypeOptions = Object.entries(ProcurementPlanType).map(
  ([, value]) =>
    ({
      label: ProcurementTypeName(value),
      value: value,
    }) as Option
);

const ProcurementProcessOptions = Object.entries(ProcurementProcess).map(
  ([, value]) =>
    ({
      label: ProcurementProcessName(value),
      value: value,
    }) as Option
);

const ProcurementTabOptions = Object.entries(ProcurementTab).map(
  ([, value]) => ({ label: ProcurementTabName(value), value: value }) as Option
);

const WorklistChildStatusColor = (value: string): ColorClass => {
  switch (value) {
    case 'Draft':
    case 'Edit':
    case 'Cancelled':
    case 'NotInvited':
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
    case 'WaitingUnitApproval':
    case 'WaitingCommitteeApproval':
    case 'WaitingApproval':
    case 'WaitingAssignment':
    case 'WaitingAssign':
    case 'WaitingComment':
    case 'WaitingAcceptance':
    case 'WaitingAccountingApproval':
    case 'WaitingDisbursementDate':
    case 'WaitingForInspector':
    case 'WaitingForCompletion':
    case 'InProgress':
      return { bgColorClass: 'bg-yellow-400', textColorClass: 'text-white' };
    case 'Approved':
    case 'Completed':
    case 'Paid':
      return { bgColorClass: 'bg-green-400', textColorClass: 'text-white' };
    case 'Rejected':
    case 'RejectToAssignee':
    case 'RejectedToAssignee':
      return { bgColorClass: 'bg-red-400', textColorClass: 'text-white' };
    default:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
  }
};

const ProcurementConstants = {
  ProcurementWorkProcessOptions,
  ProcurementTypeOptions,
  ProcurementProcessOptions,
  ProcurementTypeName,
  ProcurementStatusName,
  ProcurementTabOptions,
  WorklistProcurementStatusName,
  WorklistChildStatusName,
  WorklistChildStatusColor,
};

export default ProcurementConstants;
