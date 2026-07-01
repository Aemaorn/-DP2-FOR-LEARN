namespace GHB.DP2.Application.Features.Procurement.Pw119;

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
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreatePw119Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Pw119Status Status,
    DateTimeOffset Pw119Date,
    string DepartmentCode,
    int BudgetYear,
    string SupplyMethodCode,
    string SupplyMethodSpecialTypeCode,
    string Subject,
    string? Telephone,
    string Source,
    decimal Budget,
    decimal? MedianPrice,
    string W119CategoriesCode,
    string? Reason,
    string? AssignSegmentCode,
    Pw119AdvanceResponseDto Advance,
    VendorResponseDto[]? Vendors,
    GLAccountResponseDto[]? GLAccounts,
    AcceptorRequest[]? Acceptors,
    AttachmentsDto[] Attachments);

public record Pw119AdvanceResponseDto(
    [property: Description("เป็นการเบิกล่วงหน้า")]
    bool IsAdvance,
    [property: Description("ชื่อผู้เบิกล่วงหน้า")]
    string? AdvanceName,
    [property: Description("รหัสวิธีชำระเงินล่วงหน้า")]
    string? AdvancePaymentMethodCode,
    [property: Description("วันที่ชำระเงินล่วงหน้า")]
    DateTimeOffset? AdvancePaymentDate,
    [property: Description("รหัสธนาคารล่วงหน้า")]
    string? AdvanceBankCode,
    [property: Description("เลขบัญชีธนาคารล่วงหน้า")]
    string? AdvanceBankAccount,
    [property: Description("สาขาธนาคารล่วงหน้า")]
    string? AdvanceBankBranch,
    [property: Description("ชื่อบัญชีธนาคารล่วงหน้า")]
    string? AdvanceBankAccountName,
    [property: Description("รายละเอียดการเบิกล่วงหน้า")]
    string? AdvanceDetail);

public record VendorResponseDto(
    [property: Description("รหัสผู้ขาย")]
    Guid? Id,
    [property: Description("ประเภทผู้ขาย")]
    string VendorType,
    [property: Description("รหัสผู้ขายในระบบ")]
    Guid? SuVendorId,
    [property: Description("ชื่อผู้ขาย")]
    string VendorName,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("เลขประจำตัวผู้เสียภาษี")]
    string? TaxNumber,
    [property: Description("เลขสาขาผู้ขาย")]
    string? VendorBranchNumber,
    [property: Description("รหัสประเภทภาษีมูลค่าเพิ่ม")]
    string? VatIncludeTypeCode,
    [property: Description("รหัสประเภทบิล")]
    string BillTypeCode,
    [property: Description("ประเภทเอกสาร อื่นๆ")]
    string? BillTypeOther,
    [property: Description("เลขที่สมุดบิล")]
    string BillBookNo,
    [property: Description("วันที่บิล")]
    DateTimeOffset? BillDate,
    [property: Description("รายละเอียดบิล")]
    string? BillDetail,
    [property: Description("รายการสินค้าของผู้ขาย")]
    IEnumerable<VendorParcelResponseDto> VendorParcels);

public record VendorParcelResponseDto(
    [property: Description("รหัสรายการสินค้า")]
    Guid? Id,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รายการสินค้า")]
    string Item,
    [property: Description("รายละเอียดรายการ")]
    string? ItemDetail,
    [property: Description("จำนวน")]
    int Quantity,
    [property: Description("รหัสหน่วย")]
    string UnitCode,
    [property: Description("ราคาต่อหน่วย")]
    decimal UnitPrice,
    [property: Description("ราคารวม")]
    decimal TotalPrice,
    [property: Description("ราคารวมรวมภาษี")]
    decimal TotalPriceVat,
    string? VatIncludeTypeCode);

public record GLAccountResponseDto(
    [property: Description("รหัสบัญชี GL")]
    Guid? Id,
    [property: Description("ลำดับ")]
    int Sequence,
    [property: Description("รหัส SOL")]
    string SolId,
    [property: Description("รหัสประเภทงบประมาณ")]
    string BudgetTypeCode,
    [property: Description("รหัสบัญชี GL")]
    string GLAccountCode,
    [property: Description("เลขโครงการ")]
    string? ProjectNumber,
    [property: Description("จำนวนเงิน")]
    decimal Amount);

public class CreatePw119RequestValidator : Validator<CreatePw119Request>
{
    public CreatePw119RequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class CreatePw119Endpoint : Pw119EndpointBase<CreatePw119Request, Created<Guid>>
{
    private readonly IOperationService operationService;
    private readonly Dp2DbContext dbContext;

    public CreatePw119Endpoint(
        ILogger<CreatePw119Endpoint> logger,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        Dp2DbContext dbContext)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("CreatePw119")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreatePw119Request>("application/json"));
        this.Post("Pw119");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(
        CreatePw119Request req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var pw119Number = await this.GeneratePw119NumberAsync(req.BudgetYear, ct);
        var pw119 = CreatePw119FromRequest(req, pw119Number);

        if (req.Acceptors != null && req.Acceptors.Any())
        {
            await this.AddAcceptorPersonsToPw119Async(pw119, req.Acceptors, ct, UserId.From(req.UserId));
        }

        await this.AddAttachmentsToPw119(pw119, req.Attachments, ct);

        await this.SetDefaultDocumentTemplate(pw119, ct);

        this.dbContext.Pw119s.Add(pw119);
        await this.dbContext.SaveChangesAsync(ct);

        // Reload entity with includes needed by MapToReplaceDtoAsync
        var pw119Reloaded = await this.GetPw119ById(pw119.Id, ct);
        var lastedApproval = pw119Reloaded.LastedDraftDocument(Pw119DocumentType.Approval);
        var lastedWinner = pw119Reloaded.LastedDraftDocument(Pw119DocumentType.WinnerAnnouncement);

        if (lastedApproval is not null && lastedWinner is not null)
        {
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDtoAsync(
                pw119Reloaded,
                ct,
                false,
                creatorUserId: null);

            var approvalFileId = await documentService.CopyDocumentTemplateAsync(
                lastedApproval.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.Pw119}/{pw119Reloaded.Id}_{Pw119DocumentType.Approval}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            var winnerFileId = await documentService.CopyDocumentTemplateAsync(
                lastedWinner.FileId,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.Pw119}/{pw119Reloaded.Id}_{Pw119DocumentType.WinnerAnnouncement}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (approvalFileId.HasValue)
            {
                pw119Reloaded.AddDocumentHistory(Pw119DocumentType.Approval, approvalFileId.Value);
            }

            if (winnerFileId.HasValue)
            {
                pw119Reloaded.AddDocumentHistory(Pw119DocumentType.WinnerAnnouncement, winnerFileId.Value);
            }

            await this.dbContext.SaveChangesAsync(ct);
        }

        return TypedResults.Created(string.Empty, pw119.Id.Value);
    }

    private async Task ValidateRequestAsync(CreatePw119Request req, CancellationToken ct)
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

    private async Task<Pw119Number> GeneratePw119NumberAsync(int budgetYear, CancellationToken ct)
    {
        var lastPw119 = await this.dbContext.Pw119s
                                  .IgnoreQueryFilters()
                                 .Where(p => p.BudgetYear == budgetYear && !p.IsDeleted)
                                 .OrderByDescending(p => p.Pw119Number)
                                 .FirstOrDefaultAsync(ct);

        return lastPw119 is null
            ? Pw119Number.New(budgetYear)
            : lastPw119.Pw119Number.Next();
    }

    private static Pw119 CreatePw119FromRequest(CreatePw119Request req, Pw119Number pw119Number)
    {
        var pw119 = Pw119.Create(pw119Number)
                         .SetPw119Date(req.Pw119Date)
                         .SetDocumentDate(req.Pw119Date)
                         .SetSupplyMethod(ParameterCode.From(req.SupplyMethodCode), ParameterCode.From(req.SupplyMethodSpecialTypeCode))
                         .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
                         .SetBudgetYear(req.BudgetYear)
                         .SetSubject(req.Subject)
                         .SetTelephone(req.Telephone)
                         .SetSource(req.Source)
                         .SetBudget(req.Budget)
                         .SetMedianPrice(req.MedianPrice)
                         .SetIsAdvance(req.Advance.IsAdvance)
                         .SetW119CategoriesCode(ParameterCode.From(req.W119CategoriesCode))
                         .SetReason(req.Reason);

        if (req.AssignSegmentCode is not null)
        {
            pw119.SetAssignSegment(ParameterCode.From(req.AssignSegmentCode));
        }

        _ = pw119.SetAdvanceName(req.Advance.AdvanceName)
                     .SetAdvancePayment(
                         req.Advance.AdvancePaymentMethodCode.IsNullOrEmpty()
                             ? null
                             : ParameterCode.From(req.Advance.AdvancePaymentMethodCode!),
                         req.Advance.AdvancePaymentDate)
                     .SetAdvanceBank(req.Advance.AdvanceBankCode != null ? ParameterCode.From(req.Advance.AdvanceBankCode) : null, req.Advance.AdvanceBankAccount, req.Advance.AdvanceBankBranch, req.Advance.AdvanceBankAccountName)
                     .SetAdvanceDetail(req.Advance.AdvanceDetail);

        if (req.Vendors is not null)
        {
            req.Vendors
               .Iter(v => CreatePw119VendorFromRequest(pw119, v));
        }

        if (req.GLAccounts is not null)
        {
            req.GLAccounts
               .Iter(g => CreateGLAccountFromRequest(pw119, g));
        }

        return pw119;
    }

    private static void CreatePw119VendorFromRequest(Pw119 pw119, VendorResponseDto req)
    {
        var vendor = Pw119Vendor.Create(pw119.Id)
                                .SetSequence(req.Sequence)
                                .SetVendorType(req.VendorType)
                                .SetVendor(req.SuVendorId != null ? SuVendorId.From(req.SuVendorId.Value) : null, req.TaxNumber, req.VendorName, req.VendorBranchNumber)
                                .SetBill(req.VatIncludeTypeCode != null ? ParameterCode.From(req.VatIncludeTypeCode) : null, ParameterCode.From(req.BillTypeCode), req.BillTypeOther, req.BillBookNo, req.BillDate, req.BillDetail);

        pw119.AddVendor(vendor);

        if (req.VendorParcels is not null)
        {
            req.VendorParcels
               .Iter(vp => CreatePw119VendorParcelFromRequest(vp, vendor));
        }
    }

    private static void CreatePw119VendorParcelFromRequest(VendorParcelResponseDto req, Pw119Vendor vendor)
    {
        var parcel = Pw119VendorParcel.Create(vendor.Id)
                                      .SetSequence(req.Sequence)
                                      .SetItem(req.Item, req.ItemDetail)
                                      .SetPrice(req.Quantity, ParameterCode.From(req.UnitCode), req.UnitPrice, req.TotalPrice, req.TotalPriceVat)
                                      .SetVatIncludeType(req.VatIncludeTypeCode != null ? ParameterCode.From(req.VatIncludeTypeCode) : null);

        vendor.AddVendorParcels(parcel);
    }

    private static void CreateGLAccountFromRequest(Pw119 data, GLAccountResponseDto req)
    {
        var glAcc = Pw119GLAccount.Create(data.Id)
                                  .SetGLAccount(req.Sequence, req.SolId, ParameterCode.From(req.BudgetTypeCode), ParameterCode.From(req.GLAccountCode), req.ProjectNumber, req.Amount);
        data.AddGLAccount(glAcc);
    }

    private async Task AddAttachmentsToPw119(
        Pw119 pw119,
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
            .Map(a => Pw119Attachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => pw119.AddAttachment(a));
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

    private async Task AddAcceptorPersonsToPw119Async(
        Pw119 pw119,
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

        AddAcceptorsToPw119(pw119, requestAcceptors, users, sendToAcceptorId);
    }

    private static void AddAcceptorsToPw119(Pw119 pw119, AcceptorRequest[] requestAcceptors, SuUser[] users, UserId? sendToAcceptorId = null)
    {
        _ = requestAcceptors
            .Join(
                users,
                a => UserId.From(a.UserId),
                u => u.Id,
                (a, u) => Pw119Acceptor.Create(a.AcceptorType, u, a.Sequence))
            .Iter(a =>
            {
                a.SetSendToAcceptorId(sendToAcceptorId);
                pw119.AddAcceptor(a);
            });
    }
}