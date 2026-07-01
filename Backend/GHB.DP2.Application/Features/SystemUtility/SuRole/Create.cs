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
using ConflictWithReason = Microsoft.AspNetCore.Http.HttpResults.Conflict<string>;

public class CreateRoleRequest
{
    public string Code { get; init; }

    public string Name { get; init; }

    public bool IsActive { get; init; }

    public IEnumerable<ProgramPermission> ProgramPermissions { get; init; }
}

public class CreateRoleRequestValidator : Validator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        this.RuleFor(r => r.ProgramPermissions)
            .Must(p => !p.GroupBy(x => x.ProgramId).Any(g => g.Count() > 1))
            .WithMessage("Program id duplicated");
    }
}

public class CreateRole :
    SecureEndpointBase<CreateRoleRequest,
                       Results<Created<RoleCode>, ConflictWithReason, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateRole(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateRole> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuRole"));
        this.Post("/st/st004");
        this.AuditLog("กำหนดสิทธิ์", "สร้างสิทธิ์การเข้าถึง");
    }

    protected override async ValueTask<Results<Created<RoleCode>, ConflictWithReason, BadRequest<string>>> HandleRequestAsync(CreateRoleRequest req, CancellationToken ct)
    {
        var isRoleExist = await this.dbContext.SuRoles
                             .AnyAsync(w => (string)w.Code == req.Code, ct);

        if (isRoleExist)
        {
            return TypedResults.Conflict("รหัสสิทธิ์ซ้ำ");
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

        var res = SuRole.Create(
            RoleCode.From(req.Code),
            req.Name,
            req.IsActive);

        req.ProgramPermissions.Iter(p => res.AddProgram(p.MappingRoleProgram()));

        this.dbContext.SuRoles.Add(res);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Created(string.Empty, res.Code);
    }
}