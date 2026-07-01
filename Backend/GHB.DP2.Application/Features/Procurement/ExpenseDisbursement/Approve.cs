namespace GHB.DP2.Application.Features.Procurement.ExpenseDisbursement;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using FastEndpoints;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.Procurement.ExpenseDisbursement.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.Procurement.P79Clause2;
using GHB.DP2.Domain.Procurement.PExpenseDisbursement;
using GHB.DP2.Domain.Procurement.PPettyCashReimbursement;
using GHB.DP2.Domain.Procurement.Pw119;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ApproveExpenseDisbursementCommand
{
    public Guid Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remarks { get; init; }
}

public class ApproveExpenseDisbursementEndpoint : ExpenseDisbursementAbstractEndpoint<ApproveExpenseDisbursementCommand, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public ApproveExpenseDisbursementEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<ApproveExpenseDisbursementEndpoint> logger)
        : base(logger, dbContext, fileServiceClient)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(o => o.WithTags("Procurement/ExpenseDisbursement"));
        this.Put("expense-disbursement/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveExpenseDisbursementCommand req, CancellationToken ct)
    {
        var entity = await this.dbContext.PExpenseDisbursements
                               .Include(e => e.Acceptors)
                               .ThenInclude(e => e.User)
                               .ThenInclude(e => e.Employee)
                               .SingleOrDefaultAsync(e => e.Id == PExpenseDisbursementId.From(req.Id), ct);

        if (entity is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลเบิกจ่าย");
        }

        var source = await this.GetSourceAsync(entity, ct);

        if (entity.Status != PExpenseDisbursementStatus.WaitingApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถอนุมัติเอกสารในสถานะนี้ได้");
        }

        var approverAcceptors = entity.Acceptors
                                      .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                                      .ToArray();

        var currentPending = approverAcceptors
                             .Where(a => a.Status == AcceptorStatus.Pending && a.IsCurrent)
                             .ToArray();

        var acceptor = currentPending.Select(DelegatorExtensions.DelegatorToAcceptor)
                                     .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                         ? a.UserId == req.UserId
                                         : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติที่ใช้งานได้ หรือไม่ได้อยู่ในลำดับปัจจุบัน");
        }

        if (!acceptor.ArePreviousAcceptorsApproved(approverAcceptors))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        var currentAcceptorUser = entity.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Approve(remark: req.Remarks);

        UpdateSequentialCurrents(entity, source?.RefCode);

        entity.EvaluateAcceptorApproval();

        this.dbContext.PExpenseDisbursements.Update(entity);
        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateSequentialCurrents(PExpenseDisbursement entity, string? refCode)
    {
        var approvers = entity.Acceptors
                              .Where(a => a.Type == AcceptorType.Approver && a.IsActive)
                              .OrderBy(a => a.Sequence)
                              .ToList();

        if (approvers.Count == 0)
        {
            return;
        }

        foreach (var a in approvers)
        {
            a.SetCurrent(false);
        }

        var next = approvers.Select(DelegatorExtensions.DelegatorToAcceptor)
                            .FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        var nextSeq = next.Sequence;

        foreach (var a in approvers.Where(a => a.Sequence == nextSeq && a.Status == AcceptorStatus.Pending))
        {
            a.SetCurrent(true);
        }

        var pendingOfType = approvers.Where(a => a.Status == AcceptorStatus.Pending).ToList();
        var isLastPending = pendingOfType.Count == 1;

        if (next.Type == AcceptorType.DepartmentDirectorAgree ||
            (next.Type == AcceptorType.Approver && !isLastPending))
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForLike.Title,
                    refCode);
            }
        }
        else if (next.Type == AcceptorType.Approver && isLastPending)
        {
            foreach (var targetUserId in next.GetNotificationTargets())
            {
                _ = SendNotificationAsync(
                    entity,
                    targetUserId,
                    NotificationConstant.WaitForApprove.Title,
                    refCode);
            }
        }
    }

    private static async Task SendNotificationAsync(PExpenseDisbursement entity, UserId userId, string title, string? refCode)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  string.Format(NotificationConstant.WaitForApprove.Message, ProgramConstant.ExpenseDisbursement.Name, refCode),
                  NotificationProgram.ExpenseDisbursement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ExpenseDisbursement.Url, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
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