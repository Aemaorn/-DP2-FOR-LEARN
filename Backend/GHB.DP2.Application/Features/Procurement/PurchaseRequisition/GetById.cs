namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PurchaseRequisitionDocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetPurchaseRequisitionRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ProcurementId,
    Guid? PurchaseRequisitionId);

public record GetPurchaseRequisitionResponse(
    [property: Description("รหัสใบขอซื้อขอจ้าง")]
    PpPurchaseRequisitionId? Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    ProcurementId ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementDto? Procurement,
    [property: Description("รหัส TOR Draft")]
    PpTorDraftId? TorDraftId,
    [property: Description("รหัสเอกสารใบขอซื้อขอจ้าง")]
    Guid? PurchaseRequisitionDocumentId,
    [property: Description("รหัสเอกสารใบขอซื้อขอจ้าง เปลี่ยนแปลง")]
    bool? IsPurchaseRequisitionDocumentIdReplaced,
    [property: Description("ข้อมูลใบขอซื้อขอจ้าง")]
    GetPurchaseRequisition Requisition,
    [property: Description("งบประมาณ")] IEnumerable<GetPurchaseRequisitionBudget> Budgets,
    [property: Description("เงื่อนไขการรับประกัน")]
    IEnumerable<GetPurchaseRequisitionWarranty> Warranties,
    [property: Description("เงื่อนไขการชำระเงิน")]
    IEnumerable<GetPurchaseRequisitionPaymentTerm> PaymentTerms,
    [property: Description("อัตราปรับ")] IEnumerable<GetPurchaseRequisitionFineRate> FineRates,
    [property: Description("คณะกรรมการ")] IEnumerable<GetPurchaseRequisitionCommittee> Committees,
    [property: Description("ผู้อนุมัติ")] IEnumerable<PpPurchaseRequisitionAcceptorResponse> Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    IEnumerable<AssigneeResponse> Assignees,
    [property: Description("ขอบเขตงาน")] IEnumerable<GetTechnicalSpecification> ScopeOfWorks,
    [property: Description("เหตุผล")] string? Reason,
    [property: Description("ขอบเขตงาน")] IEnumerable<GetTorObjectResponse> TorObjectResponses,
    [property: Description("เป็นคณะกรรมการจัดซื้อจัดจ้าง")]
    bool IsProcurementCommittee,
    [property: Description("เป็นคณะกรรมการจัดซื้อจัดจ้าง")]
    bool IsInspectCommittee,
    [property: Description("เป็นคณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)")]
    bool IsMaCommittee,
    [property: Description("เป็นคณะควบคุมงาน (เฉพาะงานก่อสร้าง)")]
    bool IsSupCommittee,
    bool HasPermission,
    [property: Description("หมายเหตุส่งกลับแก้ไขจากรายงานขอซื้อขอจ้าง (จพ.005)")]
    string? SendEditRemark,
    decimal? Budget,
    bool IsCommercialMaterial,
    BusinessUnitId? DepartmentCode,
    ParameterCode? SupplyMethodCode,
    [property: Description("ประวัติเวอร์ชันเอกสาร")]
    PurchaseRequisitionDocumentVersionResponse[] DocumentVersions,
    string? TorTemplateCode,
    string? PaymentTypeCode);

public record GetPurchaseRequisition(
    [property: Description("เลขที่ใบขอซื้อขอจ้าง")]
    string? PurchaseRequisitionNumber,
    [property: Description("เลข EGP")] string? EgpNumber,
    [property: Description("เลข PR")] string? PrNumber,
    [property: Description("คำอธิบาย")] string? Description,
    [property: Description("ข้อมูลความสมเหตุสมผลของราคา")]
    string? PriceReasonablenessInfo,
    [property: Description("เบอร์โทร")] string? Telephone,
    [property: Description("ราคากลาง")] decimal? MedianPriceAmount,
    [property: Description("รหัสเกณฑ์การประเมิน")]
    string? EvaluationCriteriaCode,
    [property: Description("ระยะเวลาการส่งมอบ")]
    int? DeliveryPeriod,
    [property: Description("รหัสประเภทระยะเวลาส่งมอบ")]
    string? DeliveryPeriodTypeCode,
    [property: Description("รหัสเงื่อนไขการส่งมอบ")]
    string? DeliveryConditionCode,
    [property: Description("มีอัตราปรับ")] bool HasFineRate,
    [property: Description("มีการรับประกัน")]
    bool HasWarranty,
    [property: Description("ระยะเวลาการรับประกัน")]
    int? WarrantyPeriod,
    [property: Description("รหัสประเภทระยะเวลาการรับประกัน")]
    string? WarrantyPeriodCode,
    [property: Description("รหัสเงื่อนไขการรับประกัน")]
    string? WarrantyConditionCode,
    [property: Description("มีหลักประกันสัญญา")]
    bool HasContractGuarantee,
    [property: Description("มีคณะกรรมการตรวจรับ")]
    bool HasInspectionCommittee,
    [property: Description("มีผู้ควบคุมการก่อสร้าง")]
    bool HasConstructionSupervisor,
    Guid? PurchaseRequisitionDocumentId,
    bool? IsPurchaseRequisitionDocumentIdReplace,
    [property: Description("สถานะใบขอซื้อขอจ้าง")]
    PurchaseRequisitionStatus Status,
    [property: Description("ระยะเวลา")] DateTimeOffset? DeliveryDate,
    [property: Description("วันที่เอกสาร")] DateTimeOffset? DocumentDate);

public record GetPurchaseRequisitionBudget(
    [property: Description("รหัสงบประมาณ")]
    Guid? Id,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวนงบประมาณ")]
    decimal BudgetAmount,
    [property: Description("รายละเอียดงบประมาณ")]
    IEnumerable<GetPurchaseRequisitionBudgetDetail> Details,
    [property: Description("ลำดับ")] int Sequence);

public record GetPurchaseRequisitionBudgetDetail(
    [property: Description("รหัสรายละเอียดงบประมาณ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("หน่วยงาน")] string Department,
    [property: Description("ประเภทงบประมาณ")]
    string BudgetType,
    [property: Description("รหัสโครงการ")] string? ProjectCode,
    [property: Description("หมายเลขบัญชี")]
    string AccountNo,
    [property: Description("จำนวนงบประมาณ")]
    decimal Budget);

public record GetPurchaseRequisitionWarranty(
    [property: Description("รหัสการรับประกัน")]
    Guid? Id,
    [property: Description("มีการรับประกัน")]
    bool HasWarranty,
    [property: Description("ระยะเวลา")] int Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther);

public record GetPurchaseRequisitionPaymentTerm(
    [property: Description("รหัสเงื่อนไขการชำระเงิน")]
    Guid? Id,
    [property: Description("อันดับงวด")] int? TermNumber,
    [property: Description("ร้อยละ")] decimal? Percent,
    [property: Description("ระยะเวลา")] int? Period,
    [property: Description("คำอธิบาย")] string? Description,
    string? PaymentTypeCode,
    string? TotalPeriodTypeCode,
    int? TotalPeriod,
    string? PeriodTypeCode,
    bool? IsMA);

public record GetPurchaseRequisitionFineRate(
    [property: Description("รหัสอัตราปรับ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("อัตรา")] decimal Percentage,
    [property: Description("รหัสประเภทระยะเวลา")]
    string PeriodTypeCode,
    [property: Description("รหัสเงื่อนไข")]
    string ConditionCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther);

public record GetPurchaseRequisitionCommittee(
    [property: Description("รหัสคณะกรรมการ")]
    Guid? Id,
    [property: Description("ประเภทกลุ่ม")] GroupType GroupType,
    [property: Description("รหัสผู้ใช้")] Guid SuUserId,
    [property: Description("ชื่อเต็ม")] string FullName,
    [property: Description("ชื่อตำแหน่ง")] string PositionName,
    [property: Description("รหัสตำแหน่งกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งกรรมการ")]
    string CommitteePositionsName,
    [property: Description("ลำดับ")] int Sequence,
    string DepartmentCode);

public record GetTechnicalSpecification(
    [property: Description("รหัสข้อกำหนดทางเทคนิค")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อ")] string Name,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("รหัสหน่วย")] ParameterCode? UnitCode);

public record GetTorObjectResponse(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description);

public class GetPurchaseRequisitionRequestValidator : Validator<GetPurchaseRequisitionRequest>
{
    public GetPurchaseRequisitionRequestValidator()
    {
        this.RuleFor(x => x.ProcurementId)
            .NotEmpty().WithMessage("ProcurementId is required.");
    }
}

public class GetPurchaseRequisitionEndpoint : PurchaseRequisitionEndpointBase<GetPurchaseRequisitionRequest, Results<Ok<GetPurchaseRequisitionResponse>, NotFound<string>>>
{
    public GetPurchaseRequisitionEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetPurchaseRequisitionEndpoint> logger)
        : base(logger, dbContext)
    {
    }

    public override void Configure()
    {
        this.Get("procurement/{ProcurementId:guid}/JorPor04/{PurchaseRequisitionId:guid?}");
        this.Description(b => b
                              .WithTags("Procurement/PurchaseRequisition")
                              .WithName("GetPurchaseRequisitionById")
                              .Produces<GetPurchaseRequisitionResponse>()
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok<GetPurchaseRequisitionResponse>, NotFound<string>>> HandleRequestAsync(GetPurchaseRequisitionRequest req, CancellationToken ct)
    {
        var query =
            req.PurchaseRequisitionId.IsNull()
                ? this.GetByProcurementIdAsync(ProcurementId.From(req.ProcurementId), ct)
                : this.GetByIdAsync(
                    ProcurementId.From(req.ProcurementId),
                    PpPurchaseRequisitionId.From(req.PurchaseRequisitionId.Value),
                    ct: ct,
                    req.UserId);

        var response = await query;

        if (response is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลคำขอจัดซื้อจัดจ้าง");
        }

        return TypedResults.Ok(response);
    }
}