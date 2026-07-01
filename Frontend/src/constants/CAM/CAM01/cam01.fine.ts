
import { Cam01FineStatus } from "@/enums/CAM/CAM01/cam01.fine";
import type { ColorLabel } from "@/models/shared/color";

const Cam01FineStatusName = (status?: Cam01FineStatus): string => {
  switch (status) {
    case Cam01FineStatus.Draft:
      return "แบบร่าง";
    case Cam01FineStatus.Edit:
      return "เรียกคืนแก้ไข";
    case Cam01FineStatus.WaitingCommitteeApproval:
      return "รอคณะกรรมการตรวจรับเห็นชอบ";
    case Cam01FineStatus.WaitingAssigned:
      return "รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมายผู้รับผิดชอบ";
    case Cam01FineStatus.WaitingComment:
      return "ยืนยันมอบหมาย";
    case Cam01FineStatus.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case Cam01FineStatus.Approved:
      return "อนุมัติ";
    case Cam01FineStatus.Rejected:
      return "ส่งกลับแก้ไข";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cam01FineBadgeStatus = (value?: Cam01FineStatus): ColorLabel => {
  const label = Cam01FineStatusName(value);

  switch (value) {
    case Cam01FineStatus.Draft:
      return { color: "gray", label };

    case Cam01FineStatus.WaitingCommitteeApproval:
    case Cam01FineStatus.WaitingAssigned:
    case Cam01FineStatus.WaitingApproval:
    case Cam01FineStatus.Edit:
      return { color: "yellow", label };

    case Cam01FineStatus.WaitingComment:
      return { color: "blue", label };

    case Cam01FineStatus.Approved:
      return { color: "green", label };

    case Cam01FineStatus.Rejected:
      return { color: "red", label };

    default:
      return { color: "red", label };
  }
};

const Cam01FineConstants = {
  Cam01FineStatusName,
  Cam01FineBadgeStatus,
};

export default Cam01FineConstants;