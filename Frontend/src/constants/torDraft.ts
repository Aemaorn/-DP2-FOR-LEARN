import type { ColorLabel } from "@/models/shared/color";
import { PP002Status } from "@/views/PP/enums/pp002";

const TorDraftStatusName = (value: PP002Status): string => {
  switch (value) {
    case PP002Status.Draft:
      return 'แบบร่าง';
    case PP002Status.Rejected:
      return 'ส่งกลับแก้ไข';
    case PP002Status.Edit:
      return 'เรียกคืนแก้ไข';
    case PP002Status.WaitingCommitteeApproval:
      return 'รอบุคคล/คณะกรรมการจัดทำร่างขอบเขตของงานเห็นชอบ';
    case PP002Status.WaitingUnitApproval:
      return 'รอสายงานเห็นชอบ';
    case PP002Status.WaitingAssign:
      return 'รอเจ้าหน้าที่พัสดุให้ความเห็นมอบหมาย';
    case PP002Status.WaitingComment:
      return 'ยืนยันมอบหมายงาน';
    case PP002Status.RejectToAssignee:
      return 'ส่งกลับแก้ไขผู้รับผิดชอบ';
    case PP002Status.WaitingApproval:
      return 'รอผู้มีอำนาจเห็นชอบ/อนุมัติ';
    case PP002Status.Approved:
      return 'อนุมัติ';
  }
};

const TorDraftStatusColor = (value: PP002Status): ColorLabel => {
  const label = TorDraftStatusName(value);
  switch (value) {
    case PP002Status.Draft:
      return { color: 'gray', label };
    case PP002Status.Rejected:
      return { color: 'red', label };
    case PP002Status.Edit:
      return { color: 'yellow', label };
    case PP002Status.WaitingCommitteeApproval:
      return { color: 'yellow', label };
    case PP002Status.WaitingUnitApproval:
      return { color: 'yellow', label };
    case PP002Status.WaitingAssign:
      return { color: 'yellow', label };
    case PP002Status.WaitingComment:
      return { color: 'yellow', label };
    case PP002Status.RejectToAssignee:
      return { color: 'red', label };
    case PP002Status.WaitingApproval:
      return { color: 'yellow', label };
    case PP002Status.Approved:
      return { color: 'green', label };
  }
};

const TorDraftConstant = {
  TorDraftStatusName,
  TorDraftStatusColor,
};

export default TorDraftConstant
