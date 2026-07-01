import type { ContractDraftTemplate } from "@/enums/contractDraftt";
import type { ParticipantsAcceptor } from "@/models/shared/participants";
import type { TNationalityType, TVendorType } from "@/models/ST/st003";
import type { TAgreementBaseType, TContractDraftStatus, TPaymentBaseType, TRedeliveryBaseType } from "../../enums/pp010";
import type { QualificationResultDto, Shareholder } from "../PP006/pp006Model";
import type { EntrepreneurAttachments } from "@/models/shared/uploadFile";
import type { OperatorType } from "../../enums/pp005";
import type { DocumentVersion } from "@/models/shared/document";

export type TRentalDurationInfo = {
  year: number;
  month: number;
  day: number;
}

export type TLocationInfo = {
  code: string;
  name: string;
}

export type TContractDraftVendor = {
  id: string;
  name: string;
}

export type TContractDraftBody = {
  id?: string;
  vendorId?: string;
  documentDate?: Date;
  email: string;
  contractName: string;
  poNumber: string;
  contractNumber: string;
  budget: number;
  contractSignedDate: Date;
  contractEndDate: Date;
  periodConditionType: string;
  startDate: Date;
  endDate: Date;
  contractType: string;
  template: ContractDraftTemplate;
  subTemplate?: string;
  templateText: string;
  isWorkingDayOnly: boolean;
  contractStatus: TContractDraftStatus;
  vatRateTypeCode?: string;
  detail: {
    defectWarrantyTypeCode?: string;

    /// ข้อมูลผู้ซื้อ/ผู้รับจ้าง/ผู้ให้เช่า
    buyer: TBuyerInfo;
    /// ข้อมูลข้อตกลงในสัญญา
    agreement: TAgreementBase;
    /// ข้อมูลเงื่อนไขการชำระเงิน
    payment?: TPaymentBase;
    /// ข้อมูลการหลักประกัน
    guarantee?: TGuaranteeInfo;
    /// ข้อมูลเกี่ยวกับเบี้ยปรับ/ค่าปรับ
    penalty?: TPenaltyInfo;
    /// ข้อมูลการส่งคืนทรัพย์สินหรือการแก้ไขในกรณีที่มีข้อบกพร่องในสัญญา
    redelivery?: TRedelivery;
    /// ข้อมูลการชำระเงินล่วงหน้า
    advancePayment?: TAdvancePayment;
    /// ข้อมูลการส่งมอบ
    delivery?: TDeliveryInfo;
    /// ข้อมูลการรับประกัน
    warranty?: TWarrantyInfo;
    /// ข้อมูลการยกเลิกสัญญาหรือการสิ้นสุดสัญญา
    termination?: TTerminationInfo;
    /// ข้อมูลเช่าบริการเครื่องถ่ายเอกสาร
    copierLease?: TCopierLeaseInfo;
    /// ข้อมูลเช่าบริการคอมพิวเตอร์
    computerLease?: TComputerLeaseInfo;
    /// ข้อมูลเช่าบริการรถยนต์
    carLease?: TCarLeaseInfo;
    /// AttachmentFile
    attachments: TAttachmentBase[];
    /// ผู้ค้า
    vendor: TVendor;

    retentionPayment?: TRetentionPayment;
  }

  acceptors: ParticipantsAcceptor[];

  status: TContractDraftStatus;

  contractDraftDocumentId?: string,

  isContractDraftDocumentIdReplace?: boolean,

  approvalContractDraftDocumentId?: string,

  isApprovalContractDraftDocumentIdReplace?: boolean,

  confidentialContractDraftDocumentId?: string,

  isConfidentialContractDraftDocumentIdReplace?: boolean,

  contractDraftDocumentVersions?: DocumentVersion[];
  approvalContractDraftDocumentVersions?: DocumentVersion[];
  confidentialContractDraftDocumentVersions?: DocumentVersion[];

  egpResult?: boolean;
  egpRemark?: string;
  egpDate?: Date;
  coiResult?: boolean;
  coiRemark?: string;
  coiDate?: Date;
  watchlistResult?: boolean;
  watchlistRemark?: string;
  watchlistDate?: Date;
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  shareholder: Shareholder[];
  checkerAttachments: EntrepreneurAttachments[];
  operators: OperationSection[];
}

export type OperationSection = {
  userId: string;
  operatorType: OperatorType;
  sequence: number;
};

export type TBuyerInfo = {
  name: string;
  address: string;
  province: TLocationInfo;
  district: TLocationInfo;
  subDistrict: TLocationInfo;
}

export type TAgreementBase = {
  type: TAgreementBaseType;
  itemDetail: string;
  vatRateTypeCode: string;
  agreementPrice: number;
  vatAmount?: number;
  totalAmount: number;
  quantity?: number;
  unitCode?: string;
  isExchangeGiver?: boolean;
  duration?: TRentalDurationInfo;
  startDate?: Date;
  endDate?: Date;
  workplaceAddress?: string;
  workplaceProvince?: TLocationInfo;
  workplaceDistrict?: TLocationInfo;
  workplaceSubDistrict?: TLocationInfo;
  brand?: string;
  model?: string;
  serialNumber?: string;
  engineCapacityCc?: number;
}

export type TPaymentBase = {
  type: TPaymentBaseType;
  dueDay?: number;
  redeliveryTypeCode?: string;
  paymentTypeCode?: string;
  details?: TPaymentTermDetail[];
}

export type TPaymentTermDetail = {
  /// รหัส (ถ้าเป็นการแก้ไขจะต้องมีรหัส)
  id?: string;
  /// งวดที่
  no: number;
  /// ระยะเวลา(วัน)
  leadTime: number;
  /// วันที่ส่งมอบ
  deliveryDate?: Date;
  /// ร้อยละ
  installmentPercentage: number;
  /// จำนวนเงิน
  amount: number;
  /// จำนวนเงินหักล่วงหน้า
  advanceDeductionAmount: number;
  /// จำนวนเงินหักประกันผลงาน
  performanceDeductionAmount: number;
  /// รายละเอียด
  description: string;
  /// ลำดับ
  sequence: number;

  periodTypeCode: string;

  isCanEditLeadTime: boolean;
}

export type TGuaranteeInfo = {
  hasGuarantee: boolean;
  typeCode?: string;
  amount: number;
  percentage: number;
  guaranteeDate?: Date;
  referenceNumber?: string;
  referenceDate?: Date;
  bankCode?: string;
  bankBranch?: string;
  bankAccountNumber?: string;
  bankCollateralStartDate?: Date; // DateOnly → string (ISO)
  bankCollateralEndDate?: Date;   // DateOnly → string (ISO)
  otherDetails?: string;
};

export type TPenaltyInfo = {
  isPenalty: boolean;
  typeCode: string;
  rate: number;
  amount: number;
  rateTypeCode: string;
}

export type TRedelivery = {
  type: TRedeliveryBaseType;
  description?: string;
  rentalDuration?: TRentalDurationInfo;
  redeliveryDeadline?: number;
  redeliveryDeadlineTypeCode?: string;
  correctionDue?: number;
  correctionDueTypeCode?: string;
}

export type TAdvancePayment = {
  hasAdvancePayment: boolean;
  amount: number;
  percentage: number;
  dueDate?: number;
  conditionCode?: string;
}

export type TRetentionPayment = {
  hasRetentionPayment: string;
  amount: number;
  percentage: number;
}

export type TDeliveryInfo = {
  address: string;
  date?: Date;
  leadTime?: number;
  leadTimeTypeCode?: string;
  leadOtherTime?: number;
  leadOtherTimeTypeCode?: string;
  countingConditionCode?: string;
  periodTypeCode?: string;
}

export type TWarrantyInfo = {
  hasWarranty: boolean;
  warrantyPeriod: TRentalDurationInfo;
  fixingDeadlinePeriod: TRentalDurationInfo;
  warrantyConditionCode?: string;
  warrantyMonthlyAllowedDowntimeHours?: number;
  warrantyDowntimePercentPerMonth?: number;
  warrantyPenaltyPerHour?: number;
  downtimeResolutionHours?: number;
  downtimeResolutionDay?: number;
  repairCompletionHours?: number;
  repairCompletionDay?: number;
  repairDelayPenaltyPercentPerHour?: number;

  // ข้อมูลสัญญาเช่าบริการคอมพิวเตอร์
  maxMonthlyMalfunction?: number;
  maxMonthlyMalfunctionTypeCode?: string;
  maxMonthlyMalfunctionRate?: number;
  maxMonthlyMalfunctionPenaltyPercentageRate?: number;
  maxMonthlyMalfunctionPenaltyPerHour?: number;
  maxMonthlyMalfunctionPenaltyDueDays?: number;

  warrantyMaintenanceCount?: number;
  warrantyMaintenanceTypeCode?: string;
}

export type TTerminationInfo = {
  startDate: Date;
  endDate: Date;
  duration: TRentalDurationInfo;
  vendorProcessingTime: TRentalDurationInfo;
}

export type TCopierLeaseInfo = {
  monthlyRentPerMachine?: number;
  numberOfMachines?: number;
  totalMonthlyRent?: number;
  estimatedMonthlyCopies?: number;
  belowEstimateCondition?: number;
  perCopyRateCondition?: number;
}

export type TComputerLeaseInfo = {
  duration: TRentalDurationInfo;
  rentalStartCondition: string
}

export type TCarLeaseInfo = {
  rentPerVehicle: number;
  unitCode: string;
}

export type TAttachmentBase = {
  typeCode: string;
  description?: string;
  pageNumber?: number;
  sequence: number;
  formatOtherName?: string;
  files: TAttacementFile[];
  id?: string;
}

export type TAttacementFile = {
  fileId: string;
  fileName: string;
  fileType: string;
  sequence: number;
  pageCount?: number;
}

//เพิ่ม
export type TVendor = {
  type: TVendorType;
  name: string;
  entrepreneurType: string;
  entrepreneurTypeName: string;
  taxpayerIdentificationNo: string;
  sapBranchNumber?: string;
  nationality: TNationalityType;
  tel: string;
  email: string;
  registrationPlace: string;
  vendorRegistrationPlace: string;
  address: string;
  street: string;
  province: string;
  district: string;
  subDistrict: string;
  startDate: Date;
  endDate: Date;
}