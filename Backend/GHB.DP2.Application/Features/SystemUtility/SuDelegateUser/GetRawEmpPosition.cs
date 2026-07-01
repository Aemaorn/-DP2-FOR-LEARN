namespace GHB.DP2.Application.Features.SystemUtility.SuDelegateUser;

using FluentValidation;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.Raws.Constants;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class GetListRawEmpPositionRequest
{
    public string EmployeeCode { get; init; }
}

public record GetListRawEmpPositionResponse(
    PositionId PositionId,
    BusinessUnitId BusinessUnitId,
    string Label,
    IEnumerable<RawEmpPositionLevelOneResponse> LevelOnes);

public record RawEmpPositionLevelOneResponse(
    BusinessUnitId? ParentBusinessUnitId,
    BusinessUnitId BusinessUnitId,
    string Label,
    IEnumerable<RawEmpPositionLevelTwoResponse> LevelTwos);

public record RawEmpPositionLevelTwoResponse(
    BusinessUnitId? ParentBusinessUnitId,
    BusinessUnitId BusinessUnitId,
    string Label,
    IEnumerable<RawEmpPositionLevelThreeResponse> LevelThrees);

public record RawEmpPositionLevelThreeResponse(
    BusinessUnitId? ParentBusinessUnitId,
    BusinessUnitId BusinessUnitId,
    string Label);

public class GetListRawEmpPositionRequestValidator : Validator<GetListRawEmpPositionRequest>
{
    public GetListRawEmpPositionRequestValidator()
    {
        this.RuleFor(x => x.EmployeeCode)
            .NotEmpty()
            .WithMessage("Employee code is required.");
    }
}

public class GetListRawEmpPosition(
    Dp2DbContext dbContext,
    ILogger<GetListRawEmpPosition> logger)
    : EndpointBase<
            GetListRawEmpPositionRequest,
            Ok<IEnumerable<GetListRawEmpPositionResponse>>>(logger)
{
    public override void Configure()
    {
        this.Options(x => x.WithTags("SuDelegateUser"));
        this.Get("st/st001/business-unit-position/{EmployeeCode}");
        this.AllowAnonymous();
    }

    protected override async ValueTask<Ok<IEnumerable<GetListRawEmpPositionResponse>>> HandleRequestAsync(
        GetListRawEmpPositionRequest req,
        CancellationToken ct)
    {
        var employeeCode = EmployeeCode.From(req.EmployeeCode);

        var rawEmployeePositions = await dbContext.RawEmployeePositions
                                                  .Include(r => r.Position)
                                                  .Include(r => r.BusinessUnit)
                                                  .Where(r => r.EmployeeCode == employeeCode)
                                                  .OrderBy(r =>
                                                      r.Acting == EmployeeConstant.Acting.Primary ? 0 :
                                                      r.Acting == EmployeeConstant.Acting.ActingPosition ? 1 :
                                                      r.Acting == EmployeeConstant.Acting.Temporary ? 2 : 3)
                                                  .AsNoTracking()
                                                  .ToListAsync(ct);

        var businessUnitIds = rawEmployeePositions
                              .Select(r => r.BusinessUnitId)
                              .ToList();

        var levelOneBusinessUnits = await dbContext.RawBusinessUnits
                                                   .Where(bu => bu.ParentId.HasValue && businessUnitIds.Contains(bu.ParentId.Value))
                                                   .AsNoTracking<RawBusinessUnit>()
                                                   .ToListAsync(ct);

        var levelOneIds = levelOneBusinessUnits
                          .Select(bu => bu.Id)
                          .ToList();
        var levelTwoBusinessUnits = await dbContext.RawBusinessUnits
                                                   .Where(bu => bu.ParentId.HasValue && levelOneIds.Contains(bu.ParentId.Value))
                                                   .AsNoTracking()
                                                   .ToListAsync(ct);

        var result = rawEmployeePositions.Select(position =>
            this.CreatePositionResponse(
                position,
                levelOneBusinessUnits,
                levelTwoBusinessUnits));

        return TypedResults.Ok(result);
    }

    private GetListRawEmpPositionResponse CreatePositionResponse(
        RawEmployeePosition position,
        List<RawBusinessUnit> levelOneBusinessUnits,
        List<RawBusinessUnit> levelTwoBusinessUnits)
    {
        return new GetListRawEmpPositionResponse(
            position.PositionId,
            position.BusinessUnitId,
            $"{position.Position.Name} {position.BusinessUnit.Name}",
            this.GetLevelOneBusinessUnits(position, levelOneBusinessUnits, levelTwoBusinessUnits));
    }

    private IEnumerable<RawEmpPositionLevelOneResponse> GetLevelOneBusinessUnits(
        RawEmployeePosition position,
        List<RawBusinessUnit> levelOneBusinessUnits,
        List<RawBusinessUnit> levelTwoBusinessUnits)
    {
        var childLevelTwoUnits = levelOneBusinessUnits
                                 .Where(l1 => l1.ParentId == position.BusinessUnitId)
                                 .Select(l1 => new RawEmpPositionLevelTwoResponse(
                                     l1.ParentId,
                                     l1.Id,
                                     l1.Name,
                                     GetLevelThreeBusinessUnits(l1, levelTwoBusinessUnits)));

        var rootBusinessUnit = new RawEmpPositionLevelOneResponse(
            null,
            position.BusinessUnitId,
            position.BusinessUnit.Name,
            childLevelTwoUnits);

        return new[] { rootBusinessUnit };
    }

    private static IEnumerable<RawEmpPositionLevelThreeResponse> GetLevelThreeBusinessUnits(
        RawBusinessUnit levelTwoUnit,
        List<RawBusinessUnit> levelhreeBusinessUnits)
    {
        return levelhreeBusinessUnits
               .Where(l3 => l3.ParentId == levelTwoUnit.Id)
               .Select(l3 => new RawEmpPositionLevelThreeResponse(
                   l3.ParentId,
                   l3.Id,
                   l3.Name));
    }
}