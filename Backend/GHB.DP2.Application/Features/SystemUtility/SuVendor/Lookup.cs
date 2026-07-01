namespace GHB.DP2.Application.Features.SystemUtility.SuVendor;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class LookupSuVendorRequest
{
    public string? Keyword { get; init; }

    public string? Name { get; init; }

    public SuVendorType? Type { get; init; }

    public string? EntrepreneurType { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record LookupSuVendorResponse(
    SuVendorId Id,
    string TaxpayerIdentificationNo,
    string EstablishmentName,
    string Type,
    string EntrepreneurType,
    string SapVendorNumber,
    string SapBranchNumber,
    SuVendorNationality Nationality,
    string? Email,
    string? Tel,
    string? PlaceName);

public class LookupSuVendor : EndpointBase<LookupSuVendorRequest, Ok<PaginatedQueryResult<LookupSuVendorResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public LookupSuVendor(
        Dp2DbContext dbContext,
        ILogger<LookupSuVendor> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuVendor"));
        this.Get("/st/st003/lookup");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<LookupSuVendorResponse>>> HandleRequestAsync(LookupSuVendorRequest req, CancellationToken ct)
    {
        var listData = this.dbContext.SuVendors
                           .WhereIfTrue(req.Type != null, x => x.Type == req.Type)
                           .WhereIfTrue(!string.IsNullOrWhiteSpace(req.EntrepreneurType), x => (string)x.EntrepreneurType == req.EntrepreneurType)
                           .WhereIfTrue(
                               !string.IsNullOrWhiteSpace(req.Name),
                               x => EF.Functions.ILike(x.EstablishmentName, $"%{req.Name}%") ||
                                    EF.Functions.ILike(x.TaxpayerIdentificationNo, $"%{req.Name}%") ||
                                    EF.Functions.ILike(x.SapVendorNumber, $"%{req.Name}%") ||
                                    EF.Functions.ILike(x.SapBranchNumber, $"%{req.Name}%"))
                           .WhereIfTrue(
                               !string.IsNullOrWhiteSpace(req.Keyword),
                               x => EF.Functions.ILike(x.EstablishmentName, $"%{req.Keyword}%") ||
                                    EF.Functions.ILike(x.TaxpayerIdentificationNo, $"%{req.Keyword}%") ||
                                    EF.Functions.ILike(x.SapVendorNumber, $"%{req.Keyword}%") ||
                                    EF.Functions.ILike(x.SapBranchNumber, $"%{req.Keyword}%"))
                           .OrderByDescending(o => o.AuditInfo.CreatedAt);

        var paginated =
            await PaginatedList<GHB.DP2.Domain.SystemUtility.SuVendor>
                .CreateAsync(
                    listData,
                    req.PageNumber,
                    req.PageSize,
                    ct);

        var result = paginated.ToResult(static x => new LookupSuVendorResponse(
            x.Id,
            x.TaxpayerIdentificationNo,
            x.EstablishmentName,
            x.Type.ToString(),
            x.EntrepreneurTypeInfo.Label,
            x.SapVendorNumber,
            x.SapBranchNumber,
            x.Nationality,
            x.Email,
            x.Tel,
            x.PlaceName));

        return TypedResults.Ok(result);
    }
}
