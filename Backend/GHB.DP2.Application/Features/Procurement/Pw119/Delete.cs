namespace GHB.DP2.Application.Features.Procurement.Pw119;

using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePW119Request(Guid Id);

public class DeletePW119EndPoint : EndpointBase<DeletePW119Request, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeletePW119EndPoint(
        Dp2DbContext dbContext,
        ILogger<DeletePW119EndPoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw119")
             .WithName("DeletePw119")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("pw119/{Id:guid}");
        this.AuditLog("รายการจัดซื้อจัดจ้าง ว 119", "ลบรายการจัดซื้อจัดจ้าง ว 119");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeletePW119Request req, CancellationToken ct)
    {
        var data = await this.dbContext.Pw119s
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == Pw119Id.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการจัดซื้อจัดจ้างว 119");
        }

        if (data.Status != Pw119Status.Draft && data.Status != Pw119Status.Edit)
        {
            return TypedResults.NotFound("ข้อมูลรายการจัดซื้อจัดจ้าง ว 119 ที่ไม่ใช่สถานะแบบร่างและเรียกคืนแก้ไขไม่สามารถลบได้");
        }

        data.SetIsActive(false);
        this.dbContext.Pw119s.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}