namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPPettyCashDetailRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public record DocumentVersionResponse(
    Guid FileId,
    string Version,
    DateTimeOffset CreatedAt,
    string CreatedByName,
    bool IsCurrent);

public record GetPPettyCashResponse(
    [property: Description("รหัสเงินสดย่อย ข้อ 2")]
    Guid Id,
    [property: Description("เลขที่เงินสดย่อย ข้อ 2")]
    string PPettyCashNumber,
    [property: Description("สถานะ")] PettyCashStatus Status,
    [property: Description("รหัสเอกสารขออนุมัติ")]
    Guid? ApprovalRequestDocumentId,
    [property: Description("เอกสารขออนุมัติ เปลี่ยนแปลง")]
    bool? IsApprovalRequestDocumentReplace,
    [property: Description("ประวัติเอกสารขออนุมัติ")]
    DocumentVersionResponse[] ApprovalRequestDocumentVersions,
    [property: Description("วันที่เงินสดย่อย ข้อ 2")]
    DateTimeOffset PPettyCashDate,
    [property: Description("รหัสหน่วยงาน")]
    string DepartmentCode,
    [property: Description("ปีงบประมาณ")] int BudgetYear,
    [property: Description("รหัสวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodCode,
    [property: Description("รหัสประเภทวิธีจัดซื้อจัดจ้าง")]
    string SupplyMethodTypeCode,
    [property: Description("รหัสประเภทพิเศษวิธีจัดซื้อจัดจ้าง")]
    string? SupplyMethodSpecialTypeCode,
    [property: Description("เรื่อง")] string Subject,
    [property: Description("ขอบเขตของงานหรือรายละเอียดคุณลักษณะเฉพาะของพัสดุที่จะซื้อหรือจ้าง")] string Source,
    [property: Description("งบประมาณ")] decimal Budget,
    [property: Description("เหตุผลความจำเป็นที่ต้องซื่อหรือจ้าง")] string? Reasons,
    [property: Description("เงินสดย่อยของฝ่าย")] string? PettyCaseDepartmentCode,
    [property: Description("วันที่กำหนดเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ")]
    DateTimeOffset? DeliveryDate,
    [property: Description("กำหนดเวลาที่ต้องการใช้พัสดุนั้น หรือ ให้งานนั้นแล้วเสร็จ")]
    int? DeliveryPeriod,
    [property: Description("วัน/เดือน/ปี")]
    string? DeliveryPeriodTypeCode,
    [property: Description("เงื่อนไขการนับเวลาที่ต้องการใช้พัสดุนั้น")]
    string? DeliveryConditionCode,
    [property: Description("วันที่เบิกจ่าย")]
    DateTimeOffset? DisbursementDate,
    [property: Description("เป็นการจ่ายล่วงหน้า")]
    bool IsAdvance,
    [property: Description("ข้อมูลการจ่ายล่วงหน้า")]
    PPettyCashAdvanceResponseDto Advance,
    [property: Description("หมวดค่าใช้จ่าย")]
    IEnumerable<CategoriesDto>? Categories,
    [property: Description("ผู้ขาย")] IEnumerable<VendorResponseDto> Vendors,
    [property: Description("บัญชี GL")] IEnumerable<GLAccountResponseDto> GLAccounts,
    [property: Description("ผู้ขอซื้อขอจ้าง")]
    CommitteeDto[]? Committees,
    [property: Description("ผู้อนุมัติ")] AcceptorResponse[] Acceptors,
    [property: Description("ผู้รับมอบหมาย")]
    AssigneeResponse[] Assignees,
    [property: Description("ไฟล์แนบ")] AttachmentsDtoWithId[] Attachments,
    [property: Description("สามารถเรียกคืนแก้ไขได้")]
    bool HasPermission,
    [property: Description("ประเภทเงินสด")]
    CashType CashType,
    [property: Description("ระดับองค์กรของฝ่าย/ภาคต้นสังกัด (RawBusinessUnit.OrganizationLevel)")]
    string? DepartmentOrganizationLevel,
    [property: Description("ออก/ไม่ออกแบบฟอร์ม จพ. 001")]
    bool? IsFromJorPor001);

public class GetPlanDetail : EndpointBase<GetPPettyCashDetailRequest, Results<Ok<GetPPettyCashResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetPlanDetail(Dp2DbContext dbContext, ILogger<GetPlanDetail> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("GetPPettyCashDetail")
             .Produces<Ok>()
             .Produces<NotFound>());
        this.Get("PPettyCash/{Id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetPPettyCashResponse>, NotFound<string>>> HandleRequestAsync(GetPPettyCashDetailRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.PPettyCashs
                             .AsNoTracking()
                             .Include(pw => pw.Vendors)
                             .ThenInclude(pPettyCashVendor => pPettyCashVendor.VendorParcels)
                             .Include(pPettyCash => pPettyCash.Categories)
                             .Include(pPettyCash => pPettyCash.Committees)
                             .Include(pPettyCash => pPettyCash.Assignees)
                             .Include(pPettyCash => pPettyCash.Attachments)
                             .Include(pw => pw.GLAccounts)
                             .Include(pw => pw.Acceptors)
                             .ThenInclude(a => a.User)
                             .ThenInclude(a => a.Employee)
                             .Include(pPettyCash => pPettyCash.Department)
                             .Include(auditableEntity => auditableEntity.AuditInfo)
                             .Include(pPettyCash => pPettyCash.DocumentHistories)
                             .AsSplitQuery()
                             .FirstOrDefaultAsync(x => x.Id == PettyCashId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"PPettyCash with ID {req.Id} not found.");
        }

        var hasPermission = data.AuditInfo.CreatedBy == req.UserId;

        var lastedApprovalRequestDocument =
            data.DocumentHistories
                .OrderVersions()
                .FirstOrDefault();

        var isReplacedApproval =
            data.DocumentHistories.Any(d => d.IsReplaced);

        var approvalRequestDocumentVersions =
            data.DocumentHistories
                .OrderVersions()
                .Select((d, index) => new DocumentVersionResponse(
                    d.FileId.Value,
                    d.Version,
                    d.CreatedAt,
                    d.CreatedByName ?? string.Empty,
                    index == 0))
                .ToArray();

        var acceptorsApprover =
            data.Acceptors
                       .Where(a => a.Type != AcceptorType.InspectionCommittee)
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .ToList();

        var committees =
            data.Acceptors
                       .Where(a => a.Type == AcceptorType.InspectionCommittee)
                       .ToList();

        var acceptors =
            acceptorsApprover
                .Union(committees)
                .ToList();

        var response = new GetPPettyCashResponse(
            data.Id.Value,
            data.PettyCashNumber.Value,
            data.Status,
            lastedApprovalRequestDocument?.FileId.Value,
            false,
            approvalRequestDocumentVersions,
            data.PettyCashDate,
            data.DepartmentId.Value,
            data.BudgetYear,
            data.SupplyMethodCode.Value,
            data.SupplyMethodTypeCode.Value,
            data.SupplyMethodSpecialTypeCode?.Value,
            data.Subject,
            data.Source,
            data.Budget,
            data.Reasons,
            data.PettyCaseDepartmentCode,
            data.DeliveryDate,
            data.DeliveryPeriod,
            data.DeliveryPeriodTypeCode?.Value,
            data.DeliveryConditionCode?.Value,
            data.DisbursementDate,
            data.IsAdvance,
            new PPettyCashAdvanceResponseDto(
                data.AdvanceName,
                (string?)data.AdvancePaymentMethodCode,
                data.AdvancePaymentDate,
                (string?)data.AdvanceBankCode,
                data.AdvanceBankAccount,
                data.AdvanceBankBranch,
                data.AdvanceBankAccountName,
                data.AdvanceDetail),
            data.Categories
                .Select(pw => new CategoriesDto(
                    pw.Id.Value,
                    pw.CategoryTypeCode.Value)),
            data.Vendors
                .OrderBy(pw => pw.Sequence)
                .Select(pw => new VendorResponseDto(
                    pw.Id.Value,
                    pw.VendorType,
                    pw.SuVendorId?.Value,
                    pw.VendorName,
                    pw.Sequence,
                    pw.TaxNumber,
                    pw.VendorBranchNumber,
                    pw.VatIncludeTypeCode?.Value,
                    pw.BillTypeCode.Value,
                    pw.BillTypeOther,
                    pw.BillBookNo ?? string.Empty,
                    pw.BillDate,
                    pw.BillDetail,
                    pw.VendorParcels
                      .OrderBy(pw => pw.Sequence)
                      .Select(vp => new VendorParcelResponseDto(
                          vp.Id.Value,
                          vp.Sequence,
                          vp.Item,
                          vp.ItemDetail,
                          vp.Quantity,
                          vp.UnitCode.Value,
                          vp.UnitPrice,
                          vp.TotalPrice,
                          vp.TotalPriceVat)))),
            data.GLAccounts
                .Select(pw => new GLAccountResponseDto(
                    pw.Id.Value,
                    pw.Sequence,
                    pw.SoId,
                    pw.BudgetTypeCode.Value,
                    pw.GLAccountCode.Value,
                    pw.ProjectNumber,
                    pw.Amount))
                .OrderBy(x => x.Sequence),
            [.. data.Committees
                .Select(c => new CommitteeDto(
                    c.Id.Value,
                    c.SuUserId.Value,
                    c.FullName,
                    c.FullPositionName,
                    c.CommitteePositionsCode.Value,
                    c.CommitteePositionsName,
                    c.GroupType,
                    c.Sequence))],
            [.. acceptors
                .Where(a => !a.IsDeleted)
                .Select(a => new AcceptorResponse(
                    a.Id.Value,
                    a.Type,
                    a.UserId.Value,
                    a.Sequence,
                    a.FullName,
                    a.PositionName,
                    a.BusinessUnitName,
                    a.Status,
                    a.Remark,
                    a.ActionAt,
                    DelegateeUserId: a.Delegatee?.SuUserId.Value))
                .OrderBy(o => o.AcceptorType)
                .ThenBy(o => o.Sequence)],
            [.. data.Assignees
                .OrderBy(a => a.Sequence)
                .Select(DelegatorExtensions.DelegatorToAssignee)
                .Select(a => new AssigneeResponse(
                    a.Id.Value,
                    a.Group,
                    a.Type,
                    a.UserId.Value,
                    a.Sequence,
                    a.FullName,
                    a.PositionName,
                    a.BusinessUnitName,
                    a.Status,
                    Remark: a.Remark,
                    ActionAt: a.ActionAt,
                    DelegateeUserId: a.Delegatee?.SuUserId.Value))],
            [.. data.Attachments
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new AttachmentsDtoWithId(
                        key.Value,
                        [.. g.Select(s => new FileAttachmentsWithId(s.Id.Value, s.Id.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))],
            hasPermission,
            data.CashType,
            data.Department?.OrganizationLevel,
            data.IsFromJorPor001);

        return TypedResults.Ok(response);
    }
}