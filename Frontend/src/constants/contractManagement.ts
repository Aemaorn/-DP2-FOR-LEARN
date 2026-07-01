import { ContractManagementStatus, ContractManagementStep } from '@/enums/contractManagement';
import type { ColorClass } from '@/models/shared/color';

const ContractManagementStatusName = (value: ContractManagementStatus): string => {
  switch (value) {
    case ContractManagementStatus.Draft:
      return 'อยู่ระหว่างจัดทำ';
    case ContractManagementStatus.Pending:
      return 'รอเห็นชอบ';
    case ContractManagementStatus.WaitingApproval:
      return 'รอเห็นชอบ/อนุมัติ';
    case ContractManagementStatus.Edit:
      return 'ส่งกลับแก้ไข';
    case ContractManagementStatus.Approved:
      return 'อนุมัติแล้ว';
    case ContractManagementStatus.Rejected:
      return 'ปฏิเสธ';
    default:
      return '';
  }
};

const WorklistContractManagementStatusName = (value: ContractManagementStatus): string => {
  switch (value) {
    case ContractManagementStatus.WaitingCommitteeApproval:
      return 'รอเห็นชอบ';
    case ContractManagementStatus.Draft:
      return 'อยู่ระหว่างจัดทำ';
    case ContractManagementStatus.Pending:
      return 'รอเห็นชอบ';
    case ContractManagementStatus.WaitingApproval:
      return 'รอเห็นชอบ/อนุมัติ';
    case ContractManagementStatus.WaitingAcceptance:
      return 'รอเห็นชอบ/รับทราบผลการตรวจรับ';
    case ContractManagementStatus.WaitingAssign:
      return 'รอมอบหมาย';
    case ContractManagementStatus.WaitingComment:
      return 'รอความเห็น';
    case ContractManagementStatus.Edit:
    case ContractManagementStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
    case ContractManagementStatus.Approved:
      return 'อนุมัติแล้ว';
    case ContractManagementStatus.Rejected:
      return 'ปฏิเสธ';
    default:
      return '';
  }
};

const WorklistContractTerminationStatusName = (value: ContractManagementStatus): string => {
  switch (value) {
    case ContractManagementStatus.WaitingCommitteeApproval:
      return 'รอมอบหมาย';
    case ContractManagementStatus.WaitingComment:
      return 'รอความเห็น';
    case ContractManagementStatus.Rejected:
    case ContractManagementStatus.RejectToAssignee:
      return 'ส่งกลับแก้ไข';
    default:
      return WorklistContractManagementStatusName(value);
  }
};

const WorklistGuaranteeReturnStatusName = (value: string): string => {
  switch (value) {
    case 'Draft':                       return 'อยู่ระหว่างจัดทำ';
    case 'WaitingCommitteeApproval':    return 'รอเห็นชอบ';
    case 'WaitingAssigned':             return 'รอมอบหมาย';
    case 'Assigned':                    return 'รับมอบหมายแล้ว';
    case 'WaitingAcceptance':           return 'รอเห็นชอบ/รับทราบ';
    case 'Approved':                    return 'อนุมัติแล้ว';
    case 'Rejected':                    return 'ส่งกลับแก้ไข';
    case 'WaitingAccountingApproval':   return 'รออนุมัติการเงิน';
    case 'AccountingRejected':          return 'การเงินส่งกลับ';
    case 'WaitingDisbursementDate':     return 'รอกำหนดวันคืนหลักประกัน';
    case 'Paid':                        return 'คืนแล้ว';
    default:                            return value;
  }
};

const ContractManagementStepShortName = (value: string): string => {
  switch (value) {
    case ContractManagementStep.DeliveryAcceptance:
      return 'บันทึกส่งมอบ และตรวจรับ';
    case ContractManagementStep.DisbursementApproval:
      return 'ขออนุมัติเบิกจ่าย';
    case ContractManagementStep.ContractTermination:
      return 'บอกเลิกสัญญา';
    case ContractManagementStep.ContractGuaranteeReturn:
      return 'รายการคืนหลักประกันสัญญา';
    default:
      return value;
  }
};

const WorklistContractManagementStatusColor = (value: ContractManagementStatus): ColorClass => {
  switch (value) {
    case ContractManagementStatus.Draft:
    case ContractManagementStatus.Edit:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
    case ContractManagementStatus.Pending:
    case ContractManagementStatus.WaitingApproval:
      return { bgColorClass: 'bg-yellow-400', textColorClass: 'text-white' };
    case ContractManagementStatus.Approved:
      return { bgColorClass: 'bg-green-400', textColorClass: 'text-white' };
    case ContractManagementStatus.Rejected:
      return { bgColorClass: 'bg-red-400', textColorClass: 'text-white' };
    default:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
  }
};

const ContractManagementConstants = {
  ContractManagementStatusName,
  ContractManagementStepShortName,
  WorklistContractManagementStatusName,
  WorklistContractTerminationStatusName,
  WorklistContractManagementStatusColor,
  WorklistGuaranteeReturnStatusName,
};

export default ContractManagementConstants;
