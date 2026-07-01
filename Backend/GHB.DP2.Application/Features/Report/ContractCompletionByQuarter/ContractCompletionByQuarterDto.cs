namespace GHB.DP2.Application.Features.Report.ContractCompletionByQuarter;

public record RpContractCompletionByQuarterDetailDto(
    Guid? Id,
    int Sequence,
    string? Description,
    Guid CaContractDraftVendorId);