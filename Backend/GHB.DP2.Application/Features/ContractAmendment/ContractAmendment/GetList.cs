namespace GHB.DP2.Application.Features.ContractAmendment.ContractAmendment;

using System.Linq.Expressions;
using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractAmendment.ContractAmendment;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractAmendmentListRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    DateTimeOffset? SignedDate,
    string? ContractTypeCode,
    CmContractAmendmentType? Type,
    CamContractAmendmentStatus? Status,
    WorkProcess WorkProcess = WorkProcess.InProcess);

public record GetContractAmendmentListItemResponse(
    CamContractAmendmentId Id,
    Guid? ProcurementId,
    string ProcurementType,
    string CamContractAmendmentNumber,
    CmContractAmendmentType Type,
    CamContractAmendmentStatus Status,
    string? Remark,
    ContractDraftVendorId? ContractDraftVendorId,
    string ContractNumber,
    string ContractName,
    string PoNumber,
    DateTimeOffset? ContractSignedDate,
    string EntrepreneurTax,
    string EntrepreneurName,
    decimal Budget,
    string Department,
    string? ContractTypeLabel,
    string? SupplyMethodName);

public record GetStatusCount(
    int All,
    int Draft,
    int InProgress,
    int Completed);

public record GetContractAmendmentListResponse(
    PaginatedQueryResult<GetContractAmendmentListItemResponse> Data,
    GetStatusCount StatusCount);

public class GetContractAmendmentListEndpoint : EndpointBase<GetContractAmendmentListRequest, Ok<GetContractAmendmentListResponse>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractAmendmentListEndpoint(
        Dp2DbContext dbContext,
        ILogger<GetContractAmendmentListEndpoint> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractAmendment")
             .WithName("GetContractAmendmentList")
             .Produces<PaginatedQueryResult<GetContractAmendmentListItemResponse>>());

        this.Get("contract-amendment");
    }

    protected override async ValueTask<Ok<GetContractAmendmentListResponse>> HandleRequestAsync(GetContractAmendmentListRequest req, CancellationToken ct)
    {
        var workProcessStatus = GetWorkProcessStatuses(req.WorkProcess);
        var query = this.BuildBaseQuery();

        query = ApplyFilters(query, req, workProcessStatus);
        var baseQuery = query;

        query = query.WhereIfTrue(req.Status != null, a => a.Status == req.Status);
        query = query.OrderByDescending(o => o.AuditInfo.LastModifiedAt != null ? o.AuditInfo.LastModifiedAt : o.AuditInfo.CreatedAt);

        var paginated = await PaginatedList<CamContractAmendment>.CreateAsync(query, req.PageNumber, req.PageSize, ct);
        var baseRes = await baseQuery.ToListAsync(ct);

        var result = paginated.ToResult(MapToResponse);
        var statusCount = BuildStatusCount(baseRes);

        return TypedResults.Ok(new GetContractAmendmentListResponse(result, statusCount));
    }

    private static List<CamContractAmendmentStatus> GetWorkProcessStatuses(WorkProcess workProcess)
    {
        var statusMap = new Dictionary<WorkProcess, List<CamContractAmendmentStatus>>
        {
            { WorkProcess.InProcess, new List<CamContractAmendmentStatus> { CamContractAmendmentStatus.Draft } },
            { WorkProcess.Related, new List<CamContractAmendmentStatus> { CamContractAmendmentStatus.InProgress } },
            { WorkProcess.Completed, new List<CamContractAmendmentStatus> { CamContractAmendmentStatus.Completed } },
        };

        return statusMap.TryGetValue(workProcess, out var value) ? value : new List<CamContractAmendmentStatus>();
    }

    private IQueryable<CamContractAmendment> BuildBaseQuery()
    {
        return this.dbContext.Set<CamContractAmendment>()
            .Include(a => a.ContractDraftVendor)
                .ThenInclude(v => v.Vendor)
            .Include(a => a.ContractDraftVendor)
                .ThenInclude(cd => cd.ContractDraft)
                    .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.Department)
            .Include(a => a.ContractDraftVendor)
                .ThenInclude(cd => cd.ContractDraft)
                    .ThenInclude(cd => cd.Procurement)
                        .ThenInclude(p => p.SupplyMethod)
            .AsQueryable();
    }

    private static IQueryable<CamContractAmendment> ApplyFilters(
        IQueryable<CamContractAmendment> query,
        GetContractAmendmentListRequest req,
        List<CamContractAmendmentStatus> workProcessStatus)
    {
        query = ApplyKeywordFilter(query, req.Keyword);
        query = ApplySignedDateFilter(query, req.SignedDate);
        query = ApplyContractTypeFilter(query, req.ContractTypeCode);
        query = query.WhereIfTrue(req.Type != null, a => a.Type == req.Type);
        query = query.WhereIfTrue(workProcessStatus.Any(), a => workProcessStatus.Contains(a.Status));
        query = ApplyDepartmentFilter(query, req.DepartmentCode);

        return query;
    }

    private static IQueryable<CamContractAmendment> ApplyKeywordFilter(IQueryable<CamContractAmendment> query, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return query;
        }

        var keywordPattern = $"%{keyword}%";
        var keywordExpression = BuildKeywordExpression(keywordPattern);
        return query.Where(keywordExpression);
    }

    private static Expression<Func<CamContractAmendment, bool>> BuildKeywordExpression(string keywordPattern)
    {
        var contractAmendmentNumberMatch = ContractAmendmentNumberMatches(keywordPattern);
        var contractDraftVendorMatch = ContractDraftVendorMatches(keywordPattern);

        return contractAmendmentNumberMatch.Or(contractDraftVendorMatch);
    }

    private static Expression<Func<CamContractAmendment, bool>> ContractAmendmentNumberMatches(string keywordPattern)
    {
        return a => EF.Functions.ILike((string)a.CamContractAmendmentNumber, keywordPattern);
    }

    private static Expression<Func<CamContractAmendment, bool>> ContractDraftVendorMatches(string keywordPattern)
    {
        return a => a.ContractDraftVendor != null && (
            EF.Functions.ILike(a.ContractDraftVendor.PoNumber, keywordPattern) ||
            EF.Functions.ILike(a.ContractDraftVendor.ContractNumber, keywordPattern) ||
            EF.Functions.ILike(a.ContractDraftVendor.ContractName, keywordPattern) ||
            (a.ContractDraftVendor.Vendor != null && (
                EF.Functions.ILike(a.ContractDraftVendor.Vendor.VendorInfo.SapVendorNumber, keywordPattern) ||
                EF.Functions.ILike(a.ContractDraftVendor.Vendor.VendorInfo.EstablishmentName, keywordPattern))));
    }

    private static IQueryable<CamContractAmendment> ApplySignedDateFilter(IQueryable<CamContractAmendment> query, DateTimeOffset? signedDate)
    {
        return query.WhereIfTrue(
            signedDate.HasValue,
            x => x.ContractDraftVendor.ContractSignedDate.HasValue &&
                 x.ContractDraftVendor.ContractSignedDate.Value.Date == signedDate!.Value.Date && x.ContractDraftVendor.ContractSignedDate.Value <= signedDate!.Value);
    }

    private static IQueryable<CamContractAmendment> ApplyContractTypeFilter(IQueryable<CamContractAmendment> query, string? contractTypeCode)
    {
        return query.WhereIfTrue(
                         !string.IsNullOrWhiteSpace(contractTypeCode) && contractTypeCode != "CMType003",
                         x => x.ContractDraftVendor.ContractType!.Code == ParameterCode.From(contractTypeCode!))
                     .WhereIfTrue(
                         !string.IsNullOrWhiteSpace(contractTypeCode) && contractTypeCode == "CMType003",
                         x => x.ContractDraftVendor.ContractType!.Code == ParameterCode.From("CMRentalType001"));
    }

    private static IQueryable<CamContractAmendment> ApplyDepartmentFilter(IQueryable<CamContractAmendment> query, string? departmentCode)
    {
        return query.WhereIfTrue(!string.IsNullOrWhiteSpace(departmentCode), a =>
            a.ContractDraftVendor != null &&
            a.ContractDraftVendor.ContractDraft != null &&
            a.ContractDraftVendor.ContractDraft.Procurement != null &&
            a.ContractDraftVendor.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(departmentCode!));
    }

    private static GetContractAmendmentListItemResponse MapToResponse(CamContractAmendment a)
    {
        var vendor = a.ContractDraftVendor?.ContractInvitationVendors?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor;

        return new GetContractAmendmentListItemResponse(
            a.Id,
            a.ContractDraftVendor?.ContractDraft.Procurement.Id.Value,
            a.ContractDraftVendor?.ContractDraft.Procurement.Type.ToString() ?? string.Empty,
            a.CamContractAmendmentNumber.Value,
            a.Type,
            a.Status,
            a.Remark,
            a.ContractDraftVendor?.Id,
            a.ContractDraftVendor?.ContractNumber ?? string.Empty,
            a.ContractDraftVendor?.PoNumber ?? string.Empty,
            a.ContractDraftVendor?.ContractName ?? string.Empty,
            a.ContractDraftVendor?.ContractSignedDate,
            vendor != null ? vendor.TaxpayerIdentificationNo : string.Empty,
            vendor != null ? vendor.EstablishmentName : string.Empty,
            a.ContractDraftVendor?.Budget ?? 0m,
            a.ContractDraftVendor?.ContractDraft?.Procurement?.Department?.Name ?? string.Empty,
            a.ContractDraftVendor?.ContractType?.Label ?? string.Empty,
            a.ContractDraftVendor?.ContractDraft?.Procurement?.SupplyMethod?.Label);
    }

    private static GetStatusCount BuildStatusCount(List<CamContractAmendment> baseRes)
    {
        return new GetStatusCount(
            baseRes.Count,
            baseRes.Count(s => s.Status == CamContractAmendmentStatus.Draft),
            baseRes.Count(s => s.Status == CamContractAmendmentStatus.InProgress),
            baseRes.Count(s => s.Status == CamContractAmendmentStatus.Completed));
    }
}