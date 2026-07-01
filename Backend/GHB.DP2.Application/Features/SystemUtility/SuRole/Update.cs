namespace GHB.DP2.Application.Features.SystemUtility.SuRole;

using FluentValidation;
using GHB.DP2.Application.Features.SystemUtility.SuRole.Dto;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateRoleRequest
{
    public string Code { get; init; }

    public string Name { get; init; }

    public bool IsActive { get; init; }

    public IEnumerable<ProgramPermission> ProgramPermissions { get; init; }
}

public class UpdateRoleRequestValidator : Validator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        this.RuleFor(r => r.ProgramPermissions)
            .Must(p => !p.GroupBy(x => x.ProgramId).Any(g => g.Count() > 1))
            .WithMessage("Program id duplicated");
    }
}

public class UpdateRole :
    SecureEndpointBase<UpdateRoleRequest,
                       Results<Ok<RoleCode>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateRole(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateRole> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuRole"));
        this.Put("/st/st004/{Code}");
        this.AuditLog("กำหนดสิทธิ์", "แก้ไขสิทธิ์การใช้งาน");
    }

    protected override async ValueTask<Results<Ok<RoleCode>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateRoleRequest req, CancellationToken ct)
    {
        var roleData = await this.dbContext.SuRoles
                                 .SingleOrDefaultAsync(w => w.Code == RoleCode.From(req.Code), ct);

        if (roleData is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลสิทธิ์");
        }

        var programIds = req.ProgramPermissions.Select(p => p.ProgramId);

        var program = await this.dbContext.SuPrograms
                                .Where(w => programIds.Contains(w.Id))
                                .Select(s => s.Id)
                                .ToListAsync(ct);

        var programIdsNotFound = programIds.Except(program).Select(s => s.Value).ToList();

        if (programIdsNotFound.Count != 0)
        {
            return TypedResults.BadRequest($"Program Id {string.Join(",", programIdsNotFound)} not found.");
        }

        roleData.Update(req.Name, req.IsActive);

        roleData.RolePrograms
                .Join(
                    req.ProgramPermissions,
                    rrp => rrp.ProgramId,
                    pp => pp.ProgramId,
                    (rrp, pp) => (rrp, pp))
                .Iter(rp => rp.rrp.Update(rp.pp.Permission));

        req.ProgramPermissions
           .ExceptBy(
               roleData.RolePrograms.Select(s => s.ProgramId),
               pp => pp.ProgramId)
           .Iter(pp => roleData.AddProgram(pp.MappingRoleProgram()));

        this.dbContext.SuRoles.Update(roleData);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(roleData.Code);
    }
}