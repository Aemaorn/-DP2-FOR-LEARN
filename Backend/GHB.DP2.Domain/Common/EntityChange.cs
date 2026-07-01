namespace GHB.DP2.Domain.Common;

public class EntityChange
{
    public string EntityType { get; set; }

    public string EntityId { get; set; }

    public string ChangeType { get; set; } // Added, Modified, Deleted

    public List<PropertyChange> ChangedProperties { get; private set; } = new();
}

public class PropertyChange
{
    public string PropertyName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
