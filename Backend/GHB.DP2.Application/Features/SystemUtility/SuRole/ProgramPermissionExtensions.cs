namespace GHB.DP2.Application.Features.SystemUtility.SuRole;

using GHB.DP2.Application.Features.SystemUtility.SuRole.Dto;
using GHB.DP2.Domain.SystemUtility;

public static class ProgramPermissionExtensions
{
    public static SuRoleProgram MappingRoleProgram(this ProgramPermission programPermission) =>
        SuRoleProgram.Create(
            programPermission.ProgramId,
            programPermission.Permission);
}