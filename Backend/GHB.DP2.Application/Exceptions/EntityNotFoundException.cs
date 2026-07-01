namespace GHB.DP2.Application.Exceptions;

/// <summary>
/// Exception thrown when a required entity is not found in the system.
/// </summary>
public class EntityNotFoundException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class.
    /// </summary>
    /// <param name="entityName">The name of the entity that was not found.</param>
    /// <param name="entityId">The identifier of the entity that was not found.</param>
    public EntityNotFoundException(string entityName, object entityId)
        : base($"{entityName} with ID '{entityId}' not found.")
    {
        this.EntityName = entityName;
        this.EntityId = entityId?.ToString();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityNotFoundException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom error message.</param>
    public EntityNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Gets the name of the entity that was not found.
    /// </summary>
    public string? EntityName { get; }

    /// <summary>
    /// Gets the identifier of the entity that was not found.
    /// </summary>
    public string? EntityId { get; }
}
