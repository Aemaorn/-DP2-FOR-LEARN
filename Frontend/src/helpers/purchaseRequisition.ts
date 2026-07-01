import type { ColorLabel } from "@/models/shared/color";
import { pp004status } from "@/views/PP/enums/pp004";

const MapStatusColor = (status: pp004status): ColorLabel => {
  switch (status) {
    case pp004status.Draft:
      return { label: StatusName(status), color: 'gray' };
    case pp004status.WaitingApproval:
    case pp004status.Edit:
    case pp004status.WaitingAssign:
      return { label: StatusName(status), color: 'yellow' };
    case pp004status.Approved:
      return { label: StatusName(status), color: 'green' };
    case pp004status.Cancelled:
      return { label: StatusName(status), color: 'slate' };
    case pp004status.Rejected:
      return { label: StatusName(status), color: 'red' };
    default: return { label: StatusName(status), color: 'red' };
  }
};

const StatusName = (status: pp004status): string => {
  switch (status) {
    case pp004status.Draft:
      return 'แบบร่าง';
    case pp004status.Edit:
      return 'เรียกคืนแก้ไข';
    case pp004status.WaitingApproval:
      return 'รอเห็นชอบ/อนุมัติ';
    case pp004status.WaitingAssign:
      return 'รอมอบหมายงาน'
    case pp004status.Approved:
      return 'ยืนยันมอบหมายงาน';
    case pp004status.Rejected:
      return 'ส่งกลับแก้ไข';
    case pp004status.Cancelled:
      return 'ยกเลิกรายการ';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const PurchaseRequisionHelper = {
  StatusName,
  MapStatusColor,
};

export default PurchaseRequisionHelper;