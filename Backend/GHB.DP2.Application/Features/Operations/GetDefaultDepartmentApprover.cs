namespace GHB.DP2.Application.Features.Operations;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Features.Operations.Dto;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DefaultDepartmentApproverRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public ApproverOrganizationLevel OrganizationLevel { get; init; }

    public bool SkipCurrentEmployee { get; init; } = true;
}

public enum ApproverOrganizationLevel
{
    /// <summary>
    /// Group (กลุ่ม)
    /// </summary>
    Group,

    /// <summary>
    /// Line (สายงาน/ศูนย์)
    /// </summary>
    Line,

    /// <summary>
    /// Department (ฝ่าย)
    /// </summary>
    Department,

    /// <summary>
    ///  (ส่วนงาน)
    /// </summary>
    Segment,

    /// <summary>
    /// Zone (โซน)
    /// </summary>
    Zone,

    /// <summary>
    /// Branch (สาขา)
    /// </summary>
    Branch,
}

public class GetDefaultDepartmentApproverEndpoint : EndpointBase<DefaultDepartmentApproverRequest, Results<Ok<IEnumerable<OperationInfo>>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetDefaultDepartmentApproverEndpoint(
        ILogger<GetDefaultDepartmentApproverEndpoint> logger, Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(r => r
                          .WithTags(nameof(Operations))
                          .Produces<Ok<IEnumerable<OperationInfo>>>()
                          .Produces<NotFound<string>>()
                          .WithSummary("Get Default Department Approver")
                          .WithDescription("Retrieve the default department approver based on the user's organization level."));
        this.Get(
            "/operations/default-department-approver",
            "/operations/{UserId}/default-department-approver");
    }

    protected override async ValueTask<Results<Ok<IEnumerable<OperationInfo>>, NotFound<string>>> HandleRequestAsync(DefaultDepartmentApproverRequest req, CancellationToken ct)
    {
        var userId =
            Optional(this.HttpContext.Request.RouteValues
                         .GetValueOrDefault(nameof(req.UserId))?
                         .ToString())
                .Map(Guid.Parse)
                .IfNone(() => req.UserId);

        var user =
            await this.dbContext.SuUsers
                      .Include(suUser => suUser.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.Users)
                      .Include(suUser => suUser.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.View)
                      .Include(suUser => suUser.Employee)
                      .ThenInclude(rawEmployee => rawEmployee.Positions)
                      .SingleOrDefaultAsync(u => u.Id == UserId.From(userId), ct);

        if (user is null)
        {
            return TypedResults.NotFound("User not found.");
        }

        var organizationLevelString = (req.OrganizationLevel, user.Employee.IsGroupOperation) switch
        {
            (ApproverOrganizationLevel.Group, false) => EmployeeConstant.OrganizationLevel.Group,
            (ApproverOrganizationLevel.Group, true) => EmployeeConstant.OrganizationLevel.Line,
            (ApproverOrganizationLevel.Line, _) => EmployeeConstant.OrganizationLevel.Line,
            (ApproverOrganizationLevel.Department, _) => EmployeeConstant.OrganizationLevel.Department,
            (ApproverOrganizationLevel.Segment, _) => EmployeeConstant.OrganizationLevel.Segment,
            (ApproverOrganizationLevel.Zone, _) => EmployeeConstant.OrganizationLevel.Zone,
            (ApproverOrganizationLevel.Branch, _) => EmployeeConstant.OrganizationLevel.Branch,
            _ => throw new NotSupportedException("Unsupported organization level."),
        };

        var organizationLevel = int.Parse(organizationLevelString);

        var isBranch = req.OrganizationLevel == ApproverOrganizationLevel.Branch
            || req.OrganizationLevel == ApproverOrganizationLevel.Zone;

        var managers = req.SkipCurrentEmployee
            ? user.Employee.Positions
                  .Where(p => p is { Acting: EmployeeConstant.Acting.Primary, ManagerEmployeeCode: not null })
                  .SelectMany(p => TraverseManagees(p.Manager))
                  .Where(m => isBranch
                      ? m.InRefCode is InRefCodeConstant.Bp017 or InRefCodeConstant.Bp018
                      : m.OrganizationLevel >= organizationLevel)
            : TraverseManagees(user.Employee)
                  .Where(m => isBranch
                      ? m.InRefCode is InRefCodeConstant.Bp017 or InRefCodeConstant.Bp018
                      : m.OrganizationLevel >= organizationLevel);

        return TypedResults.Ok(managers);

        static IEnumerable<OperationInfo> TraverseManagees(RawEmployee? employee)
        {
            if (employee?.Users is null)
            {
                yield break;
            }

            var user = employee.Users.FirstOrDefault();

            if (user is null)
            {
                yield break;
            }

            if (user.IsActive is false)
            {
                yield break;
            }

            if (employee.View is null)
            {
                yield break;
            }

            if (employee.PrimaryBusinessUnit is null)
            {
                yield break;
            }

            // Yield the current employee's operation info
            yield return employee.CreateOperationInfo(inRefCode: employee.PrimaryPosition?.InRefCode);

            // Process managers from primary positions
            foreach (var managedEmployee in GetManagersFromPrimaryPositions(employee))
            {
                yield return managedEmployee;
            }
        }

        static IEnumerable<OperationInfo> GetManagersFromPrimaryPositions(RawEmployee employee)
        {
            return employee.Positions
                           .Where(p => p is { Acting: EmployeeConstant.Acting.Primary, ManagerEmployeeCode: not null })
                           .SelectMany(p => TraverseManagees(p.Manager));
        }
    }
}