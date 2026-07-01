namespace GHB.DP2.Application.Features.SystemUtility.SuNotifications;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class MarkReadRequest
{
    [FromClaim("sub")]
    public Guid UserId { get; init; }

    public Guid NotificationId { get; init; }
}

public class MarkReadEndpoint
    : EndpointBase<MarkReadRequest, Results<NoContent, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public MarkReadEndpoint(
        ILogger<MarkReadEndpoint> logger,
        Dp2DbContext dbContext)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags(nameof(SuNotifications))
             .WithSummary("Mark a notification as read")
             .WithDescription("Marks a specific notification as read for the user.")
             .Accepts<MarkReadRequest>());
        this.Put("/su/notifications/{NotificationId}/mark-read");
    }

    protected override async ValueTask<Results<NoContent, NotFound<string>>> HandleRequestAsync(MarkReadRequest req, CancellationToken ct)
    {
        var userId = UserId.From(req.UserId);

        var anyUser =
            await this.dbContext
                      .SuUsers
                      .AnyAsync(
                          u => u.Id == userId,
                          ct);

        if (!anyUser)
        {
            return TypedResults.NotFound($"User with ID {req.UserId} not found.");
        }

        var notification =
            await this.dbContext
                      .SuNotifications
                      .SingleOrDefaultAsync(
                          n => n.Id == NotificationId.From(req.NotificationId) && n.UserId == userId,
                          ct);

        if (notification == null)
        {
            return TypedResults.NotFound($"Notification with ID {req.NotificationId} not found for user {req.UserId}.");
        }

        notification.MarkRead();

        this.dbContext.SuNotifications.Update(notification);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}