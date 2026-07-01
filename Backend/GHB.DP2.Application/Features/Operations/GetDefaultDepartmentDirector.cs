namespace GHB.DP2.Application.Features.Operations;

using FluentValidation;
using GHB.DP2.Application.Features.Operations.Dto;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record GetDefaultDepartmentDirectorRequest(string BusinessUnitId);

public class GetDefaultDepartmentDirectorRequestValidator
    : Validator<GetDefaultDepartmentDirectorRequest>
{
    public GetDefaultDepartmentDirectorRequestValidator()
    {
        this.RuleFor(r => r.BusinessUnitId)
            .NotEmpty()
            .WithMessage("กรุณาระบุ BusinessUnitId");
    }
}

public class GetDefaultDepartmentDirectorEndpoint
    : EndpointBase<GetDefaultDepartmentDirectorRequest, Results<Ok<OperationInfo>, NotFound<string>>>
{
    private readonly IOperationService operationService;

    public GetDefaultDepartmentDirectorEndpoint(
        ILogger<GetDefaultDepartmentDirectorEndpoint> logger,
        IOperationService operationService)
        : base(logger)
    {
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(r => r
            .WithName("GetDefaultDepartmentDirector")
            .WithTags(nameof(Operations))
            .Produces<Ok<OperationInfo>>()
            .Produces<NotFound<string>>()
            .WithSummary("Get Default Department Director")
            .WithDescription("Retrieve the default department director based on the business unit ID."));

        this.AllowAnonymous();

        this.Get("/operations/default-department-director");
    }

    protected override async ValueTask<Results<Ok<OperationInfo>, NotFound<string>>>
        HandleRequestAsync(GetDefaultDepartmentDirectorRequest req, CancellationToken ct)
    {
        var director = await this.operationService
            .GetDefaultDepartmentDirectorByBusinessUnitIdAsync(req.BusinessUnitId, ct);

        if (director == null)
        {
            return TypedResults.NotFound("ไม่พบผู้รับผิดชอบของหน่วยงานนี้");
        }

        return TypedResults.Ok(director);
    }
}