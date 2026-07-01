import PreProcurementConstants from '@/constants/preProcurement';
import { PreProcurementDialogGroupStep, PreProcurementGroupStep } from '@/enums/preProcurement';
import type {
  TPreProcurementDialogGroupStepCount,
  TPreProcurementGroupStepCount,
} from '@/models/PP/ppModel';
import type { OptionBadge } from '@/models/shared/option';
import procurementHelper from './procurement';
import type { PCM005StatusGroupCount } from '@/models/PCM/PCM005/pcm005';

const { PreProcurementGroupStepName, PreProcurementDialogGroupStepName } = PreProcurementConstants;
const { MapPreProcurementGroupStepColor } = procurementHelper;

const AssignPreProcurementGroupStatusAttributes = (
  count: TPreProcurementGroupStepCount
): OptionBadge[] => {
  return Object.entries(PreProcurementGroupStep).filter(f => f[1] != PreProcurementGroupStep.ContractManagement).map(([, value]): OptionBadge => {
    const statusKey = value as keyof TPreProcurementGroupStepCount;
    const color = MapPreProcurementGroupStepColor(value);

    return {
      label: PreProcurementGroupStepName(value),
      value: value,
      count: count[statusKey] ?? 0,
      bgColorClass: color.bgColorClass,
      textColorClass: color.textColorClass,
    } as OptionBadge;
  });
};

const PcmGroupStatusOptions = (
  count: PCM005StatusGroupCount
): OptionBadge[] => {
  return Object.entries([PreProcurementGroupStep.All, PreProcurementGroupStep.Procurement, PreProcurementGroupStep.ContractAgreement]).map(([, value]): OptionBadge => {
    const statusKey = value as keyof PCM005StatusGroupCount;
    const color = MapPreProcurementGroupStepColor(value);

    return {
      label: PreProcurementGroupStepName(value),
      value: value,
      count: count[statusKey] ?? 0,
      bgColorClass: color.bgColorClass,
      textColorClass: color.textColorClass,
    } as OptionBadge;
  });
};

const AssignPreProcurementDialogGroupStatusAttributes = (
  count: TPreProcurementDialogGroupStepCount
): OptionBadge[] => {
  return Object.entries(PreProcurementDialogGroupStep).map(([, value]): OptionBadge => {
    const statusKey = value as keyof TPreProcurementDialogGroupStepCount;

    return {
      label: PreProcurementDialogGroupStepName(value),
      value: value,
      count: count[statusKey] ?? 0,
      bgColorClass: 'bg-gray-200',
      textColorClass: 'text-gray-600',
    } as OptionBadge;
  });
};

const preProcurementHelper = {
  AssignPreProcurementGroupStatusAttributes,
  AssignPreProcurementDialogGroupStatusAttributes,
  PcmGroupStatusOptions,
};

export default preProcurementHelper;
