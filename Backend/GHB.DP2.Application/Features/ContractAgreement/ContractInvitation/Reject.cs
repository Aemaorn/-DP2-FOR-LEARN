namespace GHB.DP2.Application.Features.ContractAgreement.ContractInvitation;

using System.IdentityModel.Tokens.Jwt;
using Codehard.FileService.Client.Abstractions;
using GHB.DP2.Application.Constants;
using GHB.DP2.Application.EventHandlers.SuNotifications;
using GHB.DP2.Application.Extensions;
using GHB.DP2.Application.Features.ContractAgreement.ContractInvitation.Abstract;
using GHB.DP2.Application.Features.Operations;
using GHB.DP2.Domain.Common;
using GHB.DP2.Domain.ContractAgreement.CaContractInvitation;
using GHB.DP2.Domain.Procurement;
using GHB.DP2.Domain.SystemUtility;
using GHB.DP2.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

public class RejectContractInvitationRequest
{
    public Guid ProcurementId { get; init; }

    public Guid Id { get; init; }

    [FromClaim(JwtRegisteredClaimNames.Sub)]
    public Guid UserId { get; init; }

    public string? Remark { get; init; }
}

public class RejectContractInvitationEndpoint
    : ContractInvitationEndpointBase<RejectContractInvitationRequest, Results<Ok, NotFound<string>, BadRequest<string>>>
{
    private readonly Dp2DbContext dbContext;

    public RejectContractInvitationEndpoint(
        Dp2DbContext dbContext,
        IOperationService operationService,
        IFileServiceClient fileServiceClient,
        ILogger<UpsertAttachmentsEndpoint> logger)
        : base(dbContext, operationService, fileServiceClient, logger)
    {
        this.dbContext = dbContext;
    }

    public override void Configure()
    {
        this.Put("procurement/{ProcurementId:guid}/contractInvitation/{Id:guid}/reject");
        this.Options(b =>
            b.WithTags("ContractAgreement/ContractInvitation")
             .WithName("RejectContractInvitation")
             .Produces<Ok>()
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status500InternalServerError));
    }

    protected override async ValueTask<Results<Ok, NotFound<string>, BadRequest<string>>> HandleRequestAsync(
        RejectContractInvitationRequest req,
        CancellationToken ct)
    {
        var contractInvitationExisting = await this.ValidateRequestAsync(req, ct);

        this.AcceptorReject(contractInvitationExisting, req);

        contractInvitationExisting.SetRejected(req.Remark);
        _ = SendNotificationAsync(contractInvitationExisting);

        this.dbContext.CaContractInvitations.Update(contractInvitationExisting);

        foreach (var vendor in contractInvitationExisting.Vendors)
        {
            await this.UpdateDocumentAsync(vendor, false, false, ContractInvitationStatus.Rejected, ct);
        }

        await this.dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }

    private async Task<CaContractInvitation> ValidateRequestAsync(
        RejectContractInvitationRequest req,
        CancellationToken ct)
    {
        var contractInvitationExisting =
            await this.GetById(
                ContractInvitationId.From(req.Id),
                ProcurementId.From(req.ProcurementId),
                ct);

        var canReject =
            contractInvitationExisting.Status is ContractInvitationStatus.WaitingApproval;

        if (!canReject)
        {
            this.ThrowError(
                r =>
                    req.Id,
                $"หนังสือเชิญชวนทำสัญญาที่ระบุไม่อยู่ในสถานะที่สามารถอนุมัติได้ (สถานะปัจจุบัน: {contractInvitationExisting.Status})",
                StatusCodes.Status409Conflict);
        }

        return contractInvitationExisting;
    }

    private void AcceptorReject(
        CaContractInvitation contractInvitationExisting,
        RejectContractInvitationRequest req)
    {
        var acceptors =
            contractInvitationExisting.Acceptors
                                      .Where(a =>
                                          a is
                                          {
                                              Type: AcceptorType.Approver,
                                              Status: AcceptorStatus.Pending,
                                              IsActive: true
                                          })
                                      .Map(DelegatorExtensions.DelegatorToAcceptor)
                                      .ToArray();

        var acceptorExisting =
            acceptors
                .FirstOrDefault(a => a.Delegatee?.SuUserId == null
                                                            ? a.UserId == UserId.From(req.UserId)
                                                            : a.Delegatee?.SuUserId == UserId.From(req.UserId));

        if (acceptorExisting is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        if (!acceptorExisting.ArePreviousAcceptorsApproved(contractInvitationExisting.Acceptors))
        {
            this.ThrowError(
                "ต้องได้รับการอนุมัติจากลำดับก่อนหน้าก่อน",
                StatusCodes.Status409Conflict);
        }

        var currentAcceptorUser =
                contractInvitationExisting.Acceptors
                       .FirstOrDefault(a => a.Id == acceptorExisting.Id);

        if (currentAcceptorUser is null)
        {
            this.ThrowError(
                "ไม่พบผู้อนุมัติที่ใช้งานได้",
                StatusCodes.Status400BadRequest);
        }

        currentAcceptorUser
            .SetDelegatee(acceptorExisting.DelegateeId)
            .Reject(remark: req.Remark);
    }

    private static async Task SendNotificationAsync(CaContractInvitation contractInvitation)
    {
        var notificationProgram = NotificationProgram.ContractAgreement;

        var programUrl = contractInvitation.Procurement.Type == ProcurementType.Rent
            ? ProgramConstant.BranchSpaceRent.Url
            : ProgramConstant.PreProcurementAppointment.Url;

        await Notification
              .Crate(
                  UserId.From(contractInvitation.AuditInfo.CreatedBy),
                  NotificationConstant.ReturnToCreator.Title,
                  string.Format(NotificationConstant.ReturnToCreator.Message, ProgramConstant.ContractInvitation.Name, contractInvitation.Procurement.ProcurementNumber),
                  notificationProgram)
              .SetReferenceId(contractInvitation.Id.Value)
              .SetLinkUrl(string.Format(programUrl, contractInvitation.Procurement.Id), "ดูรายละเอียด")
              .PublishAsync(CancellationToken.None);
    }
}