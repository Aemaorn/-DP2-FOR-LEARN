namespace GHB.DP2.Application.Features.Announcement.AnnouncementSorKorRor;

using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record DeleteAnnouncementSorKorRorRequest(Guid Id);

public class DeleteAnnouncementSorKorRorEndpoint : EndpointBase<DeleteAnnouncementSorKorRorRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public DeleteAnnouncementSorKorRorEndpoint(
        ILogger<DeleteAnnouncementSorKorRorEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Delete("announcement-sor-kor-ror/{Id:guid}");
        this.Description(b => b
            .WithTags("AnnouncementSorKorRor")
            .WithName("DeleteAnnouncementSorKorRor")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .Produces<string>(StatusCodes.Status404NotFound));
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(
        DeleteAnnouncementSorKorRorRequest req,
        CancellationToken ct)
    {
        var entity = await this.dbContext.AnnouncementSorKorRors
            .Where(x => !x.IsDeleted && x.Id == Domain.AnnouncementInfo.AnnouncementSorKorRorId.From(req.Id))
            .SingleOrDefaultAsync(ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูล");
        }

        entity.Delete();

        this.dbContext.AnnouncementSorKorRors.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
