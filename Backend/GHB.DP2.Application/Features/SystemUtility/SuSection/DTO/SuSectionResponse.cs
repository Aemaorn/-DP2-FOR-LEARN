namespace GHB.DP2.Application.Features.SystemUtility.SuSection.DTO;

using GHB.DP2.Domain.SystemUtility;

public record GetListSuSectionResponse(
    string Id,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode);

public record GetSuSectionResponse(
    string Id,
    string NewId,
    string RefBankOrder,
    decimal MaximumBudget,
    string? Remark,
    string? SupplyMethodCode,
    string? SupplyMethodSpecialTypeCode,
    IEnumerable<GetSuSectionApproverResponse> Approvers);

public record GetSuSectionApproverResponse(
    Guid Id,
    SectionProcessType ProcessType,
    string PositionName,
    string ShortPositionName,
    string InRefCode,
    decimal Budget,
    string SectionId,
    string CommandText);