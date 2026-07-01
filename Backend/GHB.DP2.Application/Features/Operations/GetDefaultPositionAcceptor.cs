namespace GHB.DP2.Application.Features.Operations;

using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetDefaultPositionAcceptorRequest(
    SectionProcessType ProcessType,
    string SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    decimal Budget,
    Guid UserId)
{
    public class Validator : Validator<GetDefaultPositionAcceptorRequest>
    {
        public Validator()
        {
            this.RuleFor(r => r.ProcessType)
                .IsInEnum()
                .WithMessage("กรุณาระบุประเภทกระบวนการอนุมัติที่ถูกต้อง");

            this.RuleFor(r => r.SupplyMethodSpecialTypeCode);

            this.RuleFor(r => r.Budget)
                .GreaterThan(0)
                .NotNull()
                .WithMessage("กรุณาระบุงบประมาณที่ต้องการ");

            this.RuleFor(r => r.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("กรุณาระบุรหัสผู้ใช้ที่ถูกต้อง");
        }
    }
}

public class GetDefaultPositionAcceptorEndpoint : EndpointBase<GetDefaultPositionAcceptorRequest, Results<Ok<IEnumerable<OperationPositionInfo>>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetDefaultPositionAcceptorEndpoint(
        ILogger<GetDefaultPositionAcceptorEndpoint> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(r => r
                          .WithName("GetDefaultPositionAcceptor")
                          .WithTags(nameof(Operations))
                          .Produces<Ok<IEnumerable<OperationPositionInfo>>>()
                          .Produces<NotFound<string>>()
                          .WithSummary("Get Default Position Acceptor")
                          .WithDescription("Retrieve the default position acceptor based on the user's primary business unit and section approvers."));

        this.AllowAnonymous();

        this.Get("/operations/default-position-acceptor");
    }

    protected override async ValueTask<Results<Ok<IEnumerable<OperationPositionInfo>>, NotFound<string>>> HandleRequestAsync(GetDefaultPositionAcceptorRequest req, CancellationToken ct)
    {
        var managers = await this.operationService.GetDefaultAcceptorPositionAsync(
            req.ProcessType,
            req.UserId,
            req.Budget,
            req.SupplyMethodCode,
            req.SupplyMethodCode is SupplyMethodConstant.Eighty ? default : req.SupplyMethodSpecialTypeCode,
            ct);

        return TypedResults.Ok(managers);
    }
}