namespace GHB.DP2.Application.Constants;

using GHB.DP2.Domain.SystemUtility;

public static class SupplyMethodConstant
{
    /// <summary>
    /// พ.ร.บ.จัดซื้อจัดจ้างฯ 2560
    /// </summary>
    public const string Sixty = "SMethod002";

    /// <summary>
    /// ข้อบังคับธนาคาร 80
    /// </summary>
    public const string Eighty = "SMethod004";

    /// <summary>
    /// font สำหรับ generate เอกสาร ตาม supply method
    /// </summary>
    public static string GetDocumentFontName(ParameterCode supplyMethodCode) =>
        supplyMethodCode == ParameterCode.From(Sixty) ? "TH SarabunIT๙" : "TH Sarabun New";
}

public static class RevStampConstant
{
    /// <summary>
    /// รหัสอากรแสตมป์
    /// </summary>
    public const string RevStamp001 = "RevStamp001";
}

public static class SuParameterCodeConstant
{
    /// <summary>
    /// ผู้จัดทำ
    /// </summary>
    public const string PosBoard006 = "PosBoard006";

    /// <summary>
    /// เงื่อนไขค่าปรับอื่น ๆ
    /// </summary>
    public const string FineTypeOtherCondition = "FineType004";
}

public static class SuParameterGroupCodeConstant
{
    /// <summary>
    /// หน่วยวัด
    /// </summary>
    public const string UnitOfMeasures = "UnitOfMea";

    /// <summary>
    /// ประเภทระยะเวลาดำเนินการ
    /// </summary>
    public const string PeriodType = "PeriodType";

    /// <summary>
    /// เงื่อนไขการนับระยะเวลาดำเนินการ
    /// </summary>
    public const string PeriodCondition = "PeriodCond";

    /// <summary>
    /// เงื่อนไขการนับระยะเวลาดำเนินการ
    /// </summary>
    public const string MaintenancePeriodType = "MPeriodType";

    /// <summary>
    /// ประเภทค่าปรับ
    /// </summary>
    public const string FineType = "FineType";

    /// <summary>
    /// SolId
    /// </summary>
    public const string SolId = "SolId";

    /// <summary>
    /// หลักเกณฑ์การพิจารณาคัดเลือกข้อเสนอ
    /// </summary>
    public const string CriteriaCons = "CriteriaCons";

    /// <summary>
    /// เวลา
    /// </summary>
    public const string PTimeType = "PTimeType";

    /// <summary>
    /// วัน
    /// </summary>
    public const string DOW = "DOW";

    /// <summary>
    /// CM ค่าปรับ
    /// </summary>
    public const string CMFineType = "CMFineType";

    /// <summary>
    /// CM ค่าปรับ
    /// </summary>
    public const string DWCUnit = "DWCUnit";

    /// <summary>
    /// รหัสบัญชี
    /// </summary>
    public const string GLAcc = "GLAcc";

    /// <summary>
    /// ประเภทงบประมาณ
    /// </summary>
    public const string BudgetTyp = "BudgetTyp";

    /// <summary>
    /// เงื่อนไขระยะเวลา
    /// </summary>
    public const string DelvCUnit = "DelvCUnit";
}

public static class WinReasonConstant
{
    /// <summary>
    /// รหัสกลุ่มเหตุผลที่ชนะการเสนอราคา
    /// </summary>
    public const string GroupCode = "WinReason";

    /// <summary>
    /// เป็นผู้เสนอราคาตรงตามข้อกำหนด และราคาไม่เกินงบประมาณ
    /// </summary>
    public const string WinReason001 = "WinReason001";

    /// <summary>
    /// เป็นผู้เสนอราคาตรงตามข้อกำหนด และราคาไม่เกินงบประมาณและราคากลาง(อ้างอิง)
    /// </summary>
    public const string WinReason002 = "WinReason002";

    /// <summary>
    /// เป็นผู้มีคุณสมบัติและข้อเสนอทางด้านเทคนิคถูกต้องครบถ้วนและเป็นผู้เสนอราคาต่ำสุด
    /// </summary>
    public const string WinReason003 = "WinReason003";

    /// <summary>
    /// เป็นผู้มีคุณสมบัติและข้อเสนอทางด้านเทคนิคถูกต้องครบถ้วนและเป็นผู้ได้คะแนนรวมสูงสุด
    /// </summary>
    public const string WinReason004 = "WinReason004";

    /// <summary>
    /// อื่นๆ
    /// </summary>
    public const string WinReason005 = "WinReason005";
}

public static class VatTypeConstant
{
    /// <summary>
    /// รหัสกลุ่มประเภทภาษีมูลค่าเพิ่ม
    /// </summary>
    public const string GroupCode = "VATType";

    /// <summary>
    /// ไม่มี VAT
    /// </summary>
    public const string NotIncludeVat = "VATType001";

    /// <summary>
    /// รวม VAT
    /// </summary>
    public const string IncluedVat = "VATType002";
}

public static class ContractTypeConstant
{
    /// <summary>
    /// สัญญาซื้อขาย
    /// </summary>
    public const string Buy = "CMType001";

    /// <summary>
    /// สัญญาจ้าง
    /// </summary>
    public const string Hire = "CMType002";

    /// <summary>
    /// สัญญาเช่า
    /// </summary>
    public const string Rent = "CMType003";
}

public static class ContractRentalTypeConstant
{
    /// <summary>
    /// สัญญาเช่า
    /// </summary>
    public const string Rent = "CMRentalType001";
}

public static class PeriodTypeConstant
{
    /// <summary>
    /// วัน
    /// </summary>
    public const string PeriodType001 = "PeriodType001";

    /// <summary>
    /// เดือน
    /// /// </summary>
    public const string PeriodType002 = "PeriodType002";

    /// <summary>
    /// ปี
    /// </summary>
    public const string PeriodType003 = "PeriodType003";
}

public static class SupplyMethodTypeConstant
{
    /// <summary>
    /// ซื้อ
    /// </summary>
    public const string Buy = "SMethodType001";

    /// <summary>
    /// จ้าง
    /// </summary>
    public const string Hire = "SMethodType002";

    /// <summary>
    /// เช่า
    /// </summary>
    public const string Rent = "SMethodType003";
}

public static class ApproverSupplyMethodTypeConstant
{
    /// <summary>
    /// บัญชีเบิกจ่าย
    /// </summary>
    public const string SectionApprover001 = "SectionApprover001";

    /// <summary>
    /// พรบ. 2560 >> ลงนามในสัญญา
    /// </summary>
    public const string SectionApprover002 = "SectionApprover002";
}

public static class BondTypeConstant
{
    /// <summary>
    /// เงินสด (เงินโอน)
    /// </summary>
    public const string PBondType001 = "PBondType001";

    /// <summary>
    /// หนังสือค้ำประกันของธนาคาร
    /// </summary>
    public const string PBondType002 = "PBondType002";

    /// <summary>
    /// หนังสือค้ำประกันอิเล็กทรอนิกส์ของธนาคาร
    /// </summary>
    public const string PBondType003 = "PBondType003";

    /// <summary>
    /// เช็คหรือดราฟท์ที่ธนาคารเซ็นสั่งจ่าย
    /// </summary>
    public const string PBondType004 = "PBondType004";

    /// <summary>
    /// หนังสือค้ำประกันของบริษัทเงินทุน
    /// </summary>
    public const string PBondType005 = "PBondType005";

    /// <summary>
    /// พันธบัตรรัฐบาลไทย
    /// </summary>
    public const string PBondType006 = "PBondType006";
}

public static class ContractFormatConstant
{
    /// <summary>
    /// (ก) จ้างก่อสร้าง
    /// </summary>
    public const string CFormat001 = "CFormat001";

    /// <summary>
    /// ซื้อสิทธิ (License Software)
    /// </summary>
    public const string CFormat005 = "CFormat005";

    /// <summary>
    /// จ้างบำรุงรักษาคอมพิวเตอร์
    /// </summary>
    public const string CFormat007 = "CFormat007";

    /// <summary>
    /// จ้างทำความสะอาด
    /// </summary>
    public const string CFormat009 = "CFormat009";

    /// <summary>
    /// จ้างรักษาความปลอดภัย
    /// </summary>
    public const string CFormat010 = "CFormat010";

    /// <summary>
    /// จ้างทำของ
    /// </summary>
    public const string CFormat013 = "CFormat013";

    /// <summary>
    /// จ้างควบคุมก่อสร้าง
    /// </summary>
    public const string CFormat014 = "CFormat014";

    /// <summary>
    /// จ้างที่ปรึกษา
    /// </summary>
    public const string CFormat015 = "CFormat015";

    /// <summary>
    /// (ข) จ้างก่อสร้าง
    /// </summary>
    public const string CFormat016 = "CFormat016";
}

public static class SplitPaymentConstant
{
    /// <summary>
    /// ชำระเงินเป็นรายงวด
    /// </summary>
    public const string SplitPayment001 = "SplitPayment001";

    /// <summary>
    /// ชำระเงินครั้งเดียว
    /// </summary>
    public const string SplitPayment002 = "SplitPayment002";

    /// <summary>
    /// ค่าบำรุงรักษาระบบ (MA) ชำระตามรอบสัญญา
    /// </summary>
    public const string SplitPayment003 = "SplitPayment003";
}

public static class SupplyMethodSpecialTypeConstant
{
    /// <summary>
    /// e-market
    /// </summary>
    public const string EMarket = "SMethod005";

    /// <summary>
    /// e-bidding
    /// </summary>
    public const string EBidding = "SMethod001";

    /// <summary>
    /// เฉพาะเจาะจง
    /// </summary>
    public const string Specific = "SMethod007";

    /// <summary>
    /// คัดเลือก
    /// </summary>
    public const string Selection = "SMethod006";

    /// <summary>
    /// พิเศษ
    /// </summary>
    public const string Special = "SMethod003";
}