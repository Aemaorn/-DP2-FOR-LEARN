

import { Cam01AdjustContractStatus } from "@/enums/CAM/CAM01/cam01.adjustContract";
import type { ColorLabel } from "@/models/shared/color";

const Cam01AdjustContractStatusName = (status?: Cam01AdjustContractStatus): string => {
  switch (status) {
    case Cam01AdjustContractStatus.Draft:
      return "แบบร่าง";
    case Cam01AdjustContractStatus.Edit:
      return "เรียกคืนแก้ไข";
    case Cam01AdjustContractStatus.WaitingCommitteeApproval:
      return "รอคณะกรรมการตรวจรับเห็นชอบ";
    case Cam01AdjustContractStatus.WaitingAssigned:
      return "รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมายผู้รับผิดชอบ";
    case Cam01AdjustContractStatus.WaitingComment:
      return "ยืนยันมอบหมาย";
    case Cam01AdjustContractStatus.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case Cam01AdjustContractStatus.Approved:
      return "อนุมัติ";
    case Cam01AdjustContractStatus.Rejected:
      return "ส่งกลับแก้ไข";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cam01AdjustContractBadgeStatus = (value?: Cam01AdjustContractStatus): ColorLabel => {
  const label = Cam01AdjustContractStatusName(value);

  switch (value) {
    case Cam01AdjustContractStatus.Draft:
      return { color: "gray", label };

    case Cam01AdjustContractStatus.WaitingCommitteeApproval:
    case Cam01AdjustContractStatus.WaitingAssigned:
    case Cam01AdjustContractStatus.WaitingApproval:
    case Cam01AdjustContractStatus.Edit:
      return { color: "yellow", label };

    case Cam01AdjustContractStatus.WaitingComment:
      return { color: "blue", label };

    case Cam01AdjustContractStatus.Approved:
      return { color: "green", label };

    case Cam01AdjustContractStatus.Rejected:
      return { color: "red", label };

    default:
      return { color: "red", label };
  }
};

const Cam01AdjustContractConstants = {
  Cam01AdjustContractStatusName,
  Cam01AdjustContractBadgeStatus,
};

export default Cam01AdjustContractConstants;