namespace GHB.DP2.Domain.SystemUtility;

using GHB.DP2.Domain.Common;
using LanguageExt;
using Vogen;

[ValueObject<Guid>(Conversions.EfCoreValueConverter | Conversions.SystemTextJson)]
public partial struct RoleProgramId
{
    public static RoleProgramId New() => From(Guid.CreateVersion7());
}

[Flags]
public enum Permission
{
    None = 0b0000,
    View = 0b0001,
    Manage = View | 0b0010,
}

public class SuRoleProgram : AuditableEntity<RoleProgramId>
{
    public SuRoleProgram()
    {
        // Reserved for EF Proxy
    }

    private SuRoleProgram(
        RoleProgramId id,
        ProgramId programId,
        bool isView,
        bool isManage)
    {
        this.Id = id;
        this.ProgramId = programId;
        this.IsView = isView;
        this.IsManage = isManage;
    }

    public override RoleProgramId Id { get; init; }

    public ProgramId ProgramId { get; init; }

    public bool IsView { get; private set; }

    public bool IsManage { get; private set; }

    public Permission Permission =>
        (this.IsView ? Permission.View : Permission.None) |
        (this.IsManage ? Permission.Manage : Permission.None);

    public virtual SuRole Role { get; protected init; }

    public virtual SuProgram Program { get; protected init; }

    public static SuRoleProgram Create(ProgramId programId, Permission permission)
    {
        var canView = HasPermission(Permission.View);
        var canManage = HasPermission(Permission.Manage);

        return new SuRoleProgram(
            RoleProgramId.New(),
            programId,
            canView,
            canManage);

        bool HasPermission(Permission perm)
        {
            return permission.HasFlag(perm);
        }
    }

    public Unit Update(Permission permission)
    {
        this.IsView = permission.HasFlag(Permission.View);
        this.IsManage = permission.HasFlag(Permission.Manage);

        return unit;
    }
}