import { Cm006AccordionTab, Cm006Status } from "@/enums/CM/cm006";
import type { ColorLabel } from "@/models/shared/color";

const Cm006StatusName = (status?: Cm006Status): string => {
  switch (status) {
    case Cm006Status.All:
      return 'ทั้งหมด';
    case Cm006Status.Draft:
      return "แบบร่าง";
    case Cm006Status.WaitingCommitteeApproval:
      return "รอคณะกรรมการตรวจรับเห็นชอบ";
    case Cm006Status.WaitingAssigned:
      return "รอมอบหมายผู้รับผิดชอบ";
    case Cm006Status.Assigned:
      return "ยืนยันมอบหมาย";
    case Cm006Status.WaitingAcceptance:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ";
    case Cm006Status.Approved:
      return "อนุมัติ";
    case Cm006Status.Rejected:
      return "ส่งกลับแก้ไข";
    case Cm006Status.WaitingAccountingApproval:
      return "รอการอนุมัติจากฝ่ายบัญชี";
    case Cm006Status.AccountingRejected:
      return "ฝ่ายบัญชีส่งกลับแก้ไข";
    case Cm006Status.WaitingDisbursementDate:
      return "รอบัญชีเบิกจ่าย";
    case Cm006Status.Paid:
      return "จ่ายเงินแล้ว";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cm006BadgeStatus = (value?: Cm006Status): ColorLabel => {
  const label = Cm006StatusName(value);

  switch (value) {
    case Cm006Status.All:
    case Cm006Status.Draft:
      return { color: "gray", label };

    case Cm006Status.WaitingCommitteeApproval:
    case Cm006Status.WaitingAssigned:
    case Cm006Status.WaitingAcceptance:
      return { color: "yellow", label };

    case Cm006Status.Assigned:
      return { color: "blue", label };

    case Cm006Status.Approved:
      return { color: "green", label };
    case Cm006Status.Rejected:
      return { color: "red", label };
    case Cm006Status.WaitingAccountingApproval:
      return { color: "yellow", label };
    case Cm006Status.AccountingRejected:
      return { color: "red", label };
    case Cm006Status.WaitingDisbursementDate:
      return { color: "blue", label };
    case Cm006Status.Paid:
      return { color: "green", label };
    default:
      return { color: "red", label };
  }
};

const Cm006AccordionTabName = (value: Cm006AccordionTab): string => {
  switch (value) {
    case Cm006AccordionTab.Committee:
      return 'คณะกรรมการตรวจรับ';
    case Cm006AccordionTab.Assignee:
      return 'มอบหมายผู้รับผิดชอบ';
    case Cm006AccordionTab.Acceptor:
      return 'ผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case Cm006AccordionTab.Accounting:
      return 'ฝ่ายบัญชีเห็นชอบ';
    case Cm006AccordionTab.AccountingConfirmer:
      return 'กลุ่มงานบัญชี';
    default:
      return 'เกิดข้อผิดพลาด';
  }
};

const Cm006Constants = {
  Cm006StatusName,
  Cm006BadgeStatus,
  Cm006AccordionTabName,
};

export default Cm006Constants;