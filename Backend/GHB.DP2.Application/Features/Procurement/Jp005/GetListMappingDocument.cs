namespace GHB.DP2.Application.Features.Procurement.Jp005;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetJp005ByIdReplaceDto(
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("วันที่ทำการประกาศแต่งตั้ง")]
    string? ApproverDate,
    [property: Description("ประกาศแต่งตั้ง")]
    Jp005PublisherReplace? Publisher,
    [property: Description("วันที่ดำเนินการทำ จพ.005")]
    string? DocumentDate,
    [property: Description("ตำแหน่งผู้มีอำนาจอนุมัติ")]
    IEnumerable<SectionApprove>? SectionApproveName,
    [property: Description("เหตุผลและความจำเป็น")]
    string? Reason,
    [property: Description("ขอบเขตของงาน")]
    IEnumerable<ParcelDescriptionDto>? ParcelDescriptions,
    [property: Description("จัดทำรายงานขอซื้อขอจ้าง (จพ.005) (วงเงินเกิน 100,000 หรือ ไม่เกิน 100,000)")]
    string? Consideration,
    [property: Description("จัดทำรายงานขอซื้อขอจ้าง (จพ.005) (วงเงินเกิน 100,000 หรือ ไม่เกิน 100,000)")]
    string? ConsiderationDescription,
    [property: Description("คำสั่งอนุมัติ")]
    string? CommandText,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    Jp005CreatorDto? Jp005CreatorDto,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    Jp005ProcurementReplaceDto Procurement,
    [property: Description("รหัส จพ.005")] PJp005Id? Id,
    [property: Description("ข้อมูลใบขอซื้อ จพ.004")]
    Jp004ReplaceDto PurchaseRequisition,
    [property: Description("ข้อมูล จพ.005")]
    Jp005ReplaceDto? Jp005,
    [property: Description("สถานะ จพ.005")]
    PJp005Status Status,
    [property: Description("เลขที่คำสั่ง จพ.")]
    string? JorPorNumber,
    [property: Description("มีสิทธิ์แก้ไข")]
    bool HasEditPermission = false);

public record Jp005ProcurementReplaceDto(
    [property: Description("รหัสแผนงาน")]
    string? PlanId,
    [property: Description("เลขที่การจัดซื้อจัดจ้าง")]
    string? ProcurementNumber,
    [property: Description("ประเภทการจัดซื้อจัดจ้าง")]
    ProcurementType ProcurementType,
    [property: Description("ขั้นตอนการจัดซื้อจัดจ้าง")]
    ProcurementStep ProcurementStep,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    [property: Description("เลขที่แผนงาน")]
    string? PlanNumber,
    [property: Description("ชื่อแผนงาน")]
    string PlanName,
    [property: Description("งบประมาณ")]
    string? Budget,
    [property: Description("งบประมาณ ภาษาไทย")]
    string? BudgetText,
    [property: Description("ปีงบประมาณ")]
    decimal? BudgetYear,
    [property: Description("วิธีการจัดหา")]
    string? SupplyMethod,
    [property: Description("รหัสวิธีการจัดหา")]
    string? SupplyMethodCode,
    [property: Description("ประเภทวิธีการจัดหา")]
    string? SupplyMethodType,
    [property: Description("รหัสประเภทวิธีการจัดหา")]
    string? SupplyMethodTypeCode,
    [property: Description("ประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialType,
    [property: Description("รหัสประเภทวิธีการจัดหาพิเศษ")]
    string? SupplyMethodSpecialTypeCode,
    [property: Description("สถานะการจัดซื้อจัดจ้าง")]
    ProcurementStatus Status,
    [property: Description("วันที่คาดว่าจะดำเนินการจัดซื้อจัดจ้าง")]
    DateTimeOffset? ExpectingProcurementAt,
    [property: Description("เป็นสต็อก")]
    bool IsStock,
    [property: Description("เป็นวัสดุเชิงพาณิชย์")]
    bool IsCommercialMaterial,
    [property: Description("ประเภทแผนงาน")]
    PlanType? PlanType,
    [property: Description("ขั้นตอนปัจจุบัน")]
    ProcessType CurrentStep);

public record Jp005PublisherReplace(
    [property: Description("ลายเซ็นผู้ประกาศแต่งตั้งิ")]
    string? Signature,
    [property: Description("ชื่อผู้ประกาศแต่งตั้ง")]
    string? FullName,
    [property: Description("ตำแหน่งผู้ประกาศแต่งตั้ง")]
    string? PositionName,
    [property: Description("ตำแหน่งผู้ปฎิบัติหน้าที่แทนผู้ประกาศแต่งตั้ง")]
    string? Delegate,
    [property: Description("กรรมการผู้จัดการ")]
    string? ManagingDirector);

public record ParcelDescriptionDto(
    string? Description,
    string? Name,
    int? Quantity,
    string? UnitName);

public record Jp005CreatorDto(
    [property: Description("ลายเซ็นต์")] string? Action,
    [property: Description("ลายเซ็นต์")] string? Signature,
    [property: Description("ชื่อ-สกุล")] string? FullName,
    [property: Description("ตำแหน่ง")] string? PositionName,
    [property: Description("ตำแหน่งในคณะกรรมการ หรือผู้จัดทำ")]
    string? PositionOnBoard);

public record Jp005ReplaceDto(
    [property: Description("ระยะเวลาในการพิจารณาผลการเสนอราคา")]
    int EvaluationDueDate,
    [property: Description("รหัสประเภทระยะเวลา")]
    string EvaluationPeriodTypeName,
    [property: Description("รหัสเงื่อนไขระยะเวลา")]
    string EvaluationPeriodConditionName,
    [property: Description("เลขโครงการ eGP")]
    string? EgpProjectNumber,
    [property: Description("รหัสเอกสารอนุมัติ จพ.005")]
    Guid? Jp005ApprovalDocumentId,
    [property: Description("รหัสเอกสารคำสั่ง จพ.005")]
    Guid? Jp005CommandDocumentId,
    [property: Description("ประเภทคณะกรรมการ หรือผู้จัดทำ")]
    string? ProcurementCommitteeSection,
    [property: Description("คณะกรรมการ")] IEnumerable<CommitteeReplaceDto> ProcurementCommittees,
    [property: Description("อำนาจหน้าที่")] IEnumerable<DutyReplaceDto> ProcurementDuties,
    [property: Description("เป็นคณะกรรมการ")] bool IsProcurementCommittee,
    [property: Description("ประเภทคณะกรรมการ หรือผู้จัดทำ")]
    string? InspectionCommitteeSection,
    [property: Description("คณะกรรมการ")] IEnumerable<CommitteeReplaceDto> InspectionCommittees,
    [property: Description("อำนาจหน้าที่")] IEnumerable<DutyReplaceDto> InspectionDuties,
    [property: Description("เป็นคณะกรรมการ")] bool IsInspectionCommittee,
    [property: Description("ประเภทคณะกรรมการ หรือผู้จัดทำ")]
    string? MaintenanceInspectionCommitteeSection,
    [property: Description("คณะกรรมการ")] IEnumerable<CommitteeReplaceDto> MaintenanceInspectionCommittees,
    [property: Description("อำนาจหน้าที่")] IEnumerable<DutyReplaceDto> MaintenanceInspectionDuties,
    [property: Description("เป็นคณะกรรมการ")] bool IsMaintenanceInspectionCommittee,
    [property: Description("ประเภทคณะกรรมการ หรือผู้จัดทำ")]
    string? ConstructionSupervisorSection,
    [property: Description("คณะกรรมการ")] IEnumerable<CommitteeReplaceDto> ConstructionSupervisorCommittees,
    [property: Description("อำนาจหน้าที่")] IEnumerable<DutyReplaceDto> ConstructionSupervisorDuties,
    [property: Description("เป็นคณะกรรมการ")] bool IsConstructionSupervisorCommittee,
    [property: Description("เลขที่ จพ.005")]
    string? Jp005Number,
    [property: Description("ผู้อนุมัติ")]
    IEnumerable<Jp005AcceptorReplace>? Acceptors,
    [property: Description("สำนักของ Assignee คนสุดท้าย จพ.004")]
    string? LastedAssigneeJp04Department);

public record CommitteeReplaceDto(
    [property: Description("รหัสคณะกรรมการ")]
    string? Id,
    [property: Description("รหัสผู้ใช้งาน")]
    string? UserId,
    [property: Description("ชื่อ-สกุล")]
    string? FullName,
    [property: Description("ชื่อตำแหน่งเต็ม")]
    string? FullPositionName,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    string? CommitteePositionsName,
    [property: Description("ลำดับ")]
    string? Sequence);

public record DutyReplaceDto(
    [property: Description("รหัสอำนาจหน้าที่")]
    string? Id,
    [property: Description("รายละเอียดอำนาจหน้าที่")]
    string? Description,
    [property: Description("ลำดับ")] string? Sequence);

public record Jp005AcceptorReplace(
    [property: Description("เห็นชอบ/อนุมัติ")]
    string? Action,
    [property: Description("ชื่อผู้เห็นชอบ/อนุมัติ")]
    string? FullName,
    [property: Description("ตำแหน่งผู้เห็นชอบ/อนุมัติ")]
    string? PositionName,
    [property: Description("ปฏิบัติหน้าที่แทน")]
    string? Delegate);

public record Jp004ReplaceDto(
    [property: Description("ผู้จัดซื้อจัดจ้าง หรือ คณะกรรมการจัดซื้อจัดจ้างุ")]
    string? ProcurementType,
    [property: Description("ผู้ตรวจรับพัสดุ หรือ คณะกรรมการตรวจรับพัสดุ")]
    string? InspectionType,
    [property: Description("ผู้ตรวจรับพัสดุงานจ้างบำรุงรักษา หรือ คณะกรรมการตรวจรับพัสดุงานจ้างบริการบำรุงรักษา")]
    string? MaintenanceInspectionType,
    [property: Description("ผู้ควบคุมงานก่อสร้าง หรือ คณะควบคุมงานก่อสร้าง")]
    string? ConstructionSupervisorType,
    [property: Description("ผู้ตรวจรับพัสดุงานจ้างบำรุงรักษา หรือ คณะกรรมการตรวจรับพัสดุงานจ้างบริการบำรุงรักษา")]
    string? MaintenanceInspectionName,
    [property: Description("ผู้ควบคุมงานก่อสร้าง หรือ คณะควบคุมงานก่อสร้าง")]
    string? ConstructionSupervisorName,
    [property: Description("รหัสใบขอซื้อ")]
    Guid PurchaseRequisitionId,
    [property: Description("ข้อมูลใบขอซื้อ")]
    GetPurchaseRequisitionReplaceDto Requisition,
    [property: Description("ข้อมูลงบประมาณ")]
    IEnumerable<GetPurchaseRequisitionBudgetReplaceDto> Budgets,
    [property: Description("ข้อมูลรายละเอียดงบประมาณ")] IEnumerable<GetPurchaseRequisitionBudgetDetailReplaceDto> BudgetDetails,
    [property: Description("ข้อมูลการรับประกัน")]
    IEnumerable<GetPurchaseRequisitionWarrantyReplaceDto> Warranties,
    [property: Description("คณะกรรมการ")] IEnumerable<GetPurchaseRequisitionCommitteeReplaceDto> Committees,
    [property: Description("ข่ายงาน")] IEnumerable<GetScopeOfWorkReplaceDto> ScopeOfWorks,
    [property: Description("ผู้ดำเนินการ")]
    IEnumerable<Jp004OperatorReplace> Operators,
    [property: Description("เป็นคณะกรรมการจัดซื้อจัดจ้าง")]
    bool IsProcurementCommittee,
    [property: Description("เป็นคณะกรรมการจัดซื้อจัดจ้าง")]
    bool IsInspectCommittee);

public record Jp004OperatorReplace(
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("ประเภทผู้ดำเนินการ")]
    GetById.Jp004OperatorType OperatorType);

public record GetPurchaseRequisitionReplaceDto(
    [property: Description("เลขที่ใบขอซื้อ")]
    string? PurchaseRequisitionNumber,
    [property: Description("เลข eGP")] string? EgpNumber,
    [property: Description("เลขที่ PR")] string? PrNumber,
    [property: Description("รายละเอียด")] string? Description,
    [property: Description("ข้อมูลความสมเหตุสมผลของราคา")]
    string? PriceReasonablenessInfo,
    [property: Description("ราคากลาง")] string? MedianPriceAmount,
    [property: Description("รหัสเกณฑ์การประเมิน")]
    string? EvaluationCriteriaName,
    [property: Description("ระยะเวลาการส่งมอบ")]
    int DeliveryPeriod,
    [property: Description("รหัสประเภทระยะเวลาส่งมอบ")]
    string? DeliveryPeriodTypeName,
    [property: Description("รหัสเงื่อนไขการส่งมอบ")]
    string? DeliveryConditionName,
    [property: Description("มีอัตราค่าปรับ")]
    bool HasFineRate,
    [property: Description("มีการรับประกัน")]
    bool HasWarranty,
    [property: Description("ระยะเวลารับประกัน")]
    int? WarrantyPeriod,
    [property: Description("รหัสประเภทระยะเวลารับประกัน")]
    string? WarrantyPeriodCode,
    [property: Description("รหัสเงื่อนไขการรับประกัน")]
    string? WarrantyConditionCode,
    [property: Description("มีหลักประกันสัญญา")]
    bool HasContractGuarantee,
    [property: Description("มีคณะกรรมการตรวจรับ")]
    bool HasInspectionCommittee,
    [property: Description("มีผู้ควบคุมงานก่อสร้าง")]
    bool HasConstructionSupervisor);

public record GetPurchaseRequisitionBudgetReplaceDto(
    [property: Description("รหัสงบประมาณ")]
    Guid? Id,
    [property: Description("รายละเอียด")] string Description,
    [property: Description("จำนวนเงินงบประมาณ")]
    string BudgetAmount,
    [property: Description("รายละเอียดงบประมาณ")]
    IEnumerable<GetPurchaseRequisitionBudgetDetailReplaceDto> Details,
    [property: Description("ลำดับ")] int Sequence);

public record GetPurchaseRequisitionBudgetDetailReplaceDto(
    [property: Description("รหัสรายละเอียดงบประมาณ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("หน่วยงาน")] string DepartmentName,
    [property: Description("ประเภทงบประมาณ")]
    string BudgetType,
    [property: Description("รหัสโครงการ")] string? ProjectCode,
    [property: Description("เลขที่บัญชี")] string AccountName,
    [property: Description("จำนวนเงินงบประมาณ")]
    string Budget);

public record GetPurchaseRequisitionWarrantyReplaceDto(
    [property: Description("รหัสการรับประกัน")]
    Guid? Id,
    [property: Description("มีการรับประกัน")]
    bool HasWarranty,
    [property: Description("ระยะเวลา")] int Period,
    [property: Description("รหัสประเภทระยะเวลา")]
    string? PeriodTypeCode,
    [property: Description("เงื่อนไขอื่น")]
    string? ConditionOther);

public record GetPurchaseRequisitionCommitteeReplaceDto(
    [property: Description("รหัสคณะกรรมการ")]
    Guid? Id,
    [property: Description("ประเภทกลุ่ม")] GroupType GroupType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid SuUserId,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ชื่อตำแหน่ง")] string PositionName,
    [property: Description("รหัสตำแหน่งคณะกรรมการ")]
    string CommitteePositionsCode,
    [property: Description("ชื่อตำแหน่งคณะกรรมการ")]
    string CommitteePositionsName,
    [property: Description("ลำดับ")] int Sequence);

public record GetScopeOfWorkReplaceDto(
    [property: Description("รหัสข่ายงาน")] Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("ชื่อข่ายงาน")] string Name,
    [property: Description("รายละเอียด")] string Description,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("รหัสหน่วย")] string UnitCode);

public class GetListMappingJp005DocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingJp005DocumentEndpoint(ILogger<GetListMappingJp005DocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/JorPor005"));
        this.Get("procurement/jp005/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(GetJp005ByIdReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}