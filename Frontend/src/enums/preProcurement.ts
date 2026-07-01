enum PreProcurementType {
  All = 'All',
  AnnualPlan = 'AnnualPlan',
  InYearPlan = 'InYearPlan',
}

enum PreProcurementCommitteeType {
  SingleCommittee = 'SingleCommittee',
  MultipleCommittee = 'MutipleCommittee',
}

// Use in PP004 & PP005 In case > 100,000
enum ConsiderOverPriceType {
  ERIA = 'ราคาที่ได้มาจากการคำนวณตามหลักเกณฑ์ที่คณะกรรมการราคากลางกำหนด',
  CGD_REFERENCE_DATABASE = 'ราคาที่ได้มาจากฐานข้อมูลราคาอ้างอิงของพัสดุที่กรมบัญชีกลางจัดทำ',
  STANDARD_PRICE_BY_AGENCY = 'ราคามาตรฐานที่สำนักงบประมาณหรือหน่วยงานกลางอื่นกำหนด เช่น กระทรวงดิจิทัลเพื่อเศรษฐกิจและสังคม กระทรวงสาธารณสุข เป็นต้น',
  MARKET_PRICE_RESEARCH = 'ราคาที่ได้มาจากการสืบราคาจากท้องตลาด',
  LAST_PURCHASE_WITHIN_2_YEARS = 'ราคาที่เคยซื้อหรือจ้างหรือเช่าครั้งสุดท้ายภายในระยะเวลาสองปีงบประมาณ',
  OTHER_OFFICIAL_METHOD = 'ราคาอื่นใดตามหลักเกณฑ์ วิธีการ หรือแนวทางปฏิบัติของหน่วยงานของรัฐนั้น ๆ',
}

enum PreProcurementStep {
  // ----------Pre-Procurement-------
  /**
   * แต่งตั้ง
   */
  Appoint = 'Appoint',
  /**
   * ร่างขอบเขตงาน
   */
  TorDraft = 'TorDraft',
  /**
   * ขออนุมัติหลักการ
   */
  PrincipleApproval = 'PrincipleApproval',
  /**
   * ขออนุมัติเช่า
   */
  PrincipleApprovalRental = 'PrincipleApprovalRental',
  /**
   * ราคากลาง
   */
  MedianPrice = 'MedianPrice',
  /**
   * จพ.04
   */
  PurchaseRequisition = 'PurchaseRequisition',

  // ------------Procurement----------
  /**
   * จพ.05
   */
  Jp005 = 'Jp005',
  /**
   * หนังสือเชิญชวนผู้ประกอบการ
   */
  Invite = 'Invite',
  /**
   * จพ.06  | (ขออนุมัติเช่า)
   */
  PurchaseOrder = 'PurchaseOrder',
  /**
   * อนุมัติใบสั่งซื้อ/จ้าง/เข่า และแจ้งทำสัญญา | (อนุมัติใบสั่งเช่า และแจ้งทำสัญญา)
   */
  PurchaseOrderApproval = 'PurchaseOrderApproval',
  W119 = 'W119',
  P79Clause2 = 'P79Clause2',
  PettyCash = 'PettyCash',
  PettyCashReimbursement = 'PettyCashReimbursement',

  // ------Contract Agreement-------
  /**
   * (หนังสือเชิญชวนทำสัญญา)
   */
  ContractInvitation = 'ContractInvitation',
  /**
   * (ร่างสัญญาและสัญญา)
   */
  ContractDraft = 'ContractDraft',

  // ------Contract Management---------
  contractReceive = 'contractReceive',
  contractExpense = 'contractExpense',
  contractReturnCollateral = 'contractReturnCollateral',

  /**
   * (บัญชีค่าใช่จ่าย)
   */
  Accounting = 'Accounting',
}

enum PreProcurementGroupStep {
  All = 'all',
  PreProcurement = 'preProcurement',
  Procurement = 'procurement',
  ContractAgreement = 'contractAgreement',
  ContractManagement = 'contractManagement',
}
// # endregion

// # region PreProcurement Dialog
enum PreProcurementDialogGroupStep {
  All = 'all',
  SupplyMethodCode60 = 'supplyMethodCode60',
  SupplyMethodCode80 = 'supplyMethodCode80',
}

enum PreProcurementDialogStep {
  ApprovePlan = 'approvePlan',
  Announcement = 'announcement',
}
// # endregion

// # region PP004 Section
enum PreProcurementPP004FileType {
  Prepare = 'Prepare',
  Tor = 'Tor',
  Quotation = 'Quotation',
}

enum SupplyMethodTypeConstant
{
    // ซื้อ
    Buy = "SMethodType001",
    // จ้าง
    Hire = "SMethodType002",
    // เช่า
    Rent = "SMethodType003",
}

// # endregion

export {
  PreProcurementType,
  PreProcurementStep,
  PreProcurementGroupStep,
  ConsiderOverPriceType,
  PreProcurementDialogGroupStep,
  PreProcurementDialogStep,
  PreProcurementCommitteeType,
  PreProcurementPP004FileType,
  SupplyMethodTypeConstant
};
