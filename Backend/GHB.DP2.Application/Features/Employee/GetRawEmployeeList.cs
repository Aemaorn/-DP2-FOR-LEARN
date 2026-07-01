namespace GHB.DP2.Application.Features.Employee;

using Codehard.Infrastructure.EntityFramework;
using FluentValidation;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetRawEmployeeListRequest
{
    public string? Keyword { get; init; }

    public string? GroupWork { get; init; }

    public string? LineWork { get; init; }

    public string? Department { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public class GetRawEmployeeListRequestValidator : Validator<GetRawEmployeeListRequest>
{
    public GetRawEmployeeListRequestValidator()
    {
        this.RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Page number cannot exceed 1000");

        this.RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100 items per page");
    }
}

public record GetRawEmployeeListResponse(
    string EmployeeCode,
    string FullName,
    string Email,
    string DepartmentName,
    string FullPositionName);

public class GetRawEmployeeList : EndpointBase<GetRawEmployeeListRequest, Ok<PaginatedQueryResult<GetRawEmployeeListResponse>>>
{
    private readonly Dp2DbContext dbContext;

    public GetRawEmployeeList(Dp2DbContext dbContext, ILogger<GetRawEmployeeList> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("Employee"));
        this.Get("/employee");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<GetRawEmployeeListResponse>>> HandleRequestAsync(GetRawEmployeeListRequest req, CancellationToken ct)
    {
        var query = this.dbContext
                        .RawEmployees
                        .Include(e => e.Positions)
                        .Where(e => e.Positions.Any(p => p.PositionId != PositionId.From(EmployeeConstant.Position.Resign)))
                        .Include(e => e.View)
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            w => EF.Functions.ILike(w.View!.FullName, $"%{req.Keyword}%"))
                        .AsNoTracking()
                        .AsQueryable();

        if (!req.LineWork.IsNullOrEmpty() && !req.GroupWork.IsNullOrEmpty() && !req.Department.IsNullOrEmpty())
        {
            var businessUnit =
                await this.dbContext.RawBusinessUnits
                          .Include(b => b.Children)
                          .WhereIfTrue(
                              !req.GroupWork.IsNullOrEmpty(),
                              b => b.OrganizationLevel == EmployeeConstant.OrganizationLevel.Group && b.Id == BusinessUnitId.From(req.GroupWork!))
                          .WhereIfTrue(
                              !req.LineWork.IsNullOrEmpty(),
                              b => b.OrganizationLevel == EmployeeConstant.OrganizationLevel.Line && b.Id == BusinessUnitId.From(req.LineWork!))
                          .WhereIfTrue(
                              !req.Department.IsNullOrEmpty(),
                              b => b.OrganizationLevel == EmployeeConstant.OrganizationLevel.Department && b.Id == BusinessUnitId.From(req.Department!))
                          .FirstOrDefaultAsync(ct);

            if (businessUnit == null)
            {
                this.ThrowError("Business unit not found", StatusCodes.Status404NotFound);
            }

            var businessUnitIds = businessUnit.Traverse()
                                              .Map(b => b.Id)
                                              .ToArray();

            query = query.Where(e => businessUnitIds.Contains(e.View!.BusinessUnitId));
        }

        query = query.OrderBy(e => e.View!.FullName);

        var paginated = await PaginatedList<RawEmployee>.CreateAsync(
            query,
            req.PageNumber,
            req.PageSize,
            ct);

        var result = paginated.ToResult(static e => new GetRawEmployeeListResponse(
            e.Id.Value,
            e.View?.FullName ?? e.FullName,
            e.Email.ToLower(),
            e.PrimaryDepartment?.Name ?? string.Empty,
            e.View?.FullPositionName ?? string.Empty));

        return TypedResults.Ok(result);
    }
}