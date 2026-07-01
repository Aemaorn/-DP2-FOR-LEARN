import type { ColorLabel } from "@/models/shared/color";
import { PP008Status } from "@/views/PP/enums/pp008";

const poaStatusName = (value: PP008Status, isNonAssign: boolean = false): string => {
  switch (value) {
    case PP008Status.Draft:
      return 'แบบร่าง';
    case PP008Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PP008Status.WaitingAssign:
      return 'รอมอบหมายผู้รับผิดชอบสัญญา';
    case PP008Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case PP008Status.Edit:
      return 'เรียกคืนแก้ไข'
    case PP008Status.Assigned:
      return isNonAssign ? 'อนุมัติ' : 'ยืนยันมอบหมาย';
  }
};

const poaStatusColor = (value: PP008Status, isNonAssign: boolean = false): ColorLabel => {
  const label = poaStatusName(value, isNonAssign);

  switch (value) {
    case PP008Status.Draft:
      return { color: 'gray', label };
    case PP008Status.WaitingApproval:
      return { color: 'yellow', label };
    case PP008Status.Edit:
      return { color: 'yellow', label };
    case PP008Status.WaitingAssign:
      return { color: 'green', label };
    case PP008Status.Rejected:
      return { color: 'red', label };
    case PP008Status.Assigned:
      return { color: 'green', label };
  }
};

const PoaConstant = {
  poaStatusName,
  poaStatusColor,
};

export default PoaConstant