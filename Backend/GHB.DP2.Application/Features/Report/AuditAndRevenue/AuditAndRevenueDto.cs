namespace GHB.DP2.Application.Features.Report.AuditAndRevenue;

public record AuditAndRevenueRequest(
    Guid? Id,
    string DocumentNumber,
    DateTimeOffset DocumentDate,
    DateTimeOffset SignStartDate,
    DateTimeOffset SignEndDate);

public record AuditAndRevenueDetailDto(
    Guid? Id,
    int Sequence,
    string? Description,
    Guid CaContractDraftVendorId);