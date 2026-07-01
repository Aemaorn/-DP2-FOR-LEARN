namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment;

using FluentValidation;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class CreateContractAmendmentRequest
{
    [FromClaim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public Guid ContractDraftVendorId { get; init; }

    public CmContractAmendmentType Type { get; set; }

    public string? Remark { get; set; }
}

public class CreateContractAmendmentRequestValidator : Validator<CreateContractAmendmentRequest>
{
    public CreateContractAmendmentRequestValidator()
    {
        this.RuleFor(r => r.Type)
            .IsInEnum()
            .WithMessage("Invalid amendment type.");
    }
}

public class CreateContractAmendmentEndpoint : EndpointBase<CreateContractAmendmentRequest, Created<Guid>>
{
    private readonly Dp2DbContext dbContext;

    public CreateContractAmendmentEndpoint(ILogger<CreateContractAmendmentEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment")
             .WithName("CreateContractAmendment")
             .Produces<Ok>()
             .Produces<NotFound>()
             .Accepts<CreateContractAmendmentRequest>("application/json"));

        this.Post("contract-amendment");
    }

    protected override async ValueTask<Created<Guid>> HandleRequestAsync(CreateContractAmendmentRequest req, CancellationToken ct)
    {
        await this.ValidateRequestAsync(req, ct);

        var amendment = CamContractAmendment.Create(ContractDraftVendorId.From(req.ContractDraftVendorId), req.Type, req.Remark);

        this.dbContext.CamContractAmendments.Add(amendment);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, amendment.Id.Value);
    }

    private async Task ValidateRequestAsync(CreateContractAmendmentRequest req, CancellationToken ct)
    {
        var validator = new CreateContractAmendmentRequestValidator();
        var validation = await validator.ValidateAsync(req, ct);

        if (!validation.IsValid)
        {
            var first = validation.Errors.First();
            this.ThrowError(r => r.Type, first.ErrorMessage);
        }

        await Task.CompletedTask;
    }
}
