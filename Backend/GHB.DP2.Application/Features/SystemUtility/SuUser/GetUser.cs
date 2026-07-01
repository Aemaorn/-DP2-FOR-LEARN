namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Features.SystemUtility.SuUser.DTO;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetUserRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid Id { get; init; }
}

public class GetUser : EndpointBase<GetUserRequest, Results<Ok<GetUserByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetUser(Dp2DbContext dbContext, ILogger<GetUser> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Get("/user");
    }

    protected override async ValueTask<Results<Ok<GetUserByIdResponse>, NotFound<string>>> HandleRequestAsync(GetUserRequest req, CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .AsNoTracking()
                             .Include(suUser => suUser.Employee)
                             .ThenInclude(employee => employee.View)
                             .Include(suUser => suUser.Roles)
                             .AsSplitQuery()
                             .FirstOrDefaultAsync(
                                 u => u.Id == UserId.From(req.Id),
                                 ct);

        if (user is null)
        {
            return TypedResults.NotFound("User not found");
        }

        var response = new GetUserByIdResponse(
            user.Id,
            user.Employee.FullName,
            user.Employee.PrimaryDepartment?.Id.Value,
            user.Employee.PrimaryDepartment?.Name,
            user.Employee.PrimaryDepartment?.OrganizationLevel,
            user.Employee.IsJorPor,
            user.Employee.Email.ToLower(),
            user.Employee.View?.PositionId.Value,
            user.Employee.View?.FullPositionName,
            user.IsActive,
            user.SignatureImageId,
            user.Roles.Select(
                role => new DTO.UserRole(role.Code)),
            user.EmployeeCode,
            user.Employee.PrimaryBusinessUnit?.OrganizationLevel,
            user.Employee.PrimaryBusinessUnit?.BusinessUnitCode,
            user.IsLockedOut());

        return TypedResults.Ok(response);
    }
}