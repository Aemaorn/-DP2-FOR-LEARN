import { Cam01Type } from "@/enums/CAM/CAM01/cam01";

const ContractTypeName = (value: Cam01Type) => {
  switch (value) {
    case Cam01Type.ChangeContractDetails:
      return 'เปลี่ยนแปลงรายละเอียดในสัญญา';

    case Cam01Type.AppendNewPurchaseOrder:
      return 'เพิ่ม PO ใหม่ต่อท้าย';

    case Cam01Type.WaiveOrReducePenalty:
      return 'ขออนุมัติงด/ลดค่าปรับ';

    case Cam01Type.AdjustContractDuration:
      return 'การขยายหรือลดระยะเวลาสัญญา';

    default:
      return 'เกิดข้อผิดพลาด';
  }
}

export const CAM01Helper = {
  ContractTypeName
};