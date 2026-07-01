namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser.Abstract;

using System.Linq.Expressions;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public abstract class SuDelegateUserEndpointBase<TRequest, TResponse> : SecureEndpointBase<TRequest, TResponse>
    where TResponse : IResult
    where TRequest : notnull
{
    private readonly Dp2DbContext dbContext;

    protected SuDelegateUserEndpointBase(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public record CreateSuDelegateeBaseDto(
        Guid SuUserId,
        string DelegatorPositionId,
        string DelegatorBusinessUnitId,
        string? ParentBusinessUnitId,
        string? RawBusinessUnitId,
        string? SubBusinessUnitId,
        string UserFullName,
        string FullPositionName,
        bool Active,
        int Sequence);

    protected async Task<SuDelegatee> CreateSuDelegateeBase(
        SuDelegator delegator,
        CreateSuDelegateeBaseDto delegateeDto,
        CancellationToken ct)
    {
        var (suUser, rawEmpPosition) = await this.TryGetUserAndRawEmpPosition(delegateeDto.SuUserId, ct);

        if (suUser == null)
        {
            this.ThrowError("User not found.", StatusCodes.Status404NotFound);
        }

        if (rawEmpPosition == null)
        {
            this.ThrowError("Primary position for the delegatee not found.", StatusCodes.Status404NotFound);
        }

        var delegatorDtoPositionId = PositionId.From(delegateeDto.DelegatorPositionId);
        var delegatorDtoBusinessUnitId = BusinessUnitId.From(delegateeDto.DelegatorBusinessUnitId);

        var dateNow = DateTimeOffset.UtcNow;

        var havingDelegation = await this.dbContext.SuDelegators
                                         .AnyAsync(
                                             d => d.SuUserId == suUser.Id &&
                                                  d.DelegationStartDate <= dateNow &&
                                                  dateNow <= d.DelegationEndDate,
                                             cancellationToken: ct);

        if (havingDelegation)
        {
            this.ThrowError(
                $"User {delegateeDto.SuUserId} already has an active delegation.",
                StatusCodes.Status409Conflict);
        }

        var businessUnit = !string.IsNullOrWhiteSpace(delegateeDto.RawBusinessUnitId)
            ? await this.dbContext.RawBusinessUnits
                        .Where(
                            p => p.Id == BusinessUnitId.From(delegateeDto.RawBusinessUnitId!))
                        .SingleOrDefaultAsync(ct)
            : null;

        var parentBusinessUnit = !string.IsNullOrWhiteSpace(delegateeDto.ParentBusinessUnitId)
            ? await this.dbContext.RawBusinessUnits
                        .Where(
                            p => p.Id == BusinessUnitId.From(delegateeDto.ParentBusinessUnitId!))
                        .SingleOrDefaultAsync(ct)
            : null;

        var subBusinessUnit = !string.IsNullOrWhiteSpace(delegateeDto.SubBusinessUnitId)
            ? await this.dbContext.RawBusinessUnits
                        .Where(
                            p => p.Id == BusinessUnitId.From(delegateeDto.SubBusinessUnitId!))
                        .SingleOrDefaultAsync(ct)
            : null;

        var rawEmployeePosition = await this.dbContext.RawEmployeePositions
                                            .Where(r => r.EmployeeCode == delegator.EmployeeCode)
                                            .Where(r =>
                                                r.PositionId == delegatorDtoPositionId &&
                                                r.BusinessUnitId == delegatorDtoBusinessUnitId)
                                            .SingleOrDefaultAsync(ct);

        if (rawEmployeePosition == null)
        {
            this.ThrowError(
                $"Delegator position with ID {delegateeDto.DelegatorPositionId} not found in business unit {delegateeDto.DelegatorBusinessUnitId}.",
                StatusCodes.Status404NotFound);
        }

        var baseDelegateeQuery = this.dbContext.SuDelegatees
                                     .Where(d => d.DelegatorId == delegator.Id &&
                                                 d.Active &&
                                                 d.DelegatorPositionId == delegatorDtoPositionId &&
                                                 d.DelegatorBusinessUnitId == delegatorDtoBusinessUnitId);

        var ownAllBusinessUnit = await baseDelegateeQuery
                                       .Where(d => d.BusinessUnitId == d.DelegatorBusinessUnitId)
                                       .SingleOrDefaultAsync(ct);

        if (ownAllBusinessUnit != null)
        {
            this.ThrowError(
                $"Delegatee with position {delegateeDto.DelegatorPositionId} in business unit {delegateeDto.DelegatorBusinessUnitId} already has all business units delegated.",
                StatusCodes.Status409Conflict);
        }

        if (delegateeDto.RawBusinessUnitId != null)
        {
            var existingDelegatee = await baseDelegateeQuery
                                          .Where(d => d.BusinessUnitId == BusinessUnitId.From(delegateeDto.RawBusinessUnitId))
                                          .SingleOrDefaultAsync(ct);

            if (existingDelegatee != null)
            {
                this.ThrowError(
                    $"ไม่สามารถเลือกปฏิบัติหน้าที่แทนหน่วยงานตำแหน่งที่ 2 ซ้ำกันได้",
                    StatusCodes.Status409Conflict);
            }
        }

        var delegatorPositionName = await this.GetFullPositionNameByBuIdAndPositionId(
            delegator.EmployeeCode,
            delegatorDtoPositionId,
            delegatorDtoBusinessUnitId,
            businessUnit?.Id,
            ct);

        var createDelegateeRequest = new CreateSuDelegateeRequest(
            delegator.Id,
            delegateeDto.Sequence,
            rawEmployeePosition.PositionId,
            rawEmployeePosition.BusinessUnitId,
            parentBusinessUnit?.Id,
            delegatorPositionName,
            rawEmployeePosition.Acting,
            suUser.Id,
            suUser.EmployeeCode,
            delegateeDto.UserFullName,
            rawEmpPosition.PositionId,
            delegateeDto.FullPositionName,
            businessUnit?.Id,
            subBusinessUnit?.Id,
            delegateeDto.Active);

        return SuDelegatee.Create(createDelegateeRequest);
    }

    private async Task<string> GetFullPositionNameByBuIdAndPositionId(
        EmployeeCode employeeCode,
        PositionId delegatorPositionId,
        BusinessUnitId delegatorBusinessUnitId,
        BusinessUnitId? businessUnitId,
        CancellationToken ct)
    {
        var rawPosition = await this.dbContext.RawPositions
                                    .AsNoTracking()
                                    .SingleOrDefaultAsync(p => p.Id == delegatorPositionId, ct);

        if (rawPosition == null)
        {
            this.ThrowError(
                $"Position with ID {delegatorPositionId} not found.",
                StatusCodes.Status404NotFound);
        }

        Expression<Func<RawBusinessUnit, bool>> getRawBusinessUnitNameCondition = b =>
            b.Id == businessUnitId;

        var rawBusinessUnitName = await this.dbContext.RawBusinessUnits
                                            .SingleOrDefaultAsync(getRawBusinessUnitNameCondition, ct);

        var rawEmployeePositions = await this.dbContext.RawEmployeePositions
                                             .Include(r => r.Position)
                                             .Include(r => r.BusinessUnit)
                                             .Where(r => r.EmployeeCode == employeeCode)
                                             .Where(r => r.PositionId == delegatorPositionId)
                                             .WhereIfTrue(
                                                 rawBusinessUnitName != null && delegatorBusinessUnitId == businessUnitId,
                                                 r => r.BusinessUnitId == rawBusinessUnitName!.Id)
                                             .OrderBy(r =>
                                                 r.Acting == EmployeeConstant.Acting.Primary ? 0 :
                                                 r.Acting == EmployeeConstant.Acting.ActingPosition ? 1 :
                                                 r.Acting == EmployeeConstant.Acting.Temporary ? 2 : 3)
                                             .ToListAsync(ct);

        if (rawEmployeePositions == null)
        {
            this.ThrowError(
                "Position not found for the given employee code, position ID, and business unit ID.",
                StatusCodes.Status404NotFound);
        }

        return $"{rawPosition.Name} {rawBusinessUnitName?.Name}";
    }

    protected async Task<(SuUser? User, RawEmployeePosition? Position)> TryGetUserAndRawEmpPosition(
        Guid userId,
        CancellationToken ct)
    {
        var user = await this.dbContext.SuUsers
                             .Include(u => u.Employee)
                             .Where(u => u.Id == UserId.From(userId))
                             .SingleOrDefaultAsync(ct);

        if (user == null)
        {
            return (null, null);
        }

        var position = await this.dbContext.RawEmployeePositions
                                 .Where(r => r.EmployeeCode == user.EmployeeCode)
                                 .Where(r => r.Acting == EmployeeConstant.Acting.Primary)
                                 .SingleOrDefaultAsync(ct);

        return (user, position);
    }
}