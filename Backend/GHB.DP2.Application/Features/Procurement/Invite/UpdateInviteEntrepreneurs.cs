namespace GHB.DP2.Application.Features.Procurement.Invite;

using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateInviteEntrepreneursRequest(
    Guid ProcurementId,
    Guid InviteId,
    Guid Id,
    int Sequence,
    bool EmailSend,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpResultRemark,
    DateTimeOffset? EgpResultAt,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    UpdateShareholderDto[]? Shareholders);

public record UpdateShareholderDto(
    Guid? CoiId,
    Guid? WatchlistId,
    int Sequence,
    string? TaxId,
    string? FirstName,
    string? LastName,
    bool? IsDirector,
    bool? IsShareholder,
    bool? IsJuristic,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpResultAt,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult);

public class UpdateInviteEntrepreneursEndpoint : EndpointBase<UpdateInviteEntrepreneursRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateInviteEntrepreneursEndpoint(ILogger<UpdateInviteEntrepreneursEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/invite-entrepreneurs/{Id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/Invite")
                              .WithName("UpdateInviteEntrepreneurs")
                              .AllowAnonymous()
                              .Produces<Ok<Guid>>(StatusCodes.Status200OK)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(UpdateInviteEntrepreneursRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PInvitedEntrepreneurs
                               .Include(e => e.Vendor)
                               .Include(e => e.InvitedEntrepreneurShareholders)
                               .ThenInclude(sh => sh.InvitedEntrepreneurShareholderCheckers)
                               .SingleOrDefaultAsync(
                                   x => x.Id == PInvitedEntrepreneursId.From(req.Id) && x.Invite.Id == PInviteId.From(req.InviteId) && x.Invite.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการที่มีรหัส {req.Id}");
        }

        entity.Update(req.Sequence, req.EmailSend)
              .SetWatchlist(req.WatchlistResult, req.WatchlistResultRemark, req.WatchlistResultAt)
              .SetCoi(req.CoiResult, req.CoiResultRemark, req.CoiResultAt)
              .SetEgp(req.EgpResult, req.EgpResultRemark, req.EgpResultAt);

        if (req.CoiCheckerResult is not null)
        {
            entity.AddChecker(
                QualificationType.COI,
                req.CoiCheckerResult.Result,
                req.CoiCheckerResult.ResultAt,
                req.CoiCheckerResult.Remark);
        }

        if (req.WatchlistCheckerResult is not null)
        {
            entity.AddChecker(
                QualificationType.Watchlist,
                req.WatchlistCheckerResult.Result,
                req.WatchlistCheckerResult.ResultAt,
                req.WatchlistCheckerResult.Remark);
        }

        UpdateShareholderList(entity, req);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateShareholderList(PInvitedEntrepreneurs invitedEntrepreneurs, UpdateInviteEntrepreneursRequest req)
    {
        if (IsShareholderListEmpty(req.Shareholders))
        {
            RemoveAllShareholders(invitedEntrepreneurs);

            return;
        }

        RemoveObsoleteShareholders(invitedEntrepreneurs, req.Shareholders);
        ProcessShareholderUpdates(invitedEntrepreneurs, req.Shareholders);
    }

    private static bool IsShareholderListEmpty(UpdateShareholderDto[]? shareholders)
    {
        return shareholders == null || shareholders.Length == 0;
    }

    private static void RemoveAllShareholders(PInvitedEntrepreneurs invitedEntrepreneurs)
    {
        var all = invitedEntrepreneurs.InvitedEntrepreneurShareholders.ToList();

        foreach (var shareholder in all)
        {
            invitedEntrepreneurs.RemovePInviteEntrepreneursShareholder(shareholder.Id);
        }
    }

    private static void RemoveObsoleteShareholders(PInvitedEntrepreneurs invitedEntrepreneurs, UpdateShareholderDto[]? shareholders)
    {
        var allKnownIds = shareholders!
            .SelectMany(a => new[] { a.CoiId, a.WatchlistId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = invitedEntrepreneurs.InvitedEntrepreneurShareholders
                                           .Where(a => !allKnownIds.Contains(a.Id.Value))
                                           .ToList();

        foreach (var shareholder in toRemove)
        {
            invitedEntrepreneurs.RemovePInviteEntrepreneursShareholder(shareholder.Id);
        }
    }

    private static void ProcessShareholderUpdates(PInvitedEntrepreneurs invitedEntrepreneurs, UpdateShareholderDto[]? shareholders)
    {
        foreach (var shareholderRequest in shareholders!)
        {
            var existingCoi = shareholderRequest.CoiId.HasValue
                ? invitedEntrepreneurs.InvitedEntrepreneurShareholders
                    .FirstOrDefault(a => a.Id == PInvitedEntrepreneurShareholdersId.From(shareholderRequest.CoiId.Value))
                : null;

            if (existingCoi == null)
            {
                AddNewShareholder(invitedEntrepreneurs, shareholderRequest, "COI");
            }
            else
            {
                UpdateExistingShareholder(invitedEntrepreneurs, existingCoi, shareholderRequest);
            }

            var existingWatchlist = shareholderRequest.WatchlistId.HasValue
                ? invitedEntrepreneurs.InvitedEntrepreneurShareholders
                    .FirstOrDefault(a => a.Id == PInvitedEntrepreneurShareholdersId.From(shareholderRequest.WatchlistId.Value))
                : null;

            if (existingWatchlist == null)
            {
                AddNewShareholder(invitedEntrepreneurs, shareholderRequest, "Watchlist");
            }
            else
            {
                UpdateExistingShareholder(invitedEntrepreneurs, existingWatchlist, shareholderRequest);
            }
        }
    }

    private static void AddNewShareholder(PInvitedEntrepreneurs invitedEntrepreneurs, UpdateShareholderDto shareholderRequest, string checkType)
    {
        var newShareholder = CreateBaseShareholder(shareholderRequest, checkType);
        AddCheckerResults(newShareholder, shareholderRequest);
        invitedEntrepreneurs.AddInvitedEntrepreneurShareholder(newShareholder);
    }

    private static void UpdateExistingShareholder(PInvitedEntrepreneurs invitedEntrepreneurs, PInvitedEntrepreneurShareholders existing, UpdateShareholderDto shareholderRequest)
    {
        UpdateBaseShareholder(existing, shareholderRequest);
        AddCheckerResults(existing, shareholderRequest);
        invitedEntrepreneurs.UpdatePInviteEntrepreneursShareholder(existing);
    }

    private static PInvitedEntrepreneurShareholders CreateBaseShareholder(UpdateShareholderDto shareholderRequest, string checkType)
    {
        return PInvitedEntrepreneurShareholders
               .Create(
                   shareholderRequest.Sequence,
                   shareholderRequest.TaxId,
                   shareholderRequest.FirstName,
                   shareholderRequest.LastName,
                   shareholderRequest.IsDirector,
                   shareholderRequest.IsShareholder,
                   shareholderRequest.IsJuristic)
               .SetCheckType(checkType)
               .SetWatchlist(shareholderRequest.WatchlistResult, shareholderRequest.WatchlistResultRemark, shareholderRequest.WatchlistResultAt)
               .SetCoi(shareholderRequest.CoiResult, shareholderRequest.CoiResultRemark, shareholderRequest.CoiResultAt)
               .SetEgp(shareholderRequest.EgpResult, shareholderRequest.EgpRemark, shareholderRequest.EgpResultAt);
    }

    private static void UpdateBaseShareholder(PInvitedEntrepreneurShareholders existing, UpdateShareholderDto shareholderRequest)
    {
        existing.Update(
                    shareholderRequest.Sequence,
                    shareholderRequest.TaxId,
                    shareholderRequest.FirstName,
                    shareholderRequest.LastName,
                    shareholderRequest.IsDirector,
                    shareholderRequest.IsShareholder,
                    shareholderRequest.IsJuristic)
                .SetWatchlist(shareholderRequest.WatchlistResult, shareholderRequest.WatchlistResultRemark, shareholderRequest.WatchlistResultAt)
                .SetCoi(shareholderRequest.CoiResult, shareholderRequest.CoiResultRemark, shareholderRequest.CoiResultAt)
                .SetEgp(shareholderRequest.EgpResult, shareholderRequest.EgpRemark, shareholderRequest.EgpResultAt);
    }

    private static void AddCheckerResults(PInvitedEntrepreneurShareholders shareholder, UpdateShareholderDto shareholderRequest)
    {
        AddCoiCheckerResult(shareholder, shareholderRequest.CoiCheckerResult);
        AddWatchlistCheckerResult(shareholder, shareholderRequest.WatchlistCheckerResult);
    }

    private static void AddCoiCheckerResult(PInvitedEntrepreneurShareholders shareholder, QualificationResultDto? coiCheckerResult)
    {
        if (coiCheckerResult is not null)
        {
            shareholder.AddChecker(
                QualificationType.COI,
                coiCheckerResult.Result,
                coiCheckerResult.ResultAt,
                coiCheckerResult.Remark);
        }
    }

    private static void AddWatchlistCheckerResult(PInvitedEntrepreneurShareholders shareholder, QualificationResultDto? watchlistCheckerResult)
    {
        if (watchlistCheckerResult is not null)
        {
            shareholder.AddChecker(
                QualificationType.Watchlist,
                watchlistCheckerResult.Result,
                watchlistCheckerResult.ResultAt,
                watchlistCheckerResult.Remark);
        }
    }
}