namespace GHB.DP2.Application.Features.Announcement.AnnouncementInfo;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteAnnouncementInfoRequest(Guid Id);

public class DeleteAnnouncementInfoEndpoint : EndpointBase<DeleteAnnouncementInfoRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteAnnouncementInfoEndpoint(
        ILogger<DeleteAnnouncementInfoEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("announcement-info/{Id:guid}");
        this.Description(b => b
            .WithTags("AnnouncementInfo")
            .WithName("DeleteAnnouncementInfo")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(
        DeleteAnnouncementInfoRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.AnnouncementInfos
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementInfoId.From(req.Id))
            .SingleOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        entity.Delete();

        this.dbContext.AnnouncementInfos.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
