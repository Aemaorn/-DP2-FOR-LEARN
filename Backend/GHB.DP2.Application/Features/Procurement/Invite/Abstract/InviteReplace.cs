namespace GHB.DP2.Application.Features.Procurement.Invite.Abstract;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Extensions.Document;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.Procurement.PJp005;
using GHB.DP2.Domain.Procurement.PpPurchaseRequisition;
using GHB.DP2.Domain.Procurement.PpTorDraft;
using GHB.DP2.Domain.SystemUtility;
using GreatFriends.ThaiBahtText;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public abstract partial class InviteEndpointBase<TRequest, TResponse>
{
    // Example for map response DTO for GetListMapping
    protected async Task<InviteReplaceDto> MapToResponseMappingDtoAsync(
        PInvite invite,
        IEnumerable<UserId> operators,
        Guid userId,
        bool hasAcceptor,
        PInvitedEntrepreneurs? currentEntrepreneur = null,
        CancellationToken ct = default)
    {
        var targetEntrepreneur = currentEntrepreneur ?? invite.InvitedEntrepreneurs.FirstOrDefault();
        var lastedHistory = targetEntrepreneur?.DocumentHistories
                                              .OrderVersions()
                                              .FirstOrDefault();

        var acceptor = GetValue(
            hasAcceptor,
            InviteEndpointBase<TRequest, TResponse>.GetInviteAcceptor(invite, UserId.From(userId)),
            null);

        var torDraft = await this.GetTorDraft(invite, ct);
        var jp004 = await this.GetJp004(invite, ct);
        var jp005 = await this.GetJp005(invite, ct);

        if (jp005 is null || jp004 is null)
        {
            this.ThrowError("Procurement not found");
        }

        var approverAcceptor = invite.Status == PInviteStatus.Approved
            ? invite.Acceptors.FirstOrDefault(a => a.UserId == UserId.From(userId) && a.IsBoardChairman())
            : null;

        var committees = jp005.Committees
                              .Where(c => c.GroupType == PJp005CommitteeGroupType.ProcurementCommittee && c.IsBoardChairman())
                              .OrderBy(x => x.Sequence)
                              .Select(c => new PJp005CommitteeReplace(
                                  FullName: c.FullName,
                                  FullPositionName: c.FullPositionName,
                                  c.CommitteePositionsName,
                                  SignName: approverAcceptor?.Signature))
                              .FirstOrDefault();

        var jp05Committees = jp005.Committees
                                  .Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                  .ToList();

        var isCommittee = jp005.Committees.All(s => s.IsCommittee());

        var procurementCommittee = jp05Committees.Select((committee, index) => new CommitteeReplate(
            index + 1,
            committee.FullName,
            committee.CommitteePositionsName,
            committee.CommitteePositionsName));

        var maxCommitteeSequence = procurementCommittee.Any() ? procurementCommittee.Max(s => s.Sequence) : 0;

        var procurementSuppliesDivisions = jp005.ProcurementSuppliesDivisions.Select((committee, index) => new CommitteeReplate(
            maxCommitteeSequence + index + 1,
            committee.FullName,
            "ผู้จัดทำ",
            "ผู้จัดทำ"));

        var procurementCommittees = MapCommitteeSectionReplate(procurementCommittee.Union(procurementSuppliesDivisions), isCommittee);

        var referenceMedianPrice = $"ราคากลาง {jp004.MedianPriceAmount.Value.ToCurrencyStringWithComma()} บาท ({jp004.MedianPriceAmount.Value.ThaiBahtText()})";

        var pTorDraftQualificationsDescription = torDraft?.PpTorDraftQualifications.Select(p => new TorDraftQualificationsReplaceDto((int)p.Sequence, p.Description)) ?? [new TorDraftQualificationsReplaceDto(0, ".........................")];

        var ppPurchaseRequisitionEvaluationCriteria = jp004.EvaluationCriteria?.Label;

        var submitProposalStartTime =
            GetValue(
                invite.IsInvite,
                invite.SubmitProposalStartTime?.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm") + " น." ?? "-",
                "-");

        var submitProposalEndTime =
            GetValue(
                invite.IsInvite,
                invite.SubmitProposalEndTime?.ToOffset(TimeSpan.FromHours(7)).ToString("HH:mm") + " น." ?? "-",
                "-");

        var additionalDetail = (invite.NeedToKnowWithinDate != null || invite.ClarifyDetailViaDate != null) ? string.Format("หากต้องการทราบรายละเอียดเพิ่มเติมเกี่ยวกับรายละเอียดคุณลักษณะเฉพาะ โปรดสอบถามมายัง ธนาคารอาคารสงเคราะห์ ผ่านทางไปรษณีย์อิเล็กทรอนิกส์ ...... (e-mail) ..... และ ...... (e-mail) ..... ภายในวันที่ {0} ในเวลาราชการ โดยธนาคารอาคารสงเคราะห์ จะชี้แจงรายละเอียดดังกล่าว ผ่านทางเว็บไซต์ www.ghbank.co.th ในวันที่ {1}", invite.NeedToKnowWithinDate != null ? invite.NeedToKnowWithinDate.ToThaiDateString() : "..................", invite.ClarifyDetailViaDate != null ? invite.ClarifyDetailViaDate.ToThaiDateString() : "..................") : string.Empty;

        var entrepreneurReplaceDto = targetEntrepreneur is not null
            ? new InviteEntrepreneurReplaceDto(
                  targetEntrepreneur.Id.Value,
                  targetEntrepreneur.Vendor.Id.Value,
                  targetEntrepreneur.Sequence,
                  targetEntrepreneur.Vendor.TaxpayerIdentificationNo,
                  targetEntrepreneur.Vendor.EntrepreneurTypeInfo.Label,
                  targetEntrepreneur.Vendor.EstablishmentName,
                  targetEntrepreneur.Vendor.Email,
                  targetEntrepreneur.WatchlistResult,
                  targetEntrepreneur.WatchlistResultRemark,
                  targetEntrepreneur.WatchlistResultAt.ToThaiDateString(),
                  targetEntrepreneur.CoiResult,
                  targetEntrepreneur.CoiResultRemark,
                  targetEntrepreneur.CoiResultAt.ToThaiDateString(),
                  targetEntrepreneur.EgpResult,
                  targetEntrepreneur.EgpResultRemark,
                  targetEntrepreneur.EgpResultAt.ToThaiDateString(),
                  targetEntrepreneur.EmailSend,
                  targetEntrepreneur.Vendor.Nationality,
                  targetEntrepreneur.Vendor.Type,
                  targetEntrepreneur.Vendor.Tel,
                  [
                      .. targetEntrepreneur.InvitedEntrepreneurShareholders
                          .Select(s => new InviteEntrepreneurShareholderReplaceDto(
                              s.Id.Value,
                              s.Sequence,
                              s.TaxId,
                              s.FirstName,
                              s.LastName,
                              s.IsDirector,
                              s.IsShareholder,
                              s.WatchlistResult,
                              s.WatchlistResultRemark,
                              s.WatchlistResultAt.ToThaiDateString(),
                              s.CoiResult,
                              s.CoiResultRemark,
                              s.CoiResultAt.ToThaiDateString(),
                              s.EgpResult,
                              s.EgpRemark,
                              s.EgpResultAt.ToThaiDateString()))
                  ])
        : null;

        var acceptorDate = invite.Status is not (PInviteStatus.Draft or PInviteStatus.Edit or PInviteStatus.Rejected)
            ? invite.DocumentDate?.ToThaiDateString(format: "d MMMM yyyy") ?? DateTime.Now.ToThaiDateString(format: "d MMMM yyyy")
            : null;

        return new InviteReplaceDto(
            acceptorDate,
            referenceMedianPrice,
            pTorDraftQualificationsDescription,
            ppPurchaseRequisitionEvaluationCriteria,
            new InviteProcurementReplaceDto(
                invite.Procurement.PlanId.Map(p => p.Value),
                invite.Procurement.ProcurementNumber?.Value,
                invite.Procurement.Type,
                invite.Procurement.Step,
                invite.Procurement.Department.Name,
                invite.Procurement.DepartmentId.Value,
                invite.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                invite.Procurement.Name,
                invite.Procurement.Budget.Value.ToCurrencyStringWithComma(),
                invite.Procurement.Budget.ThaiBahtText(),
                invite.Procurement.BudgetYear,
                invite.Procurement.SupplyMethod.Label,
                invite.Procurement.SupplyMethodCode.Value,
                invite.Procurement.SupplyMethodType?.Label ?? string.Empty,
                invite.Procurement.SupplyMethodTypeCode?.Value,
                invite.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                invite.Procurement.SupplyMethodSpecialTypeCode?.Value,
                invite.Procurement.Status,
                invite.Procurement.ExpectingProcurementAt.ToThaiDateString(),
                invite.Procurement.IsStock,
                invite.Procurement.IsCommercialMaterial,
                invite.Procurement.Plan?.Type,
                invite.Procurement.ProcessType),
            invite.Id.Value,
            invite.ProcurementId.Value,
            invite.IsInvite,
            GetValue(invite.IsInvite, invite.SubmitProposalStartDate?.ToThaiDateString() ?? "-", "-"),
            GetValue(invite.IsInvite, invite.SubmitProposalEndDate?.ToThaiDateString() ?? "-", "-"),
            submitProposalStartTime,
            submitProposalEndTime,
            GetValue(invite.IsInvite, invite.NeedToKnowWithinDate?.ToThaiDateString() ?? "-", "-"),
            GetValue(invite.IsInvite, invite.ClarifyDetailViaDate?.ToThaiDateString() ?? "-", "-"),
            GetValue(invite.IsInvite, invite.PhoneNumber ?? string.Empty, string.Empty),
            lastedHistory?.FileId.Value,
            invite.Status,
            acceptor,
            entrepreneurReplaceDto,
            committees,
            procurementCommittees,
            additionalDetail,
            operators.Any(c => c == userId));
    }

    private static CommitteeSectionReplate MapCommitteeSectionReplate(
        IEnumerable<CommitteeReplate> committeeGroup, bool isCommittee)
    {
        return new CommitteeSectionReplate(
            isCommittee ? "คณะกรรมการจัดซื้อจัดจ้าง" : "ผู้จัดซื้อจัดจ้าง",
            committeeGroup);
    }

    private Task<PJp005?> GetJp005(PInvite invite, CancellationToken ct = default)
    {
        return this.dbContext.PJp005S
                   .Include(pJp005 => pJp005.Committees)
                   .Include(pJp005 => pJp005.ProcurementSuppliesDivisions)
                   .Where(p => p.ProcurementId == invite.ProcurementId)
                   .FirstOrDefaultAsync(p => p.IsActive == true && p.IsDeleted == false, ct);
    }

    private Task<PpPurchaseRequisition?> GetJp004(PInvite invite, CancellationToken ct = default)
    {
        return this.dbContext.PpPurchaseRequisitions
                   .Include(pJp005 => pJp005.Committees).Include(ppPurchaseRequisition => ppPurchaseRequisition.EvaluationCriteria)
                   .Where(p => p.ProcurementId == invite.ProcurementId)
                   .FirstOrDefaultAsync(p => p.IsDeleted == false, ct);
    }

    private Task<PpTorDraft?> GetTorDraft(PInvite invite, CancellationToken ct = default)
    {
        return this.dbContext.PpTorDrafts
                   .Include(ppTorDraft => ppTorDraft.PpTorDraftQualifications)
                   .Where(p => p.ProcurementId == invite.ProcurementId)
                   .FirstOrDefaultAsync(p => p.IsActive == true && p.IsDeleted == false, ct);
    }

    private static AcceptorInviteReplaceDto? GetInviteAcceptor(PInvite invite, UserId userId)
    {
        return invite.Acceptors
                     .Where(a => a.UserId == userId)
                     .OrderBy(a => a.Sequence)
                     .Select(a => new AcceptorInviteReplaceDto(
                         a.Id.Value,
                         a.Type,
                         a.UserId.Value,
                         a.EmployeeCode.Value,
                         a.FullName,
                         a.PositionName,
                         a.BusinessUnitName,
                         a.Sequence,
                         a.DelegateeId?.Value,
                         a.Status,
                         a.ActionAt.ToThaiDateString(),
                         a.Remark,
                         a.IsCurrentApprover(a.Type),
                         a.IsUnableToPerformDuties,
                         a.CommitteePositionsCode.HasValue ? (string?)a.CommitteePositionsCode : string.Empty,
                         a.CommitteePosition != null ? a.CommitteePosition?.Label : string.Empty,
                         a.AuditInfo.LastModifiedAt.HasValue ? a.AuditInfo.LastModifiedAt.Value.ToThaiDateString() : a.AuditInfo.CreatedAt.ToThaiDateString()))
                     .FirstOrDefault();
    }

    private static T GetValue<T>(bool condition, T valueIfTrue, T valueIfFalse)
    {
        return condition ? valueIfTrue : valueIfFalse;
    }

    protected async ValueTask UpdateDocumentAsync(
        PInvite invite,
        PInvitedEntrepreneurs entrepreneur,
        Guid userId,
        Guid procurementId,
        bool isReplace,
        bool hasAcceptor,
        CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var getLastedDraftDocumentHistory = entrepreneur.LastedDocument;

        var lastedApprovalDocument = hasAcceptor
            ? entrepreneur.LastedWaitingApprovalDocument
            : getLastedDraftDocumentHistory;

        FileId? templateFileId = null;

        if (isReplace)
        {
            templateFileId = await documentService.GetDocumentTemplateAsync(
                         w => w.Group == DocumentTemplateGroups.INV, ct);
        }

        if (lastedApprovalDocument is not null || templateFileId is not null)
        {
            var committees = await this.dbContext.PJp005S
                                       .Where(c => c.ProcurementId == ProcurementId.From(procurementId))
                                       .SelectMany(s => s.Committees)
                                       .Where(w => w.GroupType == PJp005CommitteeGroupType.ProcurementCommittee)
                                       .ToArrayAsync(ct);

            if (!committees.Any())
            {
                this.ThrowError("ไม่พบข้อมูลผู้มีสิทธิ์จัดการข้อมูลหนังสือเชิญชวน");
            }

            var operators =
                committees.Select(s => s.SuUserId)
                          .ToArray();

            var dto = await this.MapToResponseMappingDtoAsync(invite, operators, userId, hasAcceptor, entrepreneur, ct);

            var sourceFileId = isReplace ? templateFileId!.Value : lastedApprovalDocument!.FileId;

            var replaceDocumentAsync =
                documentService.CopyDocumentTemplateAsync(
                        sourceFileId,
                        contents => OdtDocumentExtensions.ReplaceOdtDocument(contents, dto),
                        parentDirectory: $"{DocumentTemplateGroups.INV}/{entrepreneur.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                        cancellationToken: ct);

            var finalFileId = await replaceDocumentAsync;

            if (finalFileId is null)
            {
                this.ThrowError("ไม่สามารถคัดลอกเอกสารร่างได้");
            }

            await this.UpdateDocumentHistoryAsync(entrepreneur, invite.Status, finalFileId.Value, isReplace || hasAcceptor, skipCopy: true, ct);
        }
    }
}