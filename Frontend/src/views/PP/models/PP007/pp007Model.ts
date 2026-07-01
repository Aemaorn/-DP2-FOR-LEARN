import type { ParticipantsAssignee, ParticipantsCommitteeAcceptor } from "@/models/shared/participants";
import type { PurchaseOrderStatus } from "../../enums/pp007";
import type { TPaginated } from "@/models/shared/paginated";
import type { TNationalityType, TVendorType } from "@/models/ST/st003";
import type { QualificationResultDto, Shareholder } from "../PP006/pp006Model";
import type { EntrepreneurAttachments } from "@/models/shared/uploadFile";
import type { ProcurementSuppliesDivision } from "../PP005/pp005Model";
import type { TPreProcurementDetail } from "@/models/PP/ppModel";
import type { DocumentVersion } from "@/models/shared/document";

export type PP007Detail = {
  jp006Id?: string;
  procurementId: string;
  jp006DocumentId?: string;
  isJp006DocumentIdReplaced?: boolean;
  winnerDocumentId?: string;
  isWinnerDocumentIdReplaced?: boolean;
  status: PurchaseOrderStatus;
  mediaPriceBudget: number;
  entrepreneurs: PP007Entrepreneurs[];
  acceptors: ParticipantsCommitteeAcceptor[];
  assignees: ParticipantsAssignee[];
  medianPrice?: number;
  isBp: boolean;
  operatorId: string;
  operators: OperationSection[];
  procurementSuppliesDivision: ProcurementSuppliesDivision[];
  procurement: TPreProcurementDetail;
  jp006DocumentVersions?: DocumentVersion[];
  winnerDocumentVersions?: DocumentVersion[];
  purchaseOrderNumber?: string;
  documentDate?: Date;
  priceDetails: PP007PriceDetail[];
}

export type OperationSection = {
  userId: string;
  sequence: number;
};

export type PP007Entrepreneurs = {
  entrepreneurId?: string;
  vendorId?: string;
  emailSended: boolean;
  sequence: number;
  coi: PP007EntrepreneurCheckConditions;
  watchlist: PP007EntrepreneurCheckConditions;
  egp: PP007EntrepreneurCheckConditions;
  isWinner: boolean;
  selectionReasonCode?: string;
  remark?: string;
  priceDetails: PP007PriceDetail[];
  entrepreneurTaxId: string;
  entrepreneurType: string;
  entrepreneurName: string;
  entrepreneurEmail: string;
  sapBranchNumber?: string;
  type: TVendorType;
  entrepreneurNationality: TNationalityType;
  entrepreneurPlaceName: string;
  entrepreneurPhoneNumber?: string;
  isBidding: boolean;
  shareholder?: Shareholder[];
  coiCheckerResult?: QualificationResultDto;
  watchlistCheckerResult?: QualificationResultDto;
  attachments: EntrepreneurAttachments[]
}

export type PP007EntrepreneurCheckConditions = {
  result?: boolean;
  remark?: string;
  date: Date;
}

export type PP007PriceDetail = {
  priceDetailsId?: string;
  sequence: number;
  parcelName: string;
  parcelQuantity: number;
  parcelUnitCode: string;
  vatTypeCode?: string;
  offeredPrice: number;
  offeredPriceSum: number;
  agreedPrice: number;
  agreedPriceSum: number;
  description: string;
}

export type PP007GetWinnerCriteria = {
  procurementId: string;
  jp006Id: string;
  keyword?: string;
  type?: string;
  isRental?: boolean;
} & TPaginated

export type PP007GetWinnerResponse = {
  id: string;
  type: string;
  taxId: string;
  name: string;
  agreedPrice: number;
  email?: string;
}