namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateuser;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class DeleteDelegateeUserRequest
{
    public Guid DelegatorId { get; init; }

    public Guid DelegateeId { get; init; }
}

public class DeleteDelegateUser : SecureEndpointBase<DeleteDelegateeUserRequest, NoContent>
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
        this.Delete("/st/st001/{DelegatorId:guid}/delegatee/{DelegateeId:guid}");
        this.AuditLog("กำหนดผู้รับมอบหมาย", "ลบผู้รับมอบหมาย");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(
        DeleteDelegateeUserRequest req,
        CancellationToken ct)
    {
        var delegator = await this.dbContext.SuDelegators
                                  .AnyAsync(d => d.Id == DelegatorId.From(req.DelegatorId), ct);

        if (!delegator)
        {
            return TypedResults.NoContent();
        }

        var delegatee = await this.dbContext.SuDelegatees
                                  .Where(d => d.Id == req.DelegateeId)
                                  .SingleOrDefaultAsync(ct);

        if (delegatee is null)
        {
            return TypedResults.NoContent();
        }

        this.dbContext.SuDelegatees.Remove(delegatee);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}