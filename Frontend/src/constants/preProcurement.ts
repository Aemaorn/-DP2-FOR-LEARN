import {
  PreProcurementType,
  PreProcurementStep,
  PreProcurementGroupStep,
  ConsiderOverPriceType,
  PreProcurementDialogStep,
  PreProcurementDialogGroupStep,
} from '@/enums/preProcurement';
import type { Option } from '@/models/shared/option';

const PreProcurementTypeName = (value: PreProcurementType) => {
  switch (value) {
    case PreProcurementType.All:
      return 'ทั้งหมด';
    case PreProcurementType.AnnualPlan:
      return 'แผนรวมปี';
    case PreProcurementType.InYearPlan:
      return 'แผนระหว่างปี';
  }
};

const PreProcurementStepShortName = (value: string): string => {
  switch (value) {
    // ----------Pre-Procurement-------
    case PreProcurementStep.Appoint:
      return 'ขอแต่งตั้งบุคลล/คกก.';
    case PreProcurementStep.TorDraft:
      return 'ร่างขอบเขตงาน';
    case PreProcurementStep.MedianPrice:
      return 'กำหนดราคากลาง';
    case PreProcurementStep.PurchaseRequisition:
      return 'จพ.004';

    // ------------Procurement----------
    case PreProcurementStep.Jp005:
      return 'จพ.005';
    case PreProcurementStep.Invite:
      return 'เชิญชวนผู้ประกอบการ';
    case PreProcurementStep.PurchaseOrder:
      return 'จพ.006';
    case PreProcurementStep.PurchaseOrderApproval:
      return 'อนุมัติใบสั่ง/แจ้งทำสัญญา';
    case PreProcurementStep.PrincipleApproval:
      return 'ขออนุมัติหลักการ'
    case PreProcurementStep.PrincipleApprovalRental:
      return 'ขออนุมัติเช่า'
    case PreProcurementStep.W119:
      return 'รายการจัดซื้อจัดจ้าง ว119'
    case PreProcurementStep.P79Clause2:
      return 'รายการจัดซื้อจัดจ้าง กรณีเร่งด่วน'
    case PreProcurementStep.PettyCash:
      return 'รายการจัดซื้อจัดจ้าง Petty Cash'
    case PreProcurementStep.PettyCashReimbursement:
      return 'รายการเบิกเงินชดเชยเงินสดย่อย'

    // ------Contract Agreement-------
    case PreProcurementStep.ContractInvitation:
      return 'เชิญชวนทำสัญญา';
    case PreProcurementStep.ContractDraft:
      return 'ร่างสัญญา';

    // ------Contract Management---------
    case PreProcurementStep.contractReceive:
      return 'ส่งมอบตรวจรับ';
    case PreProcurementStep.contractExpense:
      return 'ขออนุมัติเบิกจ่าย';
    case PreProcurementStep.contractReturnCollateral:
      return 'คืนหลักประกันสัญญา';
    default:
      return value;
  }
};

const PreProcurementStepFullName = (value: string, isRent?: boolean): string => {
  const purchaseOrderApprovalLabel = isRent ? 'อนุมัติใบสั่งเช่า และแจ้งทำสัญญา' : 'อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา';

  switch (value) {
    // ----------Pre-Procurement-------
    case PreProcurementStep.Appoint:
      return 'ขอแต่งตั้งบุคคล/คกก.';
    case PreProcurementStep.TorDraft:
      return 'ร่างขอบเขตของงาน (TOR)';
    case PreProcurementStep.MedianPrice:
      return 'กำหนดราคากลาง (ราคาอ้างอิง)';
    case PreProcurementStep.PurchaseRequisition:
      return 'การแจ้งข้อมูลเบื้องต้น (จพ.004)';
    // ------------Procurement----------
    case PreProcurementStep.Jp005:
      return 'รายงานขอซื้อขอจ้าง (จพ.005)';
    case PreProcurementStep.Invite:
      return 'หนังสือเชิญชวนผู้ประกอบการ';
    case PreProcurementStep.PurchaseOrder:
      return 'ขออนุมัติสั่งซื้อ/สั่งจ้าง (จพ.006)';
    case PreProcurementStep.PurchaseOrderApproval:
      return purchaseOrderApprovalLabel;
    // ------Contract Agreement-------
    case PreProcurementStep.ContractInvitation:
      return 'หนังสือเชิญชวนทำสัญญา';
    case PreProcurementStep.ContractDraft:
      return 'ร่างสัญญาและสัญญา';
    // ------Contract Management---------
    case PreProcurementStep.contractReceive:
      return 'บันทึกส่งมอบและตรวจรับ';
    case PreProcurementStep.contractExpense:
      return 'ขออนุมัติเบิกจ่าย';
    case PreProcurementStep.contractReturnCollateral:
      return 'คืนหลักประกันสัญญา';
    case PreProcurementStep.PrincipleApproval:
      return 'ขออนุมัติหลักการ';
    case PreProcurementStep.PrincipleApprovalRental:
      return 'ขออนุมัติเช่า';
    default:
      return value;
  }
};

const PreProcurementGroupStepName = (value: PreProcurementGroupStep): string => {
  switch (value) {
    case PreProcurementGroupStep.All:
      return 'ทั้งหมด';
    case PreProcurementGroupStep.PreProcurement:
      return 'Pre-Procurement';
    case PreProcurementGroupStep.Procurement:
      return 'Procurement';
    case PreProcurementGroupStep.ContractAgreement:
      return 'Contract Agreement';
    case PreProcurementGroupStep.ContractManagement:
      return 'Contract Management';
    default:
      return value;
  }
};

const PreProcurementPP004HigherName = (value: ConsiderOverPriceType): string => {
  switch (value) {
    case ConsiderOverPriceType.ERIA:
      return 'ราคาที่ได้มาจากการคำนวณตามหลักเกณฑ์ที่คณะกรรมการราคากลางกำหนด';
    case ConsiderOverPriceType.CGD_REFERENCE_DATABASE:
      return 'ราคาที่ได้มาจากฐานข้อมูลราคาอ้างอิงของพัสดุที่กรมบัญชีกลางจัดทำ';
    case ConsiderOverPriceType.STANDARD_PRICE_BY_AGENCY:
      return 'ราคามาตรฐานที่สำนักงบประมาณหรือหน่วยงานกลางอื่นกำหนด เช่น กระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม กระทรวงสาธารณสุข เป็นต้น';
    case ConsiderOverPriceType.MARKET_PRICE_RESEARCH:
      return 'ราคาที่ได้มาจากการสืบราคาจากท้องตลาด';
    case ConsiderOverPriceType.LAST_PURCHASE_WITHIN_2_YEARS:
      return 'ราคาที่เคยซื้อหรือจ้างหรือเช่าครั้งสุดท้ายภายในระยะเวลาสองปีงบประมาณ';
    case ConsiderOverPriceType.OTHER_OFFICIAL_METHOD:
      return 'ราคาอื่นใดตามหลักเกณฑ์ วิธีการ หรือแนวทางปฏิบัติของหน่วยงานของรัฐนั้น ๆ';
    default:
      return 'เกิดข้อผิดพลาด';
  }
};

const PreProcurementDialogGroupStepName = (value: PreProcurementDialogGroupStep): string => {
  switch (value) {
    case PreProcurementDialogGroupStep.All:
      return 'ทั้งหมด';
    case PreProcurementDialogGroupStep.SupplyMethodCode60:
      return 'พ.ร.บ. จัดซื้อจัดจ้างฯ 2560';
    case PreProcurementDialogGroupStep.SupplyMethodCode80:
      return 'ข้อบังคับธนาคาร 80';
    default:
      return value;
  }
};

const PreProcurementDialogStepName = (value: string): string => {
  switch (value) {
    case PreProcurementDialogStep.ApprovePlan:
      return 'อนุมัติ';
    case PreProcurementDialogStep.Announcement:
      return 'เผยแพร่แผน';
    default:
      return value;
  }
};

const ConsiderOverPriceTypeOptions = Object.entries(ConsiderOverPriceType).map(
  ([, value]): Option => ({
    label: PreProcurementPP004HigherName(value),
    value: value,
  }) as Option);

const PreProcurementTypeOptions = Object.entries(PreProcurementType).map(
  ([, value]) =>
    ({
      label: PreProcurementTypeName(value),
      value: value,
    }) as Option
);

const PreProcurementConstants = {
  PreProcurementTypeName,
  PreProcurementStepShortName,
  PreProcurementStepFullName,
  PreProcurementGroupStepName,
  PreProcurementDialogGroupStepName,
  PreProcurementDialogStepName,
  ConsiderOverPriceTypeOptions,
  PreProcurementTypeOptions,
};

export default PreProcurementConstants;
