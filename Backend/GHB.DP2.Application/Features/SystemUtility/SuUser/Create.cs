namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ConflictWithReason = Microsoft.AspNetCore.Http.HttpResults.Conflict<string>;
using GHB.DP2.Application.Services;

public record CreateUserCommand(
    string EmployeeCode,
    FileId? SignatureImageId,
    bool IsActive,
    UserOtherInfo? OtherInfo,
    List<DTO.UserRole> Role);

public class CreateUserCommandValidator : Validator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        this.RuleFor(user => user.EmployeeCode)
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

public class CreateUser : SecureEndpointBase<CreateUserCommand, Results<Created<UserId>, ConflictWithReason, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<CreateUser> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Post("/st/st005");
        this.AuditLog("จัดการผู้ใช้งาน", "สร้างผู้ใช้งาน");
    }

    protected override async ValueTask<Results<Created<UserId>, ConflictWithReason, BadRequest<string>>> HandleRequestAsync(
        CreateUserCommand command, CancellationToken ct)
    {
        var isUserExist = await this.dbContext.SuUsers
                                    .AnyAsync(user => user.EmployeeCode == EmployeeCode.From(command.EmployeeCode), ct);

        if (isUserExist)
        {
            return TypedResults.Conflict($"User Code {command.EmployeeCode} already exists.");
        }

        var roleList = await this.dbContext.SuRoles
                                 .Where(role => command.Role.Select(r => r.RoleCode).Contains(role.Code))
                                 .ToListAsync(ct);

        if (!command.Role.All(role => roleList.Select(x => x.Code).Contains(role.RoleCode)))
        {
            return TypedResults.BadRequest($"One or more requested roles do not exist.");
        }

        var user = SuUser.Create(
            EmployeeCode.From(command.EmployeeCode),
            command.SignatureImageId,
            command.IsActive,
            command.OtherInfo);

        foreach (var role in roleList)
        {
            user.AddRole(role);
        }

        this.dbContext.SuUsers.Add(user);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Created(string.Empty, user.Id);
    }
}