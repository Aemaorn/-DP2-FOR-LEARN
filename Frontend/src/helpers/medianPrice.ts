import type { ColorLabel } from "@/models/shared/color";
import { MedianPriceAccordion, PP003Status } from "@/views/PP/enums/pp003";


const StatusName = (value: PP003Status): string => {
  switch (value) {
    case PP003Status.Draft:
      return 'แบบร่าง';
    case PP003Status.Edit:
      return 'เรียกคืนแก้ไข';
    case PP003Status.WaitingCommitteeApproval:
      return 'รอบุคคล/คณะกรรมการกำหนดราคากลางเห็นชอบ';
    case PP003Status.WaitingUnitApproval:
      return 'รอสายงานเห็นชอบ';
    case PP003Status.WaitingAssign:
      return 'รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมาย';
    case PP003Status.WaitingComment:
      return 'ยืนยันมอบหมาย';
    case PP003Status.RejectToAssignee:
      return 'ส่งกลับแก้ไขผู้รับผิดชอบ';
    case PP003Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PP003Status.Approved:
      return 'อนุมัติ';
    case PP003Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case PP003Status.Cancelled:
      return 'ยกเลิกรายการ';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const AccordionName = (value: MedianPriceAccordion): string => {
  switch (value) {
    case MedianPriceAccordion.MedianPriceCommittee:
      return 'บุคคล/คณะกรรมการกำหนดราคากลาง';
    case MedianPriceAccordion.Units:
      return 'สายงานเห็นชอบ';
    case MedianPriceAccordion.JorPorSuggestion:
      return 'เจ้าหน้าที่พัสดุให้ความเห็น';
    case MedianPriceAccordion.Acceptor:
      return 'ผู้มีอำนาจเห็นชอบ/อนุมัติ';
    default: return 'เกิดข้อผิดพลาด';
  }
};

const MapStatusColor = (status: PP003Status): ColorLabel => {
  switch (status) {
    case PP003Status.Draft:
      return { label: StatusName(status), color: 'gray' };
    case PP003Status.Edit:
      return { label: StatusName(status), color: 'orange' };
    case PP003Status.WaitingCommitteeApproval:
      return { label: StatusName(status), color: 'yellow' };
    case PP003Status.WaitingUnitApproval:
      return { label: StatusName(status), color: 'blue' };
    case PP003Status.WaitingApproval:
      return { label: StatusName(status), color: 'purple' };
    case PP003Status.WaitingAssign:
      return { label: StatusName(status), color: 'cyan' };
    case PP003Status.Approved:
      return { label: StatusName(status), color: 'green' };
    case PP003Status.WaitingComment:
      return { label: StatusName(status), color: 'emerald' };
    case PP003Status.Cancelled:
      return { label: StatusName(status), color: 'slate' };
    case PP003Status.RejectToAssignee:
      return { label: StatusName(status), color: 'red' };
    case PP003Status.Rejected:
      return { label: StatusName(status), color: 'red' };
    default: return { label: StatusName(status), color: 'red' };
  }
};

const MedianPriceHelper = {
  MapStatusColor,
  StatusName,
  AccordionName,
};

export default MedianPriceHelper;