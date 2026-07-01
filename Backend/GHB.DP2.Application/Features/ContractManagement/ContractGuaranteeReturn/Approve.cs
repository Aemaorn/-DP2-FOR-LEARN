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
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record ApproveContractGuaranteeReturnRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string? Remark);

public class ApproveContractGuaranteeReturnEndpoint : ContractGuaranteeReturnEndpoint<ApproveContractGuaranteeReturnRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    private readonly IOperationService operationService;

    public ApproveContractGuaranteeReturnEndpoint(
        Dp2DbContext dbContext,
        IFileServiceClient fileServiceClient,
        IOperationService operationService,
        ILogger<ApproveContractGuaranteeReturnEndpoint> logger)
        : base(logger, dbContext, fileServiceClient, operationService)
    {
        this.dbContext = dbContext;
        this.operationService = operationService;
    }

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractGuaranteeReturn")
             .WithName("ApproveContractGuaranteeReturn")
             .Accepts<ApproveContractGuaranteeReturnEndpoint>("application/json"));
        this.Post("contract/{ContractDraftVendorId:guid}/contract-guarantee-return/{Id:guid}/approve");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(ApproveContractGuaranteeReturnRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var invitationVendor = entity.ContractInvitationVendors;
        var suVendor = entity.ContractDraft.Procurement.Type is ProcurementType.Procurement
            ? invitationVendor?.PurchaseOrderApprovalContract?.Entrepreneur?.SuVendor
            : invitationVendor?.PurchaseOrderApprovalContract?.PrincipleApprovalRentalEntrepreneurs?.Vendor;

        if (suVendor is null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลผู้ประกอบการ");
        }

        var guarantee = entity.CmContractGuaranteeReturns.SingleOrDefault(t => t.Id.Value == req.Id);

        if (guarantee == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการคืนหลักประกันสัญญา");
        }

        var type = guarantee.Status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval
            ? AcceptorType.AcceptanceCommittee
            : AcceptorType.Approver;

        var acceptors = guarantee.Acceptors
                                 .Where(a => a.Type == type && a.IsActive)
                                 .OrderBy(a => a.Sequence)
                                 .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == req.UserId);

        if (type != AcceptorType.AcceptanceCommittee)
        {
            current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .SingleOrDefault(a => a.Delegatee?.SuUserId == null
                                   ? a.UserId == req.UserId
                                   : a.Delegatee?.SuUserId == UserId.From(req.UserId));
        }

        if (current == null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        if (!IsGroupAllowedToApprove(guarantee.Status, current.Type))
        {
            return TypedResults.BadRequest(GetGroupNotAllowedMessage(guarantee.Status));
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

        UpdateSequentialCurrents(guarantee, type);

        switch (guarantee.Status)
        {
            case CmContractGuaranteeReturnStatus.WaitingCommitteeApproval:
                var optionsCommittee = new GuaranteeReturnDocumentOptions(false, false, false, false, false, true);
                _ = await this.UpdateDocumentAsync(entity, guarantee, suVendor, optionsCommittee, ct);

                TryUpdateCommitteeStatus(guarantee, current, req.Remark);

                if (guarantee.Status == CmContractGuaranteeReturnStatus.WaitingAssigned)
                {
                    foreach (var targetUserId in guarantee.Assignees.SelectMany(a => a.GetAssigneeNotificationTargets()))
                    {
                        _ = SendNotificationAsync(
                            guarantee,
                            targetUserId,
                            NotificationConstant.WaitForAssignment.Title,
                            string.Format(
                                NotificationConstant.WaitForAssignment.Message,
                                ProgramConstant.ContractGuaranteeReturn.Name,
                                guarantee.GuaranteeNumber));
                    }
                }

                break;

            case CmContractGuaranteeReturnStatus.WaitingAcceptance:
                if (!IsPreviousApproved(acceptors, current))
                {
                    return TypedResults.BadRequest("ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน");
                }

                guarantee.AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.Approved,
                    ActivityLogActionTypeConstant.Approved,
                    guarantee.Status.ToString(),
                    req.Remark));

                ShouldUpdateStatus(guarantee, current.Type, acceptors);

                if (guarantee.Status == CmContractGuaranteeReturnStatus.WaitingAcceptance)
                {
                    var nextApprover = guarantee.Acceptors
                                                .FirstOrDefault(a => a.Type == AcceptorType.Approver && a.IsActive && a.IsCurrent);

                    if (nextApprover != null)
                    {
                        foreach (var targetUserId in nextApprover.GetNotificationTargets())
                        {
                            _ = SendNotificationAsync(
                                guarantee,
                                targetUserId,
                                NotificationConstant.WaitForApprove.Title,
                                string.Format(
                                    NotificationConstant.WaitForApprove.Message,
                                    ProgramConstant.ContractGuaranteeReturn.Name,
                                    guarantee.GuaranteeNumber));
                        }
                    }
                }

                var options = new GuaranteeReturnDocumentOptions(false, false, true, true, false, true);
                _ = await this.UpdateDocumentAsync(entity, guarantee, suVendor, options, ct);

                break;
        }

        if (guarantee.Status == CmContractGuaranteeReturnStatus.Approved)
        {
            if (guarantee.CaContractDraftVendor.DraftTermsConditions.Guarantee.TypeCode == ParameterCode.From("PBondType001"))
            {
                guarantee.SetStatus(CmContractGuaranteeReturnStatus.WaitingAccountingApproval);

                var committeeMembers = guarantee.Acceptors
                                                .Where(a => a.Type == AcceptorType.AcceptanceCommittee && a.IsActive)
                                                .ToList();

                foreach (var member in committeeMembers)
                {
                    _ = SendNotificationAsync(
                        guarantee,
                        member.UserId,
                        NotificationConstant.InformCommittee.Title,
                        string.Format(NotificationConstant.InformCommittee.Message, ProgramConstant.ContractAcceptancePeriod.Name, guarantee.GuaranteeNumber));
                }

                guarantee.AddActivity(
                    new ActivityInfo(
                        ActivityLogActionTypeConstant.SubmitToAccounting,
                        string.Empty,
                        nameof(CmContractGuaranteeReturnStatus.WaitingAccountingApproval),
                        "ส่งข้อมูลงวดส่งมอบและตรวจรับงานไปยังฝ่ายบัญชีเพื่อดำเนินการต่อ"));

                var expenseAcceptor = await this.operationService.GetDefaultExpenseDisbursementDirectorAsync(ct);

                if (expenseAcceptor is null)
                {
                    this.ThrowError("ไม่พบผู้รับผิดชอบฝ่ายเบิกจ่ายค่าใช้จ่าย", StatusCodes.Status400BadRequest);
                }

                if (guarantee.Acceptors.All(x => x.Type != AcceptorType.Accounting))
                {
                    var userIdsList = new List<UserId> { expenseAcceptor.UserId };

                    var accountAcceptors = await this.operationService.GetDefaultAcceptorAsync(
                        SectionProcessType.ExpenseDisbursement,
                        expenseAcceptor.UserId.Value,
                        guarantee.ReturnAmount,
                        "SectionApprover001",
                        null,
                        ct,
                        false);

                    accountAcceptors?.Iter(x =>
                    {
                        if (!userIdsList.Contains(x.UserId))
                        {
                            userIdsList.Add(x.UserId);
                        }
                    });

                    var usersIncomingList =
                        await this.dbContext.SuUsers
                                  .Include(r => r.Employee)
                                  .ThenInclude(r => r.View)
                                  .Where(w => userIdsList.Contains(w.Id))
                                  .ToArrayAsync(ct);

                    var usersById = usersIncomingList.ToDictionary(u => u.Id);

                    var usersIncomingOrdered = userIdsList
                                               .Where(id => usersById.ContainsKey(id))
                                               .Select(id => usersById[id])
                                               .ToArray();

                    usersIncomingOrdered.Iter((sequence, x) =>
                    {
                        var acceptor = CmContractGuaranteeReturnAcceptor.Create(
                            AcceptorType.Accounting,
                            x,
                            sequence + 1,
                            guarantee.Status);

                        acceptor.SetStatus(AcceptorStatus.Pending);

                        guarantee.AddAcceptor(acceptor);
                    });
                }
                else
                {
                    var accountingAcceptors = guarantee.Acceptors
                                                       .Where(a => a.Type == AcceptorType.Accounting && a.IsActive)
                                                       .ToList();

                    accountingAcceptors.Iter(a => a.SetStatus(AcceptorStatus.Pending));
                }

                var accountingMember = guarantee.Acceptors
                                                .Where(a => a.Type == AcceptorType.Accounting && a.IsActive)
                                                .ToList();

                foreach (var targetUserId in accountingMember.SelectMany(m => m.GetNotificationTargets()))
                {
                    _ = SendNotificationAsync(
                        guarantee,
                        targetUserId,
                        NotificationConstant.AccountingSendApprove.Title,
                        string.Format(NotificationConstant.AccountingSendApprove.Message, ProgramConstant.ContractAcceptancePeriod.Name, guarantee.GuaranteeNumber));
                }
            }

            var options = new GuaranteeReturnDocumentOptions(false, false, true, true, true, true);
            _ = await this.UpdateDocumentAsync(entity, guarantee, suVendor, options, ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void UpdateSequentialCurrents(CmContractGuaranteeReturn guarantee, AcceptorType type)
    {
        var approvers = guarantee.Acceptors
                                 .Where(a => a.Type == type && a.IsActive)
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

        var next = approvers.FirstOrDefault(a =>
            a.Status == AcceptorStatus.Pending);

        if (next is null)
        {
            return;
        }

        if (guarantee.Status == CmContractGuaranteeReturnStatus.WaitingAcceptance && next.Status == AcceptorStatus.Pending)
        {
            next.SetCurrent();
        }
    }

    private static bool IsGroupAllowedToApprove(CmContractGuaranteeReturnStatus status, AcceptorType group)
    {
        if (status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval && group != AcceptorType.AcceptanceCommittee)
        {
            return false;
        }

        if (status == CmContractGuaranteeReturnStatus.WaitingAcceptance && group != AcceptorType.Approver)
        {
            return false;
        }

        return true;
    }

    private static string GetGroupNotAllowedMessage(CmContractGuaranteeReturnStatus status)
    {
        if (status == CmContractGuaranteeReturnStatus.WaitingCommitteeApproval)
        {
            return "อนุมัติได้เฉพาะบุคคลคณะกรรมการตรวจรับเท่านั้น";
        }

        if (status == CmContractGuaranteeReturnStatus.WaitingAcceptance)
        {
            return "อนุมัติได้เฉพาะผู้มีอำนาจเห็นชอบ/อนุมัติเท่านั้น";
        }

        return "ไม่สามารถอนุมัติในสถานะนี้ได้";
    }

    private static bool IsPreviousApproved(List<CmContractGuaranteeReturnAcceptor> acceptors, CmContractGuaranteeReturnAcceptor current)
    {
        if (current.Sequence <= 1)
        {
            return true;
        }

        var prev = acceptors.LastOrDefault(a => a.Sequence < current.Sequence && a.IsActive);

        return prev == null || prev.Status == AcceptorStatus.Approved;
    }

    private static void ShouldUpdateStatus(CmContractGuaranteeReturn entity, AcceptorType type, List<CmContractGuaranteeReturnAcceptor> acceptors)
    {
        var isAllApproved = acceptors
                            .Where(a =>
                                a.Type == type)
                            .All(a => a.Status == AcceptorStatus.Approved);

        if (isAllApproved)
        {
            entity.SetStatusApproved();
        }
    }

    private static void TryUpdateCommitteeStatus(CmContractGuaranteeReturn cmContractGuaranteeReturn, CmContractGuaranteeReturnAcceptor current, string? remark)
    {
        cmContractGuaranteeReturn.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.CommitteeApproved,
            ActivityLogActionTypeConstant.CommitteeApproved,
            cmContractGuaranteeReturn.Status.ToString(),
            remark));

        if (current.IsBoardChairman())
        {
            cmContractGuaranteeReturn.SetStatusWaitingAssigned();
            cmContractGuaranteeReturn.Assignees.Iter(r => r.Pending());
        }
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
}