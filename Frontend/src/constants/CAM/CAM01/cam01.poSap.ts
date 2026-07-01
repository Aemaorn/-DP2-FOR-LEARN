import { Cam01PoSapStatus } from "@/enums/CAM/CAM01/cam01.poSap";
import type { ColorLabel } from "@/models/shared/color";

const Cam01PoSapStatusName = (status?: Cam01PoSapStatus): string => {
  switch (status) {
    case Cam01PoSapStatus.All:
      return "ทั้งหมด";
    case Cam01PoSapStatus.Draft:
      return "แบบร่าง";
    case Cam01PoSapStatus.Edit:
      return "เรียกคืนแก้ไข";
    case Cam01PoSapStatus.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case Cam01PoSapStatus.Approved:
      return "อนุมัติ";
    case Cam01PoSapStatus.Rejected:
      return "ส่งกลับแก้ไข";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cam01PoSapBadgeStatus = (value?: Cam01PoSapStatus): ColorLabel => {
  const label = Cam01PoSapStatusName(value);

  switch (value) {
    case Cam01PoSapStatus.Draft:
      return { color: "gray", label };

    case Cam01PoSapStatus.WaitingApproval:
    case Cam01PoSapStatus.Edit:
      return { color: "yellow", label };

    case Cam01PoSapStatus.Approved:
      return { color: "green", label };

    case Cam01PoSapStatus.Rejected:
      return { color: "red", label };

    default:
      return { color: "red", label };
  }
};

const Cam01PoSapConstants = {
  Cam01PoSapStatusName,
  Cam01PoSapBadgeStatus,
};

export default Cam01PoSapConstants;