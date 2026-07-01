namespace GHB.DP2.Application.Features.Procurement.Invite.Abstract;

using System.ComponentModel;
using System.Linq;
using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.Procurement;
using GHB.DP2.Application.Services.Document;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using GreatFriends.ThaiBahtText;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record QualificationResultDto(
    [property: Description("ผลการตรวจสอบ")]
    QualificationResult Result,
    [property: Description("วันที่ตรวจสอบ")]
    DateTimeOffset ResultAt,
    [property: Description("หมายเหตุ")] string? Remark);

public abstract partial class InviteEndpointBase<TRequest, TResponse> : EndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;

    protected InviteEndpointBase(
        Dp2DbContext dbContext,
        ILogger logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    protected async Task<PInvite> GetPInviteById(PInviteId id, ProcurementId procurementId, CancellationToken ct)
    {
        var invite =
            await this.dbContext.PInvites
                      .Include(x => x.Procurement)
                      .ThenInclude(p => p.Jp005)
                      .ThenInclude(jp005 => jp005.ProcurementSuppliesDivisions)
                      .Include(x => x.Acceptors)
                      .ThenInclude(pInviteAcceptors => pInviteAcceptors.CommitteePosition)
                      .Include(x => x.InvitedEntrepreneurs)
                      .ThenInclude(pInvitedEntrepreneurs => pInvitedEntrepreneurs.Vendor)
                      .Include(pInvite => pInvite.InvitedEntrepreneurs)
                      .ThenInclude(pInvitedEntrepreneurs => pInvitedEntrepreneurs.InvitedEntrepreneurShareholders)
                      .ThenInclude(sh => sh.InvitedEntrepreneurShareholderCheckers)
                      .Include(x => x.InvitedEntrepreneurs)
                      .ThenInclude(ie => ie.DocumentHistories)
                      .Include(invites => invites.Procurement)
                      .ThenInclude(procurement => procurement.Department)
                      .Include(invites => invites.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethod)
                      .Include(invites => invites.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethodType)
                      .Include(invites => invites.Procurement)
                      .ThenInclude(procurement => procurement.SupplyMethodSpecialType)
                      .Include(invites => invites.Procurement)
                      .ThenInclude(procurement => procurement.Plan)
                      .Include(x => x.InvitedEntrepreneurs)
                      .ThenInclude(ie => ie.InvitedEntrepreneurCheckers)
                      .Include(x => x.Acceptors)
                      .ThenInclude(a => a.User)
                      .ThenInclude(u => u.Employee)
                      .AsSplitQuery()
                      .FirstOrDefaultAsync(x => x.Id == id && x.ProcurementId == procurementId, ct);

        if (invite is null)
        {
            this.ThrowError($"ไม่พบข้อมูล", StatusCodes.Status404NotFound);
        }

        return invite;
    }

    protected async ValueTask SetDefaultDocumentTemplate(PInvitedEntrepreneurs entrepreneur, PInviteStatus status, CancellationToken ct)
    {
        var documentService =
            this.Resolve<IDocumentService>();

        var inviteDocId = await documentService.GetDocumentTemplateAsync(
            w => w.Group == DocumentTemplateGroups.INV, ct);

        var createInviteHistory = PInvitedEntrepreneursDocumentHistory.Create(
            entrepreneur.Id,
            entrepreneur.DocumentHistories.Any() ? status : PInviteStatus.Draft,
            "1.0",
            (FileId)inviteDocId);

        entrepreneur.AddDocumentHistory(createInviteHistory);
    }

    /// <summary>
    /// Creates a new document history version with a copy of the file.
    /// The OLD version keeps the original fileId (snapshot of content before edit).
    /// The NEW version gets a copied file that user will continue editing.
    /// </summary>
    /// <param name="entrepreneur">The entrepreneur entity to add history to</param>
    /// <param name="status">The current invite status</param>
    /// <param name="inviteFileId">The current file ID being edited</param>
    /// <param name="isReplace">Whether this is a replacement document</param>
    /// <param name="skipCopy">If true, uses the fileId directly without copying (when caller already copied)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The new fileId that user should continue editing, or null if no change</returns>
    protected async Task<FileId?> UpdateDocumentHistoryAsync(
        PInvitedEntrepreneurs entrepreneur,
        PInviteStatus status,
        FileId inviteFileId,
        bool? isReplace = false,
        bool skipCopy = false,
        CancellationToken ct = default)
    {
        var latestHistory = entrepreneur.DocumentHistories
                                        .OrderVersions()
                                        .FirstOrDefault();

        if (latestHistory == null)
        {
            return null;
        }

        var newVersion = RunningDocumentVersion.IncrementDocumentVersion(
            latestHistory.Version,
            latestHistory.StatusState.ToString(),
            status.ToString());

        if (skipCopy)
        {
            // Caller already copied the file, create new version with the new fileId
            var addNewHistory = PInvitedEntrepreneursDocumentHistory.Create(
                entrepreneur.Id,
                status,
                newVersion,
                inviteFileId,
                isReplace);

            entrepreneur.AddDocumentHistory(addNewHistory);

            return inviteFileId;
        }
        else
        {
            // Copy current file to create NEW version for continued editing
            var documentService = this.Resolve<IDocumentService>();
            var copiedFileId = await documentService.CopyDocumentTemplateAsync(
                inviteFileId,
                parentDirectory: $"{DocumentTemplateGroups.INV}/{entrepreneur.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.odt",
                cancellationToken: ct);

            if (!copiedFileId.HasValue)
            {
                return null;
            }

            // OLD version (latestHistory) keeps original fileId - this is the snapshot before edit
            // (no change needed - it already points to inviteFileId)

            // Create NEW version pointing to copied file - user will continue editing this
            var addNewHistory = PInvitedEntrepreneursDocumentHistory.Create(
                entrepreneur.Id,
                status,
                newVersion,
                copiedFileId.Value,
                isReplace);

            entrepreneur.AddDocumentHistory(addNewHistory);

            return copiedFileId.Value;
        }
    }

    protected InviteResponseDto MapToResponseDto(
        PInvite invite,
        IEnumerable<UserId> operators,
        IEnumerable<UserId> procurementSuppliesDivisions,
        Guid userId)
    {
        var currentCommittees = invite.Acceptors
                        .Where(x => x.Type == AcceptorType.ProcurementCommittee)
                        .ToList();

        var currentApprover = invite.Acceptors
                        .Where(x => x.Type != AcceptorType.ProcurementCommittee)
                        .Select(DelegatorExtensions.DelegatorToAcceptor)
                        .ToList();

        var acceptors =
            currentApprover
                .Union(currentCommittees)
                .Map(MapToAcceptorResponse)
                .OrderBy(o => o.AcceptorType)
                .ThenBy(o => o.CommitteePositionsCode == ParameterCode.From(SuParameterCodeConstant.PosBoard006) ? 0 : 1)
                .ThenBy(o => o.Sequence)
                .ToArray();

        var inviteEntrepreneurs =
            invite.InvitedEntrepreneurs
                  .OrderBy(i => i.Sequence)
                  .Select(e =>
                  {
                      var coiChecker = e.InvitedEntrepreneurCheckers
                                        .OrderByDescending(c => c.ResultAt)
                                        .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                      var watchlistChecker = e.InvitedEntrepreneurCheckers
                                              .OrderByDescending(c => c.ResultAt)
                                              .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                      var coiCheckerResult = coiChecker is null
                          ? null
                          : new QualificationResultDto(
                              coiChecker.Result,
                              coiChecker.ResultAt,
                              coiChecker.Remark);

                      var watchlistCheckerResult = watchlistChecker is null
                          ? null
                          : new QualificationResultDto(
                              watchlistChecker.Result,
                              watchlistChecker.ResultAt,
                              watchlistChecker.Remark);

                      Func<PInvitedEntrepreneurShareholders, InviteEntrepreneurShareholderDto> selector = s =>
                      {
                          var shareholderCoiChecker =
                              s.InvitedEntrepreneurShareholderCheckers
                               .OrderByDescending(c => c.ResultAt)
                               .FirstOrDefault(c => c.CheckType == QualificationType.COI);

                          var shareholderWatchlistChecker =
                              s.InvitedEntrepreneurShareholderCheckers
                               .OrderByDescending(c => c.ResultAt)
                               .FirstOrDefault(c => c.CheckType == QualificationType.Watchlist);

                          var shareholderCoiCheckerResult = shareholderCoiChecker is null
                              ? null
                              : new QualificationResultDto(
                                  shareholderCoiChecker.Result,
                                  shareholderCoiChecker.ResultAt,
                                  shareholderCoiChecker.Remark);

                          var shareholderWatchlistCheckerResult = shareholderWatchlistChecker is null
                              ? null
                              : new QualificationResultDto(
                                  shareholderWatchlistChecker.Result,
                                  shareholderWatchlistChecker.ResultAt,
                                  shareholderWatchlistChecker.Remark);

                          var result =
                              new InviteEntrepreneurShareholderDto(
                                  s.Id.Value,
                                  s.Sequence,
                                  s.TaxId,
                                  s.FirstName,
                                  s.LastName,
                                  s.IsDirector,
                                  s.IsShareholder,
                                  s.WatchlistResult,
                                  s.WatchlistResultRemark,
                                  s.WatchlistResultAt,
                                  s.CoiResult,
                                  s.CoiResultRemark,
                                  s.CoiResultAt,
                                  s.EgpResult,
                                  s.EgpRemark,
                                  s.EgpResultAt,
                                  shareholderCoiCheckerResult,
                                  shareholderWatchlistCheckerResult);

                          return result;
                      };

                      var lastedHistory = e.DocumentHistories
                                           .OrderVersions()
                                           .FirstOrDefault();

                      var isReplacedDoc = e.DocumentHistories.Any(d => d.IsReplaced);

                      var documentVersions = e.DocumentHistories
                          .OrderVersions()
                          .Select((d, index) => new DocumentVersionResponse(
                              d.FileId.Value,
                              d.Version,
                              d.CreatedAt,
                              d.CreatedByName ?? string.Empty,
                              index == 0))
                          .ToArray();

                      return new InviteEntrepreneurDto(
                          e.Id.Value,
                          e.Vendor.Id.Value,
                          e.Sequence,
                          e.Vendor.TaxpayerIdentificationNo,
                          e.Vendor.EntrepreneurTypeInfo.Label,
                          e.Vendor.EstablishmentName,
                          e.Vendor.Email,
                          e.WatchlistResult,
                          e.WatchlistResultRemark,
                          e.WatchlistResultAt,
                          e.CoiResult,
                          e.CoiResultRemark,
                          e.CoiResultAt,
                          e.EgpResult,
                          e.EgpResultRemark,
                          e.EgpResultAt,
                          e.EmailSend,
                          e.Vendor.Nationality,
                          e.Vendor.Type,
                          e.Vendor.Tel,
                          coiCheckerResult,
                          watchlistCheckerResult,
                          [
                              .. e.InvitedEntrepreneurShareholders
                                  .Select(selector)
                          ],
                          e.Email,
                          e.EmailTemplate,
                          e.Attachments.Select(a => new EmailAttachment(
                              a.Id.Value,
                              a.FileName,
                              a.FileId,
                              a.Sequence)).ToArray(),
                          lastedHistory?.FileId.Value,
                          false,
                          documentVersions,
                          e.Vendor.SapBranchNumber);
                  });

        var hasEditPermission =
            operators.Any(c => c == userId) ||
            procurementSuppliesDivisions.Any(c => c == userId);

        return new InviteResponseDto(
            new ProcurementDto(
                invite.Procurement.PlanId.HasValue ? (Guid)invite.Procurement.PlanId : null,
                invite.Procurement.ProcurementNumber,
                invite.Procurement.Type,
                invite.Procurement.Step,
                invite.Procurement.Department.Name,
                invite.Procurement.DepartmentId,
                invite.Procurement.Plan?.PlanNumber.ToString() ?? string.Empty,
                invite.Procurement.Name,
                invite.Procurement.Budget,
                invite.Procurement.Budget.ThaiBahtText(),
                invite.Procurement.BudgetYear,
                invite.Procurement.SupplyMethod.Label,
                invite.Procurement.SupplyMethodCode,
                invite.Procurement.SupplyMethodType?.Label ?? string.Empty,
                invite.Procurement.SupplyMethodTypeCode,
                invite.Procurement.SupplyMethodSpecialType?.Label ?? string.Empty,
                invite.Procurement.SupplyMethodSpecialTypeCode,
                invite.Procurement.Status,
                invite.Procurement.ExpectingProcurementAt,
                invite.Procurement.IsStock,
                invite.Procurement.IsCommercialMaterial,
                invite.Procurement.Plan?.Type,
                invite.Procurement.ProcessType),
            invite.Id.Value,
            invite.ProcurementId.Value,
            invite.IsInvite,
            invite.SubmitProposalStartDate,
            invite.SubmitProposalEndDate,
            invite.SubmitProposalStartTime,
            invite.SubmitProposalEndTime,
            invite.NeedToKnowWithinDate,
            invite.ClarifyDetailViaDate,
            invite.PhoneNumber,
            invite.Status,
            acceptors,
            inviteEntrepreneurs,
            false,
            hasEditPermission,
            invite.DocumentDate);
    }

    private static AcceptorInviteResponseDto MapToAcceptorResponse(PInviteAcceptors acceptor)
    {
        return new AcceptorInviteResponseDto(
                 acceptor.Id.Value,
                 acceptor.Type,
                 acceptor.UserId.Value,
                 acceptor.EmployeeCode.Value,
                 acceptor.FullName,
                 acceptor.PositionName,
                 acceptor.BusinessUnitName,
                 acceptor.Sequence,
                 acceptor.Delegatee?.SuUserId.Value,
                 acceptor.Status,
                 acceptor.ActionAt,
                 acceptor.Remark,
                 acceptor.IsCurrentApprover(acceptor.Type),
                 acceptor.IsUnableToPerformDuties,
                 acceptor.CommitteePositionsCode,
                 acceptor.CommitteePosition?.Label);
    }
}