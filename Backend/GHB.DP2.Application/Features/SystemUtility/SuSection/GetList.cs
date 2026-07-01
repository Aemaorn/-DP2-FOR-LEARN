namespace GHB.DP2.Application.Features.SystemUtility.SuSection;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.SystemUtility.SuSection.DTO;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetListSuSectionRequest(
    string? Keyword,
    int PageNumber,
    int PageSize);

public class GetListSuSection : EndpointBase<GetListSuSectionRequest, Ok<PaginatedQueryResult<GetListSuSectionResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListSuSection(
        Dp2DbContext dbContext,
        ILogger<GetListSuSection> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSection"));
        this.Get("/st/sections");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetListSuSectionResponse>>> HandleRequestAsync(GetListSuSectionRequest req, CancellationToken ct)
    {
        var listData = this.dbContext.SuSections
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x =>
                               EF.Functions.ILike(x.RefBankOrder, $"%{req.Keyword}%"))
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x =>
                               x.SupplyMethodCode != null && x.SupplyMethodCode.Value == req.Keyword);

        var paginated = await PaginatedList<GHB.DP2.Domain.SystemUtility.SuSection>
            .CreateAsync(
                listData,
                req.PageNumber,
                req.PageSize,
                ct);

        var result = paginated.ToResult(static x => new GetListSuSectionResponse(
            x.Id.Value,
            x.RefBankOrder,
            x.MaximumBudget,
            x.Remark,
            x.SupplyMethodCode?.Value,
            x.SupplyMethodSpecialTypeCode?.Value));

        return TypedResults.Ok(result);
    }
}