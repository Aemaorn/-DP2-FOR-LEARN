import { AC01Status, sourceType } from "@/enums/AC/ac01";
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const AC01StatusName = (value: AC01Status): string => {
  switch (value) {
    case AC01Status.All:
      return "ทั้งหมด";
    case AC01Status.Draft:
      return "แบบร่าง";
    case AC01Status.Approved:
      return "ยืนยันเบิกจ่าย";
    case AC01Status.Rejected:
      return "ส่งกลับแก้ไข";
    case AC01Status.Edit:
      return "เรียกคืนแก้ไข";
    case AC01Status.WaitingApproval:
      return "รอผู้มีอำนาจเห็นชอบ/อนุมัติ"
    case AC01Status.WaitingForCompletion:
      return "รอยืนยันเบิกจ่าย"
    default: return 'เกิดข้อผิดพลาด';
  }
};



const BadgeStatus = (status: AC01Status): ColorLabel => {
  const label = AC01StatusName(status);

  switch (status) {
    case AC01Status.All:
      return { color: 'gray', label };
    case AC01Status.Draft:
      return { color: 'gray', label };
    case AC01Status.Approved:
      return { color: 'green', label };
    case AC01Status.Edit:
      return { color: 'yellow', label };
    case AC01Status.Rejected:
      return { color: 'red', label };
    case AC01Status.WaitingApproval:
      return { color: 'yellow', label }
    case AC01Status.WaitingForCompletion:
      return { color: 'yellow', label }
    default: return { color: 'red', label };
  }
};

const MapStatusColor = (status: AC01Status): ColorClass => {
  switch (status) {
    case AC01Status.All:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-black' };
    case AC01Status.Draft:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
    case AC01Status.Approved:
      return { bgColorClass: 'bg-green-400', textColorClass: 'text-white' };
    case AC01Status.Rejected:
      return { bgColorClass: 'bg-red-400', textColorClass: 'text-white' };
    case AC01Status.Edit:
    case AC01Status.WaitingApproval:
      return { bgColorClass: 'bg-yellow-400', textColorClass: 'text-white' };
    default: return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
  }
};

const SourceTypeName = (type: sourceType): string => {
  switch (type) {
    case sourceType.W119:
      return 'ว 119';
    case sourceType.Clause79_2:
      return '79 วรรค 2';
    case sourceType.ContractGuaranteeReturn:
      return 'คืนหลักประกันสัญญา';
    case sourceType.Disbursement:
      return 'ตั้งหนี้'
    case sourceType.PettyCashReimbursement:
      return 'เงินสดย่อย';
  }
}

const AC01Helper = {
  AC01StatusName,
  BadgeStatus,
  SourceTypeName,
  MapStatusColor
};

export default AC01Helper;