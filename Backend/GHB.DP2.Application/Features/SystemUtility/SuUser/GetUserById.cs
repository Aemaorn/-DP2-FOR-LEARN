namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using GHB.DP2.Application.Features.SystemUtility.SuUser.DTO;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public record GetUserByIdQuery(
    Guid Id);

public record UserRole(
    RoleCode RoleCode);

public class GetUserById : SecureEndpointBase<GetUserByIdQuery, Results<Ok<GetUserByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetUserById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<GetUserById> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    protected override string? GetProgramPath()
    {
        var requestPath = this.HttpContext.Request.Path.Value ?? string.Empty;
        return requestPath.StartsWith("/api/st/st001") || requestPath.StartsWith("/st/st001")
            ? "/st/st001"
            : "/st/st005";
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Get("/st/st005/{Id}", "/st/st001/user/{Id}");
    }

    protected override async ValueTask<Results<Ok<GetUserByIdResponse>, NotFound<string>>> HandleRequestAsync(GetUserByIdQuery query, CancellationToken ct)
    {
        var userData = await this.dbContext.SuUsers
                                 .Where(user => user.Id == UserId.From(query.Id))
                                 .Include(user => user.Roles)
                                 .Include(user => user.Employee)
                                 .ThenInclude(employee => employee.View)
                                 .IgnoreQueryFilters()
                                 .FirstOrDefaultAsync(ct);

        if (userData is null)
        {
            return TypedResults.NotFound("User not found");
        }

        return TypedResults.Ok(new GetUserByIdResponse(
            userData.Id,
            userData.Employee.FullName,
            userData.Employee.PrimaryDepartment?.Id.Value,
            userData.Employee.PrimaryDepartment?.Name,
            userData.Employee.PrimaryDepartment?.OrganizationLevel,
            userData.Employee.IsJorPor,
            userData.Employee.Email.ToLower(),
            userData.Employee.View?.PositionId.Value,
            userData.Employee.View?.FullPositionName,
            userData.IsActive,
            userData.SignatureImageId,
            userData.Roles.Select(role => new DTO.UserRole(role.Code)),
            userData.EmployeeCode,
            userData.Employee.PrimaryBusinessUnit?.OrganizationLevel,
            userData.Employee.PrimaryBusinessUnit?.BusinessUnitCode,
            userData.IsLockedOut()));
    }
}