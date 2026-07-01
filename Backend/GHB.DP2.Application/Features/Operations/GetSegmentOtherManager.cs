namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetSegmentOtherManager : EndpointBase<Results<Ok<OperationInfo>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetSegmentOtherManager(
        ILogger<GetSegmentOtherManager> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetSegmentOtherManager")
             .Produces<Ok<OperationInfo>>()
             .Produces<NotFound<string>>()
             .WithDescription("Get the default segment other manager information"));
        this.Get("/operations/default-segment-other-manager");
    }

    protected override async ValueTask<Results<Ok<OperationInfo>, NotFound<string>>> HandleRequestAsync(CancellationToken ct)
    {
        var result = await this.operationService.GetSegmentOtherManagerAsync(ct);

        return TypedResults.Ok(result);
    }
}