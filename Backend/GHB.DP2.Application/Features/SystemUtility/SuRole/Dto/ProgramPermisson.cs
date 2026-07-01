namespace GHB.DP2.Application.Features.SystemUtility.SuRole.Dto;

using GHB.DP2.Domain.SystemUtility;

public sealed record ProgramPermission(ProgramId ProgramId, Permission Permission);

public sealed record ProgramPermissionResponse(
    int Sorting,
    Guid ProgramId,
    string Code,
    string Name,
    Permission Permission,
    string GroupName);