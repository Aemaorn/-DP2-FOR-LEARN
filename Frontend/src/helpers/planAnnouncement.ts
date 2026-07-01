import { PlanAnnouncementConstants } from "@/constants";
import { PlanAnnouncementStatus } from "@/enums/planAnnouncement";
import type { TPL002StatusCount } from "@/models/PL/pl002";
import type { ColorClass } from "@/models/shared/color";
import type { OptionBadge } from "@/models/shared/option";

const { AnnouncementStatusName } = PlanAnnouncementConstants;

const MapStatusColor = (status: PlanAnnouncementStatus): ColorClass => {
  switch (status) {
    case PlanAnnouncementStatus.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };

    case PlanAnnouncementStatus.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };

    case PlanAnnouncementStatus.WaitingAssign:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };


    case PlanAnnouncementStatus.WaitingAcceptor:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };

    case PlanAnnouncementStatus.WaitingAnnouncement:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };

    case PlanAnnouncementStatus.Announcement:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };

    case PlanAnnouncementStatus.Rejected:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };

    case PlanAnnouncementStatus.Cancelled:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };

    default:
      return { bgColorClass: 'bg-red-200', textColorClass: 'text-red-600' };
  }
};

const statusBadgeColor = (status: PlanAnnouncementStatus): 'Draft' | 'Success' | 'Warning' | 'Info' | 'Error' | undefined => {
  switch (status) {
    case PlanAnnouncementStatus.Draft:
      return 'Draft';
    case PlanAnnouncementStatus.WaitingAssign:
      return 'Info';
    case PlanAnnouncementStatus.WaitingAcceptor:
      return 'Warning';
    case PlanAnnouncementStatus.WaitingAnnouncement:
      return 'Info';
    case PlanAnnouncementStatus.Announcement:
      return 'Success';
    case PlanAnnouncementStatus.Rejected:
      return 'Error';
    default: return 'Error';
  }
}

const announcementStatusAttributes = (count: TPL002StatusCount): OptionBadge[] => {
  const excludeKeys = ["cancelled"];

  return Object.entries(PlanAnnouncementStatus)
    .filter(([key]): boolean => !excludeKeys.includes(key))
    .map(([, value]): OptionBadge => {
      const color = MapStatusColor(value as PlanAnnouncementStatus);
      const statusKey = value.toLowerCase();
      const countLower = Object.fromEntries(
        Object.entries(count).map(([key, val]) => [key.toLowerCase(), val]));

      return {
        label: AnnouncementStatusName(value),
        value: value,
        count: countLower[statusKey] ?? 0,
        bgColorClass: color.bgColorClass,
        textColorClass: color.textColorClass,
      } as OptionBadge;
    });
}

const planAnnouncementHelper = {
  announcementStatusAttributes,
  statusBadgeColor,
};

export default planAnnouncementHelper;