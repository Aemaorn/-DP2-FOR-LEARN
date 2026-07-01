namespace GHB.DP2.Application.Extensions;

public static class RunningDocumentVersion
{
    private const string DefaultVersion = "1.0";
    private const int MinimumVersionParts = 2;

    public static string IncrementDocumentVersion(
        string currentVersion,
        string currentStatus,
        string statusToCompare)
    {
        if (!IsValidVersion(currentVersion))
        {
            return DefaultVersion;
        }

        var versionParts = currentVersion.Split('.');
        var statusChanged = !currentStatus.Equals(statusToCompare);

        return statusChanged
            ? IncrementMajorVersion(versionParts)
            : IncrementMinorVersion(versionParts);
    }

    private static bool IsValidVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            return false;
        }

        var parts = version.Split('.');

        return parts.Length >= MinimumVersionParts;
    }

    private static string IncrementMajorVersion(string[] versionParts)
    {
        var majorVersion = int.Parse(versionParts[0]) + 1;

        return $"{majorVersion}.{versionParts[1]}";
    }

    private static string IncrementMinorVersion(string[] versionParts)
    {
        var minorVersion = int.Parse(versionParts[1]) + 1;

        return $"{versionParts[0]}.{minorVersion}";
    }
}