namespace GHB.DP2.Application.Features.Procurement.PurchaseRequisition;

using GHB.DP2.Application.Features.Procurement.PurchaseRequisition.Abstract;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetTorScopeOfWorkQuery(
    Guid ProcurementId);

public record GetTorScopeOfWorkResponse(
    IEnumerable<ListTorScopeOfWorkResponse>? ScopeOfWorks,
    string? Reason
);

public record ListTorScopeOfWorkResponse(
    Guid? Id,
    int Sequence,
    string Name,
    string Description,
    int Quantity,
    ParameterCode? UnitCode);

public class GetTorScopeOfWorkEndpoint : PurchaseRequisitionEndpointBase<GetTorScopeOfWorkQuery, Results<Ok<GetTorScopeOfWorkResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetTorScopeOfWorkEndpoint(Dp2DbContext dbContext, ILogger<GetTorScopeOfWorkEndpoint> logger)
        : base(logger, dbContext)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/PurchaseRequisition"));
        this.Get("JorPor04/Procurement/{ProcurementId:guid}");
    }

    protected override async ValueTask<Results<Ok<GetTorScopeOfWorkResponse>, NotFound<string>>> HandleRequestAsync(GetTorScopeOfWorkQuery req, CancellationToken ct)
    {
        var procurementData = await this.dbContext.Procurements
                                        .Include(x => x.TorDrafts)
                                        .ThenInclude(x => x.PpTorDraftTechnicalSpecifications)
                                        .FirstOrDefaultAsync(x => x.Id == ProcurementId.From(req.ProcurementId), ct);

        if (procurementData == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล TOR");
        }

        var torDraft = procurementData.TorDrafts.FirstOrDefault(a => a.IsActive);

        var results = new GetTorScopeOfWorkResponse(
            torDraft?.PpTorDraftTechnicalSpecifications.OrderBy(x => x.Sequence).Select(
                    x =>
                        new ListTorScopeOfWorkResponse(
                            null,
                            (int)x.Sequence,
                            x.Name ?? string.Empty,
                            x.Description ?? string.Empty,
                            (int)x.Quantity,
                            x.UnitCode)),
            torDraft?.Reason);

        return TypedResults.Ok(results);
    }
}