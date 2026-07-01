namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using Codehard.FileService.Contracts.ValueObjects;
using FluentValidation;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record CreateSt010Request(
    CreateSt010PrincipalDto SuSecretaryOwner,
    IEnumerable<CreateSt010SecretaryDto> Secretaries,
    IEnumerable<AttachmentsDtoWithId> Attachments);

public record CreateSt010PrincipalDto(
    bool IsPositionType,
    Guid? SuUserId,
    string? BusinessUnitId,
    string? BusinessUnitName,
    string? UserFullName,
    string? EmployeeCode,
    string? PositionId,
    string? FullPositionName);

public record CreateSt010SecretaryDto(
    Guid SuUserId,
    string? UserFullName,
    string? EmployeeCode,
    string? PositionId,
    string? FullPositionName,
    bool? Active,
    int Sequence,
    DateTimeOffset? EffectiveStartDate,
    DateTimeOffset? EffectiveEndDate);

public record CreateSt010Response(Guid Id);

public class CreateSt010Validation : Validator<CreateSt010Request>
{
    public CreateSt010Validation()
    {
        this.RuleFor(x => x.Secretaries)
            .NotNull()
            .WithMessage("Secretaries is required");
    }
}

public class CreateSt010Endpoint : SecureEndpointBase<CreateSt010Request, Results<Ok<CreateSt010Response>, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateSt010Endpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateSt010Endpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Post("/st/st010");
        this.AuditLog("กำหนดเลขา", "สร้างกำหนดเลขา");
    }

    protected override async ValueTask<Results<Ok<CreateSt010Response>, NotFound<string>, Conflict<string>>> HandleRequestAsync(
        CreateSt010Request req,
        CancellationToken ct)
    {
        UserId? principalUserId = null;

        if (!req.SuSecretaryOwner.IsPositionType)
        {
            if (req.SuSecretaryOwner.SuUserId is null)
            {
                return TypedResults.NotFound("SuSecretaryOwner SuUserId is required for person type");
            }

            var principalUser = await this.dbContext.SuUsers
                                          .Where(u => u.Id == UserId.From(req.SuSecretaryOwner.SuUserId.Value))
                                          .SingleOrDefaultAsync(ct);

            if (principalUser is null)
            {
                return TypedResults.NotFound($"User with Id {req.SuSecretaryOwner.SuUserId} not found");
            }

            var alreadyExists = await this.dbContext.SuSecretaryOwners
                                          .AnyAsync(o => o.SuUserId == principalUser.Id, ct);

            if (alreadyExists)
            {
                return TypedResults.Conflict("ผู้ใช้งานนี้มีการกำหนดเลขาอยู่แล้ว");
            }

            principalUserId = principalUser.Id;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(req.SuSecretaryOwner.BusinessUnitId))
            {
                return TypedResults.NotFound("SuSecretaryOwner BusinessUnitId is required for position type");
            }

            var alreadyExists = await this.dbContext.SuSecretaryOwners
                                          .AnyAsync(
                                              o => o.BusinessUnitId == BusinessUnitId.From(req.SuSecretaryOwner.BusinessUnitId)
                                                && o.PositionId == PositionId.From(req.SuSecretaryOwner.PositionId!),
                                              ct);

            if (alreadyExists)
            {
                return TypedResults.Conflict("ตำแหน่งนี้มีการกำหนดเลขาอยู่แล้ว");
            }
        }

        var positionId = !string.IsNullOrWhiteSpace(req.SuSecretaryOwner.PositionId)
            ? PositionId.From(req.SuSecretaryOwner.PositionId)
            : (PositionId?)null;

        var businessUnitId = !string.IsNullOrWhiteSpace(req.SuSecretaryOwner.BusinessUnitId)
            ? BusinessUnitId.From(req.SuSecretaryOwner.BusinessUnitId)
            : (BusinessUnitId?)null;

        var owner = SuSecretaryOwner.Create(
            req.SuSecretaryOwner.IsPositionType,
            principalUserId,
            businessUnitId,
            req.SuSecretaryOwner.BusinessUnitName,
            req.SuSecretaryOwner.UserFullName,
            req.SuSecretaryOwner.EmployeeCode,
            positionId,
            req.SuSecretaryOwner.FullPositionName);

        foreach (var secretaryDto in req.Secretaries.OrderBy(s => s.Sequence))
        {
            var secretaryUser = await this.dbContext.SuUsers
                                          .Where(u => u.Id == UserId.From(secretaryDto.SuUserId))
                                          .SingleOrDefaultAsync(ct);

            if (secretaryUser is null)
            {
                return TypedResults.NotFound($"Secretary user with Id {secretaryDto.SuUserId} not found");
            }

            if (principalUserId.HasValue && secretaryUser.Id == principalUserId.Value)
            {
                return TypedResults.Conflict("ไม่สามารถกำหนดตัวเองเป็นเลขาได้");
            }

            var secretaryPositionId = !string.IsNullOrWhiteSpace(secretaryDto.PositionId)
                ? PositionId.From(secretaryDto.PositionId)
                : (PositionId?)null;

            var secretary = SuSecretary.Create(
                owner.Id,
                secretaryUser.Id,
                secretaryDto.Sequence,
                secretaryDto.UserFullName,
                secretaryDto.EmployeeCode,
                secretaryPositionId,
                secretaryDto.FullPositionName,
                secretaryDto.Active,
                secretaryDto.EffectiveStartDate.HasValue ? DateOnly.FromDateTime(secretaryDto.EffectiveStartDate.Value.LocalDateTime) : null,
                secretaryDto.EffectiveEndDate.HasValue ? DateOnly.FromDateTime(secretaryDto.EffectiveEndDate.Value.LocalDateTime) : null);

            owner.AddSecretary(secretary);
        }

        var attachmentList = req.Attachments
                                .SelectMany(a => a.FileAttachments.Select(f => SuSecretaryAttachment.Create(
                                    owner.Id,
                                    FileId.From(f.FileId),
                                    f.FileName,
                                    f.Sequence,
                                    a.DocumentTypeCode,
                                    null,
                                    f.IsPublic)))
                                .ToList();

        foreach (var attachment in attachmentList)
        {
            owner.AddAttachment(attachment);
        }

        owner.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Create,
            ActivityLogActionTypeConstant.Create,
            "กำหนดเลขา"));

        this.dbContext.SuSecretaryOwners.Add(owner);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new CreateSt010Response(owner.Id.Value));
    }
}
