import { Cam01PoStep, Cam01Status, Cam01Type } from "@/enums/CAM/CAM01/cam01";
import { CAM01Helper } from "@/helpers/CAM/cam01";
import type { ColorClass, ColorLabel } from "@/models/shared/color";

const { ContractTypeName } = CAM01Helper;

const Cam01StatusName = (status?: Cam01Status): string => {
  switch (status) {
    case Cam01Status.All:
      return "ทั้งหมด";
    case Cam01Status.Draft:
      return "แบบร่าง";
    case Cam01Status.InProgress:
      return "กำลังดำเนินการ";
    case Cam01Status.Completed:
      return "เสร็จสิ้น";
    default:
      return "เกิดข้อผิดพลาด";
  }
};

const Cam01BadgeStatus = (value?: Cam01Status): ColorLabel => {
  const label = Cam01StatusName(value);

  switch (value) {
    case Cam01Status.All:
    case Cam01Status.Draft:
      return { color: "gray", label };

    case Cam01Status.InProgress:
      return { color: "yellow", label };

    case Cam01Status.Completed:
      return { color: "green", label };

    default:
      return { color: "red", label };
  }
};

const Cam01BadgeType = (value: Cam01Type): ColorLabel => {
  const label = ContractTypeName(value);

  return { color: "sky", label };
};

const Cam01PoProgramName = (value: Cam01PoStep): string => {
  switch (value) {
    case Cam01PoStep.PoAddendum:
      return "บันทึกรายละเอียดต่อท้ายสัญญา";
    case Cam01PoStep.PoSap:
      return "บันทึกเลขที่สัญญา PO (SAP)";
    default:
      return "Unknown";
  }
};

const Cam01ListStatusColor = (value: Cam01Status): ColorClass => {
  switch (value) {
    case Cam01Status.All:
      return { bgColorClass: 'bg-gray-100', textColorClass: 'text-gray-500' };
    case Cam01Status.Draft:
      return { bgColorClass: 'bg-gray-200', textColorClass: 'text-gray-600' };
    case Cam01Status.InProgress:
      return { bgColorClass: 'bg-yellow-200', textColorClass: 'text-yellow-600' };
    case Cam01Status.Completed:
      return { bgColorClass: 'bg-green-200', textColorClass: 'text-green-600' };
  }
};

const Cam01Constants = {
  Cam01StatusName,
  Cam01BadgeStatus,
  Cam01BadgeType,
  Cam01PoProgramName,
  Cam01ListStatusColor,
};

export default Cam01Constants;
