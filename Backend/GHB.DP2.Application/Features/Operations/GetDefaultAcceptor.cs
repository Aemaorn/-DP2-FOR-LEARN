namespace GHB.DP2.Application.Features.Operations;

using FluentValidation;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.SystemUtility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetDefaultAcceptorRequest(
    SectionProcessType ProcessType,
    string SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    decimal Budget,
    Guid UserId,
    bool SkipCurrentEmployee = true)
{
    public class Validator : Validator<GetDefaultAcceptorRequest>
    {
        public Validator()
        {
            this.RuleFor(r => r.ProcessType)
                .IsInEnum()
                .WithMessage("กรุณาระบุประเภทกระบวนการอนุมัติที่ถูกต้อง");

            this.RuleFor(r => r.SupplyMethodSpecialTypeCode);

            this.RuleFor(r => r.Budget)
                .GreaterThan(0)
                .WithMessage("งบประมาณต้องมากกว่า 0")
                .NotNull()
                .WithMessage("กรุณาระบุงบประมาณที่ต้องการ");

            this.RuleFor(r => r.UserId)
                .NotEqual(Guid.Empty)
                .WithMessage("กรุณาระบุรหัสผู้ใช้ที่ถูกต้อง");
        }
    }
}

public class GetDefaultAcceptorEndpoint : EndpointBase<GetDefaultAcceptorRequest, Results<Ok<IEnumerable<OperationInfo>>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetDefaultAcceptorEndpoint(
        ILogger<GetDefaultAcceptorEndpoint> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(r => r
                          .WithName("GetDefaultAcceptor")
                          .WithTags(nameof(Operations))
                          .Produces<Ok<IEnumerable<OperationInfo>>>()
                          .Produces<NotFound<string>>()
                          .WithSummary("Get Default Acceptor")
                          .WithDescription("Retrieve the default acceptor based on the user's primary business unit and section approvers."));

        this.AllowAnonymous();

        this.Get("/operations/default-acceptor");
    }

    protected override async ValueTask<Results<Ok<IEnumerable<OperationInfo>>, NotFound<string>>> HandleRequestAsync(GetDefaultAcceptorRequest req, CancellationToken ct)
    {
        var managers = await this.operationService.GetDefaultAcceptorAsync(
            req.ProcessType,
            req.UserId,
            req.Budget,
            req.SupplyMethodCode,
            req.SupplyMethodCode is SupplyMethodConstant.Eighty ? default : req.SupplyMethodSpecialTypeCode,
            ct,
            req.SkipCurrentEmployee);

        return TypedResults.Ok(managers);
    }
}