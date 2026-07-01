namespace GHB.DP2.Application.Features.Procurement.Pw184;

using GHB.DP2.Domain.Procurement.Pw184;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePw184Request(Guid Id);

public class DeletePw184Endpoint : EndpointBase<DeletePw184Request, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeletePw184Endpoint(Dp2DbContext dbContext, ILogger<DeletePw184Endpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Pw184")
             .WithName("DeletePw184")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("pw184/{Id:guid}");
        this.AuditLog("รายการ ว 184", "ลบรายการ ว 184");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(
        DeletePw184Request req,
        CancellationToken ct)
    {
        var data = await this.dbContext.Pw184s
                             .AsNoTracking()
                             .SingleOrDefaultAsync(w => w.Id == Pw184Id.From(req.Id), ct);

        if (data is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลรายการ ว 184");
        }

        if (data.Status != Pw184Status.Draft && data.Status != Pw184Status.Edit && data.Status != Pw184Status.Rejected)
        {
            return TypedResults.NotFound("ไม่สามารถลบข้อมูลได้ เนื่องจากสถานะไม่ใช่แบบร่าง เรียกคืนแก้ไข หรือส่งกลับแก้ไข");
        }

        this.dbContext.Pw184s.Remove(data);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
