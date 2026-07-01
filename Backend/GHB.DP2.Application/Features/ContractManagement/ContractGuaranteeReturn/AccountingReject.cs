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

public record AccountingRejectContractGuaranteeReturnRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string? Remark);

public class AccountingRejectContractGuaranteeReturnEndpoint
    : ContractGuaranteeReturnEndpoint<AccountingRejectContractGuaranteeReturnRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public AccountingRejectContractGuaranteeReturnEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        ILogger<AccountingRejectContractGuaranteeReturnEndpoint> logger,
        IOperationService operationService)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractGuaranteeReturn")
             .WithName("AccountingRejectContractGuaranteeReturn")
             .Accepts<AccountingRejectContractGuaranteeReturnRequest>("application/json"));
        this.Put("contract/{ContractDraftVendorId:guid}/contract-guarantee-return/{Id:guid}/accounting-reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        AccountingRejectContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var guarantee = entity.CmContractGuaranteeReturns.SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการคืนหลักประกันสัญญา");
        }

        if (guarantee.Status != CmContractGuaranteeReturnStatus.WaitingAccountingApproval)
        {
            return TypedResults.BadRequest("ไม่สามารถปฏิเสธได้ เนื่องจากสถานะไม่ใช่รอการอนุมัติจากฝ่ายบัญชี");
        }

        var accountingAcceptors = guarantee.Acceptors
                                           .Where(a => a is
                                           {
                                               Type: AcceptorType.Accounting,
                                               IsActive: true,
                                               Status: AcceptorStatus.Pending
                                           })
                                           .Select(DelegatorExtensions.DelegatorToAcceptor)
                                           .ToArray();

        var acceptor = accountingAcceptors.FirstOrDefault(a => a.Delegatee?.SuUserId == null
            ? a.UserId == UserId.From(req.UserId)
            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptor is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติบัญชีที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        var currentAcceptorUser = guarantee.Acceptors.FirstOrDefault(a => a.Id == acceptor.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติบัญชีที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptor.DelegateeId)
            .Reject(remark: req.Remark);

        guarantee.SetStatus(CmContractGuaranteeReturnStatus.Rejected);

        guarantee.AddActivity(
            new ActivityInfo(
                ActivityLogActionTypeConstant.AccountingReject,
                string.Empty,
                nameof(CmContractGuaranteeReturnStatus.Rejected),
                req.Remark));

        await this.dbContext.SaveChangesAsync(ct);

        var committeeMembers = guarantee.Acceptors
                                        .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                        .ToList();

        foreach (var member in committeeMembers)
        {
            _ = SendNotificationAsync(
                guarantee,
                member.UserId,
                NotificationConstant.AccountingRejected.Title,
                string.Format(
                    NotificationConstant.AccountingRejected.Message,
                    ProgramConstant.ContractGuaranteeReturn.Name,
                    string.Empty));
        }

        return TypedResults.Ok();
    }

    private static async Task SendNotificationAsync(
        CmContractGuaranteeReturn entity,
        UserId userId,
        string title,
        string message)
    {
        await Notification
              .Crate(
                  userId,
                  title,
                  message,
                  NotificationProgram.ContractManagement)
              .SetReferenceId(entity.Id.Value)
              .SetLinkUrl(
                  string.Format(
                      ProgramConstant.ContractGuaranteeReturn.Url,
                      entity.ContractDraftVendorId,
                      entity.Id),
                  "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}
