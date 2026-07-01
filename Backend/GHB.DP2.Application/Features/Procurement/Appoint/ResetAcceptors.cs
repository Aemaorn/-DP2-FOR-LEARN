namespace GHB.DP2.Application.Features.Procurement.Appoint;

using FluentValidation;
using GHB.DP2.Domain.Procurement.PpAppoint;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ResetAcceptorByAppointIdRequest(Guid Id);

public class ResetAcceptorByAppointIdRequestValidator : Validator<ResetAcceptorByAppointIdRequest>
{
    public ResetAcceptorByAppointIdRequestValidator()
    {
        this.RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required.");
    }
}

public class ResetAcceptorEndpoint : EndpointBase<ResetAcceptorByAppointIdRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public ResetAcceptorEndpoint(
        Dp2DbContext dbContext,
        ILogger<ResetAcceptorEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Procurement/Appoint"));
        this.Delete("appointments/{id}/reset-acceptors");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(ResetAcceptorByAppointIdRequest req, CancellationToken ct)
    {
        var appoint = await this.dbContext.PpAppoints
                                .Include(a => a.Acceptors)
                                .FirstOrDefaultAsync(a => a.Id == PpAppointId.From(req.Id), ct);

        if (appoint is null)
        {
            return TypedResults.NoContent();
        }

        this.dbContext.PpAppointAcceptors.RemoveRange(appoint.Acceptors);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}