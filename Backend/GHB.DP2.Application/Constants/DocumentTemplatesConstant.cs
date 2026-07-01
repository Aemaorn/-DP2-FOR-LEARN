namespace GHB.DP2.Application.Constants;

public static class MaximumBudget
{
    public const decimal MaxBudget = 9000000000;
}

public static class DocumentTemplateGroups
{
    /// <summary>ขอแต่งตั้งบุคคล/คกก. จัดทำขอบเขตของงาน/ราคากลาง</summary>
    public const string Ap = "Ap";

    /// <summary>เชิญชวนลงนามในสัญญา</summary>
    public const string CAInv = "CAInv";

    /// <summary>ร่างสัญญา</summary>
    public const string CA = "CA";

    /// <summary>คืนหลักประกัน</summary>
    public const string CMCltr = "CMCltr";

    /// <summary>รายงานสัญญาแล้วเสร็จ</summary>
    public const string CMComplete = "CMComplete";

    /// <summary>รายงานตรวจเงินแผ่นดิน และกรมสรรพากร</summary>
    public const string CMAd = "CMAd";

    /// <summary>ร่างสัญญาขยายเวลา</summary>
    public const string CAExt = "CAExt";

    /// <summary>ส่งมอบ และตรวจรับ</summary>
    public const string CMR = "CMR";

    /// <summary>จัดทำหนังสือเชิญชวนผู้ประกอบการ</summary>
    public const string INV = "INV";

    /// <summary>แจ้งข้อมูลเบื้องต้น (จพ. 004)</summary>
    public const string Jp04 = "Jp04";

    /// <summary>ขอซื้อขอจ้าง (จพ. 005)</summary>
    public const string Jp05 = "Jp05";

    /// <summary>ขออนุมัติสั่งซื้อ/สั่งจ้าง (จพ.006)</summary>
    public const string Jp06 = "Jp06";

    /// <summary>ราคากลาง</summary>
    public const string Mdp = "Mdp";

    /// <summary>รายการจัดซื้อจัดจ้าง</summary>
    public const string Plan = "Plan";

    /// <summary>ประกาศแผนจัดซื้อจัดจ้าง</summary>
    public const string PlanAnnouncement = "PlanAnnment";

    /// <summary>ร่างขอบเขตของงาน (TOR)</summary>
    public const string Tor = "Tor";

    /// <summary>ข้อ 79 ค่าสาธารณูปโภค</summary>
    public const string P79Clause2 = "P79C2";

    /// <summary>หลักการอนุมัติ</summary>
    public const string PrincipleApproval = "PRentalPcp";

    /// <summary>ขออนุมัติเช่า</summary>
    public const string PrincipleApprovalRental = "PRental";

    /// <summary>แจ้งข้อมูลเบื้องต้น (จพ. 010)</summary>
    public const string Jp10 = "Jp10";

    /// <summary>ขออนุมัติ ว119</summary>
    public const string Pw119 = "PW119";

    /// <summary>ขออนุมัติ ว119</summary>
    public const string PettyCash = "PPettyCash";

    /// <summary>บอกเลิกสัญญา</summary>
    public const string CMTermination = "CMTermination";

    /// <summary>ใบรองรับผลงาน</summary>
    public const string CertificateRequisition = "CertificateRequisition";

    /// <summary>ส่งมอบตรวจรับ</summary>
    public const string DeliveryAcceptancePeriod = "CMR";

    /// <summary>ขออนุมัติเบิกจ่าย</summary>
    public const string DisbursementApproval = "CMDbm";

    /// <summary> รายการบันทึกต่อท้ายสัญญา </summary>
    public const string AddendumList = "CamContractAmendment";

    /// <summary> ข้อมูลรายงานสำนักงานการตรวจเงินแผ่นดินและกรมสรรพากร </summary>
    public const string AuditAndRevenueReport = "AuditAndRevenueReport";

    /// <summary> เอกสารสัญญาแล้วเสร็จตามไตรมาส </summary>
    public const string QuarterlyCompletion = "QuarterlyCompletion";

    /// <summary> คืนหลักประกันสัญญา </summary>
    public const string CMGuaranteeReturn = "CMGuaranteeReturn";

    /// <summary> ขยาย/ลดระยะเวลาสัญญา </summary>
    public const string CAMExtendChange = "CAMExtendChange";

    /// <summary> งด/ลดค่าปรับ </summary>
    public const string CAMWaiveOrReducePenalty = "CAMWaiveOrReducePenalty";

    /// <summary> PO Addendum </summary>
    public const string CAMPoAddendum = "CAMPoAddendum";

    /// <summary> แก้ไขร่างสัญญา </summary>
    public const string CMContractDraftVendorEdit = "CamContractAmendment";

    /// <summary> แก้ไขคณะกรรมการ </summary>
    public const string CommitteeChange = "CommitteeChange";
}

public static class ContractAmendmentDocumentType
{
    /// <summary>
    /// ใบสั่ง/สัญญา - เพิ่ม PO ใหม่ต่อท้ายขอแก้ไข
    /// </summary>
    public const string AppendNewPurchaseOrder = "AppendNewPurchaseOrder";

    /// <summary>
    /// ใบสั่ง/สัญญา - เพิ่ม PO ขออนุมัติงด/ลดค่าปรับ
    /// </summary>
    public const string WaiveOrReducePenalty = "WaiveOrReducePenalty";

    /// <summary>
    /// ใบสั่ง/สัญญา - เพิ่ม PO การขยายหรือลดระยะเวลาสัญญา
    /// </summary>
    public const string AdjustContractDuration = "AdjustContractDuration";
}

public static class DocumentTemplatesConstant
{
    // FIXME: These constants are not complete, need to be updated with actual template IDs.
    public const string AppointTemplate = "AP001";
    public const string InviteTemplate = "INV001";
    public const string Jp004Template = "JP04001";
    public const string Jp005Template = "JP05001";
    public const string Jp006Template = "JP06001";
    public const string MedianPriceTemplate = "MDP001";
    public const string PlanTemplate = "PL001";
    public const string PlanAnnouncementTemplate = "PLA001";
    public const string TorTemplate = "TOR001";
}

public static class CommitteeChangeTemplateConstant
{
    public const string CommitteeChangeRequest = "CommitteeChangeRequest";
    public const string CommitteeChangeRequestComment = "CommitteeChangeRequestComment";
}

public static class Pw119TemplateConstant
{
    public const string ApprovalRequest60 = "W119ApprovalRequest60";
    public const string WinnerAnnounce60 = "W119WinnerAnnounce60";
}

public static class P79Clause2TemplateConstant
{
    public const string ApprovalRequest60 = "P79Clause2ApprovalRequest60";
    public const string ApprovalRequest80 = "P79Clause2ApprovalRequest80";
    public const string WinnerAnnounce60 = "P79Clause2WinnerAnnounce60";
}

public static class PettyCashTemplateConstant
{
    public const string ApprovalRequest60 = "PettyCashApprovalRequest60";
    public const string ApprovalRequestTypeNonJorPor00160 = "PettyCashApprovalRequestTypeNonJorPor00160";

    public static string GetTemplateCode(bool? isFromJorPor001) =>
        isFromJorPor001 == false ? ApprovalRequestTypeNonJorPor00160 : ApprovalRequest60;
}

public static class PlanDocumentTemplatesConstant
{
    public const string PlanInYearApprovalRequest60 = "PlanInYearApprovalRequest60";
    public const string PlanInYearApprovalRequest80 = "PlanInYearApprovalRequest80";
    public const string PlanAnnualApprovalRequest60 = "PlanAnnualApprovalRequest60";
    public const string PlanAnnualApprovalRequest80 = "PlanAnnualApprovalRequest80";
    public const string PlanPublication60 = "PlanPublication60";
    public const string PlanPublication80 = "PlanPublication80";
    public const string PlanCancelApprovalRequest60 = "PlanCancelApprovalRequest60";
    public const string PlanCancelApprovalRequest80 = "PlanCancelApprovalRequest80";
    public const string PlanCancelPublication60 = "PlanCancelPublication60";
    public const string PlanCancelPublication80 = "PlanCancelPublication80";
    public const string PlanChangeApprovalRequest60 = "PlanChangeApprovalRequest60";
    public const string PlanChangeApprovalRequest80 = "PlanChangeApprovalRequest80";
    public const string PlanChangePublication60 = "PlanChangePublication60";
    public const string PlanChangePublication80 = "PlanChangePublication80";
}

public static class AppointDocumentTemplatesConstant
{
    public const string ApGt100k60 = "ApGt100k60";
    public const string ApGt100k80 = "ApGt100k80";
    public const string ApLte100k60 = "ApLte100k60";
    public const string ApLte100k80 = "ApLte100k80";
    public const string ApCancelGt100k60 = "ApCancelGt100k60";
    public const string ApCancelGt100k80 = "ApCancelGt100k80";
    public const string ApCancelLte100k60 = "ApCancelLte100k60";
    public const string ApCancelLte100k80 = "ApCancelLte100k80";
    public const string ApEditGt100k60 = "ApEditGt100k60";
    public const string ApEditGt100k80 = "ApEditGt100k80";
    public const string ApEditLte100k60 = "ApEditLte100k60";
    public const string ApEditLte100k80 = "ApEditLte100k80";
}

public static class TorDocumentTemplatesConstant
{
    // ซื้อ-เช่า ไม่เกิน 5 แสน
    public const string TorBuyRentLte500k60 = "TorBuyRentLte500k60";
    public const string TorBuyRentLte500k80 = "TorBuyRentLte500k80";

    // จ้าง ไม่เกิน 5 แสน
    public const string TorHireLte500k60 = "TorHireLte500k60";
    public const string TorHireLte500k80 = "TorHireLte500k80";

    // ซื้อ-เช่า เกิน 5 แสน (ทั่วไป)
    public const string TorBuyRentGt500kGen60 = "TorBuyRentGt500kGen60";
    public const string TorBuyRentGt500kGen80 = "TorBuyRentGt500kGen80";

    // ซื้อ-เช่า เกิน 5 แสน (IT)
    public const string TorBuyRentGt500kIt60 = "TorBuyRentGt500kIt60";
    public const string TorBuyRentGt500kIt80 = "TorBuyRentGt500kIt80";

    // จ้างบริการบำรุงรักษา
    public const string TorHireMaintenance60 = "TorHireMaintenance60";
    public const string TorHireMaintenance80 = "TorHireMaintenance80";

    // จ้างพัฒนา
    public const string TorHireDevelopment60 = "TorHireDevelopment60";
    public const string TorHireDevelopment80 = "TorHireDevelopment80";

    // ซื้อพร้อมจ้าง
    public const string TorBuyWithHire60 = "TorBuyWithHire60";
    public const string TorBuyWithHire80 = "TorBuyWithHire80";

    // จ้างพร้อมจ้าง
    public const string TorHireWithHire60 = "TorHireWithHire60";
    public const string TorHireWithHire80 = "TorHireWithHire80";

    // ซื้อสิทธิ
    public const string TorBuyLicense60 = "TorBuyLicense60";
    public const string TorBuyLicense80 = "TorBuyLicense80";

    // เช่าวงจรสื่อสารข้อมูล
    public const string TorRentCommCircuit60 = "TorRentCommCircuit60";
    public const string TorRentCommCircuit80 = "TorRentCommCircuit80";

    // จ้างที่ปรึกษา
    public const string TorHireConsultant60 = "TorHireConsultant60";
    public const string TorHireConsultant80 = "TorHireConsultant80";

    // เช่าคอมพิวเตอร์
    public const string TorRentComputer60 = "TorRentComputer60";
    public const string TorRentComputer80 = "TorRentComputer80";

    // จ้างบริการรักษาความสะอาด
    public const string TorHireCleaning60 = "TorHireCleaning60";
    public const string TorHireCleaning80 = "TorHireCleaning80";

    // จ้างบริการรักษาความปลอดภัย
    public const string TorHireSecurity60 = "TorHireSecurity60";
    public const string TorHireSecurity80 = "TorHireSecurity80";

    // เช่ารถยนต์
    public const string TorRentVehicle60 = "TorRentVehicle60";
    public const string TorRentVehicle80 = "TorRentVehicle80";

    // จ้างปรับปรุงซ่อมแซม
    public const string TorHireRenovate60 = "TorHireRenovate60";
    public const string TorHireRenovate80 = "TorHireRenovate80";

    // ขออนุมัติร่างขอบเขตงาน
    public const string TorApprovalRequest60 = "TorApprovalRequest60";
    public const string TorApprovalRequest80 = "TorApprovalRequest80";
}

public static class InvitationVendorDocumentTemplatesConstant
{
    public const string InvitationVendor = "InvitationVendor";
}

public static class MedianPriceDocumentTemplatesConstant
{
    public const string MedianPriceBoKor0160 = "MedianPriceBoKor0160";
    public const string MedianPriceBoKor0180 = "MedianPriceBoKor0180";
    public const string MedianPriceBoKor0260 = "MedianPriceBoKor0260";
    public const string MedianPriceBoKor0280 = "MedianPriceBoKor0280";
    public const string MedianPriceBoKor0360 = "MedianPriceBoKor0360";
    public const string MedianPriceBoKor0380 = "MedianPriceBoKor0380";
    public const string MedianPriceBoKor0460 = "MedianPriceBoKor0460";
    public const string MedianPriceBoKor0480 = "MedianPriceBoKor0480";
    public const string MedianPriceBoKor0560 = "MedianPriceBoKor0560";
    public const string MedianPriceBoKor0580 = "MedianPriceBoKor0580";
    public const string MedianPriceBoKor0660 = "MedianPriceBoKor0660";
    public const string MedianPriceBoKor0680 = "MedianPriceBoKor0680";
}

public static class Jp04DocumentTemplatesConstant
{
    public const string Jp04PurchaseRequisition60 = "Jp04PurchaseRequisition60";
    public const string Jp04PurchaseRequisition80 = "Jp04PurchaseRequisition80";
}

public static class Jp05DocumentTemplatesConstant
{
    public const string Jp05ApprovePurchaseRequest60 = "Jp05ApprovePurchaseRequest60";
    public const string Jp05ApprovePurchaseRequest80 = "Jp05ApprovePurchaseRequest80";
    public const string Jp05Command80 = "Jp05Command80";
}

public static class Jp06DocumentTemplatesConstant
{
    public const string Jp06WinnerAnnounce80 = "Jp06WinnerAnnounce80";
    public const string Jp06WinnerAnnounce60 = "Jp06WinnerAnnounce60";
    public const string Jp06PurchaseOrderGt100k60 = "Jp06PurchaseOrderGt100k60";
    public const string Jp06PurchaseOrderGt100k80 = "Jp06PurchaseOrderGt100k80";
    public const string Jp06PurchaseOrderLte100k60 = "Jp06PurchaseOrderLte100k60";
    public const string Jp06PurchaseOrderLte100k80 = "Jp06PurchaseOrderLte100k80";
    public const string Jp06CancelApprovalRequest60 = "Jp06CancelApprovalRequest60";
    public const string Jp06CancelApprovalRequest80 = "Jp06CancelApprovalRequest80";
}

public static class CADocumentTemplatesConstant
{
    public const string CAHireSignInvitation = "CAHireSignInvitation";
    public const string CARentSignInvitation = "CARentSignInvitation";
    public const string CAPurchaseSignInvitation = "CAPurchaseSignInvitation";
    public const string CAHireSignInvitationNoCol = "CAHireSignInvitationNoCol";
    public const string CARentSignInvitationNoCol = "CARentSignInvitationNoCol";
    public const string CAPurchaseSignInvitationNoCol = "CAPurchaseSignInvitationNoCol";
    public const string CAHireConstruction60 = "CAHireConstruction60";
    public const string CAHireConstruction80 = "CAHireConstruction80";
    public const string CAPurchaseComputer60 = "CAPurchaseComputer60";
    public const string CAPurchaseComputer80 = "CAPurchaseComputer80";
    public const string CAPurchase60 = "CAPurchase60";
    public const string CAPurchase80 = "CAPurchase80";
    public const string CAHirerMaintenance60 = "CAHirerMaintenance60";
    public const string CAHirerMaintenance80 = "CAHirerMaintenance80";
    public const string CAHirerSecurityService60 = "CAHirerSecurityService60";
    public const string CAHirerSecurityService80 = "CAHirerSecurityService80";
    public const string CAHireCustomWork60 = "CAHireCustomWork60";
    public const string CAHireCustomWork80 = "CAHireCustomWork80";
    public const string CARentCopier60 = "CARentCopier60";
    public const string CARentCopier80 = "CARentCopier80";
    public const string CAPurchaseOpenEnd60 = "CAPurchaseOpenEnd60";
    public const string CAPurchaseOpenEnd80 = "CAPurchaseOpenEnd80";
    public const string CAPurchaseSoftwareLicenseAnd60 = "CAPurchaseSoftwareLicenseAnd60";
    public const string CAPurchaseSoftwareLicenseAnd80 = "CAPurchaseSoftwareLicenseAnd80";
    public const string CARentComputer60 = "CARentComputer60";
    public const string CARentComputer80 = "CARentComputer80";
    public const string CAHireBuildingCleaningService60 = "CAHireBuildingCleaningService60";
    public const string CAHireBuildingCleaningService80 = "CAHireBuildingCleaningService80";
    public const string CAHireDesignAndSupervision60 = "CAHireDesignAndSupervision60";
    public const string CAHireDesignAndSupervision80 = "CAHireDesignAndSupervision80";
    public const string CAHireConsulting60 = "CAHireConsulting60";
    public const string CAHireConsulting80 = "CAHireConsulting80";
    public const string CARentCar60 = "CARentCar60";
    public const string CARentCar80 = "CARentCar80";
    public const string CAExchange60 = "CAExchange60";
    public const string CAExchange80 = "CAExchange80";
    public const string CAPDPA60 = "CAPDPA60";
    public const string CAPDPA80 = "CAPDPA80";
    public const string CASigningApproval80 = "CASigningApproval80";
    public const string CASigningApproval60 = "CASigningApproval60";
}

public static class CMDocumentTemplatesConstant
{
    public const string CMGuaranteeReturn = "CMGuaranteeReturn";
    public const string CMGuaranteeReturnApprovalRequest = "CMGuaranteeReturnApprovalRequest";
    public const string CMGuaranteeReturnResule = "CMGuaranteeReturnResule";
    public const string CMReportCompletedQ1 = "CMReportCompletedQ1";
    public const string CMReportCompletedQ2 = "CMReportCompletedQ2";
    public const string CMReportCompletedQ3 = "CMReportCompletedQ3";
    public const string CMReportCompletedQ4 = "CMReportCompletedQ4";
    public const string CMReportToAuditorAndRD = "CMReportToAuditorAndRD";
    public const string CMReportToAuditorDirector = "CMReportToAuditorDirector";
    public const string CMReportToRDDirector = "CMReportToRDDirector";
    public const string CMInspection60 = "CMInspection60";
    public const string CMInspection80 = "CMInspection80";
    public const string CMDisbursementApproval = "CMDisbursementApproval";
}

public static class CMContractDraftVendorEditDocumentTemplatesConstant
{
    public const string Amendment60 = "Amendment60";
    public const string Amendment80 = "Amendment80";
    public const string AmendmentApprovalRequest60 = "AmendmentApprovalRequest60";
    public const string AmendmentApprovalRequest80 = "AmendmentApprovalRequest80";
}

/// <summary>
/// Common error messages for document operations
/// </summary>
public static class DocumentErrorMessages
{
    /// <summary>ไม่พบเอกสาร (DocumentHistories is empty)</summary>
    public const string DocumentNotFound = "ไม่พบเอกสาร";

    /// <summary>ไม่พบเอกสารเทมเพลตสำหรับรีเซ็ต</summary>
    public const string TemplateNotFoundForReset = "ไม่พบเอกสารเทมเพลตสำหรับรีเซ็ต";

    /// <summary>ไม่สามารถคัดลอกเอกสารได้</summary>
    public const string CopyDocumentFailed = "ไม่สามารถคัดลอกเอกสารได้";

    /// <summary>ไม่พบเอกสาร Template</summary>
    public const string DocumentTemplateNotFound = "ไม่พบเอกสาร Template";

    /// <summary>ไม่พบข้อมูลผู้ขาย</summary>
    public const string VendorNotFound = "ไม่พบข้อมูลผู้ขาย";
}