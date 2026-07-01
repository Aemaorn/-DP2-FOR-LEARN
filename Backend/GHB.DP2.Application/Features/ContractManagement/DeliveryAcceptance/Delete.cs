namespace GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance;

using GHB.DP2.Application.Features.ContractManagement.DeliveryAcceptance.Abstract;
using GHB.DP2.Domain.ContractManagement.CmDeliveryAcceptance;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteDeliveryAcceptanceCommand(Guid Id);

public class DeleteDeliveryAcceptanceEndpoint : DeliveryAcceptanceEndpointBase<DeleteDeliveryAcceptanceCommand, Results<NoContent, NotFound<string>, Conflict<string>>>
{
    private readonly Dp2DbContext dbcontext;

    public DeleteDeliveryAcceptanceEndpoint(
        Dp2DbContext dbContext,
        ILogger<DeleteDeliveryAcceptanceEndpoint> logger)
        : base(dbContext, logger)
    {
        this.dbcontext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/DeliveryAcceptance")
             .WithName("DeleteDeliveryAcceptanceById")
             .Produces<NoContent>());
        this.Delete("delivery-acceptance/{Id:guid}");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>, Conflict<string>>> HandleRequestAsync(DeleteDeliveryAcceptanceCommand req, CancellationToken ct)
    {
        var entity = await this.dbcontext
                               .CmDeliveryAcceptances
                               .Include(p => p.Periods)
                               .FirstOrDefaultAsync(c => c.Id == CmDeliveryAcceptanceId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายงานผลการตรวจรับ (จพ.008)");
        }

        if (entity.Periods.Any())
        {
            return TypedResults.Conflict("มีงวดงานอยู่, ไม่สามารถลบได้");
        }

        this.dbcontext.CmDeliveryAcceptances.Remove(entity);
        await this.dbcontext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}