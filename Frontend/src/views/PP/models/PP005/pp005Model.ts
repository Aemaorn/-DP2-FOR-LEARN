import type { ParticipantsAcceptor } from "@/models/shared/participants";
import type { OperatorType, PP005Status } from "../../enums/pp005";
import type { JorPor04Request } from "../PP004/pp004Model";
import type { DocumentVersion } from "@/models/shared/document";


export type PP005Detail = {
  procurementId?: string;
  id?: string;
  pJp005Number?: string;
  documentDate?: Date;
  jorPorNumber?: string;
  purchaseRequisition: JorPor04RequestWithOperation;
  jp005: PP005Response;
  status: PP005Status;
  torTemplateCode?: string;
  hasEditPermission: boolean;
};

export type JorPor04RequestWithOperation = {
  purchaseRequisitionId: string;
  operators: Array<OperationSection>;
} & JorPor04Request;

/**
 * **Model จพ.05 ส่วนข้อมูล**
 *  - **( evaluationDueDate )** - การกำหนดระยะเวลาในการพิจารณาผลการเสนอราคาให้แล้วเสร็จภายใน (ระบุเป็นจำนวนตัวเลข )
 *  - **( evaluationPeriodTypeCode )** - หนวยของระยะเวลา ( วัน | เดือน | ปี )
 *  - **( evaluationPeriodConditionCode )** - เงื่อนไขการเริ่มระยะเวลา
 *  - **( egpProjectNumber )** - ( Optional ) เลขที่โครงการ e-GP ( ระบุเมื่อเป็นเงื่อนไข วิธีการจัดหา = 60 และ วงเงินงบประมาณมากกว่า 100,000 )
 *  - **( procurementCommittees )** - ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง
 *  - **( inspectionCommittees )** - ผูู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ
 */
export type PP005Response = {
  documentDate?: Date;
  acceptors: Array<ParticipantsAcceptor>;
  procurementCommittees: CommitteeDuty;
  inspectionCommittees: CommitteeDuty;
  isHasMaintenanceInspectionCommittee: boolean;
  isHasConstructionSupervisor: boolean;
  maintenanceInspectionCommittee: CommitteeDuty;
  isConstructionSupervisor: boolean;
  constructionSupervisor: CommitteeDuty;
  jp005ApprovalDocumentId?: string;
  isJp005ApprovalDocumentIdReplaced?: boolean;
  jp005CommandDocumentId?: string;
  isJp005CommandDocumentIdReplaced?: boolean;
  procurementSuppliesDivision: Array<ProcurementSuppliesDivision>;
  approvalDocumentVersions?: DocumentVersion[];
  commandDocumentVersions?: DocumentVersion[];
} & Evaluations;

export type CommitteeDuty = {
  committees: CommitteeInfo[];
  duties: DutyInfo[];
  isCommittee: boolean;
};

export type ProcurementSuppliesDivision = {
  id?: string;
  userId: string;
  fullName: string;
  fullPositionName?: string;
  sequence: number;
}

export type CommitteeInfo = {
  id?: string;
  userId: string;
  fullName: string;
  fullPositionName: string;
  committeePositionsCode?: string;
  committeePositionName?: string;
  sequence: number;
};

export type DutyInfo = {
  id?: string;
  description: string;
  sequence: number;
};

export type OperationSection = {
  userId: string;
  operatorType: OperatorType;
  sequence: number;
};

export type PP005Payload = {
  purchaseRequisitionId: string;
  documentDate?: Date;
  status: PP005Status;
  evaluations: Evaluations;
  procurementCommittees: CommitteeDuty;
  inspectionCommittees: CommitteeDuty;
  acceptors: Array<ParticipantsAcceptor>;
  egpProjectNumber?: string;
  procurementSuppliesDivision: Array<ProcurementSuppliesDivision>;
  jorPorNumber?: string;
  prNumber?: string;
  telephone?: string;
  description?: string;
  priceReasonablenessInfo?: string;
  medianPriceAmount?: number;
};

/**
 *  - **( evaluationDueDate )** - การกำหนดระยะเวลาในการพิจารณาผลการเสนอราคาให้แล้วเสร็จภายใน (ระบุเป็นจำนวนตัวเลข )
 *  - **( evaluationPeriodTypeCode )** - หนวยของระยะเวลา ( วัน | เดือน | ปี )
 *  - **( evaluationPeriodConditionCode )** - เงื่อนไขการเริ่มระยะเวลา
 */
export type Evaluations = {
  evaluationDueDate: number;
  evaluationPeriodTypeCode: string;
  evaluationPeriodConditionCode: string;
  egpProjectNumber?: string;
}