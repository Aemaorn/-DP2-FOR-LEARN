namespace GHB.DP2.Application.Features.Dropdown;

using FluentValidation;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetParentBusinessUnitRequest
{
    public string ParentId { get; init; }
}

public record GetParentBusinessUnitResponse(
    string Value,
    string Label);

public class GetParentBusinessUnitRequestValidator : Validator<GetParentBusinessUnitRequest>
{
    public GetParentBusinessUnitRequestValidator()
    {
        this.RuleFor(x => x.ParentId)
            .NotEmpty()
            .WithMessage("ParentId is required.");
    }
}

public class GetParentBusinessUnit(Dp2DbContext dbContext, ILogger<GetParentBusinessUnit> logger) : EndpointBase<GetParentBusinessUnitRequest, Ok<List<GetParentBusinessUnitResponse>>>(logger)
{
    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/businessunit/{ParentId}");
    }

    protected override async ValueTask<Ok<List<GetParentBusinessUnitResponse>>> HandleRequestAsync(
        GetParentBusinessUnitRequest req,
        CancellationToken ct)
    {
        var query = await dbContext.RawBusinessUnits
                                   .Where(r => r.ParentId == BusinessUnitId.From(req.ParentId))
                                   .Select(static r => new GetParentBusinessUnitResponse(r.Id.Value, r.Name))
                                   .AsNoTracking()
                                   .ToListAsync(ct);

        return TypedResults.Ok(query);
    }
}