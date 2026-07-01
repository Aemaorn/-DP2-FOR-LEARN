namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement;

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Dtos;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn; // ContractGuaranteeReturn
using GHB.DP2.Domain.Procurement.P79Clause2; // P79Clause2
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement; // PettyCashReimbursement
using GHB.DP2.Domain.Procurement.Pw119; // Pw119
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static GHB.DP2.Domain.Raws.Constants.EmployeeConstant;

public record ExpenseDisbursementSourceResponse(
    PExpenseDisbursementSourceType SourceType,
    object? Data,
    string? RefCode,
    string? DepartmentName);

public record ExpenseDisbursementAcceptorResponse(
    Guid Id,
    string AcceptorType,
    Guid UserId,
    string FullName,
    int Sequence,
    string Status,
    bool IsCurrent);

public record ExpenseDisbursementGlAccountResponse(
    Guid Id,
    int Sequence,
    string SoId,
    string BudgetTypeCode,
    string GlAccountCode,
    string? ProjectNumber,
    decimal Amount);

public record GetExpenseDisbursementByIdRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid Id);

public record GetExpenseDisbursementByIdResponse(
    Guid Id,
    PExpenseDisbursementStatus Status,
    PExpenseDisbursementSourceType SourceType,
    Guid SourceId,
    DateTimeOffset Date,
    string? Description,
    bool IsAdvance,
    string? AdvanceName,
    string? AdvancePaymentMethodCode,
    DateTimeOffset? AdvancePaymentDate,
    string? AdvanceBankCode,
    bool? IsInvoiceAmount,
    decimal? InvoiceAmount,
    string? AdvanceBankAccount,
    string? AdvanceBankBranch,
    string? AdvanceBankAccountName,
    string? AdvanceDetail,
    IEnumerable<AcceptorResponse> Acceptors,
    IEnumerable<AssigneeResponse> Assignees,
    IEnumerable<ExpenseDisbursementGlAccountResponse> GlAccounts,
    ExpenseDisbursementSourceResponse Source,
    IEnumerable<AttachmentsDtoWithId> Attachments,
    bool HasPermission);

public class GetExpenseDisbursementByIdEndpoint : ExpenseDisbursementAbstractEndpoint<GetExpenseDisbursementByIdRequest, Results<Ok<GetExpenseDisbursementByIdResponse>, NotFound<string>>>
{
    private readonly Dp2DbContext dbContext;

    public GetExpenseDisbursementByIdEndpoint(
        ILogger<GetExpenseDisbursementByIdEndpoint> logger,
        IFileServiceClient fileServiceClient,
        Dp2DbContext dbContext)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Get("expense-disbursement/{id:guid}");
        this.Description(b => b
                              .WithTags("Procurement/ExpenseDisbursement")
                              .WithName("GetExpenseDisbursementById")
                              .Produces<Ok<GetExpenseDisbursementByIdResponse>>()
                              .ProducesProblem(StatusCodes.Status404NotFound)
                              .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok<GetExpenseDisbursementByIdResponse>, NotFound<string>>> HandleRequestAsync(GetExpenseDisbursementByIdRequest req, CancellationToken ct)
    {
        var entity = await this.dbContext.PExpenseDisbursements
                               .Include(e => e.Assignees)
                               .Include(e => e.Acceptors)
                               .Include(e => e.GlAccounts)
                               .Include(e => e.AuditInfo)
                               .AsNoTracking()
                               .SingleOrDefaultAsync(e => e.Id == PExpenseDisbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลเบิกจ่าย {req.Id}");
        }

        var source = await this.GetSourceAsync(entity, ct);

        var attachments = await this.GetAttachments(entity, UserId.From(req.UserId));

        var userData = await this.dbContext.SuUsers
                                 .Where(user => user.Id == UserId.From(req.UserId))
                                 .Include(user => user.Roles)
                                 .Include(user => user.Employee)
                                 .ThenInclude(employee => employee.View)
                                 .IgnoreQueryFilters()
                                 .FirstOrDefaultAsync(ct);

        var hasPermission = userData?.Employee.PrimaryDepartment?.Id.Value == DivisionCode.Accounting || entity.AuditInfo.CreatedBy == req.UserId;

        var response = new GetExpenseDisbursementByIdResponse(
            entity.Id.Value,
            entity.Status,
            entity.SourceType,
            entity.SourceId,
            entity.Date,
            entity.Description,
            entity.IsAdvance,
            entity.AdvanceName,
            entity.AdvancePaymentMethodCode?.Value,
            entity.AdvancePaymentDate,
            entity.AdvanceBankCode?.Value,
            entity.IsInvoiceAmount ?? false,
            entity.InvoiceAmount,
            entity.AdvanceBankAccount,
            entity.AdvanceBankBranch,
            entity.AdvanceBankAccountName,
            entity.AdvanceDetail,
            MapAcceptors(entity.Acceptors, entity.Status),
            MapAssignees(entity.Assignees),
            MapGlAccounts(entity.GlAccounts),
            source,
            attachments,
            hasPermission);

        return TypedResults.Ok(response);
    }

    private static IEnumerable<AssigneeResponse> MapAssignees(IEnumerable<PExpenseDisbursementAssignee> assignees)
    {
        return assignees.OrderBy(a => a.Sequence)
                        .Select(DelegatorExtensions.DelegatorToAssignee)
                        .Select(a => new AssigneeResponse(
                            a.Id.Value,
                            a.Group,
                            a.Type,
                            a.UserId.Value,
                            a.Sequence,
                            a.FullName,
                            a.PositionName,
                            a.BusinessUnitName,
                            a.Status,
                            a.Remark,
                            a.ActionAt,
                            DelegateeUserId: a.Delegatee?.SuUserId.Value));
    }

    private static IEnumerable<AcceptorResponse> MapAcceptors(IEnumerable<PExpenseDisbursementAcceptor> acceptors, PExpenseDisbursementStatus status)
    {
        return acceptors.OrderBy(a => a.Sequence)
                        .Select(DelegatorExtensions.DelegatorToAcceptor)
                        .Select(a => new AcceptorResponse(
                            a.Id.Value,
                            a.Type,
                            a.UserId.Value,
                            a.Sequence,
                            a.FullName,
                            a.PositionName,
                            a.BusinessUnitName,
                            a.Status,
                            a.Remark,
                            a.ActionAt,
                            IsCurrent: CurrentAcceptor(acceptors, a.Id.Value, status),
                            DelegateeUserId: a.Delegatee?.SuUserId.Value));
    }

    private static bool CurrentAcceptor(IEnumerable<PExpenseDisbursementAcceptor> acceptors, Guid acceptorId, PExpenseDisbursementStatus status)
    {
        if (status != PExpenseDisbursementStatus.WaitingApproval)
        {
            return false;
        }

        var current = acceptors.FirstOrDefault(a =>
            a.Id.Value == acceptorId && a.Type == AcceptorType.Approver);

        if (current == null)
        {
            return false;
        }

        var prev = acceptors
                   .Where(a =>
                       a.Type == AcceptorType.Approver &&
                       a.Sequence < current.Sequence &&
                       a.IsActive)
                   .OrderByDescending(a => a.Sequence)
                   .FirstOrDefault();

        if (prev == null)
        {
            return current.Status != AcceptorStatus.Approved;
        }

        return prev.Status == AcceptorStatus.Approved;
    }

    private static IEnumerable<ExpenseDisbursementGlAccountResponse> MapGlAccounts(IEnumerable<PExpenseDisbursementGlAccount> glAccounts)
    {
        return glAccounts.OrderBy(a => a.Sequence)
                         .Select(a => new ExpenseDisbursementGlAccountResponse(
                             a.Id.Value,
                             a.Sequence,
                             a.SoId,
                             a.BudgetTypeCode.Value,
                             a.GlAccountCode.Value,
                             a.ProjectNumber,
                             a.Amount));
    }

    private async Task<ExpenseDisbursementSourceResponse> GetSourceAsync(PExpenseDisbursement entity, CancellationToken ct)
    {
        return entity.SourceType switch
        {
            PExpenseDisbursementSourceType.W119 => await this.GetDataFromW119Async(entity, ct),
            PExpenseDisbursementSourceType.Clause79_2 => await this.GetDataFromClause79Async(entity, ct),
            PExpenseDisbursementSourceType.ContractGuaranteeReturn => await this.GetDataFromContractGuaranteeReturnAsync(entity, ct),
            PExpenseDisbursementSourceType.PettyCashReimbursement => await this.GetDataFromPettyCashReimbursementAsync(entity, ct),
            _ => new ExpenseDisbursementSourceResponse(entity.SourceType, null, null, null),
        };
    }

    private async Task<ExpenseDisbursementSourceResponse> GetDataFromW119Async(PExpenseDisbursement entity, CancellationToken ct)
    {
        var data = await this.dbContext.Pw119s
                             .AsNoTracking()
                             .Where(w => w.Id == Pw119Id.From(entity.SourceId))
                             .Select(w => new
                             {
                                 w.Id,
                                 w.Pw119Number,
                                 DepartmentName = w.Department.Name,
                                 w.Pw119Date,
                                 w.Subject,
                                 w.Budget,
                                 w.Status,
                                 w.BudgetYear,
                                 SupplyMethod = w.SupplyMethod.Label,
                                 SupplyMethodSpecialType = w.SupplyMethodSpecialType != null ? w.SupplyMethodSpecialType.Label : null,
                                 W119Categories = w.W119Categories.Label,
                                 w.Reason,
                                 w.Source,
                                 Vendors = w.Vendors
                                            .OrderBy(v => v.Sequence)
                                            .Select(v => new
                                            {
                                                v.Id,
                                                v.VendorType,
                                                v.SuVendorId,
                                                v.VendorName,
                                                v.Sequence,
                                                v.TaxNumber,
                                                v.VendorBranchNumber,
                                                VatIncludeTypeCode = v.VatIncludeType != null ? v.VatIncludeType.Code.ToString() : null,
                                                VatIncludeTypeLabel = v.VatIncludeType != null ? v.VatIncludeType.Label : null,
                                                BillTypeCode = v.BillTypeCode.Value,
                                                BillTypeLabel = v.BillType.Label,
                                                v.BillTypeOther,
                                                v.BillBookNo,
                                                v.BillDate,
                                                v.BillDetail,
                                                Parcels = v.VendorParcels
                                                           .OrderBy(p => p.Sequence)
                                                           .Select(p => new
                                                           {
                                                               p.Id,
                                                               p.Sequence,
                                                               p.Item,
                                                               p.ItemDetail,
                                                               p.Quantity,
                                                               UnitCode = p.UnitCode.Value,
                                                               UnitLabel = p.Unit.Label,
                                                               p.UnitPrice,
                                                               p.TotalPrice,
                                                               p.TotalPriceVat,
                                                           }),
                                            }),
                             })
                             .FirstOrDefaultAsync(ct);

        return new ExpenseDisbursementSourceResponse(PExpenseDisbursementSourceType.W119, data, data?.Pw119Number.Value, data?.DepartmentName);
    }

    private async Task<ExpenseDisbursementSourceResponse> GetDataFromClause79Async(PExpenseDisbursement entity, CancellationToken ct)
    {
        var data = await this.dbContext.P79Clause2s
                             .AsNoTracking()
                             .Where(p => p.Id == P79Clause2Id.From(entity.SourceId))
                             .Select(p => new
                             {
                                 p.Id,
                                 p.P79Clause2Number,
                                 DepartmentName = p.Department.Name,
                                 p.P79Clause2Date,
                                 p.Subject,
                                 p.Budget,
                                 p.Status,
                                 p.BudgetYear,
                                 SupplyMethod = p.SupplyMethod.Label,
                                 SupplyMethodType = p.SupplyMethodType.Label,
                                 SupplyMethodSpecialType = p.SupplyMethodSpecialType != null ? p.SupplyMethodSpecialType.Label : null,
                                 p.ReasonItem1,
                                 p.ReasonItem2,
                                 p.ReasonItem3,
                                 p.Source,
                                 Vendors = p.Vendors
                                            .OrderBy(v => v.Sequence)
                                            .Select(v => new
                                            {
                                                v.Id,
                                                v.VendorType,
                                                v.SuVendorId,
                                                v.VendorName,
                                                v.Sequence,
                                                v.TaxNumber,
                                                v.VendorBranchNumber,
                                                VatIncludeTypeCode = v.VatIncludeTypeCode != null ? v.VatIncludeTypeCode.Value.ToString() : null,
                                                VatIncludeTypeLabel = v.VatIncludeType != null ? v.VatIncludeType.Label : null,
                                                BillTypeCode = v.BillTypeCode.Value,
                                                BillTypeLabel = v.BillType.Label,
                                                v.BillTypeOther,
                                                v.BillBookNo,
                                                v.BillDate,
                                                v.BillDetail,
                                                Parcels = v.VendorParcels
                                                           .OrderBy(pv => pv.Sequence)
                                                           .Select(pv => new
                                                           {
                                                               pv.Id,
                                                               pv.Sequence,
                                                               pv.Item,
                                                               pv.ItemDetail,
                                                               pv.Quantity,
                                                               UnitCode = pv.UnitCode.Value,
                                                               UnitLabel = pv.Unit.Label,
                                                               pv.UnitPrice,
                                                               pv.TotalPrice,
                                                               pv.TotalPriceVat,
                                                           }),
                                            }),
                             })
                             .FirstOrDefaultAsync(ct);

        return new ExpenseDisbursementSourceResponse(PExpenseDisbursementSourceType.Clause79_2, data, data?.P79Clause2Number.Value, data?.DepartmentName);
    }

    private async Task<ExpenseDisbursementSourceResponse> GetDataFromContractGuaranteeReturnAsync(PExpenseDisbursement entity, CancellationToken ct)
    {
        var data = await this.dbContext.CmContractGuaranteeReturns
                             .AsNoTracking()
                             .Where(c => c.Id == CmContractGuaranteeReturnId.From(entity.SourceId))
                             .Select(c => new
                             {
                                 c.Id,
                                 c.GuaranteeReturnDate,
                                 c.ReturnAmount,
                                 c.IsDeducted,
                                 c.DeductedAmount,
                                 c.NetReturnAmount,
                                 c.AdditionalComment,
                                 c.Status,
                                 DepartmentName = c.CaContractDraftVendor.ContractDraft.Procurement.Department.Name,
                                 ContractDraftVendor = c.CaContractDraftVendor == null
                                     ? null
                                     : new
                                     {
                                         c.CaContractDraftVendor.Id,
                                         ContractDraftNumber = (string?)c.CaContractDraftVendor.ContractDraftNumber,
                                         c.CaContractDraftVendor.ContractNumber,
                                         c.CaContractDraftVendor.ContractName,
                                         c.CaContractDraftVendor.PoNumber,
                                         c.CaContractDraftVendor.Budget,
                                         c.CaContractDraftVendor.ContractSignedDate,
                                         c.CaContractDraftVendor.Status,
                                         c.CaContractDraftVendor.Email,
                                         ContractTypeLabel = c.CaContractDraftVendor.ContractType != null ? c.CaContractDraftVendor.ContractType.Label : string.Empty,
                                         TemplateLabel = c.CaContractDraftVendor.Template != null ? c.CaContractDraftVendor.Template.Label : string.Empty,
                                         DeliveryLeadTime = c.CaContractDraftVendor.Delivery.LeadTime,
                                         DeliveryDate = c.CaContractDraftVendor.Delivery.Date,
                                         DeliveryLeadTimeTypeLabel = c.CaContractDraftVendor.Delivery.LeadTimeType!.Label,
                                     },
                                 Conditions = c.Conditions
                                               .OrderBy(co => co.Sequence)
                                               .Select(co => new
                                               {
                                                   co.Id,
                                                   co.Sequence,
                                                   co.Description,
                                                   co.IsSatisfied,
                                               }).ToList(),
                             })
                             .FirstOrDefaultAsync(ct);

        return new ExpenseDisbursementSourceResponse(PExpenseDisbursementSourceType.ContractGuaranteeReturn, data, data?.ContractDraftVendor?.ContractDraftNumber, data?.DepartmentName);
    }

    private async Task<ExpenseDisbursementSourceResponse> GetDataFromPettyCashReimbursementAsync(PExpenseDisbursement entity, CancellationToken ct)
    {
        var data = await this.dbContext.PPettyCashReimbursements
                             .AsNoTracking()
                             .Where(p => p.Id == PPettyCashReimbursementId.From(entity.SourceId))
                             .Select(p => new
                             {
                                 p.Id,
                                 p.Number,
                                 p.Status,
                                 p.ReimbursementDate,
                                 p.Subject,
                                 p.Description,
                                 p.ReferredTo,
                                 p.BankAccountName,
                                 p.BankAccountNumber,
                                 Items = p.Items!
                                          .OrderBy(i => i.Sequence)
                                          .Select(i => new
                                          {
                                              i.Id,
                                              i.Sequence,
                                              PettyCashDate = i.PettyCashGlAccount.PettyCash.PettyCashDate,
                                              PettyCashNumber = (string)i.PettyCashGlAccount.PettyCash.PettyCashNumber,
                                              i.PettyCashGlAccount.SoId,
                                              i.PettyCashGlAccount.PettyCash.Subject,
                                              BudgetTypeCode = i.PettyCashGlAccount.BudgetTypeCode.Value,
                                              BudgetTypeLabel = i.PettyCashGlAccount.BudgetType.Label,
                                              GlAccountCode = i.PettyCashGlAccount.GLAccountCode.Value,
                                              GlAccountLabel = i.PettyCashGlAccount.GLAccount.Label,
                                              i.PettyCashGlAccount.ProjectNumber,
                                              Amount = i.PettyCashGlAccount.Amount,
                                              DepartmentName = i.PettyCashGlAccount.PettyCash.Department.Name,
                                          }).ToList(),
                             })
                             .FirstOrDefaultAsync(ct);

        return new ExpenseDisbursementSourceResponse(PExpenseDisbursementSourceType.PettyCashReimbursement, data, data?.Number, data?.Items.FirstOrDefault()?.DepartmentName);
    }
}