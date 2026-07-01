namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental;

using GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPrincipleApprovalRental;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CreateEntrepreneurEndpoint : EndpointBase<EntrepreneursRequest, Results<Created<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateEntrepreneurEndpoint(ILogger<CreateEntrepreneurEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{procurementId:guid}/principle-approval-rental/{principleApprovalRentalId:guid}/entrepreneurs");
        this.Description(b => b
            .WithTags("Procurement/PrincipleApprovalRental")
            .WithName("CreatePrincipleApprovalRentalEntrepreneurs")
            .Produces<Created<Guid>>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>>> HandleRequestAsync(EntrepreneursRequest req, CancellationToken ct)
    {
        var approvalRental = await this.dbContext.PPrincipleApprovalRentals
                               .SingleOrDefaultAsync(x => x.Id == PPrincipleApprovalRentalId.From(req.PrincipleApprovalRentalId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (approvalRental is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลขออนุมัติเช่ารหัสที่ {req.PrincipleApprovalRentalId}");
        }

        var vendor = await this.dbContext.SuVendors.SingleOrDefaultAsync(x => x.Id == SuVendorId.From(req.VendorId), ct);
        if (vendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการเสนอราคาที่มีรหัส {req.VendorId}");
        }

        var entrepreneurs = PPrincipleApprovalRentalEntrepreneurs.Create(
                                                                    vendor,
                                                                    req.Sequence,
                                                                    req.EmailSend)
                                                                .SetWatchlist(req.WatchlistResult, req.WatchlistResultRemark, req.WatchlistResultAt)
                                                                .SetCoi(req.CoiResult, req.CoiResultRemark, req.CoiResultAt)
                                                                .SetEgp(req.EgpResult, req.EgpResultRemark, req.EgpResultAt);

        if (req.CoiCheckerResult is not null)
        {
            entrepreneurs.AddChecker(QualificationType.COI, req.CoiCheckerResult.Result, req.CoiCheckerResult.ResultAt, req.CoiCheckerResult.Remark);
        }

        if (req.WatchlistCheckerResult is not null)
        {
            entrepreneurs.AddChecker(QualificationType.Watchlist, req.WatchlistCheckerResult.Result, req.WatchlistCheckerResult.ResultAt, req.WatchlistCheckerResult.Remark);
        }

        if (req.Shareholders != null && req.Shareholders.Any())
        {
            var shareholders = req.Shareholders.SelectMany(s =>
            {
                var checkTypes = s.CheckType != null
                    ? new[] { s.CheckType }
                    : new[] { "COI", "Watchlist" };

                return checkTypes.Select(checkType =>
                {
                    var sh = PPrincipleApprovalRentalEntrepreneursShareholders.Create(
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
                        sh.AddChecker(QualificationType.COI, s.CoiCheckerResult.Result, s.CoiCheckerResult.ResultAt, s.CoiCheckerResult.Remark);
                    }

                    if (s.WatchlistCheckerResult is not null)
                    {
                        sh.AddChecker(QualificationType.Watchlist, s.WatchlistCheckerResult.Result, s.WatchlistCheckerResult.ResultAt, s.WatchlistCheckerResult.Remark);
                    }

                    return sh;
                });
            }).ToList();

            foreach (var s in shareholders)
            {
                entrepreneurs.AddShareholder(s);
            }
        }

        approvalRental.AddEntrepreneur(entrepreneurs);

        await this.dbContext.SaveChangesAsync(ct);
        return TypedResults.Created(string.Empty, entrepreneurs.Id.Value);
    }
}