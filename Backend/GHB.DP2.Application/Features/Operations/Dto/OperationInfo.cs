namespace GHB.DP2.Application.Features.Operations.Dto;

using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;

public record OperationInfo(
    UserId UserId,
    EmployeeCode EmployeeCode,
    string? Signature,
    string FullName,
    PositionId PositionId,
    string FullPositionName,
    int OrganizationLevel,
    BusinessUnitId BusinessUnitId,
    string BusinessUnitName,
    string? CommandNumber,
    decimal? CommandBudget,
    string? InRefCode);

public record OperationPositionInfo(
    string? PositionName,
    string? RefBankOrder,
    string? ShortPositionName,
    decimal? Budget,
    string? InRefCode,
    SectionId? SectionId,
    string? CommandNumber,
    decimal? CommandBudget);