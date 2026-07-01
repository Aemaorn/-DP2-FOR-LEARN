namespace GHB.DP2.Domain.Common;

using Codehard.Common.DomainModel;
using LanguageExt;

public record AuditInfo(
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    string CreatedByName)
{
    public Guid? LastModifiedBy { get; private set; }

    public DateTimeOffset? LastModifiedAt { get; private set; }

    public string? LastModifiedByName { get; private set; }

    public AuditInfo Update(
        Guid lastModifiedBy, DateTimeOffset lastModifiedAt, string lastModifiedByName)
    {
        return this with
        {
            LastModifiedBy = lastModifiedBy,
            LastModifiedAt = lastModifiedAt,
            LastModifiedByName = lastModifiedByName,
        };
    }
}

public interface IAuditableEntity
{
    AuditInfo AuditInfo { get; }

    Unit Update(Guid userId, string name);
}

public abstract class AuditableEntity<TKey> : Entity<TKey>, IAuditableEntity
    where TKey : struct
{
    public virtual AuditInfo AuditInfo { get; private set; }

    public Unit Update(Guid userId, string name)
    {
        if (this.AuditInfo == null)
        {
            this.AuditInfo = new AuditInfo(userId, DateTimeOffset.UtcNow, name);
        }
        else
        {
            this.AuditInfo = this.AuditInfo.Update(userId, DateTimeOffset.UtcNow, name);
        }

        return unit;
    }
}