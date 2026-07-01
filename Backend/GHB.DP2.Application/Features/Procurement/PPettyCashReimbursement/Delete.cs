namespace GHB.DP2.Application.Features.Procurement.PPettyCashReimbursement;

using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeletePettyCashReimbursementRequest(Guid Id);

public class DeletePettyCashReimbursementEndpoint : EndpointBase<DeletePettyCashReimbursementRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeletePettyCashReimbursementEndpoint(
        Dp2DbContext dbContext,
        ILogger<DeletePettyCashReimbursementEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("Procurement/PPettyCashReimbursement")
             .WithName("DeletePettyCashReimbursement")
             .Produces<NoContent>()
             .Produces<NotFound>());
        this.Delete("petty-cash-reimbursement/{Id:guid}");
        this.AuditLog("เบิกเงินชดเชยเงินสดย่อย", "ลบเบิกเงินชดเชยเงินสดย่อย");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(DeletePettyCashReimbursementRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PPettyCashReimbursements
            .FirstOrDefaultAsync(e => e.Id == PPettyCashReimbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลเบิกเงินชดเชยเงินสดย่อย");
        }

        if (entity.Status is not (PPettyCashReimbursementStatus.Draft or PPettyCashReimbursementStatus.Edit))
        {
            return TypedResults.NotFound("ข้อมูลเบิกเงินชดเชยเงินสดย่อยที่ไม่ใช่สถานะแบบร่างหรือเรียกคืนแก้ไขไม่สามารถลบได้");
        }

        this.dbContext.PPettyCashReimbursements.Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}