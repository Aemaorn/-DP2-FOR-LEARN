namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Features.Report.ContractCompletionByQuarter.Abstract;
using GHB.DP2.Domain.Report.RpContractCompletionByQuarter;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteReportContractCompletionByQuarterRequest(Guid Id);

public class DeleteReportContractCompletionByQuarterEndpoint : ContractCompletionByQuarterEndpoint<DeleteReportContractCompletionByQuarterRequest, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteReportContractCompletionByQuarterEndpoint(ILogger<DeleteReportContractCompletionByQuarterEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("report/contract-completion-by-quarter/{id:guid}");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("DeleteReportContractCompletionByQuarter")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(DeleteReportContractCompletionByQuarterRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarters
                               .FirstOrDefaultAsync(x => x.Id == RpContractCompletionByQuarterId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        if (entity.Status != RpContractCompletionByQuarterStatus.Draft &&
            entity.Status != RpContractCompletionByQuarterStatus.Rejected &&
            entity.Status != RpContractCompletionByQuarterStatus.Edit)
        {
            return TypedResults.NotFound("อนุญาตให้ลบเฉพาะสถานะ แบบร่าง, ส่งกลับแก้ไข, หรือ เรียกคืนแก้ไข เท่านั้น");
        }

        this.dbContext.RpContractCompletionByQuarters.Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);
        return TypedResults.Ok();
    }
}