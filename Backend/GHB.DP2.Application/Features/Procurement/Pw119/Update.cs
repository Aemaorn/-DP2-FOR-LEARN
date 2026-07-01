namespace GHB.DP2.Application.Features.Procurement.Pw119;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Application.Validators;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Pw119.Abstract;
using GHB.DP2.Domain.Common;
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

public record UpdatePw119Request(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id,
    Pw119Status Status,
    DateTimeOffset Pw119Date,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription,
    string DepartmentCode,
    int BudgetYear,
    string SupplyMethodCode,
    string SupplyMethodTypeCode,
    string SupplyMethodSpecialTypeCode,
    string Subject,
    string? Telephone,
    string Source,
    decimal Budget,
    decimal? MedianPrice,
    string ReasonItem1,
    string ReasonItem2,
    string ReasonItem3,
    string W119CategoriesCode,
    string? Reason,
    string? AssignSegmentCode,
    Pw119AdvanceResponseDto Advance,
    VendorResponseDto[]? Vendors,
    GLAccountResponseDto[]? GLAccounts,
    AcceptorRequest[]? Acceptors,
    AcceptorRequest[] AcceptanceConfirmers,
    AttachmentsDto[] Attachments,
    Guid? ApprovalRequestDocumentId,
    bool? IsApprovalRequestDocumentReplace,
    Guid? WinnerAnnounceDocumentId,
    bool? IsWinnerAnnounceDocumentReplace);

public record UpdatePw119Response(
    Guid? NewApprovalRequestDocumentFileId,
    Guid? NewWinnerAnnounceDocumentFileId);

public class UpdatePw119RequestValidator : Validator<UpdatePw119Request>
{
    public UpdatePw119RequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class UpdatePw119Endpoint : Pw119EndpointBase<UpdatePw119Request, Results<Ok<UpdatePw119Response>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;
    private readonly IOperationService operationService;

    public UpdatePw119Endpoint(
        ILogger<UpdatePw119Endpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("UpdatePw119")
             .Produces<UpdatePw119Response>(StatusCodes.Status200OK)
             .Produces<NotFound>()
             .Accepts<UpdatePw119Request>("application/json"));
        this.Put("Pw119/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdatePw119Response>, NotFound<string>>> HandleRequestAsync(
        UpdatePw119Request req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var entity = await this.GetPw119ById(Pw119Id.From(req.Id), ct);

        this.ValidateDocument(req, entity);

        var (newApprovalFileId, newWinnerFileId) = await this.UpdateDocumentAsync(entity, req, ct);

        entity.SetPw119Date(req.Pw119Date)
              .SetDocumentDate(req.Pw119Date)
              .SetSupplyMethod(ParameterCode.From(req.SupplyMethodCode), ParameterCode.From(req.SupplyMethodSpecialTypeCode))
              .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
              .SetBudgetYear(req.BudgetYear)
              .SetSubject(req.Subject)
              .SetTelephone(req.Telephone)
              .SetSource(req.Source)
              .SetBudget(req.Budget)
              .SetMedianPrice(req.MedianPrice)
              .SetReason(req.Reason)
              .SetIsAdvance(req.Advance.IsAdvance)
              .SetW119CategoriesCode(ParameterCode.From(req.W119CategoriesCode));

        if (req.DisbursementDate is not null
            && req.DisbursementAmount is not null
            && req.DisbursementDescription is not null)
        {
            entity.SetDisbursement(req.DisbursementDate, req.DisbursementAmount, req.DisbursementDescription);
        }

        if (req.AssignSegmentCode is not null)
        {
            entity.SetAssignSegment(ParameterCode.From(req.AssignSegmentCode));
        }

        UpdateAdvance(entity, req);

        UpdateVendor(entity, req);

        UpdateGlAccounts(entity, req);

        AcceptorRequest[] acceptorRequest =
        [
            .. (req.Acceptors ?? []).Where(a => a.AcceptorType != AcceptorType.AccountingConfirmer),
            .. req.AcceptanceConfirmers
        ];

        var previousFirstPendingUserId = entity.Acceptors
            .Where(a => !a.IsDeleted &&
                        (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
            .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
            .ThenBy(a => a.Sequence)
            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending)
            ?.UserId;

        var newConfirmerUserIds = (req.AcceptanceConfirmers ?? [])
            .Where(a => !a.Id.HasValue)
            .Select(a => UserId.From(a.UserId))
            .ToHashSet();

        var previousAcceptorIds = entity.Acceptors.Select(a => a.Id.Value).ToHashSet();

        var keptAcceptorIds = (acceptorRequest ?? [])
            .Where(a => a.Id.HasValue)
            .Select(a => a.Id!.Value)
            .ToHashSet();

        var removedAcceptors = entity.Acceptors
            .Where(a => !keptAcceptorIds.Contains(a.Id.Value) &&
                        a.Type is AcceptorType.AccountingOperator or AcceptorType.AccountingApprover or AcceptorType.AccountingConfirmer)
            .ToList();

        await this.UpsertAcceptors(entity, acceptorRequest, UserId.From(req.UserId));

        var addedAcceptors = entity.Acceptors
            .Where(a => !previousAcceptorIds.Contains(a.Id.Value) &&
                        a.Type is AcceptorType.AccountingOperator or AcceptorType.AccountingApprover or AcceptorType.AccountingConfirmer)
            .ToList();

        if (entity.Status == Pw119Status.WaitingAccountingApproval)
        {
            var accountingAcceptors = entity.Acceptors
                .Where(a => !a.IsDeleted &&
                            (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                .ThenBy(a => a.Sequence)
                .ToList();

            var firstPending = accountingAcceptors.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

            if (firstPending != null && firstPending.UserId != previousFirstPendingUserId)
            {
                var isLastPending = accountingAcceptors.Count(a => a.Status == AcceptorStatus.Pending) == 1;
                var title = isLastPending ? NotificationConstant.WaitForApprove.Title : NotificationConstant.WaitForLike.Title;
                var message = string.Format(
                    isLastPending ? NotificationConstant.WaitForApprove.Message : NotificationConstant.WaitForLike.Message,
                    ProgramConstant.W119.Name,
                    entity.Pw119Number);

                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(entity, targetUserId, title, message);
                }
            }
        }

        if (entity.Status == Pw119Status.WaitingDisbursementDate && newConfirmerUserIds.Count > 0)
        {
            var confirmerTargets = entity.Acceptors
                .Where(a => !a.IsDeleted &&
                            a.Type == AcceptorType.AccountingConfirmer &&
                            newConfirmerUserIds.Contains(a.UserId))
                .SelectMany(a => a.GetNotificationTargets());

            foreach (var targetUserId in confirmerTargets)
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitConfirmDisbursement.Title,
                    string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.W119.Name, entity.Pw119Number));
            }
        }

        await this.RemoveFileInServiceAsync(entity, req, ct);

        if (req.Status == entity.Status)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.Update,
                ActivityLogActionTypeConstant.Update,
                nameof(entity.Status)));
        }

        foreach (var removed in removedAcceptors)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.RemoveAcceptor,
                ActivityLogActionTypeConstant.RemoveAcceptor,
                nameof(entity.Acceptors),
                removed.FullName));
        }

        foreach (var added in addedAcceptors)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.AddAcceptor,
                ActivityLogActionTypeConstant.AddAcceptor,
                nameof(entity.Acceptors),
                added.FullName));
        }

        await this.dbContext.SaveChangesAsync(ct);

        // Re-replace documents from original template — only when form data changed
        var pw119Reloaded = await this.GetPw119ById(entity.Id, ct);
        pw119Reloaded.SetStatus(req.Status);

        if (req.Status == Pw119Status.Paid)
        {
            pw119Reloaded.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.ConfirmDisbursement,
                ActivityLogActionTypeConstant.ConfirmDisbursement,
                req.Status.ToString()));

            _ = SendNotificationAsync(
                pw119Reloaded,
                UserId.From(pw119Reloaded.AuditInfo.CreatedBy),
                NotificationConstant.DisbursementPaid.Title,
                string.Format(NotificationConstant.DisbursementPaid.Message, ProgramConstant.W119.Name, pw119Reloaded.Pw119Number));
        }

        if (pw119Reloaded.Status is Pw119Status.Draft or Pw119Status.Edit or Pw119Status.Rejected or Pw119Status.WaitingApproval)
        {
            var documentService = this.Resolve<IDocumentService>();
            var replaceDto = await this.MapToReplaceDtoAsync(pw119Reloaded, ct, false, UserId.From(req.UserId));
            var isReplace = req.IsApprovalRequestDocumentReplace ?? false;

            var templateFileId = isReplace && (pw119Reloaded.Status is Pw119Status.Draft or Pw119Status.Edit or Pw119Status.Rejected)
                ? (FileId?)await this.GetDocumentTemplateForReplace(Pw119DocumentType.Approval, ct)
                : pw119Reloaded.LastedDocumentVersions(Pw119DocumentType.Approval)?.FileId;

            var approvalFileId = await documentService.CopyDocumentTemplateAsync(
                templateFileId.Value,
                contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                parentDirectory: $"{DocumentTemplateGroups.Pw119}/{pw119Reloaded.Id}_{Pw119DocumentType.Approval}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (approvalFileId.HasValue)
            {
                pw119Reloaded.AddDocumentHistory(Pw119DocumentType.Approval, approvalFileId.Value);
            }

            await this.dbContext.SaveChangesAsync(ct);
        }

        return TypedResults.Ok(new UpdatePw119Response(newApprovalFileId?.Value, newWinnerFileId?.Value));
    }

    private async Task<FileId> GetDocumentTemplateForReplace(
        Pw119DocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = documentType == Pw119DocumentType.Approval
            ? Pw119TemplateConstant.ApprovalRequest60
            : Pw119TemplateConstant.WinnerAnnounce60;

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.Pw119 &&
                dt.IsActive &&
                dt.Code == templateCode,
            ct);

        if (templateFileId is null)
        {
            this.ThrowError(
                DocumentErrorMessages.TemplateNotFoundForReset,
                StatusCodes.Status404NotFound);
        }

        return templateFileId.Value;
    }

    private async Task<(FileId? ApprovalFileId, FileId? WinnerFileId)> UpdateDocumentAsync(
        Pw119 entity,
        UpdatePw119Request req,
        CancellationToken ct)
    {
        var isApprovalRequestDocumentReplace = req.IsApprovalRequestDocumentReplace ?? false;
        var isWinnerAnnounceDocumentReplace = req.IsWinnerAnnounceDocumentReplace ?? false;

        var mustSaveApprovalDocument =
            req.ApprovalRequestDocumentId.HasValue &&
            entity.Status != Pw119Status.WaitingApproval &&
            isApprovalRequestDocumentReplace;

        var mustSaveWinnerDocument =
            req.WinnerAnnounceDocumentId.HasValue &&
            entity.Status != Pw119Status.WaitingApproval &&
            isWinnerAnnounceDocumentReplace;

        FileId? newApprovalFileId = null;
        FileId? newWinnerFileId = null;

        if (mustSaveApprovalDocument)
        {
            newApprovalFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                Pw119DocumentType.Approval,
                FileId.From(req.ApprovalRequestDocumentId!.Value),
                isApprovalRequestDocumentReplace,
                ct);
        }

        if (mustSaveWinnerDocument)
        {
            newWinnerFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                Pw119DocumentType.WinnerAnnouncement,
                FileId.From(req.WinnerAnnounceDocumentId!.Value),
                isWinnerAnnounceDocumentReplace,
                ct);
        }

        return (newApprovalFileId, newWinnerFileId);
    }

    private void ValidateDocument(UpdatePw119Request req, Pw119 entity)
    {
        if (req is { ApprovalRequestDocumentId: not null, Status: Pw119Status.WaitingApproval } &&
            !entity.DocumentHistories.Any())
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private static void UpdateGlAccounts(Pw119 entity, UpdatePw119Request req)
    {
        if (req.GLAccounts is null)
        {
            return;
        }

        entity.AddGLAccounts(req.GLAccounts.Map(gl =>
            entity.GLAccounts
                  .FirstOrNone(vd => vd.Id == (gl.Id != null ? Pw119GLAccountId.From(gl.Id.Value) : null))
                  .Match(
                      Some: g => g.SetGLAccount(
                          gl.Sequence,
                          gl.SolId,
                          ParameterCode.From(gl.BudgetTypeCode),
                          ParameterCode.From(gl.GLAccountCode),
                          gl.ProjectNumber,
                          gl.Amount),
                      None: () => CreateGlAccountFromRequest(entity!, gl))));
    }

    private static void UpdateVendor(Pw119 entity, UpdatePw119Request req)
    {
        if (req.Vendors is null)
        {
            return;
        }

        entity.AddVendors(req.Vendors.Map(rv =>
            entity.Vendors
                  .FirstOrNone(vd => vd.Id == (rv.Id != null ? Pw119VendorId.From(rv.Id.Value) : null))
                  .Match(
                      Some: vendor =>
                      {
                          vendor.SetSequence(rv.Sequence)
                                .SetVendorType(rv.VendorType)
                                .SetVendor(
                                    rv.SuVendorId != null ? SuVendorId.From(rv.SuVendorId.Value) : null,
                                    rv.TaxNumber,
                                    rv.VendorName,
                                    rv.VendorBranchNumber)
                                .SetBill(
                                    rv.VatIncludeTypeCode != null ? ParameterCode.From(rv.VatIncludeTypeCode) : null,
                                    ParameterCode.From(rv.BillTypeCode),
                                    rv.BillTypeOther,
                                    rv.BillBookNo,
                                    rv.BillDate,
                                    rv.BillDetail);

                          if (rv.VendorParcels.Any())
                          {
                              CreatePw119VendorParcelFromRequest(vendor, rv.VendorParcels);
                          }

                          return vendor;
                      },
                      None: () => CreatePw119VendorFromRequest(entity!, rv))));
    }

    private static void UpdateAdvance(Pw119 entity, UpdatePw119Request req)
    {
        entity.SetAdvanceName(req.Advance.AdvanceName)
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
    }

    private async Task RemoveFileInServiceAsync(Pw119 entity, UpdatePw119Request req, CancellationToken ct)
    {
        var deleteFileIds = await this.ManageAttachments(entity, req.Attachments, ct);

        foreach (var fileId in deleteFileIds)
        {
            await this.fileServiceClient.DeleteAsync(fileId, CancellationToken.None);
        }
    }

    private static Pw119Vendor CreatePw119VendorFromRequest(Pw119 data, VendorResponseDto req)
    {
        var vendor = Pw119Vendor.Create(data.Id)
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

        if (req.VendorParcels is not null)
        {
            CreatePw119VendorParcelFromRequest(vendor!, req.VendorParcels);
        }

        data.AddVendor(vendor);

        return vendor;
    }

    private static void CreatePw119VendorParcelFromRequest(Pw119Vendor data, IEnumerable<VendorParcelResponseDto> reqParcels)
    {
        data.UpdateVendorsParcels(reqParcels.Map(pc =>
            data.VendorParcels
                .FirstOrNone(vd => vd.Id == (pc.Id != null ? Pw119VendorParcelId.From(pc.Id.Value) : null))
                .Match(
                    Some: p =>
                    {
                        return p.SetSequence(pc.Sequence)
                                .SetItem(pc.Item, pc.ItemDetail)
                                .SetPrice(
                                    pc.Quantity,
                                    ParameterCode.From(pc.UnitCode),
                                    pc.UnitPrice,
                                    pc.TotalPrice,
                                    pc.TotalPriceVat)
                                .SetVatIncludeType(pc.VatIncludeTypeCode != null ? ParameterCode.From(pc.VatIncludeTypeCode) : null);
                    },
                    None: () => CreateVendorParcelFromRequest(data!, pc))));
    }

    private static Pw119VendorParcel CreateVendorParcelFromRequest(Pw119Vendor data, VendorParcelResponseDto reqParcel)
    {
        var parcel = Pw119VendorParcel.Create(data.Id)
                                      .SetSequence(reqParcel.Sequence)
                                      .SetItem(reqParcel.Item, reqParcel.ItemDetail)
                                      .SetPrice(
                                          reqParcel.Quantity,
                                          ParameterCode.From(reqParcel.UnitCode),
                                          reqParcel.UnitPrice,
                                          reqParcel.TotalPrice,
                                          reqParcel.TotalPriceVat);

        data.AddVendorParcels(parcel);

        return parcel;
    }

    private static Pw119GLAccount CreateGlAccountFromRequest(Pw119 data, GLAccountResponseDto reqGl)
    {
        var glAcc = Pw119GLAccount.Create(data.Id)
                                  .SetGLAccount(
                                      reqGl.Sequence,
                                      reqGl.SolId,
                                      ParameterCode.From(reqGl.BudgetTypeCode),
                                      ParameterCode.From(reqGl.GLAccountCode),
                                      reqGl.ProjectNumber,
                                      reqGl.Amount);
        data.AddGLAccount(glAcc);

        return glAcc;
    }

    private async Task ValidateRequestAsync(UpdatePw119Request req, CancellationToken ct)
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
    }

    private async Task<IEnumerable<FileId>> ManageAttachments(
        Pw119 data,
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

        var deleteIds = data.Attachments
                            .ExceptBy(
                                fileList.Select(s => s.FileId),
                                w => w.Id.Value)
                            .Select(s => s.Id)
                            .Map(r =>
                            {
                                data.RemoveAttachmentById(r);

                                return r;
                            }) ?? [];

        _ = data.Attachments.Join(
                    fileList,
                    en => en.Id.Value,
                    req => req.FileId,
                    (en, req) => new { en, req })
                .Iter(s => s.en.SetIsPublic(s.req.IsPublic).SetDocumentType(ParameterCode.From(s.req.DocumentTypeCode)));

        _ = fileList
            .ExceptBy(
                data.Attachments.Select(s => s.Id.Value),
                w => w.FileId)
            .Map(a => Pw119Attachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => data.AddAttachment(a));

        return deleteIds;
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
}