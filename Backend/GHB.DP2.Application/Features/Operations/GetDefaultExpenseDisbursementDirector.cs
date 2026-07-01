namespace GHB.DP2.Application.Features.Operations;

using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class GetDefaultExpenseDisbursementDirector : EndpointBase<Results<Ok<OperationInfo>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetDefaultExpenseDisbursementDirector(
        ILogger<GetDefaultExpenseDisbursementDirector> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(x =>
            x.WithTags("Operations")
             .WithName("GetDefaultExpenseDisbursementDirector")
             .Produces<Ok<OperationInfo>>()
             .Produces<NotFound<string>>()
             .WithDescription("Get the default ExpenseDisbursement director information"));
        this.Get("/operations/default-expense-disbursement-director");
    }

    protected override async ValueTask<Results<Ok<OperationInfo>, NotFound<string>>> HandleRequestAsync(CancellationToken ct)
    {
        var result = await this.operationService.GetDefaultExpenseDisbursementDirectorAsync(ct);

        if (result is null)
        {
            return TypedResults.NotFound("ไม่พบผู้รับผิดชอบฝ่ายเบิกจ่ายค่าใช้จ่าย");
        }

        return TypedResults.Ok(result);
    }
}