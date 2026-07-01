namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Application.Features.SystemUtility.SuParameter.DTO;
using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UpdateParameterRequest
{
    public Guid Id { get; init; }

    public string Group { get; init; }

    public string? SubGroup { get; init; }

    public Guid? ParentId { get; init; }

    public int Sequence { get; init; }

    public string Code { get; init; }

    public string Name { get; init; }

    public ParameterKeyValue[] Parameters { get; init; }

    public bool IsActive { get; init; }
}

public record UpdateParameterResponse(
    Guid Id,
    string Group,
    string? SubGroup,
    Guid? ParentId,
    string Code,
    string Name,
    int Sequence,
    ParameterKeyValue[] Values,
    bool IsActive);

public class UpdateParameter :
    SecureEndpointBase<UpdateParameterRequest,
                       Results<Ok<UpdateParameterResponse>, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public UpdateParameter(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<UpdateParameter> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuParameter"));
        this.Put("/st/st006/{Id:guid}");
        this.AuditLog("จัดการพารามิเตอร์", "แก้ไขพารามิเตอร์");
    }

    protected override async ValueTask<Results<Ok<UpdateParameterResponse>, NotFound<string>, BadRequest<string>>> HandleRequestAsync(UpdateParameterRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuParameters
                             .Include(p => p.Group)
                             .FirstOrDefaultAsync(x => x.Id == ParameterId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลนี้ในพารามิเตอร์");
        }

        var subGroup = !string.IsNullOrWhiteSpace(req.SubGroup) && data.Group.Code.Value == req.SubGroup
            ? data.Group
            : null;

        if (!string.IsNullOrWhiteSpace(req.SubGroup) && subGroup is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลกลุ่ม");
        }

        var group = await this.dbContext.SuParameters
                              .IgnoreQueryFilters()
                              .AnyAsync(x => x.Code == ParameterCode.From(req.Code) && x.Id != ParameterId.From(req.Id) && x.GroupCode == GroupCode.From(req.Group), ct);

        if (group)
        {
            return TypedResults.BadRequest($"Code มีการใช้งานแล้ว");
        }

        if (req.ParentId.HasValue)
        {
            if (req.ParentId.Value == req.Id)
            {
                return TypedResults.BadRequest($"ไม่สามารถกำหนด Parameter หลักเป็นตัวเองได้");
            }

            var parentExists = await this.dbContext.SuParameters
                                         .AnyAsync(x => x.Id == ParameterId.From(req.ParentId.Value), ct);

            if (!parentExists)
            {
                return TypedResults.BadRequest($"ไม่พบข้อมูล Parameter หลัก");
            }
        }

        data.Update(
            data.GroupCode,
            subGroup,
            req.ParentId.HasValue ? ParameterId.From(req.ParentId.Value) : null,
            req.Sequence,
            req.Name,
            req.Parameters
               .Where(x => x.Value != null)
               .ToDictionary(
                   x => x.Key,
                   x => new ParameterValue(x.Value!.Sequence, x.Value.Value)),
            req.IsActive);

        await this.dbContext.SaveChangesAsync(ct);

        var response = new UpdateParameterResponse(
            data.Id.Value,
            data.GroupCode.Value,
            data.Group?.Code.Value,
            data.ParentId?.Value,
            data.Code.Value,
            data.Label,
            data.Sequence,
            [.. data.Values
                .Select(x => new ParameterKeyValue
                {
                    Key = x.Key,
                    Value = new ParameterKeyValue.ParameterValues(
                        x.Value.Sequence,
                        x.Value.Value),
                })],
            data.IsActive);

        return TypedResults.Ok(response);
    }
}