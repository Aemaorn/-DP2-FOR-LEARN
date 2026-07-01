namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.SystemUtility.SuDelegateUser.Abstract;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class GetListSuDelegateUserRequest
{
    public List<string>? BusinessUnitIds { get; init; }

    public DateTimeOffset? DelegatorStartDate { get; init; }

    public DateTimeOffset? DelegatorEndDate { get; init; }

    public string? DelegatorName { get; init; }

    public string? DelegatorPositionName { get; init; }

    public string? DelegateeName { get; init; }

    public string? DelegateePositionName { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;
}

public record GetListSuDelegateUserResponse(PaginatedQueryResult<DelegatorListDto> DelegatorListDto);

public record DelegatorListDto(
    DelegatorId Id,
    string DelegatorName,
    string DelegatorPositionName,
    DateTimeOffset DelegatorStartDate,
    DateTimeOffset DelegatorEndDate,
    string DelegateeName,
    string DelegateePositionName,
    DateTimeOffset? UpdatedAt);

public class GetListSuDelegateUser : SuDelegateUserEndpointBase<GetListSuDelegateUserRequest, Ok<PaginatedQueryResult<DelegatorListDto>>>
{
    private readonly Dp2DbContext dbContext;

    public GetListSuDelegateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetListSuDelegateUser> logger)
        : base(dbContext, permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Get("/st/st001");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<DelegatorListDto>>> HandleRequestAsync(GetListSuDelegateUserRequest req, CancellationToken ct)
    {
        var startDate = req.DelegatorStartDate?.Subtract(req.DelegatorStartDate.Value.TimeOfDay);
        var endTime = req.DelegatorEndDate?.Subtract(req.DelegatorEndDate.Value.TimeOfDay).AddDays(1).AddTicks(-1);

        var businessUnitIds = req.BusinessUnitIds?.Select(BusinessUnitId.From).ToList();
        var businessUnitNames = businessUnitIds is { Count: > 0 }
            ? await this.dbContext.RawBusinessUnits
                                  .Where(r => businessUnitIds.Contains(r.Id))
                                  .Select(r => r.Name)
                                  .ToListAsync(ct)
            : null;

        var listData = this.dbContext.SuDelegators
                           .Include(d => d.Delegatees)
                           .ThenInclude(de => de.RawBusinessUnit)
                           .Include(d => d.Delegatees)
                           .ThenInclude(de => de.ParentBusinessUnit)
                           .Where(d => !d.IsDeleted)
                           .WhereIfTrue(
                               businessUnitNames is { Count: > 0 },
                               d => this.dbContext.Set<RawEmployeeView>()
                                                  .Any(v => v.EmployeeCode == d.EmployeeCode
                                                         && businessUnitNames!.Contains(v.BusinessUnitName)))
                           .WhereIfTrue(
                               startDate.HasValue,
                               d => d.DelegationEndDate >= startDate!.Value)
                           .WhereIfTrue(
                               endTime.HasValue,
                               d => d.DelegationStartDate <= endTime!.Value)
                           .WhereIfTrue(
                               !string.IsNullOrWhiteSpace(req.DelegatorName),
                               d => EF.Functions.ILike(d.UserFullName, $"%{req.DelegatorName}%"))
                           .WhereIfTrue(
                               !string.IsNullOrWhiteSpace(req.DelegatorPositionName),
                               d => EF.Functions.ILike(d.FullPositionName, $"%{req.DelegatorPositionName}%"))
                           .WhereIfTrue(
                               !string.IsNullOrWhiteSpace(req.DelegateeName),
                               d => d.Delegatees.Any(de => EF.Functions.ILike(de.UserFullName, $"%{req.DelegateeName}%")))
                           .WhereIfTrue(
                               !string.IsNullOrWhiteSpace(req.DelegateePositionName),
                               d => d.Delegatees.Any(de => EF.Functions.ILike(de.ParentBusinessUnit.Name + " " + de.RawBusinessUnit.Name, $"%{req.DelegateePositionName}%")))
                           .OrderByDescending(d => d.DelegationStartDate);

        var groupData = listData.Select(d => new DelegatorListDto(
            d.Id,
            d.UserFullName,
            d.FullPositionName,
            d.DelegationStartDate,
            d.DelegationEndDate,
            string.Join(", ", d.Delegatees.OrderBy(de => de.BusinessUnitId).ThenBy(de => de.Sequence).Select(de => de.UserFullName)),
            string.Join(", ", d.Delegatees.OrderBy(de => de.BusinessUnitId).ThenBy(de => de.Sequence).Select(de => de.ParentBusinessUnit.Name + " " + de.RawBusinessUnit.Name)).Trim(),
            d.AuditInfo.LastModifiedAt));

        var paginated = await PaginatedList<DelegatorListDto>
            .CreateAsync(
                groupData,
                req.PageNumber,
                req.PageSize,
                ct);

        var result = paginated.ToResult(static d => new DelegatorListDto(
            d.Id,
            d.DelegatorName,
            d.DelegatorPositionName,
            d.DelegatorStartDate,
            d.DelegatorEndDate,
            d.DelegateeName,
            d.DelegateePositionName,
            d.UpdatedAt));

        return TypedResults.Ok(result);
    }
}