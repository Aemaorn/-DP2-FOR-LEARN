namespace GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractGuaranteeReturn.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractGuaranteeReturn;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record AccountingApproveContractGuaranteeReturnRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string? Remark);

public class AccountingApproveContractGuaranteeReturnEndpoint
    : ContractGuaranteeReturnEndpoint<AccountingApproveContractGuaranteeReturnRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AccountingApproveContractGuaranteeReturnEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<AccountingApproveContractGuaranteeReturnEndpoint> logger,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractGuaranteeReturn")
             .WithName("AccountingApproveContractGuaranteeReturn")
             .Accepts<AccountingApproveContractGuaranteeReturnRequest>("application/json"));
        this.Put("contract/{ContractDraftVendorId:guid}/contract-guarantee-return/{Id:guid}/accounting-approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        AccountingApproveContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var guarantee = entity.CmContractGuaranteeReturns.SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการคืนหลักประกันสัญญา");
        }

        if (guarantee.Status != CmContractGuaranteeReturnStatus.WaitingAccountingApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถอนุมัติได้ เนื่องจากสถานะไม่ใช่รอการอนุมัติจากฝ่ายบัญชี");
        }

        var acceptors = guarantee.Acceptors
                                 .Where(a => a.Type == AcceptorType.Accounting && a.IsActive)
                                 .OrderBy(a => a.Sequence)
                                 .ToList();

        var current = acceptors
                      .Select(DelegatorExtensions.DelegatorToAcceptor)
                      .SingleOrDefault(a => a.Delegatee?.SuUserId == null
                          ? a.UserId == UserId.From(req.UserId)
                          : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติบัญชีในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsPreviousApproved(acceptors, current))
        {
            return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
        }

        var currentAcceptorUser = guarantee.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Approve(remark: req.Remark);

        currentAcceptorUser.SetCurrent(false);

        UpdateSequentialCurrents(guarantee);

        var allAcceptorsInGroupApproved =
            acceptors
                .Where(a => a.Type == AcceptorType.Accounting && !a.IsUnableToPerformDuties)
                .All(a => a.Status == AcceptorStatus.Approved);

        if (allAcceptorsInGroupApproved)
        {
            guarantee.SetStatus(CmContractGuaranteeReturnStatus.WaitingDisbursementDate);

            guarantee.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.AccountingApproved,
                    string.Empty,
                    nameof(CmContractGuaranteeReturnStatus.WaitingDisbursementDate),
                    req.Remark));

            var committeeMembers = guarantee.Acceptors
                .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                .ToList();

            foreach (var member in committeeMembers)
            {
                _ = SendNotificationAsync(
                    guarantee,
                    member.UserId,
                    NotificationConstant.AccountingSetDate.Title,
                    string.Format(
                        NotificationConstant.AccountingSetDate.Message,
                        ProgramConstant.ContractGuaranteeReturn.Name,
                        string.Empty));
            }
        }
        else
        {
            guarantee.AddActivity(
                new ActivityInfo(
                    ActivityLogActionTypeConstant.AccountingApproved,
                    string.Empty,
                    nameof(CmContractGuaranteeReturnStatus.WaitingAccountingApproval),
                    req.Remark));

            var nextAccounting = guarantee.Acceptors
                .FirstOrDefault(a => a.Type == AcceptorType.Accounting && a.IsActive && a.IsCurrent);

            if (nextAccounting != null)
            {
                foreach (var targetUserId in nextAccounting.GetNotificationTargets())
                {
                    _ = SendNotificationAsync(
                        guarantee,
                        targetUserId,
                        NotificationConstant.WaitForApprove.Title,
                        string.Format(
                            NotificationConstant.WaitForApprove.Message,
                            ProgramConstant.ContractGuaranteeReturn.Name,
                            string.Empty));
                }
            }
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(CmContractGuaranteeReturn entity, UserId userId, string title, string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(string.Format(ProgramConstant.ContractGuaranteeReturn.Url, entity.ContractDraftVendorId, entity.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }

    private static bool IsPreviousApproved(
        List<CmContractGuaranteeReturnAcceptor> acceptors,
        CmContractGuaranteeReturnAcceptor currentAcceptor)
    {
        if (currentAcceptor.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < currentAcceptor.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    private static void UpdateSequentialCurrents(CmContractGuaranteeReturn guarantee)
    {
        var approvers = guarantee.Acceptors
                                 .Where(a => a.Type == AcceptorType.Accounting && a.IsActive && !a.IsUnableToPerformDuties)
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

        var next = approvers.FirstOrDefault(a => a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        next.SetCurrent(true);
    }
}
