namespace GHB.DP2.Domain.Common;

using Codehard.FileService.Contracts.ValueObjects;
using LanguageExt;

public interface IDocumentHistory
{
    FileId FileId { get; }

    string Version { get; }

    DateTimeOffset CreatedAt { get; }

    Guid CreatedBy { get; }

    string CreatedByName { get; }

    string? Remark { get; }

    public bool IsReplaced { get; }

    Unit Create(Guid userId, string name);
}

public static class DocumentHistoryExtensions
{
    /// <summary>
    /// Determines the next version string by incrementing either the major or minor version
    /// of the latest version in the provided collection of document histories.
    /// </summary>
    /// <typeparam name="TDocumentHistory">The type of document history implementing the IDocumentHistory interface.</typeparam>
    /// <param name="documentHistories">A collection of document histories to determine the latest version from.</param>
    /// <param name="incrementMajor">
    /// A boolean value indicating whether to increment the major version.
    /// If true, the major version is incremented, and the minor version is reset to 0.
    /// Otherwise, the minor version is incremented.
    /// </param>
    /// <returns>
    /// A string representing the next version. Defaults to "1.0" if no valid version exists in the collection.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the latest version has an invalid format that cannot be parsed.
    /// </exception>
    public static string NextVersion<TDocumentHistory>(
        this IEnumerable<TDocumentHistory> documentHistories,
        bool incrementMajor = false)
        where TDocumentHistory : IDocumentHistory
    {
        var enumerable = documentHistories as TDocumentHistory[] ?? [.. documentHistories];

        var latestVersion = enumerable
                            .OrderVersions()
                            .Select(dh => dh.Version)
                            .FirstOrDefault();

        if (string.IsNullOrEmpty(latestVersion))
        {
            return "1.0";
        }

        var versionParts = latestVersion.Split('.');

        if (versionParts.Length < 2)
        {
            throw new InvalidOperationException("Invalid version format.");
        }

        var majorVersion = int.Parse(versionParts[0]);
        var minorVersion = int.Parse(versionParts[1]);

        if (incrementMajor)
        {
            majorVersion++;
            minorVersion = 0; // Reset a minor version when major is incremented
        }
        else
        {
            minorVersion++;
        }

        return $"{majorVersion}.{minorVersion}";
    }

    /// <summary>
    /// Orders a collection of document histories by their version in descending order.
    /// The versions are treated as numerical major. Minor pairs for comparison.
    /// </summary>
    /// <typeparam name="TDocumentHistory">The type of document history implementing the IDocumentHistory interface.</typeparam>
    /// <param name="documentHistories">A collection of document histories to be ordered by version.</param>
    /// <returns>
    /// An ordered collection of document histories, sorted by their version in descending order.
    /// </returns>
    /// <exception cref="FormatException">
    /// Thrown if any version string in the collection does not follow the expected major. Minor format.
    /// </exception>
    public static IOrderedEnumerable<TDocumentHistory> OrderVersions<TDocumentHistory>(
        this IEnumerable<TDocumentHistory> documentHistories)
        where TDocumentHistory : IDocumentHistory
    {
        return documentHistories
            .OrderByDescending(d =>
            {
                var versionParts = d.Version.Split('.');

                return (int.Parse(versionParts[0]), int.Parse(versionParts[1]));
            });
    }
}

public abstract class DocumentHistory<TKey> : IDocumentHistory
    where TKey : struct
{
    public abstract TKey Id { get; init; }

    public FileId FileId { get; protected set; }

    public string Version { get; protected set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void SetVersion(string version)
    {
        this.Version = version;
    }

    public Guid CreatedBy { get; private set; }

    public string CreatedByName { get; private set; }

    public string? Remark { get; init; }

    public bool IsReplaced { get; init; }

    public Unit Create(Guid userId, string name)
    {
        this.CreatedBy = userId;
        this.CreatedAt = DateTimeOffset.UtcNow;
        this.CreatedByName = name;

        return Unit.Default;
    }
}