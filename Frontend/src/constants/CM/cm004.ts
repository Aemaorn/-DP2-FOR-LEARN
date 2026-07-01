import type { ColorLabel } from "@/models/shared/color";
import { CmDisbursementApprovalStatus } from "@/enums/CM/cm004";

const cm004StatusName = (value: CmDisbursementApprovalStatus): string => {
  switch (value) {
    case CmDisbursementApprovalStatus.Draft:
      return 'แบบร่าง';
    case CmDisbursementApprovalStatus.Rejected:
      return 'ส่งกลับแก้ไข';
    case CmDisbursementApprovalStatus.Assigned:
      return 'จพ. มอบหมายงาน';
    case CmDisbursementApprovalStatus.WaitingApproval:
      return 'รอผู้มีอำนาจห็นชอบ/อนุมัติ';
    case CmDisbursementApprovalStatus.Approved:
      return 'อนุมัติแล้ว';
  }
};

const cm004StatusColor = (value: CmDisbursementApprovalStatus): ColorLabel => {
  switch (value) {
    case CmDisbursementApprovalStatus.Draft:
      return { color: 'gray', label: cm004StatusName(value) };
    case CmDisbursementApprovalStatus.Rejected:
      return { color: 'red', label: cm004StatusName(value) };
    case CmDisbursementApprovalStatus.Assigned:
      return { color: 'yellow', label: cm004StatusName(value) };
    case CmDisbursementApprovalStatus.WaitingApproval:
      return { color: 'yellow', label: cm004StatusName(value) };
    case CmDisbursementApprovalStatus.Approved:
      return { color: 'green', label: cm004StatusName(value) };
  }
};

const cm004Constant = {
  cm004StatusName,
  cm004StatusColor,
};

export default cm004Constant
