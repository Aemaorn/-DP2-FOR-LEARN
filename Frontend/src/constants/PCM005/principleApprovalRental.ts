import type { ColorLabel } from "@/models/shared/color";
import { PrincipleApprovalRentalStatus } from '@/enums/PCM005/principleApprovalRental';

const principleApprovalRentalStatusName = (value: PrincipleApprovalRentalStatus): string => {
  switch (value) {
    case PrincipleApprovalRentalStatus.Draft:
      return 'แบบร่าง';
    case PrincipleApprovalRentalStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case PrincipleApprovalRentalStatus.Edit:
      return 'เรียกคืนแก้ไข';
    case PrincipleApprovalRentalStatus.WaitingCommitteeApproval:
      return 'รอคณะกรรมการจัดเช่าเห็นชอบ';
    case PrincipleApprovalRentalStatus.WaitingComment:
      return 'ยืนยันมอบหมาย';
    case PrincipleApprovalRentalStatus.WaitingAssign:
      return 'รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมาย';
    case PrincipleApprovalRentalStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
    case PrincipleApprovalRentalStatus.WaitingAcceptance:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PrincipleApprovalRentalStatus.Approved:
      return 'อนุมัติ';
    case PrincipleApprovalRentalStatus.WaitingContractAssign:
      return 'รอผู้รับผิดชอบใบสั่งและแจ้งทำสัญญา มอบหมายผู้รับผิดชอบ';
    case PrincipleApprovalRentalStatus.ContractAssigned:
      return 'มอบหมายผู้รับผิดชอบใบสั่งและแจ้งทำสัญญา';
    case PrincipleApprovalRentalStatus.Cancelled:
      return 'ยกเลิกรายการ';
  }
};

const principleApprovalRentalStatusColor = (value: PrincipleApprovalRentalStatus): ColorLabel => {
  switch (value) {
    case PrincipleApprovalRentalStatus.Draft:
      return { color: 'gray', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.Rejected:
      return { color: 'red', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.Edit:
      return { color: 'yellow', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.WaitingCommitteeApproval:
      return { color: 'yellow', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.WaitingAssign:
    case PrincipleApprovalRentalStatus.WaitingComment:
      return { color: 'green', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.RejectToAssignee:
      return { color: 'red', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.WaitingAcceptance:
      return { color: 'yellow', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.Approved:
      return { color: 'green', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.WaitingContractAssign:
      return { color: 'yellow', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.ContractAssigned:
      return { color: 'green', label: principleApprovalRentalStatusName(value) };
    case PrincipleApprovalRentalStatus.Cancelled:
      return { color: 'gray', label: principleApprovalRentalStatusName(value) };
  }
};

const principleApprovalRentalConstant = {
  principleApprovalRentalStatusName,
  principleApprovalRentalStatusColor,
};

export default principleApprovalRentalConstant
