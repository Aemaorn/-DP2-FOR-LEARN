namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetSegmentContractManager : EndpointBase<Results<Ok<OperationInfo>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetSegmentContractManager(
        ILogger<GetSegmentContractManager> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetSegmentContractManager")
             .Produces<Ok<OperationInfo>>()
             .Produces<NotFound<string>>()
             .WithDescription("Get the default segment contract manager information"));
        this.Get("/operations/default-segment-contract-manager");
    }

    protected override async ValueTask<Results<Ok<OperationInfo>, NotFound<string>>> HandleRequestAsync(CancellationToken ct)
    {
        var result = await this.operationService.GetSegmentContractManagerAsync(ct);

        return TypedResults.Ok(result);
    }
}