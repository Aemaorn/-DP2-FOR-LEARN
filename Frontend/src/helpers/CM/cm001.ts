import { CM001PeriodAccordion, CM001PeriodStatus, CM001PeriodTabHeader, CM001Status, CmDeliveryAcceptancePeriodAccountStatus } from "@/enums/CM/cm001";
import type { CM001StatusCount } from "@/models/CM/cm001";
import type { ColorClass, ColorLabel } from "@/models/shared/color";
import type { Option, OptionBadge } from "@/models/shared/option";

const MapCM001StatusColor = (status: CM001Status): ColorClass => {
  switch (status) {
    case CM001Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };

    case CM001Status.InProgress:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };

    case CM001Status.Completed:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };

    default:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
  }
};

const MapCM001OptionBadgeStatus = (count: CM001StatusCount): OptionBadge[] => {
  return Object.entries(CM001Status)
    .map(([, value]): OptionBadge => {
      const color = MapCM001StatusColor(value);
      const statusKey = value.toLowerCase();
      const keyStatus = statusKey as keyof CM001StatusCount;
      const countLower = Object.fromEntries(
        Object.entries(count).map(([key, val]) => [key.toLowerCase(), val]));

      return {
        label: StatusName(value),
        value: value,
        count: countLower[keyStatus] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const StatusName = (value: CM001Status) => {
  switch (value) {
    case CM001Status.All:
      return 'ทั้งหมด';
    case CM001Status.InProgress:
      return 'อยู่ระหว่างดำเนินการ';
    case CM001Status.Completed:
      return 'ดำเนินการแล้วเสร็จ';
    default: return 'เกิดข้อผิดพลาด';
  };
};

const BadgeStatus = (value: CM001Status): ColorLabel => {
  const label = StatusName(value);

  switch (value) {
    case CM001Status.All:
      return { color: 'gray', label };
    case CM001Status.InProgress:
      return { color: 'yellow', label };
    case CM001Status.Completed:
      return { color: 'green', label };
    default: return { color: 'red', label: 'เกิดข้อผิดพลาด' };
  };
};

export const CM001Helper = {
  StatusName,
  BadgeStatus,
  MapCM001OptionBadgeStatus,
};

const PeriodStatusName = (value: CM001PeriodStatus) => {
  switch (value) {
    case CM001PeriodStatus.Draft:
      return 'แบบร่าง';
    case CM001PeriodStatus.WaitingCommitteeApproval:
      return 'รอคณะกรรมการเห็นชอบ';
    case CM001PeriodStatus.WaitingAssign:
      return 'รอ จพ. มอบหมายงาน';
    case CM001PeriodStatus.WaitingComment:
      return 'รอ จพ. ให้ความเห็น'
    case CM001PeriodStatus.WaitingAcceptance:
      return 'อยู่ระหว่างเห็นชอบอนุมัติ';
    case CM001PeriodStatus.Approved:
      return 'อนุมัติ';
    case CM001PeriodStatus.Edit:
      return 'เรียกคืนแก้ไข';
    case CM001PeriodStatus.RejectToAssignee:
    case CM001PeriodStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    default: return 'เกิดข้อผิดพลาด';
  };
};

const PeriodBadgeStatus = (value: CM001PeriodStatus): ColorLabel => {
  const label = PeriodStatusName(value);

  switch (value) {
    case CM001PeriodStatus.Draft:
      return { color: 'gray', label };
    case CM001PeriodStatus.WaitingCommitteeApproval:
    case CM001PeriodStatus.WaitingAssign:
    case CM001PeriodStatus.WaitingComment:
    case CM001PeriodStatus.WaitingAcceptance:
      return { color: 'yellow', label };
    case CM001PeriodStatus.Approved:
      return { color: 'green', label };
    case CM001PeriodStatus.Edit:
      return { color: 'yellow', label };
    case CM001PeriodStatus.Rejected:
    case CM001PeriodStatus.RejectToAssignee:
      return { color: 'red', label };
    default: return { color: 'red', label: 'เกิดข้อผิดพลาด' };
  };
};

const PeriodTabHeaderName = (value: CM001PeriodTabHeader) => {
  const mapType = {
    [CM001PeriodTabHeader.Detail]: 'รายละเอียด',
    [CM001PeriodTabHeader.Document]: 'เอกสารรายงานผลการตรวจรับ'
  };

  return mapType[value];
};

const PeriodTabHeaderItem = [
  {
    label: PeriodTabHeaderName(CM001PeriodTabHeader.Detail),
    value: CM001PeriodTabHeader.Detail,
  },
  {
    label: PeriodTabHeaderName(CM001PeriodTabHeader.Document),
    value: CM001PeriodTabHeader.Document,
  },
] as Array<Option>;

const PeriodAccordionName = (value: CM001PeriodAccordion) => {
  switch (value) {
    case CM001PeriodAccordion.Committee:
      return 'บุคคล/คณะกรรมการตรวจรับพัสดุ';
    case CM001PeriodAccordion.Assignee:
      return 'เจ้าหน้าที่พัสดุให้ความเห็น';
    case CM001PeriodAccordion.Acceptor:
      return 'ผู้มีอำนาจรับทราบผลการตรวจรับ';
    case CM001PeriodAccordion.Accounting:
      return 'บัญชีเห็นชอบ/อนุมัติ'
    default: return 'เกิดข้อผิดพลาด';
  }
};

const PeriodAccountStatus = (value: CmDeliveryAcceptancePeriodAccountStatus, status: CM001PeriodStatus): ColorLabel => {
  const label = PeriodAccountStatusName(value);

  if (status !== CM001PeriodStatus.Approved) {
    return { color: 'gray', label: 'รออนุมัติ' };
  }

  switch (value) {
    case CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval:
    case CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate:
      return { color: 'yellow', label };
    case CmDeliveryAcceptancePeriodAccountStatus.Paid:
      return { color: 'green', label };
    case CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected:
      return { color: 'red', label };
    default: return { color: 'red', label: 'เกิดข้อผิดพลาด' };
  }
};

const PeriodAccountStatusName = (value: CmDeliveryAcceptancePeriodAccountStatus) => {
  switch (value) {
    case CmDeliveryAcceptancePeriodAccountStatus.WaitingAccountingApproval:
      return 'รอบัญชีเห็นชอบ';
    case CmDeliveryAcceptancePeriodAccountStatus.AccountingRejected:
      return 'บัญชีส่งกลับแก้ไข';
    case CmDeliveryAcceptancePeriodAccountStatus.WaitingDisbursementDate:
      return 'รอวันที่เบิกจ่าย';
    case CmDeliveryAcceptancePeriodAccountStatus.Paid:
      return 'เบิกจ่ายแล้ว';
    default: return 'เกิดข้อผิดพลาด';
  };
};

export const CM001PeriodHelper = {
  PeriodStatusName,
  PeriodBadgeStatus,
  PeriodTabHeaderItem,
  PeriodAccordionName,
  PeriodAccountStatus,
  PeriodAccountStatusName
};