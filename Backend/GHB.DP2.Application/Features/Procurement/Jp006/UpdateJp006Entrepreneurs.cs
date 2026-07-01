namespace GHB.DP2.Application.Features.Procurement.Jp006;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record UpdateJp006EntrepreneursRequest(
    Guid ProcurementId,
    Guid PurchaseOrderId,
    Guid Id,
    int Sequence,
    bool EmailSended,
    EntrepreneurCheckConditions Coi,
    EntrepreneurCheckConditions Watchlist,
    EntrepreneurCheckConditions Egp,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    UpdateShareholderDto[]? Shareholder);

public record UpdateShareholderDto(
    Guid? CoiId,
    Guid? WatchlistId,
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
    bool? IsShareholder,
    bool? IsJuristic,
    bool? WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool? CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool? EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpResultAt,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    string? CheckType = null);

public class UpdateJp006EntrepreneursEndpoint : Jp006EndpointBase<UpdateJp006EntrepreneursRequest, Results<Ok<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateJp006EntrepreneursEndpoint(
        ILogger<AssigneePurchaseOrderEndpoint> logger,
        IOperationService operationService,
        ICommandTextService commandTextService,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, operationService, commandTextService, fileServiceClient, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{procurementId:guid}/jp006/{purchaseOrderId:guid}/jp006-entrepreneurs/{id:guid}");
        this.Description(b => b
                              .WithTags(nameof(Jp006))
                              .WithName("UpdatePJp006Entrepreneurs")
                              .Produces<Ok<Guid>>(StatusCodes.Status200OK)
                              .ProducesProblem(StatusCodes.Status400BadRequest));
    }

    protected override async ValueTask<Results<Ok<Guid>, NotFound<string>>> HandleRequestAsync(UpdateJp006EntrepreneursRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PJp006Entrepreneurs
                               .SingleOrDefaultAsync(
                                   x => x.Id == PurchaseOrderEntrepreneurId.From(req.Id) &&
                                        x.PurchaseOrderId == PurchaseOrderId.From(req.PurchaseOrderId) &&
                                        x.PPurchaseOrder.ProcurementId == ProcurementId.From(req.ProcurementId),
                                   ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ {req.Id}");
        }

        entity.SetWatchlistResult(req.Watchlist.Result, req.Watchlist.Remark, req.Watchlist.Date)
              .SetCoiResult(req.Coi.Result, req.Coi.Remark, req.Coi.Date)
              .SetEgpResult(req.Egp.Result, req.Egp.Remark, req.Egp.Date);

        if (req.CoiCheckerResult is not null)
        {
            entity
                .AddChecker(
                    QualificationType.COI,
                    req.CoiCheckerResult.Result,
                    req.CoiCheckerResult.ResultAt,
                    req.CoiCheckerResult.Remark);
        }

        if (req.WatchlistCheckerResult is not null)
        {
            entity
                .AddChecker(
                    QualificationType.Watchlist,
                    req.WatchlistCheckerResult.Result,
                    req.WatchlistCheckerResult.ResultAt,
                    req.WatchlistCheckerResult.Remark);
        }

        UpdateShareholderList(entity, req);

        this.dbContext.PJp006Entrepreneurs.Update(entity);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(entity.Id.Value);
    }

    private static void UpdateShareholderList(PPurchaseOrderEntrepreneur purchaseEntrepreneurs, UpdateJp006EntrepreneursRequest req)
    {
        if (req.Shareholder == null || req.Shareholder.Length == 0)
        {
            var all = purchaseEntrepreneurs.PurchaseOrderShareholders.ToList();

            foreach (var shareholder in all)
            {
                purchaseEntrepreneurs.RemovePurchaseOrderEntrepreneursShareholder(shareholder.Id);
            }

            return;
        }

        var allKnownIds = req.Shareholder
            .SelectMany(s => new[] { s.CoiId, s.WatchlistId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToHashSet();

        var toRemove = purchaseEntrepreneurs.PurchaseOrderShareholders
            .Where(a => !allKnownIds.Contains(a.Id.Value))
            .ToList();

        foreach (var shareholder in toRemove)
        {
            purchaseEntrepreneurs.RemovePurchaseOrderEntrepreneursShareholder(shareholder.Id);
        }

        foreach (var s in req.Shareholder)
        {
            var processTypes = s.CheckType != null
                ? new[] { s.CheckType }
                : new[] { "COI", "Watchlist" };

            foreach (var checkType in processTypes)
            {
                var id = checkType == "COI" ? s.CoiId : s.WatchlistId;
                var existing = id.HasValue
                    ? purchaseEntrepreneurs.PurchaseOrderShareholders
                        .FirstOrDefault(a => a.Id == PPurchaseOrderEntrepreneurShareholdersId.From(id.Value))
                    : null;

                if (existing == null)
                {
                    CreateNewShareholder(purchaseEntrepreneurs, s, checkType);
                }
                else
                {
                    UpdateExistingShareholder(purchaseEntrepreneurs, existing, s);
                }
            }
        }
    }

    private static void UpdateExistingShareholder(PPurchaseOrderEntrepreneur purchaseEntrepreneurs, PPurchaseOrderEntrepreneurShareholders existing, UpdateShareholderDto s)
    {
        existing.Update(
                    s.Sequence,
                    s.TaxId,
                    s.FirstName,
                    s.LastName,
                    s.IsDirector,
                    s.IsShareholder,
                    s.IsJuristic)
                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            existing.AddChecker(
                QualificationType.COI,
                s.CoiCheckerResult.Result,
                s.CoiCheckerResult.ResultAt,
                s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            existing.AddChecker(
                QualificationType.Watchlist,
                s.WatchlistCheckerResult.Result,
                s.WatchlistCheckerResult.ResultAt,
                s.WatchlistCheckerResult.Remark);
        }

        purchaseEntrepreneurs.UpdatePurchaseOrderEntrepreneursShareholder(existing);
    }

    private static void CreateNewShareholder(PPurchaseOrderEntrepreneur purchaseEntrepreneurs, UpdateShareholderDto s, string checkType)
    {
        var newShareholder =
            PPurchaseOrderEntrepreneurShareholders
                .Create(
                    s.Sequence,
                    s.TaxId,
                    s.FirstName,
                    s.LastName,
                    s.IsDirector,
                    s.IsShareholder,
                    s.IsJuristic)
                .SetCheckType(checkType)
                .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt);

        if (s.CoiCheckerResult is not null)
        {
            newShareholder.AddChecker(
                QualificationType.COI,
                s.CoiCheckerResult.Result,
                s.CoiCheckerResult.ResultAt,
                s.CoiCheckerResult.Remark);
        }

        if (s.WatchlistCheckerResult is not null)
        {
            newShareholder.AddChecker(
                QualificationType.Watchlist,
                s.WatchlistCheckerResult.Result,
                s.WatchlistCheckerResult.ResultAt,
                s.WatchlistCheckerResult.Remark);
        }

        purchaseEntrepreneurs.AddPurchaseOrderEntrepreneurShareholder(newShareholder);
    }
}