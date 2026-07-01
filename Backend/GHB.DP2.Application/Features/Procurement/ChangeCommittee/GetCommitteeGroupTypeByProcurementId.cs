namespace GHB.DP2.Application.Features.Procurement.ChangeCommittee;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record CommitteeGroupTypeRequest(Guid ProcurementId);

public class GetCommitteeGroupTypeByProcurementId : EndpointBase<CommitteeGroupTypeRequest, Results<Ok<IEnumerable<CommitteeGroupTypeServiceDto>>, NotFound<string>>>
{
    private readonly ICommitteeGroupTypeServiceService committeeGroupTypeServiceService;

    public GetCommitteeGroupTypeByProcurementId(
        ICommitteeGroupTypeServiceService committeeGroupTypeServiceService,
        ILogger<GetCommitteeGroupTypeByProcurementId> logger)
        : base(logger)
    {
        this.committeeGroupTypeServiceService = committeeGroupTypeServiceService;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("ChangeCommittee"));
        this.Get("/procurement/{ProcurementId:guid}/committee-group-type");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<IEnumerable<CommitteeGroupTypeServiceDto>>, NotFound<string>>> HandleRequestAsync(CommitteeGroupTypeRequest req, CancellationToken ct)
    {
        var response = await this.committeeGroupTypeServiceService.GetCommitteeGroupTypeByProcurementId(req.ProcurementId, ct);

        return TypedResults.Ok(response);
    }
}