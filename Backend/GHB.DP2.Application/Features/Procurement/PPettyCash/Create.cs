namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Procurement.PPettyCash.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;

public record CreatePPettyCashRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    PettyCashStatus Status,
    DateTimeOffset PPettyCashDate,
    string DepartmentCode,
    int BudgetYear,
    string SupplyMethodCode,
    string SupplyMethodTypeCode,
    string SupplyMethodSpecialTypeCode,
    string Subject,
    string Source,
    decimal Budget,
    string? Reasons,
    string? PettyCaseDepartmentCode,
    DateTimeOffset? DeliveryDate,
    int? DeliveryPeriod,
    string? DeliveryPeriodTypeCode,
    string? DeliveryConditionCode,
    bool IsAdvance,
    PPettyCashAdvanceResponseDto Advance,
    CategoriesDto[]? Categories,
    VendorResponseDto[]? Vendors,
    GLAccountResponseDto[]? GLAccounts,
    AcceptorRequest[]? Acceptors,
    AssigneeRequest[]? Assignees,
    CommitteeDto[]? Committees,
    AttachmentsDto[] Attachments,
    CashType CashType,
    bool? IsFromJorPor001);

public record CategoriesDto(
    [property: Description("รหัสค่าใช้จ่ายเงินสดย่อย")]
    Guid? Id,
    [property: Description("หมวดค่าใช้จ่าย")]
    string CategoryTypeCode);

public record PPettyCashAdvanceResponseDto(
    [property: Description("ชื่อผู้รับเงินล่วงหน้า")]
    string? AdvanceName,
    [property: Description("รหัสวิธีการจ่ายล่วงหน้า")]
    string? AdvancePaymentMethodCode,
    [property: Description("วันที่จ่ายล่วงหน้า")]
    DateTimeOffset? AdvancePaymentDate,
    [property: Description("รหัสธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankCode,
    [property: Description("เลขที่บัญชีจ่ายล่วงหน้า")]
    string? AdvanceBankAccount,
    [property: Description("สาขาธนาคารจ่ายล่วงหน้า")]
    string? AdvanceBankBranch,
    [property: Description("ชื่อบัญชีจ่ายล่วงหน้า")]
    string? AdvanceBankAccountName,
    [property: Description("รายละเอียดการจ่ายล่วงหน้า")]
    string? AdvanceDetail);

public record VendorResponseDto(
    [property: Description("รหัสผู้ขาย")] Guid? Id,
    [property: Description("ประเภทผู้ขาย")]
    string VendorType,
    [property: Description("รหัสผู้ขายในระบบ")]
    Guid? SuVendorId,
    [property: Description("ชื่อผู้ขาย")] string VendorName,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("เลขที่ผู้เสียภาษี")]
    string? TaxNumber,
    [property: Description("เลขที่สาขาผู้ขาย")]
    string? VendorBranchNumber,
    [property: Description("รหัสประเภทภาษีมูลค่าเพิ่ม")]
    string? VatIncludeTypeCode,
    [property: Description("รหัสประเภทใบเสร็จ")]
    string BillTypeCode,
    [property: Description("ประเภทเอกสาร อื่นๆ")]
    string? BillTypeOther,
    [property: Description("เลขที่เล่มใบเสร็จ")]
    string BillBookNo,
    [property: Description("วันที่ใบเสร็จ")]
    DateTimeOffset? BillDate,
    [property: Description("รายละเอียดใบเสร็จ")]
    string? BillDetail,
    [property: Description("รายการพัสดุ")] IEnumerable<VendorParcelResponseDto> VendorParcels);

public record VendorParcelResponseDto(
    [property: Description("รหัสรายการพัสดุ")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รายการ")] string Item,
    [property: Description("รายละเอียดรายการ")]
    string? ItemDetail,
    [property: Description("จำนวน")] int Quantity,
    [property: Description("รหัสหน่วยนับ")]
    string UnitCode,
    [property: Description("ราคาต่อหน่วย")]
    decimal UnitPrice,
    [property: Description("ราคารวม")] decimal TotalPrice,
    [property: Description("ราคารวมรวมภาษี")]
    decimal TotalPriceVat);

public record GLAccountResponseDto(
    [property: Description("รหัสบัญชี GL")]
    Guid? Id,
    [property: Description("ลำดับ")] int Sequence,
    [property: Description("รหัส SOL")] string SolId,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeCode,
    [property: Description("รหัสบัญชี GL")]
    string GLAccountCode,
    [property: Description("เลขที่โครงการ")]
    string? ProjectNumber,
    [property: Description("จำนวนเงิน")] decimal Amount);

public record CommitteeDto(
    Guid? Id,
    Guid UserId,
    string FullName,
    string? PositionName,
    string CommitteePositionCode,
    string CommitteePositionName,
    GroupType GroupType,
    int Sequence);

public class CreatePPettyCashRequestValidator : Validator<CreatePPettyCashRequest>
{
    public CreatePPettyCashRequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class CreatePPettyCashEndpoint : PPettyCashEndpointBase<CreatePPettyCashRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreatePPettyCashEndpoint(
        Dp2DbContext dbContext,
        ILogger<CreatePPettyCashEndpoint> logger,
        IFileServiceClient fileServiceClient)
        : base(dbContext, logger, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("CreatePPettyCash")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreatePPettyCashRequest>("application/json"));
        this.Post("PPettyCash");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreatePPettyCashRequest req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var pPettyCashNumber = await this.GeneratePPettyCashNumberAsync(req.BudgetYear, ct);
        var pPettyCash = await CreatePPettyCashFromRequestAsync(req, pPettyCashNumber);

        if (req.PettyCaseDepartmentCode == JorPor.DefaultSectionHead.JorPortDepartmentCode)
        {
            await this.CreateDirectorAssigneeAsync(pPettyCash, ct, UserId.From(req.UserId));
        }
        else if (req.Assignees is not null && req.Assignees.Length > 0)
        {
            await this.ManageAssigneeAsync(pPettyCash, req.Assignees, ct, UserId.From(req.UserId));
        }

        if (req.Committees is not null && req.Committees.Length > 0)
        {
            await this.AddCommitteePersonsToPPettyCashAsync(pPettyCash, req.Committees, CancellationToken.None);
        }

        if (req.Acceptors is not null)
        {
            await this.AddAcceptorPersonsToPPettyCashAsync(pPettyCash, req.Acceptors, ct, UserId.From(req.UserId));
        }

        await this.AddAttachmentsToPPettyCash(pPettyCash, req.Attachments, ct);

        await this.SetDefaultDocumentTemplate(pPettyCash, ct);

        pPettyCash.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            ActivityLogActionTypeConstant.Create,
            nameof(PettyCashStatus.Draft)));

        this.dbContext.PPettyCashs.Add(pPettyCash);
        await this.dbContext.SaveChangesAsync(ct);

        // Reload with AsNoTracking (GetPettyCashById) for navigation data needed by MapToReplaceDto.
        // AddDocumentHistory uses the original tracked pPettyCash since GetPettyCashById is AsNoTracking.
        var pettyCashReloaded = await this.GetPettyCashById(pPettyCash.Id, ct);
        var lastedDraft = pettyCashReloaded.LastedDraftDocument();

        if (lastedDraft is not null)
        {
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDto(
                pettyCashReloaded,
                hasAcceptor: false,
                ct,
                userId: null);

            var copiedFileId = await documentService.CopyDocumentTemplateAsync(
                lastedDraft.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{pettyCashReloaded.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (copiedFileId.HasValue)
            {
                pPettyCash.AddDocumentHistory(copiedFileId.Value);
                await this.dbContext.SaveChangesAsync(ct);
            }
        }

        return TypedResults.Created(string.Empty, pPettyCash.Id.Value);
    }

    private async Task ValidateRequestAsync(CreatePPettyCashRequest req, CancellationToken ct)
    {
        // Check if department exists
        var department = await this.dbContext.RawBusinessUnits
                                   .FirstOrDefaultAsync(d => d.Id == BusinessUnitId.From(req.DepartmentCode), ct);

        if (department is null)
        {
            this.ThrowError(
                r => r.DepartmentCode,
                $"Department with code {req.DepartmentCode} not found.",
                StatusCodes.Status404NotFound);
        }

        // Validate supply method parameter
        var supplyMethod = await this.dbContext.SuParameters
                                     .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodCode), ct);

        if (supplyMethod is null)
        {
            this.ThrowError(
                r => r.SupplyMethodCode,
                $"Supply method with code {req.SupplyMethodCode} not found.",
                StatusCodes.Status404NotFound);
        }

        if (req.SupplyMethodSpecialTypeCode is not null)
        {
            var supplyMethodSpecialType = await this.dbContext.SuParameters
                                                    .FirstOrDefaultAsync(p => p.Code == ParameterCode.From(req.SupplyMethodSpecialTypeCode), ct);

            if (supplyMethodSpecialType is null)
            {
                this.ThrowError(
                    r => r.SupplyMethodSpecialTypeCode,
                    $"Supply method special type with code {req.SupplyMethodSpecialTypeCode} not found.",
                    StatusCodes.Status404NotFound);
            }
        }

        if (req.IsFromJorPor001 == false)
        {
            return;
        }

        if (req.Committees is null)
        {
            this.ThrowError(
                "กรุณาระบุผู้ขอซื้อขอจ้าง และผู้ตรวจรับ",
                StatusCodes.Status400BadRequest);
        }

        if (req.Committees?.Any(c => c.GroupType == GroupType.ProcurementCommittee) != true)
        {
            this.ThrowError(
                "กรุณาระบุผู้ขอซื้อขอจ้าง",
                StatusCodes.Status400BadRequest);
        }

        if (req.Committees?.Any(c => c.GroupType == GroupType.InspectionCommittee) != true)
        {
            this.ThrowError(
                "กรุณาระบุผู้ตรวจรับ",
                StatusCodes.Status400BadRequest);
        }
    }

    private async Task<PettyCashNumber> GeneratePPettyCashNumberAsync(int budgetYear, CancellationToken ct)
    {
        var lastPPettyCash = await this.dbContext.PPettyCashs
                                       .Where(p => p.BudgetYear == budgetYear && !p.IsDeleted)
                                       .OrderByDescending(p => p.PettyCashNumber)
                                       .FirstOrDefaultAsync(ct);

        return lastPPettyCash is null
            ? PettyCashNumber.New(budgetYear)
            : lastPPettyCash.PettyCashNumber.Next();
    }

    private static Task<PPettyCash> CreatePPettyCashFromRequestAsync(CreatePPettyCashRequest req, PettyCashNumber pettyCashNumber)
    {
        var pPettyCash = PPettyCash.Create(pettyCashNumber)
                                   .SetPettyCashDate(req.PPettyCashDate)
                                   .SetDocumentDate(req.PPettyCashDate)
                                   .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
                                   .SetBudgetYear(req.BudgetYear)
                                   .SetSupplyMethod(ParameterCode.From(req.SupplyMethodCode), ParameterCode.From(req.SupplyMethodTypeCode), ParameterCode.From(req.SupplyMethodSpecialTypeCode))
                                   .SetSubject(req.Subject)
                                   .SetSource(req.Source)
                                   .SetBudget(req.Budget)
                                   .SetReasons(req.Reasons)
                                   .SetDeliveryDate(req.DeliveryDate)
                                   .SetPettyCaseDepartmentCode(req.PettyCaseDepartmentCode)
                                   .SetCashType(req.CashType)
                                   .SetIsAdvance(req.IsAdvance)
                                   .SetIsFromJorPor001(req.IsFromJorPor001);

        if (req.Advance is not null)
        {
            _ = pPettyCash.SetAdvanceName(req.Advance.AdvanceName)
                          .SetAdvancePayment(
                              string.IsNullOrEmpty(req.Advance.AdvancePaymentMethodCode)
                                  ? null
                                  : ParameterCode.From(req.Advance.AdvancePaymentMethodCode!),
                              req.Advance.AdvancePaymentDate)
                          .SetAdvanceBank(
                              req.Advance.AdvanceBankCode != null
                                  ? ParameterCode.From(req.Advance.AdvanceBankCode)
                                  : null,
                              req.Advance.AdvanceBankAccount,
                              req.Advance.AdvanceBankBranch,
                              req.Advance.AdvanceBankAccountName)
                          .SetAdvanceDetail(req.Advance.AdvanceDetail);
        }

        if (req.GLAccounts is not null && req.GLAccounts.Length > 0)
        {
            req.GLAccounts
               .Iter(g => CreateGLAccountFromRequest(pPettyCash, g));
        }

        if (req.Categories is not null && req.Categories.Length > 0)
        {
            req.Categories
               .Iter(c => CreatePPettyCashCategoryFromRequest(pPettyCash, c));
        }

        if (req.Vendors is not null && req.Vendors.Length > 0)
        {
            req.Vendors
               .Where(v => !string.IsNullOrWhiteSpace(v.BillTypeCode))
               .Iter(v => CreatePPettyCashVendorFromRequest(pPettyCash, v));
        }

        return Task.FromResult(pPettyCash);
    }

    private async Task CreateDirectorAssigneeAsync(PPettyCash data, CancellationToken ct, UserId? sendToAcceptorId = null)
    {
        var generalSectionHead =
            await this.dbContext.RawEmployeePositions
                      .Where(p =>
                          p.BusinessUnitId == BusinessUnitId.From(JorPor.DefaultSectionHead.GeneralBusinessUnitId) &&
                          p.Position.Name == JorPor.DefaultSectionHead.PositionName)
                      .Select(p => p.Employee)
                      .SelectMany(e => e.Users)
                      .FirstOrDefaultAsync(ct);

        var director = new AssigneeRequest(
            default,
            AssigneeGroup.GeneralHead,
            AssigneeType.Director,
            generalSectionHead!.Id.Value,
            1);

        await this.ManageAssigneeAsync(data, new[] { director! }, ct, sendToAcceptorId);
    }

    private static void CreatePPettyCashCategoryFromRequest(PPettyCash data, CategoriesDto req)
    {
        var category = PPettyCashCategories.Create(data.Id)
                                           .SetCategoryType(ParameterCode.From(req.CategoryTypeCode));

        data.AddCategory(category);
    }

    private static void CreatePPettyCashVendorFromRequest(PPettyCash data, VendorResponseDto req)
    {
        var vendor = PPettyCashVendor.Create(data.Id)
                                     .SetSequence(req.Sequence)
                                     .SetVendorType(req.VendorType)
                                     .SetVendor(req.SuVendorId != null ? SuVendorId.From(req.SuVendorId.Value) : null, req.TaxNumber, req.VendorName, req.VendorBranchNumber)
                                     .SetBill(
                                         req.VatIncludeTypeCode != null ? ParameterCode.From(req.VatIncludeTypeCode) : null,
                                         ParameterCode.From(req.BillTypeCode),
                                         req.BillTypeOther,
                                         req.BillBookNo,
                                         req.BillDate,
                                         req.BillDetail);

        data.AddVendor(vendor);

        if (req.VendorParcels is not null)
        {
            req.VendorParcels
               .Where(vp => !string.IsNullOrWhiteSpace(vp.UnitCode))
               .Iter(vp => CreatePPettyCashVendorParcelFromRequest(vp, vendor));
        }
    }

    private static void CreatePPettyCashVendorParcelFromRequest(VendorParcelResponseDto req, PPettyCashVendor vendor)
    {
        var parcel = PPettyCashVendorParcel.Create(vendor.Id)
                                           .SetSequence(req.Sequence)
                                           .SetItem(req.Item, req.ItemDetail)
                                           .SetPrice(req.Quantity, ParameterCode.From(req.UnitCode), req.UnitPrice, req.TotalPrice, req.TotalPriceVat);

        vendor.AddVendorParcels(parcel);
    }

    private static void CreateGLAccountFromRequest(PPettyCash data, GLAccountResponseDto req)
    {
        var glAccount = PPettyCashGLAccount.Create(data.Id)
                                           .SetGLAccount(req.Sequence, req.SolId, ParameterCode.From(req.BudgetTypeCode), ParameterCode.From(req.GLAccountCode), req.ProjectNumber, req.Amount);

        data.AddGLAccount(glAccount);
    }

    private async Task AddCommitteePersonsToPPettyCashAsync(
        PPettyCash pPettyCash,
        CommitteeDto[] committees,
        CancellationToken ct)
    {
        var userIds = committees.Select(a => UserId.From(a.UserId)).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(s => s.View)
                              .Where(u => userIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        var committeePositionsCodes =
            committees
                .Where(a => !string.IsNullOrWhiteSpace(a.CommitteePositionCode))
                .Select(a => a.CommitteePositionCode)
                .Map(ParameterCode.From!);

        var committeePositions =
            await this.dbContext.SuParameters
                      .Where(p => committeePositionsCodes.Contains(p.Code))
                      .ToArrayAsync(CancellationToken.None);

        AddCommitteeToPPettyCash(pPettyCash, committees, users, committeePositions);
    }

    private static void AddCommitteeToPPettyCash(PPettyCash pPettyCash, CommitteeDto[] committees, SuUser[] users, SuParameter[] paramPositions)
    {
        _ = committees
            .Join(
                users,
                c => UserId.From(c.UserId),
                u => u.Id,
                (c, u) =>
                {
                    var positionCode = ParameterCode.From(c.CommitteePositionCode);
                    var paramPosition = paramPositions.SingleOrDefault(p => p.Code == positionCode);
                    var fullPositionName = u.Employee.View?.FullPositionName ?? string.Empty;

                    return PPettyCashCommittee.Create(
                        pPettyCash.Id,
                        c.GroupType,
                        u.Id,
                        u.FullName,
                        fullPositionName,
                        positionCode,
                        paramPosition != null ? paramPosition.Label : string.Empty,
                        c.Sequence);
                })
            .Iter(c => pPettyCash.AddCommittee(c));
    }

    private async Task AddAttachmentsToPPettyCash(
        PPettyCash pPettyCash,
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        await this.ValidateDocumentTypes(attachments, ct);

        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => (
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic)))
                       .ToArray();

        _ = fileList
            .Map(a => PPettyCashAttachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => pPettyCash.AddAttachment(a));
    }

    private async Task ValidateDocumentTypes(
        AttachmentsDto[] attachments,
        CancellationToken ct)
    {
        var documentTypeCodes = attachments.Select(a => ParameterCode.From(a.DocumentTypeCode)).ToArray();

        var documentTypes = await this.dbContext.SuParameters
                                      .Where(p => documentTypeCodes.Contains(p.Code))
                                      .ToArrayAsync(ct);

        var foundDocumentTypeCodes = documentTypes.Select(p => p.Code).ToArray();

        var missingDocumentTypeCodes = documentTypeCodes.Except(foundDocumentTypeCodes).ToArray();

        if (missingDocumentTypeCodes.Any())
        {
            this.ThrowError(
                r => r.Attachments,
                $"Document types with codes {string.Join(", ", missingDocumentTypeCodes)} not found.",
                StatusCodes.Status404NotFound);
        }
    }

    private async Task AddAcceptorPersonsToPPettyCashAsync(
        PPettyCash pPettyCash,
        AcceptorRequest[] requestAcceptors,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var acceptorUserIds = requestAcceptors.Select(a => UserId.From(a.UserId)).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(s => s.View)
                              .Where(u => acceptorUserIds.Contains(u.Id))
                              .ToArrayAsync(ct);

        AddAcceptorsToPPettyCash(pPettyCash, requestAcceptors, users, sendToAcceptorId);
    }

    private static void AddAcceptorsToPPettyCash(PPettyCash pPettyCash, AcceptorRequest[] requestAcceptors, SuUser[] users, UserId? sendToAcceptorId = null)
    {
        _ = requestAcceptors
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => pPettyCash.Status == PettyCashStatus.Draft ? PPettyCashAcceptor.Create(a.AcceptorType, u, a.Sequence) : PPettyCashAcceptor.CreateWithPending(a.AcceptorType, u, a.Sequence))
            .Iter(a =>
            {
                a.SetSendToAcceptorId(sendToAcceptorId);
                pPettyCash.AddAcceptor(a);
            });
    }

    private async Task ManageAssigneeAsync(
        PPettyCash pPettyCash,
        AssigneeRequest[] assignees,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var lastAssigneeUserId = pPettyCash.Assignees
            .Where(a => a.Type == AssigneeType.Assignee)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (UserId?)a.UserId)
            .FirstOrDefault();

        if (lastAssigneeUserId is not null)
        {
            sendToAcceptorId = lastAssigneeUserId;
        }

        _ = pPettyCash.Assignees
                      .ExceptBy(
                          assignees
                              .Where(w => w.Id.HasValue)
                              .Select(s => s.Id.Value),
                          a => a.Id.Value)
                      .Iter(r => pPettyCash.RemoveAssigneeById(r.Id));

        _ = assignees.Where(w => w.Id.HasValue)
                     .Join(
                         pPettyCash.Assignees,
                         db => db.Id.Value,
                         payload => payload.Id.Value,
                         (payload, db) => new { db, payload })
                     .Iter(r =>
                     {
                         r.db.SetSequence(r.payload.Sequence);
                         r.db.SetSendToAcceptorId(sendToAcceptorId);
                     });

        var newIds = assignees
                     .Where(w => !w.Id.HasValue)
                     .Select(s => UserId.From(s.UserId))
                     .ToArray();

        var userData = await this.dbContext.SuUsers
                                 .Include(e => e.Employee)
                                 .ThenInclude(v => v.View)
                                 .Where(w => newIds.Contains(w.Id))
                                 .ToArrayAsync(ct);

        _ = assignees.Where(w => !w.Id.HasValue)
                     .Join(
                         userData,
                         a => UserId.From(a.UserId),
                         u => u.Id,
                         (a, u) => PPettyCashAssignee.Create(
                             a.AssigneeType,
                             u,
                             a.Sequence))
                     .Iter(a =>
                     {
                         a.SetSendToAcceptorId(sendToAcceptorId);
                         pPettyCash.AddAssignee(a);
                     });
    }
}