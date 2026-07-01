namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetSuSectionApproverBySuSectionIdRequest(string SuSectionId);

public record GetSuSectionApproverItemResponse(
    string Id,
    string InRefCode,
    string PositionName,
    string ShortPosition,
    decimal Budget,
    SectionProcessType ProcessType,
    string CommandText,
    string SuSectionId,
    decimal? CommandBudget);

public record GetSuSectionApproverBySuSectionIdResponse(
    string Id,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethod,
    string? SupplyMethodSpecialTypeCode,
    string? SupplyMethodSpecialType,
    IEnumerable<GetSuSectionApproverItemResponse> Approvers);

public class GetSuSectionApproverBySuSectionId : EndpointBase<GetSuSectionApproverBySuSectionIdRequest, Results<Ok<GetSuSectionApproverBySuSectionIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuSectionApproverBySuSectionId(
        Dp2DbContext dbContext,
        ILogger<GetSuSectionApproverBySuSectionId> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Get("/st/section-approver/by-section/{SuSectionId}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Results<Ok<GetSuSectionApproverBySuSectionIdResponse>, NotFound<string>>> HandleRequestAsync(GetSuSectionApproverBySuSectionIdRequest req, CancellationToken ct)
    {
        var section = await this.dbContext.SuSections
                                .Include(s => s.Approvers)
                                .Include(s => s.SupplyMethod)
                                .Include(s => s.SupplyMethodSpecialType)
                                .FirstOrDefaultAsync(x => x.Id == SectionId.From(req.SuSectionId), ct);

        if (section is null)
        {
            return TypedResults.NotFound($"SuSection with Id {req.SuSectionId} not found");
        }

        var approvers = section.Approvers.Select(a => new GetSuSectionApproverItemResponse(
            a.Id.Value.ToString(),
            a.InRefCode,
            a.PositionName,
            a.ShortPosition,
            a.Budget,
            a.ProcessType,
            a.CommandText,
            a.SuSectionId.Value,
            a.CommandBudget));

        var response = new GetSuSectionApproverBySuSectionIdResponse(
            section.Id.Value,
            section.RefBankOrder,
            section.MaximumBudget,
            section.Remark,
            section.SupplyMethodCode?.Value,
            section.SupplyMethod?.Label,
            section.SupplyMethodSpecialTypeCode?.Value,
            section.SupplyMethodSpecialType?.Label,
            approvers);

        return TypedResults.Ok(response);
    }
}
