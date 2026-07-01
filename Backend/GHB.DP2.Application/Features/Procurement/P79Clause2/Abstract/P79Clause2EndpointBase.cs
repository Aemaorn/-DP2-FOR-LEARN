namespace GHB.DP2.Application.Features.Procurement.P79Clause2.Abstract;

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
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

public abstract class P79Clause2EndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;
    private readonly IOperationService operationService;
    private readonly IFileServiceClient fileServiceClient;

    protected P79Clause2EndpointBase(ILogger logger, Dp2DbContext dbContext, IOperationService operationService, IFileServiceClient fileServiceClient)
        : base(logger)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
        this.fileServiceClient = fileServiceClient;
    }

    protected async Task<P79Clause2> GetP79Clause2ById(P79Clause2Id id, CancellationToken ct)
    {
        var p79Clause2 = await this.dbContext.P79Clause2s
                                   .Include(p => p.Vendors)
                                   .ThenInclude(v => v.VendorParcels)
                                   .ThenInclude(vp => vp.Unit)
                                   .Include(p => p.Vendors)
                                   .ThenInclude(v => v.BillType)
                                   .Include(p => p.Vendors)
                                   .ThenInclude(v => v.VatIncludeType)
                                   .Include(p => p.GLAccounts)
                                   .ThenInclude(gl => gl.GLAccount)
                                   .Include(p => p.GLAccounts)
                                   .ThenInclude(gl => gl.BudgetType)
                                   .Include(p => p.Acceptors)
                                   .ThenInclude(a => a.User)
                                   .ThenInclude(u => u.Employee)
                                   .Include(p => p.Attachments)
                                   .Include(p => p.DocumentHistories)
                                   .Include(p => p.Department)
                                   .Include(p => p.SupplyMethod)
                                   .Include(p => p.SupplyMethodType)
                                   .Include(p => p.SupplyMethodSpecialType)
                                   .Include(p => p.AdvancePaymentMethod)
                                   .Include(p => p.AdvanceBank)
                                   .AsSplitQuery()
                                   .SingleOrDefaultAsync(p => p.Id == id, ct);

        if (p79Clause2 is null)
        {
            this.ThrowError($"P79Clause2 with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return p79Clause2;
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
                    dt.Group == DocumentTemplateGroups.P79Clause2 &&
                    dt.Code == templateCode &&
                    dt.IsActive,
                ct);

        return (FileId)fileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        P79Clause2 p79Clause2,
        CancellationToken ct)
    {
        var approvalRequestDocumentId =
            await this.GetDocumentTemplateByCriteria(
                p79Clause2.SupplyMethodCode == SupplyMethodConstant.Sixty ? P79Clause2TemplateConstant.ApprovalRequest60 : P79Clause2TemplateConstant.ApprovalRequest80,
                ct);

        var winnerAnnouncementDocumentId =
            await this.GetDocumentTemplateByCriteria(
                P79Clause2TemplateConstant.WinnerAnnounce60,
                ct);

        p79Clause2.AddDocumentHistory(
            P79Clause2DocumentType.Approval,
            approvalRequestDocumentId);

        p79Clause2.AddDocumentHistory(
            P79Clause2DocumentType.WinnerAnnouncement,
            winnerAnnouncementDocumentId);
    }

    /// <summary>
    /// Creates a new document history version with a copy of the file.
    /// </summary>
    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        P79Clause2 p79Clause2,
        P79Clause2DocumentType documentType,
        FileId fileId,
        bool? isReplace = false,
        CancellationToken ct = default)
    {
        var latestHistory = p79Clause2.DocumentHistories
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
            p79Clause2.Status.ToString());

        var documentService = this.Resolve<IDocumentService>();
        var copiedFileId = await documentService.CopyDocumentTemplateAsync(
            fileId,
            parentDirectory: $"{DocumentTemplateGroups.P79Clause2}/{p79Clause2.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
            cancellationToken: ct);

        if (!copiedFileId.HasValue)
        {
            return null;
        }

        p79Clause2.AddDocumentHistory(documentType, copiedFileId.Value, isReplace ?? false);

        var newHistory = p79Clause2.DocumentHistories
                                   .OrderVersions()
                                   .First(h => h.FileId == copiedFileId.Value);
        newHistory.SetVersion(newVersion);

        return copiedFileId.Value;
    }

    protected GetP79Clause2Response MapToResponse(Domain.Procurement.P79Clause2.P79Clause2 data)
    {
        var lastedApprovalRequestDocument =
            data.DocumentHistories
                .Where(d => d.DocumentType == P79Clause2DocumentType.Approval)
                .OrderVersions()
                .FirstOrDefault();

        var approvalRequestDocumentVersions =
            data.DocumentHistories
                .Where(d => d.DocumentType == P79Clause2DocumentType.Approval)
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
                .Where(d => d.DocumentType == P79Clause2DocumentType.WinnerAnnouncement)
                .OrderVersions()
                .FirstOrDefault();

        var isReplacedWinner =
            data.DocumentHistories
                .Any(d => d.DocumentType == P79Clause2DocumentType.WinnerAnnouncement && d.IsReplaced);

        var winnerAnnounceDocumentVersions =
            data.DocumentHistories
                .Where(d => d.DocumentType == P79Clause2DocumentType.WinnerAnnouncement)
                .OrderVersions()
                .Select((d, index) => new DocumentVersionResponse(
                    d.FileId.Value,
                    d.Version,
                    d.CreatedAt,
                    d.CreatedByName ?? string.Empty,
                    index == 0))
                .ToArray();

        return new GetP79Clause2Response(
            data.Id.Value,
            data.P79Clause2Number.Value,
            data.Status,
            lastedApprovalRequestDocument?.FileId.Value,
            false,
            approvalRequestDocumentVersions,
            lastedWinnerAnnouncementDocument?.FileId.Value,
            isReplacedWinner,
            winnerAnnounceDocumentVersions,
            data.P79Clause2Date,
            data.DepartmentId.Value,
            data.Department?.OrganizationLevel,
            data.BudgetYear,
            data.SupplyMethodCode.Value,
            data.SupplyMethodTypeCode.Value,
            data.SupplyMethodSpecialTypeCode?.Value,
            data.AssignSegmentCode?.Value,
            data.Subject,
            data.Telephone ?? string.Empty,
            data.Source,
            data.Budget,
            data.MedianPrice,
            data.ReasonItem1,
            data.ReasonItem2,
            data.ReasonItem3,
            data.IsAdvance,
            new P79Clause2AdvanceResponseDto(
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
                    pw.BillBookNo,
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
                .OrderBy(o => o.Sequence)
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
            data.AuditInfo.CreatedBy,
            data.DeliveryDate,
            data.ProcurementReasonItem1,
            data.ProcurementReasonItem2,
            data.DisbursementDate,
            data.DisbursementAmount,
            data.DisbursementDescription,
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
            ]);
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

    protected async Task<GetP79Clause2ReplaceDto> MapToReplaceDto(
        P79Clause2 data,
        CancellationToken ct,
        bool hasAcceptor,
        UserId? creatorUserId)
    {
        var creatorReplace = await this.GetCreatorReplaceAsync(data, creatorUserId, ct);
        var publisherReplace = P79Clause2EndpointBase<TRequest, TResponse>.GetPublisherReplace(data);
        var acceptorsReplace = hasAcceptor ? this.GetAcceptorReplace(data) : [];

        var advancePaymentDate =
            data.AdvancePaymentDate == null
                ? string.Empty
                : data.AdvancePaymentDate.ToThaiDateString(includeBuddhistEra: false);

        var advanceResponse = new P79Clause2AdvanceReplaceDto(
            data.AdvanceName ?? string.Empty,
            data.AdvancePaymentMethod != null ? data.AdvancePaymentMethod.Code.Value : string.Empty,
            data.AdvancePaymentMethod != null ? data.AdvancePaymentMethod.Label : string.Empty,
            advancePaymentDate,
            (string?)data.AdvanceBank?.Code ?? string.Empty,
            data.AdvanceBank != null ? data.AdvanceBank.Label : string.Empty,
            data.AdvanceBankAccount ?? string.Empty,
            data.AdvanceBankBranch ?? string.Empty,
            data.AdvanceBankAccountName ?? string.Empty,
            data.AdvanceDetail ?? string.Empty);

        var vendors = P79Clause2EndpointBase<TRequest, TResponse>.GetVendors(data);
        var glAccounts = P79Clause2EndpointBase<TRequest, TResponse>.GetGlAccounts(data);
        var attachments = P79Clause2EndpointBase<TRequest, TResponse>.GetAttachments(data);

        var vendorParcels = vendors
                            .Where(v => v.VendorParcels != null)
                            .SelectMany(v => v.VendorParcels)
                            .ToList();

        var sumTotalPriceVat = data.Vendors
                                   .Sum(e => e.VendorParcels.Sum(pd => pd.TotalPriceVat));

        IEnumerable<SectionApprove> sectionApprover = Enumerable.Empty<SectionApprove>();

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

            sectionApprover = managers.Select(m => new SectionApprove(m.PositionName));
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

            sectionApprover = managers.Select(m => new SectionApprove(m.PositionName));
        }

        var hasIncludeVat =
            data.Vendors
                .Any(v =>
                    v.VatIncludeTypeCode is { Value: VatTypeConstant.IncluedVat });

        var commandText = "...........................................";

        var vendorNames =
            string.Join(
                ", ",
                data.Vendors.Select(v => v.VendorName));

        string glAccountCodes =
            string.Join(
                ", ",
                data.GLAccounts.Select(gl => gl.GLAccount));

        var totalAmount = data.GLAccounts.Sum(x => x.Amount);

        var suParameters =
            await this.dbContext.SuParameters
                      .AsNoTracking()
                      .Where(p =>
                          p.GroupCode == GroupCode.From(SuParameterGroupCodeConstant.SolId))
                      .ToListAsync(ct);

        var items =
            data.GLAccounts?
                .Select(dep =>
                {
                    var param = suParameters.FirstOrDefault(p => p.Code == dep.SoId);

                    if (param == null)
                    {
                        return null;
                    }

                    var soIdParts = param.Label.Split(':', 2);

                    if (soIdParts.Length < 2)
                    {
                        return null;
                    }

                    var soIdName = soIdParts[1].Trim();

                    var glParts = dep.GLAccount.Label.Split(':', 2);

                    if (glParts.Length < 2)
                    {
                        return null;
                    }

                    var glCode = glParts[0].Trim();
                    var glName = glParts[1].Trim();

                    return $"{soIdName} {glName} รหัส {glCode} จำนวนเงิน {dep.Amount.ToCurrencyStringWithComma()} บาท ({dep.Amount.ThaiBahtText()})";
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList()
            ?? new List<string>();

        string glAccountText = items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            _ => string.Concat(
                string.Join(", ", items.Take(items.Count - 1)),
                " และ ",
                items.Last()),
        };

        var acceptorDate =
            data.Status is not (P79Clause2Status.Draft or P79Clause2Status.Edit or P79Clause2Status.Rejected)
                ? data.DocumentDate?.ToThaiDateString(includeBuddhistEra: false) ?? DateTimeOffset.Now.ToThaiDateString(includeBuddhistEra: false)
                : null;

        var vatDescription =
            hasIncludeVat
                ? "รวมภาษีมูลค่าเพิ่ม"
                : "ไม่รวมภาษีมูลค่าเพิ่ม";

        var result =
            new GetP79Clause2ReplaceDto(
                data.Id.Value,
                data.P79Clause2Number.Value,
                sectionApprover,
                data.Status,
                data.P79Clause2Date.ToThaiDateString(includeBuddhistEra: false),
                data.DepartmentId.Value,
                data.Department?.Name ?? string.Empty,
                data.BudgetYear,
                data.SupplyMethodCode.Value,
                data.SupplyMethod.Label,
                data.SupplyMethodTypeCode.Value,
                data.SupplyMethodType.Label,
                data.SupplyMethodSpecialTypeCode?.Value,
                data.SupplyMethodSpecialType?.Label ?? string.Empty,
                data.Subject,
                data.Telephone ?? string.Empty,
                data.Source,
                data.Budget.ToCurrencyStringWithComma(),
                data.Budget.ThaiBahtText(),
                data.MedianPrice is > 0 ? data.MedianPrice.ToCurrencyStringWithComma() : string.Empty,
                data.MedianPrice is > 0 ? data.MedianPrice.ThaiBahtText() : string.Empty,
                data.ReasonItem1 ?? string.Empty,
                data.ReasonItem2 ?? string.Empty,
                data.ReasonItem3 ?? string.Empty,
                data.IsAdvance,
                advanceResponse,
                vendors,
                glAccounts,
                acceptorsReplace,
                attachments,
                creatorReplace,
                publisherReplace,
                vatDescription,
                vendorNames,
                commandText,
                glAccountCodes,
                acceptorDate,
                totalAmount.ToCurrencyStringWithComma(),
                totalAmount.ThaiBahtText(),
                glAccountText,
                data.DeliveryDate.ToThaiDateString(),
                data.ProcurementReasonItem1,
                data.ProcurementReasonItem2,
                vendorParcels);

        return result;
    }

    private static IEnumerable<VendorReplace> GetVendors(P79Clause2 data) =>
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
                (string?)pw.VatIncludeTypeCode,
                pw.VatIncludeType?.Label ?? string.Empty,
                pw.BillTypeCode.Value,
                pw.BillType.Label,
                pw.BillTypeOther,
                pw.BillBookNo ?? string.Empty,
                pw.BillDate.ToThaiDateString(),
                pw.BillDetail,
                pw.VendorParcels
                  .OrderBy(x => x.Sequence)
                  .Select(vp => new VendorParcelReplace(
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

    private static IEnumerable<GLAccountReplace> GetGlAccounts(P79Clause2 data) =>
        data.GLAccounts
            .Select(pw => new GLAccountReplace(
                pw.Id.Value,
                pw.Sequence,
                pw.SoId,
                pw.BudgetTypeCode.Value,
                pw.BudgetType.Label,
                pw.GLAccountCode.Value,
                pw.GLAccount.Label,
                pw.ProjectNumber,
                pw.Amount.ToCurrencyStringWithComma(),
                pw.Amount.ThaiBahtText()));

    private static AttachmentsDto[] GetAttachments(P79Clause2 data) =>
    [
        .. data.Attachments
               .GroupBy(
                   a => a.DocumentTypeCode,
                   (key, g) => new AttachmentsDto(
                       key.Value,
                       [
                           .. g.Select(s => new FileAttachments(
                               s.Id.Value,
                               s.FileName,
                               s.Sequence,
                               s.IsPublic,
                               s.AuditInfo.CreatedBy))
                       ]))
    ];

    private async Task<CreatorReplace?> GetCreatorReplaceAsync(P79Clause2 data, UserId? creatorUserId, CancellationToken ct)
    {
        if (data.Status is
            P79Clause2Status.Draft or
            P79Clause2Status.Edit or
            P79Clause2Status.Rejected)
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

    private static PublisherDto? GetPublisherReplace(P79Clause2 data)
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

    private AcceptorReplace[] GetAcceptorReplace(P79Clause2 data)
    {
        AcceptorReplace[] acceptors =
        [
            .. data.Acceptors
                   .Where(a => a.Type == AcceptorType.Approver)
                   .Map(DelegatorExtensions.DelegatorToAcceptor)
                   .Map(this.MapAcceptorReplace)
                   .OrderBy(a => a.Sequence)
        ];

        if (acceptors.Any())
        {
            acceptors[^1] =
                acceptors.Last() with { Action = "อนุมัติ" };
        }

        return [.. acceptors.Where(a => a.Status == AcceptorStatus.Approved)];
    }

    private AcceptorReplace MapAcceptorReplace(P79Clause2Acceptor acceptor)
    {
        return new AcceptorReplace(
            acceptor.UserId.Value,
            acceptor.Sequence,
            "เห็นชอบ",
            acceptor.User?.FullName,
            acceptor.FullName,
            acceptor.User?.Employee.View?.FullPositionName ?? string.Empty,
            string.Empty,
            string.Empty,
            acceptor.Status);
    }

    protected async Task UpsertAcceptors(P79Clause2 entity, AcceptorRequest[]? acceptors, UserId? sendToAcceptorId = null)
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
                $"ไม่พบผู้ใช้งานในระบบ",
                StatusCodes.Status404NotFound);
        }

        _ = entity.Acceptors.ExceptBy(
                      acceptors.Where(c => c.Id.HasValue)
                               .Select(c => c.Id.Value),
                      a => a.Id.Value)
                  .Map(entity.RemoveAcceptor)
                  .ToHashSet();

        _ =
            acceptors.Where(r => !r.Id.HasValue).Join(
                         users,
                         a => a.UserId,
                         u => u.Id.Value,
                         (a, u) =>
                         {
                             var acceptor =
                                 P79Clause2Acceptor.Create(
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

                             entity.AddAcceptor(acceptor);

                             return acceptor;
                         })
                     .ToHashSet();

        _ = entity.Acceptors
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

        if (entity.Status == P79Clause2Status.WaitingAccountingApproval)
        {
            var accountingApprovers = entity.Acceptors
                                            .Where(a => !a.IsDeleted && (a.Type == AcceptorType.AccountingApprover || a.Type == AcceptorType.AccountingOperator))
                                            .OrderBy(a => a.Type == AcceptorType.AccountingOperator ? 0 : 1)
                                            .ThenBy(a => a.Sequence)
                                            .ToList();

            if (accountingApprovers.Any())
            {
                foreach (var approver in entity.Acceptors.Where(a => !a.IsDeleted))
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

    protected async Task UpsertAttachments(P79Clause2 entity, AttachmentsDtoWithId[] attachments)
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

        newFiles.Map(f => P79Clause2Attachments.Create(ParameterCode.From(f.DocumentTypeCode), FileId.From(f.FileId), f.FileName, f.Sequence, f.IsPublic))
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

    // แจ้งผู้จัดทำให้ทราบเมื่อมีการส่งอนุมัติ
    private async Task SendNotificationSendApproveAsync(P79Clause2 entity)
    {
        var acceptors = entity.Acceptors.Where(a => a.Type == AcceptorType.Approver);

        foreach (var targetUserId in acceptors.SelectMany(a => a.GetNotificationTargets()))
        {
            await BaseSendNotificationAsync(
                entity,
                targetUserId,
                NotificationConstant.WaitForLike.Title,
                string.Format(NotificationConstant.WaitForLike.Message, ProgramConstant.Urgent79Clause2.Name, entity.P79Clause2Number));
        }
    }

    // แจ้งผู้จัดทำทราบเมื่อมีการส่งแก้ไขจากฝ่ายบัญชี
    private async Task SendNotificationRejectedAsync(P79Clause2 entity)
    {
        var acceptors = entity.Acceptors.Where(a => a.Type == AcceptorType.AccountingApprover);

        foreach (var targetUserId in acceptors.SelectMany(a => a.GetNotificationTargets()))
        {
            await BaseSendNotificationAsync(
                entity,
                targetUserId,
                NotificationConstant.ReturnToCreator.Title,
                string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.Urgent79Clause2.Name, entity.P79Clause2Number));
        }
    }

    protected async Task SendNotificationAsync(P79Clause2 entity)
    {
        var notifyTask = entity.Status switch
        {
            P79Clause2Status.WaitingApproval => this.SendNotificationSendApproveAsync(entity),
            P79Clause2Status.Rejected => this.SendNotificationRejectedAsync(entity),
            _ => Task.CompletedTask,
        };

        await notifyTask;
    }

    protected static async Task SendNotificationAsync(P79Clause2 entity, UserId userId, string title, string message)
    {
        await BaseSendNotificationAsync(entity, userId, title, message);
    }

    private static async Task BaseSendNotificationAsync(P79Clause2 entity, UserId receiverId, string title, string message)
    {
        await Notification
              .Crate(
                  receiverId,
                  title,
                  message,
                  NotificationProgram.Procurement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.Urgent79Clause2.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}