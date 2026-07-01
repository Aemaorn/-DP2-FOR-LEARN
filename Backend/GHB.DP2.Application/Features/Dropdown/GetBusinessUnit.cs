namespace GHB.DP2.Application.Features.Dropdown;

using System.Linq.Expressions;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public enum OrganizationLevel
{
    Head = 100,
    Group = 200,
    Line = 300,
    Department = 400,
    Segment = 600,
    Center = 401,
    Zone = 500,
    Branch = 601,
}

public record GetBusinessUnitRequest(
    OrganizationLevel OrganizationLevel,
    string? ParentId);

public record GetBusinessUnitResponse(
    string Value,
    string Label);

public class GetBusinessUnit(Dp2DbContext dbContext, ILogger<GetParameterByGroupCode> logger) : EndpointBase<GetBusinessUnitRequest, Ok<List<GetBusinessUnitResponse>>>(logger)
{
    public override void Configure()
    {
        this.Options(x => x.WithTags("Dropdown"));
        this.Get("/dropdown/businessunit");
    }

    protected override async ValueTask<Ok<List<GetBusinessUnitResponse>>> HandleRequestAsync(
        GetBusinessUnitRequest req,
        CancellationToken ct)
    {
        var levelFilter = ApplyOrganizationLevelFilter(req.OrganizationLevel);

        var query = await dbContext.RawBusinessUnits
                                   .Where(levelFilter)
                                   .WhereIfTrue(!string.IsNullOrWhiteSpace(req.ParentId), r => r.ParentId == BusinessUnitId.From(req.ParentId!))
                                   .Select(r => new GetBusinessUnitResponse(r.Id.Value, r.Name))
                                   .AsNoTracking()
                                   .ToListAsync(ct);

        return TypedResults.Ok(query);
    }

    private static Expression<Func<RawBusinessUnit, bool>> ApplyOrganizationLevelFilter(
        OrganizationLevel organizationLevel)
        => organizationLevel switch
        {
            OrganizationLevel.Head => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Head,
            OrganizationLevel.Group => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Group,
            OrganizationLevel.Line => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Line,
            OrganizationLevel.Department =>
                r => r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Department ||
                     r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Zone ||
                     r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Branch ||
                     (r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Segment &&
                      r.Parent.OrganizationLevel == EmployeeConstant.OrganizationLevel.Line) ||
                     r.BusinessUnitCode == "88860",
            OrganizationLevel.Center => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Center,
            OrganizationLevel.Zone => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Zone,
            OrganizationLevel.Segment => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Segment,
            OrganizationLevel.Branch => r =>
                r.OrganizationLevel == EmployeeConstant.OrganizationLevel.Branch,
            _ => r => false,
        };
}