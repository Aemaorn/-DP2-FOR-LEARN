namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Procurement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record PurchaseRequisitionReplaceDto(
    string SectionApproveName,
    [property: Description("เรื่อง")]
    string? Subject,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    ProcurementReplate Procurement,
    [property: Description("ข้อมูลใบขอซื้อขอจ้าง")]
    PurchaseRequisitionReplate Requisition,
    [property: Description("ขอบเขตงาน")] IEnumerable<TorObjectReplate> TorObject,
    [property: Description("งบประมาณ")] IEnumerable<BudgetReplate> Budgets,
    [property: Description("กำหนดเวลาที่ต้องการใช้พัสดุนั้น")]
    DeliveryReplate Delivery,
    string? DeliveryPeriod,
    [property: Description("หลักเกณฑ์การคัดเลือก")]
    string? EvaluationCriteriaValue,
    [property: Description("ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง")]
    CommitteeSectionReplate? ProcurementCommittee,
    [property: Description("ผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุ")]
    CommitteeSectionReplate? InspectCommittee,
    [property: Description("ผู้ตรวจรับพัสดุ/คณะกรรมการตรวจรับพัสดุ (งานจ้างบริการบำรุงรักษา)")]
    CommitteeSectionReplate? MaCommittee,
    [property: Description("ผู้ควบคุมงาน (เฉพาะงานก่อสร้าง)")]
    CommitteeSectionReplate? SupCommittee,
    [property: Description("เงื่อนไขการรับประกัน")]
    IEnumerable<WarrantyReplate> Warranties,
    [property: Description("เงื่อนไขการชำระเงิน")]
    IEnumerable<PaymentTermReplate> PaymentTerms,
    [property: Description("อัตราปรับ")] IEnumerable<FineRateReplate> FineRates,
    [property: Description("เหตุผล")] string? Reason,
    [property: Description("วันที่ดำเนินการทำ")] string? AcceptorDate,
    [property: Description("ผู้จัดทำ")] CreateReplate? Create,
    [property: Description("ผู้เห็นชอบ/อนุมัติ")]
    IEnumerable<AcceptorReplace> Acceptor,
    [property: Description("ขอบเขตงาน")]
    TorTechnicalSpecificationReplace[]? TechnicalSpecifications);

public record ProcurementReplate(
    [property: Description("เลขที่การจัดซื้อจัดจ้าง")]
    ProcurementNumber? ProcurementNumber,
    [property: Description("ประเภทการจัดซื้อจัดจ้าง")]
    string DepartmentName,
    [property: Description("เลขที่แผนงาน")]
    string? PlanNumber,
    [property: Description("ชื่อแผนงาน")] string PlanName,
    [property: Description("งบประมาณ")] string? Budget,
    [property: Description("งบประมาณ ภาษาไทย")]
    string? BudgetText,
    [property: Description("ปีงบประมาณ")] decimal? BudgetYear,
    [property: Description("วิธีการจัดหา")]
    string? SupplyMethod,
    [property: Description("ประเภทวิธีการจัดหา")]
    string? SupplyMethodType,
    [property: Description("ประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialType,
    [property: Description("วันที่คาดว่าจะดำเนินการจัดซื้อจัดจ้าง")]
    DateTimeOffset? ExpectingProcurementAt,
    [property: Description("เป็นสต็อก")] bool IsStock,
    [property: Description("เป็นวัสดุเชิงพาณิชย์")]
    bool IsCommercialMaterial);

public record PurchaseRequisitionReplate(
    [property: Description("เลขที่ใบขอซื้อขอจ้าง")]
    string? PurchaseRequisitionNumber,
    [property: Description("เลข EGP")] string? EgpNumber,
    [property: Description("เลข PR")] string? PrNumber,
    [property: Description("คำอธิบาย")] string? Description,
    [property: Description("ข้อมูลความสมเหตุสมผลของราคา")]
    string? PriceReasonablenessInfo,
    [property: Description("ราคากลาง")] string? MedianPriceAmount,
    [property: Description("มีหลักประกันสัญญา")]
    bool HasContractGuarantee,
    [property: Description("มีคณะกรรมการตรวจรับ")]
    bool HasInspectionCommittee,
    [property: Description("มีผู้ควบคุมการก่อสร้าง")]
    bool HasConstructionSupervisor);

public record BudgetReplate(
    [property: Description("ลำดับหน่วยงาน")]
    int Sequence,
    [property: Description("รหัสหน่วยงาน")]
    string Department,
    [property: Description("รหัสบัญชี")] string AccountNo,
    [property: Description("จำนวนเงิน")] string Amount);

public record DeliveryReplate(
    [property: Description("ประเภทการจัดส่งพัสดุ")]
    string Type,
    [property: Description("ระยะเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ (วัน)")]
    int Period,
    [property: Description("หลักเกณฑ์การคัดเลือก")]
    string Condition);

public record TorObjectReplate(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("คำอธิบาย")] string Description);

public record TorTechnicalSpecificationReplace(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อ")] string Name,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("รหัสหน่วยวัด")] string UnitCode,
    [property: Description("หน่วยวัด")] string UnitLabel);

public record CommitteeSectionReplate(
    [property: Description("รหัสคณะกรรมการ")]
    string Section,
    IEnumerable<CommitteeReplate> Committees);

public record CommitteeReplate(
    [property: Description("ลำดับคณะกรรมการ")]
    string? Sequence,
    [property: Description("ชื่อ-นามสกุล")]
    string? FullName,
    [property: Description("ตำแหน่ง")] string? Position,
    [property: Description("ตำแหน่งคณะกรรมการ")]
    string? CommitteePosition);

public record WarrantyReplate(
    [property: Description("มีการรับประกัน")]
    bool HasWarranty,
    [property: Description("ระยะเวลา")] int Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther);

public record PaymentTermReplate(
    [property: Description("อันดับงวด")] int TermNumber,
    [property: Description("ร้อยละ")] decimal Percent,
    [property: Description("ระยะเวลา")] int Period,
    [property: Description("คำอธิบาย")] string Description);

public record FineRateReplate(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("อัตรา")] decimal Rate,
    [property: Description("รหัสประเภทระยะเวลา")]
    string PeriodTypeCode,
    [property: Description("รหัสเงื่อนไข")]
    string ConditionCode,
    [property: Description("เงื่อนไขอื่น ๆ")]
    string? ConditionOther);

public record ScopeOfWorksReplate(
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อ")] string Name,
    [property: Description("คำอธิบาย")] string Description,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("หน่วย")] string? Unit);

public record CreateReplate(
    [property: Description("ผู้จัดทำ")] string Action,
    [property: Description("ชื่อผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งผู้จัดทำ")]
    string Position);

public record AcceptorReplace(
    [property: Description("เห็นชอบ หรืออนุมัติ")]
    string Action,
    [property: Description("ชื่อผู้เห็นชอบ หรืออนุมัติ")]
    string FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ หรืออนุมัติ")]
    string PositionName,
    [property: Description("ตำแหน่งผู้ปฏิบัติหน้าที่แทน เห็นชอบ หรืออนุมัติ")]
    string Delegate);

public class GetListMappingPurchaseRequisitionDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingPurchaseRequisitionDocumentEndpoint(ILogger<GetListMappingPurchaseRequisitionDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PurchaseRequisition"));
        this.Get("JorPor04/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(PurchaseRequisitionReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}