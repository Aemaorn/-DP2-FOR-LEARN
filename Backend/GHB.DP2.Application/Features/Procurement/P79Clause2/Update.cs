namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

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
using GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateP79Clause2Request(
    Guid Id,
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
    GLAccountResponseDto[]? GlAccounts,
    AcceptorRequest[]? Acceptors,
    AttachmentsDto[] Attachments,
    Guid? ApprovalRequestDocumentId,
    bool? IsApprovalRequestDocumentReplace,
    Guid? WinnerAnnounceDocumentId,
    bool? IsWinnerAnnounceDocumentReplace,
    DateTimeOffset? DisbursementDate,
    decimal? DisbursementAmount,
    string? DisbursementDescription,
    AcceptorRequest[] AcceptanceConfirmers);

public record UpdateP79Clause2Response(
    Guid? NewApprovalRequestDocumentFileId,
    Guid? NewWinnerAnnounceDocumentFileId);

public class UpdateP79Clause2RequestValidator : Validator<UpdateP79Clause2Request>
{
    public UpdateP79Clause2RequestValidator()
    {
        this.RuleForEach(x => x.Attachments)
            .ChildRules(a => a.RuleForEach(x => x.FileAttachments)
                .ChildRules(f => f.RuleFor(x => x.FileName).MustBeValidFileExtension()));
    }
}

public class UpdateP79Clause2Endpoint : P79Clause2EndpointBase<UpdateP79Clause2Request, Results<Ok<UpdateP79Clause2Response>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;
    private readonly IOperationService operationService;

    public UpdateP79Clause2Endpoint(
        ILogger<UpdateP79Clause2Endpoint> logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger, dbContext, operationService, fileServiceClient)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("UpdateP79Clause2")
             .Produces<UpdateP79Clause2Response>(StatusCodes.Status200OK)
             .Produces<NotFound>()
             .Accepts<UpdateP79Clause2Request>("application/json"));
        this.Put("P79Clause2/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<UpdateP79Clause2Response>, NotFound<string>>> HandleRequestAsync(
        UpdateP79Clause2Request req,
        CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var entity = await this.GetP79Clause2ById(P79Clause2Id.From(req.Id), ct);

        this.ValidateDocument(req, entity);

        var isChangeTemplate =
            entity.SupplyMethodCode != ParameterCode.From(req.SupplyMethodCode);

        var (newApprovalFileId, newWinnerFileId) = await this.UpdateDocumentAsync(entity, req, ct);

        entity.SetP79Clause2Date(req.P79Clause2Date)
              .SetDocumentDate(req.P79Clause2Date)
              .SetSupplyMethod(
                  ParameterCode.From(req.SupplyMethodCode),
                  ParameterCode.From(req.SupplyMethodTypeCode),
                  req.SupplyMethodSpecialTypeCode.IsNullOrEmpty() ? null : ParameterCode.From(req.SupplyMethodSpecialTypeCode!))
              .SetDepartmentId(BusinessUnitId.From(req.DepartmentCode))
              .SetBudgetYear(req.BudgetYear)
              .SetSubject(req.Subject)
              .SetTelephone(req.Telephone)
              .SetSource(req.Source)
              .SetReasonItem(req.ReasonItem1, req.ReasonItem2, req.ReasonItem3)
              .SetBudget(req.Budget)
              .SetMedianPrice(req.MedianPrice)
              .SetIsAdvance(req.IsAdvance)
              .SetDeliveryDate(req.DeliveryDate)
              .SetProcurementReasonItem(req.ProcurementReasonItem1, req.ProcurementReasonItem2);

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

        AcceptorRequest[] acceptorRequest =
        [
            .. (req.Acceptors ?? []).Where(a => a.AcceptorType != AcceptorType.AccountingConfirmer),
            .. req.AcceptanceConfirmers ?? []
        ];

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

        if (entity.Status == P79Clause2Status.WaitingAccountingApproval)
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
                    ProgramConstant.Urgent79Clause2.Name,
                    entity.P79Clause2Number);

                foreach (var targetUserId in firstPending.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(entity, targetUserId, title, message);
                }
            }
        }

        var isBranch = entity.Department?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Branch
            || entity.Department?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Zone
            || entity.Department?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Segment;

        if (!isBranch && newConfirmerUserIds.Count > 0)
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
                    string.Format(NotificationConstant.WaitConfirmDisbursement.Message, ProgramConstant.Urgent79Clause2.Name, entity.P79Clause2Number));
            }
        }

        await this.RemoveFileInServiceAsync(entity, req, ct);

        if (isChangeTemplate)
        {
            entity.ClearDocumentHistories();
            await this.SetDefaultDocumentTemplate(entity, ct);
        }

        if (req.Status == entity.Status)
        {
            entity.AddActivity(new ActivityInfo(
                req.Status == P79Clause2Status.WaitingApproval ? ActivityLogActionTypeConstant.SendApprove : ActivityLogActionTypeConstant.Update,
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

        if (req.Status == P79Clause2Status.Paid)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.ConfirmDisbursement,
                ActivityLogActionTypeConstant.ConfirmDisbursement,
                req.Status.ToString()));

            _ = SendNotificationAsync(
                entity,
                UserId.From(entity.AuditInfo.CreatedBy),
                NotificationConstant.DisbursementPaid.Title,
                string.Format(NotificationConstant.DisbursementPaid.Message, ProgramConstant.Urgent79Clause2.Name, entity.P79Clause2Number));
        }

        await this.dbContext.SaveChangesAsync(ct);

        if (req.Status is P79Clause2Status.Draft or P79Clause2Status.Edit or P79Clause2Status.Rejected or P79Clause2Status.WaitingApproval)
        {
            var isReplace = req.IsApprovalRequestDocumentReplace ?? false;
            var shouldCopy = isReplace || req.Status == P79Clause2Status.WaitingApproval;

            if (shouldCopy)
            {
                var p79Reloaded = await this.GetP79Clause2ById(entity.Id, ct);
                p79Reloaded.SetStatus(req.Status);
                var documentService = this.Resolve<IDocumentService>();
                var replaceDto = await this.MapToReplaceDto(p79Reloaded, ct, false, UserId.From(req.UserId));

                var sourceFileId = isReplace
                    ? (FileId?)await this.GetDocumentTemplateForReplace(P79Clause2DocumentType.Approval, ct)
                    : p79Reloaded.LastedDocumentVersions(P79Clause2DocumentType.Approval)?.FileId;

                if (sourceFileId is not null)
                {
                    var finalFileId = await documentService.CopyDocumentTemplateAsync(
                        sourceFileId.Value,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, replaceDto),
                        parentDirectory: $"{DocumentTemplateGroups.P79Clause2}/{p79Reloaded.Id}_{P79Clause2DocumentType.Approval}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct);

                    if (finalFileId is null)
                    {
                        this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
                    }

                    if (finalFileId.HasValue)
                    {
                        p79Reloaded.AddDocumentHistory(P79Clause2DocumentType.Approval, finalFileId.Value, isReplace);
                        await this.dbContext.SaveChangesAsync(ct);
                    }
                }
            }
        }

        await this.SendNotificationAsync(entity);

        return TypedResults.Ok(new UpdateP79Clause2Response(newApprovalFileId?.Value, newWinnerFileId?.Value));
    }

    private async Task<FileId> GetDocumentTemplateForReplace(
        P79Clause2DocumentType documentType,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var templateCode = documentType == P79Clause2DocumentType.Approval
            ? P79Clause2TemplateConstant.ApprovalRequest60
            : P79Clause2TemplateConstant.WinnerAnnounce60;

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            dt =>
                dt.Group == DocumentTemplateGroups.P79Clause2 &&
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
        P79Clause2 entity,
        UpdateP79Clause2Request req,
        CancellationToken ct)
    {
        var isApprovalRequestDocumentReplace = req.IsApprovalRequestDocumentReplace ?? false;
        var isWinnerAnnounceDocumentReplace = req.IsWinnerAnnounceDocumentReplace ?? false;

        var mustSaveApprovalDocument =
            req.ApprovalRequestDocumentId.HasValue &&
            entity.Status != P79Clause2Status.WaitingApproval &&
            isApprovalRequestDocumentReplace;

        var mustSaveWinnerDocument =
            req.WinnerAnnounceDocumentId.HasValue &&
            entity.Status != P79Clause2Status.WaitingApproval &&
            isWinnerAnnounceDocumentReplace;

        FileId? newApprovalFileId = null;
        FileId? newWinnerFileId = null;

        if (mustSaveApprovalDocument)
        {
            newApprovalFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                P79Clause2DocumentType.Approval,
                FileId.From(req.ApprovalRequestDocumentId!.Value),
                isApprovalRequestDocumentReplace,
                ct);
        }

        if (mustSaveWinnerDocument)
        {
            newWinnerFileId = await this.UpdateDocumentHistoryAsync(
                entity,
                P79Clause2DocumentType.WinnerAnnouncement,
                FileId.From(req.WinnerAnnounceDocumentId!.Value),
                isWinnerAnnounceDocumentReplace,
                ct);
        }

        return (newApprovalFileId, newWinnerFileId);
    }

    private void ValidateDocument(UpdateP79Clause2Request req, P79Clause2 entity)
    {
        if (req is { ApprovalRequestDocumentId: not null, Status: P79Clause2Status.WaitingApproval } &&
            !entity.DocumentHistories.Any())
        {
            this.ThrowError("กรุณาจัดทำเอกสาร", StatusCodes.Status400BadRequest);
        }
    }

    private static void UpdateGlAccounts(P79Clause2 entity, UpdateP79Clause2Request req)
    {
        if (req.GlAccounts is null)
        {
            return;
        }

        entity.UpdateGLAccounts(req.GlAccounts.Map(rgl =>
            entity.GLAccounts
                  .FirstOrNone(vd => vd.Id == (rgl.Id != null ? P79Clause2GLAccountId.From(rgl.Id.Value) : null))
                  .Match(
                      Some: gl => gl.SetGLAccount(
                          rgl.Sequence,
                          rgl.SolId,
                          ParameterCode.From(rgl.BudgetTypeCode),
                          ParameterCode.From(rgl.GLAccountCode),
                          rgl.ProjectNumber,
                          rgl.Amount),
                      None: () => CreateGlAccountFromRequest(entity!, rgl))));
    }

    private static void UpdateVendor(P79Clause2 entity, UpdateP79Clause2Request req)
    {
        if (req.Vendors is null)
        {
            return;
        }

        entity.UpdateVendors(
            req.Vendors.Map(rvd =>
                entity.Vendors
                      .FirstOrNone(vd => vd.Id == (rvd.Id != null ? P79Clause2VendorId.From(rvd.Id.Value) : null))
                      .Match(
                          Some: vendor =>
                          {
                              vendor.SetSequence(rvd.Sequence)
                                    .SetVendorType(rvd.VendorType)
                                    .SetVendor(
                                        rvd.SuVendorId != null ? SuVendorId.From(rvd.SuVendorId.Value) : null,
                                        rvd.TaxNumber,
                                        rvd.VendorName,
                                        rvd.VendorBranchNumber)
                                    .SetBill(
                                        rvd.VatIncludeTypeCode != null ? ParameterCode.From(rvd.VatIncludeTypeCode) : null,
                                        ParameterCode.From(rvd.BillTypeCode),
                                        rvd.BillTypeOther,
                                        rvd.BillBookNo,
                                        rvd.BillDate,
                                        rvd.BillDetail);

                              if (rvd.VendorParcels.Any())
                              {
                                  CreateP79Clause2VendorParcelFromRequest(vendor, rvd.VendorParcels);
                              }

                              return vendor;
                          },
                          None: () => CreateP79Clause2VendorFromRequest(entity, rvd))));
    }

    private static void UpdateAdvance(P79Clause2 entity, UpdateP79Clause2Request req)
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

    private async Task RemoveFileInServiceAsync(P79Clause2 entity, UpdateP79Clause2Request req, CancellationToken ct)
    {
        var deleteFileIds = await this.ManageAttachments(entity, req.Attachments, ct);

        foreach (var fileId in deleteFileIds)
        {
            await this.fileServiceClient.DeleteAsync(fileId, CancellationToken.None);
        }
    }

    private static P79Clause2Vendor CreateP79Clause2VendorFromRequest(P79Clause2 data, VendorResponseDto req)
    {
        var vendor = P79Clause2Vendor.Create(data.Id)
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
            CreateP79Clause2VendorParcelFromRequest(vendor!, req.VendorParcels);
        }

        data.AddVendor(vendor);

        return vendor;
    }

    private static void CreateP79Clause2VendorParcelFromRequest(P79Clause2Vendor data, IEnumerable<VendorParcelResponseDto> reqParcels)
    {
        data.UpdateVendorsParcels(reqParcels.Map(rgl =>
            data.VendorParcels
                .FirstOrNone(vd => vd.Id == (rgl.Id != null ? P79Clause2VendorParcelId.From(rgl.Id.Value) : null))
                .Match(
                    Some: p => p.SetSequence(rgl.Sequence)
                                .SetItem(rgl.Item, rgl.ItemDetail)
                                .SetPrice(
                                    rgl.Quantity,
                                    ParameterCode.From(rgl.UnitCode),
                                    rgl.UnitPrice,
                                    rgl.TotalPrice,
                                    rgl.TotalPriceVat,
                                    rgl.VatIncludeTypeCode != null ? ParameterCode.From(rgl.VatIncludeTypeCode) : null),
                    None: () => CreateVendorParcelFromRequest(data!, rgl))));
    }

    private static P79Clause2VendorParcel CreateVendorParcelFromRequest(P79Clause2Vendor data, VendorParcelResponseDto reqParcel)
    {
        var parcel = P79Clause2VendorParcel.Create(data.Id)
                                           .SetSequence(reqParcel.Sequence)
                                           .SetItem(reqParcel.Item, reqParcel.ItemDetail)
                                           .SetPrice(
                                               reqParcel.Quantity,
                                               ParameterCode.From(reqParcel.UnitCode),
                                               reqParcel.UnitPrice,
                                               reqParcel.TotalPrice,
                                               reqParcel.TotalPriceVat,
                                               reqParcel.VatIncludeTypeCode != null ? ParameterCode.From(reqParcel.VatIncludeTypeCode) : null);

        data.AddVendorParcels(parcel);

        return parcel;
    }

    private static P79Clause2GLAccount CreateGlAccountFromRequest(P79Clause2 data, GLAccountResponseDto rgl)
    {
        var glAcc = P79Clause2GLAccount.Create(data.Id)
                                       .SetGLAccount(
                                           rgl.Sequence,
                                           rgl.SolId,
                                           ParameterCode.From(rgl.BudgetTypeCode),
                                           ParameterCode.From(rgl.GLAccountCode),
                                           rgl.ProjectNumber,
                                           rgl.Amount);
        data.AddGLAccount(glAcc);

        return glAcc;
    }

    private async Task ValidateRequestAsync(UpdateP79Clause2Request req, CancellationToken ct)
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

        var deleteIds = p79Clause2.Attachments
                                  .ExceptBy(
                                      fileList.Select(s => s.FileId),
                                      w => w.Id.Value)
                                  .Select(s => s.Id)
                                  .Map(r =>
                                  {
                                      p79Clause2.RemoveAttachmentById(r);

                                      return r;
                                  }) ?? [];

        _ = p79Clause2.Attachments.Join(
                          fileList,
                          en => en.Id.Value,
                          req => req.FileId,
                          (en, req) => new { en, req })
                      .Iter(s => s.en.SetIsPublic(s.req.IsPublic).SetDocumentType(ParameterCode.From(s.req.DocumentTypeCode)));

        _ = fileList
            .ExceptBy(
                p79Clause2.Attachments.Select(s => s.Id.Value),
                w => w.FileId)
            .Map(a => P79Clause2Attachments.Create(
                ParameterCode.From(a.DocumentTypeCode),
                FileId.From(a.FileId),
                a.FileName,
                a.Sequence,
                a.IsPublic))
            .Iter(a => p79Clause2.AddAttachment(a));

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