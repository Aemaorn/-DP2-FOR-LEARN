namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public record UpdateUserByIdCommand(
    Guid Id,
    FileId? SignatureImageId,
    bool IsActive,
    UserOtherInfo? OtherInfo,
    List<DTO.UserRole> Role);

public class UpdateUserByIdCommandValidator : Validator<UpdateUserByIdCommand>
{
    public UpdateUserByIdCommandValidator()
    {
        this.RuleFor(user => user.Id)
            .NotNull()
            .NotEmpty();

        this.RuleFor(user => user.IsActive)
            .NotEmpty();

        this.RuleFor(user => user.Role)
            .Must(user => user.Count > 0)
            .NotNull()
            .NotEmpty();
    }
}

public class UpdateUserById : SecureEndpointBase<UpdateUserByIdCommand, Results<Ok<UserId>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateUserById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<UpdateUserById> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Put("/st/st005/{id}");
        this.AuditLog("จัดการผู้ใช้งาน", "แก้ไขผู้ใช้งาน");
    }

    protected override async ValueTask<Results<Ok<UserId>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateUserByIdCommand command, CancellationToken ct)
    {
        var currentUserId = this.GetUserIdFromClaims();
        var isSelfEdit = currentUserId != null && UserId.From(command.Id) == currentUserId;

        var userData = await this.dbContext.SuUsers
                                 .Where(user => user.Id == UserId.From(command.Id))
                                 .Include(user => user.Roles)
                                 .IgnoreQueryFilters()
                                 .SingleOrDefaultAsync(ct);

        if (userData is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ใช้งาน");
        }

        if (!isSelfEdit)
        {
            var roleList = await this.dbContext.SuRoles
                                     .Where(role => command.Role.Select(r => r.RoleCode).Contains(role.Code))
                                     .ToListAsync(ct);

            if (!command.Role.All(role => roleList.Select(suRole => suRole.Code).Contains(role.RoleCode)))
            {
                return TypedResults.BadRequest("One or more required roles not found.");
            }

            var roleToRemove = userData.Roles.Except(roleList);

            foreach (var role in roleToRemove)
            {
                userData.RemoveRole(role);
            }

            var newRole = roleList.Except(userData.Roles);

            foreach (var role in newRole)
            {
                userData.AddRole(role);
            }
        }

        userData.Update(command.SignatureImageId, command.IsActive, command.OtherInfo);

        this.dbContext.SuUsers.Update(userData);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok(userData.Id);
    }
}