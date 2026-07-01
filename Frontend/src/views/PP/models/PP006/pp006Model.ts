import type { TNationalityType, TVendorType } from "@/models/ST/st003";
import type { PP006Status, QualificationResult } from "../../enums/pp006";
import type { ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { OnlyFileAttachment } from "@/models/shared/uploadFile";
import type { DocumentVersion } from "@/models/shared/document";

export type PP006Detail = {
  id?: string;
  procurementId: string;
  isInvite?: boolean;
  documentDate?: Date;
  submitProposalStartDate?: Date;
  submitProposalEndDate?: Date;
  submitProposalStartTime?: Date;
  submitProposalEndTime?: Date;
  needToKnowWithinDate?: Date;
  clarifyDetailViaDate?: Date;
  startTime?: string;
  endTime?: string;
  phoneNumber?: string;
  documentId?: string;
  status: PP006Status;
  acceptors: AcceptorInvite[];
  invitedEntrepreneurs: InvitedEntrepreneurs[];
  hasEditPermission: boolean;
  isDocumentReplace: boolean;
};

export type AcceptorInvite = {
  employeeCode: string;
} & ParticipantsCommitteeAcceptor

export type InvitedEntrepreneurs = {
  id?: string;
  vendorId?: string;
  sequence: number;
  entrepreneurTaxId?: string;
  entrepreneurType?: string;
  entrepreneurName?: string;
  entrepreneurEmail?: string;
  sapBranchNumber?: string;
  emailSend: boolean;
  shareholders?: Shareholder[];
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  email?: string;
  emailTemplate?: string;
  attachments?: OnlyFileAttachment[];
  documentId?: string;
  isDocumentReplace?: boolean;
  documentVersions?: DocumentVersion[];
} & EnterpreneurShowData & WatchList & COI & Egp;

export type WatchList = {
  watchlistResult?: boolean;
  watchlistResultRemark?: string;
  watchlistResultAt?: Date;
}

export type COI = {
  coiResult?: boolean;
  coiResultRemark?: string;
  coiResultAt?: Date;
}

export type Egp = {
  egpResult?: boolean;
  egpResultRemark?: string;
  egpResultAt?: Date;
}

export type EnterpreneurShowData = {
  type: TVendorType;
  entrepreneurType?: string;
  entrepreneurTypeLabel?: string;
  sapVendorNumber: string;
  sapBranchNumber: string;
  nationality: TNationalityType;
  tel?: string;
}

export type Shareholder = {
  id: string;
  sequence: number;
  taxId: string;
  firstName: string;
  lastName?: string;
  checkType?: string;
  isDirectorOr20PctShareholder?: boolean;
  isDirector?: boolean;
  isShareholder?: boolean;
  isJuristic?: boolean;
  watchlistResult?: boolean;
  watchlistResultRemark: string;
  watchlistResultAt: Date;
  coiResult?: boolean;
  coiResultRemark: string;
  coiResultAt: Date;
  egpResult?: boolean;
  egpRemark: string;
  egpResultAt: Date;
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  coiCheckerResults?: QualificationResultDto[];
  watchlistCheckerResults?: QualificationResultDto[];
};

export type QualificationResultDto = {
  result: QualificationResult;
  resultAt: Date;
  remark?: string;
}
