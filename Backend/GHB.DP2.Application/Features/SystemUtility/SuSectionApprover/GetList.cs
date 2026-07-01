namespace GHB.DP2.Application.Features.SystemUtility.SuSectionApprover;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListSuSectionApproverRequest(
    string? Keyword,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    int PageNumber,
    int PageSize);

public record GetListSuSectionApproverResponse(
    string Id,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethod,
    string? SupplyMethodSpecialTypeCode,
    string? SupplyMethodSpecialType);

public class GetListSuSectionApprover : EndpointBase<GetListSuSectionApproverRequest, Ok<PaginatedQueryResult<GetListSuSectionApproverResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListSuSectionApprover(
        Dp2DbContext dbContext,
        ILogger<GetListSuSectionApprover> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSectionApprover"));
        this.Get("/st/section-approver");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListSuSectionApproverResponse>>> HandleRequestAsync(GetListSuSectionApproverRequest req, CancellationToken ct)
    {
        var listData = this.dbContext.SuSections
                           .Include(x => x.SupplyMethod)
                           .Include(x => x.SupplyMethodSpecialType)
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x =>
                               EF.Functions.ILike(x.RefBankOrder, $"%{req.Keyword}%") ||
                               (x.Remark != null && EF.Functions.ILike(x.Remark, $"%{req.Keyword}%")))
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SupplyMethodCode), x =>
                               x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SupplyMethodSpecialTypeCode), x =>
                               x.SupplyMethodSpecialTypeCode == ParameterCode.From(req.SupplyMethodSpecialTypeCode!));

        var paginated = await PaginatedList<GHB.DP2.Domain.SystemUtility.SuSection>
            .CreateAsync(
                listData,
                req.PageNumber,
                req.PageSize,
                ct);

        var result = paginated.ToResult(static x => new GetListSuSectionApproverResponse(
            x.Id.Value,
            x.RefBankOrder,
            x.MaximumBudget,
            x.Remark,
            x.SupplyMethodCode?.Value,
            x.SupplyMethod?.Label,
            x.SupplyMethodSpecialTypeCode?.Value,
            x.SupplyMethodSpecialType?.Label));

        return TypedResults.Ok(result);
    }
}
