namespace GHB.DP2.Application.EventHandlers.SuNotifications;

using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Notification
{
    private UserId UserId { get; init; }

    private string Title { get; init; }

    private string Message { get; init; }

    private NotificationProgram Program { get; init; }

    private Guid ReferenceId { get; set; }

    private string? LinkUrl { get; set; }

    private string? LinkButtonText { get; set; }

    public Notification SetReferenceId(Guid referenceId)
    {
        this.ReferenceId = referenceId;

        return this;
    }

    public Notification SetLinkUrl(string linkUrl, string linkButtonText)
    {
        this.LinkUrl = linkUrl;
        this.LinkButtonText = linkButtonText;

        return this;
    }

    public Task PublishAsync(CancellationToken ct)
    {
        var notification =
            SuNotification
                .Create(
                    this.UserId,
                    this.Title,
                    this.Message)
                .SetProgram(this.Program)
                .SetReferenceId(this.ReferenceId);

        if (this.LinkUrl != null && this.LinkButtonText != null)
        {
            notification.SetLink(this.LinkUrl, this.LinkButtonText);
        }

        return new SaveNotification(notification)
            .PublishAsync(Mode.WaitForNone, ct);
    }

    public static Notification Crate(
        UserId userId,
        string title,
        string message,
        NotificationProgram program)
    {
        return new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Program = program,
        };
    }
}

public record SaveNotification(SuNotification SuNotification) : IEvent;

public class SaveNotificationHandler : IEventHandler<SaveNotification>
{
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<SaveNotificationHandler> logger;

    public SaveNotificationHandler(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<SaveNotificationHandler> logger)
    {
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public async Task HandleAsync(SaveNotification eventModel, CancellationToken ct)
    {
        this.logger.LogInformation(
            "Handling SaveNotification for UserId: {UserId}, Title: {Title}",
            eventModel.SuNotification.UserId,
            eventModel.SuNotification.Title);

        await using var scope = this.serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Dp2DbContext>();

        var configuration = scope.ServiceProvider.GetService<IConfiguration>();
        var baseUrl = configuration?.GetValue<string>("EmailBaseUrl:BaseUrl");

        var existingLink = eventModel.SuNotification.LinkUrl;
        if (!string.IsNullOrWhiteSpace(existingLink))
        {
            var full = BuildFullUrl(existingLink, baseUrl);
            var buttonText = eventModel.SuNotification.LinkButtonText ?? string.Empty;

            eventModel.SuNotification.SetLink(full, buttonText);
        }

        dbContext.SuNotifications.Add(eventModel.SuNotification);

        try
        {
            await dbContext.SaveChangesAsync(ct);
            this.logger.LogInformation(
                "Notification saved successfully for UserId: {UserId}, Title: {Title}",
                eventModel.SuNotification.UserId,
                eventModel.SuNotification.Title);
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Failed to save notification for UserId: {UserId}, Title: {Title}",
                eventModel.SuNotification.UserId,
                eventModel.SuNotification.Title);
        }
    }

    private static string BuildFullUrl(string linkUrl, string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(linkUrl))
        {
            return baseUrl ?? string.Empty;
        }

        // If already absolute, return as-is
        if (linkUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            linkUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return linkUrl;
        }

        var trimmedBase = baseUrl?.TrimEnd('/') ?? string.Empty;
        if (string.IsNullOrEmpty(trimmedBase))
        {
            return linkUrl;
        }

        return trimmedBase + "/" + linkUrl.TrimStart('/');
    }
}