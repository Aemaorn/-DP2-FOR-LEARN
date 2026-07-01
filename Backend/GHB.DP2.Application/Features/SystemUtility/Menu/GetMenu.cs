namespace GHB.DP2.Application.Features.SystemUtility.Menu;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetMenuByUserRequest
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid Id { get; init; }
}

public record GetMenuResponse(
    Guid Id,
    string Code,
    string Label,
    string Path,
    int Sequence,
    Permission Permission,
    List<GetMenuResponse> Children);

public class GetMenuEndpoint : EndpointBase<GetMenuByUserRequest, Results<Ok<List<GetMenuResponse>>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetMenuEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetMenuEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUserMenu"));
        this.Get("/menus");
    }

    protected override async ValueTask<Results<Ok<List<GetMenuResponse>>, NotFound<string>>> HandleRequestAsync(GetMenuByUserRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.Id);

        var user = await this.dbContext.SuUsers
                             .Include(u => u.Roles)
                             .ThenInclude(r => r.RolePrograms)
                             .ThenInclude(rp => rp.Program)
                             .AsSplitQuery()
                             .SingleOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            return TypedResults.NotFound("User not found");
        }

        var userRoles = user.Roles?.ToList() ?? [];

        if (!userRoles.Any())
        {
            return TypedResults.NotFound("User has no roles assigned");
        }

        var rolePrograms = userRoles
            .SelectMany(r => r.RolePrograms)
            .Where(rp => rp.Program != null && (rp.IsManage is true || rp.IsView is true))
            .Distinct()
            .ToList();

        var accessiblePrograms = rolePrograms
            .Select(rp => rp.Program)
            .Distinct()
            .ToList();

        var allPrograms = await this.GetAllRequiredPrograms(accessiblePrograms, ct);

        var menu = this.BuildHierarchicalMenu(allPrograms, rolePrograms);

        return TypedResults.Ok(menu);
    }

    private async Task<List<SuProgram>> GetAllRequiredPrograms(IEnumerable<SuProgram> accessiblePrograms, CancellationToken ct)
    {
        var allRequired = new HashSet<SuProgram>(accessiblePrograms);

        var programIds = accessiblePrograms.Select(p => p.Id).ToList();

        var programsFromDb = await this.dbContext.SuPrograms
                                      .Where(p => programIds.Contains(p.Id))
                                      .ToListAsync(ct);

        await this.AddParentProgramsForAll(programsFromDb, allRequired, ct);

        var allRequiredIds = allRequired.Select(p => p.Id).ToHashSet();

        return await this.dbContext.SuPrograms
                         .Where(p => allRequiredIds.Contains(p.Id))
                         .OrderBy(p => p.Sorting)
                         .ToListAsync(ct);
    }

    private async Task AddParentProgramsForAll(List<SuProgram> programs, HashSet<SuProgram> allRequiredIds, CancellationToken ct)
    {
        foreach (var program in programs)
        {
            await AddParentProgramsRecursively(program.Parent, allRequiredIds, ct);
        }
    }

    private static async Task AddParentProgramsRecursively(SuProgram? parent, HashSet<SuProgram> allRequiredIds, CancellationToken ct)
    {
        if (parent == null || !allRequiredIds.Add(parent))
        {
            return; // No parent or already processed
        }

        if (parent?.ParentId != null)
        {
            await AddParentProgramsRecursively(parent, allRequiredIds, ct);
        }
    }

    private List<GetMenuResponse> BuildHierarchicalMenu(List<SuProgram> allPrograms, List<SuRoleProgram> rolePrograms)
    {
        var permissionLookup = rolePrograms
            .GroupBy(rp => rp.ProgramId)
            .ToDictionary(
                g => g.Key,
                g => g.First().Permission);

        var rootPrograms = allPrograms.Where(p => p.ParentId == null).ToList();

        return rootPrograms
               .Select(program => this.BuildMenuNode(program, allPrograms, permissionLookup))
               .Where(node => node != null)
               .ToList()!;
    }

    private GetMenuResponse? BuildMenuNode(
        SuProgram program,
        List<SuProgram> allPrograms,
        Dictionary<ProgramId, Permission> permissionLookup)
    {
        var children = this.BuildChildrenNodes(program, allPrograms, permissionLookup);
        var hasDirectPermission = permissionLookup.TryGetValue(program.Id, out var permission);

        if (!ShouldIncludeNode(hasDirectPermission, children, program))
        {
            return null;
        }

        return new GetMenuResponse(
            Id: program.Id.Value,
            Code: program.Code,
            Label: program.Label,
            Path: program.Path ?? string.Empty,
            Sequence: program.Sorting,
            Permission: permission,
            Children: children);
    }

    private List<GetMenuResponse> BuildChildrenNodes(
        SuProgram program,
        List<SuProgram> allPrograms,
        Dictionary<ProgramId, Permission> permissionLookup)
    {
        return allPrograms
               .Where(p => p.ParentId == program.Id)
               .Select(child => this.BuildMenuNode(child, allPrograms, permissionLookup))
               .Where(node => node != null)
               .OrderBy(node => node!.Sequence)
               .ToList()!;
    }

    private static bool ShouldIncludeNode(bool hasDirectPermission, List<GetMenuResponse> children, SuProgram program)
    {
        var hasChildrenWithPermissions = children.Any();

        // Exclude if no direct permissions and no children with permissions
        if (!hasDirectPermission && !hasChildrenWithPermissions)
        {
            return false;
        }

        // Exclude if no path and no children (dead-end node)
        if (string.IsNullOrEmpty(program.Path) && !children.Any())
        {
            return false;
        }

        return true;
    }
}