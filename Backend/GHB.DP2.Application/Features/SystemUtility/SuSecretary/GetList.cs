namespace GHB.DP2.Application.Features.SystemUtility.SuSecretary;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetSt010ListRequest
{
    public List<string>? BusinessUnitIds { get; init; }

    public string? Keyword { get; init; }

    public DateTimeOffset? EffectiveStartDate { get; init; }

    public DateTimeOffset? EffectiveEndDate { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record SecretaryOwnerListDto(
    SecretaryOwnerId Id,
    bool IsPositionType,
    string? UserFullName,
    string? FullPositionName,
    string? BusinessUnitName,
    int SecretaryCount,
    string? SecretaryNames,
    DateTimeOffset? UpdatedAt);

file sealed record SecretaryInfoRow(string? UserFullName, DateOnly? EffectiveStartDate, DateOnly? EffectiveEndDate);

file sealed record SecretaryOwnerRow(
    SecretaryOwnerId Id,
    bool IsPositionType,
    string? UserFullName,
    string? FullPositionName,
    string? BusinessUnitName,
    int SecretaryCount,
    List<SecretaryInfoRow> Secretaries,
    DateTimeOffset? UpdatedAt);

public class GetSt010ListEndpoint : SecureEndpointBase<GetSt010ListRequest, Ok<PaginatedQueryResult<SecretaryOwnerListDto>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSt010ListEndpoint(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetSt010ListEndpoint> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuSecretary"));
        this.Get("/st/st010");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<SecretaryOwnerListDto>>> HandleRequestAsync(
        GetSt010ListRequest req,
        CancellationToken ct)
    {
        var businessUnitIds = req.BusinessUnitIds?.Select(BusinessUnitId.From).ToList();
        var businessUnitNames = businessUnitIds is { Count: > 0 }
            ? await this.dbContext.RawBusinessUnits
                                  .Where(r => businessUnitIds.Contains(r.Id))
                                  .Select(r => r.Name)
                                  .ToListAsync(ct)
            : null;

        var effectiveStart = req.EffectiveStartDate.HasValue
            ? DateOnly.FromDateTime(req.EffectiveStartDate.Value.LocalDateTime)
            : (DateOnly?)null;
        var effectiveEnd = req.EffectiveEndDate.HasValue
            ? DateOnly.FromDateTime(req.EffectiveEndDate.Value.LocalDateTime)
            : (DateOnly?)null;

        var query = this.dbContext.SuSecretaryOwners
                        .WhereIfTrue(
                            businessUnitNames is { Count: > 0 },
                            o => this.dbContext.Set<RawEmployeeView>()
                                               .Any(v => v.EmployeeCode == o.EmployeeCode
                                                      && businessUnitNames!.Contains(v.BusinessUnitName)))
                        .WhereIfTrue(
                            !string.IsNullOrWhiteSpace(req.Keyword),
                            o => EF.Functions.ILike(o.UserFullName!, $"%{req.Keyword}%")
                              || EF.Functions.ILike(o.FullPositionName!, $"%{req.Keyword}%")
                              || o.Secretaries.Any(s =>
                                     EF.Functions.ILike(s.UserFullName!, $"%{req.Keyword}%")
                                  || EF.Functions.ILike(s.FullPositionName!, $"%{req.Keyword}%")))
                        .WhereIfTrue(
                            effectiveStart.HasValue,
                            o => o.Secretaries.Any(s => s.EffectiveStartDate.HasValue && s.EffectiveStartDate >= effectiveStart!.Value))
                        .WhereIfTrue(
                            effectiveEnd.HasValue,
                            o => o.Secretaries.Any(s => s.EffectiveStartDate.HasValue && s.EffectiveStartDate <= effectiveEnd!.Value))
                        .OrderByDescending(o => o.AuditInfo.LastModifiedAt ?? o.AuditInfo.CreatedAt)
                        .Select(o => new SecretaryOwnerRow(
                            o.Id,
                            o.IsPositionType,
                            o.UserFullName,
                            o.FullPositionName,
                            o.BusinessUnitName,
                            o.Secretaries.Count(),
                            o.Secretaries
                                .OrderBy(s => s.Sequence)
                                .Select(s => new SecretaryInfoRow(s.UserFullName, s.EffectiveStartDate, s.EffectiveEndDate))
                                .ToList(),
                            o.AuditInfo.LastModifiedAt));

        var paginated = await PaginatedList<SecretaryOwnerRow>
            .CreateAsync(query, req.PageNumber, req.PageSize, ct);

        var result = paginated.ToResult(d => new SecretaryOwnerListDto(
            d.Id,
            d.IsPositionType,
            d.UserFullName,
            d.FullPositionName,
            d.BusinessUnitName,
            d.SecretaryCount,
            string.Join(", ", d.Secretaries.Select(FormatSecretary)),
            d.UpdatedAt));

        return TypedResults.Ok(result);

        static string FormatSecretary(SecretaryInfoRow s)
        {
            var name = s.UserFullName ?? string.Empty;

            if (!s.EffectiveStartDate.HasValue && !s.EffectiveEndDate.HasValue)
            {
                return name;
            }

            var start = s.EffectiveStartDate?.ToString("dd/MM/yyyy") ?? string.Empty;
            var end = s.EffectiveEndDate?.ToString("dd/MM/yyyy") ?? string.Empty;

            var range = (s.EffectiveStartDate.HasValue, s.EffectiveEndDate.HasValue) switch
            {
                (true, true) => $"ปฏิบัติหน้าที่ตั้งแต่ {start} - {end}",
                (true, false) => $"ปฏิบัติหน้าที่ตั้งแต่ {start}",
                _ => $"ปฏิบัติหน้าที่ถึง {end}",
            };

            return $"{name} ({range})";
        }
    }
}
