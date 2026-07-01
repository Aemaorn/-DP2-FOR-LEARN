namespace GHB.DP2.Application.Extensions;

using GHB.DP2.Domain.SystemUtility;

public static class SecretaryExtensions
{
    private static bool IsEffectiveOn(this SuSecretary secretary, DateOnly today) =>
        secretary.Active == true
        && (secretary.EffectiveStartDate is null || secretary.EffectiveStartDate <= today)
        && (secretary.EffectiveEndDate is null || secretary.EffectiveEndDate >= today);

    /// <summary>
    /// Returns all active secretary UserIds for the given user (as a principal).
    /// </summary>
    public static IEnumerable<UserId> GetActiveSecretaryUserIds(this SuSecretaryOwner? secretaryOwner)
    {
        if (secretaryOwner is null)
        {
            yield break;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var secretary in secretaryOwner.Secretaries.Where(s => s.IsEffectiveOn(today)))
        {
            yield return secretary.SuUserId;
        }
    }

    /// <summary>
    /// Returns UserIds to notify: the principal user and all active secretaries.
    /// </summary>
    public static IEnumerable<UserId> GetSecretaryNotificationTargets(this SuUser user)
    {
        yield return user.Id;

        if (user.SecretaryOwner is null)
        {
            yield break;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var secretary in user.SecretaryOwner.Secretaries.Where(s => s.IsEffectiveOn(today)))
        {
            if (secretary.SuUserId != user.Id)
            {
                yield return secretary.SuUserId;
            }
        }
    }

    /// <summary>
    /// Returns true if the given userId is an active secretary of this user.
    /// </summary>
    public static bool IsSecretaryOf(this SuUser user, UserId secretaryUserId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return user.SecretaryOwner?.Secretaries
            .Any(s => s.SuUserId == secretaryUserId && s.IsEffectiveOn(today)) ?? false;
    }
}
