namespace GHB.DP2.Application.Features.SystemUtility.SuSection;

using GHB.DP2.Application.Features.SystemUtility.SuSection.DTO;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetSuSectionByIdRequest(string Id);

public class GetSuSectionById : EndpointBase<GetSuSectionByIdRequest, Results<Ok<GetSuSectionResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuSectionById(
        Dp2DbContext dbContext,
        ILogger<GetSuSectionById> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSection"));
        this.Get("/st/sections/{Id}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<GetSuSectionResponse>, NotFound<string>>> HandleRequestAsync(GetSuSectionByIdRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuSections
                             .Include(s => s.Approvers)
                             .Include(s => s.SupplyMethod)
                             .Include(s => s.SupplyMethodSpecialType)
                             .FirstOrDefaultAsync(x => x.Id == SectionId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"SuSection with Id {req.Id} not found");
        }

        var approvers = data.Approvers.Select(a => new GetSuSectionApproverResponse(
            a.Id.Value,
            a.ProcessType,
            a.PositionName,
            a.ShortPosition,
            a.InRefCode,
            a.Budget,
            a.SuSectionId.Value,
            a.CommandText));

        var response = new GetSuSectionResponse(
            data.Id.Value,
            data.Id.Value,
            data.RefBankOrder,
            data.MaximumBudget,
            data.Remark,
            data.SupplyMethodCode?.Value,
            data.SupplyMethodSpecialTypeCode?.Value,
            approvers);

        return TypedResults.Ok(response);
    }
}