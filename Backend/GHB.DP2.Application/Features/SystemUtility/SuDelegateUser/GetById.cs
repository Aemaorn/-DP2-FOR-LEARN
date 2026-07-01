namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using GHB.DP2.Application.Features.SystemUtility.SuDelegateUser.Abstract;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public class GetSuDelegateUserByIdRequest
{
    public Guid Id { get; init; }
}

public record GetSuDelegateUserByIdResponse(
    DelegatorResponse Delegator,
    IEnumerable<DelegateeResponse> Delegatees);

public record DelegatorResponse(
    DelegatorId Id,
    string EmployeeCode,
    string UserFullName,
    string FullPositionName,
    string Email,
    DateTimeOffset DelegationStartDate,
    DateTimeOffset DelegationEndDate,
    string Annotation);

public record DelegateeResponse(
    DelegateeId Id,
    UserId SuUserId,
    string DelegatorPositionId,
    string DelegatorBusinessUnitId,
    string UserFullName,
    string FullPositionName,
    string Email,
    BusinessUnitId? ParentBusinessUnitId,
    BusinessUnitId? BusinessUnitId,
    BusinessUnitId? SubBusinessUnitId,
    bool Active,
    int Sequence);

public class GetSuDelegateUserById : SuDelegateUserEndpointBase<GetSuDelegateUserByIdRequest, Results<Ok<GetSuDelegateUserByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetSuDelegateUserById(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<GetSuDelegateUserById> logger)
        : base(dbContext, permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Get("/st/st001/{id:guid}");
    }

    protected override async ValueTask<Results<Ok<GetSuDelegateUserByIdResponse>, NotFound<string>>> HandleRequestAsync(GetSuDelegateUserByIdRequest req, CancellationToken ct)
    {
        var delegator = await this.dbContext.SuDelegators
                                  .Include(d => d.SuUser)
                                  .ThenInclude(u => u.Employee)
                                  .Include(d => d.Delegatees)
                                  .ThenInclude(d => d.SuUser)
                                  .ThenInclude(d => d.Employee)
                                  .AsNoTracking()
                                  .SingleOrDefaultAsync(d => d.Id == DelegatorId.From(req.Id), ct);

        if (delegator is null)
        {
            return TypedResults.NotFound($"SuDelegateUser with Id {req.Id} not found");
        }

        var response = new GetSuDelegateUserByIdResponse(
            new DelegatorResponse(
                delegator.Id,
                delegator.EmployeeCode.Value,
                delegator.UserFullName,
                delegator.FullPositionName,
                delegator.SuUser.Employee.Email,
                delegator.DelegationStartDate,
                delegator.DelegationEndDate,
                delegator.Annotation),
            delegator.Delegatees.OrderBy(s => s.Sequence).Select(d => new DelegateeResponse(
                d.Id,
                d.SuUserId,
                d.DelegatorPositionId.Value,
                d.DelegatorBusinessUnitId.Value,
                d.UserFullName,
                d.FullPositionName,
                d.SuUser.Employee.Email,
                d.ParentBusinessUnitId,
                d.BusinessUnitId,
                d.SubBusinessUnitId,
                d.Active,
                d.Sequence)));

        return TypedResults.Ok(response);
    }

    private BusinessUnitId? HandleGetParentRawBusinessUnit(BusinessUnitId rootDelegatorbusinessUnitId, BusinessUnitId businessUnitId)
    {
        if (rootDelegatorbusinessUnitId == businessUnitId)
        {
            return businessUnitId;
        }

        var rawBusinessUnit = this.dbContext.RawBusinessUnits
                                  .AsNoTracking()
                                  .SingleOrDefault(bu => bu.Id == businessUnitId);

        if (rawBusinessUnit is null)
        {
            return null;
        }

        return rawBusinessUnit.ParentId;
    }
}