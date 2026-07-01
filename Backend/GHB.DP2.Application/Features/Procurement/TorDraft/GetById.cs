namespace GHB.DP2.Application.Features.Procurement.TorDraft;

using System.ComponentModel;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Features.Procurement.TorDraft.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetTorDraftRequest(
    Guid ProcurementId,
    Guid Id);

public record TorTemplateComputerResponse(
    string? DocumentDescription,
    string? ManuelDescription,
    TorPreventiveMaintenanceResponse? PreventiveMaintenance,
    TorCorrectiveMaintenanceResponse? CorrectiveMaintenance,
    TorTrainingResponse? Training,
    TorTrainingItemResponse[]? TrainingItems,
    TorImpedimentResponse[]? Impediments);

public record TorTrainingResponse(
    int? TrainingCount,
    string? TrainingCountUnit,
    string? TrainingUnitId);

public record TorCorrectiveMaintenanceResponse(
    string? CmProductName,
    DateTimeOffset? StartDate,
    DateTimeOffset? EndDate,
    int? CmCount,
    string? CmUnit,
    int? CmCompleteCount,
    string? CmCompleteUnit,
    decimal? CmFinePercent,
    decimal? CmDisruptedFinePercent,
    string? DayStart,
    string? DayEnd,
    string? StartTime,
    string? EndTime,
    string? CmFinePercentUnit);

public record TorPreventiveMaintenanceResponse(
    string? PmProductName,
    int? PmCount,
    string? PmUnit,
    decimal? PmFinePct,
    decimal? PmFineAmount,
    string? Condition,
    int? DisruptedCount,
    string? DisruptedCountUnit,
    decimal? DisruptedPercent,
    decimal? DisruptedFinePercent,
    decimal? DisruptedFineAmount,
    string? PmFinePctUnit);

public record TorImpedimentResponse(
    Guid Id,
    int? Sequence,
    string? Description,
    decimal? ImpedimentValue);

public record TorTrainingItemResponse(
    Guid Id,
    int? Sequence,
    string? CourseName,
    int? PeriodDay,
    string? Place,
    int? TrainingCount,
    int? TotalPersonPerTime);

public record TorDraftObjectResponse(
    [property: Description("รหัสวัตถุประสงค์")]
    Guid Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("คำอธิบาย")] string? Description);

public record TorDraftQualificationResponse(
    [property: Description("รหัสคุณสมบัติผู้เสนอราคา")]
    Guid Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("คำอธิบาย")] string? Description);

public record TorDraftTechnicalPeriodResponse(
    [property: Description("รหัสระยะเวลาดำเนินงานทางเทคนิค")]
    Guid Id,
    [property: Description("ระยะเวลา")] int? Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("รหัสเงื่อนไขระยะเวลา")]
    string? PeriodConditionCode,
    [property: Description("วันที่เริ่มต้น")]
    DateTimeOffset? StartDate,
    [property: Description("วันที่สิ้นสุด")]
    DateTimeOffset? EndDate,
    [property: Description("รหัสเงื่อนไขการส่งมอบ")]
    string? DeliveryConditionCode,
    [property: Description("วันที่ส่งมอบ")]
    DateTimeOffset? DeliveryDate,
    [property: Description("รายละเอียดระยะเวลา")]
    TorDraftTechnicalPeriodDetailResponse[]? Details = null);

public record TorDraftTechnicalPeriodDetailResponse(
    [property: Description("รหัสรายละเอียดระยะเวลา")]
    Guid Id,
    [property: Description("สาขา")] string? Branch,
    [property: Description("จำนวนบุคลากร")]
    string? PersonalCount,
    [property: Description("วันที่เริ่มต้น")]
    DateTimeOffset? StartDate,
    [property: Description("วันที่สิ้นสุด")]
    DateTimeOffset? EndDate
);

public record TorDraftTechnicalSpecificationResponse(
    [property: Description("รหัสข้อกำหนดทางเทคนิค")]
    Guid Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("ชื่อ")] string? Name,
    [property: Description("คำอธิบาย")] string? Description,
    [property: Description("จำนวน")] int? Quantity,
    [property: Description("รหัสหน่วย")] string? UnitCode);

public record TorDraftBudgetDetailResponse(
    [property: Description("รหัสรายละเอียดงบประมาณ")]
    Guid Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("หน่วยงาน")] string? Department,
    [property: Description("ประเภทงบประมาณ")]
    string? BudgetType,
    [property: Description("รหัสโครงการ")] string? ProjectCode,
    [property: Description("หมายเลขบัญชี")]
    string? AccountNo,
    [property: Description("จำนวนงบประมาณ")]
    decimal? Budget
);

public record TorDraftBudgetResponse(
    [property: Description("รหัสงบประมาณ")]
    Guid Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("คำอธิบาย")] string? Description,
    [property: Description("จำนวนงบประมาณ")]
    decimal? BudgetAmount,
    [property: Description("รายละเอียดงบประมาณ")]
    TorDraftBudgetDetailResponse[]? Details
);

public record TorDraftPaymentTermDetailResponse(
    [property: Description("รหัสรายละเอียดเงื่อนไขการชำระเงิน")]
    Guid Id,
    [property: Description("หมายเลขงวด")] int? TermNumber,
    [property: Description("เปอร์เซ็นต์")] decimal? Percent,
    [property: Description("ระยะเวลา")] int? Period,
    [property: Description("คำอธิบาย")] string? Description
);

public record TorDraftPaymentTermResponse(
    [property: Description("รหัสเงื่อนไขการชำระเงิน")]
    Guid Id,
    [property: Description("รหัสประเภทการคิดสัดส่วน")]
    string? ProRateTypeCode,
    [property: Description("เปอร์เซ็นต์การชำระเงิน")]
    decimal? PaymentPercent,
    [property: Description("รายละเอียด")] string? Description,
    [property: Description("ระยะเวลา")] int? Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("ระยะเวลารวม")] int? TotalPeriod,
    [property: Description("รหัสประเภทระยะเวลารวม")]
    string? TotalPeriodTypeCode,
    bool? IsMA,
    [property: Description("รายละเอียดเงื่อนไขการชำระเงิน")]
    TorDraftPaymentTermDetailResponse[]? Details
);

public record TorDraftPaymentTermPeriodsResponse(
    Guid Id,
    int Sequence,
    string? Description,
    int? Quantity,
    string? PeriodTypeCode,
    int? TotalQuantity,
    string? TotalPeriodTypeCode
);

public record TorDraftWarrantyResponse(
    [property: Description("รหัสเงื่อนไขการรับประกัน")]
    Guid Id,
    [property: Description("มีการรับประกัน")]
    bool? HasWarranty,
    [property: Description("ระยะเวลา")] int? Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther
);

public record TorDraftFineRateResponse(
    [property: Description("รหัสอัตราปรับ")]
    Guid Id,
    [property: Description("ลำดับ")] int? Sequence,
    [property: Description("รายละเอียด")] string? Description,
    [property: Description("อัตรา")] decimal? Rate,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("รหัสเงื่อนไข")]
    string? ConditionCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther
);

public record DocumentVersionResponse(
    [property: Description("รหัสไฟล์")]
    Guid FileId,
    [property: Description("เวอร์ชัน")]
    string Version,
    [property: Description("วันที่สร้าง")]
    DateTimeOffset CreatedAt,
    [property: Description("ชื่อผู้สร้าง")]
    string CreatedByName,
    [property: Description("เป็นเวอร์ชันปัจจุบัน")]
    bool IsCurrent);

public record TorDraftAcceptorResponse(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
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
    string? DepartmentCode = default,
    [property: Description("รหัสผู้ใช้งานผู้ปฏิบัติหน้าที่แทน")]
    Guid? DelegateeUserId = default);

public record TorDraftResponse(
    [property: Description("รหัส TOR Draft")]
    Guid Id,
    [property: Description("เลขที่อ้างอิง")]
    string ReferenceNumber,
    [property: Description("วันที่เอกสาร")]
    DateTimeOffset? DocumentDate,
    [property: Description("หมายเลขโทรศัพท์")]
    string? TelephoneNumber,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementDto Procurement,
    [property: Description("มีหลักประกันการเสนอราคา")]
    bool? BidGuarantee,
    [property: Description("เป็นสต็อก")] bool? IsStock,
    [property: Description("เหตุผล")] string? Reason,
    [property: Description("เกณฑ์การประเมิน")]
    string? EvaluationCriteria,
    [property: Description("มีการเปลี่ยนแปลง")]
    bool IsChange,
    [property: Description("ถูกยกเลิก")] bool IsCancel,
    [property: Description("สถานะ")] string Status,
    [property: Description("ใช้งานอยู่")] bool IsActive,
    [property: Description("รหัสเทมเพลตเอกสาร TOR")]
    string? TorDocumentTemplateCode,
    [property: Description("รหัสเอกสาร TOR Draft")]
    Guid? TorDraftDocumentId,
    [property: Description("รหัสเอกสาร TOR Draft เปลี่ยนแปลง")]
    bool? IsTorDraftDocumentIdReplaced,
    [property: Description("รหัสเอกสารอนุมัติ TOR Draft")]
    Guid? TorDraftApprovalDocumentId,
    [property: Description("รหัสเอกสารอนุมัติ TOR Draft เปลี่ยนแปลง")]
    bool? IsTorDraftApprovalDocumentIdReplaced,
    [property: Description("ประวัติเวอร์ชันเอกสาร TOR")]
    DocumentVersionResponse[] TorDocumentVersions,
    [property: Description("ประวัติเวอร์ชันเอกสารอนุมัติ")]
    DocumentVersionResponse[] ApprovalDocumentVersions,
    [property: Description("วัตถุประสงค์")]
    TorDraftObjectResponse[]? Objects,
    [property: Description("คุณสมบัติผู้เสนอราคา")]
    TorDraftQualificationResponse[]? Qualifications,
    [property: Description("ข้อกำหนดทางเทคนิค")]
    TorDraftTechnicalSpecificationResponse[]? TechnicalSpecifications,
    [property: Description("ระยะเวลาดำเนินงาน")]
    TorDraftTechnicalPeriodResponse[]? TechnicalPeriods,
    [property: Description("งบประมาณ")] TorDraftBudgetResponse[]? Budgets,
    [property: Description("เงื่อนไขการชำระเงิน")]
    TorDraftPaymentTermResponse[]? PaymentTerms,
    [property: Description("เงื่อนไขการรับประกัน")]
    TorDraftWarrantyResponse[]? Warranties,
    [property: Description("อัตราปรับ")] TorDraftFineRateResponse[]? FineRates,
    [property: Description("ผู้อนุมัติ")] TorDraftAcceptorResponse[]? Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[]? Assignees,
    [property: Description("หมายเหตุขอยกเลิก")]
    string? CancelReason,
    [property: Description("หมายเหตุขอเปลี่ยนแปลง")]
    string? ChangeReason,
    bool? IsContractGuarantee,
    decimal? PercentageContract,
    string? SupplyMethodTypeCode,
    string? DocumentDescription,
    TorPreventiveMaintenanceResponse? PreventiveMaintenance,
    TorCorrectiveMaintenanceResponse? CorrectiveMaintenance,
    TorTrainingResponse? Training,
    TorTrainingItemResponse[]? TrainingItems,
    string? ManuelDescription,
    TorImpedimentResponse[]? Impediments,
    bool? IsMigration,
    bool? IsMA,
    bool? IsCM,
    bool? IsPM,
    bool? IsImpediment,
    bool? IsTraining,
    TorDraftPaymentTermPeriodsResponse[]? PaymentTermPeriods);

public class GetTorDraftDetail : TorDraftEndpointBase<GetTorDraftRequest, Results<Ok<TorDraftResponse>, NotFound<string>, BadRequest<string>>>
{
    public GetTorDraftDetail(
        Dp2DbContext dbContext,
        IOperationService operationService,
        ICommandTextService commandTextService,
        ILogger<GetTorDraftDetail> logger)
        : base(logger, dbContext, operationService, commandTextService)
    {
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/tordraft/{Id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/TorDraft")
                              .WithName("GetTorDraftDetail")
                              .Produces<TorDraftResponse>()
                              .Produces<string>(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<TorDraftResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(GetTorDraftRequest req, CancellationToken ct)
    {
        var entity = await this.GetTorDraftById(
            PpTorDraftId.From(req.Id),
            ProcurementId.From(req.ProcurementId),
            ct);

        var response = this.MapToResponse(entity);

        return TypedResults.Ok(response);
    }
}