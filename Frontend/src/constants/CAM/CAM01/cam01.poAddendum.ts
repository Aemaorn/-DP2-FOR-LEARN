import { Cam01PoAddendumStatus } from "@/enums/CAM/CAM01/cam01.poAddendum";
import type { ColorLabel } from "@/models/shared/color";

const Cam01PoAddendumStatusName = (status?: Cam01PoAddendumStatus): string => {
  switch (status) {
    case Cam01PoAddendumStatus.Draft:
      return "แบบร่าง";
    case Cam01PoAddendumStatus.Edit:
      return "เรียกคืนแก้ไข";
    case Cam01PoAddendumStatus.WaitingCommitteeApproval:
      return "รอคณะกรรมการตรวจรับเห็นชอบ";
    case Cam01PoAddendumStatus.WaitingAssigned:
      return "รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมายผู้รับผิดชอบ";
    case Cam01PoAddendumStatus.WaitingComment:
      return "ยืนยันมอบหมาย";
    case Cam01PoAddendumStatus.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case Cam01PoAddendumStatus.Approved:
      return "อนุมัติ";
    case Cam01PoAddendumStatus.Rejected:
      return "ส่งกลับแก้ไข";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cam01AddendumBadgeStatus = (value?: Cam01PoAddendumStatus): ColorLabel => {
  const label = Cam01PoAddendumStatusName(value);

  switch (value) {
    case Cam01PoAddendumStatus.Draft:
      return { color: "gray", label };

    case Cam01PoAddendumStatus.WaitingCommitteeApproval:
    case Cam01PoAddendumStatus.WaitingAssigned:
    case Cam01PoAddendumStatus.WaitingApproval:
    case Cam01PoAddendumStatus.Edit:
      return { color: "yellow", label };

    case Cam01PoAddendumStatus.WaitingComment:
      return { color: "blue", label };

    case Cam01PoAddendumStatus.Approved:
      return { color: "green", label };

    case Cam01PoAddendumStatus.Rejected:
      return { color: "red", label };

    default:
      return { color: "red", label };
  }
};

const Cam01PoAddendumConstants = {
  Cam01PoAddendumStatusName,
  Cam01AddendumBadgeStatus,
};

export default Cam01PoAddendumConstants;