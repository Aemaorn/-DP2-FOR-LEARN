namespace GHB.DP2.Domain.SystemUtility;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.Raws;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct UserId
{
    public static UserId New() => From(Guid.CreateVersion7());
}

public class SuUser : AuditableEntity<UserId>
{
    public override UserId Id { get; init; }

    public EmployeeCode EmployeeCode { get; init; }

    public FileId? SignatureImageId { get; private set; }

    public string FullName => this.Employee.FullName;

    public bool IsActive { get; private set; }

    public int FailedLoginAttempts { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

    public UserOtherInfo? OtherInfo { get; private set; }

    public virtual RawEmployee Employee { get; init; }

    public virtual IReadOnlyCollection<RefreshToken> RefreshTokens { get; private set; }

    public virtual IReadOnlyCollection<SuRole> Roles { get; private set; }

    public virtual IReadOnlyCollection<SuDelegator> Delegators { get; init; }

    public virtual IReadOnlyCollection<SuDelegatee> Delegatees { get; init; }

    public virtual IReadOnlyCollection<SuNotification> Notifications { get; init; }

    public virtual SuSecretaryOwner? SecretaryOwner { get; init; }

    public SuDelegatee? Delegatee =>
        this.Delegators
            .Where(d =>
            {
                var start = DateTimeOffset.UtcNow.Date;
                var end = start.AddDays(1).AddTicks(-1);

                return d.DelegationStartDate <= start &&
                       d.DelegationEndDate >= end;
            })
            .SelectMany(d => d.Delegatees)
            .FirstOrDefault();

    public SuDelegatee? GetActiveDelegatee(RawBusinessUnit? businessUnit = null)
    {
        var activeDelegatees =
            this.Delegators
                .Where(d =>
                {
                    var now = DateTimeOffset.UtcNow;

                    return d.DelegationStartDate <= now &&
                           d.DelegationEndDate >= now;
                })
                .SelectMany(d =>
                    d.Delegatees
                     .Where(dg => dg.Active))
                .ToList();

        if (businessUnit is null)
        {
            return activeDelegatees.FirstOrDefault();
        }

        foreach (var bu in businessUnit.PathToRoot())
        {
            var match = activeDelegatees
                .FirstOrDefault(dg => (dg.BusinessUnitId ?? dg.ParentBusinessUnitId) == bu.Id);

            if (match is not null)
            {
                return match;
            }
        }

        return activeDelegatees.Count == 1 ? activeDelegatees[0] : null;
    }

    public SuDelegator? EffectiveDelegator =>
        this.Delegatees?
            .Where(dg => dg.Active)
            .Select(dg => dg.SuDelegator)
            .FirstOrDefault(del =>
            {
                var now = DateTimeOffset.UtcNow;

                return del.DelegationStartDate <= now &&
                       del.DelegationEndDate >= now;
            });

    public Unit AddRefreshToken(RefreshToken refreshToken)
    {
        var refreshTokens = this.RefreshTokens.ToHashSet();

        refreshTokens.Add(refreshToken);

        this.RefreshTokens = refreshTokens.OrderByDescending(t => t.Expires)
                                          .Take(5)
                                          .ToHashSet();

        return unit;
    }

    public Unit RemoveRefreshToken(string token)
    {
        var refreshTokens = this.RefreshTokens.ToHashSet();

        var refreshToken = refreshTokens.FirstOrDefault(rt => rt.Token == token);

        if (refreshToken is not null)
        {
            refreshTokens.Remove(refreshToken);
        }

        this.RefreshTokens = refreshTokens;

        return unit;
    }

    public bool IsLockedOut() =>
        this.LockoutEnd.HasValue && this.LockoutEnd.Value > DateTimeOffset.UtcNow;

    public void RecordFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        this.FailedLoginAttempts++;

        if (this.FailedLoginAttempts >= maxAttempts)
        {
            this.LockoutEnd = DateTimeOffset.UtcNow.Add(lockoutDuration);
        }
    }

    public void RecordSuccessfulLogin()
    {
        this.FailedLoginAttempts = 0;
        this.LockoutEnd = null;
    }

    public void UnlockAccount()
    {
        this.FailedLoginAttempts = 0;
        this.LockoutEnd = null;
    }

    public static SuUser Create(
        EmployeeCode employeeCode,
        FileId? signatureImageId,
        bool isActive,
        UserOtherInfo? otherInfo)
    {
        return new SuUser()
        {
            Id = UserId.New(),
            EmployeeCode = employeeCode,
            SignatureImageId = signatureImageId,
            IsActive = isActive,
            OtherInfo = otherInfo,
            Roles = [],
        };
    }

    public static SuUser Create(EmployeeCode employeeCode)
    {
        return new SuUser
        {
            Id = UserId.New(),
            EmployeeCode = employeeCode,
            IsActive = true,
            Roles = [],
        };
    }

    public SuUser SetIsActive(bool isActive)
    {
        this.IsActive = isActive;

        return this;
    }

    public SuUser Inactivate()
    {
        this.IsActive = false;

        return this;
    }

    public Unit Update(
        FileId? signatureImageId,
        bool isActive,
        UserOtherInfo? otherInfo)
    {
        this.SignatureImageId = signatureImageId;
        this.IsActive = isActive;
        this.OtherInfo = otherInfo;

        return unit;
    }

    public Unit AddRole(SuRole role)
    {
        if (this.Roles is null)
        {
            throw new InvalidOperationException("Roles cannot be null.");
        }

        if (this.Roles.Contains(role))
        {
            throw new InvalidOperationException($"Role {role.Name} already exists.");
        }

        var rolesHashSet = this.Roles.ToHashSet();

        rolesHashSet.Add(role);

        this.Roles = rolesHashSet;

        return unit;
    }

    public Unit RemoveRole(SuRole role)
    {
        if (this.Roles is null)
        {
            throw new InvalidOperationException("Roles cannot be null.");
        }

        if (!this.Roles.Contains(role))
        {
            throw new InvalidOperationException($"Role {role.Name} does not exist.");
        }

        var rolesHashSet = this.Roles.ToHashSet();

        var removeRole = rolesHashSet.FirstOrDefault(rt => rt.Code == role.Code);

        if (removeRole is not null)
        {
            rolesHashSet.Remove(removeRole);
        }

        this.Roles = rolesHashSet;

        return unit;
    }
}

public record UserOtherInfo(
    string? Telephone2);