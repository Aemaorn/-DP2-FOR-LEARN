namespace GHB.DP2.Application.Features.Procurement.Jp006;

using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PPurchaseOrder;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateJp006EntrepreneursRequest(
    Guid ProcurementId,
    Guid PurchaseOrderId,
    Guid VendorId,
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
    IEnumerable<ShareholderDto>? Shareholders
);

public record ShareholderDto(
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
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
    QualificationResultDto CoiCheckerResult,
    QualificationResultDto WatchlistCheckerResult,
    string? CheckType = null
);

public class CreateJp006EntrepreneursEndpoint : EndpointBase<CreateJp006EntrepreneursRequest, Results<Created<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateJp006EntrepreneursEndpoint(ILogger<CreateJp006EntrepreneursEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/jp006/{PurchaseOrderId:guid}/entrepreneurs");
        this.Description(b => b
                              .WithTags("Procurement/Jp006")
                              .WithName("CreateJp006Entrepreneurs")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>>> HandleRequestAsync(CreateJp006EntrepreneursRequest req, CancellationToken ct)
    {
        var purchaseOrder = await this.dbContext.PPurchaseOrder
                               .SingleOrDefaultAsync(x => x.Id == PurchaseOrderId.From(req.PurchaseOrderId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (purchaseOrder is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคำเชิญที่มีรหัส {req.PurchaseOrderId}");
        }

        var vendor = await this.dbContext.SuVendors.SingleOrDefaultAsync(x => x.Id == SuVendorId.From(req.VendorId), ct);

        if (vendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ขายที่มีรหัส {req.VendorId}");
        }

        var entity =
            PPurchaseOrderEntrepreneur
                .Create(
                    purchaseOrder.Id,
                    vendor.Id);

        if (req.Shareholders != null && req.Shareholders.Any())
        {
            var shareholders =
                req.Shareholders.SelectMany(s =>
                {
                    var checkTypes = s.CheckType != null
                        ? new[] { s.CheckType }
                        : new[] { "COI", "Watchlist" };

                    return checkTypes.Select(checkType =>
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
                            .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt));
                }).ToList();

            entity.AddPurchaseOrderEntrepreneurShareholderList(shareholders);
        }

        this.dbContext.PPurchaseOrderEntrepreneurs.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}