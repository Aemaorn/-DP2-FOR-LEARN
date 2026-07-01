import type { Option } from "@/models/shared/option";
import type { TNationalityType, TVendorType } from "@/models/ST/st003";

const nationalityNameBycode = (value: TNationalityType): string => {
  switch (value) {
    case "TH":
      return 'ไทย';
    case 'Foreign':
      return 'ต่างชาติ';
    default:
      return 'เกิดข้อผิดพลาด';
  };
};

const typeNameByCode = (value: TVendorType): string => {
  switch (value) {
    case 'Individual':
      return 'บุคคลธรรมดา';
    case 'JuristicPerson':
      return 'นิติบุคคล';
    case 'Consortium':
      return 'กิจการค้าร่วม (Consortium)';
    case 'JointVenture':
      return 'กิจการร่วมค้า (Joint Venture)';
    default:
      return 'เกิดข้อผิดพลาด';
  };
};

const nationalityOptions = [
  { value: 'TH', label: nationalityNameBycode('TH') },
  { value: 'Foreign', label: nationalityNameBycode('Foreign') },
] as Option[];

const vendorTypeOptions = [
  { value: 'Individual', label: typeNameByCode('Individual') },
  { value: 'JuristicPerson', label: typeNameByCode('JuristicPerson') },
  { value: 'Consortium', label: typeNameByCode('Consortium') },
  { value: 'JointVenture', label: typeNameByCode('JointVenture') },
] as Option[];

const VendorConstants = {
  nationalityOptions,
  vendorTypeOptions,
  nationalityNameBycode,
  typeNameByCode,
};

export default VendorConstants;