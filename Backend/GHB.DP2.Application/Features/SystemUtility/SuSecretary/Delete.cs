namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteSt010Request
{
    public Guid Id { get; init; }
}

public class DeleteSt010Endpoint : SecureEndpointBase<DeleteSt010Request, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSt010Endpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSt010Endpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Delete("/st/st010/{Id:guid}");
        this.AuditLog("กำหนดเลขา", "ลบกำหนดเลขา");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(
        DeleteSt010Request req,
        CancellationToken ct)
    {
        var owner = await this.dbContext.SuSecretaryOwners
                              .Where(o => o.Id == SecretaryOwnerId.From(req.Id))
                              .SingleOrDefaultAsync(ct);

        if (owner is null)
        {
            return TypedResults.NoContent();
        }

        this.dbContext.SuSecretaryOwners.Remove(owner);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
