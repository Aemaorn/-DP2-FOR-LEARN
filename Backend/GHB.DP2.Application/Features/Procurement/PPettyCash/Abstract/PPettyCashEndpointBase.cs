namespace GHB.DP2.Application.Features.Procurement.PPettyCash.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract class PPettyCashEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;

    protected PPettyCashEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger,
        IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
    }

    protected async Task<PPettyCash> GetPettyCashById(PettyCashId id, CancellationToken ct)
    {
        var data =
            await this.dbContext.PPettyCashs
                      .AsNoTracking()
                      .Include(pw => pw.Vendors)
                      .ThenInclude(pPettyCashVendor => pPettyCashVendor.VendorParcels)
                      .Include(pPettyCash => pPettyCash.Categories)
                      .Include(pPettyCash => pPettyCash.Committees)
                      .Include(pPettyCash => pPettyCash.Assignees)
                      .Include(pPettyCash => pPettyCash.Attachments)
                      .Include(pw => pw.GLAccounts)
                      .Include(pw => pw.Acceptors)
                      .Include(auditableEntity => auditableEntity.AuditInfo)
                      .Include(pPettyCash => pPettyCash.DocumentHistories)
                      .Include(pPettyCash => pPettyCash.Department)
                      .Include(pPettyCash => pPettyCash.SupplyMethod)
                      .Include(pPettyCash => pPettyCash.SupplyMethodType)
                      .Include(pPettyCash => pPettyCash.SupplyMethodSpecialType)
                      .Include(pPettyCash => pPettyCash.DeliveryPeriodType)
                      .Include(pPettyCash => pPettyCash.DeliveryCondition)
                      .Include(pPettyCash => pPettyCash.Committees)
                          .ThenInclude(c => c.CommitteePositions)
                      .Include(pPettyCash => pPettyCash.Committees)
                          .ThenInclude(c => c.User)
                          .ThenInclude(u => u.Employee)
                          .ThenInclude(e => e.View)
                      .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (data is null)
        {
            this.ThrowError($"Pw119 with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return data;
    }

    /// <summary>
    /// Get PPettyCash entity WITH change tracking for update operations.
    /// Use this when you need to modify and save the entity.
    /// </summary>
    protected async Task<PPettyCash> GetPettyCashByIdForUpdateAsync(PettyCashId id, CancellationToken ct)
    {
        var data =
            await this.dbContext.PPettyCashs
                      .Include(pPettyCash => pPettyCash.DocumentHistories)
                      .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (data is null)
        {
            this.ThrowError($"PPettyCash with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return data;
    }

    protected async Task UpsertAttachments(PPettyCash entity, AttachmentsDtoWithId[] attachments)
    {
        var fileList = attachments
                       .SelectMany(r => r.FileAttachments.Select(f => new
                       {
                           f.Id,
                           r.DocumentTypeCode,
                           f.FileId,
                           f.FileName,
                           f.Sequence,
                           f.IsPublic,
                       }))
                       .ToArray();

        var incomingFileIds = fileList.Select(f => FileId.From(f.FileId)).ToHashSet();
        var existingFileIds = entity.Attachments.Select(a => a.Id).ToHashSet();

        var removedAttachments = entity.Attachments
                                       .Where(a => !incomingFileIds.Contains(a.Id))
                                       .ToArray();

        foreach (var attachment in removedAttachments)
        {
            entity.RemoveAttachment(attachment);
            await this.fileServiceClient.DeleteAsync(attachment.Id, CancellationToken.None);
        }

        if (removedAttachments.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.DeleteFile,
                ActivityLogActionTypeConstant.DeleteFile,
                nameof(entity.Status),
                string.Join(", ", removedAttachments.Select(a => a.FileName))));
        }

        var newFiles = fileList.Where(f => !existingFileIds.Contains(FileId.From(f.FileId))).ToArray();

        newFiles.Map(f => PPettyCashAttachments.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
                .Iter(r => entity.AddAttachment(r));

        if (newFiles.Length > 0)
        {
            entity.AddActivity(new ActivityInfo(
                ActivityLogActionTypeConstant.UploadFile,
                ActivityLogActionTypeConstant.UploadFile,
                nameof(entity.Status),
                string.Join(", ", newFiles.Select(f => f.FileName))));
        }

        foreach (var existing in entity.Attachments)
        {
            var match = fileList.FirstOrDefault(f => FileId.From(f.FileId) == existing.Id);

            if (match != null)
            {
                existing.SetIsPublic(match.IsPublic)
                        .SetSequence(match.Sequence)
                        .SetDocumentType(ParameterCode.From(match.DocumentTypeCode));
            }
        }
    }

    private async Task<FileId> GetDocumentTemplateByCriteria(
        string templateCode,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var fileId =
            await documentService.GetDocumentTemplateAsync(
                dt =>
                    dt.Group == DocumentTemplateGroups.PettyCash &&
                    dt.Code == templateCode &&
                    dt.IsActive,
                ct);

        return (FileId)fileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        PPettyCash pettyCash,
        CancellationToken ct)
    {
        var approvalRequestDocumentId =
            await this.GetDocumentTemplateByCriteria(
                PettyCashTemplateConstant.GetTemplateCode(pettyCash.IsFromJorPor001),
                ct);

        pettyCash.AddDocumentHistory(approvalRequestDocumentId);
    }

    /// <summary>
    /// Creates a new document history version with a copy of the file.
    /// </summary>
    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PPettyCash pettyCash,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = pettyCash.DocumentHistories
                                      .OrderVersions()
                                      .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            pettyCash.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.PettyCash}/{pettyCash.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        pettyCash.AddDocumentHistory(copiedFileId.Value, (bool)isReplace);

        var newHistory = pettyCash.DocumentHistories
            .OrderVersions()
            .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    private async Task<SuUser?> GetLastActivityCreatedByAsync(
        string key,
        string type,
        CancellationToken ct)
    {
        var lastActivity =
            await this.dbContext.SuActivityLogs
                      .Where(l =>
                          l.Key == key &&
                          l.ActivityInfo.Type == type)
                      .OrderByDescending(l => l.AuditInfo.CreatedAt)
                      .FirstOrDefaultAsync(cancellationToken: ct);

        if (lastActivity is null)
        {
            return null;
        }

        var createByUser =
            await this.dbContext.SuUsers
                      .Include(u => u.Employee)
                      .ThenInclude(e => e.View)
                      .FirstOrDefaultAsync(
                          u => u.Id == UserId.From(lastActivity.AuditInfo.CreatedBy),
                          ct);

        return createByUser;
    }

    protected async Task<GetPPettyCashReplaceDto> MapToReplaceDto(
        PPettyCash data,
        bool hasAcceptor,
        CancellationToken ct,
        UserId? userId)
    {
        var creatorReplace = await GetCreatorReplaceAsync();
        var acceptorsReplace = hasAcceptor ? GetAcceptorReplace(AcceptorType.DepartmentDirectorAgree) : [];

        var categories =
            data.Categories
                .Select(pw => new CategoriesDto(
                    pw.Id.Value,
                    pw.CategoryTypeCode.Value));

        var vendors =
            data.Vendors
                .OrderBy(pw => pw.Sequence)
                .Select(pw => new VendorReplace(
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
                    pw.BillDate.HasValue ? pw.BillDate.Value.ToThaiDateString(includeBuddhistEra: false) : string.Empty,
                    pw.BillDetail,
                    pw.VendorParcels
                      .OrderBy(pw => pw.Sequence)
                      .Select(vp => new VendorParcelReplace(
                          vp.Id.Value,
                          vp.Sequence,
                          vp.Item,
                          vp.ItemDetail,
                          vp.Quantity,
                          vp.UnitCode.Value,
                          vp.UnitPrice.ToCurrencyStringWithComma(),
                          vp.TotalPrice.ToCurrencyStringWithComma(),
                          vp.TotalPriceVat.ToCurrencyStringWithComma()))));

        var suParameters =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .Where(p =>
                          p.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.SolId) ||
                          p.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.GLAcc) ||
                          p.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.SolId))
                      .ToListAsync(ct);

        var glAccounts =
            data.GLAccounts
                .Select(pw => new GlAccountReplace(
                    pw.Id.Value,
                    pw.Sequence,
                    pw.SoId,
                    suParameters.FirstOrDefault(x => x.Code == pw.SoId)?.Label ?? string.Empty,
                    pw.BudgetTypeCode.Value,
                    suParameters.FirstOrDefault(x => x.Code == pw.BudgetTypeCode)?.Label ?? string.Empty,
                    pw.GLAccountCode.Value,
                    suParameters.FirstOrDefault(x => x.Code == pw.GLAccountCode)?.Label ?? string.Empty,
                    pw.ProjectNumber,
                    pw.Amount.ToCurrencyStringWithComma()));

        var procurementCommittees = data.Committees
                    .Where(c => c.GroupType == GroupType.ProcurementCommittee)
                    .Select(c => new CommitteeReplace(
                        c.Id.Value,
                        c.Sequence,
                        "เห็นชอบ",
                        string.Empty,
                        c.FullName,
                        c.User.Employee.View?.FullPositionName ?? string.Empty,
                        c.CommitteePositionsName,
                        c.CommitteePositions.Label,
                        string.Empty))
                    .ToArray();

        var acceptorInspectionCommittees = GetValue(
                hasAcceptor && data.Status == PettyCashStatus.WaitingForInspector,
                data.Committees
                    .Where(c => c.GroupType == GroupType.InspectionCommittee)
                    .Select(c => new CommitteeReplace(
                        c.Id.Value,
                        c.Sequence,
                        "เห็นชอบ",
                        string.Empty,
                        c.FullName,
                        c.User.Employee.View?.FullPositionName ?? string.Empty,
                        c.CommitteePositionsName,
                        c.CommitteePositions.Label,
                        string.Empty))
                    .ToArray(),
                null);

        var inspectionCommittees = data.Committees
                    .Where(c => c.GroupType == GroupType.InspectionCommittee)
                    .Select(c => new CommitteeReplace(
                        c.Id.Value,
                        c.Sequence,
                        "เห็นชอบ",
                        string.Empty,
                        c.FullName,
                        c.User.Employee.View?.FullPositionName ?? string.Empty,
                        c.CommitteePositionsName,
                        c.CommitteePositions.Label,
                        string.Empty))
                    .ToArray();

        var assignees = data.Assignees
                .OrderBy(a => a.Sequence)
                .Select(a => new AssigneeResponse(
                    a.Id.Value,
                    a.Group,
                    a.Type,
                    a.UserId.Value,
                    a.Sequence,
                    a.FullName,
                    a.PositionName,
                    a.BusinessUnitName,
                    a.Status))
                .ToArray();

        var attachments = GetAttachments();

        var positionName = data.Acceptors.Where(x => x.Type == AcceptorType.DepartmentDirectorAgree).FirstOrDefault()?.PositionName ?? string.Empty;

        var sectionApproveName = new List<SectionApprove>
        {
            new(positionName),
        };

        var acceptorDate =
            data.Status is not (PettyCashStatus.Draft or PettyCashStatus.Edit or PettyCashStatus.Rejected)
                ? data.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
                : null;

        var result = new GetPPettyCashReplaceDto(
            data.Id.Value,
            acceptorDate,
            string.Empty,
            sectionApproveName,
            data.PettyCashNumber.Value,
            data.Status,
            data.PettyCashDate.ToThaiDateString(includeBuddhistEra: false),
            data.Department.Value,
            data.Department.Name,
            data.BudgetYear,
            data.SupplyMethodCode.Value,
            data.SupplyMethod.Label,
            data.SupplyMethodTypeCode.Value,
            data.SupplyMethodType.Label,
            data.SupplyMethodSpecialTypeCode?.Value,
            data.SupplyMethodSpecialType.Label,
            data.Subject,
            data.Source,
            data.Reasons ?? string.Empty,
            data.DeliveryDate.HasValue ? data.DeliveryDate.Value.ToThaiDateString(includeBuddhistEra: false) : string.Empty,
            data.Budget.ToCurrencyStringWithComma(),
            data.Budget.ThaiBahtText(),
            data.DeliveryPeriod,
            data.DeliveryPeriodType?.Label,
            data.DeliveryCondition?.Label,
            data.DisbursementDate.HasValue ? data.DisbursementDate.Value.ToThaiDateString(includeBuddhistEra: false) : string.Empty,
            data.IsAdvance,
            new PPettyCashAdvanceReplace(
                data.AdvanceName,
                (string?)data.AdvancePaymentMethodCode,
                data.AdvancePaymentDate.HasValue ? data.AdvancePaymentDate.Value.ToThaiDateString(includeBuddhistEra: false) : string.Empty,
                (string?)data.AdvanceBankCode,
                data.AdvanceBankAccount,
                data.AdvanceBankBranch,
                data.AdvanceBankAccountName,
                data.AdvanceDetail),
            categories,
            vendors,
            glAccounts,
            procurementCommittees,
            inspectionCommittees,
            acceptorInspectionCommittees,
            acceptorsReplace,
            assignees,
            attachments,
            true,
            data.CashType.ToString(),
            creatorReplace);

        return result;

        AttachmentsDtoWithId[] GetAttachments()
        {
            return
                [.. data.Attachments
                    .GroupBy(
                        a => a.DocumentTypeCode,
                        (key, g) => new AttachmentsDtoWithId(
                            key.Value,
                            [.. g.Select(s => new FileAttachmentsWithId(
                                 s.Id.Value,
                                 s.Id.Value,
                                 s.FileName,
                                 s.Sequence,
                                 s.IsPublic,
                                 s.AuditInfo.CreatedBy))]))];
        }

        async Task<CreatorReplace?> GetCreatorReplaceAsync()
        {
            var sendToCommitteeApproveByUser =
                userId is not null
                    ? await this.dbContext.SuUsers
                                .Include(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .FirstOrDefaultAsync(u => u.Id == userId, ct)
                    : await this.GetLastActivityCreatedByAsync(
                        data.Id.ToString(),
                        ActivityLogActionTypeConstant.SendApprove,
                        ct);

            if (sendToCommitteeApproveByUser == null)
            {
                return null;
            }

            return new CreatorReplace(
                sendToCommitteeApproveByUser.Id.Value,
                "ผู้จัดทำ",
                sendToCommitteeApproveByUser.FullName,
                sendToCommitteeApproveByUser.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty);
        }

        AcceptorReplace[] GetAcceptorReplace(AcceptorType acceptorType)
        {
            var acceptorsSource =
                data.Acceptors.Where(a => a.Type == acceptorType)
                    .ToArray();

            if (!acceptorsSource.Any())
            {
                return [];
            }

            AcceptorReplace[] acceptors =
                [.. acceptorsSource
                    .Map(MapAcceptorReplace)
                    .OrderBy(a => a.Sequence)];

            if (acceptors.Any() && acceptorType == AcceptorType.Approver)
            {
                acceptors[^1] =
                    acceptors.Last() with { Action = "อนุมัติ" };
            }

            return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        AcceptorReplace MapAcceptorReplace(PPettyCashAcceptor acceptor)
        {
            return new AcceptorReplace(
                acceptor.UserId.Value,
                acceptor.Sequence,
                "เห็นชอบ",
                acceptor.User.FullName,
                acceptor.FullName,
                acceptor.User.Employee.View?.FullPositionName ?? string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                acceptor.Status);
        }
    }

    private static T GetValue<T>(bool condition, T valueIfTrue, T valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }
}