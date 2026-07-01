import ProcurementConstants from '@/constants/procurement';
import { ProcurementStatus } from '@/enums/procurement';
import type { TPL002StatusCount } from '@/models/PL/pl002';
import type { ColorClass, ColorLabel } from '@/models/shared/color';
import type { OptionBadge } from '@/models/shared/option';
import type { Pcm002StatusCount } from '@/models/PCM/pcm002';
import type { Pcm003StatusCount } from '@/models/PCM/pcm003';
import { PreProcurementGroupStep, PreProcurementStep } from '@/enums/preProcurement';
import type { Pcm004StatusCount } from '@/models/PCM/pcm004';
import { PreProcurementConstants } from '@/constants';

const { ProcurementStatusName } = ProcurementConstants;
const { PreProcurementStepFullName } = PreProcurementConstants;

const MapStatusColor = (status: ProcurementStatus): ColorClass => {
  switch (status) {
    case ProcurementStatus.All:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-black' };

    case ProcurementStatus.Draft:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };

    case ProcurementStatus.InProgress:
      return { bgColorClass: 'bg-yellow-400', textColorClass: 'text-white' };

    case ProcurementStatus.Completed:
      return { bgColorClass: 'bg-green-400', textColorClass: 'text-white' };

    case ProcurementStatus.Cancelled:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };

    default:
      return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' };
  }
};

const ProcurementBadgeStatus = (value: ProcurementStatus): ColorLabel => {
  switch (value) {
    case ProcurementStatus.All:
      return { label: ProcurementStatusName(value), color: 'gray' };

    case ProcurementStatus.Draft:
      return { label: ProcurementStatusName(value), color: 'gray' };

    case ProcurementStatus.InProgress:
      return { label: ProcurementStatusName(value), color: 'yellow' };

    case ProcurementStatus.Completed:
      return { label: ProcurementStatusName(value), color: 'green' };

    case ProcurementStatus.Cancelled:
      return { label: ProcurementStatusName(value), color: 'gray' };

    default:
      return { label: 'เกิดข้อผิดพลาด', color: 'red' };
  }
}

const ChildStatusBadge = (value: string): ColorLabel => {
  switch (value) {
    case 'Draft':
    case 'Edit':
      return { label: 'แบบร่าง', color: 'gray' };

    case 'WaitingApproval':
    case 'WaitingCommitteeApproval':
    case 'WaitingComment':
    case 'WaitingAssign':
    case 'Assigned':
    case 'SendApprove':
    case 'SendEdit':
    case 'InProgress':
      return { label: 'รออนุมัติ', color: 'yellow' };

    case 'Approved':
    case 'Completed':
    case 'Announcement':
      return { label: 'อนุมัติแล้ว', color: 'green' };

    case 'Rejected':
      return { label: 'ส่งกลับแก้ไข', color: 'red' };

    case 'Cancelled':
    case 'Cancel':
      return { label: 'ยกเลิก', color: 'gray' };

    default:
      return { label: value, color: 'gray' };
  }
}


const ProcurementBageStep = (value: PreProcurementStep): ColorLabel => {
  return { label: PreProcurementStepFullName(value), color: 'gray' }
}

const ProcurementStepColor = (): ColorClass => {
  return { bgColorClass: 'bg-gray-400', textColorClass: 'text-white' }
}

const MapPreProcurementGroupStepColor = (status: PreProcurementGroupStep): ColorClass => {
  switch (status) {
    case PreProcurementGroupStep.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };

    case PreProcurementGroupStep.PreProcurement:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };

    case PreProcurementGroupStep.Procurement:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };

    case PreProcurementGroupStep.ContractAgreement:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };


    case PreProcurementGroupStep.ContractManagement:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };

    default:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
  }
};

const ApproveProcurementStatusAttributes = (count: TPL002StatusCount): OptionBadge[] => {
  const excludeKeys = ['SendAppprove', 'MakeDocument', 'Approved', 'Cancel'];

  return Object.entries(ProcurementStatus)
    .filter(([key]): boolean => !excludeKeys.includes(key))
    .map(([, value]): OptionBadge => {
      const color = MapStatusColor(value as ProcurementStatus);
      const statusKey = value as keyof TPL002StatusCount;

      return {
        label: ProcurementStatusName(value),
        value: value,
        count: count[statusKey] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const pcm002StatusAttributes = (count: Pcm002StatusCount): OptionBadge[] => {
  const status = ['all', 'draft', 'sendAppprove', 'approved', 'announcement', 'sendEdit', 'cancel'];

  return Object.entries(ProcurementStatus)
    .filter(([, value]): boolean => status.includes(value))
    .map(([, value]): OptionBadge => {
      const color = MapStatusColor(value as ProcurementStatus);
      const statusKey = value as keyof Pcm002StatusCount;

      return {
        label: ProcurementStatusName(value),
        value: value,
        count: count[statusKey] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const pcm003StatusAttributes = (count: Pcm003StatusCount) => {
  const status = ['all', 'draft', 'sendApprove', 'approved', 'sendEdit', 'cancel'];

  return Object.entries(ProcurementStatus)
    .filter(([, value]) => status.includes(value))
    .map(([, value]) => {
      const color = MapStatusColor(value as ProcurementStatus);
      const statusKey = value as keyof Pcm003StatusCount;

      return {
        label: ProcurementStatusName(value),
        value: value,
        count: count[statusKey] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const pcm004StatusAttributes = (count: Pcm004StatusCount) => {
  const status = ['all', 'draft', 'edit', 'sendApprove', 'approved', 'sendEdit', 'cancel'];

  return Object.entries(ProcurementStatus)
    .filter(([, value]) => status.includes(value))
    .map(([, value]) => {
      const color = MapStatusColor(value as ProcurementStatus);
      const statusKey = value as keyof Pcm004StatusCount;

      return {
        label: ProcurementStatusName(value),
        value: value,
        count: count[statusKey] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
};

const procurementHelper = {
  ApproveProcurementStatusAttributes,
  pcm002StatusAttributes,
  pcm003StatusAttributes,
  pcm004StatusAttributes,
  MapPreProcurementGroupStepColor,
  ProcurementBadgeStatus,
  ChildStatusBadge,
  ProcurementBageStep,
  ProcurementStepColor
};

export default procurementHelper;