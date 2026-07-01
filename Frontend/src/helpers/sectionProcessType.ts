import { SectionProcessType } from '@/enums/operations';
import type { Option } from '@/models/shared/option';

/**
 * Converts SectionProcessType enum to Option array for select components
 * @returns Array of options with label and value
 */
export function getSectionProcessTypeOptions(): Option[] {
  return [
    {
      label: 'แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้าง',
      value: SectionProcessType.AppointPreProcurement,
    },
    {
      label: 'แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้างสำหรับที่ดินเชิงพาณิชย์',
      value: SectionProcessType.AppointPreProcurementCommercialParcel,
    },
    {
      label: 'แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้างสำหรับพัสดุคงคลัง',
      value: SectionProcessType.AppointPreProcurementStock,
    },
    {
      label: 'แผนงาน',
      value: SectionProcessType.Plan,
    },
    {
      label: 'ขอบเขตงาน (Terms of Reference)',
      value: SectionProcessType.TOR,
    },
    {
      label: 'ขอบเขตงานสำหรับที่ดินเชิงพาณิชย์',
      value: SectionProcessType.TORCommercialParcel,
    },
    {
      label: 'ขอบเขตงานสำหรับพัสดุคงคลัง',
      value: SectionProcessType.TORStock,
    },
    {
      label: 'ขอบเขตงาน กรณี MD',
      value: SectionProcessType.TORHasMD,
    },
    {
      label: 'ขอบเขตงานสำหรับที่ดินเชิงพาณิชย์ กรณี MD',
      value: SectionProcessType.TORCommercialParcelHasMD,
    },
    {
      label: 'ราคากลาง',
      value: SectionProcessType.MedianPrice,
    },
    {
      label: 'ราคากลาง กรณี 4 ฝ่ายตามตารางแนบท้าย',
      value: SectionProcessType.MedianPriceCommercialParcel,
    },
    {
      label: 'ราคากลาง กรณี MD',
      value: SectionProcessType.MedianPriceHasMD,
    },
    {
      label: 'ราคากลาง กรณี 4 ฝ่ายตามตารางแนบท้าย กรณี MD',
      value: SectionProcessType.MedianPriceCommercialParcelHasMD,
    },
    {
      label: 'ราคากลาง Stock',
      value: SectionProcessType.MedianPriceStock,
    },
    {
      label: 'จพ.05',
      value: SectionProcessType.ApprovePurchaseRequest,
    },
    {
      label: 'จพ. 005 กรณี 4 ฝ่ายตามตารางแนบท้าย',
      value: SectionProcessType.ApprovePurchaseRequestCommercialParcel,
    },
    {
      label: 'จพ. 006, 119, 79วรรคสอง, pettycash',
      value: SectionProcessType.PurchaseOrder,
    },
    {
      label: 'จพ. 006 กรณี 4 ฝ่ายตามตารางแนบท้าย',
      value: SectionProcessType.PurchaseOrderCommercialParcel,
    },
    {
      label: 'อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา',
      value: SectionProcessType.ApprovePurchaseOrder,
    },
    {
      label: 'อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา ตามตารางแนบท้าย',
      value: SectionProcessType.ApprovePurchaseOrderCommercialParcel,
    },
    {
      label: 'หนังสือเชิญชวนทำสัญญา',
      value: SectionProcessType.ContractInvitation,
    },
    {
      label: 'ร่างสัญญาและสัญญา',
      value: SectionProcessType.ContractDraft,
    },
    {
      label: 'บันทึกส่งมอบและตรวจรับ',
      value: SectionProcessType.DeliveryAcceptancePeriod,
    },
    {
      label: 'บันทึกส่งมอบและตรวจรับ กรณี 4 ฝ่ายตามตารางแนบท้าย',
      value: SectionProcessType.DeliveryAcceptancePeriodCommercialParcel,
    },
    {
      label: 'บันทึกส่งมอบและตรวจรับ กรณีมีค่าปรับ',
      value: SectionProcessType.DeliveryAcceptancePeriodPenalty,
    },
    {
      label: 'คืนหลักประกันสัญญา',
      value: SectionProcessType.ContractGuaranteeReturn,
    },
    {
      label: 'ขออนุมัติหลักการ',
      value: SectionProcessType.PrincipleRentalApproval,
    },
    {
      label: 'ขออนุมัติเช่า',
      value: SectionProcessType.RentalApproval,
    },
    {
      label: 'เบิกจ่าย (สำหรับบัญชี)',
      value: SectionProcessType.ExpenseDisbursement,
    },
    {
      label: 'บันทึกต่อท้ายสัญญา',
      value: SectionProcessType.ContractAmendment,
    },
    {
      label: 'ขออนุมัติบอกเลิกสัญญา',
      value: SectionProcessType.ContractTermination,
    },
  ];
}

/**
 * Gets the default SectionProcessType value for new approvers
 * @returns The default process type value
 */
export function getDefaultSectionProcessType(): string {
  return SectionProcessType.AppointPreProcurement;
}
