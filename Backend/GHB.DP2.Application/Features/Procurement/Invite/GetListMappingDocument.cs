namespace GHB.DP2.Application.Features.Procurement.Invite;

using System.ComponentModel;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Plan;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record InviteReplaceDto(
    [property: Description("วันที่ดำเนินการทำหนังสือเชิญชวนผู้ประกอบการ")]
    string? AcceptorDate,
    [property: Description("ราคากลาง")] string? ReferenceMedianPrice,
    [property: Description("คุณสมบัติผู้เสนอราคา")]
    IEnumerable<TorDraftQualificationsReplaceDto>? PpTorDraftQualifications,
    [property: Description("หลักเกณฑ์พิจารณา")]
    string? PpPurchaseRequisitionEvaluationCriteria,
    [property: Description("ข้อมูลการจัดซื้อจัดจ้าง")]
    InviteProcurementReplaceDto Procurement,
    [property: Description("รหัสคำเชิญ")] Guid Id,
    [property: Description("รหัสการจัดซื้อจัดจ้าง")]
    Guid ProcurementId,
    [property: Description("เป็นการเชิญ")] bool IsInvite,
    [property: Description("วันเริ่มต้นส่งข้อเสนอ")]
    string? SubmitProposalStartDate,
    [property: Description("วันสิ้นสุดส่งข้อเสนอ")]
    string? SubmitProposalEndDate,
    [property: Description("เวลาเริ่มต้นส่งข้อเสนอ")]
    string? SubmitProposalStartTime,
    [property: Description("เวลาสิ้นสุดส่งข้อเสนอ")]
    string? SubmitProposalEndTime,
    [property: Description("วันที่ต้องแจ้งให้ทราบภายใน")]
    string? NeedToKnowWithinDate,
    [property: Description("วันขอชี้แจงรายละเอียดผ่านทาง")]
    string? ClarifyDetailViaDate,
    [property: Description("หมายเลขโทรศัพท์")]
    string? Telephone,
    [property: Description("รหัสเอกสารคำเชิญ")]
    Guid? InviteDocumentId,
    [property: Description("สถานะคำเชิญ")] PInviteStatus Status,
    [property: Description("ผู้อนุมัติ")] AcceptorInviteReplaceDto? Acceptor,
    [property: Description("ผู้ประกอบการที่ได้รับเชิญ")]
    InviteEntrepreneurReplaceDto? InvitedEntrepreneurs,
    [property: Description("ผู้จัดทำ")] PJp005CommitteeReplace? PJp005Committee,
    [property: Description("ผู้จัดซื้อจัดจ้าง/คณะกรรมการจัดซื้อจัดจ้าง")]
    CommitteeSectionReplate? ProcurementCommittee,
    string? AdditionalDetail,
    [property: Description("มีสิทธิ์แก้ไข")]
    bool HasEditPermission = false);

public record TorDraftQualificationsReplaceDto(
    int Sequence,
    string? Description);

public record InviteProcurementReplaceDto(
    [property: Description("รหัสแผนงาน")] Guid? PlanId,
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
    [property: Description("ชื่อแผนงาน")] string PlanName,
    [property: Description("งบประมาณ")] string? Budget,
    [property: Description("งบประมาณ ภาษาไทย")]
    string? BudgetText,
    [property: Description("ปีงบประมาณ")] decimal? BudgetYear,
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
    string? ExpectingProcurementAt,
    [property: Description("เป็นสต็อก")] bool IsStock,
    [property: Description("เป็นวัสดุเชิงพาณิชย์")]
    bool IsCommercialMaterial,
    [property: Description("ประเภทแผนงาน")]
    PlanType? PlanType,
    [property: Description("ขั้นตอนปัจจุบัน")]
    ProcessType CurrentStep);

public record PJp005CommitteeReplace(
    [property: Description("ชื่อผู้จัดทำ")]
    string FullName,
    [property: Description("ตำแหน่งจริงผู้จัดทำ")]
    string? FullPositionName,
    [property: Description("ตำแหน่งในคณะกรรมการของผู้จัดทำ")]
    string? CommitteePositionsName,
    string? SignName = null);

public record CommitteeSectionReplate(
    [property: Description("รหัสคณะกรรมการ")]
    string Section,
    IEnumerable<CommitteeReplate> Committees);

public record CommitteeReplate(
    [property: Description("ลำดับคณะกรรมการ")]
    int? Sequence,
    [property: Description("ชื่อ-นามสกุล")]
    string? FullName,
    [property: Description("ตำแหน่ง")] string? Position,
    [property: Description("ตำแหน่งคณะกรรมการ")]
    string? CommitteePosition);

public record InviteEntrepreneurReplaceDto(
    [property: Description("รหัสผู้ประกอบการที่ได้รับเชิญ")]
    Guid Id,
    [property: Description("รหัสผู้ขาย")] Guid VendorId,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เลขที่ผู้เสียภาษีผู้ประกอบการ")]
    string EntrepreneurTaxId,
    [property: Description("ประเภทผู้ประกอบการ")]
    string EntrepreneurType,
    [property: Description("ชื่อผู้ประกอบการ")]
    string EntrepreneurName,
    [property: Description("อีเมลผู้ประกอบการ")]
    string EntrepreneurEmail,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    bool WatchlistResult,
    [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
    string? WatchlistResultRemark,
    [property: Description("วันที่ตรวจสอบ Watchlist")]
    string? WatchlistResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    bool CoiResult,
    [property: Description("หมายเหตุผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultRemark,
    [property: Description("วันที่ตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultAt,
    [property: Description("ผลการตรวจสอบ eGP")]
    bool EgpResult,
    [property: Description("หมายเหตุผลการตรวจสอบ eGP")]
    string? EgpResultRemark,
    [property: Description("วันที่ตรวจสอบ eGP")]
    string? EgpResultAt,
    [property: Description("ส่งอีเมลแล้ว")]
    bool EmailSend,
    [property: Description("สัญชาติ")] SuVendorNationality? Nationality,
    [property: Description("ประเภทผู้ขาย")]
    SuVendorType? Type,
    [property: Description("เบอร์โทรศัพท์")]
    string? Tel,
    [property: Description("ผู้ถือหุ้น")] InviteEntrepreneurShareholderReplaceDto[]? Shareholders);

public record InviteEntrepreneurShareholderReplaceDto(
    [property: Description("รหัสผู้ถือหุ้น")]
    Guid Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string? TaxId,
    [property: Description("ชื่อจริง")] string? FirstName,
    [property: Description("นามสกุล")] string? LastName,
    [property: Description("เป็นกรรมการหรือถือหุ้น 20%")]
    bool? IsDirector,
    [property: Description("เป็นผู้ถือหุ้น")]
    bool? IsShareholder,
    [property: Description("ผลการตรวจสอบ Watchlist")]
    bool WatchlistResult,
    [property: Description("หมายเหตุผลการตรวจสอบ Watchlist")]
    string? WatchlistResultRemark,
    [property: Description("วันที่ตรวจสอบ Watchlist")]
    string? WatchlistResultAt,
    [property: Description("ผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    bool CoiResult,
    [property: Description("หมายเหตุผลการตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultRemark,
    [property: Description("วันที่ตรวจสอบความขัดแย้งทางผลประโยชน์")]
    string? CoiResultAt,
    [property: Description("ผลการตรวจสอบ eGP")]
    bool EgpResult,
    [property: Description("หมายเหตุ eGP")]
    string? EgpRemark,
    [property: Description("วันที่ตรวจสอบ eGP")]
    string? EgpResultAt
);

public record AcceptorInviteReplaceDto(
    [property: Description("รหัสผู้อนุมัติ")]
    Guid Id,
    [property: Description("ประเภทผู้อนุมัติ")]
    AcceptorType AcceptorType,
    [property: Description("รหัสผู้ใช้งาน")]
    Guid UserId,
    [property: Description("รหัสพนักงาน")] string EmployeeCode,
    [property: Description("ชื่อ-สกุล")] string FullName,
    [property: Description("ตำแหน่ง")] string PositionName,
    [property: Description("ชื่อหน่วยงาน")]
    string DepartmentName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัสผู้รับมอบอำนาจ")]
    Guid? DelegateeId,
    [property: Description("สถานะการอนุมัติ")]
    AcceptorStatus Status,
    [property: Description("วันที่ดำเนินการ")]
    string? ActionAt,
    [property: Description("หมายเหตุ")] string? Remark,
    [property: Description("เป็นผู้อนุมัติปัจจุบัน")]
    bool IsCurrent,
    bool IsUnableToPerformDuties,
    string? CommitteePositionsCode,
    string? CommitteePositionName,
    string? LasModifiedAt
);

public class GetListMappingInviteDocumentEndpoint : EndpointBase<Ok<ResponseDtoDescriptionExtension>>
{
    public GetListMappingInviteDocumentEndpoint(ILogger<GetListMappingInviteDocumentEndpoint> logger)
        : base(logger)
    {
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Invite"));
        this.Get("procurement/invite/mapping-document");
    }

    protected override async ValueTask<Ok<ResponseDtoDescriptionExtension>> HandleRequestAsync(CancellationToken ct)
    {
        await Task.CompletedTask;

        var dtoType = typeof(InviteReplaceDto);

        var result = DtoDescriptionExtensions.ToDictionary(dtoType);

        return TypedResults.Ok(result);
    }
}