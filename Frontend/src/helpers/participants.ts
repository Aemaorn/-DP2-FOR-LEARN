import type { ParticipantsAcceptor } from "@/models/shared/participants";
import { AcceptorStatus, AcceptorType } from "@/enums/participants";

export const getCurrentPendingAcceptor = (
  acceptors: ParticipantsAcceptor[],
  acceptorType?: AcceptorType
): ParticipantsAcceptor | undefined =>
  acceptors
    .filter((a): boolean =>
      a.status === AcceptorStatus.Pending &&
      (acceptorType === undefined || a.acceptorType === acceptorType)
    )
    .sort((a, b): number => a.sequence - b.sequence)[0];

export const isCurrentPendingAcceptor = (
  acceptors: ParticipantsAcceptor[],
  userId: string,
  acceptorType?: AcceptorType
): boolean => {
  const acceptor = getCurrentPendingAcceptor(acceptors, acceptorType);
  return acceptor
    ? (acceptor.delegateeUserId ?? acceptor.userId) === userId
    : false;
};
