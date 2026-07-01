namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class DeleteSuDelegateUserRequest
{
    public Guid DelegatorId { get; init; }
}

public class DeleteDelegateUser : SecureEndpointBase<DeleteSuDelegateUserRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public DeleteDelegateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteDelegateUser> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Delete("/st/st001/{DelegatorId:guid}");
        this.AuditLog("กำหนดผู้รับมอบหมาย", "ลบผู้ให้มอบหมาย");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(
        DeleteSuDelegateUserRequest req,
        CancellationToken ct)
    {
        var data = await this.dbContext.SuDelegators
                             .Where(d => d.Id == DelegatorId.From(req.DelegatorId))
                             .SingleOrDefaultAsync(ct);

        if (data is null)
        {
            return TypedResults.NoContent();
        }

        this.dbContext.SuDelegators.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}