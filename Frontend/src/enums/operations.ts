enum OrganizationLevel {
  /**
   * กลุ่ม
   */
  Group = 'Group',
  /**
   * สายงาน/ศูนย์
   */
  Line = 'Line',
  /**
   * ฝ่าย
   */
  Department = 'Department',
  /**
   * ส่วนงาน
   */
  Segment = 'Segment',
  /**
   * โซน
   */
  Zone = 'Zone',
  /**
 * สาขา
 */
  Branch = 'Branch',
}

enum SectionApproverSupplyMethodExtra {
  /// บัญชีเบิกจ่าย
  SectionApprover001 = "SectionApprover001",
  /// พรบ. 2560 >> ลงนามในสัญญา
  SectionApprover002 = "SectionApprover002",
}

enum SectionProcessType {
  /// แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้าง
  AppointPreProcurement = "AppointPreProcurement",

  /// แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้างสำหรับที่ดินเชิงพาณิชย์
  AppointPreProcurementCommercialParcel = "AppointPreProcurementCommercialParcel",

  /// แต่งตั้งคณะกรรมการก่อนการจัดซื้อจัดจ้างสำหรับพัสดุคงคลัง
  AppointPreProcurementStock = "AppointPreProcurementStock",

  /// แผนงาน
  Plan = "Plan",

  /// ขอบเขตงาน (Terms of Reference)
  TOR = "TOR",

  /// ขอบเขตงานสำหรับที่ดินเชิงพาณิชย์
  TORCommercialParcel = "TORCommercialParcel",

  /// ขอบเขตงานสำหรับพัสดุคงคลัง
  TORStock = "TORStock",

  /// ขอบเขตงาน กรณี MD
  TORHasMD = "TORHasMD",

  /// ขอบเขตงานสำหรับที่ดินเชิงพาณิชย์ กรณี MD
  TORCommercialParcelHasMD = "TORCommercialParcelHasMD",

  /**
   * ราคากลาง
   */
  MedianPrice = "MedianPrice",

  /// ราคากลาง กรณี 4 ฝ่ายตามตารางแนบท้าย
  MedianPriceCommercialParcel = "MedianPriceCommercialParcel",

  /**
 * ราคากลาง กรณี MD
 */
  MedianPriceHasMD = "MedianPriceHasMD",

  /// ราคากลาง กรณี 4 ฝ่ายตามตารางแนบท้าย กรณี MD
  MedianPriceCommercialParcelHasMD = "MedianPriceCommercialParcelHasMD",

  /// ราคากลาง Stock
  MedianPriceStock = "MedianPriceStock",

  /**
   * จพ.05
   */
  ApprovePurchaseRequest = "ApprovePurchaseRequest",

  /**
   * จพ. 005 กรณี 4 ฝ่ายตามตารางแนบท้าย
   */
  ApprovePurchaseRequestCommercialParcel = "ApprovePurchaseRequestCommercialParcel",

  /// จพ. 006, 119, 79วรรคสอง, pettycash
  PurchaseOrder = "PurchaseOrder",

  /// จพ. 006 กรณี 4 ฝ่ายตามตารางแนบท้าย
  PurchaseOrderCommercialParcel = "PurchaseOrderCommercialParcel",

  /**
   * อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา
   */
  ApprovePurchaseOrder = 'ApprovePurchaseOrder',

  /**
 * อนุมัติใบสั่งซื้อ/จ้าง/เช่า และแจ้งทำสัญญา ตามตารางแนบท้าย
 */
  ApprovePurchaseOrderCommercialParcel = 'ApprovePurchaseOrderCommercialParcel',

  /**
   * หนังสือเชิญชวนทำสัญญา
   */
  ContractInvitation = 'ContractInvitation',

  /**
   * ร่างสัญญาและสัญญา
   */
  ContractDraft = 'ContractDraft',

  /**
   * บันทึกส่งมอบและตรวจรับ
   */
  DeliveryAcceptancePeriod = 'DeliveryAcceptancePeriod',

  /**
 * บันทึกส่งมอบและตรวจรับ กรณี 4 ฝ่ายตามตารางแนบท้าย
 */
  DeliveryAcceptancePeriodCommercialParcel = 'DeliveryAcceptancePeriodCommercialParcel',

  /**
   * บันทึกส่งมอบและตรวจรับ กรณีมีค่าปรับ
   */
  DeliveryAcceptancePeriodPenalty = 'DeliveryAcceptancePeriodPenalty',

  /**
   * คืนหลักประกันสัญญา
   */
  ContractGuaranteeReturn = 'ContractGuaranteeReturn',

  /**
   * ขออนุมัติหลักการ
   */
  PrincipleRentalApproval = 'PrincipleRentalApproval',

  /**
   * ขออนุมัติเช่า
   */
  RentalApproval = 'RentalApproval',

  /**
    * เบิกจ่าย (สำหรับบัญชี)
    */
  ExpenseDisbursement = 'ExpenseDisbursement',

  /**
   * ขออนุมัติบอกเลิกสัญญา
   */
  ContractTermination = 'ContractTermination',

  ContractAmendment = 'ContractAmendment'
}

export {
  OrganizationLevel,
  SectionProcessType,
  SectionApproverSupplyMethodExtra
}