namespace GHB.DP2.Application.Features.Procurement.P79Clause2;

using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record P79Clause2DeleteRequest(Guid Id);

public class DeleteP79Clause2EndPoint : EndpointBase<P79Clause2DeleteRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteP79Clause2EndPoint(
        Dp2DbContext dbContext,
        ILogger<DeleteP79Clause2EndPoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("P79Clause2")
             .WithName("DeleteP79Clause2")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("p79clause2/{Id:guid}");
        this.AuditLog("รายการจัดซื้อจัดจ้าง กรณีเร่งด่วน", "ลบรายการจัดซื้อจัดจ้าง กรณีเร่งด่วน");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(P79Clause2DeleteRequest req, CancellationToken ct)
    {
        var data = await this.dbContext
                             .P79Clause2s
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == P79Clause2Id.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง กรณีเร่งด่วน");
        }

        if (data.Status != P79Clause2Status.Draft && data.Status != P79Clause2Status.Edit)
        {
            return TypedResults.NotFound("ข้อมูลรายการจัดซื้อจัดจ้าง กรณีเร่งด่วน ที่ไม่ใช่สถานะแบบร่างและเรียกคืนแก้ไขไม่สามารถลบได้");
        }

        data.SetIsActive(false);
        this.dbContext.P79Clause2s.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}