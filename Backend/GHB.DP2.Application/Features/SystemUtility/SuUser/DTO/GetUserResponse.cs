namespace GHB.DP2.Application.Features.SystemUtility.SuUser.DTO;

using Codehard.FileService.Contracts.ValueObjects;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;

public record GetUserByIdResponse(
    UserId Id,
    string Name,
    string? DepartmentCode,
    string? DepartmentName,
    string? DepartmentOrganizationLevel,
    bool IsJorPor,
    string? Email,
    string? PositionCode,
    string? PositionName,
    bool IsActive,
    FileId? SignatureImageId,
    IEnumerable<UserRole> Role,
    EmployeeCode EmployeeCode,
    string? OrganizationLevel,
    string? BusinessUnitCode,
    bool IsLockedOut);

public record UserRole(
    RoleCode RoleCode);