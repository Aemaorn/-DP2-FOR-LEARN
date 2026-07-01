namespace GHB.DP2.Application.Features.Procurement.PrincipleApprovalRental.Dto;

using GHB.DP2.Application.Features.Procurement.Invite.Abstract;
using GHB.DP2.Application.Features.Procurement.Jp006.Dto;
using GHB.DP2.Domain.Procurement.PPrincipleApproval;

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

public record EntrepreneursRequest(
    Guid? Id,
    Guid ProcurementId,
    Guid PrincipleApprovalRentalId,
    Guid VendorId,
    int Sequence,
    bool EmailSend,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpResultRemark,
    DateTimeOffset? EgpResultAt,
    ShareholderDto[]? Shareholders,
    EntrepreneursPriceDetailDto[]? Details,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    EntrepreneurResponseAttachment[]? Attachments
);

public record ShareholderDto(
    Guid? CoiId,
    Guid? WatchlistId,
    int Sequence,
    string TaxId,
    string FirstName,
    string LastName,
    bool IsDirector,
    bool? IsShareholder,
    bool? IsJuristic,
    bool WatchlistResult,
    string? WatchlistResultRemark,
    DateTimeOffset? WatchlistResultAt,
    bool CoiResult,
    string? CoiResultRemark,
    DateTimeOffset? CoiResultAt,
    bool EgpResult,
    string? EgpRemark,
    DateTimeOffset? EgpResultAt,
    QualificationResultDto? CoiCheckerResult,
    QualificationResultDto? WatchlistCheckerResult,
    string? CheckType = null
);

public record EntrepreneursPriceDetailRequest(
    Guid? Id,
    Guid ProcurementId,
    Guid PrincipleApprovalRentalId,
    Guid PrincipleApprovalRentalEntrepreneurId,
    EntrepreneursPriceDetailDto[]? EntrepreneursPriceDetails);

public record EntrepreneursPriceDetailDto(
    Guid? Id,
    int Sequence,
    string ParcelName,
    int ParcelQuantity,
    string ParcelUnitCode,
    string VatTypeCode,
    decimal OfferedPrice,
    decimal AgreedPrice,
    string Description
);