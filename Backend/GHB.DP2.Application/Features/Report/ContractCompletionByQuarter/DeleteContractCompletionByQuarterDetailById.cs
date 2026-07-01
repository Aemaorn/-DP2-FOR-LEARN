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

public sealed record DeleteContractCompletionByQuarterDetailById(
    Guid Id,
    Guid DetailId);

public class DeleteContractCompletionByQuarterDetailByIdEndpoint : ContractCompletionByQuarterEndpoint<DeleteContractCompletionByQuarterDetailById, Results<Ok, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteContractCompletionByQuarterDetailByIdEndpoint(ILogger<DeleteReportContractCompletionByQuarterEndpoint> logger, Dp2DbContext dbContext, IFileServiceClient fileServiceClient)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("report/contract-completion-by-quarter/{id:guid}/detail/{detailId:guid}");
        this.Description(b => b
                              .WithTags("Report/ContractCompletionByQuarter")
                              .WithName("DeleteReportContractCompletionByQuarterDetailByid")
                              .AllowAnonymous()
                              .Produces(StatusCodes.Status200OK)
                              .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>>> HandleRequestAsync(DeleteContractCompletionByQuarterDetailById req, CancellationToken ct)
    {
        var entity = await this.dbContext.RpContractCompletionByQuarterDetails
                               .FirstOrDefaultAsync(
                                   x =>
                                       x.RpContractCompletionByQuarter.Id == RpContractCompletionByQuarterId.From(req.Id) &&
                                       x.Id == RpContractCompletionByQuarterDetailId.From(req.DetailId),
                                   ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลรายงานรหัส {req.Id}");
        }

        this.dbContext.RpContractCompletionByQuarterDetails.Remove(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}