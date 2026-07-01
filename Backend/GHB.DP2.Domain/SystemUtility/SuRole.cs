namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<string>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RoleCode;

public class SuRole : IAuditableEntity
{
    private readonly List<SuRoleProgram> rolePrograms = new();

    public RoleCode Code { get; private init; }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public AuditInfo AuditInfo { get; private set; }

    public virtual IReadOnlyCollection<SuRoleProgram> RolePrograms => this.rolePrograms;

    public virtual IReadOnlyCollection<SuUser> Users { get; init; }

    public static SuRole Create(
        RoleCode code,
        string name,
        bool isActive)
    {
        return new SuRole
        {
            Code = code,
            Name = name,
            IsActive = isActive,
        };
    }

    public Unit Update(
        string name,
        bool isActive)
    {
        this.Name = name;
        this.IsActive = isActive;

        return unit;
    }

    public Unit AddProgram(SuRoleProgram program)
    {
        this.rolePrograms.Add(program);

        return unit;
    }

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