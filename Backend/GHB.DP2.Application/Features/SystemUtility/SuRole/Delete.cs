namespace GHB.DP2.Application.Features.SystemUtility.SuRole;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteRoleRequest
{
    public string Code { get; init; }
}

public class DeleteRole :
    SecureEndpointBase<
        DeleteRoleRequest,
        Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteRole(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteRole> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuRole"));
        this.Delete("/st/st004/{Code}");
        this.AuditLog("กำหนดสิทธิ์", "ลบสิทธิ์การใช้งาน");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeleteRoleRequest req, CancellationToken ct)
    {
        var roleData = await this.dbContext.SuRoles
                                 .SingleOrDefaultAsync(w => w.Code == RoleCode.From(req.Code), ct);

        if (roleData is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลสิทธิ์");
        }

        var anyUsed = await this.dbContext
                                .SuUsers
                                .SelectMany(s => s.Roles)
                                .Where(w => w.Code == roleData.Code)
                                .AnyAsync(ct);

        if (anyUsed)
        {
            return TypedResults.Conflict("สิทธิ์นี้มีผู้ใช้งานอยู่");
        }

        this.dbContext.SuRoles.Remove(roleData);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}