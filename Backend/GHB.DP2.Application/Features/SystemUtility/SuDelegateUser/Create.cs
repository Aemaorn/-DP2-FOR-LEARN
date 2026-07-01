namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using FluentValidation;
using GHB.DP2.Application.Features.SystemUtility.SuDelegateUser.Abstract;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using GHB.DP2.Application.Services;

public record CreateSuDelegateUserRequest(
    SuDelegateCreateUserRequestDto Delegator,
    IEnumerable<SuDelegateeCreateUserRequestDto> Delegatees);

public record SuDelegateCreateUserRequestDto(
    Guid SuUserId,
    string UserFullName,
    string PositionId,
    string FullPositionName,
    DateTimeOffset DelegationStartDate,
    DateTimeOffset DelegationEndDate,
    string Annotation);

public record SuDelegateeCreateUserRequestDto(
    string DelegatorPositionId,
    string DelegatorBusinessUnitId,
    string? ParentBusinessUnitId,
    Guid SuUserId,
    string UserFullName,
    string PositionId,
    string FullPositionName,
    string? BusinessUnitId,
    string? SubBusinessUnitId,
    bool Active,
    int Sequence);

public record CreateSuDelegateUserResponse(Guid Id);

public class CreateSuDelegateUserValidation : Validator<CreateSuDelegateUserRequest>
{
    public CreateSuDelegateUserValidation()
    {
        this.RuleFor(x => x.Delegator.SuUserId)
            .NotNull()
            .NotEmpty()
            .WithMessage("Delegator SuUserId is required");

        this.RuleFor(x => x.Delegator.UserFullName)
            .NotNull()
            .NotEmpty()
            .WithMessage("Delegator UserFullName is required");

        this.RuleFor(x => x.Delegator.PositionId)
            .NotNull()
            .NotEmpty()
            .WithMessage("Delegator PositionId is required");

        this.RuleFor(x => x.Delegator.DelegationEndDate)
            .GreaterThanOrEqualTo(x => x.Delegator.DelegationStartDate)
            .WithMessage("Delegation end date must be greater than or equal to the start date");

        this.RuleForEach(x => x.Delegatees)
            .ChildRules(delegatees =>
            {
                delegatees.RuleFor(x => x.DelegatorPositionId)
                          .NotNull()
                          .NotEmpty()
                          .WithMessage("Delegator PositionId is required");

                delegatees.RuleFor(x => x.DelegatorBusinessUnitId)
                          .NotNull()
                          .NotEmpty()
                          .WithMessage("Delegator BusinessUnitId is required");

                delegatees.RuleFor(x => x.SuUserId)
                          .NotNull()
                          .NotEmpty()
                          .WithMessage("Delegatee SuUserId is required");

                delegatees.RuleFor(x => x.UserFullName)
                          .NotNull()
                          .NotEmpty()
                          .WithMessage("Delegatee UserFullName is required");

                delegatees.RuleFor(x => x.PositionId)
                          .NotNull()
                          .NotEmpty()
                          .WithMessage("Delegatee PositionId is required");
            });
    }
}

public class CreateDelegateUser : SuDelegateUserEndpointBase<CreateSuDelegateUserRequest, Results<Ok<CreateSuDelegateUserResponse>, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateDelegateUser(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateDelegateUser> logger)
        : base(dbContext, permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Post("/st/st001");
        this.AuditLog("กำหนดผู้รับมอบหมาย", "สร้างผู้รับมอบหมาย");
    }

    protected override async ValueTask<Results<Ok<CreateSuDelegateUserResponse>, NotFound<string>, Conflict<string>>> HandleRequestAsync(
        CreateSuDelegateUserRequest req,
        CancellationToken ct)
    {
        var (suUser, rawEmpPosition) = await this.TryGetUserAndRawEmpPosition(req.Delegator.SuUserId, ct);

        if (suUser == null)
        {
            this.ThrowError("User not found.", StatusCodes.Status404NotFound);
        }

        if (rawEmpPosition == null)
        {
            this.ThrowError("Primary position for the delegatee not found.", StatusCodes.Status404NotFound);
        }

        var dateInRangeExist = await this.dbContext.SuDelegators
                                         .AnyAsync(
                                             d =>
                                                 d.SuUserId == UserId.From(req.Delegator.SuUserId) &&
                                                 d.DelegationStartDate <= req.Delegator.DelegationEndDate &&
                                                 d.DelegationEndDate >= req.Delegator.DelegationStartDate,
                                             cancellationToken: ct);

        if (dateInRangeExist)
        {
            return TypedResults.Conflict("Delegation dates overlap with existing delegations for this user.");
        }

        var createDelegatorRequest = new CreateSuDelegatorRequest(
            suUser.Id,
            suUser.EmployeeCode,
            req.Delegator.UserFullName,
            rawEmpPosition.PositionId,
            req.Delegator.FullPositionName,
            req.Delegator.DelegationStartDate,
            req.Delegator.DelegationEndDate,
            req.Delegator.Annotation);

        var createDelegator = SuDelegator.Create(createDelegatorRequest);

        foreach (var delegatee in req.Delegatees)
        {
            var delegateeDto = new CreateSuDelegateeBaseDto(
                delegatee.SuUserId,
                delegatee.DelegatorPositionId,
                delegatee.DelegatorBusinessUnitId,
                delegatee.ParentBusinessUnitId,
                delegatee.BusinessUnitId,
                delegatee.SubBusinessUnitId,
                delegatee.UserFullName,
                delegatee.FullPositionName,
                delegatee.Active,
                delegatee.Sequence);

            var newDelegatee = await this.CreateSuDelegateeBase(
                createDelegator,
                delegateeDto,
                ct);

            createDelegator.AddDelegatee(newDelegatee);
        }

        this.dbContext.SuDelegators.Add(createDelegator);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(
            new CreateSuDelegateUserResponse(createDelegator.Id.Value));
    }
}