namespace GHB.DP2.Domain.SystemUtility.Event;

using Codehard.Common.DomainModel;
using FastEndpoints;

public record SendEmailEvent(
    NotificationId Id,
    DateTimeOffset Timestamp) : IDomainEvent<NotificationId>, IEvent
{
    public static SendEmailEvent Create(NotificationId id)
    {
        return new SendEmailEvent(id, DateTimeOffset.UtcNow);
    }
}