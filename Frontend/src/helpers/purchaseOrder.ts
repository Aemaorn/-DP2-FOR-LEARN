import type { ColorLabel } from "@/models/shared/color";
import { PurchaseOrderStatus } from "@/views/PP/enums/pp007";


const StatusName = (value: PurchaseOrderStatus, isBp: boolean): string => {
  const acceptorLabel = isBp ? 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ' : 'รอสายงานเห็นชอบ/อนุมัติ';
  switch (value) {
    case PurchaseOrderStatus.Draft:
      return 'แบบร่าง';
    case PurchaseOrderStatus.Edit:
      return 'เรียกคืนแก้ไข';
    case PurchaseOrderStatus.WaitingCommitteeApproval:
      return 'รอผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างเห็นชอบ';
    case PurchaseOrderStatus.WaitingAssign:
      return 'รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมาย';
    case PurchaseOrderStatus.WaitingComment:
      return 'ยืนยันมอบหมาย';
    case PurchaseOrderStatus.WaitingApproval:
      return acceptorLabel;
    case PurchaseOrderStatus.Approved:
      return 'อนุมัติ';
    case PurchaseOrderStatus.Rejected:
    case PurchaseOrderStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
    case PurchaseOrderStatus.Cancelled:
      return 'ยกเลิกรายการ';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const MapStatusColor = (status: PurchaseOrderStatus, isBp: boolean): ColorLabel => {
  const label = StatusName(status, isBp);
  switch (status) {
    case PurchaseOrderStatus.Draft:
      return { label, color: 'gray' };
    case PurchaseOrderStatus.WaitingCommitteeApproval:
    case PurchaseOrderStatus.WaitingAssign:
    case PurchaseOrderStatus.WaitingApproval:
    case PurchaseOrderStatus.Edit:
    case PurchaseOrderStatus.WaitingComment:
      return { label, color: 'yellow' };
    case PurchaseOrderStatus.Approved:
      return { label, color: 'green' };
    case PurchaseOrderStatus.Cancelled:
      return { label, color: 'slate' };
    case PurchaseOrderStatus.Rejected:
    case PurchaseOrderStatus.RejectToAssignee:
      return { label, color: 'red' };
    default: return { label, color: 'red' };
  }
};

const purchaseOrderHelper = {
  MapStatusColor,
  StatusName,
};

export default purchaseOrderHelper;