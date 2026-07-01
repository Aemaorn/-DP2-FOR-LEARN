namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteSt010SecretaryRequest
{
    public Guid Id { get; init; }

    public Guid SecretaryId { get; init; }
}

public class DeleteSt010SecretaryEndpoint : SecureEndpointBase<DeleteSt010SecretaryRequest, NoContent>
{
    private readonly Dp2DbContext dbContext;

    public DeleteSt010SecretaryEndpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteSt010SecretaryEndpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Delete("/st/st010/{Id:guid}/secretary/{SecretaryId:guid}");
        this.AuditLog("กำหนดเลขา", "ลบเลขา");
    }

    protected override async ValueTask<NoContent> HandleRequestAsync(
        DeleteSt010SecretaryRequest req,
        CancellationToken ct)
    {
        var owner = await this.dbContext.SuSecretaryOwners
                                .SingleOrDefaultAsync(o => o.Id == SecretaryOwnerId.From(req.Id), ct);

        if (owner is null)
        {
            return TypedResults.NoContent();
        }

        var secretary = await this.dbContext.SuSecretaries
                                  .Where(s => s.Id == Domain.SystemUtility.SecretaryId.From(req.SecretaryId))
                                  .SingleOrDefaultAsync(ct);

        if (secretary is null)
        {
            return TypedResults.NoContent();
        }

        this.dbContext.SuSecretaries.Remove(secretary);

        owner.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Delete,
            ActivityLogActionTypeConstant.Delete,
            "กำหนดเลขา",
            secretary.UserFullName));

        this.dbContext.SuSecretaryOwners.Update(owner);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
