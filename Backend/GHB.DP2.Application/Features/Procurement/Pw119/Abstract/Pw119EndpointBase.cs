namespace GHB.DP2.Application.Features.Procurement.Pw119.Abstract;

using Codehard.FileService.Client.Abstractions;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class Pw119EndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;
    private readonly IFileServiceClient fileServiceClient;
    private readonly IOperationService operationService;

    protected Pw119EndpointBase(
        ILogger logger,
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.fileServiceClient = fileServiceClient;
        this.operationService = operationService;
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
                    dt.Group == DocumentTemplateGroups.Pw119 &&
                    dt.Code == templateCode &&
                    dt.IsActive,
                ct);

        return (FileId)fileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        Pw119 pw119,
        CancellationToken ct)
    {
        var approvalRequestDocumentId =
            await this.GetDocumentTemplateByCriteria(
                Pw119TemplateConstant.ApprovalRequest60,
                ct);

        var winnerAnnouncementDocumentId =
            await this.GetDocumentTemplateByCriteria(
                Pw119TemplateConstant.WinnerAnnounce60,
                ct);

        pw119.AddDocumentHistory(
            Pw119DocumentType.Approval,
            approvalRequestDocumentId);

        pw119.AddDocumentHistory(
            Pw119DocumentType.WinnerAnnouncement,
            winnerAnnouncementDocumentId);
    }

    /// <summary>
    /// Creates a new document history version with a copy of the file.
    /// The OLD version keeps the original fileId (snapshot of content before edit).
    /// The NEW version gets a copied file that user will continue editing.
    /// </summary>
    /// <param name="pw119">The Pw119 entity to add history to</param>
    /// <param name="documentType">The document type (Approval or WinnerAnnouncement)</param>
    /// <param name="fileId">The current file ID being edited</param>
    /// <param name="isReplace">Whether this is a replacement document</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The new fileId that user should continue editing, or null if no change</returns>
    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        Pw119 pw119,
        Pw119DocumentType documentType,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = pw119.DocumentHistories
                                 .Where(dh => dh.DocumentType == documentType)
                                 .OrderVersions()
                                 .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            pw119.Status.ToString());

        // Copy current file to create NEW version for continued editing
        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.Pw119}/{pw119.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        // Create NEW version pointing to copied file - user will continue editing this
        pw119.AddDocumentHistory(documentType, copiedFileId.Value, isReplace ?? false);

        var newHistory = pw119.DocumentHistories
                              .OrderVersions()
                              .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected async Task<Pw119> GetPw119ById(Pw119Id id, CancellationToken ct)
    {
        var data = await this.dbContext.Pw119s
                             .Include(pw => pw.Vendors)
                             .ThenInclude(pw119Vendor => pw119Vendor.VendorParcels)
                             .Include(pw119 => pw119.Attachments)
                             .Include(pw => pw.GLAccounts)
                             .ThenInclude(gl => gl.GLAccount)
                             .Include(pw => pw.GLAccounts)
                             .ThenInclude(gl => gl.BudgetType)
                             .Include(pw => pw.Acceptors)
                             .ThenInclude(pw => pw.User)
                             .ThenInclude(pw => pw.Employee)
                             .Include(pw119 => pw119.DocumentHistories)
                             .Include(pw119 => pw119.W119Categories)
                             .Include(pw119 => pw119.Department)
                             .Include(pw119 => pw119.SupplyMethod)
                             .Include(pw119 => pw119.SupplyMethodSpecialType)
                             .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (data is null)
        {
            this.ThrowError($"Pw119 with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return data;
    }

    protected GetPw119Response MapToResponse(Domain.Procurement.Pw119.Pw119 data)
    {
        var lastedApprovalRequestDocument =
            data.DocumentHistories
                .Where(d => d.DocumentType == Pw119DocumentType.Approval)
                .OrderVersions()
                .FirstOrDefault();

        var isReplacedApproval =
            data.DocumentHistories
                .Any(d => d.DocumentType == Pw119DocumentType.Approval && d.IsReplaced);

        var approvalRequestDocumentVersions =
            data.DocumentHistories
                .Where(d => d.DocumentType == Pw119DocumentType.Approval)
                .OrderVersions()
                .Select((d, index) => new DocumentVersionResponse(
                    d.FileId.Value,
                    d.Version,
                    d.CreatedAt,
                    d.CreatedByName ?? string.Empty,
                    index == 0))
                .ToArray();

        var lastedWinnerAnnouncementDocument =
            data.DocumentHistories
                .Where(d => d.DocumentType == Pw119DocumentType.WinnerAnnouncement)
                .OrderVersions()
                .FirstOrDefault();

        var isReplacedAWinner =
            data.DocumentHistories
                .Any(d => d.DocumentType == Pw119DocumentType.WinnerAnnouncement && d.IsReplaced);

        var winnerAnnounceDocumentVersions =
            data.DocumentHistories
                .Where(d => d.DocumentType == Pw119DocumentType.WinnerAnnouncement)
                .OrderVersions()
                .Select((d, index) => new DocumentVersionResponse(
                    d.FileId.Value,
                    d.Version,
                    d.CreatedAt,
                    d.CreatedByName ?? string.Empty,
                    index == 0))
                .ToArray();

        return new GetPw119Response(
            data.Id.Value,
            data.Pw119Number.Value,
            data.Status,
            lastedApprovalRequestDocument?.FileId.Value,
            false,
            approvalRequestDocumentVersions,
            lastedWinnerAnnouncementDocument?.FileId.Value,
            isReplacedAWinner,
            winnerAnnounceDocumentVersions,
            data.Pw119Date,
            data.DepartmentId.Value,
            data.Department?.OrganizationLevel,
            data.BudgetYear,
            data.SupplyMethodCode.Value,
            data.SupplyMethodSpecialTypeCode?.Value,
            data.AssignSegmentCode?.Value,
            data.Subject,
            data.Source,
            data.Budget,
            data.MedianPrice,
            data.W119CategoriesCode.Value,
            data.Reason,
            data.Telephone ?? string.Empty,
            new Pw119AdvanceResponseDto(
                data.IsAdvance,
                data.AdvanceName,
                (string?)data.AdvancePaymentMethodCode,
                data.AdvancePaymentDate,
                (string?)data.AdvanceBankCode,
                data.AdvanceBankAccount,
                data.AdvanceBankBranch,
                data.AdvanceBankAccountName,
                data.AdvanceDetail),
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
                      .OrderBy(x => x.Sequence)
                      .Select(vp => new VendorParcelResponseDto(
                          vp.Id.Value,
                          vp.Sequence,
                          vp.Item,
                          vp.ItemDetail,
                          vp.Quantity,
                          vp.UnitCode.Value,
                          vp.UnitPrice,
                          vp.TotalPrice,
                          vp.TotalPriceVat,
                          vp.VatIncludeTypeCode?.Value)))),
            data.GLAccounts
                .Select(pw => new GLAccountResponseDto(
                    pw.Id.Value,
                    pw.Sequence,
                    pw.SoId,
                    pw.BudgetTypeCode.Value,
                    pw.GLAccountCode.Value,
                    pw.ProjectNumber,
                    pw.Amount)),
            [
                .. data.Acceptors
                       .Where(a => !a.IsDeleted)
                       .OrderBy(a => a.Sequence)
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
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
                           IsCurrent: a.IsCurrent,
                           DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            [
                .. data.Attachments
                       .GroupBy(
                           a => a.DocumentTypeCode,
                           (key, g) => new AttachmentsDto(
                               key.Value,
                               [.. g.Select(s => new FileAttachments(s.Id.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))]))
            ],
            [
                .. data.Acceptors
                       .Where(a => !a.IsDeleted && a.Type == AcceptorType.AccountingConfirmer)
                       .OrderBy(a => a.Sequence)
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
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
                           IsCurrent: a.IsCurrent,
                           DelegateeUserId: a.Delegatee?.SuUserId.Value))
            ],
            data.DisbursementDate,
            data.DisbursementAmount,
            data.DisbursementDescription);
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

    protected async Task<Pw119ReplaceDto> MapToReplaceDtoAsync(
        Pw119 data,
        CancellationToken ct,
        bool hasAcceptor,
        UserId? creatorUserId)
    {
        var creatorReplace = await GetCreatorReplaceAsync();

        var advanceResponseReplace =
            new Pw119AdvanceResponseDto(
                data.IsAdvance,
                data.AdvanceName,
                (string?)data.AdvancePaymentMethodCode,
                data.AdvancePaymentDate,
                data.AdvanceBankCode?.Value,
                data.AdvanceBankAccount,
                data.AdvanceBankBranch,
                data.AdvanceBankAccountName,
                data.AdvanceDetail);

        var solIdParameters =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .Where(su => su.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.SolId))
                      .ToArrayAsync(ct);

        var vendorsReplace = MapVendorResponses(data.Vendors);
        var glAccountsReplace = MapGlAccounts(data.GLAccounts).ToArray();
        var acceptorsReplace = hasAcceptor ? GetAcceptorReplace() : [];
        var publisherReplace = GetPublisherReplace();
        var attachmentReplace =
            data.Attachments
                .GroupBy(
                    a => a.DocumentTypeCode,
                    (key, g) => new AttachmentsDto(
                        key.Value,
                        [.. g.Select(s => new FileAttachments(s.Id.Value, s.FileName, s.Sequence, s.IsPublic, s.AuditInfo.CreatedBy))])).ToArray();

        var parcelItemCount =
            data.Vendors
                .Sum(v => v.VendorParcels.Count);

        var acceptorDate =
            data.Status is not (Pw119Status.Draft or Pw119Status.Edit or Pw119Status.Rejected)
                ? data.Pw119Date.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
                : null;

        var sumTotalPriceVat = data.Vendors
                                   .Sum(e => e.VendorParcels.Sum(pd => pd.TotalPriceVat));

        IEnumerable<SectionApprove> sectionApproverPositionName = Enumerable.Empty<SectionApprove>();

        if (data.AssignSegmentCode.HasValue)
        {
            var userId = (string)data.AssignSegmentCode.Value switch
            {
                "AssignDept001" => await this.operationService.GetSegmentOtherManagerAsync(ct).Select(c => c?.UserId),
                "AssignDept002" => await this.operationService.GetSegmentITManagerAsync(ct).Select(c => c?.UserId),
                _ => null,
            };

            var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
                SectionProcessType.PurchaseOrder,
                (Guid)userId.Value,
                sumTotalPriceVat,
                data.SupplyMethodCode.Value,
                data.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? null : (string?)data.SupplyMethodSpecialTypeCode,
                ct,
                false);

            sectionApproverPositionName = managers.Select(m => new SectionApprove(m.PositionName));
        }
        else if (data.Department?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Branch ||
                 data.Department?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Zone ||
                 data.Department?.OrganizationLevel == EmployeeConstant.OrganizationLevel.Segment)
        {
            var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
                SectionProcessType.PurchaseOrder,
                UserId.From(data.AuditInfo.CreatedBy).Value,
                sumTotalPriceVat,
                data.SupplyMethodCode.Value,
                data.SupplyMethodCode.Value is SupplyMethodConstant.Eighty ? null : (string?)data.SupplyMethodSpecialTypeCode,
                ct,
                false);

            sectionApproverPositionName = managers.Select(m => new SectionApprove(m.PositionName));
        }

        var hasIncludeVat =
            data.Vendors
                .Any(v =>
                    v.VatIncludeTypeCode is { Value: VatTypeConstant.IncluedVat });

        var vatDescription =
            hasIncludeVat
                ? "รวมภาษีมูลค่าเพิ่ม"
                : "ไม่รวมภาษีมูลค่าเพิ่ม";

        var vendorNames =
            string.Join(
                ", ",
                data.Vendors.Select(v => v.VendorName));

        string glAccountCodes =
            string.Join(
                ", ",
                glAccountsReplace.Select(gl => gl.GLAccountCode));

        var w119Categorie = string.Format("ข้อ {0}", data.W119Categories != null ? data.W119Categories.Sequence : string.Empty);

        var result =
            new Pw119ReplaceDto(
                data.Id.Value,
                data.Pw119Number.Value,
                acceptorDate,
                sectionApproverPositionName,
                data.Telephone ?? string.Empty,
                data.Status,
                data.Pw119Date,
                data.DepartmentId.Value,
                data.Department?.Name ?? string.Empty,
                data.BudgetYear,
                data.SupplyMethodCode.Value,
                data.SupplyMethod.Label,
                data.SupplyMethodSpecialTypeCode?.Value,
                data.SupplyMethodSpecialType == null ? string.Empty : data.SupplyMethodSpecialType.Label,
                data.Subject,
                data.Source,
                data.Budget.ToCurrencyStringWithComma(),
                data.Budget.ThaiBahtText(),
                data.MedianPrice.HasValue ? data.MedianPrice.ToCurrencyStringWithComma() : string.Empty,
                data.MedianPrice.HasValue ? data.MedianPrice.ThaiBahtText() : string.Empty,
                data.W119CategoriesCode.Value,
                data.W119Categories != null ? data.W119Categories.Label : string.Empty,
                data.Reason,
                advanceResponseReplace,
                vendorsReplace,
                glAccountsReplace,
                acceptorsReplace,
                attachmentReplace,
                creatorReplace,
                publisherReplace,
                parcelItemCount.ToString(),
                vatDescription,
                vendorNames,
                glAccountCodes,
                w119Categorie);

        return result;

        async Task<CreatorReplace?> GetCreatorReplaceAsync()
        {
            if (data.Status is
                Pw119Status.Draft or
                Pw119Status.Edit or
                Pw119Status.Rejected)
            {
                return null;
            }

            var sendToCommitteeApproveByUser =
                creatorUserId is not null
                    ? await this.dbContext.SuUsers
                                .Include(suUser => suUser.Employee)
                                .ThenInclude(rawEmployee => rawEmployee.View)
                                .FirstOrDefaultAsync(u => u.Id == creatorUserId, ct)
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

        PublisherDto? GetPublisherReplace()
        {
            var approverList =
                data.Acceptors
                    .Where(a => a.Type == AcceptorType.Approver)
                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                    .ToArray();

            if (!approverList.Any())
            {
                return null;
            }

            var approverApproveAll =
                approverList.All(a => a.Status == AcceptorStatus.Approved);

            if (!approverApproveAll)
            {
                return null;
            }

            var publisherUser =
                approverList.MaxBy(a => a.Sequence)!;

            return
                new PublisherDto(
                    publisherUser.Delegatee != null ? publisherUser.SignatureDelegatee : publisherUser.Signature,
                    publisherUser.FullName,
                    publisherUser.PositionName,
                    string.Empty,
                    string.Empty,
                    DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false));
        }

        IEnumerable<VendorReplace> MapVendorResponses(IEnumerable<Pw119Vendor> vendors)
        {
            return vendors
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
                       pw.BillType.Label,
                       pw.BillTypeOther,
                       pw.BillBookNo ?? string.Empty,
                       pw.BillDate.ToThaiDateString(),
                       pw.BillDetail,
                       pw.VendorParcels
                         .OrderBy(x => x.Sequence)
                         .Select(vp =>
                             new VendorParcelReplace(
                                 vp.Id.Value,
                                 vp.Sequence,
                                 vp.Item,
                                 vp.ItemDetail,
                                 vp.Quantity,
                                 vp.UnitCode.Value,
                                 vp.Unit.Label,
                                 vp.UnitPrice.ToCurrencyStringWithComma(),
                                 vp.UnitPrice.ThaiBahtText(),
                                 vp.TotalPrice.ToCurrencyStringWithComma(),
                                 vp.TotalPrice.ThaiBahtText(),
                                 vp.TotalPriceVat.ToCurrencyStringWithComma(),
                                 vp.TotalPriceVat.ThaiBahtText()))));
        }

        IEnumerable<GLAccountReplace> MapGlAccounts(IEnumerable<Pw119GLAccount> glAccounts)
        {
            return glAccounts.Select(pw =>
            {
                var (solCode, solLabel) = ParseLabel(
                    solIdParameters
                        .FirstOrDefault(p => p.Code == ParameterCode.From(pw.SoId))?.Label);

                var (glAccountCode, glAccountLabel) = ParseLabel(pw.GLAccount.Label);

                return new GLAccountReplace(
                    pw.Id.Value,
                    pw.Sequence,
                    pw.SoId,
                    solCode,
                    solLabel,
                    pw.BudgetTypeCode.Value,
                    pw.BudgetType.Label,
                    glAccountCode,
                    glAccountLabel,
                    pw.ProjectNumber,
                    pw.Amount.ToCurrencyStringWithComma(),
                    pw.Amount.ThaiBahtText());
            });
        }

        AcceptorReplace[] GetAcceptorReplace()
        {
            AcceptorReplace[] acceptors =
            [
                .. data.Acceptors
                       .Where(a => a.Type == AcceptorType.Approver)
                       .Select(DelegatorExtensions.DelegatorToAcceptor)
                       .Map(MapAcceptorReplace)
                       .OrderBy(a => a.Sequence)
            ];

            if (acceptors.Any())
            {
                acceptors[^1] =
                    acceptors.Last() with { Action = "อนุมัติ" };
            }

            return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
        }

        AcceptorReplace MapAcceptorReplace(Pw119Acceptor acceptor)
        {
            return new AcceptorReplace(
                acceptor.UserId.Value,
                acceptor.Sequence,
                "เห็นชอบ",
                acceptor.Signature,
                acceptor.FullName,
                acceptor.PositionName ?? string.Empty,
                string.Empty,
                string.Empty,
                acceptor.Status);
        }
    }

    private static (string Code, string Label) ParseLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return (string.Empty, string.Empty);
        }

        var parts = label.Split(":");
        var code = parts[0].Trim();
        var labelText = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        return (code, labelText);
    }

    protected static async Task SendNotificationAsync(Pw119 entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(userId, title, message, NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.W119.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    protected async Task UpsertAcceptors(Pw119 data, AcceptorRequest[]? acceptors, UserId? sendToAcceptorId = null)
    {
        if (acceptors is null)
        {
            return;
        }

        var userIds =
            acceptors
                .Map(a => a.UserId)
                .Map(UserId.From)
                .ToArray();

        var users =
            await this.dbContext.SuUsers
                      .Where(u => userIds.Contains(u.Id))
                      .ToArrayAsync(CancellationToken.None);

        var userExists
            = userIds.Except(users.Map(u => u.Id)).ToArray();

        if (userExists.Length > 0)
        {
            this.ThrowError(
                $"User with ID {string.Join(", ", userExists)} not found.",
                StatusCodes.Status404NotFound);
        }

        _ = data.Acceptors.ExceptBy(
                    acceptors.Where(c => c.Id.HasValue)
                             .Select(c => c.Id.Value),
                    a => a.Id.Value)
                .Map(data.RemoveAcceptor)
                .ToHashSet();

        _ = acceptors.Where(r => !r.Id.HasValue).Join(
            users,
            a => a.UserId,
            u => u.Id.Value,
            (a, u) =>
            {
                var acceptor =
                    Pw119Acceptor.Create(
                        a.AcceptorType,
                        u,
                        a.Sequence);

                var status = a.AcceptorType switch
                {
                    AcceptorType.Approver => AcceptorStatus.Draft,
                    AcceptorType.AccountingApprover => AcceptorStatus.Pending,
                    AcceptorType.AccountingOperator => AcceptorStatus.Pending,
                    AcceptorType.AccountingConfirmer => AcceptorStatus.Draft,
                    _ => throw new ArgumentOutOfRangeException(nameof(a.AcceptorType), "Invalid acceptor type."),
                };

                acceptor.SetStatus(status);
                acceptor.SetSendToAcceptorId(sendToAcceptorId);

                data.AddAcceptor(acceptor);

                return acceptor;
            }).ToHashSet();

        _ = data.Acceptors
                .Join(
                    acceptors.Where(r => r.Id.HasValue),
                    domainAcceptor => domainAcceptor.Id.Value,
                    request => request.Id.Value,
                    (domainAcceptor, request) =>
                    {
                        domainAcceptor.SetSequence(request.Sequence);
                        domainAcceptor.SetSendToAcceptorId(sendToAcceptorId);

                        return domainAcceptor;
                    })
                .ToHashSet();

        if (data.Status == Pw119Status.WaitingAccountingApproval)
        {
            var accountingApprovers = data.Acceptors
                                          .Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                                          .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                          .ThenBy(a => a.Sequence)
                                          .ToList();

            if (accountingApprovers.Any())
            {
                foreach (var approver in data.Acceptors.Where(a => !a.IsDeleted))
                {
                    approver.SetCurrent(false);
                }

                var firstPending = accountingApprovers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

                if (firstPending != null)
                {
                    var firstSeq = firstPending.Sequence;

                    foreach (var a in accountingApprovers.Where(a => a.Sequence == firstSeq && a.Status == AcceptorStatus.Pending))
                    {
                        a.SetCurrent(true);
                    }
                }
            }
        }
    }

    protected async Task UpsertAttachments(Pw119 entity, AttachmentsDtoWithId[] attachments)
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

        newFiles.Map(f => Pw119Attachments.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
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
}