namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using FluentValidation;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public sealed record DeleteUserCommand(
    Guid Id);

public class DeleteUserCommandValidator : Validator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        this.RuleFor(user => user.Id)
            .NotEmpty()
            .NotNull();
    }
}

public class DeleteUser : SecureEndpointBase<DeleteUserCommand, Results<NoContent, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<DeleteUser> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Delete("/st/st005/{id}");
        this.AuditLog("จัดการผู้ใช้งาน", "ลบผู้ใช้งาน");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, BadRequest<string>>> HandleRequestAsync(DeleteUserCommand command, CancellationToken ct)
    {
        var userData = await this.dbContext.SuUsers
                                 .IgnoreQueryFilters()
                                 .SingleOrDefaultAsync(user => user.Id == UserId.From(command.Id), ct);

        if (userData is null)
        {
            return TypedResults.NotFound("ไม่พบผู้ใช้งาน");
        }

        if (userData.IsActive)
        {
            return TypedResults.BadRequest("ผู้ใช้งานเปิดใช้งานอยู่ไม่สามารถลบได้");
        }

        this.dbContext.SuUsers.Remove(userData);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}