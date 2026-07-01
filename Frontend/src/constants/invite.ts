import type { ColorLabel } from "@/models/shared/color";
import { PP006Status } from "@/views/PP/enums/pp006";

const inviteStatusName = (value: PP006Status, isSixtyMorethan100k: boolean): string => {
  const waitingLabel = isSixtyMorethan100k ? 'รอหัวหน้าส่วนเห็นชอบ' : 'รอผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้างเห็นชอบ';
  switch (value) {
    case PP006Status.Draft:
      return 'แบบร่าง';
    case PP006Status.Rejected:
      return 'ไม่เห็นชอบ';
    case PP006Status.Edit:
      return 'เรียกคืนแก้ไข';
    case PP006Status.WaitingApproval:
      return waitingLabel;
    case PP006Status.Approved:
      return 'อนุมัติ';
    case PP006Status.NotInvited:
      return 'ไม่เชิญชวน';
  }
};

const inviteStatusColor = (value: PP006Status, isSixtyMorethan100k: boolean): ColorLabel => {
  const label = inviteStatusName(value, isSixtyMorethan100k);
  switch (value) {
    case PP006Status.Draft:
      return { color: 'gray', label };
    case PP006Status.Rejected:
      return { color: 'red', label };
    case PP006Status.Edit:
      return { color: 'gray', label };
    case PP006Status.WaitingApproval:
      return { color: 'yellow', label };
    case PP006Status.Approved:
      return { color: 'green', label };
    case PP006Status.NotInvited:
      return { color: 'orange', label };
  }
};

const inviteConstant = {
  inviteStatusName,
  inviteStatusColor,
};

export default inviteConstant
