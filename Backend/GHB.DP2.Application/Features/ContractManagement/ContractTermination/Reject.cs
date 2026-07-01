namespace GHB.DP2.Application.Features.ContractManagement.ContractTermination;

using System.IdentityModel.Tokens.Jwt;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractManagement.ContractTermination.Abstract;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractDraft;
using GHB.DP2.Domain.ContractManagement.CmContractTermination;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public record RejectContractTerminationRequest(
    [property: FromClaim(JwtRegisteredClaimNames.Sub)]
    Guid UserId,
    Guid ContractDraftVendorId,
    Guid Id,
    string Remark);

public class RejectContractTerminationEndpoint : ContractTerminationEndpoint<RejectContractTerminationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectContractTerminationEndpoint(
        Dp2DbContext dbContext,
        ILogger<RejectContractTerminationEndpoint> logger)
        : base(logger, dbContext) => this.dbContext = dbContext;

    public override void Configure()
    {
        this.Options(b =>
            b.WithTags("ContractManagement/ContractTermination")
             .WithName("RejectContractTermination")
             .AllowAnonymous()
             .Accepts<RejectContractTerminationRequest>("application/json"));

        this.Post("contract/{ContractDraftVendorId:guid}/contract-termination/{Id:guid}/reject");
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RejectContractTerminationRequest req, CancellationToken ct)
    {
        var entity = await this.GetByIdAsync(ContractDraftVendorId.From(req.ContractDraftVendorId), ct);

        var termination = entity.CmContractTerminations.FirstOrDefault(s => s.Id == CmContractTerminationId.From(req.Id));

        var delivery = entity.Delivery;

        var suVendor = this.MapSuVendorByType(entity.ContractInvitationVendors, entity.ContractDraft.Procurement.Type);

        if (suVendor is null)
        {
            return TypedResults.NotFound($"ไม่พบข้อมูลผู้ประกอบการ");
        }

        if (termination == null)
        {
            return TypedResults.NotFound("ไม่พบข้อมูลการยกเลิกสัญญา");
        }

        if (termination.Status is CmContractTerminationStatus.WaitingAssign or CmContractTerminationStatus.RejectToAssignee)
        {
            termination
                .SetStatus(CmContractTerminationStatus.Rejected)
                .AddActivity(new ActivityInfo(
                    ActivityLogActionTypeConstant.AssigneeReject,
                    ActivityLogActionTypeConstant.AssigneeReject,
                    nameof(CmContractTerminationStatus.Rejected),
                    req.Remark));

            var lastedDraftOrRejectedCheckPoint = termination.LastedDraftOrRejectedDocument;

            if (lastedDraftOrRejectedCheckPoint != null)
            {
                await this.CopyCheckpointDocumentAsync(termination, lastedDraftOrRejectedCheckPoint, ct);
            }

            await this.dbContext.SaveChangesAsync(ct);

            return TypedResults.Ok();
        }

        var type = termination.Status == CmContractTerminationStatus.WaitingCommitteeApproval
            ? AcceptorType.AcceptanceCommittee
            : AcceptorType.Approver;

        var acceptors = termination.Acceptors
                                   .Where(a => a.Type == type && a.IsActive)
                                   .OrderBy(a => a.Sequence)
                                   .ToList();

        var current = acceptors.FirstOrDefault(a => a.UserId == UserId.From(req.UserId));

        if (type != AcceptorType.AcceptanceCommittee)
        {
            current = acceptors.Select(DelegatorExtensions.DelegatorToAcceptor)
                               .SingleOrDefault(a => a.Delegatee?.SuUserId == null
                                   ? a.UserId == UserId.From(req.UserId)
                                   : a.Delegatee?.SuUserId == UserId.From(req.UserId));
        }

        if (current is null)
        {
            return TypedResults.BadRequest("ไม่พบผู้อนุมัติในกลุ่มหรือขั้นตอนนี้");
        }

        var currentAcceptorUser = termination.Acceptors.FirstOrDefault(a => a.Id == current.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(current.DelegateeId)
            .Reject(remark: req.Remark);

        switch (termination.Status)
        {
            case CmContractTerminationStatus.WaitingCommitteeApproval:
                TryUpdateCommitteeStatusAfterReject(termination, current, req.Remark);

                if (termination.Status == CmContractTerminationStatus.Rejected)
                {
                    var draftOrRejectedCheckpoint2 = termination.LastedDraftOrRejectedDocument;

                    if (draftOrRejectedCheckpoint2 != null)
                    {
                        await this.CopyCheckpointDocumentAsync(termination, draftOrRejectedCheckpoint2, ct);
                    }
                }

                break;

            case CmContractTerminationStatus.WaitingApproval:
                termination.SetStatus(CmContractTerminationStatus.RejectToAssignee, req.Remark);

                // Use WaitingComment document (still has acceptor placeholder tags + user edits)
                // Fallback to WaitingAssign document if no WaitingComment checkpoint exists
                var assignCheckpoint = termination.LastedWaitingCommentDocument
                                       ?? termination.LastedWaitingAssignDocument;

                if (assignCheckpoint != null)
                {
                    await this.CopyCheckpointDocumentAsync(termination, assignCheckpoint, ct);
                }

                break;
        }

        this.dbContext.CmContractTerminations.Update(termination);

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private static void TryUpdateCommitteeStatusAfterReject(CmContractTermination contractTermination, CmContractTerminationAcceptor current, string? remark)
    {
        contractTermination.AddActivity(new ActivityInfo(
            ActivityLogActionTypeConstant.CommitteeReject,
            ActivityLogActionTypeConstant.CommitteeReject,
            contractTermination.Status.ToString(),
            remark));

        if (contractTermination.HasMajorityRejection() || current.IsBoardChairman())
        {
            contractTermination.SetStatus(CmContractTerminationStatus.Rejected);
        }
    }
}