namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSt010ByIdRequest
{
    public Guid Id { get; init; }
}

public record GetSt010ByIdResponse(
    SecretaryOwnerResponse SuSecretaryOwner,
    IEnumerable<SecretaryResponse> Secretaries,
    AttachmentsDtoWithId[] Attachments);

public record SecretaryOwnerResponse(
    SecretaryOwnerId Id,
    bool IsPositionType,
    UserId? SuUserId,
    string? UserFullName,
    string? EmployeeCode,
    string? PositionId,
    string? FullPositionName,
    string? Email,
    string? BusinessUnitId,
    string? BusinessUnitName);

public record SecretaryResponse(
    SecretaryId Id,
    UserId SuUserId,
    string? UserFullName,
    string? EmployeeCode,
    string? PositionId,
    string? FullPositionName,
    string? Email,
    bool? Active,
    int Sequence,
    string? EffectiveStartDate,
    string? EffectiveEndDate);

public class GetSt010ByIdEndpoint : SecureEndpointBase<GetSt010ByIdRequest, Results<Ok<GetSt010ByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSt010ByIdEndpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetSt010ByIdEndpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Get("/st/st010/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetSt010ByIdResponse>, NotFound<string>>> HandleRequestAsync(
        GetSt010ByIdRequest req,
        CancellationToken ct)
    {
        var owner = await this.dbContext.SuSecretaryOwners
                              .Include(o => o.SuUser!)
                              .ThenInclude(u => u.Employee)
                              .Include(o => o.Secretaries)
                              .ThenInclude(s => s.SuUser)
                              .ThenInclude(u => u.Employee)
                              .Include(o => o.Attachments)
                              .AsNoTracking()
                              .SingleOrDefaultAsync(o => o.Id == SecretaryOwnerId.From(req.Id), ct);

        if (owner is null)
        {
            return TypedResults.NotFound($"SuSecretaryOwner with Id {req.Id} not found");
        }

        var attachments = owner.Attachments
                               .OrderBy(a => a.Sequence)
                               .GroupBy(a => a.DocumentTypeCode ?? string.Empty)
                               .Select(g => new AttachmentsDtoWithId(
                                   g.Key,
                                   g.Select(a => new FileAttachmentsWithId(
                                       a.Id.Value,
                                       a.FileId.Value,
                                       a.FileName,
                                       a.Sequence,
                                       a.IsPublic,
                                       a.AuditInfo.CreatedBy)).ToArray()))
                               .ToArray();

        var response = new GetSt010ByIdResponse(
            SuSecretaryOwner: new SecretaryOwnerResponse(
                owner.Id,
                owner.IsPositionType,
                owner.SuUserId,
                owner.UserFullName,
                owner.EmployeeCode,
                owner.PositionId?.Value,
                owner.FullPositionName,
                owner.SuUser?.Employee?.Email,
                owner.BusinessUnitId?.Value,
                owner.BusinessUnitName),
            Secretaries: owner.Secretaries
                 .OrderBy(s => s.Sequence)
                 .Select(s => new SecretaryResponse(
                     s.Id,
                     s.SuUserId,
                     s.UserFullName,
                     s.EmployeeCode,
                     s.PositionId?.Value,
                     s.FullPositionName,
                     s.SuUser?.Employee?.Email,
                     s.Active,
                     s.Sequence,
                     s.EffectiveStartDate?.ToString("yyyy-MM-dd"),
                     s.EffectiveEndDate?.ToString("yyyy-MM-dd"))),
            Attachments: attachments);

        return TypedResults.Ok(response);
    }
}
