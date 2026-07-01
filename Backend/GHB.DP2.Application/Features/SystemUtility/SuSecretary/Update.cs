namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using Codehard.FileService.Contracts.ValueObjects;
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

public record UpdateSt010Request(
    Guid Id,
    UpdateSt010PrincipalDto SuSecretaryOwner,
    IEnumerable<UpdateSt010SecretaryDto> Secretaries,
    IEnumerable<AttachmentsDtoWithId> Attachments);

public record UpdateSt010PrincipalDto(
    string? UserFullName,
    string? FullPositionName,
    string? BusinessUnitName);

public record UpdateSt010SecretaryDto(
    Guid? Id,
    Guid SuUserId,
    string? UserFullName,
    string? EmployeeCode,
    string? PositionId,
    string? FullPositionName,
    bool? Active,
    int Sequence,
    DateTimeOffset? EffectiveStartDate,
    DateTimeOffset? EffectiveEndDate);

public class UpdateSt010Endpoint : SecureEndpointBase<UpdateSt010Request, Results<Ok, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateSt010Endpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateSt010Endpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Put("/st/st010/{Id:guid}");
        this.AuditLog("กำหนดเลขา", "แก้ไขกำหนดเลขา");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, Conflict<string>>> HandleRequestAsync(
        UpdateSt010Request req,
        CancellationToken ct)
    {
        var owner = await this.dbContext.SuSecretaryOwners
                              .Include(o => o.Secretaries)
                              .Include(o => o.Attachments)
                              .SingleOrDefaultAsync(o => o.Id == SecretaryOwnerId.From(req.Id), ct);

        if (owner is null)
        {
            return TypedResults.NotFound($"SuSecretaryOwner with Id {req.Id} not found");
        }

        owner.Update(req.SuSecretaryOwner.UserFullName, req.SuSecretaryOwner.FullPositionName, req.SuSecretaryOwner.BusinessUnitName);

        var existingSecretaries = owner.Secretaries.ToList();

        foreach (var secretaryDto in req.Secretaries)
        {
            if (secretaryDto.Id.HasValue)
            {
                var existing = existingSecretaries.SingleOrDefault(s => s.Id == SecretaryId.From(secretaryDto.Id.Value));
                existing?.Update(
                    secretaryDto.Sequence,
                    secretaryDto.Active,
                    secretaryDto.EffectiveStartDate.HasValue ? DateOnly.FromDateTime(secretaryDto.EffectiveStartDate.Value.LocalDateTime) : null,
                    secretaryDto.EffectiveEndDate.HasValue ? DateOnly.FromDateTime(secretaryDto.EffectiveEndDate.Value.LocalDateTime) : null);
            }
            else
            {
                var secretaryUser = await this.dbContext.SuUsers
                                              .Where(u => u.Id == UserId.From(secretaryDto.SuUserId))
                                              .SingleOrDefaultAsync(ct);

                if (secretaryUser is null)
                {
                    return TypedResults.NotFound($"Secretary user with Id {secretaryDto.SuUserId} not found");
                }

                if (owner.SuUserId.HasValue && secretaryUser.Id == owner.SuUserId.Value)
                {
                    return TypedResults.Conflict("ไม่สามารถกำหนดตัวเองเป็นเลขาได้");
                }

                var secretaryPositionId = !string.IsNullOrWhiteSpace(secretaryDto.PositionId)
                    ? PositionId.From(secretaryDto.PositionId)
                    : (PositionId?)null;

                var newSecretary = SuSecretary.Create(
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

                owner.AddSecretary(newSecretary);
            }
        }

        var toRemove = existingSecretaries
            .Where(s => req.Secretaries
                .Where(d => d.Id.HasValue)
                .All(d => s.Id != SecretaryId.From(d.Id!.Value)))
            .ToList();

        foreach (var secretary in toRemove)
        {
            owner.RemoveSecretary(secretary);
        }

        this.dbContext.SuSecretaries.RemoveRange(toRemove);

        var incomingAttachments = req.Attachments
                                     .SelectMany(a => a.FileAttachments.Select(f => (
                                         a.DocumentTypeCode,
                                         FileId: FileId.From(f.FileId),
                                         f.FileName,
                                         f.Sequence,
                                         f.IsPublic,
                                         Id: f.Id)))
                                     .ToList();

        var existingAttachments = owner.Attachments.ToList();

        var removedAttachments = existingAttachments
            .Where(e => incomingAttachments.All(f => f.FileId != e.FileId))
            .ToList();

        foreach (var removed in removedAttachments)
        {
            owner.RemoveAttachmentById(removed.Id.Value);
        }

        this.dbContext.SuSecretaryAttachments.RemoveRange(removedAttachments);

        var newAttachments = incomingAttachments
            .Where(f => existingAttachments.All(e => e.FileId != f.FileId))
            .ToList();

        foreach (var newAttach in newAttachments)
        {
            var attachment = SuSecretaryAttachment.Create(
                owner.Id,
                newAttach.FileId,
                newAttach.FileName,
                newAttach.Sequence,
                newAttach.DocumentTypeCode,
                null,
                newAttach.IsPublic);

            owner.AddAttachment(attachment);
        }

        owner.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.Update,
            ActivityLogActionTypeConstant.Update,
            "กำหนดเลขา"));

        this.dbContext.SuSecretaryOwners.Update(owner);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
