namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using FluentValidation;
using GHB.DP2.Application.Features.SystemUtility.SuDelegateUser.Abstract;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public record UpdateSuDelegateUserRequest(
    Guid DelegatorId,
    SuDelegateUpdateUserRequestDto Delegator,
    IEnumerable<SuDelegateeUpdateUserRequestDto> Delegatees);

public record SuDelegateUpdateUserRequestDto(
    DateTimeOffset DelegationStartDate,
    DateTimeOffset DelegationEndDate,
    string Annotation);

public record SuDelegateeUpdateUserRequestDto(
    Guid? Id,
    string? BusinessUnitId,
    bool Active,
    string? DelegatorPositionId,
    string? DelegatorBusinessUnitId,
    string? ParentBusinessUnitId,
    string? SubBusinessUnitId,
    string? Acting,
    Guid? SuUserId,
    string? UserFullName,
    string? PositionId,
    string? FullPositionName,
    int Sequence);

public record UpdateSuDelegateUserResponse(Guid Id);

public class UpdateSuDelegateUserValidation : Validator<UpdateSuDelegateUserRequest>
{
    public UpdateSuDelegateUserValidation()
    {
        this.RuleForEach(x => x.Delegatees)
            .ChildRules(delegatee =>
            {
                delegatee.RuleFor(d => d.BusinessUnitId);
            });
    }
}

public class UpdateDelegateUser : SuDelegateUserEndpointBase<UpdateSuDelegateUserRequest, Results<Ok<UpdateSuDelegateUserResponse>, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateDelegateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateDelegateUser> logger)
        : base(dbContext, permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Put("/st/st001/{DelegatorId:guid}");
        this.AuditLog("กำหนดผู้รับมอบหมาย", "แก้ไขผู้รับมอบหมาย");
    }

    protected override async ValueTask<Results<Ok<UpdateSuDelegateUserResponse>, NotFound<string>, Conflict<string>>> HandleRequestAsync(
        UpdateSuDelegateUserRequest req,
        CancellationToken ct)
    {
        var delegatorData = await this.dbContext.SuDelegators
                                      .Where(d => d.Id == DelegatorId.From(req.DelegatorId))
                                      .SingleOrDefaultAsync(ct);

        if (delegatorData == null)
        {
            return TypedResults.NotFound($"Delegator with ID {req.DelegatorId} not found.");
        }

        var hasUsedDelegatees = await this.dbContext.SuDelegators
                                          .Where(d => d.Id == DelegatorId.From(req.DelegatorId))
                                          .SelectMany(d => d.Delegatees)
                                          .AnyAsync(dt => dt.DelegateeHistories.Any(), ct);

        if (hasUsedDelegatees)
        {
            return TypedResults.Conflict("ไม่สามารถแก้ไขปฏิบัติหน้าที่แทนได้");
        }

        delegatorData.Update(
            req.Delegator.DelegationStartDate,
            req.Delegator.DelegationEndDate,
            req.Delegator.Annotation);

        this.dbContext.SuDelegators.Update(delegatorData);

        var delegateeList = await this.dbContext.SuDelegatees
                                      .Where(d =>
                                          d.DelegatorId == delegatorData.Id)
                                      .ToListAsync(ct);

        foreach (var delegatee in req.Delegatees)
        {
            if (!string.IsNullOrWhiteSpace(delegatee.BusinessUnitId))
            {
                var existingDelegatee = delegateeList.SingleOrDefault(d =>
                    d.DelegatorBusinessUnitId == BusinessUnitId.From(delegatee.BusinessUnitId) &&
                    d.DelegatorId != DelegatorId.From(req.DelegatorId));

                if (existingDelegatee != null)
                {
                    return TypedResults.Conflict($"ไม่สามารถเลือกปฏิบัติหน้าที่แทนหน่วยงานตำแหน่งที่ 2 ซ้ำกันได้");
                }
            }

            if (delegatee.Id == null)
            {
                await this.CreateNewDelegatee(delegatorData, delegatee, ct);
            }
            else
            {
                this.UpdateExistingDelegatee(delegatee, delegateeList);
            }
        }

        var toRemove = delegateeList.Where(x => req.Delegatees.Where(d => d.Id.HasValue).All(d => x.Id != DelegateeId.From(d.Id.Value))).ToList();

        foreach (var del in toRemove)
        {
            delegatorData.RemoveDelegatee(del);
        }

        this.dbContext.SuDelegatees.RemoveRange(toRemove);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new UpdateSuDelegateUserResponse(req.DelegatorId));
    }

    private async Task CreateNewDelegatee(SuDelegator delegatorData, SuDelegateeUpdateUserRequestDto delegatee, CancellationToken ct)
    {
        var delegateeDto = new CreateSuDelegateeBaseDto(
            delegatee.SuUserId.Value,
            delegatee.DelegatorPositionId!,
            delegatee.DelegatorBusinessUnitId!,
            delegatee.ParentBusinessUnitId,
            delegatee.BusinessUnitId,
            delegatee.SubBusinessUnitId,
            delegatee.UserFullName!,
            delegatee.FullPositionName ?? string.Empty,
            delegatee.Active,
            delegatee.Sequence);

        var newDelegatee = await this.CreateSuDelegateeBase(
            delegatorData,
            delegateeDto,
            ct);

        delegatorData.AddDelegatee(newDelegatee);
    }

    private void UpdateExistingDelegatee(
        SuDelegateeUpdateUserRequestDto delegatee,
        List<SuDelegatee> delegateeListt)
    {
        var delegateeData = delegateeListt.SingleOrDefault(d => d.Id == DelegateeId.From(delegatee.Id!.Value));

        if (delegateeData != null)
        {
            delegateeData.Update(delegatee.Sequence, !string.IsNullOrWhiteSpace(delegatee.ParentBusinessUnitId) ? BusinessUnitId.From(delegatee.ParentBusinessUnitId) : null, !string.IsNullOrWhiteSpace(delegatee.BusinessUnitId) ? BusinessUnitId.From(delegatee.BusinessUnitId) : null, !string.IsNullOrWhiteSpace(delegatee.SubBusinessUnitId) ? BusinessUnitId.From(delegatee.SubBusinessUnitId) : null);

            delegateeData.UpdateActive(delegatee.Active);
            this.dbContext.SuDelegatees.Update(delegateeData);
        }
    }
}