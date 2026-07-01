import type { ColorClass, ColorLabel } from "@/models/shared/color";
import { CmContractTerminationStatus } from "@/enums/CM/cm005";

const cm005ColorClass = (status: CmContractTerminationStatus): ColorClass => {
  switch (status) {
    case CmContractTerminationStatus.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case CmContractTerminationStatus.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };

    case CmContractTerminationStatus.WaitingCommitteeApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case CmContractTerminationStatus.WaitingComment:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case CmContractTerminationStatus.WaitingApproval:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };

    case CmContractTerminationStatus.WaitingAssign:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case CmContractTerminationStatus.Approved:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };

    default:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
  }
};

const cm005StatusName = (value: CmContractTerminationStatus): string => {
  switch (value) {
    case CmContractTerminationStatus.All:
      return 'ทั้งหมด';
    case CmContractTerminationStatus.Draft:
      return 'แบบร่าง';
    case CmContractTerminationStatus.WaitingCommitteeApproval:
      return 'อยู่ระหว่าง คกก. เห็นชอบ';
    case CmContractTerminationStatus.WaitingAssign:
      return 'รอ จพ. มอบหมาย';
    case CmContractTerminationStatus.WaitingComment:
      return 'อยู่ระหว่าง จพ. ให้ความเห็น';
    case CmContractTerminationStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case CmContractTerminationStatus.WaitingApproval:
      return 'รออนุมัติ';
    case CmContractTerminationStatus.Approved:
      return 'อนุมัติแล้ว';
    case CmContractTerminationStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
  }
};

const cm005StatusColor = (value: CmContractTerminationStatus): ColorLabel => {
  switch (value) {
    case CmContractTerminationStatus.All:
      return { color: 'gray', label: cm005StatusName(value) };
    case CmContractTerminationStatus.Draft:
      return { color: 'gray', label: cm005StatusName(value) };
    case CmContractTerminationStatus.WaitingCommitteeApproval:
      return { color: 'yellow', label: cm005StatusName(value) };
    case CmContractTerminationStatus.WaitingAssign:
      return { color: 'yellow', label: cm005StatusName(value) };
    case CmContractTerminationStatus.WaitingComment:
      return { color: 'yellow', label: cm005StatusName(value) };
    case CmContractTerminationStatus.Rejected:
      return { color: 'red', label: cm005StatusName(value) };
    case CmContractTerminationStatus.WaitingApproval:
      return { color: 'yellow', label: cm005StatusName(value) };
    case CmContractTerminationStatus.Approved:
      return { color: 'green', label: cm005StatusName(value) };
    case CmContractTerminationStatus.RejectToAssignee:
      return { color: 'red', label: cm005StatusName(value) };
  }
};

const cm005Constant = {
  cm005StatusName,
  cm005StatusColor,
  cm005ColorClass,
};

export default cm005Constant
