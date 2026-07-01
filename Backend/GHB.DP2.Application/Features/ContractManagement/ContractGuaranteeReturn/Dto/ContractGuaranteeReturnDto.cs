namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Dto;

public record ConditionRequest(
    Guid? Id,
    int Sequence,
    string Description,
    bool IsSatisfied);

public record RequiredDocumentRequest(
    Guid? Id,
    int Sequence,
    string DocumentName,
    bool IsSubmitted);