namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateP79Clause2Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    P79Clause2Status Status,
    DateTimeOffset P79Clause2Date,
    DateTimeOffset? DeliveryDate,
    string? ProcurementReasonItem1,
    string? ProcurementReasonItem2,
    string DepartmentCode,
    int BudgetYear,
    string SupplyMethodCode,
    string SupplyMethodTypeCode,
    string? SupplyMethodSpecialTypeCode,
    string Subject,
    string? Telephone,
    string Source,
    decimal Budget,
    decimal? MedianPrice,
    string? ReasonItem1,
    string? ReasonItem2,
    string? ReasonItem3,
    bool IsAdvance,
    string? AssignSegmentCode,
    P79Clause2AdvanceResponseDto Advance,
    VendorResponseDto[]? Vendors,
    GLAccountResponseDto[]? GLAccounts,
    AcceptorRequest[]? Acceptors,
    AttachmentsDto[] Attachments,
    [property: Description("ข้อมูลผู้จัดทำเอกสาร")]
    CreatorReplace? Creator,
    [property: Description("ข้อมูลผู้ประกาศผู้ชนะ")]
    PublisherDto? Publisher);

public record P79Clause2AdvanceResponseDto(
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
    string? BillBookNo,
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
    decimal TotalPriceVat,
    string? VatIncludeTypeCode);

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

public class CreateP79Clause2RequestValidator : Validator<CreateP79Clause2Request>
{
    public CreateP79Clause2RequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class CreateP79Clause2Endpoint : P79Clause2EndpointBase<CreateP79Clause2Request, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;

    public CreateP79Clause2Endpoint(
        ILogger<CreateP79Clause2Endpoint> logger,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, operationService, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("CreateP79Clause2")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreateP79Clause2Request>("application/json"));
        this.Post("P79Clause2");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreateP79Clause2Request req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var p79Clause2Number = await this.GenerateP79Clause2NumberAsync(req.BudgetYear, ct);
        var p79Clause2 = CreateP79Clause2FromRequest(req, p79Clause2Number);

        if (req.Acceptors != null && req.Acceptors.Any())
        {
            await this.AddAcceptorPersonsToP79Clause2Async(p79Clause2, req.Acceptors, ct, UserId.From(req.UserId));
        }

        await this.AddAttachmentsToP79Clause2(p79Clause2, req.Attachments, ct);

        await this.SetDefaultDocumentTemplate(p79Clause2, ct);

        p79Clause2.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            ActivityLogActionTypeConstant.Create,
            nameof(P79Clause2Status.Draft)));

        this.dbContext.P79Clause2s.Add(p79Clause2);
        await this.dbContext.SaveChangesAsync(ct);

        // Reload entity with includes needed by MapToReplaceDto
        var p79Reloaded = await this.GetP79Clause2ById(p79Clause2.Id, ct);
        var lastedApproval = p79Reloaded.LastedDraftDocument(P79Clause2DocumentType.Approval);
        var lastedWinner = p79Reloaded.LastedDraftDocument(P79Clause2DocumentType.WinnerAnnouncement);

        if (lastedApproval is not null && lastedWinner is not null)
        {
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDto(
                p79Reloaded,
                ct,
                false,
                creatorUserId: null);

            var approvalFileId = await documentService.CopyDocumentTemplateAsync(
                lastedApproval.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.P79Clause2}/{p79Reloaded.Id}_{P79Clause2DocumentType.Approval}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            var winnerFileId = await documentService.CopyDocumentTemplateAsync(
                lastedWinner.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.P79Clause2}/{p79Reloaded.Id}_{P79Clause2DocumentType.WinnerAnnouncement}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (approvalFileId.HasValue)
            {
                p79Reloaded.AddDocumentHistory(P79Clause2DocumentType.Approval, approvalFileId.Value);
            }

            if (winnerFileId.HasValue)
            {
                p79Reloaded.AddDocumentHistory(P79Clause2DocumentType.WinnerAnnouncement, winnerFileId.Value);
            }

            await this.dbContext.SaveChangesAsync(ct);
        }

        await this.SendNotificationAsync(p79Clause2);

        return TypedResults.Created(string.Empty, p79Clause2.Id.Value);
    }

    private async Task ValidateRequestAsync(CreateP79Clause2Request req, CancellationToken ct)
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

        if (req.GLAccounts is null)
        {
            this.ThrowError(
                "กรุณาระบุข้อมูลรหัสบัญชีและการใช้งบประมาณของฝ่าย",
                StatusCodes.Status400BadRequest);
        }
    }

    private async Task<P79Clause2Number> GenerateP79Clause2NumberAsync(int budgetYear, CancellationToken ct)
    {
        var lastP79Clause2 = await this.dbContext.P79Clause2s
                                       .IgnoreQueryFilters()
                                       .Where(p => p.BudgetYear == budgetYear && !p.IsDeleted)
                                       .OrderByDescending(p => p.P79Clause2Number)
                                       .FirstOrDefaultAsync(ct);

        return lastP79Clause2 is null
            ? P79Clause2Number.New(budgetYear)
            : lastP79Clause2.P79Clause2Number.Next();
    }

    private static P79Clause2 CreateP79Clause2FromRequest(CreateP79Clause2Request req, P79Clause2Number p79Clause2Number)
    {
        var p79Clause2 = P79Clause2.Create(p79Clause2Number)
                                   .SetP79Clause2Date(req.P79Clause2Date)
                                   .SetDocumentDate(req.P79Clause2Date)
                                   .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
                                   .SetBudgetYear(req.BudgetYear)
                                   .SetSupplyMethod(
                                       ParameterCode.From(req.SupplyMethodCode),
                                       ParameterCode.From(req.SupplyMethodTypeCode),
                                       req.SupplyMethodSpecialTypeCode.IsNullOrEmpty() ? null : ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
                                   .SetSubject(req.Subject)
                                   .SetTelephone(req.Telephone)
                                   .SetSource(req.Source)
                                   .SetBudget(req.Budget)
                                   .SetMedianPrice(req.MedianPrice)
                                   .SetReasonItem(req.ReasonItem1, req.ReasonItem2, req.ReasonItem3)
                                   .SetIsAdvance(req.IsAdvance)
                                   .SetDeliveryDate(req.DeliveryDate)
                                   .SetProcurementReasonItem(req.ProcurementReasonItem1, req.ProcurementReasonItem2);

        if (req.AssignSegmentCode is not null)
        {
            p79Clause2.SetAssignSegment(ParameterCode.From(req.AssignSegmentCode));
        }

        _ = p79Clause2.SetAdvanceName(req.Advance.AdvanceName)
                      .SetAdvancePayment(
                          req.Advance.AdvancePaymentMethodCode.IsNullOrEmpty()
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

        if (req.Vendors is not null && req.Vendors.Length > 0)
        {
            req.Vendors
               .Iter(v => CreateP79Clause2VendorFromRequest(p79Clause2, v));
        }

        if (req.GLAccounts is not null && req.GLAccounts.Length > 0)
        {
            req.GLAccounts
               .Iter(g => CreateGLAccountFromRequest(p79Clause2, g));
        }

        return p79Clause2;
    }

    private static void CreateP79Clause2VendorFromRequest(P79Clause2 data, VendorResponseDto req)
    {
        var vendor = P79Clause2Vendor.Create(data.Id)
                                     .SetSequence(req.Sequence)
                                     .SetVendorType(req.VendorType)
                                     .SetVendor(req.SuVendorId != null ? SuVendorId.From(req.SuVendorId.Value) : null, req.TaxNumber, req.VendorName, req.VendorBranchNumber)
                                     .SetBill(
                                         req.VatIncludeTypeCode != null
                                             ? ParameterCode.From(req.VatIncludeTypeCode)
                                             : null,
                                         ParameterCode.From(req.BillTypeCode),
                                         req.BillTypeOther,
                                         req.BillBookNo,
                                         req.BillDate,
                                         req.BillDetail);

        data.AddVendor(vendor);

        req.VendorParcels
           .Iter(vp => CreateP79Clause2VendorParcelFromRequest(vp, vendor));
    }

    private static void CreateP79Clause2VendorParcelFromRequest(VendorParcelResponseDto req, P79Clause2Vendor vendor)
    {
        var parcel = P79Clause2VendorParcel.Create(vendor.Id)
                                           .SetSequence(req.Sequence)
                                           .SetItem(req.Item, req.ItemDetail)
                                           .SetPrice(
                                               req.Quantity,
                                               ParameterCode.From(req.UnitCode),
                                               req.UnitPrice,
                                               req.TotalPrice,
                                               req.TotalPriceVat,
                                               req.VatIncludeTypeCode != null ? ParameterCode.From(req.VatIncludeTypeCode) : null);

        vendor.AddVendorParcels(parcel);
    }

    private static void CreateGLAccountFromRequest(P79Clause2 data, GLAccountResponseDto req)
    {
        var glAccount = P79Clause2GLAccount.Create(data.Id)
                                           .SetGLAccount(req.Sequence, req.SolId, ParameterCode.From(req.BudgetTypeCode), ParameterCode.From(req.GLAccountCode), req.ProjectNumber, req.Amount);

        data.AddGLAccount(glAccount);
    }

    private async Task AddAttachmentsToP79Clause2(
        P79Clause2 p79Clause2,
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
            .Map(a => P79Clause2Attachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => p79Clause2.AddAttachment(a));
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

    private async Task AddAcceptorPersonsToP79Clause2Async(
        P79Clause2 p79Clause2,
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

        AddAcceptorsToP79Clause2(p79Clause2, requestAcceptors, users, sendToAcceptorId);
    }

    private static void AddAcceptorsToP79Clause2(P79Clause2 p79Clause2, AcceptorRequest[] requestAcceptors, SuUser[] users, UserId? sendToAcceptorId = null)
    {
        _ = requestAcceptors
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => P79Clause2Acceptor.Create(a.AcceptorType, u, a.Sequence))
            .Iter(a =>
            {
                a.SetSendToAcceptorId(sendToAcceptorId);
                p79Clause2.AddAcceptor(a);
            });
    }
}