import type { ColorLabel } from "@/models/shared/color";
import type { Option } from "@/models/shared/option";
import { ContractInvitationHeader, PP009Accordion, PP009Status } from "@/views/PP/enums/pp009";

/**
 * **ฟังชั่นที่ใช้ในการแสดงผลสถานะ หนังสือเชิญชวนทำสัญญา**
 * @param status สถานะ หนังสือเชิญชวนทำสัญญา
 */
const StatusName = (status: PP009Status): string => {
  switch (status) {
    case PP009Status.Draft:
      return 'แบบร่าง';
    case PP009Status.Edit:
      return 'เรียกคืนแก้ไข';
    case PP009Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PP009Status.Approved:
      return 'อนุมัติ';
    case PP009Status.Rejected:
      return 'ส่งกลับแก้ไข';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const tabHeaderLabel = (value: ContractInvitationHeader) => {
  const mapRecord: Record<ContractInvitationHeader, string> = {
    Detail: 'รายละเอียด',
    InvitationDocument: 'เอกสารเชิญชวนทำสัญญา'
  };

  return mapRecord[value];
};

const tabHeaderOptions = Object.entries(ContractInvitationHeader)
  .map(([, value]): Option => ({
    label: tabHeaderLabel(value),
    value: value,
  }));

/**
* **ฟังชั่นที่ใช้ในการแสดงผลชื่อส่วนอนุมัติเห็นชอบ**
* @param status Enum PP009Accordion
*/
const AccordionName = (value: PP009Accordion): string => {
  switch (value) {
    case PP009Accordion.Acceptor:
      return 'ผู้มีอำนาจเห็นชอบ/อนุมัติ';
    default: return 'เกิดข้อผิดพลาด';
  }
};

/**
 * **ฟังชั่นที่ใช้ในการแสดงผลชื่อสถานะกับสี หนังสือเชิญชวนทำสัญญา**
 * @param status สถานะ หนังสือเชิญชวนทำสัญญา
 */
const BadgeStatus = (status: PP009Status): ColorLabel => {
  switch (status) {
    case PP009Status.Draft:
      return { label: StatusName(status), color: 'gray' };
    case PP009Status.Edit:
    case PP009Status.WaitingApproval:
      return { label: StatusName(status), color: 'yellow' };
    case PP009Status.Approved:
      return { label: StatusName(status), color: 'green' };
    case PP009Status.Rejected:
      return { label: StatusName(status), color: 'red' };
    default: return { label: StatusName(status), color: 'red' };
  }
};

const ContractInvitationHelper = {
  tabHeaderOptions,
  AccordionName,
  BadgeStatus,
};

export default ContractInvitationHelper;