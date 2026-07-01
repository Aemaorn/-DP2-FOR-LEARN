export const committeeGroupTypeMappingName = (committeeType: string) => {
  switch (committeeType) {
    case 'TOR':
      return 'บุคคล/คณะกรรมการจัดทำร่างขอบเขตงาน';
    case 'MedianPrice':
      return 'บุคคล/คณะกรรมการกำหนดราคากลาง';
    case 'ProcurementCommittee':
      return 'ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง';
    case 'InspectionCommittee':
      return 'ผูู้ตรวจรับ/คณะกรรมการตรวจรับพัสดุ';
    case 'MaintenanceInspectionCommittee':
      return 'คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)';
    case 'ConstructionSupervisor':
      return 'ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)';
    case 'RentCommittee':
      return 'บุคคล/คณะกรรมการจัดเช่า';
    case 'AcceptanceCommittee':
      return 'บุคคล/คณะกรรมการตรวจรับ';
    default:
      return committeeType;
  }
}