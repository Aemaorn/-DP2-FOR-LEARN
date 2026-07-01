namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetDefaultJorPorDirector : EndpointBase<Results<Ok<OperationInfo>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetDefaultJorPorDirector(
        ILogger<GetDefaultJorPorDirector> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetDefaultJorPorDirector")
             .Produces<Ok<OperationInfo>>()
             .Produces<NotFound<string>>()
             .WithDescription("Get the default JorPor director information"));
        this.Get("/operations/default-jorpor-director");
    }

    protected override async ValueTask<Results<Ok<OperationInfo>, NotFound<string>>> HandleRequestAsync(CancellationToken ct)
    {
        var result = await this.operationService.GetDefaultJorPorDirectorAsync(ct);

        if (result is null)
        {
            return TypedResults.NotFound("ไม่พบผู้อำนวยการฝ่ายจัดหาและการพัสดุ (จพ.)");
        }

        return TypedResults.Ok(result);
    }
}