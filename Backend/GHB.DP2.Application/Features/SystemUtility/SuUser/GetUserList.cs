namespace GHB.DP2.Application.Features.SystemUtility.SuUser;

using Codehard.Infrastructure.EntityFramework;
using FluentValidation;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Infrastructure;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetUserListRequest(
    string? SearchText,
    string? GroupCode,
    string? LineCode,
    string? DepartmentCode,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 10);

public class GetUserListRequestValidator : Validator<GetUserListRequest>
{
    public GetUserListRequestValidator()
    {
        this.RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage("Page number must be greater than 0")
            .LessThanOrEqualTo(1000)
            .WithMessage("Page number cannot exceed 1000");

        this.RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100 items per page");
    }
}

public sealed record UserInfo(
    Guid Id,
    string? Name,
    EmployeeCode EmployeeCode,
    PositionId? PositionCode,
    string? PositionName,
    BusinessUnitId? DepartmentCode,
    string? DepartmentName,
    string? Email,
    string? LastModifiedByName,
    DateTimeOffset? LastModifiedAt,
    bool IsActive,
    Guid? DelegateeId,
    string? OrganizationLevel,
    bool IsLockedOut);

public class GetUserListEndpoint : EndpointBase<GetUserListRequest, Ok<PaginatedQueryResult<UserInfo>>>
{
    private readonly Dp2DbContext dbContext;

    public GetUserListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetUserListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuUser"));
        this.Get("/st/st005");
    }

    protected override async ValueTask<Ok<PaginatedQueryResult<UserInfo>>> HandleRequestAsync(GetUserListRequest req, CancellationToken ct)
    {
        var query =
            this.dbContext.SuUsers
                .Include(u => u.Employee)
                .ThenInclude(e => e.View)
                .Where(u => u.IsActive)
                .WhereIfTrue(
                    !req.SearchText.IsNullOrEmpty(),
                    u => u.Employee.View!.FullName.Contains(req.SearchText!)
                         || u.Employee.Email.Contains(req.SearchText!)
                         || ((string)u.Employee.Id).Contains(req.SearchText!))
                .WhereIfTrue(
                    req.IsActive.HasValue,
                    u => u.IsActive == req.IsActive)
                .AsNoTracking()
                .OrderBy(o => o.Employee.View!.FullName)
                .AsQueryable();

        if (!req.DepartmentCode.IsNullOrEmpty() || !req.LineCode.IsNullOrEmpty() || !req.GroupCode.IsNullOrEmpty())
        {
            var reqBusinessUnitCode = req.GroupCode ?? req.LineCode ?? req.DepartmentCode;

            query = query.Where(u => u.Employee.View!.BusinessUnitId == BusinessUnitId.From(reqBusinessUnitCode!));
        }

        var result =
            await PaginatedList<Domain.SystemUtility.SuUser>.CreateAsync(
                query,
                req.PageNumber,
                req.PageSize,
                ct);

        return TypedResults.Ok(result.ToResult(u => new UserInfo(
            (Guid)u.Id,
            u.Employee.View?.FullName,
            u.Employee.Id,
            u.Employee.View?.PositionId,
            u.Employee.View?.FullPositionName.Trim(),
            u.Employee.PrimaryDepartment?.Id,
            u.Employee.PrimaryDepartment?.Name,
            u.Employee.Email.ToLower(),
            u.AuditInfo.LastModifiedByName,
            u.AuditInfo.LastModifiedAt,
            u.IsActive,
            u.Delegatee != null ? u.Delegatee.SuUserId.Value : null,
            u.Employee.PrimaryOrganizationLevel,
            u.IsLockedOut())));
    }
}