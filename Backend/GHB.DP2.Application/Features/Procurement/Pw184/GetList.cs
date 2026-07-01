namespace GHB.DP2.Application.Features.Procurement.Pw184;

using System.IdentityModel.Tokens.Jwt;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetPw184ListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode,
    Pw184Status? Status,
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId);

public record GetPw184ListItemResponse(
    Guid Id,
    string Pw184Number,
    string Subject,
    decimal Budget,
    string DepartmentName,
    string SupplyMethodName,
    string? SupplyMethodSpecialName,
    Pw184Status Status);

public record GetPw184StatusCount(
    int All,
    int Draft,
    int Edit,
    int WaitingApproval,
    int WaitingCommitteeApprove,
    int WaitingAccounting,
    int WaitingDisbursementDate,
    int Paid,
    int Rejected);

public record GetPw184ListResult(
    GetPw184StatusCount StatusCount,
    PaginatedQueryResult<GetPw184ListItemResponse> Data);

public class GetPw184ListEndpoint : EndpointBase<GetPw184ListRequest, Ok<GetPw184ListResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetPw184ListEndpoint(Dp2DbContext dbContext, ILogger<GetPw184ListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("GetPw184List")
             .Produces<Ok<GetPw184ListResult>>());
        this.Get("pw184");
    }

    protected override async ValueTask<Ok<GetPw184ListResult>> HandleRequestAsync(GetPw184ListRequest req, CancellationToken ct)
    {
        var baseQuery = this.dbContext.Pw184s
                            .AsNoTracking()
                            .Include(p => p.Department)
                            .Include(p => p.SupplyMethod)
                            .Include(p => p.SupplyMethodSpecialType)
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), x =>
                                EF.Functions.ILike(x.Subject, $"%{req.Keyword}%") ||
                                EF.Functions.ILike((string)x.Pw184Number, $"%{req.Keyword}%"))
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), x =>
                                x.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                            .WhereIfTrue(req.BudgetYear.HasValue, x => x.BudgetYear == req.BudgetYear)
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SupplyMethodCode), x =>
                                x.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                            .OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt);

        var allItems = await baseQuery.ToListAsync(ct);
        var statusCount = new GetPw184StatusCount(
            allItems.Count,
            allItems.Count(s => s.Status == Pw184Status.Draft),
            allItems.Count(s => s.Status == Pw184Status.Edit),
            allItems.Count(s => s.Status == Pw184Status.WaitingApproval),
            allItems.Count(s => s.Status == Pw184Status.WaitingCommitteeApprove),
            allItems.Count(s => s.Status == Pw184Status.WaitingAccounting),
            allItems.Count(s => s.Status == Pw184Status.WaitingDisbursementDate),
            allItems.Count(s => s.Status == Pw184Status.Paid),
            allItems.Count(s => s.Status == Pw184Status.Rejected));

        var filteredQuery = baseQuery.WhereIfTrue(req.Status.HasValue, x => x.Status == req.Status);

        var paginated = await PaginatedList<Domain.Procurement.Pw184.Pw184>.CreateAsync(filteredQuery, req.PageNumber, req.PageSize, ct);

        var data = paginated.ToResult(x => new GetPw184ListItemResponse(
            x.Id.Value,
            x.Pw184Number.Value,
            x.Subject,
            x.Budget,
            x.Department.Name,
            x.SupplyMethod.Label,
            x.SupplyMethodSpecialType?.Label,
            x.Status));

        return TypedResults.Ok(new GetPw184ListResult(statusCount, data));
    }
}
