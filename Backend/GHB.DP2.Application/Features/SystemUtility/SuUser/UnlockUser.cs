namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using FluentValidation;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed record UnlockUserCommand(
    Guid Id);

public class UnlockUserCommandValidator : Validator<UnlockUserCommand>
{
    public UnlockUserCommandValidator()
    {
        this.RuleFor(user => user.Id)
            .NotEmpty()
            .NotNull();
    }
}

public class UnlockUser : SecureEndpointBase<UnlockUserCommand, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UnlockUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<UnlockUser> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Post("/st/st005/{id}/unlock");
        this.AuditLog("จัดการผู้ใช้งาน", "ปลดล็อคบัญชี");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(UnlockUserCommand command, CancellationToken ct)
    {
        var userData = await this.dbContext.SuUsers
                                 .IgnoreQueryFilters()
                                 .SingleOrDefaultAsync(user => user.Id == UserId.From(command.Id), ct);

        if (userData is null)
        {
            return TypedResults.NotFound("ไม่พบผู้ใช้งาน");
        }

        userData.UnlockAccount();
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
