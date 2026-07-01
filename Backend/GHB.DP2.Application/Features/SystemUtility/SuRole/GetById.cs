namespace GHB.DP2.Application.Features.SystemUtility.SuRole;

using GHB.DP2.Application.Features.SystemUtility.SuRole.Dto;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSuRoleByIdRequest
{
    public string Code { get; init; }
}

public record GetSuRoleByIdResponse(
    string Code,
    string Name,
    bool IsActive,
    IEnumerable<ProgramPermissionResponse> ProgramPermissions);

public class GetRoleById : SecureEndpointBase<GetSuRoleByIdRequest, Results<Ok<GetSuRoleByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetRoleById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<GetRoleById> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuRole"));
        this.Get("/st/st004/{Code}");
    }

    protected override async ValueTask<Results<Ok<GetSuRoleByIdResponse>, NotFound<string>>> HandleRequestAsync(GetSuRoleByIdRequest req, CancellationToken ct)
    {
        var roleData = await this.dbContext.SuRoles
                                 .Include(rp => rp.RolePrograms)
                                 .ThenInclude(p => p.Program)
                                 .AsNoTracking()
                                 .SingleOrDefaultAsync(w => w.Code == RoleCode.From(req.Code), ct);

        if (roleData is null)
        {
            return TypedResults.NotFound("Role data not found.");
        }

        var programIds = roleData.RolePrograms.Select(p => p.ProgramId);

        var newPrograms = await this.dbContext.SuPrograms
                                    .Include(sp => sp.Parent)
                                    .Where(w => w.Path != null && !programIds.Contains(w.Id))
                                    .AsNoTracking()
                                    .ToListAsync(ct);

        var programPermissions = roleData.RolePrograms
                                         .Select(rp => new ProgramPermissionResponse(
                                             rp.Program.Sorting,
                                             rp.ProgramId.Value,
                                             rp.Program.Code,
                                             rp.Program.Label,
                                             rp.Permission,
                                             rp.Program.Parent?.Label ?? rp.Program.Label))
                                         .Concat(newPrograms.Select(p => new ProgramPermissionResponse(
                                             p.Sorting,
                                             p.Id.Value,
                                             p.Code,
                                             p.Label,
                                             Permission.None,
                                             p.Parent?.Label ?? p.Label)))
                                         .OrderBy(o => o.Sorting);

        var resp = new GetSuRoleByIdResponse(
            roleData.Code.Value,
            roleData.Name,
            roleData.IsActive,
            programPermissions);

        return TypedResults.Ok(resp);
    }
}