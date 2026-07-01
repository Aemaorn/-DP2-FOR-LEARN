namespace GHB.DP2.Application.Features.ContractAgreement.ContractDraft;

using Codehard.Infrastructure.EntityFramework;
using GHB.DP2.Application.Common;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.Raws;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record GetContractDraftVendorListWithSupplyMethodCountsRequest(
    int PageNumber,
    int PageSize,
    string? Keyword,
    string? DepartmentCode,
    int? BudgetYear,
    string? SupplyMethodCode);

public record ContractDraftVendorListItem(
    Guid Id,
    string ContractDraftNumber,
    string? ContractNumber,
    string? PoNumber,
    string SupplyMethodName,
    string DepartmentName,
    int? BudgetYear,
    DateTimeOffset? ContractSignedDate,
    string ContractTypeName,
    string ContractName,
    string VendorName,
    decimal Budget);

public record ContractDraftVendorSupplyMethodCounts(int All, int SMethod002, int SMethod004);

public record GetContractDraftVendorListWithSupplyMethodCountsResult(
    ContractDraftVendorSupplyMethodCounts Counts,
    PaginatedQueryResult<ContractDraftVendorListItem> Data);

public class GetContractDraftVendorListWithSupplyMethodCounts : EndpointBase<GetContractDraftVendorListWithSupplyMethodCountsRequest, Ok<GetContractDraftVendorListWithSupplyMethodCountsResult>>
{
    private readonly Dp2DbContext dbContext;

    public GetContractDraftVendorListWithSupplyMethodCounts(
        Dp2DbContext dbContext,
        ILogger<GetContractDraftVendorListWithSupplyMethodCounts> logger)
        : base(logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(o => o.WithTags("ContractDraft").WithName("GetContractDraftVendorListWithSupplyMethodCounts"));
        this.Get("contract-draft/list-with-supply-method-counts");
    }

    protected override async ValueTask<Ok<GetContractDraftVendorListWithSupplyMethodCountsResult>> HandleRequestAsync(
        GetContractDraftVendorListWithSupplyMethodCountsRequest req,
        CancellationToken ct)
    {
        var baseQuery = this.dbContext.CaContractDraftVendors
                            .Where(v => v.Status == ContractDraftVendorStatus.Approved && v.ContractSignedDate != null)
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.Keyword), v =>
                                EF.Functions.ILike(v.ContractName, $"%{req.Keyword}%") ||
                                EF.Functions.ILike(v.ContractNumber!, $"%{req.Keyword}%") ||
                                this.dbContext.SuVendors.Any(sv => sv.Id == v.Vendor.VendorId && EF.Functions.ILike(sv.EstablishmentName, $"%{req.Keyword}%")) ||
                                EF.Functions.ILike(v.PoNumber, $"%{req.Keyword}%"))
                            .WhereIfTrue(!string.IsNullOrWhiteSpace(req.DepartmentCode), v => v.ContractDraft.Procurement.DepartmentId == BusinessUnitId.From(req.DepartmentCode!))
                            .WhereIfTrue(req.BudgetYear.HasValue, v => v.ContractDraft.Procurement.BudgetYear == req.BudgetYear);

        var allCount = await baseQuery.CountAsync(ct);
        var sixtyCode = ParameterCode.From(SupplyMethodConstant.Sixty);
        var eightyCode = ParameterCode.From(SupplyMethodConstant.Eighty);

        var sixtyCount = await baseQuery.Where(v => v.ContractDraft.Procurement.SupplyMethodCode == sixtyCode).CountAsync(ct);
        var eightyCount = await baseQuery.Where(v => v.ContractDraft.Procurement.SupplyMethodCode == eightyCode).CountAsync(ct);

        var listQuery = baseQuery
                      .WhereIfTrue(!string.IsNullOrWhiteSpace(req.SupplyMethodCode), v => v.ContractDraft.Procurement.SupplyMethodCode == ParameterCode.From(req.SupplyMethodCode!))
                      .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.SupplyMethod)
                      .Include(v => v.ContractDraft).ThenInclude(cd => cd.Procurement).ThenInclude(p => p.Department)
                      .Include(v => v.ContractType)
                      .Include(v => v.Vendor)
                      .ThenInclude(v => v.VendorInfo)
                      .OrderByDescending(v => v.ContractDraftNumber)
                      .AsSplitQuery()
                      .AsNoTracking();

        var page = await PaginatedList<CaContractDraftVendor>.CreateAsync(listQuery, req.PageNumber, req.PageSize, ct);

        var vendorIds = page.Select(v => v.Vendor.VendorId).Distinct().ToList();
        var vendorNames = await this.dbContext.SuVendors
            .Where(sv => vendorIds.Contains(sv.Id))
            .Select(sv => new { sv.Id, sv.EstablishmentName })
            .ToDictionaryAsync(sv => sv.Id, sv => sv.EstablishmentName, ct);

        var pageResult = page.ToResult(v => new ContractDraftVendorListItem(
            v.Id.Value,
            v.ContractDraftNumber.Value,
            v.ContractNumber,
            v.PoNumber,
            v.ContractDraft.Procurement.SupplyMethod.Label,
            v.ContractDraft.Procurement.Department.Name,
            v.ContractDraft.Procurement.BudgetYear,
            v.ContractSignedDate,
            v.ContractType?.Label ?? string.Empty,
            v.ContractName,
            vendorNames.GetValueOrDefault(v.Vendor.VendorId) ?? string.Empty,
            v.Agreement?.TotalAmount ?? 0));

        var counts = new ContractDraftVendorSupplyMethodCounts(allCount, sixtyCount, eightyCount);

        return TypedResults.Ok(new GetContractDraftVendorListWithSupplyMethodCountsResult(counts, pageResult));
    }
}