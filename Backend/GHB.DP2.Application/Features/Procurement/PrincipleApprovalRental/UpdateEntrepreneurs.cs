namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateEntrepreneursEndpoint : EndpointBase<EntrepreneursRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateEntrepreneursEndpoint(ILogger<UpdateEntrepreneursEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/entrepreneurs/{id:guid}");
        this.Description(b => b
            .WithTags("Procurement/PrincipleApprovalRental")
            .WithName("UpdatePrincipleApprovalRentalEntrepreneurs")
            .Produces<Ok<Guid>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(EntrepreneursRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPrincipleApprovalRentalEntrepreneurs
            .Include(e => e.Checkers)
            .Include(e => e.EntrepreneursShareholders)
                .ThenInclude(sh => sh.Checkers)
            .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalEntrepreneursId.From(req.Id.Value) && x.PPrincipleApprovalRental.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId) && x.PPrincipleApprovalRental.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

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
            entity.AddChecker(QualificationType.COI, req.CoiCheckerResult.Result, req.CoiCheckerResult.ResultAt, req.CoiCheckerResult.Remark);
        }

        if (req.WatchlistCheckerResult is not null)
        {
            entity.AddChecker(QualificationType.Watchlist, req.WatchlistCheckerResult.Result, req.WatchlistCheckerResult.ResultAt, req.WatchlistCheckerResult.Remark);
        }

        UpdateShareholderList(entity, req);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateShareholderList(PPrincipleApprovalRentalEntrepreneurs entrepreneurs, EntrepreneursRequest req)
    {
        if (req.Shareholders == null || req.Shareholders.Length == 0)
        {
            var all = entrepreneurs.EntrepreneursShareholders.ToList();
            foreach (var shareholder in all)
            {
                entrepreneurs.RemoveShareholder(shareholder);
            }

            return;
        }

        var allKnownIds = req.Shareholders
            .SelectMany(s => new[] { s.CoiId, s.WatchlistId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = entrepreneurs.EntrepreneursShareholders
            .Where(a => !allKnownIds.Contains(a.Id.Value))
            .ToList();

        foreach (var shareholder in toRemove)
        {
            entrepreneurs.RemoveShareholder(shareholder);
        }

        foreach (var s in req.Shareholders)
        {
            var processTypes = s.CheckType != null
                ? new[] { s.CheckType }
                : new[] { "COI", "Watchlist" };

            foreach (var checkType in processTypes)
            {
                var id = checkType == "COI" ? s.CoiId : s.WatchlistId;
                var existing = id.HasValue
                    ? entrepreneurs.EntrepreneursShareholders.FirstOrDefault(a => a.Id == PPrincipleApprovalRentalEntrepreneursShareholdersId.From(id.Value))
                    : null;

                if (existing == null)
                {
                    CreateNewShareholder(entrepreneurs, s, checkType);
                }
                else
                {
                    UpdateExistingShareholder(existing, s);
                }
            }
        }
    }

    private static void CreateNewShareholder(PPrincipleApprovalRentalEntrepreneurs entrepreneurs, Dto.ShareholderDto s, string checkType)
    {
        var newShareholder = PPrincipleApprovalRentalEntrepreneursShareholders
            .Create(s.Sequence, s.TaxId, s.FirstName, s.LastName, s.IsDirector, s.IsShareholder, s.IsJuristic)
            .SetCheckType(checkType)
            .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
            .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
            .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            newShareholder.AddChecker(QualificationType.COI, s.CoiCheckerResult.Result, s.CoiCheckerResult.ResultAt, s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            newShareholder.AddChecker(QualificationType.Watchlist, s.WatchlistCheckerResult.Result, s.WatchlistCheckerResult.ResultAt, s.WatchlistCheckerResult.Remark);
        }

        entrepreneurs.AddShareholder(newShareholder);
    }

    private static void UpdateExistingShareholder(PPrincipleApprovalRentalEntrepreneursShareholders existing, Dto.ShareholderDto s)
    {
        existing.Update(s.Sequence, s.TaxId, s.FirstName, s.LastName, s.IsDirector, s.IsShareholder, s.IsJuristic)
                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            existing.AddChecker(QualificationType.COI, s.CoiCheckerResult.Result, s.CoiCheckerResult.ResultAt, s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            existing.AddChecker(QualificationType.Watchlist, s.WatchlistCheckerResult.Result, s.WatchlistCheckerResult.ResultAt, s.WatchlistCheckerResult.Remark);
        }
    }
}