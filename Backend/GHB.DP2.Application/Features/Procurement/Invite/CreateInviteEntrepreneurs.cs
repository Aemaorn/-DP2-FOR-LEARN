namespace GHB.DP2.Application.Features.Procurement.Invite;

using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.Procurement.PInvite;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateInviteEntrepreneursRequest(
    Guid ProcurementId,
    Guid InviteId,
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
    QualificationResultDto WatchlistCheckerResult
);

public class CreateInviteEntrepreneursEndpoint : EndpointBase<CreateInviteEntrepreneursRequest, Results<Created<Guid>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateInviteEntrepreneursEndpoint(ILogger<CreateInviteEntrepreneursEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Post("procurement/{ProcurementId:guid}/invite/{InviteId:guid}/invite-entrepreneurs");
        this.Description(b => b
                              .WithTags("Procurement/Invite")
                              .WithName("CreateInviteEntrepreneurs")
                              .AllowAnonymous()
                              .Produces<Created<Guid>>(StatusCodes.Status201Created)
                              .ProducesProblem(StatusCodes.Status400BadRequest)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Created<Guid>, NotFound<string>>> HandleRequestAsync(CreateInviteEntrepreneursRequest req, CancellationToken ct)
    {
        var invite = await this.dbContext.PInvites
                               .SingleOrDefaultAsync(x => x.Id == PInviteId.From(req.InviteId) && x.ProcurementId == ProcurementId.From(req.ProcurementId), ct);

        if (invite is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลคำเชิญที่มีรหัส {req.InviteId}");
        }

        var vendor = await this.dbContext.SuVendors.SingleOrDefaultAsync(x => x.Id == SuVendorId.From(req.VendorId), ct);

        if (vendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ขายที่มีรหัส {req.VendorId}");
        }

        var entity =
            PInvitedEntrepreneurs
                .Create(
                    invite,
                    vendor,
                    req.Sequence,
                    req.EmailSend)
                .SetWatchlist(req.WatchlistResult, req.WatchlistResultRemark, req.WatchlistResultAt)
                .SetCoi(req.CoiResult, req.CoiResultRemark, req.CoiResultAt)
                .SetEgp(req.EgpResult, req.EgpResultRemark, req.EgpResultAt);

        if (req.Shareholders != null && req.Shareholders.Any())
        {
            var shareholders =
                req.Shareholders.SelectMany(s => new[]
                {
                    PInvitedEntrepreneurShareholders
                        .Create(
                            s.Sequence,
                            s.TaxId,
                            s.FirstName,
                            s.LastName,
                            s.IsDirector,
                            s.IsShareholder,
                            s.IsJuristic)
                        .SetCheckType("COI")
                        .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                        .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                        .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt),
                    PInvitedEntrepreneurShareholders
                        .Create(
                            s.Sequence,
                            s.TaxId,
                            s.FirstName,
                            s.LastName,
                            s.IsDirector,
                            s.IsShareholder,
                            s.IsJuristic)
                        .SetCheckType("Watchlist")
                        .SetWatchlist(s.WatchlistResult, s.WatchlistResultRemark, s.WatchlistResultAt)
                        .SetCoi(s.CoiResult, s.CoiResultRemark, s.CoiResultAt)
                        .SetEgp(s.EgpResult, s.EgpRemark, s.EgpResultAt),
                }).ToList();

            entity.AddInvitedEntrepreneurShareholderList(shareholders);
        }

        this.dbContext.PInvitedEntrepreneurs.Add(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, entity.Id.Value);
    }
}