import type { TCommitteeSection, TDutySection } from '../../../../models/PP/ppModel';
import type { AppointStatus } from '../../enums/pp001';
import type { ParticipantsAcceptor } from '@/models/shared/participants';
import type { AcceptorType } from '@/enums/participants';
import type { DocumentVersion } from '@/models/shared/document';

export type TPP001Detail = {
  budget: number;
  appoint: TPP001AppointDto;
  torDraftCommittees: TCommitteeSection[];
  torDraftCommitteeDuties: TDutySection[];
  isTorCommittee: boolean;
  isMedianPriceCommittee: boolean;
  medianPriceCommittees: TCommitteeSection[];
  medianPriceCommitteeDuties: TDutySection[];
  acceptors: ParticipantsAcceptor[];
  appointDocumentId?: string;
  isAppointDocumentIdReplaced?: boolean;
  hasPermission: boolean;
  appointDocumentVersions?: DocumentVersion[];
};

export type TPP001AppointDto = {
  id?: string;
  procurementId: string;
  appointNumber?: string;
  procurementBudgetYear: number;
  memorandumDate: Date;
  memorandumNumber?: string;
  telephone?: string;
  reason?: string;
  status: AppointStatus;
  changeReason?: string;
  cancelReason?: string;
  isChange: boolean
  isCancel: boolean
};

export type TP001SendAction = {
  appointId: string;
  acceptorId: string;
  acceptorType: AcceptorType;
  remark?: string;
}