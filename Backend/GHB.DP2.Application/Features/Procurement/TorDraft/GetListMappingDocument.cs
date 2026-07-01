namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Procurement.Procurement.Abstact;
using GHB.DP2.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record TorDraftObjectReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description);

public record TorDraftQualificationReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description);

public record TorDraftTechnicalPeriodReplace(
    [property: Description("ระยะเวลา")] int? Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("รหัสเงื่อนไขระยะเวลา")]
    string? PeriodConditionCode,
    [property: Description("วันที่เริ่มต้น")]
    DateTimeOffset? StartDate,
    [property: Description("วันที่สิ้นสุด")]
    DateTimeOffset? EndDate,
    DateTimeOffset? DeliveryDate,
    [property: Description("รายละเอียดระยะเวลา")]
    TorDraftTechnicalPeriodDetailReplace[]? Details = null);

public record TorDraftTechnicalPeriodSummaryReplace(
    [property: Description("ระยะเวลา")]
    string? Period,
    [property: Description("ประเภทระยะเวลา")]
    string? PeriodTypeLabel,
    [property: Description("เงื่อนไขระยะเวลา")]
    string? PeriodConditionLabel,
    [property: Description("วันที่เริ่มต้น")]
    string? StartDate,
    [property: Description("วันที่สิ้นสุด")]
    string? EndDate,
    string? DeliveryDate);

public record TorDraftTechnicalPeriodDetailReplace(
    [property: Description("สาขา")] string Branch,
    [property: Description("จำนวนบุคลากร")]
    string PersonalCount,
    [property: Description("วันที่เริ่มต้น")]
    DateTimeOffset StartDate,
    [property: Description("วันที่สิ้นสุด")]
    DateTimeOffset EndDate
);

public record TorDraftTechnicalSpecificationReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อ")] string Name,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวน")] string Quantity,
    [property: Description("รหัสหน่วยวัด")] string UnitCode,
    [property: Description("หน่วยวัด")] string UnitLabel,
    [property: Description("รายละเอียดทางเทคนิค")] string? TechnicalDetail);

public record TorDraftBudgetDetailReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("หน่วยงาน")] string Department,
    [property: Description("ประเภทงบประมาณ")]
    string BudgetType,
    [property: Description("รหัสโครงการ")] string? ProjectCode,
    [property: Description("หมายเลขบัญชี")]
    string AccountNo,
    [property: Description("จำนวนงบประมาณ")]
    decimal Budget
);

public record TorDraftBudgetReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวนงบประมาณ")]
    decimal BudgetAmount,
    [property: Description("จำนวนงบประมาณ")]
    string? BudgetAmountText,
    [property: Description("รายละเอียดงบประมาณ")]
    TorDraftBudgetDetailReplace[]? Details
);

public record TorDraftBudgetSummaryReplace(
    [property: Description("จำนวนงบประมาณ")]
    string BudgetAmount,
    [property: Description("จำนวนงบประมาณ (ตัวอักษร)")]
    string BudgetAmountText);

public record TorDraftPaymentTermDetailReplace(
    [property: Description("หมายเลขงวด")] int TermNumber,
    [property: Description("เปอร์เซ็นต์")] decimal Percent,
    [property: Description("ระยะเวลา (วัน)")] int Period,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("เงื่อนไขการชำระเงิน")] string PaymentTermDetail,
    [property: Description("รายละเอียดเงื่อนไขการชำระเงิน")] string PaymentDescription);

public record TorDraftPaymentTermReplace(
    [property: Description("รหัสประเภทการคิดสัดส่วน")]
    string? ProRateTypeCode,
    [property: Description("เปอร์เซ็นต์การชำระเงิน")]
    decimal? PaymentPercent,
    [property: Description("งวดที่")]
    int? Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("ประเภทระยะเวลา")]
    string? PeriodTypeLabel,
    [property: Description("ระยะเวลารวม")]
    int? TotalPeriod,
    [property: Description("รหัสประเภทระยะเวลารวม")]
    string? TotalPeriodTypeCode,
    string? TotalPeriodTypeLabel,
    [property: Description("รายละเอียด")]
    string? Description,
    [property: Description("รายละเอียดเงื่อนไขการชำระเงิน")]
    TorDraftPaymentTermDetailReplace[]? Details,
    [property: Description("ข้อความการชำระเงิน")]
    IEnumerable<string> PaymentDescription
);

public record TorDraftWarrantyReplace(
    [property: Description("มีการรับประกัน")]
    bool HasWarranty,
    [property: Description("ระยะเวลา")] string? Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("ประเภทระยะเวลา")]
    string? PeriodTypeLabel,
    [property: Description("รหัสเงื่อนไขอื่น ๆ")]
    string? ConditionOtherCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOtherLabel
);

public record TorDraftFineRateReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายละเอียด")] string Description,
    [property: Description("อัตราร้อยละ")] string Rate,
    [property: Description("รหัสประเภทระยะเวลา")]
    string PeriodTypeCode,
    [property: Description("ประเภทระยะเวลา")]
    string PeriodTypeLabel,
    [property: Description("รหัสเงื่อนไข")]
    string ConditionCode,
    [property: Description("เงื่อนไข")]
    string ConditionLabel,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther);

public record TorDraftAcceptorReplace(
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้")] Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อเต็ม")] string FullName,
    [property: Description("ชื่อตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัสผู้รับมอบหมาย")]
    Guid? DelegateeId,
    [property: Description("สถานะผู้อนุมัติ")]
    AcceptorStatus Status,
    [property: Description("วันที่ดำเนินการ")]
    DateTimeOffset? ActionAt,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("ใช้งานอยู่")] bool IsActive,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool? IsCurrent,
    [property: Description("รหัสตำแหน่งกรรมการ")]
    string? CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งกรรมการ")]
    string? CommitteePositionName,
    [property: Description("ไม่สามารถปฏิบัติหน้าที่ได้")]
    bool IsUnableToPerformDuties = false,
    [property: Description("รหัสหน่วยงาน")]
    string? DepartmentsCode = default);

public record CreatorReplace(
    [property: Description("รหัสผู้ใช้งาน")] Guid UserId,
    [property: Description("ลายเซ็นต์")] string Signature,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

public record TorCommitteeReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("คณะกรรมการหรือผู้จัดทำ เห็นชอบ")]
    string Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งจริง คณะกรรมการ หรือผู้จัดทำ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard,
    [property: Description("ตำแหน่งปฏิบัติหน้าที่แทน คณะกรรมการ หรือผู้จัดทำ")]
    string? Delegate);

public record TorAcceptorReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("การดำเนินการ")]
    string Action,
    [property: Description("เห็นชอบหรืออนุมัติ")]
    string Signature,
    [property: Description("ชื่อคณะกรรมการหรือผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้ผู้เห็นชอบ หรืออนุมัติ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string? Delegate,
    [property: Description("หมายเหตุ")]
    string? Remark);

public record TorDraftImpedimentReplace(
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รายการค่าตัวถ่วง")]
    string? Description,
    [property: Description("ค่าตัวถ่วง")]
    string? ImpedimentValue
);

public record TorDraftTrainingItemReplace(
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("ชื่อหลักสูตร")]
    string? CourseName,
    [property: Description("จำนวนวัน")]
    string? PeriodDay,
    [property: Description("สถานที่อบรม")]
    string? Place,
    [property: Description("จำนวนครั้ง")]
    string? TrainingCount,
    [property: Description("จำนวนคนต่อครั้ง")]
    string? TotalPersonPerTime
);

public record TorTrainingReplace(
    [property: Description("ต้องจัดการฝึกอบรมภายในเวลา")]
    string? TrainingCount,
    string? TrainingCountUnit,
    string? TrainingUnit);

public record TorCorrectiveMaintenanceReplace(
    [property: Description("ต้องให้บริการซ่อมแซม ในวัน")]
    string? DayStart,
    [property: Description("ถึงวัน")]
    string? DayEnd,
    [property: Description("เวลา")]
    string? StartTime,
    [property: Description("เริ่มจัดการซ่อมแซมภายใน")]
    string? CmCount,
    string? CmCountText,
    string? CmUnit,
    [property: Description("ให้แล้วเสร็จภายใน")]
    string? CmCompleteCount,
    string? CmCompleteCountText,
    string? CmCompleteUnit,
    [property: Description("ยินยอมให้คิดค่าปรับ")]
    string? CmFinePercent,
    string? CmFinePercentText,
    [property: Description("ค่าปรับกรณีเกิดความเสียหาย")]
    string? CmDisruptedFinePercent,
    string? CmFinePercentUnit);

public record TorPreventiveMaintenanceReplace(
    [property: Description("ชื่อพัสดุ")]
    string? PmProductName,
    [property: Description("ตรวจสอบบำรุงรักษาอย่างน้อยต่อครั้ง")]
    string? PmCount,
    string? PmCountText,
    string? PmUnit,
    [property: Description("มิฉะนั้นจะยอมให้คิดค่าปรับในอัตราร้อยละ")]
    string? PmFinePct,
    string? PmFinePctUnit,
    [property: Description("มูลค่ารวมของการปรับแต่ละครั้งต่ำสุด")]
    string? PmFineAmount,
    string? PmFineAmountText,
    [property: Description("เงื่อนไขการบำรุงรักษา")]
    string? Condition,
    [property: Description("ขัดข้องรวมตามเกณฑ์ไม่เกินเดือนละ")]
    string? DisruptedCount,
    string? DisruptedCountText,
    string? DisruptedCountUnit,
    [property: Description("หรือไม่เกินร้อยละ (%) ของเวลาทั้งหมดของเดือนนั้น")]
    string? DisruptedPercent,
    string? DisruptedPercentText,
    [property: Description("มิฉะนั้นจะถูกปรับอัตราชั่วโมงละ (%)")]
    string? DisruptedFinePercent,
    string? DisruptedFineAmount,
    string? DisruptedFineAmountText);

public record TorDraftReplace(
    [property: Description("วันที่ทำเอกสาร")]
    string? TorDraftDate,
    [property: Description("เลขที่อ้างอิง")]
    string ReferenceNumber,
    [property: Description("เลขที่แต่งตั้ง")]
    string? AppointmentNumber,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("วันที่ทำการแต่งตั้ง")]
    string MemorandumDate,
    [property: Description("บุคคล/คณะกรรมการจัดทำร่างขอบเขตของงาน")]
    string? TorDraftCommitteeType,
    [property: Description("อำนาจอนุมัติตามคำสั่งธนาคาร")]
    string? CommandText,
    [property: Description("หมายเลขโทรศัพท์")]
    string? TelephoneNumber,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplaceDto Procurement,
    [property: Description("มีหลักประกันการเสนอราคา")]
    bool BidGuarantee,
    [property: Description("เป็นสต็อก")] bool IsStock,
    [property: Description("เหตุผล")] string? Reason,
    [property: Description("เกณฑ์การประเมิน")]
    string? EvaluationCriteria,
    [property: Description("มีการเปลี่ยนแปลง")]
    bool IsChange,
    [property: Description("ถูกยกเลิก")] bool IsCancel,
    [property: Description("สถานะ")] string Status,
    [property: Description("ใช้งานอยู่")] bool IsActive,
    [property: Description("รหัสเทมเพลตเอกสาร TOR")]
    string TorDocumentTemplateCode,
    [property: Description("รหัสเอกสาร TOR Draft")]
    Guid? TorDraftDocumentId,
    [property: Description("รหัสเอกสารอนุมัติ TOR Draft")]
    Guid? TorDraftApprovalDocumentId,
    [property: Description("วัตถุประสงค์")]
    TorDraftObjectReplace[]? Objects,
    [property: Description("คุณสมบัติผู้เสนอราคา")]
    TorDraftQualificationReplace[]? Qualifications,
    [property: Description("ข้อกำหนดทางเทคนิค")]
    TorDraftTechnicalSpecificationReplace[]? TechnicalSpecifications,
    [property: Description("ระยะเวลาดำเนินงาน")]
    TorDraftTechnicalPeriodReplace[]? TechnicalPeriods,
    TorDraftTechnicalPeriodSummaryReplace? TechnicalPeriod,
    [property: Description("งบประมาณ")] TorDraftBudgetReplace[]? Budgets,
    [property: Description("งบประมาณรวม")] TorDraftBudgetSummaryReplace? BudgetSummary,
    [property: Description("เงื่อนไขการชำระเงิน")]
    TorDraftPaymentTermReplace[]? PaymentTerms,
    TorDraftPaymentTermReplace? PaymentTerm,
    [property: Description("เงื่อนไขการชำระเงิน MA")]
    TorDraftPaymentTermReplace? MaPaymentTerm,
    [property: Description("เงื่อนไขการรับประกัน")]
    TorDraftWarrantyReplace[]? Warranties,
    TorDraftWarrantyReplace? Warranty,
    [property: Description("อัตราปรับ")]
    TorDraftFineRateReplace? FineRates,
    TorDraftFineRateReplace? FineRatesMA,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[]? Assignees,
    [property: Description("หมายเหตุขอยกเลิก")]
    string? CancelReason,
    [property: Description("หมายเหตุขอเปลี่ยนแปลง")]
    string? ChangeReason,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Creator,
    [property: Description("คณะกรรมการหรือผู้จัดทำ")]
    TorCommitteeReplace[] Committees,
    [property: Description("ผู้อนุมัติ")]
    TorAcceptorReplace[] Acceptors,
    [property: Description("เอกสารประกอบการเสนอราคา(ถ้ามี)")]
    string? DocumentDescription,
    [property: Description("ต้องจัดส่งเอกสาร/คู่มือให้แก่ธนาคารภายในเวลาที่กำหนดดังต่อไปนี้ (IT)")]
    string? ManuelDescription,
    [property: Description("การบำรุงรักษา (Preventive Maintenance) (IT)")]
    TorPreventiveMaintenanceReplace? PreventiveMaintenance,
    [property: Description("การซ่อมแซมแก้ไข (Corrective Maintenance)")]
    TorCorrectiveMaintenanceReplace? CorrectiveMaintenance,
    [property: Description("การฝึกอบรม (IT)")]
    TorTrainingReplace? Training,
    [property: Description("การกำหนดตัวถ่วง (IT)")]
    TorDraftImpedimentReplace[]? Impediments,
    [property: Description("การฝึกอบรม (IT)")]
    TorDraftTrainingItemReplace[]? TrainingItems,
    string CommitteeTorSection,
    string CommitteeMedianPriceSection,
    [property: Description("วันที่")]
    string? DeliveryDescriptions,
    [property: Description("ผู้รับมอบหมาย")]
    JorPorCommentReplace? JorPorComment);

public class GetListMappingTorDraftDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingTorDraftDocumentEndpoint(ILogger<GetListMappingTorDraftDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/TorDraft"));
        this.Get("procurement/tordraft/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(TorDraftReplace);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}