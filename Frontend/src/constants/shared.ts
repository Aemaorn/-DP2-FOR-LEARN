import { EWorkProcess, EDateType } from "@/enums/shared";
import type { Option } from "@/models/shared/option";

const WorkProcessName = (value: EWorkProcess) => {
  switch (value) {
    case EWorkProcess.InProcess:
      return "อยู่ระหว่างดำเนินการ (In Process)";
    case EWorkProcess.Related:
      return "ดำเนินการแล้ว (Related)";
    case EWorkProcess.Completed:
      return "ดำเนินการแล้วเสร็จ (Completed)";
    case EWorkProcess.All:
      return "งานทั้งหมด (All)";
  }
};

const PeriodTypeName = (value: EDateType) => {
  switch (value) {
    case EDateType.Year:
      return "ปี";
    case EDateType.Month:
      return "เดือน";
    case EDateType.Day:
      return "วัน";
    default: return "เกิดข้อผิดพลาด";
  }
};

const WorkProcessOptions = Object.entries(EWorkProcess).map(([, value]) => ({
  label: WorkProcessName(value),
  value: value,
} as Option));

const DateTypeOptions = Object.entries(EDateType).map(([, value]) => ({
  label: PeriodTypeName(value),
  value: value,
} as Option));

const HasOptions = [
  { label: 'มี', value: true, },
  { label: 'ไม่มี', value: false, }
];

const SharedConstants = {
  WorkProcessOptions,
  DateTypeOptions,
  HasOptions,
};

export default SharedConstants;