namespace GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Features.ContractAgreement.ContractDraft;
using GHB.DP2.Application.Features.ContractManagement.ContractDraftVendorEdit.Dto;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CaContractDraftVendorEdit;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public abstract partial class ContractDraftVendorEditEndpoint<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    private readonly Dp2DbContext dbContext;

    protected ContractDraftVendorEditEndpoint(
        ILogger logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<CaContractDraftVendorEdit> GetEditByIdAsync(ContractDraftVendorEditId id, CancellationToken ct = default)
    {
        var entity = await this.dbContext.CaContractDraftVendorEdits
                               .Include(e => e.Assignees)
                               .ThenInclude(a => a.Delegatee)
                               .Include(e => e.Components)
                               .Include(e => e.Acceptors)
                               .ThenInclude(a => a.Delegatee)
                               .Include(e => e.Acceptors)
                               .ThenInclude(a => a.CommitteePosition)
                               .Include(e => e.PaymentTerms)
                               .Include(e => e.Attachments)
                               .ThenInclude(a => a.Files)
                               .Include(e => e.Shareholders)
                               .ThenInclude(s => s.VendorShareholderCheckers)
                               .Include(e => e.Checkers)
                               .Include(e => e.CheckerAttachment)
                               .Include(e => e.DocumentHistories)
                               .Include(e => e.DraftTermsConditions)
                               .Include(e => e.DraftEquipmentRental)
                               .Include(e => e.ContractType)
                               .Include(e => e.Template)
                               .Include(e => e.SubTemplate)
                               .AsSplitQuery()
                               .SingleOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)
        {
            this.ThrowError($"ไม่พบข้อมูลแก้ไขร่างสัญญารหัส {id}", StatusCodes.Status404NotFound);
        }

        return entity;
    }

    protected async Task UpsertAcceptors(
        CaContractDraftVendorEdit entity,
        AcceptorRequest[] requests,
        CancellationToken ct,
        UserId? sendToAcceptorId = null)
    {
        var canEditApprovers = entity.Status is
            ContractDraftVendorEditStatus.WaitingAssignment
            or ContractDraftVendorEditStatus.WaitingComment
            or ContractDraftVendorEditStatus.RejectedToAssignee;

        var effectiveRequests = canEditApprovers
            ? requests
            : requests.Where(r => r.AcceptorType != AcceptorType.Approver).ToArray();

        var requestIds = effectiveRequests.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();

        entity.Acceptors
              .Where(a => !requestIds.Contains(a.Id.Value)
                          && (canEditApprovers || a.Type != AcceptorType.Approver))
              .ToList()
              .ForEach(a => entity.RemoveAcceptor(a));

        var userIds = effectiveRequests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, ct);

        var lastAssigneeUserId = entity.Assignees
                                       .OrderByDescending(a => a.Sequence)
                                       .Select(a => (UserId?)a.UserId)
                                       .FirstOrDefault();

        var resolvedSendToAcceptorId = lastAssigneeUserId ?? sendToAcceptorId;

        _ = effectiveRequests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var acceptor = CaContractDraftEditAcceptor.Create(usr, req.AcceptorType, req.Sequence);

                            acceptor.SetSendToAcceptorId(resolvedSendToAcceptorId);

                            if (!string.IsNullOrWhiteSpace(req.CommitteePositionsCode))
                            {
                                acceptor.SetCommitteePositionsCode(ParameterCode.From(req.CommitteePositionsCode));
                            }

                            entity.AddAcceptor(acceptor);

                            if (req.IsUnableToPerformDuties == true)
                            {
                                acceptor.UnableToPerformDuties();
                            }

                            return acceptor;
                        })
                    .ToList();

        _ = effectiveRequests.Where(w => w.Id.HasValue)
                    .Join(
                        entity.Acceptors,
                        req => req.Id!.Value,
                        acc => acc.Id.Value,
                        (req, acc) =>
                        {
                            var user = users.FirstOrDefault(u => u.Id == UserId.From(req.UserId));

                            if (user != null)
                            {
                                acc.Update(user);
                                acc.SetType(req.AcceptorType);
                                acc.SetSequence(req.Sequence);
                            }

                            if (req.IsUnableToPerformDuties == true)
                            {
                                acc.UnableToPerformDuties();
                            }

                            return acc;
                        })
                    .ToList();
    }

    protected async Task UpsertAssignee(
        CaContractDraftVendorEdit entity,
        AssigneeRequest[] requests,
        CancellationToken ct)
    {
        var existingIds = requests.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();

        entity.Assignees
              .Where(a => !existingIds.Contains(a.Id.Value))
              .Iter(a => a.Delete());

        var userIds = requests.Select(r => r.UserId).ToList();
        var users = await this.ValidateUsersAsync(userIds, ct);

        _ = requests.Where(w => !w.Id.HasValue)
                    .Join(
                        users,
                        req => UserId.From(req.UserId),
                        usr => usr.Id,
                        (req, usr) =>
                        {
                            var assignee = CaContractDraftVendorEditAssignee.Create(
                                req.AssigneeGroup,
                                req.AssigneeType,
                                usr,
                                req.Sequence);

                            entity.AddAssignee(assignee);

                            return assignee;
                        })
                    .ToList();
    }

    private async Task<SuUser[]> ValidateUsersAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Map(UserId.From).ToArray();

        var users = await this.dbContext.SuUsers
                              .Include(u => u.Employee)
                              .ThenInclude(e => e.View)
                              .Where(u => ids.Contains(u.Id))
                              .ToArrayAsync(cancellationToken);

        var missingIds = ids.Except(users.Map(u => u.Id)).ToArray();

        if (missingIds.Length > 0)
        {
            this.ThrowError(
                $"ไม่พบผู้ใช้ที่มีรหัส {string.Join(", ", missingIds)}",
                StatusCodes.Status404NotFound);
        }

        return users;
    }

    // ── Document Template Helpers ──
    protected async Task<ParameterCode> GetSupplyMethodCodeAsync(CaContractDraftVendorEdit entity, CancellationToken ct)
    {
        var sourceVendor = await this.dbContext.CaContractDraftVendors
                                     .Include(v => v.ContractDraft)
                                     .ThenInclude(cd => cd.Procurement)
                                     .SingleOrDefaultAsync(v => v.Id == entity.ContractDraftVendorId, ct);

        if (sourceVendor is null)
        {
            this.ThrowError("ไม่พบข้อมูลสัญญาต้นฉบับ", StatusCodes.Status404NotFound);
        }

        return sourceVendor.ContractDraft.Procurement.SupplyMethodCode;
    }

    protected async Task<ContractDraftVendorEditReplaceDto> GetMappingDtoAsync(
        CaContractDraftVendorEdit entity,
        UserId? creatorUserId = null,
        bool hasAcceptor = false,
        bool hasCommittee = false,
        bool hasComment = false,
        CancellationToken ct = default)
    {
        var lastApprover = entity.Acceptors
                                 .Where(a => a is { Type: AcceptorType.Approver })
                                 .OrderBy(a => a.Sequence)
                                 .LastOrDefault();

        var acceptors = hasAcceptor
            ? entity.Acceptors
                    .Where(a => a is { Status: AcceptorStatus.Approved, Type: AcceptorType.Approver })
                    .Select(DelegatorExtensions.DelegatorToAcceptor)
                    .OrderBy(a => a.Sequence)
                    .Select(a => new ContractDraftVendorEditAcceptorReplace(
                        (a.Status, lastApprover == a) switch
                        {
                            (AcceptorStatus.Approved, false) => "เห็นชอบ",
                            (AcceptorStatus.Approved, true) => "อนุมัติ",
                            _ => string.Empty,
                        },
                        a.FullName,
                        a.PositionName))
            : [];

        var committee = hasCommittee
            ? entity.Acceptors
                    .Where(a => a is { Status: AcceptorStatus.Approved, Type: AcceptorType.AcceptanceCommittee })
                    .OrderBy(a => a.Sequence)
                    .Select(a => new ContractDraftVendorEditCommitteeReplace(
                        "เห็นชอบ",
                        a.FullName,
                        a.PositionName))
            : [];

        var assignees = entity.Assignees
                              .OrderBy(a => a.Sequence)
                              .Select(DelegatorExtensions.DelegatorToAssignee)
                              .Select(a => new ContractDraftVendorEditAssigneeReplace(
                                  a.FullName,
                                  a.PositionName,
                                  a.Remark));

        var creator = await this.GetCreatorReplaceAsync(entity, creatorUserId, ct);

        var sourceVendor = await this.dbContext.CaContractDraftVendors
                                     .Include(v => v.PaymentTerms)
                                     .Include(v => v.Attachments)
                                     .ThenInclude(a => a.Files)
                                     .SingleOrDefaultAsync(v => v.Id == entity.ContractDraftVendorId, ct);

        var contractDraftInfo = sourceVendor is not null
            ? ContractDraftInfoReplaceDto.FromEntity(sourceVendor)
            : ContractDraftInfoReplaceDto.FromEntity(entity);

        var newContractDraftInfo = ContractDraftInfoReplaceDto.FromEntity(entity, sourceVendor);

        var acceptorSign = entity.Acceptors
                                 .Where(x => x.Type == AcceptorType.AcceptorSign)
                                 .Map(DelegatorExtensions.DelegatorToAcceptor)
                                 .Map(MapAcceptorSign)
                                 .FirstOrDefault();

        var jorPorComment = hasComment
            ? entity.Assignees
                    .Where(x => x.Type == AssigneeType.Assignee && !string.IsNullOrWhiteSpace(x.Remark))
                    .OrderByDescending(x => x.ActionAt)
                    .Select(x => new JorPorCommentDto(
                        "ให้ความเห็น",
                        x.FullName,
                        x.PositionName,
                        x.Remark))
                    .FirstOrDefault()
            : null;

        var documentDate = entity.Status is not (ContractDraftVendorEditStatus.Draft
             or ContractDraftVendorEditStatus.Editing or ContractDraftVendorEditStatus.Rejected)
         ? entity.DocumentDate?.ToThaiDateString() ?? DateTimeOffset.Now.ToThaiDateString()
         : null;

        return new ContractDraftVendorEditReplaceDto(
            entity.ContractName ?? string.Empty,
            entity.ContractNumber ?? string.Empty,
            entity.ContractDraftNumber.Value,
            entity.ContractSignedDate?.ToThaiDateString() ?? string.Empty,
            entity.Budget.ToCurrencyStringWithComma(),
            entity.Budget > 0 ? entity.Budget.ThaiBahtText() : "-",
            entity.Title,
            entity.Description,
            documentDate,
            acceptorSign,
            creator,
            acceptors,
            committee,
            assignees,
            jorPorComment,
            contractDraftInfo,
            new NewContractDraftEditReplaceDto(newContractDraftInfo));
    }

    private static AcceptorSignDto MapAcceptorSign(CaContractDraftEditAcceptor acceptor)
    {
        return new AcceptorSignDto(
            acceptor.FullName,
            acceptor.PositionName);
    }

    private async Task<ContractDraftVendorEditCreatorReplace?> GetCreatorReplaceAsync(
        CaContractDraftVendorEdit entity,
        UserId? creatorUserId,
        CancellationToken ct)
    {
        var user =
            creatorUserId is not null
                ? await this.dbContext.SuUsers
                            .Include(u => u.Employee)
                            .ThenInclude(e => e.View)
                            .FirstOrDefaultAsync(u => u.Id == creatorUserId, ct)
                : await this.GetLastActivityCreatedByAsync(
                    entity.Id.ToString(),
                    ActivityLogActionTypeConstant.SendApprove,
                    ct);

        if (user == null)
        {
            return null;
        }

        return new ContractDraftVendorEditCreatorReplace(
            "ผู้จัดทำ",
            user.Employee.Signature,
            user.FullName,
            user.Employee.View?.FullPositionName ?? string.Empty);
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

        return await this.dbContext.SuUsers
                         .Include(u => u.Employee)
                         .ThenInclude(e => e.View)
                         .FirstOrDefaultAsync(
                             u => u.Id == UserId.From(lastActivity.AuditInfo.CreatedBy),
                             ct);
    }

    protected record ContractDraftVendorEditDocumentOptions(
        bool IsAmendmentReplace,
        bool IsApprovalRequestReplace,
        bool IsMarkReplaced = false);

    protected async Task<FileId> GetDocumentTemplateByTypeAsync(
        CaContractDraftEditVendorDocumentType documentType,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var documentService = this.Resolve<IDocumentService>();

        var isEighty = supplyMethodCode.Value == SupplyMethodConstant.Eighty;

        var contractTemplateCode = documentType switch
        {
            CaContractDraftEditVendorDocumentType.Amendment when isEighty => CMContractDraftVendorEditDocumentTemplatesConstant.Amendment80,
            CaContractDraftEditVendorDocumentType.Amendment => CMContractDraftVendorEditDocumentTemplatesConstant.Amendment60,
            CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest when isEighty => CMContractDraftVendorEditDocumentTemplatesConstant.AmendmentApprovalRequest80,
            CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest => CMContractDraftVendorEditDocumentTemplatesConstant.AmendmentApprovalRequest60,
            _ when isEighty => CMContractDraftVendorEditDocumentTemplatesConstant.Amendment80,
            _ => CMContractDraftVendorEditDocumentTemplatesConstant.Amendment60,
        };

        var templateFileId = await documentService.GetDocumentTemplateAsync(
            c => c.Group == DocumentTemplateGroups.CMContractDraftVendorEdit &&
                 c.SupplyMethodCode == supplyMethodCode &&
                 c.Code == contractTemplateCode,
            ct);

        if (templateFileId == null)
        {
            this.ThrowError(
                DocumentErrorMessages.DocumentTemplateNotFound,
                StatusCodes.Status404NotFound);
        }

        return (FileId)templateFileId;
    }

    protected async ValueTask SetDefaultDocumentTemplate(
        CaContractDraftVendorEdit entity,
        ParameterCode supplyMethodCode,
        CancellationToken ct)
    {
        var amendmentTemplateDocId = await this.GetDocumentTemplateByTypeAsync(
            CaContractDraftEditVendorDocumentType.Amendment,
            supplyMethodCode,
            ct);

        var approvalRequestTemplateDocId = await this.GetDocumentTemplateByTypeAsync(
            CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest,
            supplyMethodCode,
            ct);

        entity.AddDocumentHistory(CaContractDraftEditVendorDocumentType.Amendment, amendmentTemplateDocId, false);
        entity.AddDocumentHistory(CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest, approvalRequestTemplateDocId, false);
    }

    protected async ValueTask<(FileId? AmendmentFileId, FileId? ApprovalRequestFileId)> UpdateDocumentAsync(
        CaContractDraftVendorEdit entity,
        ParameterCode supplyMethodCode,
        ContractDraftVendorEditDocumentOptions options,
        CancellationToken ct,
        UserId? creatorUserId = null,
        bool hasAcceptor = false,
        bool hasCommittee = false,
        bool hasComment = false)
    {
        var documentService = this.Resolve<IDocumentService>();
        var replaceDto = await this.GetMappingDtoAsync(entity, creatorUserId, hasAcceptor, hasCommittee, hasComment, ct);

        var lastedAmendmentDocument = entity.GetAmendmentDocumentForStatus(entity.Status);
        var lastedApprovalRequestDocument = entity.GetApprovalRequestDocumentForStatus(entity.Status);

        if (lastedAmendmentDocument is null || lastedApprovalRequestDocument is null)
        {
            this.ThrowError("ไม่พบเอกสารร่าง ที่ต้องการอัปโหลด", StatusCodes.Status404NotFound);
        }

        var isEighty = supplyMethodCode.Value == SupplyMethodConstant.Eighty;

        var amendmentFileId = await ReplaceDocument(
            lastedAmendmentDocument.FileId,
            CaContractDraftEditVendorDocumentType.Amendment,
            options.IsAmendmentReplace,
            isEighty);

        var approvalRequestFileId = await ReplaceDocument(
            lastedApprovalRequestDocument.FileId,
            CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest,
            options.IsApprovalRequestReplace,
            isEighty);

        entity.AddDocumentHistory(
            CaContractDraftEditVendorDocumentType.Amendment,
            amendmentFileId,
            options.IsMarkReplaced);

        entity.AddDocumentHistory(
            CaContractDraftEditVendorDocumentType.AmendmentApprovalRequest,
            approvalRequestFileId,
            options.IsMarkReplaced);

        return (amendmentFileId, approvalRequestFileId);

        async Task<FileId> ReplaceDocument(
            FileId fileId,
            CaContractDraftEditVendorDocumentType documentType,
            bool isReplace,
            bool isEighty)
        {
            var sourceFileId = isReplace
                ? await this.GetDocumentTemplateByTypeAsync(documentType, supplyMethodCode, ct)
                : fileId;

            var parentDirectory =
                $"{DocumentTemplateGroups.CMContractDraftVendorEdit}/{entity.Id}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt";

            var elementCode = this.MapTemplateToElementCode(entity.TemplateCode?.Value ?? string.Empty);

            var elementFileId = await this.dbContext.SuDocumentTemplates
                                          .Where(c => c.Code == elementCode)
                                          .Select(x => x.FileId)
                                          .FirstOrDefaultAsync(ct);

            var sections = entity.Components
                                 .Where(c => c.IsEdited)
                                 .ToList()
                                 .OrderBy(c => ExtractLeadingNumber(c.ComponentName))
                                 .Select(c => MapComponentCodeToSection(c.ComponentCode))
                                 .Append("SectionDetail")
                                 .ToArray();

            var fontName = isEighty ? "TH Sarabun New" : "TH SarabunIT๙";

            var copyFileId = await documentService.CopyDocumentTemplateAsync(
                sourceFileId,
                elementFileId,
                (contentA, contentB) => OdtDocumentExtensions.ReplaceOdtSectionFromElementAndDocument(
                    contentA, contentB, sections, replaceDto),
                parentDirectory: parentDirectory,
                cancellationToken: ct);

            if (copyFileId is null)
            {
                this.ThrowError(
                    DocumentErrorMessages.CopyDocumentFailed,
                    StatusCodes.Status500InternalServerError);
            }

            return copyFileId.Value;
        }
    }

    private static int ExtractLeadingNumber(string name)
    {
        var match = System.Text.RegularExpressions.Regex.Match(name, @"\d+");

        return match.Success ? int.Parse(match.Value) : int.MaxValue;
    }

    private static string MapComponentCodeToSection(string componentCode) => componentCode switch
    {
        "SalesAgreement" => "ContractDraftInfoDetail.Agreement",
        "PartOfContract" => "ContractDraftInfoDetail.Attachments",
        "ContractPerformance" => "ContractDraftInfoDetail.Guarantee",
        "Payment" => "ContractDraftInfoDetail.Payment",
        "AdvancePayment" => "ContractDraftInfoDetail.AdvancePayment",
        "RetentionPayment" => "ContractDraftInfoDetail.RetentionPayment",
        "TerminationInfoDuration" => "ContractDraftInfoDetail.Termination",
        "TerminationInfoDate" => "ContractDraftInfoDetail.Termination",
        "Mulct" => "ContractDraftInfoDetail.Penalty",
        "Warranty" => "ContractDraftInfoDetail.Warranty",
        "DefectWarranty" => "ContractDraftInfoDetail.Warranty",
        "Delivery" => "ContractDraftInfoDetail.Delivery",
        "CarLeaseInfo" => "ContractDraftInfoDetail.CarLease",
        "Redelivery" => "ContractDraftInfoDetail.Redelivery",
        "WarrantyMA" => "New.ContractDraftInfoDetail.Warranty.Warranty",
        "ComputerLeaseInfo" => "ContractDraftInfoDetail.ComputerLease.Duration",
        "CopierLeaseInfo" => "ContractDraftInfoDetail.CopierLeaseInfo",
        "Period" => "ContractDraftInfoDetail.Agreement.DurationText",
        "RentalFee" => "ContractDraftInfoDetail.RentalFee",
        _ => $"ContractDraftInfoDetail.{componentCode}",
    };

    public ContractDraftVendorEditComponentDto[] MapComponentByTemplateCode(string templateCode, ContractDraftVendorEditComponentDto[] contractDraftVendorEditComponentDtos)
    {
        if (contractDraftVendorEditComponentDtos.Any())
        {
            return contractDraftVendorEditComponentDtos;
        }

        return templateCode switch
        {
            "CFormat002" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "CAPurchase2", "สัญญาข้อ 2 การรับรองคุณภาพ", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Delivery", "สัญญาข้อ 4 การส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "CAPurchase5", "สัญญาข้อ 5 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 6 การชำระเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "CAPurchase9", "สัญญาข้อ 9 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 10 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "CAPurchase11", "สัญญาข้อ 11 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "CAPurchase12", "สัญญาข้อ 12 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "CAPurchase13", "สัญญาข้อ 13 การใช้เรือไทย", false),
            },

            "CFormat003" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd2", "สัญญาข้อ 2 การรับรองคุณภาพ", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd4", "สัญญาข้อ 4 การออกใบสั่งซื้อแต่ละคราว", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd5", "สัญญาข้อ 5 การส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd6", "สัญญาข้อ 6 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 7 การชำระเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 8 การรับประกันความชำรุดบกพร่อง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 9 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd10", "สัญญาข้อ 10 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 11 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd12", "สัญญาข้อ 12 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd13", "สัญญาข้อ 13 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseOpenEnd14", "สัญญาข้อ 14 การใช้เรือไทย", false),
            },

            "CFormat004" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer2", "สัญญาข้อ 2 การรับรองคุณภาพ", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Delivery", "สัญญาข้อ 4 การส่งมอบและติดตั้ง", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer5", "สัญญาข้อ 5 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 6 การชำระเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer9", "สัญญาข้อ 9 การโอนกรรมสิทธิ์", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer10", "สัญญาข้อ 10 การอบรม", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer11", "สัญญาข้อ 11 คู่มือการใช้คอมพิวเตอร์", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer12", "สัญญาข้อ 12 การรับประกันความเสียหาย", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer13", "สัญญาข้อ 13 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 14 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer15", "สัญญาข้อ 15 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer16", "สัญญาข้อ 16 การงดหรือลดค่าปรับ หรือขยายเวลาในการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseComputer17", "สัญญาข้อ 17 การใช้เรือไทย", false),
            },

            "CFormat005" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd1", "สัญญาข้อ 1 คำนิยาม", false),
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 2 ข้อตกลงซื้อขายและอนุญาตให้ใช้สิทธิ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd3", "สัญญาข้อ 3 การรับรองและการอนุญาตให้ใช้สิทธิ", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 4 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Delivery", "สัญญาข้อ 5 การส่งมอบและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd6", "สัญญาข้อ 6 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 7 การชำระเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd8", "สัญญาข้อ 8 สิทธิของผู้ซื้อ", false),
                new ContractDraftVendorEditComponentDto(null, "DefectWarranty", "สัญญาข้อ 9 การรับประกันความชำรุดบกพร่อง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 10 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd11", "สัญญาข้อ 11 สการอบรม", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd12", "สัญญาข้อ 12 คู่มือการใช้โปรแกรมคอมพิวเตอร์และการให้คำแนะนำ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd13", "สัญญาข้อ 13 การรักษาความลับทางการค้า", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd14", "สัญญาข้อ 14 ความคุ้มครองเกี่ยวกับลิขสิทธิ์", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd15", "สัญญาข้อ 15 โปรแกรมคอมพิวเตอร์ที่ได้รับการแก้ไขพัฒนาให้ดีขึ้น", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd16", "สัญญาข้อ 16 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 17 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd18", "สัญญาข้อ 18 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd19", "สัญญาข้อ 19 การส่งคืนโปรแกรมคอมพิวเตอร์", false),
                new ContractDraftVendorEditComponentDto(null, "PurchaseSoftwareLicenseAnd20", "สัญญาข้อ 20 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ", false),
            },

            "CFormat012" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange3", "สัญญาข้อ 3 การตรวจสอบสิ่งของ", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange4", "สัญญาข้อ 4 การรับรองคุณภาพ", false),
                new ContractDraftVendorEditComponentDto(null, "Delivery", "สัญญาข้อ 5 การส่งมอบและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange6", "สัญญาข้อ 6 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 7 การรับประกันความชำรุดบกพร่อง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange9", "สัญญาข้อ 9 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 10 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange10", "สัญญาข้อ 11 การงดหรือลดค่าปรับ หรือขยายเวลาส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange11", "สัญญาข้อ 12 ข้อจำกัดความรับผิดของผู้ให้แลกเปลี่ยน", false),
                new ContractDraftVendorEditComponentDto(null, "Exchange12", "สัญญาข้อ 13 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
            },

            "CFormat001" or "CFormat016" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "AdvancePayment", "สัญญาข้อ 5 เงินค่าจ้างล่วงหน้า", false),
                new ContractDraftVendorEditComponentDto(null, "RetentionPayment", "สัญญาข้อ 6 การหักเงินประกันผลงาน", false),
                new ContractDraftVendorEditComponentDto(null, "TerminationInfoDuration", "สัญญาข้อ 7 (ก) กำหนดเวลาแล้วเสร็จและสิทธิ์ของผู้ว่าจ้างในการบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "TerminationInfoDate", "สัญญาข้อ 7 (ข) กำหนดเวลาแล้วเสร็จและสิทธิ์ของผู้ว่าจ้างในการบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 8 ความรับผิดชอบในความชำรุดบกพร่องของงานจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction9", "สัญญาข้อ 9 การจ้างช่วง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction10", "สัญญาข้อ 10 การควบคุมงานของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction11", "สัญญาข้อ 11 ความรับผิดของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction12", "สัญญาข้อ 12 การจ่ายเงินแก่ลูกจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction13", "สัญญาข้อ 13 การตรวจงานจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction14", "สัญญาข้อ 14 แบบรูปและรายการละเอียดคลาดเคลื่อน", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction15", "สัญญาข้อ 15 การควบคุมงานโดยผู้ว่าจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction16", "สัญญาข้อ 16 งานพิเศษและการแก้ไขงาน ", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 17 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction18", "สัญญาข้อ 18 สิทธิของผู้ว่าจ้างภายหลังบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction19", "สัญญาข้อ 19 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction20", "สัญญาข้อ 20 การทำบริเวณก่อสร้างให้เรียบร้อย", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction21", "สัญญาข้อ 21 การงดหรือลดค่าปรับ หรือการขยายเวลาปฏิบัติงานตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction22", "สัญญาข้อ 22 การใช้เรือไทย", false),
                new ContractDraftVendorEditComponentDto(null, "HireConstruction23", "สัญญาข้อ 23 มาตรฐานฝีมือช่าง", false),
            },

            "CFormat013" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "AdvancePayment", "สัญญาข้อ 5 เงินค่าจ้างล่วงหน้า", false),
                new ContractDraftVendorEditComponentDto(null, "TerminationInfoDate", "สัญญาข้อ 6 กำหนดเวลาแล้วเสร็จและสิทธิของผู้ว่าจ้างในการบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 7 ความรับผิดชอบในความชำรุดบกพร่องของงานจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork8", "สัญญาข้อ 8 การจ้างช่วง", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork9", "สัญญาข้อ 9 ความรับผิดของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork10", "สัญญาข้อ 10 การจ่ายเงินแก่ลูกจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork11", "สัญญาข้อ 11 การตรวจรับงานจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork12", "สัญญาข้อ 12 รายละเอียดของงานจ้างคลาดเคลื่อน", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 13 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork14", "สัญญาข้อ 14 สิทธิของผู้ว่าจ้างภายหลังบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork15", "สัญญาข้อ 15 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork16", "สัญญาข้อ 16 การงดหรือลดค่าปรับ หรือการขยายเวลาปฏิบัติงานตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "HireCustomWork17", "สัญญาข้อ 17 การใช้เรือไทย", false),
            },

            "CFormat009" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 3 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 5 หน้าที่และความรับผิดชอบของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService6", "สัญญาข้อ 6 การจ้างช่วง", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService7", "สัญญาข้อ 7 การควบคุมงานของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService8", "สัญญาข้อ 8 การตรวจงานจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService9", "สัญญาข้อ 9 การแก้ไขเปลี่ยนแปลงงาน และต่อสัญญาจ้างในกรณีจำเป็น", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService10", "สัญญาข้อ 10 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService11", "สัญญาข้อ 11 การควบคุมงานโดยผู้ว่าจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService12", "สัญญาข้อ 12 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "HireBuildingCleaningService13", "สัญญาข้อ 13 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา", false),
            },

            "CFormat014" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 3 ค่าจ้างงานออกแบบและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 4 หน้าที่ของผู้ให้บริการงานออกแบบ", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision5", "สัญญาข้อ 5 ข้อตกลงว่าจ้างงานควบคุมงานก่อสร้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision6", "สัญญาข้อ 6 ค่าจ้างควบคุมงานก่อสร้างและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision7", "สัญญาข้อ 7 หน้าที่ของผู้ให้บริการงานควบคุมงานก่อสร้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision8", "สัญญาข้อ 8 ค่าจ้างงานควบคุมงานกรณีผู้รับจ้างปฏิบัติงานล่วงเลยกำหนดเวลา", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision9", "สัญญาข้อ 9 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่ายของงานออกแบบและควบคุมงานก่อสร้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision10", "สัญญาข้อ 10 การงดหรือลดค่าปรับ หรือการขยายเวลาการปฏิบัติงานออกแบบและควบคุมงานก่อสร้าง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 11 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision12", "สัญญาข้อ 12 การจ้างช่วงงานออกแบบและควบคุมงานก่อสร้าง", false),
                new ContractDraftVendorEditComponentDto(null, "HireDesignAndSupervision13", "สัญญาข้อ 13 การโอนสิทธิประโยชน์ของผู้ให้บริการงานออกแบบและควบคุมงานก่อสร้าง", false),
            },

            "CFormat007" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "TerminationInfoDuration", "สัญญาข้อ 3 ระยะเวลาให้บริการ", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 4 ค่าจ้างและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "WarrantyMA", "สัญญาข้อ 5 การรับประกันผลงาน", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 6 การให้บริการ", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 7 ความรับผิดชอบของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerMaintenance9", "สัญญาข้อ 9 การจ้างช่วง", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerMaintenance10", "สัญญาข้อ 10 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerMaintenance11", "สัญญาข้อ 11 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerMaintenance12", "สัญญาข้อ 12 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา", false),
            },

            "CFormat010" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 2 การจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService3", "สัญญาข้อ 3 หน้าที่และความรับผิดของผู้รับจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService4", "สัญญาข้อ 4 หน้าที่และความรับผิดชอบของผู้ว่าจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService5", "สัญญาข้อ 5 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService6", "สัญญาข้อ 6 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService7", "สัญญาข้อ 7 การจ้างช่วง", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 8 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService9", "สัญญาข้อ 9 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
            },

            "CFormat015" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 2 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 3 ค่าจ้างและการจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "AdvancePayment", "สัญญาข้อ 4 เงินค่าจ้างล่วงหน้า", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService5", "สัญญาข้อ 5 ความรับผิดชอบของที่ปรึกษา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService6", "สัญญาข้อ 6 การระงับการทำงานชั่วคราวและการบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService7", "สัญญาข้อ 7 สิทธิและหน้าที่ของที่ปรึกษา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService8", "สัญญาข้อ 8 ความรับผิดชอบของที่ปรึกษาต่อบุคคลภายนอก", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService9", "สัญญาข้อ 9 พันธะหน้าที่ของผู้ว่าจ้าง", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 10 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService11", "สัญญาข้อ 11 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "RetentionPayment", "สัญญาข้อ 12 (ก) เงินประกันผลงาน", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 12 (ข) หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService13", "สัญญาข้อ 13 การจ้างช่วง", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService14", "สัญญาข้อ 14 การโอนสิทธิตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ElementCAMHirerSecurityService15", "สัญญาข้อ 15 การงดหรือลดค่าปรับ หรือขยายเวลาปฏิบัติงานตามสัญญา", false),
            },

            "CFormat011" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "CopierLeaseInfo", "สัญญาข้อ 2 ค่าเช่าเครื่องถ่ายเอกสาร", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Delivery", "สัญญาข้อ 4 การส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier5", "สัญญาข้อ 5 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier6", "สัญญาข้อ 6 การงดหรือลดค่าปรับ หรือขยายเวลาในการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Warranty", "สัญญาข้อ 7 การบำรุงรักษาตรวจสภาพและซ่อมแซมเครื่องถ่ายเอกสารที่เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier8", "สัญญาข้อ 8 หน้าที่ของผู้ให้เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier9", "สัญญาข้อ 9 ค่าปรับกรณีความชำรุดบกพร่องของเครื่องถ่ายเอกสาร", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 10 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier11", "สัญญาข้อ 11 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 12 ค่าปรับกรณีส่งมอบล่าช้า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier13", "สัญญาข้อ 13 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier14", "สัญญาข้อ 14 การโอนสิทธิของผู้ให้เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier15", "สัญญาข้อ 15 การนำเครื่องถ่ายเอกสารที่เช่ากลับคืนเมื่อสัญญาสิ้นสุดลง", false),
                new ContractDraftVendorEditComponentDto(null, "RentCopier16", "สัญญาข้อ 16 ข้อจำกัดความรับผิดของผู้เช่า", false),
            },

            "CFormat006" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "RentComputer1", "สัญญาข้อ 1 คำนิยาม", false),
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 2 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "ComputerLeaseInfo", "สัญญาข้อ 4 ระยะเวลาการเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer5", "สัญญาข้อ 5 การชำระค่าเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer6", "สัญญาข้อ 6 การรับรองคุณภาพ", false),
                new ContractDraftVendorEditComponentDto(null, "Delivery", "สัญญาข้อ 7 การส่งมอบและติดตั้ง", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer8", "สัญญาข้อ 8 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer9", "สัญญาข้อ 9 การบำรุงรักษา", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer10", "สัญญาข้อ 10 การซ่อมแซมแก้ไข", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer11", "สัญญาข้อ 11 การใช้ประโยชน์", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer12", "สัญญาข้อ 12 การจัดอบรม", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer13", "สัญญาข้อ 13 คู่มือการใช้คอมพิวเตอร์", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 14 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer15", "สัญญาข้อ 15 ข้อตกลงการใช้โปรแกรม", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer16", "สัญญาข้อ 16 การรับประกันความเสียหาย", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer17", "สัญญาข้อ 17 ความรับผิดต่อความเสียหาย", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer18", "สัญญาข้อ 18 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 19 ค่าปรับ", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer20", "สัญญาข้อ 20 การนำคอมพิวเตอร์กลับคืนไป", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer21", "สัญญาข้อ 21 การโอนกรรมสิทธิ์ให้บุคคลอื่น", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer22", "สัญญาข้อ 22 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer23", "สัญญาข้อ 23 การงดหรือลดค่าปรับ หรือขยายเวลาในการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "RentComputer24", "สัญญาข้อ 24 การโอนสิทธิและหน้าที่ตามสัญญา", false),
            },

            "CFormat008" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลง", false),
                new ContractDraftVendorEditComponentDto(null, "CarLeaseInfo", "สัญญาข้อ 2 ค่าเช่ารถยนต์", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 3 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Payment", "สัญญาข้อ 4 การจ่ายเงิน", false),
                new ContractDraftVendorEditComponentDto(null, "Redelivery", "สัญญาข้อ 5 การตรวจรับ", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar6", "สัญญาข้อ 6 การงดหรือลดค่าปรับ หรือการขยายเวลาในการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar7", "สัญญาข้อ 7 หน้าที่ของผู้ให้เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar8", "สัญญาข้อ 8 การบอกเลิกสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Mulct", "สัญญาข้อ 9 ค่าปรับกรณีส่งมอบล่าช้า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar10", "สัญญาข้อ 10 การบังคับค่าปรับ ค่าเสียหาย และค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar11", "สัญญาข้อ 11 การใช้ประโยชน์จากรถยนต์ที่เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar12", "สัญญาข้อ 12 การรับมอบรถยนต์ที่เช่ากลับคืน", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 13 หลักประกันการปฏิบัติตามสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "RentCar14", "สัญญาข้อ 14 ข้อจำกัดความรับผิดของผู้เช่า", false),
            },

            "CMRentalTpl001" or "CMRentalTpl002" or "CMRentalTpl003" or "CMRentalTpl004" => new[]
            {
                new ContractDraftVendorEditComponentDto(null, "SalesAgreement", "สัญญาข้อ 1 ข้อตกลงซื้อขาย", false),
                new ContractDraftVendorEditComponentDto(null, "Rental2", "สัญญาข้อ 2 คำรับรองเกี่ยวกับกรรมสิทธิ์ในสถานที่เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental3", "สัญญาข้อ 3 การส่งมอบ", false),
                new ContractDraftVendorEditComponentDto(null, "Period", "สัญญาข้อ 4 ระยะเวลาเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "RentalFee", "สัญญาข้อ 5 ค่าเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental6", "สัญญาข้อ 6 คำมั่นจะให้เช่าต่อไปอีกเมื่อครบกำหนดระยะเวลาเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental7", "สัญญาข้อ 7 สิทธิและหน้าที่ของผู้ให้เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental8", "สัญญาข้อ 8 สิทธิและหน้าที่ของผู้เช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental9", "สัญญาข้อ 9 การบังคับค่าเสียหายและค่าใช้จ่าย", false),
                new ContractDraftVendorEditComponentDto(null, "PartOfContract", "สัญญาข้อ 10 เอกสารอันเป็นส่วนหนึ่งของสัญญา", false),
                new ContractDraftVendorEditComponentDto(null, "Rental11", "สัญญาข้อ 11 การพักการเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental12", "สัญญาข้อ 12 การสิ้นสุดของสัญญาเช่า", false),
                new ContractDraftVendorEditComponentDto(null, "Rental13", "สัญญาข้อ 13 การบอกกล่าว", false),
                new ContractDraftVendorEditComponentDto(null, "Rental14", "สัญญาข้อ 14 การประกันภัย", false),
                new ContractDraftVendorEditComponentDto(null, "ContractPerformance", "สัญญาข้อ 15 เงินประกันการเช่า", false),
            },

            _ => [],
        };
    }

    public string MapTemplateToElementCode(string templateCode)
    {
        return templateCode switch
        {
            "CFormat002" => "ElementCAMPurchase",

            "CFormat003" => "ElementCAMPurchaseOpenEnd",

            "CFormat004" => "ElementCAMPurchaseComputer",

            "CFormat005" => "ElementCAMPurchaseSoftwareLicenseAnd",

            "CFormat012" => "ElementCAMExchange",

            "CFormat016" => "ElementCAMHireConstructionKor",

            "CFormat001" => "ElementCAMHireConstruction",

            "CFormat013" => "ElementCAMHireCustomWork",

            "CFormat009" => "ElementCAMHireBuildingCleaningService",

            "CFormat014" => "ElementCAMHireDesignAndSupervision",

            "CFormat007" => "ElementCAMHirerMaintenance",

            "CFormat010" => "ElementCAMHirerSecurityService",

            "CFormat015" => "ElementCAMHireConsulting",

            "CFormat011" => "ElementCAMRentCopier",

            "CFormat006" => "ElementCAMRentComputer",

            "CFormat008" => "ElementCAMRentCar",

            "CMRentalTpl001" => "ElementCAMRentArea",

            "CMRentalTpl002" => "ElementCAMRentBuilding",

            "CMRentalTpl003" => "ElementCAMRentParking",

            "CMRentalTpl004" => "ElementCAMRentBillboard",

            _ => string.Empty,
        };
    }
}