namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class UpdateUserCommand
{
    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid Id { get; init; }

    public FileId? SignatureImageId { get; set; }

    public bool IsActive { get; set; }

    public UserOtherInfo? OtherInfo { get; set; }

    public List<DTO.UserRole> Role { get; set; }
}

public class UpdateUserCommandValidator : Validator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        this.RuleFor(user => user.Id)
            .NotNull()
            .NotEmpty();

        this.RuleFor(user => user.IsActive)
            .NotEmpty();

        this.RuleFor(user => user.Role)
            .NotNull()
            .NotEmpty();
    }
}

public class UpdateUser : SecureEndpointBase<UpdateUserCommand, Results<Ok<UserId>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionService,
        ILogger<UpdateUser> logger)
        : base(permissionService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Put("/st/st005");
        this.AuditLog("จัดการผู้ใช้งาน", "แก้ไขผู้ใช้งาน");
    }

    protected override async ValueTask<Results<Ok<UserId>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateUserCommand command, CancellationToken ct)
    {
        var userData = await this.dbContext.SuUsers
                                 .Where(user => user.Id == UserId.From(command.Id))
                                 .Include(user => user.Roles)
                                 .SingleOrDefaultAsync(ct);

        if (userData is null)
        {
            return TypedResults.NotFound("User not found.");
        }

        // Self-update endpoint (Id from JWT): only allow profile changes, not role mutations
        userData.Update(command.SignatureImageId, command.IsActive, command.OtherInfo);

        this.dbContext.SuUsers.Update(userData);
        await this.dbContext.SaveChangesAsync(CancellationToken.None);

        return TypedResults.Ok(userData.Id);
    }
}