namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetSegmentAccountingMembers : EndpointBase<Ok<IEnumerable<OperationInfo>>>
{
    private readonly IOperationService operationService;

    public GetSegmentAccountingMembers(
        ILogger<GetSegmentAccountingMembers> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetSegmentAccountingMembers")
             .Produces<Ok<IEnumerable<OperationInfo>>>()
             .WithDescription("Get all members of the accounting segment"));
        this.Get("/operations/default-segment-accounting-members");
    }

    protected override async ValueTask<Ok<IEnumerable<OperationInfo>>> HandleRequestAsync(CancellationToken ct)
    {
        var result = await this.operationService.GetSegmentAccountingMembersAsync(ct);

        return TypedResults.Ok(result);
    }
}
