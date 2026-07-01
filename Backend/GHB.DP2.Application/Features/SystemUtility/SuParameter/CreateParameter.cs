namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Application.Features.SystemUtility.SuParameter.DTO;
using GHB.DP2.Application.Features.SystemUtility.SuParameter.Validator;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class CreateParameterRequest
{
    public string Group { get; init; }

    public string? SubGroup { get; init; }

    public Guid? ParentId { get; init; }

    public int Sequence { get; init; }

    public string Code { get; init; }

    public string Name { get; init; }

    public ParameterKeyValue[] Parameters { get; init; }

    public bool IsActive { get; init; }
}

public class CreateParameter
    : SecureEndpointBase<CreateParameterRequest,
                         Results<Ok<ParameterId>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public CreateParameter(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<CreateParameter> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuParameter"));
        this.Post("/st/st006");
        this.Validator<CreateParameterRequestValidator>();
        this.AuditLog("จัดการพารามิเตอร์", "สร้างพารามิเตอร์");
    }

    protected override async ValueTask<Results<Ok<ParameterId>, BadRequest<string>>> HandleRequestAsync(CreateParameterRequest req, CancellationToken ct)
    {
        var groupCode = req.SubGroup ?? req.Group;
        var group = await this.dbContext
                              .SuParameterGroups
                              .SingleOrDefaultAsync(x => x.Code == GroupCode.From(groupCode), ct);

        if (group is null)
        {
            return TypedResults.BadRequest($"ไม่พบข้อมูลกลุ่ม");
        }

        var data = await this.dbContext.SuParameters
                             .IgnoreQueryFilters()
                             .AnyAsync(x => x.Code == ParameterCode.From(req.Code) && x.GroupCode == GroupCode.From(req.Group), ct);

        if (data)
        {
            return TypedResults.BadRequest($"Code มีการใช้งานแล้ว");
        }

        if (req.ParentId.HasValue)
        {
            var parentExists = await this.dbContext.SuParameters
                                         .AnyAsync(x => x.Id == ParameterId.From(req.ParentId.Value), ct);

            if (!parentExists)
            {
                return TypedResults.BadRequest($"ไม่พบข้อมูล Parameter หลัก");
            }
        }

        var parameter = SuParameter.Create(
            group.Code,
            req.ParentId.HasValue ? ParameterId.From(req.ParentId.Value) : null,
            req.Code,
            req.Name,
            req.Sequence,
            req.Parameters
               .Where(x => x.Value != null)
               .ToDictionary(
                   x => x.Key,
                   x => new ParameterValue(x.Value!.Sequence, x.Value.Value)),
            req.IsActive);

        this.dbContext.SuParameters.Add(parameter);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(parameter.Id);
    }
}