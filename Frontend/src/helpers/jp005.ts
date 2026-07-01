import type { ColorLabel } from "@/models/shared/color";
import { PP005Accordion, PP005Status } from "@/views/PP/enums/pp005";

/**
 * **ฟังชั่นที่ใช้ในการแสดงผลสถานะ จพ.005**
 * @param status สถานะ จพ.005
 */
const StatusName = (status: PP005Status, isSixtyOverPrice: boolean): string => {
  const waitingApprovalLabel = isSixtyOverPrice ? 'รอส่วนงานเห็นชอบ' : 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';

  const approvedLabel = isSixtyOverPrice ? 'เห็นชอบ' : 'อนุมัติ';

  switch (status) {
    case PP005Status.Draft:
      return 'แบบร่าง';
    case PP005Status.Edit:
      return 'เรียกคืนแก้ไข';
    case PP005Status.WaitingApproval:
      return waitingApprovalLabel;
    case PP005Status.Approved:
      return approvedLabel;
    case PP005Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case PP005Status.Cancelled:
      return 'ยกเลิกรายการ';
    default: return 'เกิดข้อผิดพลาด';
  }
};

/**
 * **ฟังชั่นที่ใช้ในการแสดงผลชื่อส่วนอนุมัติเห็นชอบ**
 * @param status Enum PP005Accordion
 */
const AccordionName = (value: PP005Accordion): string => {
  switch (value) {
    case PP005Accordion.Acceptor:
      return 'ผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PP005Accordion.Segment:
      return 'ส่วนงาน';
    default: return 'เกิดข้อผิดพลาด';
  }
};

/**
 * **ฟังชั่นที่ใช้ในการแสดงผลชื่อสถานะกับสี จพ.005**
 * @param status สถานะ จพ.005
 */
const BadgeStatus = (status: PP005Status, isSixtyOverPrice: boolean): ColorLabel => {
  const label = StatusName(status, isSixtyOverPrice);
  switch (status) {
    case PP005Status.Draft:
      return { label, color: 'gray' };
    case PP005Status.Edit:
    case PP005Status.WaitingApproval:
      return { label, color: 'yellow' };
    case PP005Status.Approved:
      return { label, color: 'green' };
    case PP005Status.Rejected:
      return { label, color: 'red' };
    case PP005Status.Cancelled:
      return { label, color: 'slate' };
    default: return { label, color: 'red' };
  }
};

const Jp005Helper = {
  StatusName,
  AccordionName,
  BadgeStatus,
};

export default Jp005Helper;