import type { ColorLabel } from "@/models/shared/color";
import { TContractDraftStatus } from "@/views/PP/enums/pp010";


const StatusName = (status: TContractDraftStatus): string => {
  switch (status) {
    case TContractDraftStatus.Draft:
      return 'แบบร่าง';
    case TContractDraftStatus.Pending:
      return 'รอเห็นชอบ/อนุมัติ';
    case TContractDraftStatus.Approved:
      return 'อนุมัติ';
    case TContractDraftStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case TContractDraftStatus.Edit:
      return 'เรียกคืนแก้ไข'
    default: return 'เกิดข้อผิดพลาด';
  }
};

const BadgeStatus = (status: TContractDraftStatus): ColorLabel => {
  switch (status) {
    case TContractDraftStatus.Draft:
      return { label: StatusName(status), color: 'gray' };
    case TContractDraftStatus.Pending:
    case TContractDraftStatus.Edit:
      return { label: StatusName(status), color: 'yellow' };
    case TContractDraftStatus.Approved:
      return { label: StatusName(status), color: 'green' };
    case TContractDraftStatus.Rejected:
      return { label: StatusName(status), color: 'red' };
    default: return { label: StatusName(status), color: 'red' };
  }
};

const ContractDraftHelper = {
  BadgeStatus,
};

export default ContractDraftHelper;


