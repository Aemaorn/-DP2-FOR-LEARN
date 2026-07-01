import type { ColorLabel } from "@/models/shared/color";
import { PrincipleStatus } from '@/enums/PCM005/principle';

const principleStatusName = (value: PrincipleStatus): string => {
  switch (value) {
    case PrincipleStatus.Draft:
      return 'แบบร่าง';
    case PrincipleStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case PrincipleStatus.Edit:
      return 'เรียกคืนแก้ไข';
    case PrincipleStatus.WaitingUnitApproval:
      return 'รอสายงานเห็นชอบ';
    case PrincipleStatus.WaitingComment:
      return 'ยืนยันมอบหมาย'
    case PrincipleStatus.WaitingAssign:
      return 'รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมาย';
    case PrincipleStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
    case PrincipleStatus.WaitingAcceptance:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PrincipleStatus.Approved:
      return 'อนุมัติ';
  }
};

const principleStatusColor = (value: PrincipleStatus): ColorLabel => {
  switch (value) {
    case PrincipleStatus.Draft:
      return { color: 'gray', label: principleStatusName(value) };
    case PrincipleStatus.Rejected:
      return { color: 'red', label: principleStatusName(value) };
    case PrincipleStatus.Edit:
      return { color: 'yellow', label: principleStatusName(value) };
    case PrincipleStatus.WaitingUnitApproval:
    case PrincipleStatus.WaitingComment:
      return { color: 'yellow', label: principleStatusName(value) };
    case PrincipleStatus.WaitingAssign:
      return { color: 'green', label: principleStatusName(value) };
    case PrincipleStatus.RejectToAssignee:
      return { color: 'red', label: principleStatusName(value) };
    case PrincipleStatus.WaitingAcceptance:
      return { color: 'yellow', label: principleStatusName(value) };
    case PrincipleStatus.Approved:
      return { color: 'green', label: principleStatusName(value) };
  }
};

const principleConstant = {
  principleStatusName,
  principleStatusColor,
};

export default principleConstant
