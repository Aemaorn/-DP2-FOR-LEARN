namespace GHB.DP2.Application.Features.Procurement;

using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record TestCommandTextRequest(
    string Program,
    SectionProcessType ProcessType,
    Guid UserId,
    string SupplyMethodCode,
    decimal Budget,
    string? SupplyMethodSpecialTypeCode = null);

public record TestCommandTextManagerResponse(
    string? PositionName,
    string? RefBankOrder,
    decimal? Budget,
    string? InRefCode,
    string? CommandNumber,
    decimal? CommandBudget);

public record TestCommandTextResponse(
    string CommandText,
    IEnumerable<TestCommandTextManagerResponse> Managers);

public class TestCommandTextEndpoint : EndpointBase<TestCommandTextRequest, Ok<TestCommandTextResponse>>
{
    private readonly IOperationService operationService;
    private readonly ICommandTextService commandTextService;
    private readonly Dp2DbContext dbContext;

    public TestCommandTextEndpoint(
        IOperationService operationService,
        ICommandTextService commandTextService,
        Dp2DbContext dbContext,
        ILogger<TestCommandTextEndpoint> logger)
        : base(logger)
    {
        this.operationService = operationService;
        this.commandTextService = commandTextService;
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Test"));
        this.Post("test/command-text");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<TestCommandTextResponse>> HandleRequestAsync(TestCommandTextRequest req, CancellationToken ct)
    {
        var supplyMethodSpecialTypeCode = req.SupplyMethodCode is SupplyMethodConstant.Eighty
            ? null
            : req.SupplyMethodSpecialTypeCode;

        var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
            req.ProcessType,
            req.UserId,
            req.Budget,
            req.SupplyMethodCode,
            supplyMethodSpecialTypeCode,
            ct);

        var managerList = managers.ToList();

        var commandNumber = managerList.FirstOrDefault()?.CommandNumber;

        var supplyMethodSpecialName = supplyMethodSpecialTypeCode is null
            ? null
            : await this.dbContext.SuParameters
                .Where(s => s.Code == ParameterCode.From(supplyMethodSpecialTypeCode))
                .Select(s => s.Label)
                .FirstOrDefaultAsync(ct);

        var commandText = this.commandTextService.GetCommandText(
            program: req.Program,
            managers: managerList,
            supplyMethodCode: ParameterCode.From(req.SupplyMethodCode),
            budget: req.Budget,
            supplyMethodSpecialType: supplyMethodSpecialTypeCode is null ? null : ParameterCode.From(supplyMethodSpecialTypeCode),
            supplyMethodSpecialName: supplyMethodSpecialName,
            commandNumber: commandNumber);

        var managerResponses = managerList.Select(m => new TestCommandTextManagerResponse(
            PositionName: m.PositionName,
            RefBankOrder: m.RefBankOrder,
            Budget: m.Budget,
            InRefCode: m.InRefCode,
            CommandNumber: m.CommandNumber,
            CommandBudget: m.CommandBudget));

        return TypedResults.Ok(new TestCommandTextResponse(commandText, managerResponses));
    }
}
