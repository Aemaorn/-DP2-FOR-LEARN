namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Application.Features.Report.AuditAndRevenue.Abstract;
using GHB.DP2.Domain.Report.RpAuditAndRevenue;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteAuditAndRevenueRequest(Guid Id);

public class DeleteAuditAndRevenueEndpoint : AuditAndRevenueEndpoint<DeleteAuditAndRevenueRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteAuditAndRevenueEndpoint(ILogger<DeleteAuditAndRevenueEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("report/audit-revenue/{id:guid}");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("DeleteAuditAndRevenue")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(DeleteAuditAndRevenueRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenues.Include(rpAuditAndRevenue => rpAuditAndRevenue.Details)
                               .FirstOrDefaultAsync(x => x.Id == RpAuditAndRevenueId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        if (entity.Status != RpAuditAndRevenueStatus.Draft &&
            entity.Status != RpAuditAndRevenueStatus.Rejected &&
            entity.Status != RpAuditAndRevenueStatus.Edit)
        {
            return TypedResults.NotFound("อนุญาตให้ลบเฉพาะสถานะ แบบร่าง, ส่งกลับแก้ไข, หรือ เรียกคืนแก้ไข เท่านั้น");
        }

        this.dbContext.RpAuditAndRevenueDetails.RemoveRange(entity.Details);
        await this.dbContext.SaveChangesAsync(ct);

        this.dbContext.RpAuditAndRevenues.Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}