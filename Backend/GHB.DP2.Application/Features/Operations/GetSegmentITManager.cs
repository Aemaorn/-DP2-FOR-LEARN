namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetSegmentITManager : EndpointBase<Results<Ok<OperationInfo>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetSegmentITManager(
        ILogger<GetSegmentITManager> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetSegmentITManager")
             .Produces<Ok<OperationInfo>>()
             .Produces<NotFound<string>>()
             .WithDescription("Get the default segment it manager information"));
        this.Get("/operations/default-segment-it-manager");
    }

    protected override async ValueTask<Results<Ok<OperationInfo>, NotFound<string>>> HandleRequestAsync(CancellationToken ct)
    {
        var result = await this.operationService.GetSegmentITManagerAsync(ct);

        return TypedResults.Ok(result);
    }
}