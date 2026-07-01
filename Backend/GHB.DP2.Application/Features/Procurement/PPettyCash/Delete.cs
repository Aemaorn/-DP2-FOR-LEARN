namespace GHB.DP2.Application.Features.Procurement.PPettyCash;

using GHB.DP2.Domain.Procurement.PPettyCash;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePettyCashRequest(Guid Id);

public class DeletePPettyCashEndPoint : EndpointBase<DeletePettyCashRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeletePPettyCashEndPoint(
        Dp2DbContext dbContext,
        ILogger<DeletePPettyCashEndPoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("PPettyCash")
             .WithName("DeletePettyCash")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("PPettyCash/{Id:guid}");
        this.AuditLog("รายการจัดซื้อจัดจ้าง เงินสดย่อย", "ลบรายการจัดซื้อจัดจ้าง เงินสดย่อย");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeletePettyCashRequest req, CancellationToken ct)
    {
        var data = await this.dbContext.PPettyCashs
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == PettyCashId.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้าง เงินสดย่อย");
        }

        if (data.Status != PettyCashStatus.Draft && data.Status != PettyCashStatus.Edit)
        {
            return TypedResults.NotFound("ข้อมูลรายการจัดซื้อจัดจ้าง เงินสดย่อย ที่ไม่ใช่สถานะแบบร่างและเรียกคืนแก้ไขไม่สามารถลบได้");
        }

        data.SetActive(false);

        this.dbContext.PPettyCashs.Update(data);
        this.dbContext.PPettyCashs.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}