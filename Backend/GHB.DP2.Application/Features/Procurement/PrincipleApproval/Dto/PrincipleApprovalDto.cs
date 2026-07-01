namespace GHB.DP2.Application.Features.Procurement.PrincipleApproval.Dto;

using GHB.DP2.Domain.Procurement.PPrincipleApproval;

public record CommitteeRequest(
    Guid? Id,
    Guid UserId,
    CommitteeGroupType GroupType,
    string CommitteePositionsCode,
    string CommitteePositionsName,
    int Sequence
);

public record PerfSupportDataRequest(
    Guid? Id,
    int? TransactionVolume,
    string? ActivityDescription,
    int? PeriodYear,
    int? StartMonth,
    int? EndMonth
);

public record PerfSupportDataDetailsRequest(
    Guid? Id,
    int Sequence,
    string ActivityDescription,
    decimal AccountCountYear1,
    decimal AmountYear1,
    decimal AccountCountYear2,
    decimal AmountYear2
);

public record RoiLoanAndDepositSummaryRequest(
    Guid? Id,
    int Sequence,
    string ActivityDescription,
    decimal AmountYear1,
    decimal AmountYear2,
    decimal AmountYear3
);

public record RoiPerfResultRequest(
    Guid? Id,
    int Sequence,
    PerformanceResultGroup PerformanceResultGroup,
    int Year,
    decimal AccountActual,
    decimal AccountGrowth,
    decimal AmountTarget,
    decimal AmountActual,
    decimal AmountRate,
    decimal AmountGrowth
);

public record BudgetDetail(
    Guid? Id,
    int Sequence,
    string Department,
    string BudgetType,
    string? ProjectCode,
    string AccountNo,
    decimal Budget
);

public record BudgetRequest(
    Guid? Id,
    int Sequence,
    string Description,
    decimal BudgetAmount,
    BudgetDetail[]? Details
);

public record RentalAnalysisRequest(
    Guid? Id,
    int Sequence,
    RentalAnalysisType Type,
    string Description,
    RentalAnalysisDetail[]? Details
);

public record RentalAnalysisDetail(
    Guid? Id,
    int Year,
    decimal Amount
);