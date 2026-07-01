namespace GHB.DP2.Application.Features.SystemUtility.SuParameter;

using GHB.DP2.Application.Services;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class DeleteParameterRequest
{
    public Guid Id { get; init; }
}

public class DeleteParameter :
    SecureEndpointBase<DeleteParameterRequest,
                       Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteParameter(
        Dp2DbContext dbContext,
        IPermissionValidationService permissionValidationService,
        ILogger<DeleteParameter> logger)
        : base(permissionValidationService, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(x => x.WithTags("SuParameter"));
        this.Delete("/st/st006/{Id:guid}");
        this.AuditLog("จัดการพารามิเตอร์", "ลบพารามิเตอร์");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeleteParameterRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.SuParameters
            .FirstOrDefaultAsync(x => x.Id == ParameterId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("Parameter not found");
        }

        this.dbContext.SuParameters.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
