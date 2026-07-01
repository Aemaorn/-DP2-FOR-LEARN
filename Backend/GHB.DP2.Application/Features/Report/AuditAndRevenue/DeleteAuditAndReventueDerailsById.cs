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

public sealed record DeleteAuditAndRevenueDetailsById(Guid Id, Guid DetailId);

public class DeleteAuditAndRevenueDetailsByIdEndpoint : AuditAndRevenueEndpoint<DeleteAuditAndRevenueDetailsById, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteAuditAndRevenueDetailsByIdEndpoint(ILogger<DeleteAuditAndRevenueEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient, IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("report/audit-revenue/{id:guid}/detail/{detailId:guid}");
        this.Description(b => b
                              .WithTags("Report/AuditAndRevenue")
                              .WithName("DeleteAuditAndRevenueDetailsById")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(DeleteAuditAndRevenueDetailsById req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpAuditAndRevenueDetails
                               .FirstOrDefaultAsync(x => x.RpAuditAndRevenue.Id == RpAuditAndRevenueId.From(req.Id) && x.Id == RpAuditAndRevenueDetailId.From(req.DetailId), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        this.dbContext.RpAuditAndRevenueDetails.Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}