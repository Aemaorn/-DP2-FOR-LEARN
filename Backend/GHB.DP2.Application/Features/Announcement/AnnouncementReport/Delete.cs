namespace GHB.DP2.Application.Features.Announcement.AnnouncementReport;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteAnnouncementReportRequest(Guid Id);

public class DeleteAnnouncementReportEndpoint : EndpointBase<DeleteAnnouncementReportRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteAnnouncementReportEndpoint(
        ILogger<DeleteAnnouncementReportEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("announcement-report/{Id:guid}");
        this.Description(b => b
            .WithTags("AnnouncementReport")
            .WithName("DeleteAnnouncementReport")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(
        DeleteAnnouncementReportRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.AnnouncementReports
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementReportId.From(req.Id))
            .SingleOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        entity.Delete();

        this.dbContext.AnnouncementReports.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
